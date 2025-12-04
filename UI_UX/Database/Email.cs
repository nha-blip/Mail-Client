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
        public string FolderName { get; set; }
        public string AccountName { get; set; }
        public string Subject { get; set; }
        public string From { get; set; }
        public string[] To { get; set; }
        public string FromUser { get; set; }
        public DateTime DateSent { get; set; }
        public DateTime DateReceived { get; set; }
        public string BodyText { get; set; }
        public bool IsRead { get; set; }
        public bool IsFlag { get; set; }

        public List<string> AttachmentPaths { get; set; } = new List<string>();
        public string SenderName
        {
            get
            {
                if (DateSent.Date == DateTime.Today)
                {
                    // Nếu là hôm nay → chỉ hiển thị giờ:phút
                    return DateSent.ToString("HH:mm");
                }
                else
                {
                    // Nếu không phải hôm nay → hiển thị dd/MM/yyyy
                    return DateSent.ToString("dd/MM/yyyy");
                }
            }
        }
        // Tạo email
        public Email()
        {
            this.db = new DatabaseHelper();
            this.To = new string[] { }; // Khởi tạo mảng rỗng để tránh lỗi null
        }
        public Email(string folderName, string fromuser, string accountName, string subject, string from, string[] to, DateTime dateSent, DateTime dateReceived, string bodyText, bool isRead, int ID = 0)
        {
            this.db = new DatabaseHelper();
            this.FolderName = folderName;
            this.Subject = subject;
            this.From = from;
            this.To = to;
            this.FromUser = fromuser;
            this.DateSent = dateSent;
            this.DateReceived = dateReceived;
            this.BodyText = bodyText;
            this.IsRead = isRead;
            this.emailID = ID;
            this.IsFlag = false;
            this.FolderID = FolderID;
            this.AccountName = accountName;
            string query = @"Select FolderID from Folder where FolderName=@FolderName";
            SqlParameter[] folder = new SqlParameter[]
            {
                new SqlParameter("@FolderName",folderName)
            };
            DataTable f = db.ExecuteQuery(query, folder);
            if (f.Rows.Count > 0)
            {
                this.FolderID = Convert.ToInt32(f.Rows[0][0]);
            }
            f.Dispose();
            query = @"Select AccountID from Account where AccountName=@AccountName";
            SqlParameter[] account = new SqlParameter[]
            {
                new SqlParameter("@AccountName",accountName)
            };
            DataTable a = db.ExecuteQuery(query, account);
            if (a.Rows.Count > 0)
            {
                this.AccountID = Convert.ToInt32(a.Rows[0][0]);
            }
            a.Dispose();
        }

        public void AddEmail() // Thêm thư vào database
        {
            string toString = string.Join(",", To.Select(e => e.Trim()));
            string checkQuery = @"SELECT COUNT(*) FROM Email 
                          WHERE BodyText=@BodyText";

            SqlParameter[] checkParams = new SqlParameter[]
            {
                new SqlParameter("@BodyText",BodyText)
            };
            DataTable data = db.ExecuteQuery(checkQuery, checkParams);
            if (data.Rows.Count > 0 && Convert.ToInt32(data.Rows[0][0]) > 0) return;


            string query = @"Insert into Email(AccountID,FolderID, SubjectEmail,FromUser, FromAdd, ToAdd,DateSent, DateReceived, BodyText, IsRead)
                            Values(@AccountID,@FolderID, @SubjectEmail, @FromUser, @FromAdd, @ToAdd, @DateSent, @DateReceived, @BodyText, @IsRead);
                            SELECT SCOPE_IDENTITY();";
            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@AccountID",AccountID),
                new SqlParameter("@FolderID",FolderID),
                new SqlParameter("@SubjectEmail",Subject),
                new SqlParameter("@FromUser",FromUser),
                new SqlParameter("@FromAdd",From),
                new SqlParameter("@ToAdd",toString),
                new SqlParameter("@DateSent",DateSent),
                new SqlParameter("@DateReceived",DateReceived),
                new SqlParameter("@BodyText",BodyText),
                new SqlParameter("@IsRead",IsRead)
            };

            DataTable dt = db.ExecuteQuery(query, parameters);
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
            db.ExecuteNonQuery(query, parameters);
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
        // Thuộc tính này không lưu vào bảng Email, chỉ dùng để vận chuyển dữ liệu từ Parser
        public List<Attachment> TempAttachments { get; set; } = new List<Attachment>();

        public void UpdateFolderEmail(string foldername)
        {
            // Lấy ID của folder Trash
            string query = @"Select FolderID from Folder where FolderName=@FolderName and AccountID = @AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@FolderName",foldername),
                new SqlParameter("@AccountID",AccountID)
            };
            DataTable dt = db.ExecuteQuery(query, parameters);
            int newfolderID = Convert.ToInt32(dt.Rows[0][0]);

            //Giảm TotalMail của Source
            query = @"UPDATE Folder 
                        SET TotalMail = TotalMail - 1 
                        WHERE FolderID = @SourceFolderID";
            SqlParameter[] source = new SqlParameter[]
            {
                new SqlParameter("@SourceFolderID",FolderID)
            };
            db.ExecuteNonQuery(query, source);
            FolderID = newfolderID;

            // Chuyển folderID mail hiện tại sang Trash
            query = @"Update Email Set FolderID=@FolderID where ID=@ID";
            SqlParameter[] folder = new SqlParameter[]
            {
                new SqlParameter("@FolderID",newfolderID),
                new SqlParameter("@ID",emailID)
            };
            db.ExecuteNonQuery(query, folder);

            // Tăng TotalMail của Trash
            query = @"UPDATE Folder 
                        SET TotalMail = TotalMail + 1 
                        WHERE FolderID = @SourceFolderID";
            SqlParameter[] trash = new SqlParameter[]
            {
                new SqlParameter("@SourceFolderID",FolderID)
            };
            db.ExecuteNonQuery(query, trash);

        }
    }
}