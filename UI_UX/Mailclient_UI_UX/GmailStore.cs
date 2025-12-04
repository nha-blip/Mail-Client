using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Mailclient;
using MailClient.Core.Services; // Nếu EmailParser nằm ở đây
using Microsoft.Data.SqlClient;
using MimeKit;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MailClient
{
    public class GmailStore
    {
        static string ApplicationName = "WPF Mail Client";
        public GmailService Service { get; set; }
        public string UserEmail { get; set; }
        public string Username { get; set; }

        static string[] Scopes = {
            "https://mail.google.com/",
            "https://www.googleapis.com/auth/userinfo.profile",
            "https://www.googleapis.com/auth/userinfo.email",
            GmailService.Scope.MailGoogleCom,
        };

        UserCredential credential;

        // Đường dẫn file cấu hình Client Secret (ID của App)
        // Lưu ý: Đây là file cấu hình của APP, không phải token của User
        string jsonPath = @"mailclient.json";

        // --- [QUAN TRỌNG] HÀM LOGIN ĐÃ SỬA ĐỔI ---
        // Thêm tham số customStore để nhận AccountTokenStore
        public async Task<bool> LoginAsync(string userId, IDataStore customStore)
        {
            try
            {
                // Kiểm tra file client secret
                if (!File.Exists(jsonPath))
                {
                    // Backup path (như code cũ của bạn)
                    jsonPath = @"D:\drive-download-20251202T014458Z-1-001\UI_UX\Mailclient_UI_UX\googlesv\mailclient.json";
                }

                if (!File.Exists(jsonPath))
                {
                    MessageBox.Show("Không tìm thấy file cấu hình 'mailclient.json'!");
                    return false;
                }

                using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
                {
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        userId, // ID user (thường là "user" hoặc email)
                        CancellationToken.None,
                        customStore // <--- DÙNG STORE CỦA BẠN THAY VÌ FileDataStore
                    );
                }

                // Khởi tạo dịch vụ Gmail
                Service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                // 1. Lấy thông tin Email
                var profile = await Service.Users.GetProfile("me").ExecuteAsync();
                UserEmail = profile.EmailAddress;

                // 2. People API (để lấy Tên hiển thị)
                try
                {
                    var peopleService = new PeopleServiceService(new BaseClientService.Initializer()
                    {
                        HttpClientInitializer = credential,
                        ApplicationName = ApplicationName
                    });

                    var request = peopleService.People.Get("people/me");
                    request.PersonFields = "names";

                    var me = await request.ExecuteAsync();
                    Username = me.Names?.FirstOrDefault()?.DisplayName ?? UserEmail;
                }
                catch
                {
                    // Nếu lỗi lấy tên thì dùng tạm email làm tên
                    Username = UserEmail;
                }

                return true;
            }
            catch (Exception ex)
            {
                // Nếu lỗi "invalid_grant" nghĩa là token hết hạn hoặc bị thu hồi
                // Bạn có thể return false để bên ngoài biết mà xử lý (ví dụ xóa token trong DB)
                MessageBox.Show("Lỗi đăng nhập Google: " + ex.Message);
                return false;
            }
        }

        // --- ĐÃ XÓA HÀM ReloadUserToken (VÌ KHÔNG CẦN NỮA) ---

        public int GetFolderIDByFolderName(string folderName)
        {
            string query = @"Select FolderID From Folder Where FolderName=@folderName And AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@folderName", folderName),
                new SqlParameter("@AccountID", App.CurrentAccountID)
            };

            DatabaseHelper db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0][0]);
            return 0; // Tránh lỗi nếu không tìm thấy
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
            if (Service == null) return;

            try
            {
                var request = Service.Users.Messages.List("me");
                request.LabelIds = new List<string>() { foldername };
                request.MaxResults = 20; // Chỉ lấy 20 mail mới nhất để test cho nhanh

                var response = await request.ExecuteAsync();

                if (response.Messages != null)
                {
                    var parser = new EmailParser();

                    // Tạo thư mục cache
                    string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                    if (!Directory.Exists(cacheFolder))
                    {
                        Directory.CreateDirectory(cacheFolder);
                    }

                    foreach (var msgItem in response.Messages)
                    {
                        try
                        {
                            // Tải nội dung RAW của email
                            var emailReq = Service.Users.Messages.Get("me", msgItem.Id);
                            emailReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                            var emailInfo = await emailReq.ExecuteAsync();

                            // Decode Base64URL
                            byte[] emailBytes = Convert.FromBase64String(emailInfo.Raw.Replace("-", "+").Replace("_", "/"));

                            using (var stream = new MemoryStream(emailBytes))
                            {
                                var mimeMessage = MimeMessage.Load(stream);

                                // Parse email (Dùng parser của bạn)
                                MailClient.Email emailToSave = await parser.ParseAsync(mimeMessage);

                                // Điền thông tin bổ sung
                                emailToSave.AccountID = localAccountID;
                                emailToSave.FolderName = ConvertToSentenceCase(foldername);
                                emailToSave.FolderID = GetFolderIDByFolderName(emailToSave.FolderName);
                                emailToSave.AccountName = UserEmail;

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
                        }
                        catch (Exception innerEx)
                        {
                            Console.WriteLine($"Lỗi sync email {msgItem.Id}: {innerEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi sync folder {foldername}: {ex.Message}");
            }
        }
    }
}