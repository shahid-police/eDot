namespace Elegance.Infrastructure.Notifications;

// Mirrors the approved "back_in_stock_notification" WhatsApp template body. Only used to build the
// manual wa.me chat link the admin panel offers against each subscription - for an admin to send
// this by hand while real Meta WhatsApp Cloud API credentials aren't configured yet. The automated
// send path (BackInStockNotificationWorker) never uses this: it sends {{1}}-{{4}} as template
// parameters straight to Meta, which renders the approved template on its own side.
public static class WhatsAppMessageTemplates
{
    public static string BuildBackInStockMessage(string customerName, string productName, string priceText, string productUrl) =>
        $"Hello {customerName}!\n\n" +
        "Good news! The product you were interested in is back in stock at E. (ELEGANCE).\n\n" +
        $"Product: {productName}\n" +
        $"Price: {priceText}\n\n" +
        "You can view and order it here:\n" +
        $"{productUrl}\n\n" +
        "Thank you for choosing E.\n\n" +
        "E. ELEGANCE";

    // wa.me's "click to chat" link - opens the admin's own WhatsApp (app or web) with the chat to
    // that number pre-filled with this text. The admin still presses send themselves.
    public static string BuildChatUrl(string e164Number, string message) =>
        $"https://wa.me/{e164Number.TrimStart('+')}?text={Uri.EscapeDataString(message)}";
}
