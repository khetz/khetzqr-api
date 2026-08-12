# 🔳 KhetzQR — .NET Backend

A lightweight .NET API that powers the AI generation feature for KhetzQR. Takes a natural language prompt, calls the **Claude API**, and returns structured QR code configuration JSON to the Angular client.

## Tech Stack

- **.NET 10** with Minimal APIs
- **Claude API** (Anthropic) for AI-powered QR config generation

## How It Works

1. The Angular client sends a text prompt describing the desired QR code (e.g. "a blue QR code with rounded corners pointing to my portfolio").
2. The API forwards the prompt to Claude with instructions to return structured JSON matching the app's QR configuration schema.
3. Claude's response is parsed and returned to the client.
4. The Angular AI generation component emits the config to the QR preview component for instant rendering.

## Getting Started

### Prerequisites

- .NET 10 SDK

### Configuration

Add your Anthropic API key in `appsettings.Development.json`:

```json
{
  "Anthropic": {
    "ApiKey": "your-api-key"
  }
}
```

### Run

```bash
dotnet run
```

The API runs at `https://localhost:7260` by default.

## Related

- **Frontend** — Angular client with QR preview and AI generation UI ([frontend repo](https://github.com/khetz/qr-code-generator))

## License

MIT