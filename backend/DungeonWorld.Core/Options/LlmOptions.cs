namespace DungeonWorld.Core.Options;

public class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>
    /// Base URL of an OpenAI-compatible Chat Completions endpoint.
    /// e.g. https://api.openai.com/v1, https://api.openai.com/v1 (Azure-compatible gateways),
    /// or http://localhost:11434/v1 for Ollama.
    /// </summary>
    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>Model identifier, e.g. gpt-4o-mini or a local Ollama model.</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>API key. Leave empty for keyless providers such as local Ollama.</summary>
    public string ApiKey { get; set; } = "";

    /// <summary>Maximum PDF pages sent to the model in one call.</summary>
    public int ChunkPageSize { get; set; } = 8;

    /// <summary>Request timeout in seconds for a single model call.</summary>
    public int TimeoutSeconds { get; set; } = 120;

    /// <summary>Ask the provider for strict JSON output where supported.</summary>
    public bool JsonMode { get; set; } = true;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ApiKey) || IsLocalEndpoint;

    private bool IsLocalEndpoint =>
        Endpoint.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
        Endpoint.Contains("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
        Endpoint.Contains("host.docker.internal", StringComparison.OrdinalIgnoreCase);
}
