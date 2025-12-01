using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using Google.Apis.Util;
using Google.Apis.PeopleService.v1;
using MailKit.Net.Smtp;
using MailKit.Net.Imap;
using MailKit.Security;
using System.Runtime.CompilerServices;
using Google.Apis.Services;

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
            return _credential != null && !String.IsNullOrEmpty(_credential.Token.RefreshToken);
        }

        // Set cachedEmail <IMPORTANT FOR LATTER OPERATIONS>
        private void SetCurrentEmail(String email)
        {
            _cachedEmail = email;
        }

        // Get email
        public String GetCurrentUserEmail()
        {
            return !String.IsNullOrEmpty(_cachedEmail) ? _cachedEmail : _credential?.UserId; // ? => check if _credential is null before accessing UserId
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
    }
}
