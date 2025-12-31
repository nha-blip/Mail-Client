using System;
using System.Collections.Generic;
using System.Linq;
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
using MailClient;

namespace Mailclient
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private bool isPasswordVisible = false;
        public Login()
        {
            InitializeComponent();
        }

        private void hide(object sender, RoutedEventArgs e)
        {
            if (isPasswordVisible == false)
            {
                // === ĐANG ẨN -> CHUYỂN SANG HIỆN ===
                // (Logic của hàm "apear" cũ)

                password1.Text = password.Password;
                password1.Visibility = Visibility.Visible;
                password.Visibility = Visibility.Collapsed;

                // Đặt con trỏ vào TextBox
                password1.Focus();
                password1.Select(password1.Text.Length, 0);
                // Cập nhật lại trạng thái
                isPasswordVisible = true;
            }
            else // (Nếu isPasswordVisible == true)
            {
                // === ĐANG HIỆN -> CHUYỂN SANG ẨN ===
                // (Logic của hàm "hide" cũ)
                password.Password = password1.Text;
                password1.Visibility = Visibility.Collapsed;
                password.Visibility = Visibility.Visible;
                password1.Text = ""; // Xóa text an toàn

                // Đặt con trỏ về PasswordBox
                password.Focus();
                // Cập nhật lại trạng thái
                isPasswordVisible = false;
            }
        }

        private void OPregister(object sender, MouseButtonEventArgs e)
        {
            logincontent.Visibility = Visibility.Collapsed;

            // Hiện "tab" Register
            RegisterContent.Visibility = Visibility.Visible;
        }
        public void ShowLoginView()
        {
            // Ẩn "tab" Register
            RegisterContent.Visibility = Visibility.Collapsed;

            forgotPasswordContent.Visibility = Visibility.Collapsed;
            // Hiện "tab" Login
            logincontent.Visibility = Visibility.Visible;
        }

        private void FGPassword(object sender, MouseButtonEventArgs e)
        {
           logincontent.Visibility = Visibility.Collapsed;
           forgotPasswordContent.Visibility = Visibility.Visible;
        }
        private void Login_Offline(object sender, EventArgs e)
        {
           Account acc = new Account(nameuser.Text, password.Password);
            if (acc.CheckAccount(nameuser.Text,password.Password))
            {
                DatabaseHelper db= new DatabaseHelper();
                db.SetCurrentAccountID(acc.AccountID);
                App.currentAccountService = null;
                App.currentMailService = null;
                MainWindow mainWindow= new MainWindow();
                mainWindow.Show();
                this.Close();
                Application.Current.MainWindow=mainWindow;
            }
            else
            {
                MessageBox.Show("Tài khoản hoặc mật khẩu sai!");
            }
                
        }

        private void textchange(object sender, TextChangedEventArgs e)
        {
            password.Password=password1.Text;
        }
    }
}
