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
    public class ListAccount
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
                                        Convert.ToString(row["AccountName"]) ?? ""
                                        );
                a.AccountID=Convert.ToInt32(row["AccountID"]);
                listAccount.Add(a);
            }
            soluong = dt.Rows.Count;
        }
        public void AddAccount(Account a)
        {
            a.AddAccount();
            listAccount.Add(a);
            Folder[] f = new Folder[]{
                new Folder(a.AccountID,"Inbox",0),
                new Folder(a.AccountID,"Sent",0),
                new Folder(a.AccountID,"Draft",0),
                new Folder(a.AccountID,"Spam",0),
                new Folder(a.AccountID,"All mail",0)
            };
            foreach (Folder f2 in f)
            {
                f2.AddFolder();
            }
        }
        public void RemoveAccount(Account a)
        {
            a.DeleteAccount();
            listAccount.Remove(a);
        }
    }
}
