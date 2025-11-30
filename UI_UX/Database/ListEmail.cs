using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    public class ListEmail
    {
        private DatabaseHelper db;
        public int soluong;
        public List<Email> listemail;
        public ListEmail()
        {
            listemail = new List<Email>();
            db = new DatabaseHelper();
            string query = @"SELECT E.*, F.FolderName,A.AccountName 
                            FROM Email E
                            JOIN Folder F ON E.FolderID = F.FolderID AND E.AccountID = F.AccountID
                            JOIN Account A ON E.AccountID=A.AccountID";
            DataTable email = db.ExecuteQuery(query);

            foreach (DataRow row in email.Rows)
            {
                string toField = Convert.ToString(row["ToAdd"]) ?? "";
                string[] toArray = string.IsNullOrWhiteSpace(toField) ? new string[0] : toField.Split(',');
                
                Email e = new Email(
                    Convert.ToInt32(row["AccountID"]),
                    Convert.ToInt32(row["FolderID"]),
                    Convert.ToString(row["FolderName"]) ?? "",
                    Convert.ToString(row["AccountName"]) ?? "",
                    Convert.ToString(row["SubjectEmail"]) ?? "",
                    Convert.ToString(row["FromAdd"]) ?? "",
                    toArray,
                    Convert.ToDateTime(row["DateSent"]),
                    Convert.ToDateTime(row["DateReceived"]),
                    Convert.ToString(row["BodyText"]) ?? "",
                    Convert.ToBoolean(row["IsRead"] ?? false),
                    Convert.ToInt32(row["ID"])
                );
                listemail.Add(e);
            }
        }
        public void DeleteEmail()
        {
            string query = @"Delete From Email where IsFlag=1";
            db.ExecuteNonQuery(query);
            foreach(Email e in listemail)
            {
                if (e.IsFlag)
                {
                    listemail.Remove(e);
                    
                    query = @"Update Folder set TotalMail=TotalMail-1 where FolderID=@FolderID and AccountID=@AccountID";
                    SqlParameter[] Update = new SqlParameter[]
                    {
                        new SqlParameter("@FolderID",e.FolderID),
                        new SqlParameter("@AccountID",e.AccountID)
                    };
                    db.ExecuteNonQuery(query, Update);
                }
            }
        }
        public void AddEmail(Email e)
        {
            e.AddEmail();
            this.listemail.Add(e);
        }
        //public void Print()
        //{
        //    foreach (Email e in listemail)
        //    {
        //        e.PrintE();
        //    }
        //}

    }
}
