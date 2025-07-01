using System.Security.Cryptography;
using System.Text;

namespace FourtitudeMiddleware.Services
{
    public class PartnerService : IPartnerService
    {
        private readonly Dictionary<string, string> _allowedPartners = new()
        {
            { "FG-00001", "FAKEPASSWORD1234" },
            { "FG-00002", "FAKEPASSWORD4578" }
        };

        public bool ValidatePartner(string partnerKey, string encodedPassword)
        {
            if (!_allowedPartners.TryGetValue(partnerKey, out var storedPassword))
                return false;

            try
            {
                byte[] decodedBytes = Convert.FromBase64String(encodedPassword);
                string decodedPassword = Encoding.UTF8.GetString(decodedBytes);
                return decodedPassword == storedPassword;
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateSignature(Dictionary<string, string> parameters, string timestamp, string signature)
        {
            try
            {
                // Sort parameters alphabetically by name
                var orderedParams = parameters
                    .OrderBy(p => p.Key)
                    .ToDictionary(p => p.Key, p => p.Value);

                // Concatenate all parameter values in specified order
                var concatenated = string.Join("", orderedParams.Values) + timestamp;

                // Compute SHA-256 hash
                using var sha256 = SHA256.Create();
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatenated));

                // Convert to lowercase hexadecimal
                var hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                // Convert hex hash to Base64
                byte[] hexBytes = Encoding.UTF8.GetBytes(hexHash);
                string base64Result = Convert.ToBase64String(hexBytes);

                return base64Result == signature;
            }
            catch
            {
                return false;
            }
        }
    }
}
