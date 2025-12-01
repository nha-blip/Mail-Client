using System.Configuration;
using System.Data;
using System.Windows;

namespace Mailclient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        // Lưu ID tài khoản đang đăng nhập
        public static int CurrentAccountID;

        // Lưu đối tượng Google để dùng lại (đỡ phải login lại)
        public static MailClient.GmailStore CurrentGmailStore;
    }

}
