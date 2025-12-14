﻿using System;
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
        public string Jsonpath = @"googlesv\mailclient.json";

        // Event fires when a token refresh successfully occurs
        // Can be used to ensure the connection remain active or update its status
        public EventHandler TokenRefreshed;
        public String? _userEmail { get; private set; } = String.Empty;
        public String? _userName { get; private set; } = String.Empty;

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
                    ApplicationName = "WPF Mail Client"
                });

                var request = peopleService.People.Get("people/me");
                request.PersonFields = "emailAddresses";

                var profile = await request.ExecuteAsync(cancellationToken);

                var primaryEmail = profile.EmailAddresses?.FirstOrDefault()?.Value;

                if (!String.IsNullOrEmpty(primaryEmail))
                {
                    System.Console.WriteLine($"[DEBUG] Successfully fetched email: {primaryEmail}");
                    return primaryEmail;
                }

                System.Console.WriteLine("[DEBUG] People API returned profile but contained no email addresses.");
                return String.Empty;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] FetchPrimaryEmailAsync failed: {ex.Message}");
                return string.Empty;
            }
        }

        private async Task<String> FetchFullNameAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_credential == null)
                    return String.Empty;

                var peopleService = new PeopleServiceService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = _credential,
                    ApplicationName = "WPF Mail Client"
                });

                var request = peopleService.People.Get("people/me");
                request.PersonFields = "names";

                var profile = await request.ExecuteAsync(cancellationToken);

                var fullName = profile.Names?.FirstOrDefault()?.DisplayName;

                if (!String.IsNullOrEmpty(fullName))
                {
                    System.Console.WriteLine($"[DEBUG] Successfully fetched full name: {fullName}");
                    return fullName;
                }

                System.Console.WriteLine("[DEBUG] People API returned profile but contained no name.");
                return String.Empty;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ERROR] FetchFullNameAsync failed: {ex.Message}");
                return string.Empty;
            }
        }

        // Sign in
        public async Task SignInAsync(IDataStore customStore)
        {
            using (var stream = new FileStream(Jsonpath, FileMode.Open, FileAccess.Read))
            {
                String[] Scopes = { "https://mail.google.com", "profile", "email" };

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    customStore);
            }

            if (IsSignedIn())
            {
                _userEmail = await FetchPrimaryEmailAsync(CancellationToken.None);
                _userName = await FetchFullNameAsync(CancellationToken.None);
            }
        }

        // Sign in check
        public bool IsSignedIn()
        {
            return _credential != null && !String.IsNullOrEmpty(_credential.Token.RefreshToken);
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
        public async Task LogoutAsync()
        {
            try
            {
                // This is the correct way to clean up the token store file
                if (Directory.Exists("token_store"))
                {
                    Directory.Delete("token_store", true);
                }

                // Clear the in-memory credential
                _credential = null;
                _userEmail = String.Empty;
                _userName = String.Empty;
            }
            catch (Exception e)
            {
                // Use System.Console as the namespace is not included globally
                System.Console.WriteLine($"Error during logout cleanup: {e.Message}");
            }
        }
    }
}
