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
        public ListEmail(int accountID)
        {
            listemail = new List<Email>();
            db = new DatabaseHelper();
            // 1. Sửa câu truy vấn: Dùng @AccID làm tham số
            string query = @"SELECT * FROM Email E
                             JOIN Folder F ON F.FolderID = E.FolderID
                             JOIN Account A ON A.AccountID=E.AccountID
                             WHERE E.AccountID = @AccID ORDER BY E.DateSent DESC";

            // 2. Tạo tham số và truyền giá trị thật từ App.CurrentAccountID vào
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@AccID", accountID)
            };

            // 3. Gọi hàm ExecuteQuery kèm theo parameters
            DataTable dt = db.ExecuteQuery(query, parameters);
            foreach (DataRow row in dt.Rows)
            {
                string toField = Convert.ToString(row["ToAdd"]) ?? "";
                string[] toArray = string.IsNullOrWhiteSpace(toField) ? new string[0] : toField.Split(',');

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