using Microsoft.AspNetCore.Mvc;
using OpenHosting.Services;
using System.Text;

namespace OpenHosting.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebhookController : ControllerBase
{
    private readonly ContainerService _containerService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(ContainerService containerService, ILogger<WebhookController> logger)
    {
        _containerService = containerService;
        _logger = logger;
    }

    [HttpPost("github")]
    public async Task<IActionResult> GitHubWebhook()
    {
        try
        {
            // Get the signature from headers
            var signature = Request.Headers["X-Hub-Signature-256"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("GitHub webhook received without signature");
                return BadRequest("Missing signature");
            }

            // Read the payload
            using var reader = new StreamReader(Request.Body, Encoding.UTF8);
            var payload = await reader.ReadToEndAsync();

            // Get the webhook secret from configuration (you should set this in appsettings.json)
            var secret = Environment.GetEnvironmentVariable("GITHUB_WEBHOOK_SECRET") ?? "your-webhook-secret";

            // Handle the webhook
            var success = await _containerService.HandleGitHubWebhookAsync(payload, signature, secret);
            
            if (success)
            {
                _logger.LogInformation("GitHub webhook processed successfully");
                return Ok("Webhook processed successfully");
            }
            else
            {
                _logger.LogWarning("GitHub webhook processing failed");
                return BadRequest("Webhook processing failed");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing GitHub webhook");
            return StatusCode(500, "Internal server error");
        }
    }
}
