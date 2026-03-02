using System.Net.Http.Headers;
using System.Text.Json;
using System.Text;

namespace GrokImagine.Client;

public class GrokImagineClient
{
    private static readonly HttpClient _httpClient = new();

    public async Task GenerateImageAsync(string apiKey, string model, string sourcePosePath, string? sourceStickPath, string? sourceStylePath, string outputPath, string prompt, string? negativePrompt = null, double? imageStrength = null, double? guidanceScale = null, int? steps = null)
    {
        if (!File.Exists(sourcePosePath))
            throw new FileNotFoundException("Source pose image not found.", sourcePosePath);
        if (sourceStickPath != null && !File.Exists(sourceStickPath))
            throw new FileNotFoundException("Source stick image not found.", sourceStickPath);
        if (sourceStylePath != null && !File.Exists(sourceStylePath))
            throw new FileNotFoundException("Source style image not found.", sourceStylePath);

        var images = new List<string>();

        // Load pose image
        var poseBytes = await File.ReadAllBytesAsync(sourcePosePath);
        var poseBase64 = Convert.ToBase64String(poseBytes);
        var poseExt = Path.GetExtension(sourcePosePath).ToLowerInvariant();
        var poseMime = poseExt switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/png"
        };
        var poseData = $"data:{poseMime};base64,{poseBase64}";
        images.Add(poseData);

        // Load stick image if provided
        if (sourceStickPath != null)
        {
            var stickBytes = await File.ReadAllBytesAsync(sourceStickPath);
            var stickBase64 = Convert.ToBase64String(stickBytes);
            var stickExt = Path.GetExtension(sourceStickPath).ToLowerInvariant();
            var stickMime = stickExt switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/png"
            };
            var stickData = $"data:{stickMime};base64,{stickBase64}";
            images.Add(stickData);
        }

        // Load style image if provided
        if (sourceStylePath != null)
        {
            var styleBytes = await File.ReadAllBytesAsync(sourceStylePath);
            var styleBase64 = Convert.ToBase64String(styleBytes);
            var styleExt = Path.GetExtension(sourceStylePath).ToLowerInvariant();
            var styleMime = styleExt switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                _ => "image/png"
            };
            var styleData = $"data:{styleMime};base64,{styleBase64}";
            images.Add(styleData);
        }

        var imageData = images.ToArray();

        // Generate image from prompt and image
        var generationPrompt = prompt;
        if (!string.IsNullOrEmpty(negativePrompt))
        {
            generationPrompt += $" Avoid: {negativePrompt}";
        }

        var requestBody = new Dictionary<string, object>
        {
            ["model"] = model,
            ["prompt"] = generationPrompt,
            ["image"] = imageData,
            ["n"] = 1,
            ["response_format"] = "url"
        };

        if (imageStrength.HasValue) requestBody["image_strength"] = imageStrength.Value;
        if (guidanceScale.HasValue) requestBody["guidance_scale"] = guidanceScale.Value;
        if (steps.HasValue) requestBody["steps"] = steps.Value;

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.ai/v1/images/generations")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"API request failed: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<ImageGenerationResponse>(responseJson);
        if (result?.data == null || result.data.Length == 0)
            throw new Exception("No image generated.");

        var imageUrl = result.data[0].url;
        using var downloadResponse = await _httpClient.GetAsync(imageUrl);
        downloadResponse.EnsureSuccessStatusCode();

        var imageDataBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        await File.WriteAllBytesAsync(outputPath, imageDataBytes);
    }

    private async Task<string> GetImageDescriptionAsync(string apiKey, string model, string imageData, string prompt)
    {
        var messages = new[]
        {
            new
            {
                role = "user",
                content = new object[]
                {
                    new { type = "text", text = prompt },
                    new { type = "image_url", image_url = new { url = imageData } }
                }
            }
        };

        var requestBody = new
        {
            model = model,
            messages = messages,
            max_tokens = 500
        };

        var json = JsonSerializer.Serialize(requestBody);
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.x.ai/v1/chat/completions")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Chat API request failed: {response.StatusCode} - {errorContent}");
        }

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return content ?? "No description generated.";
    }
}

public class ImageGenerationResponse
{
    public ImageData[] data { get; set; }
}

public class ImageData
{
    public string url { get; set; }
}
