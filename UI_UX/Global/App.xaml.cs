using System.Configuration;
using System.Data;
using System.Windows;
using MailClient.Core.Services;

namespace Mailclient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        // Lưu ID tài khoản đang đăng nhập
        public static int CurrentAccountID;
        public static AccountService currentAccountService;
        public static MailService currentMailService;
    }

}
