using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    internal class Account
    {
        private DatabaseHelper db;
        private string Email;
        private string Password;
        private string Username;
        private string IncomingServer;
        private string IncomingPort;
        private string OutgoingServer;
        private string OutgoingPort;
        public Account(string email, string password, string username,
                   string incomingServer, string incomingPort,
                   string outgoingServer, string outgoingPort)
        {
            Email = email;
            Password = password;
            Username = username;
            IncomingServer = incomingServer;
            IncomingPort = incomingPort;
            OutgoingServer = outgoingServer;
            OutgoingPort = outgoingPort;
            db = new DatabaseHelper();
        }
        public string GetEmail() { return Email; }
        public void SetEmail(string Email) { this.Email = Email; }

        public string GetPassword() { return Password; }
        public void SetPassword(string Password) { this.Password = Password; }

        public string GetUsername() { return Username; }
        public void SetUsername(string Username) { this.Username = Username; }

        public string GetIncomingServer() { return IncomingServer; }
        public void SetIncomingServer(string IncomingServer) { this.IncomingServer = IncomingServer; }

        public string GetIncomingPort() { return IncomingPort; }
        public void SetIncomingPort(string IncomingPort) { this.IncomingPort = IncomingPort; }

        public string GetOutgoingServer() { return OutgoingServer; }
        public void SetOutgoingServer(string OutgoingServer) { this.OutgoingServer = OutgoingServer; }

        public string GetOutgoingPort() { return OutgoingPort; }
        public void SetOutgoingPort(string OutgoingPort) { this.OutgoingPort = OutgoingPort; }
        public void AddAccount()
        {
            string query = @"
            IF NOT EXISTS (SELECT 1 FROM Account WHERE Email = @Email)
            Begin
            INSERT INTO Account
            (Email, EncryptedPassword, AccountName, IncomingServer, IncomingPort, OutgoingServer, OutgoingPort)
            VALUES
            (@Email, @EncryptedPassword, @AccountName, @IncomingServer, @IncomingPort, @OutgoingServer, @OutgoingPort)
            End";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Email",Email),
                new SqlParameter("@EncryptedPassword",Password),
                new SqlParameter("@AccountName",Username),
                new SqlParameter("@IncomingServer",IncomingServer),
                new SqlParameter("@IncomingPort",IncomingPort),
                new SqlParameter("@OutgoingServer",OutgoingServer),
                new SqlParameter("@OutgoingPort",OutgoingPort)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public int CheckAccount(string email, string password)
        {
            string query = @"SELECT * FROM Account WHERE Email=@email AND EncryptedPassword=@password";
            SqlParameter[] parameters =
            {
                new SqlParameter("@email", email),
                new SqlParameter("@password", password)
            };

            DataTable dt = db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0)
                return 1;
            else
                throw new Exception("Account not found");
        }
        public void DeleteAccount()
        {
            string query = @"Delete from Account where Email=@Email";
            SqlParameter[] parameter = new SqlParameter[]
            {
                new SqlParameter("@Email",Email)
            };
            db.ExecuteNonQuery(query, parameter);     
        }

    }
}
