using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    internal class Email
    {
        private DatabaseHelper db;
        private int emailID;
        private string email;
        private string FolderName;
        private string Subject;
        private string From;
        private string[] To;
        private DateTime DateSent;
        private DateTime DateReceived;
        private string BodyText;
        private bool IsRead;
        private bool IsFlag;
        // Tạo email
        public Email(string email,string FolderName,string subject, string from, string[] to, DateTime dateSent, DateTime dateReceived, string bodyText, bool isRead,int ID=0)
        {            
            this.db = new DatabaseHelper();
            this.email = email;
            this.FolderName = FolderName;
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
        public string GetFolderIName() { return FolderName; }
        public void SetFolderName(string FolderName) { this.FolderName = FolderName; }

        public string GetEmail() { return email; }
        public void SetEmail(string Email) { this.email = Email; }
        public string GetSubject() { return Subject; }
        public void SetSubject(string subject) { this.Subject = subject; }

        public string GetFrom() { return From; }
        public void SetFrom(string from) { this.From = from; }

        public string[] GetTo() { return To; }
        public void SetTo(string[] to) { this.To = to; }

        public DateTime GetDateSent() { return DateSent; }
        public void SetDateSent(DateTime dateSent) { this.DateSent = dateSent; }

        public DateTime GetDateReceived() { return DateReceived; }
        public void SetDateReceived(DateTime dateReceived) { this.DateReceived = dateReceived; }

        public string GetBodyText() { return BodyText; }
        public void SetBodyText(string bodyText) { this.BodyText = bodyText; }

        public bool GetIsRead() { return IsRead; }
        public void SetIsRead(bool isRead) { this.IsRead = isRead; }
        public bool GetIsFlag() { return IsFlag; }
        public void SetIsFlag(bool isFlag) { this.IsFlag = isFlag; }
        public void AddEmail() // Thêm thư vào database
        {
            string toString = string.Join(",", To.Select(e => e.Trim()));
            string query = @"Insert into Email(Email,FolderName, SubjectEmail, FromAdd, ToAdd,DateSent, DateReceived, BodyText, IsRead)
                            Values(@Email,@FolderName, @SubjectEmail, @FromAdd, @ToAdd, @DateSent, @DateReceived, @BodyText, @IsRead);
                            SELECT SCOPE_IDENTITY();";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Email",email),
                new SqlParameter("@FolderName",FolderName),
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
            query = @"UPDATE Folder SET TotalMail=TotalMail+1 Where FolderName=@FolderName and Email=@Email";
            SqlParameter[] Updateparameters = new SqlParameter[]
            {
                new SqlParameter("@FolderName",FolderName),
                new SqlParameter("@Email",email)
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
        public void PrintE()
        {
            Console.WriteLine("EmailID: " + emailID);
            Console.WriteLine("Email: " + email);
            Console.WriteLine("FolderName: " + FolderName);
            Console.WriteLine("Subject: " + Subject);
            Console.WriteLine("From: " + From);
            Console.WriteLine("To: " + string.Join(", ", To)); // Nối mảng To thành chuỗi
            Console.WriteLine("Date Sent: " + DateSent);
            Console.WriteLine("Date Received: " + DateReceived);
            Console.WriteLine("Body Text: " + BodyText);
            Console.WriteLine("Is Read: " + (IsRead ? "Yes" : "No"));
            Console.WriteLine(new string('-', 50));
        }
    }
}
