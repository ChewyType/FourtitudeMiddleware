using System;
using System.Collections.Generic;
using System.Linq;
using FluentValidation;
using Moq;
using NUnit.Framework;
using FourtitudeMiddleware.Dtos;
using FourtitudeMiddleware.Services;

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

        [Test]
        public void ProcessTransaction_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            var request = new SubmitTransactionRequest
            {
                PartnerKey = "FG-00001",
                PartnerRefNo = "FG-00001",
                PartnerPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("FAKEPASSWORD1234")),
                TotalAmount = 1000,
                Timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Sig = "validsig"
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
                Assert.That(result.TotalAmount, Is.EqualTo(1000));
                Assert.That(result.FinalAmount, Is.LessThan(1000));
            });
        }

        [Test]
        public void ProcessTransaction_InvalidPartnerCredentials_ReturnsAccessDenied()
        {
            var request = new SubmitTransactionRequest
            {
                PartnerKey = "FG-00001",
                PartnerRefNo = "FG-00001",
                PartnerPassword = "invalid",
                TotalAmount = 1000,
                Timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Sig = "validsig"
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
            var request = new SubmitTransactionRequest
            {
                PartnerKey = "FG-00001",
                PartnerRefNo = "FG-00001",
                PartnerPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("FAKEPASSWORD1234")),
                TotalAmount = 1000,
                Timestamp = "not-a-date",
                Sig = "validsig"
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
            var request = new SubmitTransactionRequest
            {
                PartnerKey = "FG-00001",
                PartnerRefNo = "FG-00001",
                PartnerPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("FAKEPASSWORD1234")),
                TotalAmount = 1000,
                Timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Sig = "invalidsig"
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
            var request = new SubmitTransactionRequest
            {
                PartnerKey = "FG-00001",
                PartnerRefNo = "FG-00001",
                PartnerPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("FAKEPASSWORD1234")),
                TotalAmount = 1000,
                Timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Sig = "validsig"
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

        [TestCase(100, 0, 100)]
        [TestCase(300, 15, 285)] // 5% discount
        [TestCase(600, 42, 558)] // 7% discount
        [TestCase(1000, 100, 900)] // 10% discount
        [TestCase(1300, 195, 1105)] // 15% discount
        [TestCase(997, 179, 818)] // 10% base + 8% prime = 18%
        [TestCase(1005, 201, 804)] // 10% base + 10% ends with 5
        public void ProcessTransaction_DiscountCalculation_Works(long totalAmount, long expectedDiscount, long expectedFinal)
        {
            // Arrange
            var request = new SubmitTransactionRequest
            {
                PartnerKey = "FG-00001",
                PartnerRefNo = "FG-00001",
                PartnerPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("FAKEPASSWORD1234")),
                TotalAmount = totalAmount,
                Timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss"),
                Sig = "validsig"
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