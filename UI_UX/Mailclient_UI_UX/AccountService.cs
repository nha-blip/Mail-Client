using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.PeopleService.v1;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;

namespace MailClient.Core.Services
{
    public class AccountService
    {
        private UserCredential _credential;
        public UserCredential Credential => _credential;

        // Event fires when a token refresh successfully occurs
        // Can be used to ensure the connection remain active or update its status
        public EventHandler TokenRefreshed;
        private String? _cachedEmail = String.Empty;
        private readonly GmailStore _gmailStore;

        // Constructor mới: Nhận đối tượng GmailStore đã đăng nhập
        public AccountService(GmailStore gmailStore)
        {
            _gmailStore = gmailStore;
        }
        // Get Email Info
        private async Task<String> FetchPrimaryEmailAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_credential == null)
                    return String.Empty;

                var peopleService = new PeopleServiceService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "MailClient"
                });

                var request = peopleService.People.Get("people/me");
                request.PersonFields = "emailAddresses";

                var profile = await request.ExecuteAsync(cancellationToken);

                var primaryEmail = profile.EmailAddresses?.FirstOrDefault()?.Value;

                if (!String.IsNullOrEmpty(primaryEmail))
                {
                    Console.WriteLine($"[DEBUG] Successfully fetched email: {primaryEmail}");
                    return primaryEmail;
                }

                Console.WriteLine("[DEBUG] People API returned profile but contained no email addresses.");
                return String.Empty;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] FetchPrimaryEmailAsync failed: {ex.Message}");
                return string.Empty;
            }
        }

        // Sign in
        public async Task SignInAsync(String credentialPath, String tokenPath)
        {
            using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                String credPath = tokenPath;
                String[] Scopes = { "https://mail.google.com", "profile", "email" };

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }

            if (IsSignedIn())
            {
                String email = await FetchPrimaryEmailAsync(CancellationToken.None);
                if (!String.IsNullOrEmpty(email))
                {
                    SetCurrentEmail(email);
                }
            }
        }

        // Sign in check
        public bool IsSignedIn()
        {
            // Kiểm tra xem Service (từ GmailStore) đã được khởi tạo sau LoginAsync chưa
            return _gmailStore.Service != null;
        }

        // Set cachedEmail <IMPORTANT FOR LATTER OPERATIONS>
        private void SetCurrentEmail(String email)
        {
            _cachedEmail = email;
        }

        // Get email
        
        

        // Log out
        public async Task LogoutAsync(String tokenPath)
        {
            try
            {
                // This is the correct way to clean up the token store file
                if (Directory.Exists(tokenPath))
                {
                    Directory.Delete(tokenPath, true);
                }

                // Clear the in-memory credential
                _credential = null;
                _cachedEmail = String.Empty;
            }
            catch (Exception e)
            {
                // Use System.Console as the namespace is not included globally
                System.Console.WriteLine($"Error during logout cleanup: {e.Message}");
            }
        }
        public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            var credential = _gmailStore.Service.HttpClientInitializer as UserCredential;

            if (credential == null)
            {
                throw new InvalidOperationException("Credential object is null.");
            }

            // 1. Kiểm tra nếu Token là NULL (có thể xảy ra ngay sau khi đăng nhập nhưng chưa tải token)
            if (credential.Token == null)
            {
                // Tùy chọn: Thử tải hoặc làm mới token ở đây nếu cần thiết, 
                // hoặc ném ngoại lệ nếu không có cách nào để lấy token.
                throw new InvalidOperationException("Credential Token data is missing.");
            }
            var clock = Google.Apis.Util.SystemClock.Default;
            // 2. Kiểm tra Token hết hạn VÀ có Refresh Token (để làm mới)
            // Dùng toán tử Null-coalescing '??' hoặc kiểm tra null rõ ràng.
            if (credential.Token.IsExpired(clock) && credential.Token.RefreshToken != null)
            {
                // Làm mới token
                bool success = await credential.RefreshTokenAsync(cancellationToken);

                if (!success)
                {
                    throw new InvalidOperationException("Failed to refresh access token. User must re-login.");
                }
            }

            // 3. Trả về Access Token đã được làm mới hoặc còn hạn
            return credential.Token.AccessToken;
        }

        // 3. Lấy Email
        public string GetCurrentUserEmail()
        {
            return _gmailStore.UserEmail;
        }
    }
}
