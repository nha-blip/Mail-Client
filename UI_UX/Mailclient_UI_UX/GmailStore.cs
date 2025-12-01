using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System.Windows.Threading;
namespace MailClient
{

    public class GmailStore
    {
        
        static string[] Scopes = { GmailService.Scope.GmailModify };
        static string ApplicationName = "WPF Mail Client";
        public GmailService Service { get; private set; }
        public string UserEmail { get; private set; }

        public async Task<bool> LoginAsync()
        {
            try
            {
                UserCredential credential;
                // Nhớ sửa đường dẫn file json của bạn nếu cần
                string jsonPath = @"mailclient.json";
                if (!File.Exists(jsonPath))
                {
                    // Thử tìm đường dẫn tuyệt đối nếu file local ko có (Code hỗ trợ bạn debug)
                    jsonPath = @"D:\NHA\IT008_Lập trình trực quan\test\UI_UX-20251201T123457Z-1-001\UI_UX\Mailclient_UI_UX\googlesv\mailclient.json";
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

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi đăng nhập Google: " + ex.Message);
                return false;
            }
        }

        public async Task SyncEmailsToDatabase(int localAccountID)
        {
            if (Service == null) return;

            // Lấy danh sách ID thư
            var request = Service.Users.Messages.List("me");
            request.LabelIds = new List<string>() { "INBOX" };
            request.MaxResults = 50;

            var response = await request.ExecuteAsync();

            if (response.Messages != null)
            {
                foreach (var msgItem in response.Messages)
                {
                    // Lấy chi tiết thư (Full format)
                    var emailReq = Service.Users.Messages.Get("me", msgItem.Id);
                    emailReq.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Full;
                    var emailInfo = await emailReq.ExecuteAsync();

                    // 1. Lấy Header
                    string subject = GetHeader(emailInfo.Payload.Headers, "Subject");
                    string from = GetHeader(emailInfo.Payload.Headers, "From");
                    string dateStr = GetHeader(emailInfo.Payload.Headers, "Date");

                    // 2. LẤY FULL BODY (Thay vì Snippet)
                    string body = GetEmailBody(emailInfo.Payload);

                    // 3. Xử lý ngày tháng
                    DateTimeOffset dateOffset;
                    if (!DateTimeOffset.TryParse(dateStr, out dateOffset))
                        dateOffset = DateTimeOffset.Now;
                    DateTime dateReceived = dateOffset.DateTime;

                    // 4. Lưu vào Database
                    string[] to = { "me" };

                    // Kiểm tra thư đã tồn tại chưa để tránh trùng (Dựa vào ID thư của Google nếu bạn lưu nó, hoặc tạm thời check subject/time)
                    // Ở đây mình insert luôn để demo:
                    MailClient.Email newEmail = new MailClient.Email(
                        localAccountID, 1, "Inbox", UserEmail,
                        subject, from, to, dateReceived, DateTime.Now,
                        body, // <-- Giờ đây là HTML xịn
                        false
                    );
                    newEmail.AddEmail();
                }
            }
        }

        // --- CÁC HÀM HỖ TRỢ LẤY DỮ LIỆU ---

        private string GetHeader(IList<MessagePartHeader> headers, string name)
        {
            var header = headers.FirstOrDefault(h => h.Name == name);
            return header != null ? header.Value : "(No Subject)";
        }

        // Hàm đệ quy để tìm nội dung HTML trong đống Parts hỗn độn của Gmail
        private string GetEmailBody(MessagePart payload)
        {
            string encodedData = "";

            // Trường hợp 1: Body nằm ngay ở ngoài (thư đơn giản)
            if (payload.Body != null && payload.Body.Data != null)
            {
                encodedData = payload.Body.Data;
            }
            // Trường hợp 2: Body nằm trong Parts (thư chứa HTML, file đính kèm...)
            else if (payload.Parts != null)
            {
                foreach (var part in payload.Parts)
                {
                    // Ưu tiên lấy text/html
                    if (part.MimeType == "text/html" && part.Body.Data != null)
                    {
                        encodedData = part.Body.Data;
                        break;
                    }
                    // Nếu không có html thì lấy tạm text/plain
                    if (part.MimeType == "text/plain" && part.Body.Data != null)
                    {
                        encodedData = part.Body.Data;
                    }
                    // Nếu vẫn chưa thấy, tìm sâu hơn (đệ quy)
                    if (part.Parts != null)
                    {
                        string nested = GetEmailBody(part);
                        if (!string.IsNullOrEmpty(nested)) return nested;
                    }
                }
            }

            // Giải mã Base64Url sang HTML String
            if (!string.IsNullOrEmpty(encodedData))
            {
                return DecodeBase64Url(encodedData);
            }

            return "(Không có nội dung)";
        }

        // Hàm giải mã chuẩn của Google
        private string DecodeBase64Url(string base64Url)
        {
            string base64 = base64Url.Replace("-", "+").Replace("_", "/");
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            byte[] bytes = Convert.FromBase64String(base64);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}