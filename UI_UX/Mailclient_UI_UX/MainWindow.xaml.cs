// Thêm các "using" cần thiết ở đầu file
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq; // <-- QUAN TRỌNG: Cần cho .Where()
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Database;
namespace Mailclient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 1. ĐỔI TÊN: Đây là danh sách "gốc" (master list)
        private MailClient.ListEmail list;
        private SolidColorBrush? colorSelected = (SolidColorBrush)(new BrushConverter().ConvertFrom("#A8C7FA"));
        // 2. XÓA BỎ CLASS "EMAIL" ĐƠN GIẢN (LỒNG BÊN TRONG)
        // (Đã xóa)

        // Hàm để tạo danh sách (Giữ nguyên)
        

        // Hàm khởi tạo (Constructor)
        public MainWindow()
        {
            InitializeComponent();

            // 3. SỬA LẠI: Nạp dữ liệu vào "list.listemail"
            list = new MailClient.ListEmail();
        }
            

        private void OPLogin(object sender, RoutedEventArgs e)
        {
            Login login = new Login();
            login.Show();
        }

        private void opcompose(object sender, RoutedEventArgs e)
        {
            composecontent.Visibility = Visibility.Visible;
        }

        // 4. SỬA LẠI HÀM TÌM KIẾM
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchText = SearchBar.Text.ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // Hiển thị lại danh sách "gốc"
                MyEmailList.ItemsSource = list.listemail;
            }
            else
            {
                // Lọc từ danh sách "gốc" (list.listemail)
                var filteredEmails = list.listemail.Where(email =>

                    // SỬA LỖI CS8602: Thêm kiểm tra "!= null"
                    (email.Subject != null && email.Subject.ToLower().Contains(searchText))).ToList();

                // Hiển thị danh sách đã lọc
                MyEmailList.ItemsSource = filteredEmails;
            }
        }

        private void inbox(object sender, RoutedEventArgs e)
        {
            resetcolor();

            inboxbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email =>email.FolderName=="Inbox").ToList();
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }
        private void resetcolor()
        {
            inboxbt.Background = Brushes.White;
            sentbt.Background = Brushes.White;
            draftsbt.Background = Brushes.White;
            spambt.Background = Brushes.White;
            allmailbt.Background = Brushes.White;
        }
        private void sent(object sender, RoutedEventArgs e)
        {
            resetcolor();
            sentbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName=="Sent").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }

        private void spam(object sender, RoutedEventArgs e)
        {
            resetcolor();
            spambt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName=="Spam").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }

        private void drafts(object sender, RoutedEventArgs e)
        {
            resetcolor();
            draftsbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName=="Draft").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }

        private void allmail(object sender, RoutedEventArgs e)
        {
            resetcolor();
            allmailbt.Background = colorSelected;
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = list.listemail;
        }

    } 
    
}