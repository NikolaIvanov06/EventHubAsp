using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using EventHubASP.Core;
using Moq;
using NUnit.Framework;

namespace EventHubASP.Tests.Tests
{
    [TestFixture]
    public class EmailServiceTests
    {
        private Mock<EmailService> _mockEmailService;
        private EmailService _emailService;
        private const string _smtpServer = "smtp.example.com";
        private const int _smtpPort = 587;
        private const string _senderEmail = "test@example.com";
        private const string _senderPassword = "password123";

        [SetUp]
        public void Setup()
        {
            _mockEmailService = new Mock<EmailService>(_smtpServer, _smtpPort, _senderEmail, _senderPassword)
            {
                CallBase = true
            };

            _emailService = new EmailService(_smtpServer, _smtpPort, _senderEmail, _senderPassword);

            _mockEmailService.Setup(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
        }

        [Test]
        public void Constructor_ShouldInitializeProperties()
        {
            var emailService = new EmailService("smtp.test.com", 465, "sender@test.com", "secret");

            var type = typeof(EmailService);

            var smtpServerField = type.GetField("_smtpServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var smtpPortField = type.GetField("_smtpPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var senderEmailField = type.GetField("_senderEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var senderPasswordField = type.GetField("_senderPassword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            Assert.That(smtpServerField.GetValue(emailService), Is.EqualTo("smtp.test.com"));
            Assert.That(smtpPortField.GetValue(emailService), Is.EqualTo(465));
            Assert.That(senderEmailField.GetValue(emailService), Is.EqualTo("sender@test.com"));
            Assert.That(senderPasswordField.GetValue(emailService), Is.EqualTo("secret"));
        }

        [Test]
        public async Task SendRecoveryCodeAsync_ShouldCallSendMailAsync()
        {
            string testEmail = "recipient@example.com";
            string testCode = "123456";

            await _mockEmailService.Object.SendRecoveryCodeAsync(testEmail, testCode);

            _mockEmailService.Verify(e => e.SendRecoveryCodeAsync(testEmail, testCode), Times.Once);
        }

        [Test]
        public void SendRecoveryCodeAsync_ShouldThrowException_WhenSmtpFails()
        {
            var exceptionMock = new Mock<EmailService>(_smtpServer, _smtpPort, _senderEmail, _senderPassword)
            {
                CallBase = true
            };

            exceptionMock.Setup(e => e.SendRecoveryCodeAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("SMTP error"));

            var exception = Assert.ThrowsAsync<Exception>(async () =>
                await exceptionMock.Object.SendRecoveryCodeAsync("test@example.com", "123456"));

            Assert.That(exception.Message, Contains.Substring("SMTP error"));
        }

        [Test]
        public void SendRecoveryCodeAsync_ShouldUseCorrectSmtpSettings()
        {

            var type = typeof(EmailService);
            var smtpServerField = type.GetField("_smtpServer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var smtpPortField = type.GetField("_smtpPort", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var senderEmailField = type.GetField("_senderEmail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var senderPasswordField = type.GetField("_senderPassword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var server = smtpServerField.GetValue(_emailService) as string;
            var port = (int)smtpPortField.GetValue(_emailService);
            var email = senderEmailField.GetValue(_emailService) as string;
            var password = senderPasswordField.GetValue(_emailService) as string;

            using var client = new SmtpClient(server, port)
            {
                EnableSsl = true,
                Credentials = new NetworkCredential(email, password)
            };

            Assert.That(client.Host, Is.EqualTo(_smtpServer));
            Assert.That(client.Port, Is.EqualTo(_smtpPort));
            Assert.That(client.EnableSsl, Is.True);

            var credentials = client.Credentials as NetworkCredential;
            Assert.That(credentials.UserName, Is.EqualTo(_senderEmail));
            Assert.That(credentials.Password, Is.EqualTo(_senderPassword));
        }

        [Test]
        public void SendRecoveryCodeAsync_ShouldCreateCorrectMailMessage()
        {

            string testEmail = "test@recipient.com";
            string testCode = "ABCDEF";

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_senderEmail, "EventHub Team"),
                Subject = "Your Password Recovery Code",
                Body = $"Your recovery code is: {testCode}\n\nKeep this code safe!",
                IsBodyHtml = false
            };
            mailMessage.To.Add(testEmail);

            Assert.That(mailMessage.From.Address, Is.EqualTo(_senderEmail));
            Assert.That(mailMessage.From.DisplayName, Is.EqualTo("EventHub Team"));
            Assert.That(mailMessage.Subject, Is.EqualTo("Your Password Recovery Code"));
            Assert.That(mailMessage.Body, Contains.Substring(testCode));
            Assert.That(mailMessage.IsBodyHtml, Is.False);
            Assert.That(mailMessage.To.Count, Is.EqualTo(1));
            Assert.That(mailMessage.To[0].Address, Is.EqualTo(testEmail));
        }
    }
}