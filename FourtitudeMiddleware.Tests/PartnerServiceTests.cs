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
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var totalAmount = 10000L;

            var parameters = new Dictionary<string, string>
            {
                { "partnerkey", partnerKey },
                { "partnerrefno", partnerRefNo },
                { "totalamount", totalAmount.ToString() }
            };

            // Use the service to generate the signature
            var signature = _partnerService.GenerateSignature(parameters);

            // Act
            var result = _partnerService.ValidateSignature(parameters, signature.Timestamp, signature.Signature);

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