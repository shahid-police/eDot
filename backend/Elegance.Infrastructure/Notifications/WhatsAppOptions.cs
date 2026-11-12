namespace Elegance.Infrastructure.Notifications;

public class WhatsAppOptions
{
    public const string SectionName = "WhatsApp";

    // "Mock" (default - safe everywhere, never contacts a real API) or "CloudApi" (Meta's
    // official WhatsApp Business Cloud API, added in a later phase once real credentials and an
    // approved message template exist).
    public string Provider { get; set; } = "Mock";

    public string AccessToken { get; set; } = "";
    public string PhoneNumberId { get; set; } = "";
    public string BusinessAccountId { get; set; } = "";

    // Verifies Meta's GET /webhook handshake (WhatsAppWebhookController.Verify) - a value only
    // you and Meta's app dashboard know, not a secret used for anything cryptographic.
    public string WebhookVerifyToken { get; set; } = "";

    // Verifies the X-Hub-Signature-256 header on every POST webhook delivery (the Meta App's own
    // "App Secret", a different credential from the access token above) - without this, anyone
    // who finds the webhook URL could forge delivery-status updates.
    public string AppSecret { get; set; } = "";

    public string ApiVersion { get; set; } = "v21.0";
    public string TemplateName { get; set; } = "back_in_stock_notification";
    public string TemplateLanguage { get; set; } = "en";

    // Used to build the product link in the WhatsApp message ({{4}} in the spec's template).
    public string FrontendBaseUrl { get; set; } = "http://localhost:3000";

    public int MaxRetries { get; set; } = 5;
    public int RetryBaseDelaySeconds { get; set; } = 60;
    public int PollIntervalSeconds { get; set; } = 20;

    // E.164 number customers reach the store on directly (the storefront's "Discuss on WhatsApp"
    // product-card link) - not used for sending anything, just displayed/linked to publicly, so
    // it's plain config rather than a secret.
    public string StoreContactNumber { get; set; } = "";
}
