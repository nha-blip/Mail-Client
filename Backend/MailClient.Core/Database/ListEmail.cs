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
        private int soluong;
        private List<Email> listemail;
        public ListEmail()
        {
            listemail = new List<Email>();
            db = new DatabaseHelper();
            string query = @"SELECT * FROM Email  JOIN Folder ON Email.FolderID = Folder.ID";
            DataTable email = db.ExecuteQuery(query);

            foreach (DataRow row in email.Rows)
            {
                string toField = Convert.ToString(row["ToAdd"]);
                string[] toArray = string.IsNullOrWhiteSpace(toField) ? new string[0] : toField.Split(';');

                Email e = new Email(
                    Convert.ToString(row["SubjectEmail"]),
                    Convert.ToString(row["FromAdd"]),
                    toArray,
                    Convert.ToDateTime(row["DateSent"]),
                    Convert.ToDateTime(row["DateReceived"]),
                    Convert.ToString(row["BodyText"]),
                    Convert.ToBoolean(row["IsRead"] ?? false),
                    Convert.ToString(row["FolderName"] ?? "")
                );
                Console.WriteLine("Add");
                listemail.Add(e);
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
