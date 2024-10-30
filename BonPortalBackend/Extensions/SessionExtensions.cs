using Microsoft.AspNetCore.Http;

namespace BonPortalBackend.Extensions
{
    public static class SessionExtensions
    {
        public static void SetBool(this ISession session, string key, bool value)
        {
            session.Set(key, BitConverter.GetBytes(value));
        }

        public static bool? GetBool(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length == 0)
                return null;
            return BitConverter.ToBoolean(data, 0);
        }
    }
}