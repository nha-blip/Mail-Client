using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace MailClient
{
    internal class ListAccount
    {
        private DatabaseHelper db;
        public List<Account> listAccount;
        public int soluong;
        public ListAccount()
        {
            db=new DatabaseHelper();
            listAccount = new List<Account>();
            string query = @"Select * from Account";
            DataTable dt = db.ExecuteQuery(query);
            foreach (DataRow row in dt.Rows)
            {
                Account a = new Account(
                                        Convert.ToString(row["Email"]) ?? "",
                                        Convert.ToString(row["EncryptedPassword"]) ?? "",
                                        Convert.ToString(row["AccountName"]) ?? "",
                                        Convert.ToString(row["IncomingServer"]) ?? "",
                                        Convert.ToString(row["IncomingPort"]) ?? "",
                                        Convert.ToString(row["OutgoingServer"]) ?? "",
                                        Convert.ToString(row["OutgoingPort"]) ?? ""
                                        );
                
                listAccount.Add(a);
            }
            soluong = dt.Rows.Count;
        }
    }
}
