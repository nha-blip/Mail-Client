using Google.Apis.Auth.OAuth2;
using Mailclient;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
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
            message.From.Add(new MailboxAddress(mailModel.AccountName, mailModel.From));

            // Set receipient
            foreach (string receipient in mailModel.To)
            {
                message.To.Add(InternetAddress.Parse(receipient));
            }

            // Set subject
            message.Subject = mailModel.Subject ?? "(No Subject)";

            // Header: In-Reply-To
            if (!string.IsNullOrEmpty(mailModel.InReplyTo))
            {
                // Loại bỏ ký tự < > nếu có, vì MimeKit tự thêm
                string cleanId = mailModel.InReplyTo.Trim('<', '>', ' ');
                message.InReplyTo = cleanId;
            }

            // Header: References
            if (!string.IsNullOrEmpty(mailModel.References))
            {
                // References thường là chuỗi các ID cách nhau bởi dấu cách
                var refs = mailModel.References.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var refId in refs)
                {
                    string cleanRef = refId.Trim('<', '>', ' ');
                    message.References.Add(cleanRef);
                }
            }

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

        /// <summary>
        /// Đồng bộ TẤT CẢ các thư mục từ Gmail về Database
        /// </summary>
        public async Task SyncAllFoldersToDatabase(int localAccountID, CancellationToken token = default)
        {
            if (!_accountService.IsSignedIn()) return;

            var accessToken = await _accountService.GetAccessTokenAsync(token);

            // Tạo kết nối MỘT LẦN dùng chung cho tất cả folder
            using (var client = new ImapClient())
            {
                try
                {
                    await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect, token);
                    await client.AuthenticateAsync(new SaslMechanismOAuth2(_accountService._userEmail, accessToken), token);

                    // Lấy danh sách tất cả folder trên server
                    var personalNamespace = client.PersonalNamespaces[0];
                    var allFolders = await client.GetFoldersAsync(personalNamespace, StatusItems.None, true, token);

                    foreach (var folder in allFolders)
                    {
                        // Bỏ qua folder gốc [Gmail] hoặc các folder không chọn được
                        if ((folder.Attributes & FolderAttributes.NoSelect) != 0) continue;
                        if (folder.Name == "[Gmail]") continue;

                        // Gọi hàm xử lý nội bộ, tái sử dụng client
                        await SyncFolderInternal(client, folder, localAccountID, token);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Error] SyncAllFolders: {ex.Message}");
                }
                finally
                {
                    if (client.IsConnected) await client.DisconnectAsync(true, token);
                }
            }
        }

        public async Task LoadOlderEmails(int localAccountID, string folderName, int amountToLoad = 20)
        {
            if (!_accountService.IsSignedIn()) return;

            var accessToken = await _accountService.GetAccessTokenAsync();

            using (var client = new ImapClient())
            {
                try
                {
                    await client.ConnectAsync(ImapHost, ImapPort, SecureSocketOptions.SslOnConnect);
                    await client.AuthenticateAsync(new SaslMechanismOAuth2(_accountService._userEmail, accessToken));

                    IMailFolder folder = null;
                    var personalNamespace = client.PersonalNamespaces[0];

                    // Lấy danh sách TẤT CẢ folder (recursive = true) 
                    var allFolders = await client.GetFoldersAsync(personalNamespace, StatusItems.None, true);

                    // Duyệt và tìm folder khớp tên 
                    // folderName ở đây là "Sent", "Inbox", v.v. từ giao diện gửi xuống
                    foreach (var f in allFolders)
                    {
                        // NormalizeFolderName sẽ biến "[Gmail]/Sent Mail" thành "Sent" để so sánh
                        if (NormalizeFolderName(f).Equals(folderName, StringComparison.OrdinalIgnoreCase) ||
                            f.Name.Equals(folderName, StringComparison.OrdinalIgnoreCase))
                        {
                            folder = f;
                            break;
                        }
                    }

                    // Nếu vẫn không thấy (ví dụ folder Inbox đôi khi nằm ngoài logic trên)
                    if (folder == null && folderName.Equals("Inbox", StringComparison.OrdinalIgnoreCase))
                    {
                        folder = client.Inbox;
                    }

                    if (folder == null) return;

                    await folder.OpenAsync(FolderAccess.ReadOnly);

                    // 2. Lấy ID Folder trong DB và Min UID hiện tại
                    int dbFolderId = GetFolderIDByFolderName(ConvertToSentenceCase(folderName), localAccountID);
                    if (dbFolderId == 0) return; // Folder chưa tồn tại trong DB thì không load cũ được

                    UniqueId oldestKnownUid = GetOldestSyncedUid(dbFolderId, localAccountID);

                    IList<UniqueId> uidsToFetch;

                    if (oldestKnownUid == UniqueId.MaxValue)
                    {
                        // DB chưa có gì -> Gọi lại logic sync thường để lấy mới nhất
                        return;
                    }
                    else
                    {
                        // 3. Logic lấy thư CŨ HƠN
                        // Lấy tất cả UID trên server
                        var allUidsOnServer = await folder.SearchAsync(SearchQuery.All);

                        // Lọc ra các UID NHỎ HƠN oldestKnownUid
                        // OrderByDescending để lấy những thư "cũ nhưng gần nhất" (liền kề với thư đang có)
                        uidsToFetch = allUidsOnServer
                                        .Where(uid => uid.Id < oldestKnownUid.Id)
                                        .OrderByDescending(uid => uid.Id)
                                        .Take(amountToLoad) // Chỉ lấy số lượng quy định (VD: 20)
                                        .ToList();
                    }

                    if (uidsToFetch.Count == 0) return; // Không còn thư cũ hơn

                    // 4. Đoạn này Copy y nguyên logic lưu DB từ SyncFolderInternal xuống
                    // (Để code gọn hơn, bạn nên tách đoạn lưu DB ra thành hàm riêng: SaveEmailsToDb)
                    var parser = new EmailParser();
                    string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");

                    foreach (var uid in uidsToFetch)
                    {
                        try
                        {
                            long threadId = 0;
                            var items = await folder.FetchAsync(new[] { uid }, MessageSummaryItems.GMailThreadId | MessageSummaryItems.UniqueId);
                            var summary = items.FirstOrDefault();

                            if (summary != null && summary.GMailThreadId.HasValue)
                            {
                                threadId = (long)summary.GMailThreadId.Value;
                            }

                            var mimeMessage = await folder.GetMessageAsync(uid);
                            Email emailToSave = await parser.ParseAsync(mimeMessage);

                            emailToSave.AccountID = localAccountID;
                            emailToSave.FolderID = dbFolderId;
                            emailToSave.FolderName = ConvertToSentenceCase(folderName);
                            emailToSave.AccountName = _accountService._userName;
                            emailToSave.UID = uid.Id;
                            emailToSave.ThreadId = threadId;

                            emailToSave.AddEmail();

                            if (emailToSave.emailID > 0 && emailToSave.TempAttachments != null)
                            {
                                foreach (var attach in emailToSave.TempAttachments)
                                {
                                    attach.EmailID = emailToSave.emailID;
                                    attach.AddAttachment();
                                    string saveFileName = $"{attach.Name}";
                                    string fullPath = Path.Combine(cacheFolder, saveFileName);
                                    if (attach.OriginalMimePart != null && !File.Exists(fullPath))
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
                            Console.WriteLine($"Err saving old mail {uid}: {innerEx.Message}");
                        }
                    }

                    await folder.CloseAsync(false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Err loading old mails: {ex.Message}");
                }
            }
        }

        // Thực hiện tải mail từ 1 folder IMAP xuống DB
        private async Task SyncFolderInternal(ImapClient client, IMailFolder folder, int localAccountID, CancellationToken token = default)
        {
            try
            {
                await folder.OpenAsync(FolderAccess.ReadOnly, token);

                string cleanName = NormalizeFolderName(folder);
                int dbFolderId = GetFolderIDByFolderName(ConvertToSentenceCase(cleanName), localAccountID);
                if (dbFolderId == 0) return;

                UniqueId lastKnownUid = GetLastSyncedUid(dbFolderId, localAccountID);

                IList<UniqueId> uidsToFetch;

                if (lastKnownUid == UniqueId.MinValue)
                {
                    // Trường hợp 1: Lần đầu tiên chạy (DB chưa có gì)
                    // Lấy 20 mail mới nhất để khởi tạo
                    int total = folder.Count;
                    int fetchCount = 10;
                    int start = Math.Max(0, total - fetchCount);
                    var summaries = await folder.FetchAsync(start, -1, MessageSummaryItems.UniqueId, token);
                    uidsToFetch = summaries.Select(x => x.UniqueId).ToList();
                }
                else
                {
                    // Trường hợp 2: Đã có dữ liệu trong DB
                    // Lấy danh sách TOÀN BỘ UID hiện có trên Server
                    var allUidsOnServer = await folder.SearchAsync(SearchQuery.All, token);

                    // 2. Dùng C# để lọc: Chỉ giữ lại những UID lớn hơn UID trong Database của bạn
                    // (uid.Id là số nguyên, so sánh trực tiếp được)
                    uidsToFetch = allUidsOnServer
                                    .Where(uid => uid.Id > lastKnownUid.Id)
                                    .OrderBy(uid => uid.Id) // Sắp xếp từ nhỏ đến lớn cho gọn
                                    .ToList();
                }

                if (uidsToFetch.Count == 0) return; // Không có gì mới

                var parser = new EmailParser();
                string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                if (!Directory.Exists(cacheFolder)) Directory.CreateDirectory(cacheFolder);

                // Duyệt qua các email
                foreach (var uid in uidsToFetch)
                {
                    try
                    {
                        long threadId = 0;
                        // Chúng ta fetch Summary trước để lấy ThreadId
                        var items = await folder.FetchAsync(new[] { uid },
                            MessageSummaryItems.GMailThreadId |
                            MessageSummaryItems.UniqueId |
                            MessageSummaryItems.Envelope | 
                            MessageSummaryItems.References);

                        var summary = items.FirstOrDefault();

                        // 2. Lấy giá trị GMThreadId (Kiểu ulong) và ép sang long
                        if (summary != null && summary.GMailThreadId.HasValue)
                        {
                            threadId = (long)summary.GMailThreadId.Value;
                        }

                        // Lấy nội dung thư đầy đủ
                        var mimeMessage = await folder.GetMessageAsync(uid, token);

                        // Parse Email
                        Email emailToSave = await parser.ParseAsync(mimeMessage);

                        // Gán thông tin Metadata

                        emailToSave.AccountID = localAccountID;
                        emailToSave.FolderID = dbFolderId;
                        emailToSave.FolderName = ConvertToSentenceCase(cleanName);
                        emailToSave.AccountName = _accountService._userName;
                        emailToSave.UID = uid.Id;
                        emailToSave.ThreadId = threadId;
                        emailToSave.MessageID = mimeMessage.MessageId;
                        emailToSave.References = mimeMessage.References.ToString();

                        // Lưu vào DB
                        emailToSave.AddEmail();

                        // Lưu Attachments
                        if (emailToSave.emailID > 0 && emailToSave.TempAttachments != null)
                        {
                            foreach (var attach in emailToSave.TempAttachments)
                            {
                                attach.EmailID = emailToSave.emailID;
                                attach.AddAttachment(); // Lưu DB

                                // Lưu File vật lý
                                string saveFileName = $"{attach.Name}";
                                string fullPath = Path.Combine(cacheFolder, saveFileName);

                                if (attach.OriginalMimePart != null && !File.Exists(fullPath))
                                {
                                    using (var fileStream = File.Create(fullPath))
                                    {
                                        await attach.OriginalMimePart.Content.DecodeToAsync(fileStream, token);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception innerEx)
                    {
                        Console.WriteLine($"Err saving mail {uid}: {innerEx.Message}");
                    }
                }

                // Đóng folder (quan trọng nếu muốn chuyển folder khác trên cùng 1 connection)
                await folder.CloseAsync(false, token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Err syncing folder {folder.Name}: {ex.Message}");
            }
        }

        public UniqueId GetLastSyncedUid(int folderID, int accountID)
        {
            string query = @"Select MAX(UID) From Email Where FolderID=@FolderID And AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FolderID", folderID),
                new SqlParameter("@AccountID", accountID)
            };

            DatabaseHelper db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                long uidVal = Convert.ToInt64(dt.Rows[0][0]);
                // Chỉ trả về nếu > 0
                if (uidVal > 0) return new UniqueId((uint)uidVal);
            }

            // Nếu chưa có mail nào (hoặc null), trả về MinValue (tương đương 0)
            return UniqueId.MinValue;
        }

        public UniqueId GetOldestSyncedUid(int folderID, int accountID)
        {
            // Lấy UID nhỏ nhất (MIN) thay vì MAX
            string query = @"Select MIN(UID) From Email Where FolderID=@FolderID And AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FolderID", folderID),
                new SqlParameter("@AccountID", accountID)
            };

            DatabaseHelper db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                long uidVal = Convert.ToInt64(dt.Rows[0][0]);
                if (uidVal > 0) return new UniqueId((uint)uidVal);
            }

            // Nếu chưa có mail nào, trả về MaxValue để logic so sánh hoạt động đúng (lấy tất cả nhỏ hơn Max)
            return UniqueId.MaxValue;
        }

        // Hàm helper để chuẩn hóa tên folder
        private string NormalizeFolderName(IMailFolder folder)
        {
            if (folder.Attributes.HasFlag(FolderAttributes.Sent) || folder.Name.EndsWith("Sent Mail")) return "Sent";
            if (folder.Attributes.HasFlag(FolderAttributes.Drafts)) return "Draft";
            if (folder.Attributes.HasFlag(FolderAttributes.Trash)) return "Trash";
            if (folder.Attributes.HasFlag(FolderAttributes.Junk)) return "Spam";
            return folder.Name.Replace("[Gmail]/", "");
        }
    }
}