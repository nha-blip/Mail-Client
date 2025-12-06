using Google.Apis.Auth.OAuth2;
using Mailclient;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Data.SqlClient;
using MimeKit;
using MimeKit.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MailClient.Core.Services
{
    public class MailService
    {
        private const String ImapHost = "imap.gmail.com";
        private const int ImapPort = 993;
        private const String SmtpHost = "smtp.gmail.com";
        private const int SmtpPort = 465;   // Use 465 for implicit SSL

        private readonly AccountService _accountService;
        private int fetched = 0;

        public MailService(AccountService accountService)
        {
            _accountService = accountService; // check null
        }

        private MimeMessage CreateMimeMessage(Email mailModel)
        {
            var message = new MimeMessage();

            // Set sender
            message.From.Add(new MailboxAddress(mailModel.From, mailModel.From));

            // Set receipient
            foreach (string receipient in mailModel.To)
            {
                message.To.Add(InternetAddress.Parse(receipient));
            }

            // Set subject
            message.Subject = mailModel.Subject ?? "(No Subject)";

            // Set body contents (handles both HTML and Plain Text)
            var bodyBuilder = new BodyBuilder();

            // Lấy nội dung HTML thô
            string rawHtml = mailModel.BodyText ?? "";

            // Xử lý tách ảnh Base64 -> CID (Gọi hàm vừa viết)
            // Hàm này sẽ tự động thêm ảnh vào bodyBuilder.LinkedResources
            string processedHtml = ProcessInlineImages(rawHtml, bodyBuilder);

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
            message.Date = mailModel.DateSent;

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
            var emailAddress = _accountService._userEmail;

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

        public async Task<List<MimeMessage>> FetchMessageAsync(CancellationToken cancellationToken = default)
        {
            if (!_accountService.IsSignedIn())
            {
                throw new InvalidOperationException("User is not signed in. Cannot fetch email.");
            }

            List<MimeMessage> messages = new List<MimeMessage>();

            String emailAddress = _accountService._userEmail;
            var accessToken = await _accountService.GetAccessTokenAsync(cancellationToken);

            using (var client = new ImapClient())
            {
                try
                {
                    // 1. Connect using implicit SSL
                    await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect, cancellationToken);

                    // 2. Authenticate using XOAUTH2
                    var oauth2 = new MailKit.Security.SaslMechanismOAuth2(emailAddress, accessToken);
                    await client.AuthenticateAsync(oauth2, cancellationToken);

                    // 3. Open the inbox folder
                    var inbox = client.Inbox;
                    await inbox.OpenAsync(MailKit.FolderAccess.ReadOnly, cancellationToken);

                    // 4. Fetch the messages
                    int count = inbox.Count - fetched;
                    int start = Math.Max(0, count - 10);

                    // Fetch full MimeMessages
                    for (int i = start; i < count; i++)
                    {
                        // Fetch the MimeMessage content
                        var mimeMessage = await inbox.GetMessageAsync(i, cancellationToken);

                        System.Console.WriteLine($"[IMAP] Fetched message {i + 1}/{count}: '{mimeMessage.Subject}");

                        // Add the raw MailKit object to the list
                        messages.Add(mimeMessage);
                        fetched++;
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
        public int GetFolderIDByFolderName(string folderName, int accountID)
        {
            string query = @"Select FolderID From Folder Where FolderName=@folderName And AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@folderName", folderName),
                new SqlParameter("@AccountID", accountID)
            };

            DatabaseHelper db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);
            return 0;
        }
        public static string ConvertToSentenceCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            string lowerCase = input.ToLower();
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return lowerCase.Substring(0, 1).ToUpper() + lowerCase.Substring(1);
        }
        public async Task SyncAllFoldersToDatabase(int localAccountID)
        {
            // Lưu ý: Folder trên Gmail thường viết hoa (INBOX, SENT, TRASH...)
            string[] foldersToSync = { "INBOX", "SENT", "DRAFT", "TRASH", "SPAM" };

            foreach (var folderName in foldersToSync)
            {
                // Gọi hàm sync từng folder
                // Lưu ý: Cần đảm bảo tên folder này khớp với tên Label trên Gmail
                await SyncEmailsToDatabase(localAccountID, folderName);
            }
        }

        public async Task SyncEmailsToDatabase(int localAccountID, string foldername)
        {
            if (!_accountService.IsSignedIn()) return;
            List<MimeMessage> message = await FetchMessageAsync();
            if (message != null)
            {
                var parser = new EmailParser();
                // Tạo thư mục cache
                string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }
                foreach (var msgItem in message)
                {
                    try
                    {
                        // Parse email (Dùng parser của bạn)
                        Email emailToSave = await parser.ParseAsync(msgItem);

                        // Điền thông tin bổ sung
                        emailToSave.AccountID = localAccountID;
                        emailToSave.FolderName = ConvertToSentenceCase(foldername);
                        emailToSave.FolderID = GetFolderIDByFolderName(emailToSave.FolderName, localAccountID);
                        emailToSave.AccountName = _accountService._userName;

                        // Lưu Email vào DB
                        emailToSave.AddEmail();

                        // LƯU ATTACHMENT
                        if (emailToSave.emailID > 0 && emailToSave.TempAttachments != null && emailToSave.TempAttachments.Count > 0)
                        {
                            foreach (var attach in emailToSave.TempAttachments)
                            {
                                // A. Lưu thông tin vào DB
                                attach.EmailID = emailToSave.emailID;
                                attach.AddAttachment();

                                // B. Lưu file vật lý
                                // Nên thêm ID vào tên file để tránh trùng lặp: {ID}_{Name}
                                string saveFileName = $"{attach.Name}";
                                string fullPath = Path.Combine(cacheFolder, saveFileName);

                                if (attach.OriginalMimePart != null)
                                {
                                    using (var fileStream = File.Create(fullPath))
                                    {
                                        await attach.OriginalMimePart.Content.DecodeToAsync(fileStream);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"Lỗi sync email: {innerEx.Message}");
                    }
                }
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