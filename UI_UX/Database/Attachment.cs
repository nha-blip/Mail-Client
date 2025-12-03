using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;
using MimeKit;

namespace MailClient
{
    public class Attachment
    {
        private DatabaseHelper db;
        public int ID { get; set; }
        public int EmailID { get; set; }
        public string Name { get; set; }
        public string TypeMine { get; set; }
        public int Size { get; set; }
        public int IsDownload { get; set; }
        public MimePart OriginalMimePart { get; set; }
        public Attachment()
        {
            this.db = new DatabaseHelper();
        }
        public Attachment(int emailID, string name, string typeMine, int size, int isDownload, int ID = 0)
        {
            this.db = new DatabaseHelper();
            this.EmailID = emailID;
            this.Name = name;
            this.TypeMine = typeMine;
            this.Size = size;
            this.IsDownload = isDownload;
            this.ID = ID;
        }
        public void AddAttachment()
        {
            string query = @"Insert into Attachment(EmailID,NameFile,TypeMime,Size,Downloaded)
                          Values(@EmailID,@NameFile,@TypeMime,@Size,@Downloaded)
                           SELECT SCOPE_IDENTITY();";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@EmailID",EmailID),
                new SqlParameter("@NameFile",Name),
                new SqlParameter("@TypeMime",TypeMine),
                new SqlParameter("@Size",Size),
                new SqlParameter("@Downloaded",IsDownload)
            };
            DataTable dt = db.ExecuteQuery(query, parameters);
            if (dt.Rows.Count > 0)
            {
                this.EmailID = Convert.ToInt32(dt.Rows[0][0]);
            }
        }
        public void MarkIsFlag(int attachID)
        {
            string query = @"Update Attachment set IsFlag=1 where ID=@attachID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@attachID",attachID)
            };
            db.ExecuteNonQuery(query, parameters);
        }
        public void DeleteAttachment()
        {
            string query = @"Delete from Attachment where IsFlag=1";
            db.ExecuteNonQuery(query);
        }

        public static List<Attachment> GetListAttachments(int emailID)
        {
            List<Attachment> list = new List<Attachment>();
            DatabaseHelper db = new DatabaseHelper();

            string query = "SELECT * FROM Attachment WHERE EmailID = @EmailID";
            SqlParameter[] parameters = new SqlParameter[]
            {
                new SqlParameter("@EmailID", emailID)
            };

            DataTable dt = db.ExecuteQuery(query, parameters);

            foreach (DataRow row in dt.Rows)
            {
                // Mapping dữ liệu từ SQL sang Object Attachment
                Attachment att = new Attachment();
                att.ID = Convert.ToInt32(row["ID"]);
                att.EmailID = Convert.ToInt32(row["EmailID"]);

                // Kiểm tra null để tránh lỗi
                att.Name = row["NameFile"] != DBNull.Value ? row["NameFile"].ToString() : "Unknown";
                att.TypeMine = row["TypeMime"] != DBNull.Value ? row["TypeMime"].ToString() : "";

                // Size trong DB là bigint (long), ép sang int cho class Attachment
                att.Size = row["Size"] != DBNull.Value ? Convert.ToInt32(row["Size"]) : 0;

                // Downloaded là bit (bool), cần convert sang int (0 hoặc 1)
                att.IsDownload = (row["Downloaded"] != DBNull.Value && (bool)row["Downloaded"]) ? 1 : 0;

                list.Add(att);
            }

            return list;
        }
    }
}
