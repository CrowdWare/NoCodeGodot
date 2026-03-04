# ForgeAiLib

Unified AI integration library for Forge.

## Scope

- Grok chat/text completion
- Grok vision image analysis (image + prompt)
- Grok image-to-image stylization (pose/style/extra)
- Grok video-to-video stylization (submit + poll + download)

## Environment

Set API key:

```bash
export GROK_API_KEY=...
```

## Runtime Integration

`ForgeRunner` exposes an SMS global object `ai`:

- `ai.isConfigured()`
- `ai.describeImage(imagePath, prompt?, model?)`
- `ai.stylizeImage(posePath, outputPath, prompt, stylePath?, extraPath?, negativePrompt?, model?)`
- `ai.stylizeVideo(inputVideoPath, outputPath, prompt, negativePrompt?, model?)`

`outputPath` supports `<version>` placeholder, e.g. `renders/frame_<version>.png`.
