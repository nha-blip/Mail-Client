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

                if (dt.Columns.Contains("UID") && row["UID"] != DBNull.Value)
                {
                    e.UID = Convert.ToInt64(row["UID"]);
                }

                if (dt.Columns.Contains("ThreadId") && row["ThreadId"] != DBNull.Value)
                {
                    e.ThreadId = Convert.ToInt64(row["ThreadId"]);
                }

                listemail.Add(e);
            }
        }

        public void LoadMoreOldEmails(int accountID, string currentFolderName)
        {
            DateTime oldestDate;

            // Lọc ra các thư thuộc folder hiện tại đang có trong RAM
            // Lưu ý: Cần xử lý trường hợp "AllMail" (lấy tất cả)
            IEnumerable<Email> emailsInFolder;

            if (currentFolderName == "AllMail")
            {
                emailsInFolder = listemail;
            }
            else
            {
                // So sánh không phân biệt hoa thường để tránh lỗi "INBOX" vs "Inbox"
                emailsInFolder = listemail.Where(e => e.FolderName.Equals(currentFolderName, StringComparison.OrdinalIgnoreCase));
            }

            // Tìm ngày cũ nhất trong nhóm đó
            if (emailsInFolder.Any())
            {
                oldestDate = emailsInFolder.Min(e => e.DateSent);
            }
            else
            {
                // Nếu folder này chưa có thư nào trong RAM (trống trơn), lấy mốc hiện tại để tải hết
                oldestDate = DateTime.Now;
            }

            // Chuẩn bị câu Query
            string query;
            SqlParameter[] parameters;

            if (currentFolderName == "AllMail")
            {
                // Query cho AllMail (Không lọc folder)
                query = @"SELECT * FROM Email E
                          JOIN Folder F ON F.FolderID = E.FolderID
                          JOIN Account A ON A.AccountID = E.AccountID
                          WHERE E.AccountID = @AccID 
                          AND E.DateSent < @OldestDate
                          ORDER BY E.DateSent DESC";

                parameters = new SqlParameter[] {
                    new SqlParameter("@AccID", accountID),
                    new SqlParameter("@OldestDate", oldestDate)
                };
            }
            else
            {
                // Query cho folder cụ thể (Thêm điều kiện FolderName)
                query = @"SELECT * FROM Email E
                          JOIN Folder F ON F.FolderID = E.FolderID
                          JOIN Account A ON A.AccountID = E.AccountID
                          WHERE E.AccountID = @AccID 
                          AND F.FolderName = @FolderName -- Lọc đúng folder đang xem
                          AND E.DateSent < @OldestDate
                          ORDER BY E.DateSent DESC";

                parameters = new SqlParameter[] {
                    new SqlParameter("@AccID", accountID),
                    new SqlParameter("@OldestDate", oldestDate),
                    new SqlParameter("@FolderName", currentFolderName)
                };
            }

            DatabaseHelper db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);

            // Thêm thư mới vào danh sách
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

                if (dt.Columns.Contains("UID") && row["UID"] != DBNull.Value) e.UID = Convert.ToInt64(row["UID"]);
                if (dt.Columns.Contains("ThreadId") && row["ThreadId"] != DBNull.Value) e.ThreadId = Convert.ToInt64(row["ThreadId"]);

                // Thêm vào ObservableCollection -> UI sẽ nhận được thông báo
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

        public List<MailClient.Email> GetConversation(long threadId, long folderId)
        {
            if (threadId == 0) return new List<MailClient.Email>();

            string query = @"SELECT * FROM Email WHERE ThreadId = @ThreadId AND FolderId = @FolderId ORDER BY DateSent ASC";
            SqlParameter[] param = { 
                new SqlParameter("@ThreadId", threadId),
                new SqlParameter("@FolderId", folderId)
            };

            DatabaseHelper db = new DatabaseHelper();

            DataTable dt = db.ExecuteQuery(query, param);
            var conversationList = new List<MailClient.Email>();

            foreach (DataRow row in dt.Rows)
            {
                // Xử lý mảng To (ToAdd -> string[])
                string toField = Convert.ToString(row["ToAdd"]) ?? "";
                string[] toArray = string.IsNullOrWhiteSpace(toField) ? new string[0] : toField.Split(',');

                // Tạo đối tượng Email với đúng tên cột từ Database
                MailClient.Email e = new MailClient.Email
                {
                    emailID = Convert.ToInt32(row["ID"]),
                    AccountID = Convert.ToInt32(row["AccountID"]),
                    FolderID = Convert.ToInt32(row["FolderID"]),

                    // MAP CỘT SQL 'FromAdd' VÀO THUỘC TÍNH 'From'
                    From = Convert.ToString(row["FromAdd"]) ?? "",
                    FromUser = Convert.ToString(row["FromUser"]) ?? "",

                    // MAP MẢNG ĐÃ TÁCH VÀO THUỘC TÍNH 'To'
                    To = toArray,

                    Subject = Convert.ToString(row["SubjectEmail"]) ?? "(No Subject)",
                    BodyText = Convert.ToString(row["BodyText"]) ?? "",
                    DateSent = Convert.ToDateTime(row["DateSent"]),
                    IsRead = Convert.ToBoolean(row["IsRead"]),
                    UID = row["UID"] != DBNull.Value ? Convert.ToInt64(row["UID"]) : 0,
                    ThreadId = row["ThreadId"] != DBNull.Value ? Convert.ToInt64(row["ThreadId"]) : 0  
                };

                conversationList.Add(e);
            }

            return conversationList;
        }
        public void GetAllMail()
        {
            string query = @"SELECT * FROM Email E
                             JOIN Folder F ON F.FolderID = E.FolderID
                             JOIN Account A ON A.AccountID=E.AccountID
                             ORDER BY E.DateSent DESC";

            // 3. Gọi hàm ExecuteQuery kèm theo parameters
            DataTable dt = db.ExecuteQuery(query);
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

                if (dt.Columns.Contains("UID") && row["UID"] != DBNull.Value)
                {
                    e.UID = Convert.ToInt64(row["UID"]);
                }

                if (dt.Columns.Contains("ThreadId") && row["ThreadId"] != DBNull.Value)
                {
                    e.ThreadId = Convert.ToInt64(row["ThreadId"]);
                }

                listemail.Add(e);
            }
        }
    }
}