using DungeonWorld.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace DungeonWorld.Infrastructure.Ai;

public interface ILlmClient
{
    /// <summary>
    /// Sends a chat completion request and returns the model's message content.
    /// Callers are responsible for requesting JSON output via the prompt.
    /// </summary>
    Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Minimal OpenAI-compatible Chat Completions client. Works with OpenAI, Azure
/// gateways, Ollama (/v1), LM Studio, llama.cpp and other OpenAI-compatible servers.
/// </summary>
public sealed class OpenAiCompatibleLlmClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly IOptions<LlmOptions> _options;
    private readonly ILogger<OpenAiCompatibleLlmClient> _logger;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public OpenAiCompatibleLlmClient(
        HttpClient http,
        IOptions<LlmOptions> options,
        ILogger<OpenAiCompatibleLlmClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    public async Task<string> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        var opts = _options.Value;
        if (string.IsNullOrWhiteSpace(opts.Endpoint))
            throw new InvalidOperationException("Llm:Endpoint is not configured.");

        var request = new ChatCompletionRequest
        {
            Model = opts.Model,
            Temperature = 0,
            ResponseFormat = opts.JsonMode ? new ResponseFormat { Type = "json_object" } : null,
            Messages = new[]
            {
                new ChatMessage { Role = "system", Content = systemPrompt },
                new ChatMessage { Role = "user", Content = userPrompt },
            },
        };

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, BuildUrl(opts.Endpoint))
        {
            Content = new StringContent(JsonSerializer.Serialize(request, SerializerOptions), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(opts.ApiKey))
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opts.ApiKey);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(TimeSpan.FromSeconds(opts.TimeoutSeconds <= 0 ? 120 : opts.TimeoutSeconds));

        using var response = await _http.SendAsync(requestMessage, timeoutCts.Token).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(timeoutCts.Token).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("LLM request failed ({StatusCode}): {Body}",
                (int)response.StatusCode, Truncate(body, 800));
            throw new HttpRequestException($"LLM request failed with status {(int)response.StatusCode}.");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        if (string.IsNullOrWhiteSpace(content))
            throw new InvalidOperationException("LLM returned empty content.");

        return content;
    }

    private static string BuildUrl(string endpoint) =>
        $"{endpoint.TrimEnd('/')}/chat/completions";

    private static string Truncate(string s, int max)
    {
        if (s.Length <= max) return s;
        return s[..max] + "...";
    }

    private sealed class ChatCompletionRequest
    {
        public string Model { get; set; } = "";
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();
        public double Temperature { get; set; }
        public ResponseFormat? ResponseFormat { get; set; }
    }

    private sealed class ChatMessage
    {
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
    }

    private sealed class ResponseFormat
    {
        public string Type { get; set; } = "";
    }
}
