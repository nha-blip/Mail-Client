using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    public class Email
    {
        private DatabaseHelper db;
        public int emailID { get; set; }      
        public int AccountID { get; set; }
        public int FolderID { get; set; }

        public string Subject { get; set; }
        public string From { get; set; }
        public string[] To { get; set; }

        public DateTime DateSent { get; set; }
        public DateTime DateReceived { get; set; }
        public string BodyText { get; set; }
        public bool IsRead { get; set; }
        public bool IsFlag { get; set; }
        // Tạo email
        public Email(int accountID,int folderID,string subject, string from, string[] to, DateTime dateSent, DateTime dateReceived, string bodyText, bool isRead,int ID=0)
        {            
            this.db = new DatabaseHelper();
            this.AccountID = accountID;
            this.FolderID=accountID;
            this.Subject = subject;
            this.From = from;
            this.To = to;
            this.DateSent = dateSent;
            this.DateReceived = dateReceived;
            this.BodyText = bodyText;
            this.IsRead = isRead;
            this.emailID = ID;
            this.IsFlag = false;
        }
        public void AddEmail() // Thêm thư vào database
        {
            string toString = string.Join(",", To.Select(e => e.Trim()));
            string query = @"Insert into Email(AccountID,FolderID, SubjectEmail, FromAdd, ToAdd,DateSent, DateReceived, BodyText, IsRead)
                            Values(@AccountID,@FolderID, @SubjectEmail, @FromAdd, @ToAdd, @DateSent, @DateReceived, @BodyText, @IsRead);
                            SELECT SCOPE_IDENTITY();";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AccountID",AccountID),
                new SqlParameter("@FolderID",FolderID),
                new SqlParameter("@SubjectEmail",Subject),
                new SqlParameter("@FromAdd",From),
                new SqlParameter("@ToAdd",toString),
                new SqlParameter("@DateSent",DateSent),
                new SqlParameter("@DateReceived",DateReceived),
                new SqlParameter("@BodyText",BodyText),
                new SqlParameter("@IsRead",IsRead)
            };
            
            DataTable dt=db.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
            {
                this.emailID = Convert.ToInt32(dt.Rows[0][0]);
            }
            query = @"UPDATE Folder SET TotalMail=TotalMail+1 Where FolderID=@FolderID AND AccountID=@AccountID";
            SqlParameter[] Updateparameters = new SqlParameter[]
            {
                new SqlParameter("@FolderID",FolderID),
                new SqlParameter("@AccountID",AccountID)
            };
            db.ExecuteNonQuery(query, Updateparameters);
        }
        public void MarkAsRead() // Đánh dấu đã đọc
        {
            string query = @"Update Email set IsRead=1 where ID=@EmailID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@EmailID",emailID)
            };
            db.ExecuteNonQuery(query,parameters);
            IsRead = true;
        }
        public void MarkAsFlag() // Đánh dấu chọn
        {
            IsFlag = !IsFlag;
            string query = @"Update Email set IsFlag=@IsFlag where ID=@EmailId";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@IsFlag",IsFlag),
                new SqlParameter("@EmailId",emailID)
            };
            
            db.ExecuteNonQuery(query, parameters);
        }
        //public void PrintE()
        //{
        //    Console.WriteLine("EmailID: " + emailID);
        //    Console.WriteLine("Email: " + email);
        //    Console.WriteLine("FolderName: " + FolderName);
        //    Console.WriteLine("Subject: " + Subject);
        //    Console.WriteLine("From: " + From);
        //    Console.WriteLine("To: " + string.Join(", ", To)); // Nối mảng To thành chuỗi
        //    Console.WriteLine("Date Sent: " + DateSent);
        //    Console.WriteLine("Date Received: " + DateReceived);
        //    Console.WriteLine("Body Text: " + BodyText);
        //    Console.WriteLine("Is Read: " + (IsRead ? "Yes" : "No"));
        //    Console.WriteLine(new string('-', 50));
        //}
    }
}
