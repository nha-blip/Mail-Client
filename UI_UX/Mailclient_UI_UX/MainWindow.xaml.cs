using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq; 
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
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
        private SolidColorBrush? colorSelected = (SolidColorBrush)(new BrushConverter().ConvertFrom("#33FFFFFF"));
        // 2. XÓA BỎ CLASS "EMAIL" ĐƠN GIẢN (LỒNG BÊN TRONG)
        // (Đã xóa)

        // Hàm để tạo danh sách (Giữ nguyên)
        


        // Hàm khởi tạo (Constructor)
        public MainWindow()
        {
            InitializeComponent();

            // 3. SỬA LẠI: Nạp dữ liệu vào "list.listemail"
            list = new MailClient.ListEmail();
            var filter = list.listemail.Where(email => email.FolderName == "Inbox");
            MyEmailList.ItemsSource = filter;
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void OPLogin(object sender, RoutedEventArgs e)
        {
            Logingg login = new Logingg();
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
            inboxbt.Background = Brushes.Transparent;
            sentbt.Background = Brushes.Transparent;
            draftsbt.Background = Brushes.Transparent;
            spambt.Background = Brushes.Transparent;
            allmailbt.Background = Brushes.Transparent;
            trashmailbt.Background = Brushes.Transparent;
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
            allmailbt.Background= colorSelected;
            // Hiển thị danh sách đã lọc
            var filteredEmails = list.listemail.Where(email => email.FolderName != "trash").ToList();
            MyEmailList.ItemsSource = filteredEmails;
        }
        private void trash(object sender, RoutedEventArgs e)
        {
            resetcolor();
            trashmailbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName == "Trash").ToList();
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            EnableBlur();
        }

        // =============================================================
        // ĐOẠN CODE DƯỚI ĐÂY LÀ ĐỂ GỌI WINDOWS API LÀM MỜ NỀN
        // =============================================================
        private void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);
            var accent = new AccentPolicy();
            accent.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND;
            accent.GradientColor = unchecked((int)0x66000000);

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData();
            data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
            data.SizeOfData = accentStructSize;
            data.Data = accentPtr;

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }

        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        internal enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }

        internal enum AccentState
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, 
            ACCENT_INVALID_STATE = 5
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        private void close(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void Maximize(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Normal)
            {
                // Nếu đang bình thường -> Phóng to
                this.WindowState = WindowState.Maximized;
                mainborder.Padding = new Thickness(8);
                btnMaximize.Content = "\uE923";
            }
            else
            {
                // Nếu đang phóng to -> Trở về bình thường
                this.WindowState = WindowState.Normal;
                mainborder.Padding = new Thickness(0);
                btnMaximize.Content = "\uE922";
            }
        }

        private void deletemail(object sender, RoutedEventArgs e)
        {
            // Lấy Button được click
            var button = sender as Button;

            // Lấy đối tượng Email được liên kết với Button đó
            var emailToDelete = button?.DataContext as MailClient.Email;

            if (emailToDelete != null)
            {
                // Thực hiện logic xóa:
                // 1. Chuyển email sang loại "trash" (thùng rác)
                string folder = emailToDelete.FolderName;
                emailToDelete.FolderName = "Trash";

                if (folder == "Inbox")
                {
                    var filteredEmails = list.listemail.Where(email => email.FolderName == "Inbox").ToList();
                    MyEmailList.ItemsSource = filteredEmails;
                }
                else if (folder == "Spam")
                {
                    var filteredEmails = list.listemail.Where(email => email.FolderName == "Spam").ToList();
                    MyEmailList.ItemsSource = filteredEmails;
                }
                else if (folder == "Drafts")
                {
                    var filteredEmails = list.listemail.Where(email => email.FolderName == "Drafts").ToList();
                    MyEmailList.ItemsSource = filteredEmails;
                }
                else if (folder == "Sent")
                {
                    var filteredEmails = list.listemail.Where(email => email.FolderName == "Sent").ToList();
                    MyEmailList.ItemsSource = filteredEmails;
                }
                else if(folder == "Trash") { }
                else
                {
                    var filteredEmails = list.listemail.Where(email => email.FolderName!= "Trash").ToList();
                    MyEmailList.ItemsSource = filteredEmails;
                }
            }
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