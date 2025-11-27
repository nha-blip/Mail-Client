using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    public class Folder
    {
        private DatabaseHelper db;
        public int FolderID { get; set; } 
        public int AccountID { get; set; }
        public string FolderName { get; set; }
        public int TotalMail {  get; set; }
        // Khởi tạo folder
        public Folder(int AccountID, string folderName, int totalMail)
        {
            this.db = new DatabaseHelper();
            this.AccountID = AccountID;
            this.FolderName = folderName;
            this.TotalMail = totalMail;
        }
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
