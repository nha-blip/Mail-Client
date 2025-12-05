using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    public class Account
    {
        private DatabaseHelper db;
        public int AccountID { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Username { get; set; }
        public string TokenJson { get; set; }

        // --- [MỚI] Thêm thuộc tính lưu chuỗi JSON Token ---
        public string TokenJson { get; set; }

        public Account(string email, string username)
        {
            Email = email;
            Username = username;
            db = new DatabaseHelper();
        }

<<<<<<< HEAD
        // --- [SỬA] Constructor nạp cả Token từ DB lên ---
=======
        // Constructor nạp cả Token từ DB lên 
>>>>>>> 89def9711dbbf6499b2e60e3c2080e782c62a1b8
        public Account(int id)
        {
            AccountID = id;
            string query = @"select * from Account where AccountID=@AccountID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@AccountID",id)
            };
            db = new DatabaseHelper();
            DataTable dt = db.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
            {
                Username = Convert.ToString(dt.Rows[0]["AccountName"]) ?? "";
                Email = Convert.ToString(dt.Rows[0]["Email"]) ?? "";

<<<<<<< HEAD
                // [MỚI] Lấy chuỗi Token từ DB đổ vào biến
=======
                // Lấy chuỗi Token từ DB đổ vào biến
>>>>>>> 89def9711dbbf6499b2e60e3c2080e782c62a1b8
                TokenJson = Convert.ToString(dt.Rows[0]["TokenJson"]) ?? "";
            }
        }

<<<<<<< HEAD
        // --- [SỬA] Hàm thêm mới có lưu Token ---
=======
        // Hàm thêm mới có lưu Token 
>>>>>>> 89def9711dbbf6499b2e60e3c2080e782c62a1b8
        public void AddAccount()
        {
            // Logic cũ của bạn: Nếu chưa có thì Insert, nếu có rồi thì thôi.
            // Nhưng mình bổ sung thêm cột TokenJson vào câu lệnh Insert
            string query = @"
            IF NOT EXISTS (SELECT 1 FROM Account WHERE Email = @Email)
            BEGIN
                INSERT INTO Account
                (Email, EncryptedPassword, AccountName, TokenJson) -- Thêm cột TokenJson
                VALUES
                (@Email, 'GoogleAuth', @AccountName, @TokenJson);  -- Thêm tham số @TokenJson
                
                SELECT SCOPE_IDENTITY();
            END";

            SqlParameter[] parameters = new SqlParameter[] {
                new SqlParameter("@Email", Email),
                new SqlParameter("@AccountName", Username),
                // Nếu TokenJson null thì truyền DBNull.Value
                new SqlParameter("@TokenJson", (object)TokenJson ?? DBNull.Value)
            };

            DataTable dt = db.ExecuteQuery(query, parameters);

            if (dt.Rows.Count > 0 && dt.Rows[0][0] != DBNull.Value)
            {
                this.AccountID = Convert.ToInt32(dt.Rows[0][0]);

                // Tạo các folder mặc định cho user mới
                CreateDefaultFolders();
            }
        }

        // Tách hàm tạo folder ra cho gọn
        private void CreateDefaultFolders()
        {
            string query = @"INSERT INTO Folder (AccountID, FolderName)
                            VALUES (@AccID, 'Inbox'), (@AccID, 'Sent'), 
                                   (@AccID, 'Spam'), (@AccID, 'Trash'), (@AccID, 'Draft');";
            SqlParameter[] folder = new SqlParameter[] { new SqlParameter("@AccID", AccountID) };
            db.ExecuteNonQuery(query, folder);
        }

<<<<<<< HEAD
        // --- [MỚI] Hàm cập nhật riêng Token (Dùng khi Google Refresh Token) ---
=======
        // Hàm cập nhật riêng Token (Dùng khi Google Refresh Token)
>>>>>>> 89def9711dbbf6499b2e60e3c2080e782c62a1b8
        public void UpdateToken(string newTokenJson)
        {
            this.TokenJson = newTokenJson;
            if (this.AccountID > 0)
            {
                string query = "UPDATE Account SET TokenJson = @TokenJson WHERE AccountID = @AccountID";
                SqlParameter[] parameters = new SqlParameter[] {
                    new SqlParameter("@TokenJson", newTokenJson),
                    new SqlParameter("@AccountID", AccountID)
                };
                db.ExecuteNonQuery(query, parameters);
            }
        }

<<<<<<< HEAD
        // --- [SỬA] Hàm Login/Register nhận thêm Token mới nhất ---
=======
        // Hàm Login/Register nhận thêm Token mới nhất
>>>>>>> 89def9711dbbf6499b2e60e3c2080e782c62a1b8
        // Khi đăng nhập xong, ta có token mới, ta cần truyền vào đây để lưu/update
        public int LoginOrRegisterGoogle(string tokenJsonFromGoogle)
        {
            this.TokenJson = tokenJsonFromGoogle; // Gán vào biến của class

            string checkQuery = "SELECT AccountID, TokenJson FROM Account WHERE Email = @Email";
            SqlParameter[] checkParams = { new SqlParameter("@Email", Email) };

            DataTable dt = db.ExecuteQuery(checkQuery, checkParams);

            if (dt.Rows.Count > 0)
            {
                // Trường hợp 1: Tài khoản ĐÃ TỒN TẠI
                int existingID = Convert.ToInt32(dt.Rows[0]["AccountID"]);
                this.AccountID = existingID;

                // Quan trọng: Cập nhật lại Token mới nhất vào DB (vì token cũ trong DB có thể đã hết hạn)
                UpdateToken(tokenJsonFromGoogle);

                return existingID;
            }
            else
            {
                // Trường hợp 2: Tài khoản MỚI -> Thêm mới (kèm token)
                AddAccount();
                return AccountID;
            }
        }

<<<<<<< HEAD
        // ... Các hàm DeleteAccount, CheckAccount giữ nguyên ...
=======
>>>>>>> 89def9711dbbf6499b2e60e3c2080e782c62a1b8
        public int CheckAccount(string email, string password)
        {
            string query = @"SELECT * FROM Account WHERE Email=@email AND EncryptedPassword=@password";
            SqlParameter[] parameters = { new SqlParameter("@email", email), new SqlParameter("@password", password) };
            DataTable dt = db.ExecuteQuery(query, parameters);
            return dt.Rows.Count > 0 ? 1 : throw new Exception("Account not found");
        }

        public void DeleteAccount()
        {
            string query = @"Delete from Account where AccountID=@AccountID";
            SqlParameter[] parameter = { new SqlParameter("@AccountID", AccountID) };
            db.ExecuteNonQuery(query, parameter);
        }
    }
}