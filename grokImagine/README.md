# GrokImagine

A C# API and CLI for generating rendered images from greybox images using GrokImagine (xAI API).

## Features

- Send greybox images to GrokImagine for rendering.
- Supports prompt and optional negative prompt.
- Downloads generated PNG images.

## API Usage

```csharp
using GrokImagine.Client;

var client = new GrokImagineClient();
await client.GenerateImageAsync(
    apiKey: "your-xai-api-key",
    model: "grok-imagine-image",
    sourceImagePath: "path/to/greybox.png",
    outputPath: "path/to/rendered.png",
    prompt: "Render this greybox level as a realistic cyberpunk city",
    negativePrompt: "blurry, low quality" // optional
);
```

## CLI Usage

Build:

```bash
dotnet build
```

Use an SML config file (recommended):

Create `prompts/img2img.sml`:

```
Img2Img{
    model: "grok-imagine-image"
    prompt: "Render this greybox level realistically"
    negativePrompt: ""
    input: "pose1.png"
    output: "pose1_<version>.png"
}
```

Run:

```bash
dotnet run --project GrokImagine.Cli -- prompts/img2img.sml
```

Or use the sample script:

```bash
./run.sh prompts/img2img.sml
```

The output filename auto-increments version (e.g., pose1_1.png, pose1_2.png).

API key from `GROK_API_KEY` env var.

### Parameters

- `--apikey`: xAI API key (optional if GROK_API_KEY env var is set)
- `--model`: Model name (e.g., grok-imagine-image)
- `--inputfile`: Filename in /input folder (e.g., greybox.png)
- `--outputfile`: Filename in /output folder (e.g., rendered.png)
- `--prompt`: Rendering prompt (text)
- `--promptfile`: Path to SML prompt file (alternative to --prompt)
- `--negative`: Optional negative prompt

## Requirements

- .NET 8+
- xAI API key

## Notes

- Assumes xAI API supports img2img with base64 image input.
- Output is PNG.