namespace BonPortalBackend.Utils
{
    public static class SafetyLinkGenerator
    {
        public static string GenerateSafetyLink()
        {
            return $"{Guid.NewGuid()}-{DateTime.UtcNow.Ticks}";
        }

        public static string GetFullSafetyLink(HttpRequest request, string safetyToken)
        {
            var baseUrl = $"{request.Scheme}://{request.Host}";
            return $"{baseUrl}/ContactInformation/HandleSafetyRequest?token={safetyToken}";
        }
    }
}