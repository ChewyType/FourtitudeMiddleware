using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Moq;
using NUnit.Framework;
using FourtitudeMiddleware.Dtos;
using FourtitudeMiddleware.Services;
using System.Text;

namespace FourtitudeMiddleware.Tests
{
    [TestFixture]
    public class TransactionServiceTests
    {
        private Mock<IPartnerService> _partnerServiceMock;
        private Mock<IValidator<SubmitTransactionRequest>> _validatorMock;
        private TransactionService _transactionService;

        [SetUp]
        public void SetUp()
        {
            _partnerServiceMock = new Mock<IPartnerService>();
            _validatorMock = new Mock<IValidator<SubmitTransactionRequest>>();
            _transactionService = new TransactionService(_partnerServiceMock.Object, _validatorMock.Object);
        }

        private string GenerateValidSignature(string partnerKey, string partnerRefNo, long totalAmount, string partnerPassword, string timestamp)
        {
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerPassword));
            var concatenated = string.Concat(timestamp, partnerKey, partnerRefNo, totalAmount.ToString(), encodedPassword);
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(concatenated));
            var hexHash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            var hexBytes = Encoding.UTF8.GetBytes(hexHash);
            return Convert.ToBase64String(hexBytes);
        }

        [Test]
        public void ProcessTransaction_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var partnerPassword = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerPassword));
            var totalAmount = 10000L;
            var timestamp = DateTime.UtcNow.ToString("o");
            var sig = GenerateValidSignature(partnerKey, partnerRefNo, totalAmount, partnerPassword, timestamp);
            var request = new SubmitTransactionRequest
            {
                PartnerKey = partnerKey,
                PartnerRefNo = partnerRefNo,
                PartnerPassword = encodedPassword,
                TotalAmount = totalAmount,
                Timestamp = timestamp,
                Sig = sig
            };
            _validatorMock.Setup(v => v.Validate(request)).Returns(new FluentValidation.Results.ValidationResult());
            _partnerServiceMock.Setup(p => p.ValidatePartner(request.PartnerRefNo, request.PartnerPassword)).Returns(true);
            _partnerServiceMock.Setup(p => p.ValidateSignature(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), request.Sig)).Returns(true);

            // Act
            var result = _transactionService.ProcessTransaction(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.EqualTo(1));
                Assert.That(result.TotalAmount, Is.EqualTo(10000));
                Assert.That(result.FinalAmount, Is.EqualTo(10000));
            });
        }

        [Test]
        public void ProcessTransaction_InvalidPartnerCredentials_ReturnsAccessDenied()
        {
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var partnerPassword = "FAKEPASSWORD1234";
            var encodedPassword = "invalid";
            var totalAmount = 10000L;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var sig = GenerateValidSignature(partnerKey, partnerRefNo, totalAmount, partnerPassword, timestamp);
            var request = new SubmitTransactionRequest
            {
                PartnerKey = partnerKey,
                PartnerRefNo = partnerRefNo,
                PartnerPassword = encodedPassword,
                TotalAmount = totalAmount,
                Timestamp = timestamp,
                Sig = sig
            };
            _validatorMock.Setup(v => v.Validate(request)).Returns(new FluentValidation.Results.ValidationResult());
            _partnerServiceMock.Setup(p => p.ValidatePartner(request.PartnerRefNo, request.PartnerPassword)).Returns(false);

            var result = _transactionService.ProcessTransaction(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.EqualTo(0));
                Assert.That(result.ResultMessage, Is.EqualTo("Access Denied!"));
            });
        }

        [Test]
        public void ProcessTransaction_InvalidTimestamp_ReturnsInvalidTimestamp()
        {
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var partnerPassword = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerPassword));
            var totalAmount = 10000L;
            var timestamp = "not-a-date";
            var sig = "invalidsig";
            var request = new SubmitTransactionRequest
            {
                PartnerKey = partnerKey,
                PartnerRefNo = partnerRefNo,
                PartnerPassword = encodedPassword,
                TotalAmount = totalAmount,
                Timestamp = timestamp,
                Sig = sig
            };
            _validatorMock.Setup(v => v.Validate(request)).Returns(new FluentValidation.Results.ValidationResult());
            _partnerServiceMock.Setup(p => p.ValidatePartner(request.PartnerRefNo, request.PartnerPassword)).Returns(true);

            var result = _transactionService.ProcessTransaction(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.EqualTo(0));
                Assert.That(result.ResultMessage, Is.EqualTo("Invalid timestamp format"));
            });
        }

        [Test]
        public void ProcessTransaction_InvalidSignature_ReturnsInvalidSignature()
        {
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var partnerPassword = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerPassword));
            var totalAmount = 10000L;
            var timestamp = DateTime.UtcNow.ToString("o");
            var sig = "invalidsig";
            var request = new SubmitTransactionRequest
            {
                PartnerKey = partnerKey,
                PartnerRefNo = partnerRefNo,
                PartnerPassword = encodedPassword,
                TotalAmount = totalAmount,
                Timestamp = timestamp,
                Sig = sig
            };
            _validatorMock.Setup(v => v.Validate(request)).Returns(new FluentValidation.Results.ValidationResult());
            _partnerServiceMock.Setup(p => p.ValidatePartner(request.PartnerRefNo, request.PartnerPassword)).Returns(true);
            _partnerServiceMock.Setup(p => p.ValidateSignature(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), request.Sig)).Returns(false);

            var result = _transactionService.ProcessTransaction(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.EqualTo(0));
                Assert.That(result.ResultMessage, Is.EqualTo("Invalid signature"));
            });
        }

        [Test]
        public void ProcessTransaction_ValidationFails_ReturnsValidationError()
        {
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var partnerPassword = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerPassword));
            var totalAmount = 10000L;
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var sig = GenerateValidSignature(partnerKey, partnerRefNo, totalAmount, partnerPassword, timestamp);
            var request = new SubmitTransactionRequest
            {
                PartnerKey = partnerKey,
                PartnerRefNo = partnerRefNo,
                PartnerPassword = encodedPassword,
                TotalAmount = totalAmount,
                Timestamp = timestamp,
                Sig = sig
            };
            var validationResult = new FluentValidation.Results.ValidationResult(new[]
            {
                new FluentValidation.Results.ValidationFailure("PartnerKey", "PartnerKey is required")
            });
            _validatorMock.Setup(v => v.Validate(request)).Returns(validationResult);

            var result = _transactionService.ProcessTransaction(request);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.EqualTo(0));
                Assert.That(result.ResultMessage, Does.Contain("PartnerKey is required"));
            });
        }

        [TestCase(10000, 0, 10000)] // RM100, no discount
        [TestCase(30000, 1500, 28500)] // RM300, 5% discount
        [TestCase(60000, 4200, 55800)] // RM600, 7% discount
        [TestCase(100000, 10000, 90000)] // RM1000, 10% discount
        [TestCase(130000, 19500, 110500)] // RM1300, 15% discount
        [TestCase(99700, 17946, 81754)] // RM997, 10% base + 8% prime = 18%
        [TestCase(100500, 20100, 80400)] // RM1005, 10% base + 10% ends with 5 = 20%
        public void ProcessTransaction_DiscountCalculation_Works(long totalAmount, long expectedDiscount, long expectedFinal)
        {
            // Arrange
            var partnerKey = "FG-00001";
            var partnerRefNo = "FG-00001";
            var partnerPassword = "FAKEPASSWORD1234";
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(partnerPassword));
            var timestamp = DateTime.UtcNow.ToString("o");
            var sig = GenerateValidSignature(partnerKey, partnerRefNo, totalAmount, partnerPassword, timestamp);
            var request = new SubmitTransactionRequest
            {
                PartnerKey = partnerKey,
                PartnerRefNo = partnerRefNo,
                PartnerPassword = encodedPassword,
                TotalAmount = totalAmount,
                Timestamp = timestamp,
                Sig = sig
            };

            _validatorMock.Setup(v => v.Validate(request)).Returns(new FluentValidation.Results.ValidationResult());
            _partnerServiceMock.Setup(p => p.ValidatePartner(request.PartnerRefNo, request.PartnerPassword)).Returns(true);
            _partnerServiceMock.Setup(p => p.ValidateSignature(It.IsAny<Dictionary<string, string>>(), It.IsAny<string>(), request.Sig)).Returns(true);

            // Act
            var result = _transactionService.ProcessTransaction(request);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.Result, Is.EqualTo(1));
                Assert.That(result.TotalDiscount, Is.EqualTo(expectedDiscount));
                Assert.That(result.FinalAmount, Is.EqualTo(expectedFinal));
            });
        }
    }
} 