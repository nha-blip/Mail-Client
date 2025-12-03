
using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    public class DatabaseHelper
    {
        private readonly IConfigurationRoot config;
        private readonly string connectionString;
        public DatabaseHelper()
        {
            config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false)
                .Build();

            connectionString = config.GetConnectionString("MailDB")
                ?? throw new InvalidOperationException("Missing connection string 'MailDB'");
        }
        public DataTable ExecuteQuery(string query, SqlParameter[]? parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                SqlCommand cmd = new SqlCommand(query, conn);
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                DataTable dt = new DataTable();
                SqlDataAdapter adt = new SqlDataAdapter(cmd);
                adt.Fill(dt);
                return dt;
            }
        }
        public int ExecuteNonQuery(string query, SqlParameter[]? parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                SqlCommand cmd = new SqlCommand(query, conn);
                if (parameters != null)
                {
                    cmd.Parameters.AddRange(parameters);
                }
                return cmd.ExecuteNonQuery();
            }
        }

        // Thực thi câu lệnh trả về 1 giá trị int (ví dụ SCOPE_IDENTITY())
        public int ExecuteScalarInt(string query, SqlParameter[]? parameters = null)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    if (parameters != null)
                    {
                        cmd.Parameters.AddRange(parameters);
                    }
                    object? result = cmd.ExecuteScalar();
                    if (result == null || result == DBNull.Value) return 0;
                    try
                    {
                        return Convert.ToInt32(result);
                    }
                    catch
                    {
                        return 0;
                    }
                }
            }
        }

        // Lấy FolderID theo AccountID + FolderName, nếu chưa có thì tạo và trả về ID mới
        public int GetOrCreateFolderId(int accountId, string folderName)
        {
            string selectQ = "SELECT FolderID FROM Folder WHERE AccountID = @AccountID AND FolderName = @FolderName";
            SqlParameter[] selParams = new SqlParameter[]
            {
                new SqlParameter("@AccountID", accountId),
                new SqlParameter("@FolderName", folderName)
            };
            DataTable dt = ExecuteQuery(selectQ, selParams);
            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["FolderID"]);
            }

            // Nếu chưa có folder, tạo mới và trả về SCOPE_IDENTITY()
            string insertQ = @"INSERT INTO Folder(AccountID, FolderName, TotalMail) VALUES(@AccountID, @FolderName, 0); SELECT SCOPE_IDENTITY();";
            SqlParameter[] insParams = new SqlParameter[]
            {
                new SqlParameter("@AccountID", accountId),
                new SqlParameter("@FolderName", folderName)
            };
            int newId = ExecuteScalarInt(insertQ, insParams);
            if (newId <= 0)
            {
                throw new InvalidOperationException("Không thể tạo Folder mới trong database.");
            }
            return newId;
        }

        public int LoginOrRegisterGoogle(string email, string displayName)
        {
            string checkQuery = "SELECT AccountID FROM Account WHERE Email = @Email";

            SqlParameter[] checkParams = { new SqlParameter("@Email", email) };

            DataTable dt = ExecuteQuery(checkQuery, checkParams);

            if (dt.Rows.Count > 0)
            {
                return Convert.ToInt32(dt.Rows[0]["AccountID"]);
            }
            else
            {
                // Sửa ở đây nữa:
                string insertQuery = @"INSERT INTO Account(Email, EncryptedPassword, AccountName) 
                               VALUES(@Email, 'GoogleAuth', @Name);
                               SELECT SCOPE_IDENTITY();";

                SqlParameter[] insertParams = {
<<<<<<< HEAD
                    new SqlParameter("@Email", email),
                    new SqlParameter("@Name", displayName)
                };
=======
            new SqlParameter("@Email", email),
            new SqlParameter("@Name", displayName)
        };

>>>>>>> 1896dab998a8a33d2bb403ecf0e83fe2a21a921c
                DataTable dtNew = ExecuteQuery(insertQuery, insertParams);
                return Convert.ToInt32(dtNew.Rows[0][0]);
            }
        }

    }
}