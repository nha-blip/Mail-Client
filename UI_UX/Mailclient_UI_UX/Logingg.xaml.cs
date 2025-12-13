using MailClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MailClient.Core.Services;

namespace Mailclient
{
    public partial class Logingg : Window
    {
        public Logingg()
        {
            InitializeComponent();
        }

        // --- CÁC HÀM XỬ LÝ GIAO DIỆN CỬA SỔ GIỮ NGUYÊN ---
        private void Window_MouseDown(object sender, MouseButtonEventArgs e) { if (e.ButtonState == MouseButtonState.Pressed) this.DragMove(); }
        private void Minimize_Click(object sender, RoutedEventArgs e) { this.WindowState = WindowState.Minimized; }
        private void Close_Click(object sender, RoutedEventArgs e) { this.Close(); }

        // --- HÀM XỬ LÝ ĐĂNG NHẬP (ĐÃ SỬA) ---
        private async void openACC(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn) btn.IsEnabled = false;
            try
            {
                // 1. Tạo Account tạm. 
                // Lưu ý: Nếu Account không có constructor rỗng, hãy truyền chuỗi rỗng
                Account tempAccount = new Account("", "");

                // 2. Tạo Store nối với Account tạm
                var myStore = new AccountTokenStore(tempAccount);

                var account = new AccountService();

                // 3. Login với Store tùy biến
                // Token sẽ tự động chui vào tempAccount.TokenJson
                await account.SignInAsync(myStore);
                bool success = account.IsSignedIn();

                if (success)
                {
                    string? realEmail = account._userEmail;
                    string? realName = account._userName; // Lấy tên hoặc dùng email làm tên

                    // 4. LƯU VÀO DATABASE
                    // Tạo đối tượng Account thực tế với thông tin vừa lấy được
                    Account realAccount = new Account(realEmail, realName);

                    // Gọi hàm xử lý logic DB (đã viết trong class Account ở câu trả lời trước)
                    // Hàm này sẽ tự Insert hoặc Update token và trả về ID
                    int accID = realAccount.LoginOrRegisterGoogle(tempAccount.TokenJson);

                    // 5. Cài đặt biến toàn cục
                    App.CurrentAccountID = accID;
                    App.currentAccountService = account;
                    App.currentMailService = new MailService(account);

                    // 6. Chuyển màn hình
                    MainWindow newMain = new MainWindow();
                    Application.Current.MainWindow = newMain;
                    newMain.Show();

                    // Đóng các cửa sổ cũ
                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow && window != newMain)
                        {
                            window.Close();
                            break; // Chỉ đóng cửa sổ Main cũ
                        }
                    }
                    this.Close(); // Đóng cửa sổ Login
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đăng nhập: {ex.Message}");
            }
            finally
            {
                if (sender is Button btnfinal) btnfinal.IsEnabled = true;
            }
        }
    }
}