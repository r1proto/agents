using GitLabWebhook.Interfaces;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<IIssueEventDispatcher, NoOpIssueEventDispatcher>();

var app = builder.Build();

// Warn at startup if the webhook secret is not configured.
var secret = app.Configuration["GitLab:WebhookSecret"];
if (string.IsNullOrWhiteSpace(secret))
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogWarning(
        "GitLab:WebhookSecret is not configured. All webhook requests will be rejected with 401.");
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>
/// Default no-op dispatcher. Replace with a real implementation to forward events downstream.
/// </summary>
internal sealed class NoOpIssueEventDispatcher : IIssueEventDispatcher
{
    public Task DispatchAsync(GitLabWebhook.Models.GitLabIssueEvent issueEvent, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[NoOpDispatcher] Received issue event: action={issueEvent.ObjectAttributes?.Action}, iid={issueEvent.ObjectAttributes?.Iid}");
        return Task.CompletedTask;
    }
}

// Required for WebApplicationFactory<Program> in integration tests.
public partial class Program { }
