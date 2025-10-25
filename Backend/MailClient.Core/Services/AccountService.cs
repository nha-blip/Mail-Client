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
        // Sign in
        public async Task SignInAsync(String credentialPath, String tokenPath)
        {
            using (var stream = new FileStream(credentialPath, FileMode.Open, FileAccess.Read))
            {
                String credPath = tokenPath;

                _credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.FromStream(stream).Secrets,
                    new[] { "http://mail.google.com" },
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true));
            }
        }
    }
}
