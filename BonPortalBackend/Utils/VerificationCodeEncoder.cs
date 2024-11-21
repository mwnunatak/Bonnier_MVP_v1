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

        public static bool ValidateChecksum(string code)
        {
            // Remove dashes and validate format
            code = code.Replace("-", "");
            if (code.Length != 12)
                return false;

            // Extract checksum (positions 1 and 4 in the original format)
            string expectedChecksum = $"{code[1]}{code[4]}";
            
            // Create string without checksum digits for calculation
            string codeWithoutChecksum = 
                code.Substring(0, 1) +
                code.Substring(2, 2) +
                code.Substring(5, 7);

            // Calculate actual checksum
            int sum = 0;
            foreach (char c in codeWithoutChecksum)
            {
                if (char.IsLetter(c))
                    sum += (char.ToUpper(c) - 'A' + 1);
                else if (char.IsDigit(c))
                    sum += (c - '0');
            }

            // Get last two digits of sum
            string actualChecksum = (sum % 100).ToString("D2");
            
            return expectedChecksum == actualChecksum;
        }

        public static string GenerateChecksum(string partialCode)
        {
            // Remove any existing dashes
            partialCode = partialCode.Replace("-", "");
            
            // Calculate sum
            int sum = 0;
            foreach (char c in partialCode)
            {
                if (char.IsLetter(c))
                    sum += (char.ToUpper(c) - 'A' + 1);
                else if (char.IsDigit(c))
                    sum += (c - '0');
            }
            
            return (sum % 100).ToString("D2");
        }

        public static bool ValidateFormat(string code)
        {
            // Remove dashes first
            code = code.Replace("-", "");
            
            // Check length
            if (code.Length != 12)
                return false;

            // Check if all characters are either letters or digits
            foreach (char c in code)
            {
                if (!char.IsLetterOrDigit(c))
                    return false;
                if (char.IsLetter(c) && !char.IsUpper(c))
                    return false;
            }

            return true;
        }
    }
}