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
using System.Threading;
using System.Security.Authentication; // Giả sử class Email nằm trong namespace này hoặc được tham chiếu

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
            _accountService = accountService ?? throw new ArgumentException(nameof(accountService));
        }

        // ******** CẬP NHẬT: DÙNG CLASS EMAIL ********
        private MimeMessage CreateMimeMessage(Email mailModel)
        {
            var message = new MimeMessage();

            // Set sender
            // Sử dụng thuộc tính From (địa chỉ email) và FromUser (tên hiển thị)
            message.From.Add(new MailboxAddress(mailModel.AccountName, mailModel.From));

            // Set receipients: To là một mảng string[]
            // Cần kiểm tra null trước khi dùng .Select()
            if (mailModel.To != null && mailModel.To.Any())
            {
                // To là string[], cần map sang MailboxAddress
                message.To.AddRange(mailModel.To.Select(t => new MailboxAddress(t, t)));
            }

            // Set subject
            message.Subject = mailModel.Subject;

            // Set body contents (Plain Text)
            var bodyBuilder = new BodyBuilder();
            if (!String.IsNullOrEmpty(mailModel.BodyText))
            {
                // Dùng BodyText làm nội dung TextBody
                bodyBuilder.TextBody = mailModel.BodyText;
            }

            // Handle attachments (Nếu Email class có chứa thông tin Attachment)
            // Hiện tại không có thông tin Attachment trong class Email bạn cung cấp.

            // Set the complete body to MimeMessage
            message.Body = bodyBuilder.ToMessageBody();

            // Dùng DateSent (DateTime)
            // MailKit thường muốn DateTimeOffset, nhưng DateTime cũng hoạt động
            message.Date = new DateTimeOffset(mailModel.DateSent);

            return message;
        }

        // ******** CẬP NHẬT: DÙNG CLASS EMAIL ********
        public async Task SendEmailAsync(Email mailModel, CancellationToken cancellationToken = default)
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
                    client.SslProtocols = SslProtocols.Tls12;

                    // ******** SỬA LỖI: BỎ MẬT KHẨU ỨNG DỤNG VÀ DÙNG XOAUTH2 ********
                    // BỎ DÒNG NÀY: client.Authenticate("nhavotan2k6@gmail.com", "omhzionramrvglwu");

                    // Authenticate using XOAUTH2
                    var oauth2 = new MailKit.Security.SaslMechanismOAuth2(emailAddress, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);

                    // Send the message
                    await client.SendAsync(mimeMessage, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Lỗi 535 5.7.8 (BadCredentials) thường xảy ra ở đây
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

        // ******** CẬP NHẬT: DÙNG XOAUTH2 CHO IMAP ********
        public async Task<List<MimeMessage>> FetchMessageAsync(CancellationToken cancellationToken = default)
        {
            if (!_accountService.IsSignedIn())
            {
                throw new InvalidOperationException("User is not signed in. Cannot fetch email.");
            }

            List<MimeMessage> messages = new List<MimeMessage>();

            String emailAddress = _accountService.GetCurrentUserEmail();
            var accessToken = await _accountService.GetAccessTokenAsync(cancellationToken);

            using (var client = new ImapClient())
            {
                try
                {
                    // 1. Connect using implicit SSL
                    await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);
                    client.SslProtocols = SslProtocols.Tls12; // Tùy chọn: Thêm TLS12 cho IMAP

                    // 2. Authenticate using XOAUTH2
                    var oauth2 = new MailKit.Security.SaslMechanismOAuth2(emailAddress, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);

                    // 3. Open the inbox folder
                    var inbox = client.Inbox;
                    await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly, cancellationToken);

                    // 4. Fetch the messages
                    int count = inbox.Count;
                    int start = Math.Max(0, count - 10);

                    // Fetch full MimeMessages
                    for (int i = start; i < count; i++)
                    {
                        var mimeMessage = await inbox.GetMessageAsync(i, cancellationToken);
                        Console.WriteLine($"[IMAP] Fetched message {i + 1}/{count}: '{mimeMessage.Subject}");
                        messages.Add(mimeMessage);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to fetch emails via IMAP for account {emailAddress}: {ex.Message}");
                }
                finally
                {
                    if (client.IsConnected)
                    {
                        await client.DisconnectAsync(true, cancellationToken);
                    }
                }

                return messages;
            }
        }
    }
}