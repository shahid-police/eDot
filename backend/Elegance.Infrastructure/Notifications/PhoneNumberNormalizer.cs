using PhoneNumbers;

namespace Elegance.Infrastructure.Notifications;

// Validates and normalizes a customer-entered phone number into E.164 using Google's
// libphonenumber (the standard library for this - hand-rolled regex validation of international
// numbers is unreliable). Defaults to Pakistan when the customer didn't include a country code,
// per the spec's "+92 default" requirement, but still accepts any other country's number when one
// is given (e.g. "+1 555..." or "0044 20...").
public static class PhoneNumberNormalizer
{
    private const string DefaultRegion = "PK";

    public static bool TryNormalize(string raw, out string e164)
    {
        e164 = string.Empty;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var util = PhoneNumberUtil.GetInstance();
        try
        {
            var parsed = util.Parse(raw.Trim(), DefaultRegion);
            if (!util.IsValidNumber(parsed))
            {
                return false;
            }

            e164 = util.Format(parsed, PhoneNumberFormat.E164);
            return true;
        }
        catch (NumberParseException)
        {
            return false;
        }
    }
}
