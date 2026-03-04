# ForgeCli

`ForgeCli` is a command-line tool for Forge users to:

- scaffold a new app project (`app.sml`, `main.sml`, `main.sms`)
- validate SML/SMS syntax using `SMLCore` + `SMSCore`
- generate `main.sml` / `main.sms` from an AI prompt with retry-on-validation-feedback

## Commands

```bash
forgecli new <name> [--output <dir>] [--force]
forgecli validate [--project <dir>] [--verbose]
forgecli generate --prompt "..." [--project <dir>] [--provider mock|grok|openrouter]
                 [--model <name>] [--api-key <key>] [--max-iterations <n>] [--dry-run]
```

## Provider Notes

- `mock`: local deterministic generator (no network)
- `grok`: OpenAI-compatible API (`https://api.x.ai/v1/chat/completions`)
  - API key via `--api-key` or `GROK_API_KEY`
- `openrouter`: OpenAI-compatible API (`https://openrouter.ai/api/v1/chat/completions`)
  - API key via `--api-key` or `OPENROUTER_API_KEY`

## Current Validation Scope

- `app.sml` parse check
- `main.sml` parse check + parser warnings
- `main.sms` syntax validation

Next iterations can add deeper compatibility checks against runtime schemas.
