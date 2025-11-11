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
        private int FolderID;
        private string Subject;
        private string From;
        private string[] To;
        private DateTime DateSent;
        private DateTime DateReceived;
        private string BodyText;
        private bool IsRead;
        public Email(string subject, string from, string[] to, DateTime dateSent, DateTime dateReceived, string bodyText, bool isRead, string folderName)
        {
            this.db = new DatabaseHelper();
            string query = @"Select fd.ID
                            From Account ac, Folder fd
                            Where ac.ID=fd.AccountID and fd.FolderName=@folderName";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@folderName",folderName)
            };
            DataTable dt=db.ExecuteQuery(query, parameters);
            this.FolderID = Convert.ToInt32(dt.Rows[0]["ID"]);
            this.Subject = subject;
            this.From = from;
            this.To = to;
            this.DateSent = dateSent;
            this.DateReceived = dateReceived;
            this.BodyText = bodyText;
            this.IsRead = isRead;
        }
        public int GetFolderID() { return FolderID; }
        public void SetFolderID(int folderID) { this.FolderID = folderID; }

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
        public void AddEmail()
        {
            string toString = string.Join(",", To.Select(e => e.Trim()));
            string query = @"Insert into Email(FolderId, SubjectEmail, FromAdd, ToAdd,DateSent, DateReceived, BodyText, IsRead)
                            Values(@FolderId, @SubjectEmail, @FromAdd, @ToAdd, @DateSent, @DateReceived, @BodyText, @IsRead)";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@FolderId",FolderID),
                new SqlParameter("@SubjectEmail",Subject),
                new SqlParameter("@FromAdd",From),
                new SqlParameter("@ToAdd",toString),
                new SqlParameter("@DateSent",DateSent),
                new SqlParameter("@DateReceived",DateReceived),
                new SqlParameter("@BodyText",BodyText),
                new SqlParameter("@IsRead",IsRead)
            };
            db.ExecuteNonQuery(query, parameters);
            query = @"UPDATE Folder SET TotalMail=TotalMail+1 Where ID=@FolderID";
            SqlParameter[] Updateparameters = new SqlParameter[]
            {
                new SqlParameter("@FolderID",FolderID)
            };
            db.ExecuteNonQuery(query, Updateparameters);
        }
        public void DeleteEmail()
        {
            string query = @"Delete from Email where Isflag=1";
            db.ExecuteNonQuery(query);
        }
        public void MarkAsRead(int emailId, bool isRead)
        {
            string query = @"Update Email set IsRead=@IsRead where ID=@EmailId";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@IsRead",isRead),
                new SqlParameter("@EmailId",emailId)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public void MarkAsFlag(int emailId, bool isFlag)
        {
            string query = @"Update Email set IsFlag=@IsFlag where ID=@EmailId";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@IsFlag",isFlag),
                new SqlParameter("@EmailId",emailId)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public DataTable GetAllEmailByFolderID(int FolderID)
        {
            string query = "Select * from Email\n Where Email.FolderID=@FolderID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FolderID",FolderID)
            };
            return db.ExecuteQuery(query,parameters);
        }
        public void PrintE()
        {
            Console.WriteLine("FolderID: " + FolderID);
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
