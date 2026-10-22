namespace Beyond90.Api;

public sealed class FoundryOptions
{
    public bool Enabled { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Deployment { get; set; } = "gpt-4.1-mini";
    public string ApiKey { get; set; } = string.Empty;

    public bool IsValid() => !Enabled ||
        (!string.IsNullOrWhiteSpace(ApiKey) && !string.IsNullOrWhiteSpace(Deployment) &&
         Uri.TryCreate(Endpoint, UriKind.Absolute, out var uri) && uri.Scheme == "https" &&
         (uri.Host.EndsWith(".services.ai.azure.com", StringComparison.OrdinalIgnoreCase) ||
          uri.Host.EndsWith(".openai.azure.com", StringComparison.OrdinalIgnoreCase)) &&
         uri.AbsolutePath == "/openai/v1/responses" && uri.IsDefaultPort &&
         uri.Query.Length == 0 && uri.Fragment.Length == 0 && uri.UserInfo.Length == 0);
}
