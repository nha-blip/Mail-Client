using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Mailclient;
using MailClient.Core.Services;
using Microsoft.Data.SqlClient;
using MimeKit;

namespace MailClient
{
    public class GmailStore
    {
        static string ApplicationName = "WPF Mail Client";
        public GmailService Service { get; private set; }
        public string UserEmail { get; private set; }
        public string Username { get; private set; }
        static string[] Scopes = {
            "https://mail.google.com/",
            "https://www.googleapis.com/auth/userinfo.profile",
            "https://www.googleapis.com/auth/userinfo.email",
            GmailService.Scope.MailGoogleCom,
        };
        public async Task<bool> LoginAsync()
        {
            try
            {
                UserCredential credential;
                string jsonPath = @"mailclient.json";
                if (!File.Exists(jsonPath))
                {
                    // Đường dẫn dự phòng (Hardcode để debug)
                    jsonPath = @"D:\NHA\IT008_Lập trình trực quan\MailClient\UI_UX\Mailclient_UI_UX\googlesv\mailclient.json";
                }

                using (var stream = new FileStream(jsonPath, FileMode.Open, FileAccess.Read))
                {
                    string credPath = "token_store";
                    credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                        GoogleClientSecrets.FromStream(stream).Secrets,
                        Scopes,
                        "user",
                        CancellationToken.None,
                        new FileDataStore(credPath, true));
                }

                Service = new GmailService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName,
                });

                var profile = await Service.Users.GetProfile("me").ExecuteAsync();
                UserEmail = profile.EmailAddress;

                // 2. People API (để lấy Display Name)
                var peopleService = new PeopleServiceService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = ApplicationName
                });

                var request = peopleService.People.Get("people/me");
                request.PersonFields = "names";

                var me = await request.ExecuteAsync();
                Username = me.Names?.FirstOrDefault()?.DisplayName ?? "";
                return true;

            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đăng nhập Google: " + ex.Message);
                return false;
            }
        }
        public int GetFolderIDByFolderName(string folderName)
        {
            string query = @"Select FolderID
                            From Folder
                            Where FolderName=@folderName And AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@folderName",folderName),
                new SqlParameter("@AccountID",App.CurrentAccountID)
            };
            DatabaseHelper db=new DatabaseHelper();
            DataTable dt=db.ExecuteQuery(query,parameters);
            return Convert.ToInt32(dt.Rows[0][0]);
        }
        public static string ConvertToSentenceCase(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }
            string lowerCase = input.ToLower();
            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            string result = lowerCase.Substring(0, 1).ToUpper() + lowerCase.Substring(1);

            return result;
        }
        public async Task SyncAllFoldersToDatabase(int localAccountID)
        {
            string[] foldersToSync = { "INBOX", "SENT", "DRAFT", "TRASH","SPAM" };

            foreach (var folderName in foldersToSync)
            {
                await SyncEmailsToDatabase(localAccountID, folderName);
            }
        }
        public async Task SyncEmailsToDatabase(int localAccountID,string foldername)
        {
            if (Service == null) return;

            var request = Service.Users.Messages.List("me");
            request.LabelIds = new List<string>() { foldername };
            request.MaxResults = 20;

            var response = await request.ExecuteAsync();

            if (response.Messages != null)
            {
                var parser = new EmailParser();

                // 1. TẠO THƯ MỤC CACHE (Nếu chưa có)
                // Đường dẫn: bin/Debug/net8.0-windows/Attachments/
                string cacheFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                if (!Directory.Exists(cacheFolder))
                {
                    Directory.CreateDirectory(cacheFolder);
                }

                foreach (var msgItem in response.Messages)
                {
                    try
                    {
                        // Tải RAW
                        var emailReq = Service.Users.Messages.Get("me", msgItem.Id);
                        emailReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                        var emailInfo = await emailReq.ExecuteAsync();
                        byte[] emailBytes = Convert.FromBase64String(emailInfo.Raw.Replace("-", "+").Replace("_", "/"));
                        using (var stream = new MemoryStream(emailBytes))
                        {
                            var mimeMessage = MimeMessage.Load(stream);

                            // Parse
                            MailClient.Email emailToSave = await parser.ParseAsync(mimeMessage);
                            // Điền thông tin còn thiếu
                            emailToSave.AccountID = localAccountID;
                            emailToSave.FolderName = ConvertToSentenceCase(foldername);
                            emailToSave.FolderID=GetFolderIDByFolderName(emailToSave.FolderName);
                            emailToSave.AccountName = UserEmail;

                            // Lưu Email vào DB
                            emailToSave.AddEmail();

                            // 2. LƯU ATTACHMENT
                            if (emailToSave.emailID > 0 && emailToSave.TempAttachments.Count > 0)
                            {
                                foreach (var attach in emailToSave.TempAttachments)
                                {
                                    // A. Lưu thông tin vào DB
                                    attach.EmailID = emailToSave.emailID;
                                    attach.AddAttachment(); // Lúc này attach.ID được tạo ra (ví dụ: 101)

                                    // B. Lưu file vật lý vào thư mục Cache
                                    // Tên file: {ID}_{TênGốc} để tránh trùng lặp (ví dụ: 101_bailam.pdf)
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
                    catch (Exception ex) { Console.WriteLine("Lỗi sync: " + ex.Message); }
                }
            }
        }
    }

}