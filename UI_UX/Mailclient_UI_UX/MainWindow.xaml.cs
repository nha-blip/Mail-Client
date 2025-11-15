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

namespace Mailclient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // 1. ĐỔI TÊN: Đây là danh sách "gốc" (master list)
        private List<Email> allEmails;
        private SolidColorBrush? colorSelected = (SolidColorBrush)(new BrushConverter().ConvertFrom("#A8C7FA"));
        // 2. XÓA BỎ CLASS "EMAIL" ĐƠN GIẢN (LỒNG BÊN TRONG)
        // (Đã xóa)

        // Hàm để tạo danh sách (Giữ nguyên)
        public List<Email> CreateEmailList()
        {
            List<Email> emailList = new List<Email>();

            // Đối tượng 1 (Chưa đọc)
            emailList.Add(new Email
            {
                Title = "Xin đừng trả lời",
                Subject = "Bạn đã nộp bài làm cho Nộp Bài tập Chương 5...",
                Date = new DateTime(2025, 11, 10),
                IsRead = false,
                IsChecked = false, // Khởi tạo giá trị
                loai = "inbox"
            });

            // Đối tượng 2 (Chưa đọc)
            emailList.Add(new Email
            {
                Title = "Trần Văn Như Y",
                Subject = "IT005.Q16: Thông báo lịch học tuần này (11/11)...",
                Date = new DateTime(2025, 11, 10),
                IsRead = false,
                IsChecked = false,
                loai = "inbox"
            });
            emailList.Add(new Email
            {
                Title = "Phòng Đào tạo",
                Subject = "Thông báo V/v đăng ký học phần Học kỳ 2",
                Date = new DateTime(2025, 11, 10),
                IsRead = false,
                IsChecked = false,
                loai = "inbox"
            });

            // 2. Spam - Quảng cáo
            emailList.Add(new Email
            {
                Title = "FPT Shop",
                Subject = "KHUYẾN MÃI ĐỘC QUYỀN: Giảm giá 50% cho iPhone 20!",
                Date = new DateTime(2025, 11, 10),
                IsRead = true,
                IsChecked = false,
                loai = "spam"
            });

            // 3. Sent - Đã gửi
            emailList.Add(new Email
            {
                Title = "Me (Gửi: Thầy A)",
                Subject = "Em xin nộp bổ sung Bài tập Lớn ạ",
                Date = new DateTime(2025, 11, 9),
                IsRead = true,
                IsChecked = false,
                loai = "sent"
            });

            // 4. Inbox - Cá nhân
            emailList.Add(new Email
            {
                Title = "Nguyễn Văn B",
                Subject = "Cuối tuần này đi cafe không?",
                Date = new DateTime(2025, 11, 9),
                IsRead = false,
                IsChecked = false,
                loai = "inbox"
            });

            // 5. Inbox - Hệ thống (Giống ví dụ của bạn)
            emailList.Add(new Email
            {
                Title = "Diễn đàn - Trường Đ...",
                Subject = "[Diễn đàn UIT] Trả lời chủ đề: Thảo luận môn XYZ",
                Date = new DateTime(2025, 11, 8),
                IsRead = true,
                IsChecked = false,
                loai = "inbox"
            });

            // 6. Spam - Lừa đảo
            emailList.Add(new Email
            {
                Title = "Tài khoản Ngân hàng",
                Subject = "CẢNH BÁO BẢO MẬT: Tài khoản của bạn đã bị khóa",
                Date = new DateTime(2025, 11, 8),
                IsRead = false,
                IsChecked = false,
                loai = "inbox"
            });

            // 7. Inbox - Công việc/Học tập
            emailList.Add(new Email
            {
                Title = "Phòng, CTSV",
                Subject = "Thông báo về việc tham gia Tuần lễ Sinh viên 5 Tốt",
                Date = new DateTime(2025, 11, 7),
                IsRead = true,
                IsChecked = false,
                loai = "inbox"
            });

            // 8. Sent - Đã gửi
            emailList.Add(new Email
            {
                Title = "Me (Gửi: Nhóm 5)",
                Subject = "Tài liệu và phân công cho slide thuyết trình",
                Date = new DateTime(2025, 11, 7),
                IsRead = true,
                IsChecked = false,
                loai = "sent"
            });

            // 9. Inbox - Thông báo (Giống ví dụ của bạn)
            emailList.Add(new Email
            {
                Title = "Xin đừng trả lời",
                Subject = "Bạn đã nộp bài làm cho Lớp 2 | Bài tập LAB 03...",
                Date = new DateTime(2025, 11, 6),
                IsRead = true,
                IsChecked = false,
                loai = "inbox"
            });

            // 10. Inbox - Mạng xã hội
            emailList.Add(new Email
            {
                Title = "LinkedIn",
                Subject = "Bạn có 3 lời mời kết nối mới",
                Date = new DateTime(2025, 11, 6),
                IsRead = false,
                IsChecked = false,
                loai = "inbox"
            });
            return emailList;
        }

        // Hàm khởi tạo (Constructor)
        public MainWindow()
        {
            InitializeComponent();

            // 3. SỬA LẠI: Nạp dữ liệu vào "allEmails"
            allEmails = CreateEmailList();

            // Gán danh sách "gốc" cho ListBox
            var filteredEmails = allEmails.Where(email => email.loai == "inbox" ).ToList();
            inboxbt.Background = colorSelected;
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
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
                MyEmailList.ItemsSource = allEmails;
            }
            else
            {
                // Lọc từ danh sách "gốc" (allEmails)
                var filteredEmails = allEmails.Where(email =>

                    // SỬA LỖI CS8602: Thêm kiểm tra "!= null"
                    (email.Title != null && email.Title.ToLower().Contains(searchText)) ||
                    (email.Subject != null && email.Subject.ToLower().Contains(searchText))

                ).ToList();

                // Hiển thị danh sách đã lọc
                MyEmailList.ItemsSource = filteredEmails;
            }
        }

        private void inbox(object sender, RoutedEventArgs e)
        {
            resetcolor();
            inboxbt.Background = colorSelected;
            var filteredEmails = allEmails.Where(email =>email.loai=="inbox"
                ).ToList();
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
            var filteredEmails = allEmails.Where(email => email.loai == "sent").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }

        private void spam(object sender, RoutedEventArgs e)
        {
            resetcolor();
            spambt.Background = colorSelected;
            var filteredEmails = allEmails.Where(email => email.loai == "spam").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }

        private void drafts(object sender, RoutedEventArgs e)
        {
            resetcolor();
            draftsbt.Background = colorSelected;
            var filteredEmails = allEmails.Where(email => email.loai == "draft").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }

        private void allmail(object sender, RoutedEventArgs e)
        {
            resetcolor();
            allmailbt.Background = colorSelected;
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = allEmails;
        }

    } // <-- KẾT THÚC CLASS MAINWINDOW


    // 5. GIỮ LẠI CLASS "PHỨC TẠP" (CÓ INotifyPropertyChanged)
    // (Nó nằm BÊN NGOÀI MainWindow, nhưng BÊN TRONG namespace)
    public class Email : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // --- Các thuộc tính (Properties) ---
        private string _loai = "";
        public string loai
        {
            get { return _loai; }
            set { _loai = value; OnPropertyChanged(); }
        }
        private string _title = "";
        public string Title
        {
            get { return _title; }
            set { _title = value; OnPropertyChanged(); }
        }

        private string _subject = "";
        public string Subject
        {
            get { return _subject; }
            set { _subject = value; OnPropertyChanged(); }
        }

        private DateTime _date;
        public DateTime Date
        {
            get { return _date; }
            set { _date = value; OnPropertyChanged(); }
        }

        public string DateDisplay => Date.ToString("MMM d");

        private bool _isRead;
        public bool IsRead
        {
            get { return _isRead; }
            set
            {
                if (_isRead != value)
                {
                    _isRead = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isChecked;
        public bool IsChecked
        {
            get { return _isChecked; }
            set
            {
                if (_isChecked != value)
                {
                    _isChecked = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}