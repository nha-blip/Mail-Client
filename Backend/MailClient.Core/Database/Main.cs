using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text;

namespace MailClient
{
    internal class MainProgram
    {
        static void Main()
        {
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            // 1. Tài khoản Gmail
            Account acc1 = new Account(
                "user1@gmail.com",
                "password1",
                "User One",
                "imap.gmail.com",
                "993",
                "smtp.gmail.com",
                "587"
            );

            // 2. Tài khoản Yahoo
            Account acc2 = new Account(
                "user2@yahoo.com",
                "password2",
                "User Two",
                "imap.mail.yahoo.com",
                "993",
                "smtp.mail.yahoo.com",
                "465"
            );

            // 3. Tài khoản Outlook
            Account acc3 = new Account(
                "user3@outlook.com",
                "password3",
                "User Three",
                "imap-mail.outlook.com",
                "993",
                "smtp-mail.outlook.com",
                "587"
            );

            // 4. Email rỗng (test edge case)
            Account acc4 = new Account(
                "",
                "nopass",
                "NoName",
                "imap.test.com",
                "123",
                "smtp.test.com",
                "456"
            );

            ListAccount a = new ListAccount();
            a.AddAccount(acc1);
            a.AddAccount(acc2);
            a.AddAccount(acc3);
            a.AddAccount(acc4);


        }
    }
}
