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
        public Folder(DatabaseHelper data)
        {
            db = data;
        }
        public void AddFolder(int AccountID,string name)
        {
            string query = @"Insert into Folder(AccountID,FdName) values (@AccountID,@FdName) ";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FdName", name),
                new SqlParameter("@AccountID",AccountID)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public void DeleteFolder(int AccountID,string FolderID) 
        {
            string query = @"Delete from Folder\n Where AccountID=@AccountID and FolderID=@FolderID";
            SqlParameter[] parameters= new SqlParameter[]
            {
                new SqlParameter("@AccountID",AccountID),
                new SqlParameter("@FolderID",FolderID)
            };
            db.ExecuteNonQuery(query, parameters);
        }
    }
       
}
