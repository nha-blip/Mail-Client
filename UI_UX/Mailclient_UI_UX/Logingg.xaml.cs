using MailClient.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Mailclient.MainWindow;
namespace Mailclient
{
    /// <summary>
    /// Interaction logic for Logingg.xaml
    /// </summary>
    public partial class Logingg : Window
    {
        private AccountService _accountService = new AccountService();
        public Logingg()
        {
            InitializeComponent();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void openACC(object sender, RoutedEventArgs e)
        {
            login.IsEnabled = false;

            try
            {
                // File này bạn phải tải từ Google Cloud Console về và để trong thư mục Debug
                string credPath = "C:\\Users\\buitr\\source\\repos\\UI_UX\\Mailclient_UI_UX\\googlesv\\mailclient.json";

                // Thư mục này code sẽ tự tạo ra để lưu phiên đăng nhập
                string tokenPath = "token_store";

                // GỌI HÀM TỪ FILE ACCOUNT SERVICE CỦA BẠN
                await _accountService.SignInAsync(credPath, tokenPath);
                // Kiểm tra kết quả
                if (_accountService.IsSignedIn())
                {
                    // Lưu ý: Hàm GetCurrentUserEmail() hiện tại của bạn sẽ trả về chữ "user" 
                    // (do cách gọi AuthorizeAsync), chưa lấy được email thật ngay đâu. 
                    // Muốn lấy email thật cần gọi thêm API UserInfo, nhưng tạm thời thế này là đã login thành công.
                    MessageBox.Show("Đăng nhập Google thành công!");

                    // --- CHUYỂN MÀN HÌNH ---
                    // Ví dụ: Mở màn hình chính
                    // MainWindow main = new MainWindow();
                    // main.Show();
                    // this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đăng nhập: {ex.Message}");
            }
            finally
            {
                // Mở lại nút
                login.IsEnabled = true;
            }
        }
    }
    
}
