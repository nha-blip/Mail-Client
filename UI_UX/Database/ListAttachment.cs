using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;

namespace MailClient
{
    public class ListAttachment
    {
        private DatabaseHelper db;
        public List<Attachment> listAttachment;
        public int soluong;
        public ListAttachment()
        {
            db=new DatabaseHelper();
            listAttachment=new List<Attachment>();
            string query = @"Select * from Attachment";
            DataTable dt = db.ExecuteQuery(query);
            foreach (DataRow dr in dt.Rows)
            {
                Attachment a = new Attachment(                                              
                                              Convert.ToInt32(dr["EmailID"]),
                                              Convert.ToString(dr["NameFile"]) ?? "",
                                              Convert.ToString(dr["TypeMime"]) ?? "",
                                              Convert.ToInt32(dr["Size"]),
                                              Convert.ToInt32(dr["Downloaded"]),
                                              Convert.ToInt32(dr["ID"])
                                              );
                listAttachment.Add(a);
            }
            soluong = dt.Rows.Count;
        }
    }
}
