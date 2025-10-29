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
        DatabaseHelper db;
        public Email(DatabaseHelper data)
        {
            db = data;
        }
        public void AddEmail(int FolderId, string Subject, string From, string To, string CC, string BCC, string Body, DateTime sent, DateTime ReceivedAt, bool IsRead, bool IsFlag)
        {
            string query = @"Insert into Email(FolderId, SubjectEmail, FromAdd, ToAdd, ccAdd,bcc,DateSent, DateReceived, BodyText, IsRead,IsFlag)
                            Values(@FolderId, @SubjectEmail, @FromAdd, @ToAdd, @ccAdd,@bcc, @DateSent, @DateReceived, @BodyText, @IsRead,@IsFlag)";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@FolderId",FolderId),
                new SqlParameter("@SubjectEmail",Subject),
                new SqlParameter("@FromAdd",From),
                new SqlParameter("@ToAdd",To),
                new SqlParameter("@ccAdd",CC),
                new SqlParameter("@bcc",BCC),
                new SqlParameter("@DateSent",sent),
                new SqlParameter("@DateReceived",ReceivedAt),
                new SqlParameter("@BodyText",Body),
                new SqlParameter("@IsRead",IsRead),
                new SqlParameter("@IsFlag",IsFlag)
            };
            db.ExecuteNonQuery(query, parameters);
            query = @"UPDATE Folder\r\nSET TotalMail = (\r\n    SELECT COUNT(*) \r\n    FROM Email \r\nWHERE Email.FolderId=@FolderID    WHERE Folder.ID=@FolderID\r\n);";
            db.ExecuteNonQuery(query, parameters);
        }
        public void DeleteEmail()
        {
            string query = @"Delete from Email where Isflag=true";
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
    }
}
