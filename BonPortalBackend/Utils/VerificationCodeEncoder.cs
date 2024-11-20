using System;
using System.Text;

namespace BonPortalBackend.Utils
{
    public static class VerificationCodeEncoder
    {
        public static string EncodeVerificationCode(string code)
        {
            if (string.IsNullOrEmpty(code))
                return string.Empty;

            // Remove any existing dashes
            code = code.Replace("-", "");
            
            // Convert to bytes and encode to Base64
            byte[] codeBytes = Encoding.UTF8.GetBytes(code);
            string base64 = Convert.ToBase64String(codeBytes);
            
            // Make the Base64 string URL-safe
            return base64
                .Replace("+", "-")
                .Replace("/", "_")
                .Replace("=", "");
        }

        public static string DecodeVerificationCode(string encodedCode)
        {
            if (string.IsNullOrEmpty(encodedCode))
                return string.Empty;

            try
            {
                // Restore Base64 padding and characters
                string base64 = encodedCode
                    .Replace("-", "+")
                    .Replace("_", "/");

                // Add padding if needed
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                // Decode Base64
                byte[] codeBytes = Convert.FromBase64String(base64);
                string decodedCode = Encoding.UTF8.GetString(codeBytes);

                // Format the code with dashes (XXXX-XXXX-XXXX)
                if (decodedCode.Length == 12)
                {
                    return $"{decodedCode.Substring(0, 4)}-{decodedCode.Substring(4, 4)}-{decodedCode.Substring(8, 4)}";
                }

                return decodedCode;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}