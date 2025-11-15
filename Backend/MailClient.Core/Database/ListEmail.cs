using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    internal class ListEmail
    {
        private DatabaseHelper db;
        public int soluong;
        public List<Email> listemail;
        public ListEmail()
        {
            listemail = new List<Email>();
            db = new DatabaseHelper();
            string query = @"SELECT * FROM Email";
            DataTable email = db.ExecuteQuery(query);

            foreach (DataRow row in email.Rows)
            {
                string toField = Convert.ToString(row["ToAdd"]) ?? "";
                string[] toArray = string.IsNullOrWhiteSpace(toField) ? new string[0] : toField.Split(',');

                Email e = new Email(
                    Convert.ToString(row["Email"]) ?? "",
                    Convert.ToString(row["FolderName"]) ?? "",
                    Convert.ToString(row["SubjectEmail"]) ?? "",
                    Convert.ToString(row["FromAdd"]) ?? "",
                    toArray,
                    Convert.ToDateTime(row["DateSent"]),
                    Convert.ToDateTime(row["DateReceived"]),
                    Convert.ToString(row["BodyText"]) ?? "",
                    Convert.ToBoolean(row["IsRead"] ?? false),
                    Convert.ToInt32(row["ID"])
                );
                Console.WriteLine("Add");
                listemail.Add(e);
            }
        }
        public void DeleteEmail()
        {
            string query = @"Delete From Email where IsFlag=1";
            db.ExecuteNonQuery(query);
            foreach(Email e in listemail)
            {
                if (e.GetIsFlag())
                {
                    listemail.Remove(e);
                    query = @"Update Folder set TotalMail=TotalMail-1 where FolderName=@FolderName and Email=@Email";
                    SqlParameter[] Update = new SqlParameter[]
                    {
                        new SqlParameter("@FolderName",e.GetFolderIName()),
                        new SqlParameter("@Email",e.GetEmail())
                    };
                    db.ExecuteNonQuery(query, Update);
                }
            }
        }
        public void Print()
        {
            foreach (Email e in listemail)
            {
                e.PrintE();
            }
        }

    }
}
