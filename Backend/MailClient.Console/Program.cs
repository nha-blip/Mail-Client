using MailClient.Core.Models;
using MailClient.Core.Services;
using System.Net.Mail;

namespace MailClient.Console
{
    class Program
    {
        public static async Task Main(string[] args)
        {
            // Sign in
            System.Console.WriteLine("Starting sign in test...");
            var accountService = new AccountService();
            String credentialPath = "credentials.json";
            String tokenPath = "RefreshTokenStore";

            try
            {
                // Check if credentials file exists in the runtime path
                if (!File.Exists(credentialPath))
                {
                    System.Console.WriteLine("---------------------------------------------");
                    System.Console.WriteLine($"ERROR: The required file '{credentialPath}' was not found.");
                    System.Console.WriteLine("Please ensure the file is in the application's runtime directory (e.g., bin/Debug/...).");
                    System.Console.WriteLine("In Visual Studio, set the file's 'Copy to Output Directory' property to 'Copy if newer'.");
                    System.Console.WriteLine("---------------------------------------------");
                    return; // Exit if file is missing
                }

                // Sign in
                System.Console.WriteLine("Starting sign in...");
                System.Console.WriteLine(">>> NOTE: If this is the first time, a browser window will open. Complete the sign-in process there.");

                // This call blocks until the browser interaction is complete or the token is loaded from disk.
                await accountService.SignInAsync(credentialPath, tokenPath);

                if (accountService.IsSignedIn())
                {
                    accountService.SetCurrentEmail(accountService.FetchPrimaryEmailAsync());
                    System.Console.WriteLine("---------------------------------------------");
                    System.Console.WriteLine("Sign in complete! Successfully obtained token.");
                    System.Console.WriteLine($"Logged in Email: {accountService.GetCurrentUserEmail()}");
                    System.Console.WriteLine("---------------------------------------------");

                    // --- Send email test ---
                    // Renamed the variable and class usage to MailModel as requested
                    MailModel mailModel = new MailModel();
                    mailModel.From = "nguyencsgo2006@gmail.com"; // MUST be the authenticated user's email
                    mailModel.To = new List<String> { "huunguyen.personal@gmail.com" }; // Change to a valid recipient
                    mailModel.Subject = "Test Email from MailClient OAuth2";
                    mailModel.TextBody = "This is the plain text body of the test email.";
                    mailModel.HtmlBody = "<h1>Hello from MailClient!</h1><p>This is the <b>HTML body</b> with rich text.</p>";
                    // Ensure this file path is valid on your machine for testing attachments
                    mailModel.Attachments = new List<String> { "C:\\Study\\IT008\\BTH3_NguyenHuuNguyen_24521189.pdf" };

                    MailService mailService = new MailService(accountService);

                    System.Console.Write("Attempting to send email...");
                    await mailService.SendEmailAsync(mailModel);
                    System.Console.WriteLine("SUCCESS!");

                }
                else
                {
                    // This block means SignInAsync finished, but IsSignedIn is false, 
                    // likely due to authentication failure in the browser.
                    System.Console.WriteLine("ERROR: Sign in failed. Check network connection or ensure you granted access in the browser.");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nCRITICAL FAILURE: {ex.Message}");
                // The inner exception often contains the true error (e.g., FileStream failed)
                System.Console.WriteLine("Details: " + (ex.InnerException?.Message ?? "No inner exception details."));
            }
            finally
            {
                // Sign out (Clean up saved token)
                if (accountService.IsSignedIn())
                {
                    System.Console.WriteLine("\nStarting sign out test (removing local token)...");
                    await accountService.LogoutAsync(tokenPath);
                    System.Console.WriteLine("Log out complete!");
                }
            }
        }
    }
}