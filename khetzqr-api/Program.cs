using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient("Anthropic", client =>
{
    client.BaseAddress = new Uri("https://api.anthropic.com/");
    client.DefaultRequestHeaders.Add("x-api-key", builder.Configuration["Anthropic:ApiKey"]);
    client.DefaultRequestHeaders.Add("antrhopic-version", "2023-06-01");
});

var app = builder.Build();
app.UseCors();

app.MapPost("/api/qr/generate", async (QrRequest request, IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("Anthropic");

    var systemPrompt = """
    You are a QR code content generator. The user will describe what they want a QR code for.
        Your job is to determine the QR type and generate the correctly formatted content string.

        Supported QR types and their formats:

        1. URL — just return the URL as-is (add https:// if missing)
           Example: "https://linkedin.com/in/vukheta"

        2. WiFi — format: WIFI:T:{security};S:{ssid};P:{password};;
           Security types: WPA, WPA2, WEP, nopass
           Example: "WIFI:T:WPA2;S:HomeNet;P:mypassword;;"

        3. vCard — format:
           BEGIN:VCARD
           VERSION:3.0
           N:{lastname};{firstname}
           FN:{fullname}
           TEL:{phone}
           EMAIL:{email}
           ORG:{company}
           TITLE:{title}
           URL:{website}
           END:VCARD
           (Only include fields the user provides)

        4. Email — format: mailto:{address}?subject={subject}&body={body}
           (URL-encode the subject and body)

        5. SMS — format: smsto:{number}:{message}

        6. Phone — format: tel:{number}

        7. Text — plain text as provided

        Respond ONLY with valid JSON in this exact format, no markdown, no backticks:
        {
            "type": "url|wifi|vcard|email|sms|phone|text",
            "label": "Short description of what this QR does",
            "content": "The formatted QR string ready to encode",
            "fields": { "field1": "value1", "field2": "value2" }
        }

        The "fields" object should contain the parsed key-value pairs so the UI can display them.
        For example, for WiFi: {"ssid": "HomeNet", "password": "mypassword", "security": "WPA2"}
        For vCard: {"name": "Vukheta Maluleke", "phone": "072 745 4051", "email": "vukheta99@gmail.com"}

        If the input is ambiguous or you need more info, still make your best guess and return valid JSON.
    """;

    var body = new
    {
        model = "claude-sonnet-4-6",
        max_tokens = 500,
        system = systemPrompt,
        messages = new[]
        {
            new { role = "user", content = request.Prompt }
        }
    };

    var response = await client.PatchAsJsonAsync("v1/messages", body);
    var responseBody = await response.Content.ReadAsStringAsync();

    if (!response.IsSuccessStatusCode)
    {
        return Results.Problem($"Anthropic API error: {responseBody}, statusCode: 502");
    }

    var anthropicResponse = JsonSerializer.Deserialize<AnthropicResponse>(responseBody);
    var textContent = anthropicResponse?.Content?.FirstOrDefault(c => c.Type == "text")?.Text;

    if (string.IsNullOrEmpty(textContent))
    {
        return Results.Problem("No response from AI", statusCode: 502);
    }


    try
    {
        var qrResult = JsonSerializer.Deserialize<QrGenerationResult>(textContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return Results.Ok(qrResult);
    }
    catch (JsonException)
    {
        return Results.Ok(new QrGenerationResult
        {
            Type = "text",
            Label = "Text QR Code",
            Content = textContent,
            Fields = new Dictionary<string, string> { { "text", textContent } }
        });
    }
});


app.Run();

// --- Models ---

public record QrRequest(string Prompt);

public class QrGenerationResult
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("label")]
    public string Label { get; set; } = "";

    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("fields")]
    public Dictionary<string, string> Fields { get; set; } = new();
}

public class AnthropicResponse
{
    [JsonPropertyName("content")]
    public List<ContentBlock> Content { get; set; } = new();
}

public class ContentBlock
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}
