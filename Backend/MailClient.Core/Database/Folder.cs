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
        private int AccountID;
        private string FolderName;
        private int TotalMail;
        public Folder(string Email, string folderName, int totalMail)
        {
            this.db = new DatabaseHelper();
            string query = @"Select ID from Account where Email=@Email";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@Email",Email)
            };
            DataTable dataTable = db.ExecuteQuery(query,parameters);
            this.AccountID = Convert.ToInt32(dataTable.Rows[0]["ID"]);
            this.FolderName = folderName;
            this.TotalMail = totalMail;
        }
        public int GetAccountID() { return AccountID; }
        public void SetAccountID(int AccountID) { this.AccountID = AccountID; }

        public string GetFolderName() { return FolderName; }
        public void SetFolderName(string FolderName) { this.FolderName = FolderName; }

        public int GetTotalMail() { return TotalMail; }
        public void SetTotalMail(int TotalMail) { this.TotalMail = TotalMail; }
        public void AddFolder()
        {
            string query = @"Insert into Folder(AccountID,FolderName) values (@AccountID,@FolderName) ";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FolderName", FolderName),
                new SqlParameter("@AccountID",AccountID)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public void DeleteFolder(int AccountID,string FolderID) 
        {
            string query = @"Delete from Folder Where AccountID=@AccountID and FolderName=@FolderID";
            SqlParameter[] parameters= new SqlParameter[]
            {
                new SqlParameter("@AccountID",AccountID),
                new SqlParameter("@FolderID",FolderID)
            };
            db.ExecuteNonQuery(query, parameters);
        }
    }
       
}
