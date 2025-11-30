using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    internal class Folder
    {
        private DatabaseHelper db;
        private int FolderID;
        private int AccountID;
        private string FolderName;
        private int TotalMail;
        // Khởi tạo folder
        public Folder(int AccountID, string folderName, int totalMail)
        {
            this.db = new DatabaseHelper();
            this.AccountID = AccountID;
            this.FolderName = folderName;
            this.TotalMail = totalMail;
        }
        public int GetAccountID() {  return this.AccountID; }
        public int GetEmail() { return AccountID; }
        public void SetEmail(int Email) { this.AccountID = Email; }

        public string GetFolderName() { return FolderName; }
        public void SetFolderName(string FolderName) { this.FolderName = FolderName; }

        public int GetTotalMail() { return TotalMail; }
        public void SetTotalMail(int TotalMail) { this.TotalMail = TotalMail; }
        public void AddFolder() // Thêm folder vào database
        {
            string query = @"Insert into Folder(AccountID,FolderName) values (@AccountID,@FolderName) ";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@AccountID",AccountID),
                new SqlParameter("@FolderName", FolderName)
                
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public void DeleteFolder() // Xóa folder 
        {
            string query = @"Delete from Folder Where FolderID=@FolderID";
            SqlParameter[] parameters= new SqlParameter[]
            {
                new SqlParameter("@FolderID",FolderID)
            };
            db.ExecuteNonQuery(query, parameters);
        }
    }
       
}
