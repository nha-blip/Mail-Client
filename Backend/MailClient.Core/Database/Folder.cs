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
        private string Email;
        private string FolderName;
        private int TotalMail;
        // Khởi tạo folder
        public Folder(string Email, string folderName, int totalMail)
        {
            this.db = new DatabaseHelper();
            this.Email = Email;
            this.FolderName = folderName;
            this.TotalMail = totalMail;
        }
        public string GetEmail() { return Email; }
        public void SetEmail(string Email) { this.Email = Email; }

        public string GetFolderName() { return FolderName; }
        public void SetFolderName(string FolderName) { this.FolderName = FolderName; }

        public int GetTotalMail() { return TotalMail; }
        public void SetTotalMail(int TotalMail) { this.TotalMail = TotalMail; }
        public void AddFolder() // Thêm folder vào database
        {
            string query = @"Insert into Folder(Email,FolderName) values (@Email,@FolderName) ";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FolderName", FolderName),
                new SqlParameter("@Email",Email)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public void DeleteFolder() // Xóa folder 
        {
            string query = @"Delete from Folder Where Email=@Email and FolderName=@FolderName";
            SqlParameter[] parameters= new SqlParameter[]
            {
                new SqlParameter("@Email",Email),
                new SqlParameter("@FolderName",FolderName)
            };
            db.ExecuteNonQuery(query, parameters);
        }
    }
       
}
