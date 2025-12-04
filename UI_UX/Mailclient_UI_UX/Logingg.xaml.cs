using MailClient; // Namespace chứa GmailStore và DatabaseHelper
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

namespace Mailclient
{
    /// <summary>
    /// Interaction logic for Logingg.xaml
    /// </summary>
    public partial class Logingg : Window
    {
        public Logingg()
        {
            InitializeComponent();
        }

        // --- CÁC HÀM XỬ LÝ GIAO DIỆN CỬA SỔ ---
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

        // --- HÀM XỬ LÝ ĐĂNG NHẬP GOOGLE ---
        // (Đảm bảo bên XAML nút bấm của bạn có sự kiện Click="openACC")
        private async void openACC(object sender, RoutedEventArgs e)
        {
            string tokenPath = "token_store";
            if (Directory.Exists(tokenPath))
            {
                Directory.Delete(tokenPath, true); // Xóa folder token
            }
            else if (File.Exists(tokenPath))
            {
                File.Delete(tokenPath); // Xóa file token
            }

            // 1. Khóa nút để tránh người dùng bấm liên tục
            // (Giả sử nút của bạn tên là "login", nếu tên khác thì sửa lại nhé)
            if (sender is Button btn) btn.IsEnabled = false;

            try
            {
                // 2. Khởi tạo GmailStore (Class mới chúng ta vừa tạo)
                var googleHelper = new MailClient.GmailStore();

                // 3. Gọi hàm đăng nhập (Hàm này sẽ mở trình duyệt web)
                bool success = await googleHelper.LoginAsync();

                // 4. Kiểm tra kết quả
                if (success)
                {
                    // Hiện thông báo chào mừng (Tùy chọn)
                    //MessageBox.Show($"Đăng nhập thành công!\nXin chào: {googleHelper.UserEmail}");

                    // 5. KẾT NỐI DATABASE
                    // Gọi hàm để kiểm tra xem Email này đã có trong DB chưa, nếu chưa thì tạo mới
                    //Account acc = new Account(googleHelper.UserEmail, googleHelper.UserEmail);
                    DatabaseHelper dp = new DatabaseHelper();
                    int accID = dp.LoginOrRegisterGoogle(googleHelper.UserEmail, googleHelper.Username);

                    // 6. LƯU THÔNG TIN VÀO BIẾN TOÀN CỤC (Để MainWindow dùng)
                    App.CurrentAccountID = accID;       // Lưu ID để biết đang tải thư của ai
                    App.CurrentGmailStore = googleHelper; // Lưu kết nối để tí nữa tải thư không cần login lại

                    MailClient.Account newAccount = new MailClient.Account(accID);
                    newAccount.Email = googleHelper.UserEmail;
                    newAccount.Username = googleHelper.UserEmail;
                    var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

                    if (mainWindow != null)
                    {
                        // TRƯỜNG HỢP 1: Đã có cửa sổ chính (Ví dụ: Đang ở Main bấm nút "Thêm tài khoản")
                        // => Chỉ cần cập nhật thêm vào danh sách
                        mainWindow.ListAccountControl.AddNewAccountToUI(newAccount);

                        // Mở popup cho user thấy
                        mainWindow.AccountPopup.IsOpen = true;

                        // Đưa cửa sổ chính lên trên cùng
                        mainWindow.Activate();
                    }
                    else
                    {
                        // TRƯỜNG HỢP 2: Chưa có cửa sổ chính (Ví dụ: Mới mở App lên là form Login luôn)
                        // => Phải TẠO MỚI cửa sổ chính
                        MainWindow newMain = new MainWindow();

                        // Quan trọng: Phải hiện lên trước thì các Control bên trong mới được tạo
                        newMain.Show();

                        // Sau khi Show, gán dữ liệu tài khoản vào danh sách
                        newMain.ListAccountControl.AddNewAccountToUI(newAccount);
                    }
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi đăng nhập: {ex.Message}\n\n{ex.InnerException?.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Mở lại nút dù thành công hay thất bại
                if (sender is Button btnfinal) btnfinal.IsEnabled = true;
            }
        }
    }
}