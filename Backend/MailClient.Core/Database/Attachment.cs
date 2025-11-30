using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace MailClient
{
    internal class Attachment
    {
        private DatabaseHelper db;
        private int ID;
        private int EmailID;
        private string Name;
        private string TypeMine;
        private int Size;
        private int IsDownload;
        public Attachment(int emailID, string name, string typeMine, int size, int isDownload,int ID=0)
        {
            this.db = new DatabaseHelper();
            this.EmailID = emailID;
            this.Name = name;
            this.TypeMine = typeMine;
            this.Size = size;
            this.IsDownload = isDownload;
            this.ID = ID;
        }
        public int GetID() { return ID; }
        public void SetID(int ID) { this.ID = ID; }
        public int GetEmailID() { return EmailID; }
        public void SetEmailID(int emailID) { this.EmailID = emailID; }

        public string GetName() { return Name; }
        public void SetName(string name) { this.Name = name; }

        public string GetTypeMine() { return TypeMine; }
        public void SetTypeMine(string typeMine) { this.TypeMine = typeMine; }

        public int GetSize() { return Size; }
        public void SetSize(int size) { this.Size = size; }

        public int GetIsDownload() { return IsDownload; }
        public void SetIsDownload(int isDownload) { this.IsDownload = isDownload; }
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
            DataTable dt=db.ExecuteQuery(query, parameters);
            if(dt.Rows.Count > 0)
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
    }
}
