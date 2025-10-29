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
        public Account(DatabaseHelper database)
        {
            db = database;
        }
        public void AddAccount(string email,string pas, string name, string domain, int inport, int outport,string outserver)
        {
            string query = @"
INSERT INTO Account
(Email, EncryptedPassword, AccountName, IncomingServer, IncomingPort, OutgoingServer, OutgoingPort, UseSSL, LastSyncTime)
VALUES
(@Email, @EncryptedPassword, @AccountName, @IncomingServer, @IncomingPort, @OutgoingServer, @OutgoingPort, @UseSSL, @LastSyncTime)";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Email",email),
                new SqlParameter("@EncryptedPassword",pas),
                new SqlParameter("@AccountName",name),
                new SqlParameter("@IncomingServer",domain),
                new SqlParameter("@IncomingPort",inport),
                new SqlParameter("@OutgoingServer",outserver),
                new SqlParameter("@OutgoingPort",outport),
                new SqlParameter("@UseSSL",true),
                new SqlParameter("@LastSyncTime",DateTime.Now)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public DataTable GetAllAccounts()
        {
            string query = "SELECT * FROM Account";
            return db.ExecuteQuery(query);
        }
        public int GetAccountIDByEmail(string email)
        {
            string query = @"Select AccountID\nfrom Account\nWhere Email=@email";
            SqlParameter[] parameter = new SqlParameter[] 
            {
                new SqlParameter("@email", email)
            };
            DataTable dt=db.ExecuteQuery(query, parameter);
            if (dt.Rows.Count > 0)
                return Convert.ToInt32(dt.Rows[0]["ID"]);
            else
                throw new Exception("Account not found");
        }
    }
}
