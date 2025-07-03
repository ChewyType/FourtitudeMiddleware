using FourtitudeMiddleware.Services;
using NUnit.Framework;

namespace FourtitudeMiddleware.Tests
{
    [TestFixture]
    public class PartnerServiceTests
    {
        private PartnerService _partnerService;

        [SetUp]
        public void SetUp()
        {
            _partnerService = new PartnerService();
        }

        [Test]
        public void ValidatePartner_ValidCredentials_ReturnsTrue()
        {
            // Arrange
            var partnerRefNo = "FG-00001";
            var password = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

            // Act
            var result = _partnerService.ValidatePartner(partnerRefNo, encodedPassword);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidatePartner_InvalidPartnerKey_ReturnsFalse()
        {
            // Arrange
            var partnerRefNo = "INVALID";
            var password = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

            // Act
            var result = _partnerService.ValidatePartner(partnerRefNo, encodedPassword);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidatePartner_InvalidPassword_ReturnsFalse()
        {
            // Arrange
            var partnerRefNo = "FG-00001";
            var password = "WRONGPASSWORD";
            var encodedPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password));

            // Act
            var result = _partnerService.ValidatePartner(partnerRefNo, encodedPassword);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidatePartner_InvalidBase64_ReturnsFalse()
        {
            // Arrange
            var partnerRefNo = "FG-00001";
            var encodedPassword = "not_base64";

            // Act
            var result = _partnerService.ValidatePartner(partnerRefNo, encodedPassword);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidateSignature_ValidSignature_ReturnsTrue()
        {
            // Arrange
            var parameters = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var timestamp = "20240101";
            // Concatenate values in order: "1" + "2" + timestamp
            var concatenated = "12" + timestamp;
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(concatenated));
            var hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            var hexBytes = System.Text.Encoding.UTF8.GetBytes(hexHash);
            var signature = Convert.ToBase64String(hexBytes);

            // Act
            var result = _partnerService.ValidateSignature(parameters, timestamp, signature);

            // Assert
            Assert.IsTrue(result);
        }

        [Test]
        public void ValidateSignature_InvalidSignature_ReturnsFalse()
        {
            // Arrange
            var parameters = new Dictionary<string, string>
            {
                { "a", "1" },
                { "b", "2" }
            };
            var timestamp = "20240101";
            var signature = "invalid_signature";

            // Act
            var result = _partnerService.ValidateSignature(parameters, timestamp, signature);

            // Assert
            Assert.IsFalse(result);
        }

        [Test]
        public void ValidateSignature_EmptyParameters_ReturnsFalse()
        {
            // Arrange
            var parameters = new Dictionary<string, string>();
            var timestamp = "20240101";
            var signature = "";

            // Act
            var result = _partnerService.ValidateSignature(parameters, timestamp, signature);

            // Assert
            Assert.IsFalse(result);
        }
    }
} 