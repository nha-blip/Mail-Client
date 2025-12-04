using Google.Apis.Auth.OAuth2;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO; 
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using MimeKit.Utils;

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
            message.Subject = mailModel.Subject ?? "(No Subject)";

            // Set body contents (Plain Text)
            var bodyBuilder = new BodyBuilder();

            // Lấy nội dung HTML thô
            string rawHtml = mailModel.BodyText ?? "";

            // Xử lý tách ảnh Base64 -> CID (Gọi hàm vừa viết)
            // Hàm này sẽ tự động thêm ảnh vào bodyBuilder.LinkedResources
            string processedHtml = ProcessInlineImages(rawHtml, bodyBuilder);

            // Gán HTML đã xử lý (lúc này src="cid:..." chứ không còn là base64 nữa)
            bodyBuilder.HtmlBody = processedHtml;

            if (mailModel.AttachmentPaths != null && mailModel.AttachmentPaths.Count > 0)
            {
                foreach (string filePath in mailModel.AttachmentPaths)
                {
                    // Kiểm tra file có thực sự tồn tại trên ổ cứng không
                    if (File.Exists(filePath))
                    {
                        try
                        {
                            // Hàm này sẽ đọc file và mã hóa nó vào email
                            bodyBuilder.Attachments.Add(filePath);
                        }
                        catch (Exception ex)
                        {
                            // Nếu file bị lỗi (ví dụ đang mở bởi app khác), bỏ qua hoặc log lỗi
                            Console.WriteLine($"Lỗi đính kèm file {filePath}: {ex.Message}");
                        }
                    }
                }
            }

            // Set the complete body to MimeMessage
            message.Body = bodyBuilder.ToMessageBody();

            // Dùng DateSent (DateTime)
            // MailKit thường muốn DateTimeOffset, nhưng DateTime cũng hoạt động
            message.Date = new DateTimeOffset(mailModel.DateSent);

            return message;
        }

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

                    // Authenticate using XOAUTH2
                    var oauth2 = new MailKit.Security.SaslMechanismOAuth2(emailAddress, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);

                    // Send the message
                    await client.SendAsync(mimeMessage, cancellationToken);
                }
                catch (Exception ex)
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

        // DÙNG XOAUTH2 CHO IMAP 
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

        private string ProcessInlineImages(string htmlContent, BodyBuilder bodyBuilder)
        {
            if (string.IsNullOrEmpty(htmlContent)) return string.Empty;

            // Regex để tìm thẻ <img src="data:image/...">
            var regex = new Regex(@"<img[^>]+src=[""']data:image/(?<type>[a-zA-Z]+);base64,(?<data>[^""']+)[""'][^>]*>", RegexOptions.IgnoreCase);

            // Thay thế từng ảnh tìm được
            var newHtml = regex.Replace(htmlContent, match =>
            {
                try
                {
                    string type = match.Groups["type"].Value; // ví dụ: png, jpeg
                    string base64Data = match.Groups["data"].Value;

                    // 1. Chuyển Base64 thành byte[]
                    byte[] imageBytes = Convert.FromBase64String(base64Data);

                    // 2. Tạo LinkedResource (Ảnh nhúng)
                    var imageStream = new MemoryStream(imageBytes);
                    string imageName = $"image_{Guid.NewGuid()}.{type}";

                    // Thêm vào danh sách LinkedResources của BodyBuilder
                    // Lưu ý: ContentType phải đúng (image/png, image/jpeg...)
                    var linkedResource = bodyBuilder.LinkedResources.Add(imageName, imageBytes, MimeKit.ContentType.Parse($"image/{type}"));

                    // 3. Tạo Content-ID (CID)
                    linkedResource.ContentId = MimeUtils.GenerateMessageId();

                    // 4. Trả về thẻ img mới với src="cid:..."
                    // Giữ lại các thuộc tính khác của thẻ img (nếu có) bằng cách thay thế mỗi src
                    string originalTag = match.Value;
                    string newSrc = $"cid:{linkedResource.ContentId}";

                    // Thay thế đoạn data:image... bằng cid:...
                    return originalTag.Replace(match.Groups[0].Value, originalTag.Replace(match.Groups["data"].Value, "").Replace($"data:image/{type};base64,", newSrc));
                }
                catch (Exception)
                {
                    // Nếu lỗi convert ảnh thì giữ nguyên (hoặc bỏ qua)
                    return match.Value;
                }
            });

            // Cách replace trên hơi phức tạp, để đơn giản và an toàn nhất, ta dùng cách thay thế chuỗi src trực tiếp:
            // Chạy lại vòng lặp để thay thế chính xác đường dẫn src
            var matches = regex.Matches(htmlContent);
            string finalHtml = htmlContent;

            foreach (Match m in matches)
            {
                string type = m.Groups["type"].Value;
                string base64Data = m.Groups["data"].Value;
                byte[] imageBytes = Convert.FromBase64String(base64Data);

                var linkedResource = bodyBuilder.LinkedResources.Add($"image.{type}", imageBytes, MimeKit.ContentType.Parse($"image/{type}"));
                linkedResource.ContentId = MimeUtils.GenerateMessageId();

                linkedResource.ContentDisposition = new MimeKit.ContentDisposition(MimeKit.ContentDisposition.Inline);

                // Tìm chuỗi "data:image/..." cũ và thay bằng "cid:..."
                string oldSrc = $"data:image/{type};base64,{base64Data}";
                string newSrc = $"cid:{linkedResource.ContentId}";

                finalHtml = finalHtml.Replace(oldSrc, newSrc);
            }

            return finalHtml;
        }
    }
}