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
        public Account(int id)
        {
            db = new DatabaseHelper();
            this.AccountID = id;

            // Tự động vào SQL lấy thông tin ra luôn
            string query = "SELECT * FROM Account WHERE AccountID = @ID";
            SqlParameter[] p = { new SqlParameter("@ID", id) };

            DataTable dt = db.ExecuteQuery(query, p);
            if (dt.Rows.Count > 0)
            {
                DataRow row = dt.Rows[0];
                this.Email = row["Email"].ToString();
                this.Username = row["AccountName"].ToString();
                this.Password = row["EncryptedPassword"].ToString();
            }
        }
        public Account(string email, string username)
        {
            Email = email;
            Username = username;
            db = new DatabaseHelper();
        }       
        public void AddAccount()
        {
            string query = @"
            IF NOT EXISTS (SELECT 1 FROM Account WHERE Email = @Email)
            Begin
            INSERT INTO Account
            (Email, EncryptedPassword, AccountName)
            VALUES
            (@Email, @EncryptedPassword, @AccountName);
            SELECT SCOPE_IDENTITY();
            End";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Email",Email),
                new SqlParameter("@EncryptedPassword","Google Auth"),
                new SqlParameter("@AccountName",Username)
            };
            DataTable dt=db.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
            {
                this.AccountID = Convert.ToInt32(dt.Rows[0][0]);
            }
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

    }
}
