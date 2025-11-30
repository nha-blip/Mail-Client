using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Util;
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
                String[] Scopes = { "https://mail.google.com" };

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
        }

        // Sign in check
        public bool IsSignedIn()
        {
            return _credential != null && !String.IsNullOrEmpty(_credential.Token.RefreshToken);
        }

        // Get email
        public String GetCurrentUserEmail()
        {
            return _credential?.UserId; // ? => check if _credential is null before accessing UserId
        }

        // Ensures the access token is valid (refreshing if neccessary) and returns it
        // This is crucial before using the token for XOAUTH2 authentication
        public async Task<String> GetAccessTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_credential == null)
            {
                throw new InvalidOperationException("User is not signed in. Credential is null.");
            }

            // Check if the token needs to be refreshed. If expired, it handles the refresh
            if (_credential.Token.IsExpired(SystemClock.Default))
            {
                bool success = await _credential.RefreshTokenAsync(cancellationToken);
                if (success)
                {
                    TokenRefreshed?.Invoke(this, EventArgs.Empty);
                }
                else
                {
                    throw new Exception("Failed to refresh Google access token. User may need to sign in again");
                }
            }

            return _credential.Token.AccessToken;
        }

        // Log out
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
