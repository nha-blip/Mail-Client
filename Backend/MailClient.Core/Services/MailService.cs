using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Google.Apis.Auth.OAuth2;
using MailClient.Core.Models;
using System.Threading;

namespace MailClient.Core.Services
{
    public class MailService
    {
        private const String ImapHost = "imap.gmail.com";
        private const int ImapPort = 993;
        private const String SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 465;   // Use 465 for implicit SSL

        private readonly AccountService _accountService;

        public MailService(AccountService accountService)
        {
            _accountService = accountService ?? throw new ArgumentException(nameof(accountService)); // check null
        }

        private MimeMessage CreateMimeMessage(MailModel mailModel)
        {
            var message = new MimeMessage();

            // Set sender
            // NOTE: Can use user's name instead of email address for both args
            message.From.Add(new MailboxAddress(mailModel.From, mailModel.From));

            // Set receipients
            message.To.AddRange(mailModel.To.Select(t => new MailboxAddress(t, t)));
            if (mailModel.Cc.Any())
            {
                message.Cc.AddRange(mailModel.Cc.Select(c => new MailboxAddress(c, c)));
            }
            if (mailModel.Bcc.Any())
            {
                message.Bcc.AddRange(mailModel.Bcc.Select(b => new MailboxAddress(b, b)));
            }

            // Set subject
            message.Subject = mailModel.Subject;

            // Set body contents (handles both HTML and Plain Text)
            var bodyBuilder = new BodyBuilder();
            if (!String.IsNullOrEmpty(mailModel.HtmlBody))
            {
                bodyBuilder.HtmlBody = mailModel.HtmlBody;
            }
            if (!String.IsNullOrEmpty(mailModel.TextBody))
            {
                bodyBuilder.TextBody = mailModel.TextBody;
            }
            else if (!String.IsNullOrEmpty(mailModel.HtmlBody))
            {
                bodyBuilder.TextBody = mailModel.Subject;
            }

            // Handle attachments
            foreach(var path in mailModel.Attachments)
            {
                if (System.IO.File.Exists(path))
                {
                    bodyBuilder.Attachments.Add(path);
                }
            }

            // Set the complete body to MimeMessage
            message.Body = bodyBuilder.ToMessageBody();
            message.Date = mailModel.Date;

            return message;
        }

        public async Task SendEmailAsync(MailModel mailModel, CancellationToken cancellationToken = default)
        {
            // Check sign-in status and retreive required OAuth data
            if (!_accountService.IsSignedIn())
            {
                throw new InvalidOperationException("User is not signed in. Cannot send email using OAuth2");
            }

            // This ensures the token is refreshed if it's expired
            var accessToken = await _accountService.GetAccessTokenAsync(cancellationToken);
            var emailAddress = _accountService.GetCurrentUserEmail();

            var mimeMessage = CreateMimeMessage(mailModel);

            using (var client = new SmtpClient())
            {
                try
                {
                    // Connect using implicit SSL
                    await client.ConnectAsync(SmtpHost, SmtpPort, SecureSocketOptions.SslOnConnect, cancellationToken);

                    // Authenticate using XOAUTH2
                    var oauth2 = new MailKit.Security.SaslMechanismOAuth2(emailAddress, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);

                    // Send the message
                    await client.SendAsync(mimeMessage, cancellationToken);
                }
                catch  (Exception ex)
                {
                    throw new Exception($"Failed to send email using XOAUTH2 for account {emailAddress}: {ex.Message}", ex);
                }
                finally
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true, cancellationToken);
                    }
                }
            }
        }
    }
}
