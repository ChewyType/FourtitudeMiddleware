using System.Security.Cryptography;
using System.Text;
using FourtitudeMiddleware.Commons;
using FourtitudeMiddleware.Dtos;

namespace FourtitudeMiddleware.Services
{
    public class PartnerService : IPartnerService
    {
        private record PartnerInfo(string Name, string Password);
        private readonly Dictionary<string, PartnerInfo> _allowedPartners = new()
        {
            { "FG-00001", new PartnerInfo("FAKEGOOGLE", "FAKEPASSWORD1234") },
            { "FG-00002", new PartnerInfo("FAKEPEOPLE", "FAKEPASSWORD4578") }
        };

        public bool ValidatePartner(string partnerRefNo, string encodedPassword)
        {
            if (!_allowedPartners.TryGetValue(partnerRefNo, out var partnerInfo))
                return false;

            try
            {
                byte[] decodedBytes = Convert.FromBase64String(encodedPassword);
                string decodedPassword = Encoding.UTF8.GetString(decodedBytes);
                return decodedPassword == partnerInfo.Password;
            }
            catch
            {
                return false;
            }
        }

        public bool ValidateSignature(Dictionary<string, string> parameters, string timestamp, string signature)
        {
            // Required keys
            parameters.TryGetValue(DictionaryKeys.SignaturePartnerKey, out var partnerKey);
            parameters.TryGetValue(DictionaryKeys.SignaturePartnerRefNo, out var partnerRefNo);
            parameters.TryGetValue(DictionaryKeys.SignatureTotalAmount, out var totalAmount);

            if (string.IsNullOrEmpty(partnerKey) || !_allowedPartners.TryGetValue(partnerRefNo, out var partnerInfo))
                return false;

            // Use the encoded password (as stored)
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerInfo.Password));

            // Concatenate in the required order
            var concatenated = string.Concat(timestamp, partnerKey, partnerRefNo, totalAmount, encodedPassword);

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

        public GenerateSignatureResponse GenerateSignature(Dictionary<string, string> parameters, string timestamp = null)
        {
            if (string.IsNullOrEmpty(timestamp))
                timestamp = DateTime.UtcNow.ToString("o");

            // Required keys
            parameters.TryGetValue(DictionaryKeys.SignaturePartnerKey, out var partnerKey);
            parameters.TryGetValue(DictionaryKeys.SignaturePartnerRefNo, out var partnerRefNo);
            parameters.TryGetValue(DictionaryKeys.SignatureTotalAmount, out var totalAmount);

            if (string.IsNullOrEmpty(partnerKey)
                || !_allowedPartners.TryGetValue(partnerRefNo, out var partnerInfo))
            {
                return new GenerateSignatureResponse
                {
                    Signature = null,
                    Timestamp = timestamp
                };
            }

            // Use the encoded password (as stored)
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerInfo.Password));

            // Concatenate in the required order
            var concatenated = string.Concat(timestamp, partnerKey, partnerRefNo, totalAmount, encodedPassword);

            // Compute SHA-256 hash
            using var sha256 = SHA256.Create();
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatenated));

            // Convert to lowercase hexadecimal
            var hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

            // Convert hex hash to Base64
            byte[] hexBytes = Encoding.UTF8.GetBytes(hexHash);
            string base64Result = Convert.ToBase64String(hexBytes);

            return new GenerateSignatureResponse
            {
                Signature = base64Result,
                Timestamp = timestamp
            };
        }
    }
}
