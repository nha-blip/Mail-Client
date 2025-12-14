using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Data;
using System.Globalization;
using System.IO;
using System.Reflection;
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

                    new SqlParameter("@Email", email),
                    new SqlParameter("@Name", displayName)
                };
                DataTable dtNew = ExecuteQuery(insertQuery, insertParams);
                return Convert.ToInt32(dtNew.Rows[0][0]);
            }
        }

        /// <summary>
        /// Thực thi câu lệnh Select và tự động map dữ liệu vào List Class T
        /// </summary>
        public List<T> ExecuteQueryToList<T>(string query, SqlParameter[]? parameters = null) where T : new()
        {
            // Lấy dữ liệu dạng DataTable bằng hàm có sẵn
            DataTable dt = ExecuteQuery(query, parameters);
            List<T> list = new List<T>();

            // Duyệt từng dòng và chuyển đổi sang Object T
            foreach (DataRow row in dt.Rows)
            {
                T item = new T();
                // Dùng Reflection để quét các thuộc tính của Class T
                foreach (PropertyInfo prop in typeof(T).GetProperties())
                {
                    // Nếu cột trong SQL có tên trùng với Property trong Class và không null
                    if (dt.Columns.Contains(prop.Name) && row[prop.Name] != DBNull.Value)
                    {
                        try
                        {
                            // Gán giá trị từ SQL vào Object C#
                            prop.SetValue(item, row[prop.Name]);
                        }
                        catch
                        {
                            // Bỏ qua nếu lỗi sai kiểu dữ liệu (vd: SQL là int nhưng C# là long)
                            // Bạn có thể thêm logic Convert.ChangeType ở đây nếu cần thiết
                        }
                    }
                }
                list.Add(item);
            }
            return list;
        }
    }
}