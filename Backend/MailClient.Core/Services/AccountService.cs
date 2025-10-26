using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using MailKit.Net.Smtp;
using MailKit.Net.Imap;
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

        // Sign in
        public async Task SignInAsync(String credentialPath, String tokenPath)
        {
            using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                String credPath = tokenPath;

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { "https://mail.google.com" },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
        }

        // Sign in check
        public bool IsSignedIn()
        {
            return _credential != null && String.IsNullOrEmpty(_credential.Token.RefreshToken);
        }

        // Get email
        public String GetCurrentUserEmail()
        {
            return _credential?.UserId; // ? => check if _credential is null before accessing UserId
        }

        // Send email
        public async Task LogoutAsync(String tokenPath)
        {
            try
            {
                if (Directory.Exists(tokenPath))
                {
                    Directory.Delete(tokenPath, true);
                }
                _credential = null;

                await Task.CompletedTask;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
