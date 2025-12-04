using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    public class Account
    {
        private DatabaseHelper db;
        public int AccountID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }

        public Account(string email, string username)
        {
            Email = email;
            Username = username;
            db = new DatabaseHelper();
        }
        public Account(int id)
        {
            AccountID = id;
            string query = @"select * from Account where AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@AccountID",id)
            };
            db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
            {
                Username = Convert.ToString(dt.Rows[0]["AccountName"]) ?? "";
                Email = Convert.ToString(dt.Rows[0]["Email"]) ?? "";
            }
        }
        public void AddAccount()
        {
            string query = @"
            IF NOT EXISTS (SELECT 1 FROM Account WHERE Email = @Email)
            Begin
            INSERT INTO Account
            (Email, EncryptedPassword, AccountName)
            VALUES
            (@Email, 'GoogleAuth', @AccountName);
            SELECT SCOPE_IDENTITY();
            End";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Email",Email),
                new SqlParameter("@AccountName",Username)
            };
            DataTable dt = db.ExecuteQuery(query, parameters);


            if (dt.Rows.Count > 0)
            {
                this.AccountID = Convert.ToInt32(dt.Rows[0][0]);
            }
            query = @"INSERT INTO Folder (AccountID, FolderName)
                        VALUES
                            (@AccID, 'Inbox'),
                            (@AccID, 'Sent'),
                            (@AccID, 'Spam'),
                            (@AccID, 'Trash'),
                            (@AccID, 'Draft');
                        ";
            SqlParameter[] folder = new SqlParameter[]
            {
                new SqlParameter("@AccID",AccountID)
            };
            db.ExecuteNonQuery(query, folder);

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
            string query = @"Delete from Account where AccountID=@AccountID";
            SqlParameter[] parameter = new SqlParameter[]
            {
                new SqlParameter("@AccountID",AccountID)
            };
            db.ExecuteNonQuery(query, parameter);
        }
        public int LoginOrRegisterGoogle()
        {
            // Sửa 'EmailAddress' thành 'Email' (hoặc tên đúng trong DB của bạn)
            string checkQuery = "SELECT AccountID FROM Account WHERE Email = @Email";

            SqlParameter[] checkParams = { new SqlParameter("@Email", Email) };

            DataTable dt = db.ExecuteQuery(checkQuery, checkParams);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["AccountID"]);
            }
            else
            {
                AddAccount();
                return AccountID;
            }
        }

    }
}