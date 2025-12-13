using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;

namespace MailClient
{
    public class ListEmail
    {
        private DatabaseHelper db;
        public int soluong;
        public ObservableCollection<Email>  listemail;
        public DateTime _latestDateSent;
        public ListEmail(int accountID)
        {
            listemail = new ObservableCollection<Email>();
            db = new DatabaseHelper();
            _latestDateSent = new DateTime(1753, 1, 1);
            LoadEmail(accountID);
            
        }
        public void Refresh(int accountID)
        {
            LoadEmail(accountID);
        }
        public void LoadEmail(int accountID)
        {
            string query = @"SELECT * FROM Email E
                             JOIN Folder F ON F.FolderID = E.FolderID
                             JOIN Account A ON A.AccountID=E.AccountID
                             WHERE E.AccountID = @AccID AND E.DateSent > @LastDate 
                             ORDER BY E.DateSent DESC";

            
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@AccID", accountID),
                new SqlParameter("@LastDate", _latestDateSent)
            };

            // 3. Gọi hàm ExecuteQuery kèm theo parameters
            DataTable dt = db.ExecuteQuery(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                string toField = Convert.ToString(row["ToAdd"]) ?? "";
                string[] toArray = string.IsNullOrWhiteSpace(toField) ? new string[0] : toField.Split(',');

                DateTime currentSentDate = Convert.ToDateTime(row["DateSent"]);
                if (currentSentDate > _latestDateSent)
                {
                    _latestDateSent = currentSentDate;
                }

                Email e = new Email(
                    Convert.ToString(row["FolderName"]) ?? "",
                    Convert.ToString(row["FromUser"]) ?? "",
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
            foreach (Email e in listemail)
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