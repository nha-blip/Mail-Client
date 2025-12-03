using Database;
using MailClient;
using Microsoft.Win32;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq; 
using System.Runtime.CompilerServices;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
namespace Mailclient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        // 1. ĐỔI TÊN: Đây là danh sách "gốc" (master list)
        private MailClient.ListEmail list;
        private SolidColorBrush colorSelected = (SolidColorBrush)(new BrushConverter().ConvertFrom("#33FFFFFF"));
        private DispatcherTimer syncTimer;
        private string currentFolder = "Inbox";
        private GmailStore gmailStore;
        private MailClient.ListAccount listAcc;
        public MainWindow()
        {
            gmailStore = new GmailStore();
            InitializeComponent();
            list = new MailClient.ListEmail(App.CurrentAccountID);
            inboxbt.Background = colorSelected;
            var filter = list.listemail.Where(email => email.FolderName == "Inbox");
            MyEmailList.ItemsSource = filter;
            SyncAndReload();
            StartEmailSync();
        }
        private async void SyncAndReload()
        {
            // Kiểm tra xem có đang đăng nhập Google không
            if (App.CurrentGmailStore != null && App.CurrentGmailStore.Service != null)
            {
                // 1. Tải thư từ Google -> Lưu vào SQL
                // (Hàm này nằm trong file GmailStore.cs mình gửi bài trước)
                await App.CurrentGmailStore.SyncEmailsToDatabase(App.CurrentAccountID);

                // 2. QUAN TRỌNG: Đọc lại Database để lấy dữ liệu mới vừa lưu
                list = new MailClient.ListEmail(App.CurrentAccountID);

                // 3. Cập nhật lại giao diện (đang đứng ở Inbox thì load lại Inbox)
                if (currentFolder == "Inbox")
                {
                    inboxbt.Background = colorSelected;
                   
                    MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Inbox").ToList();
                }
                else if (currentFolder == "AllMail")
                {
                    // Nếu đang ở All Mail thì load lại All Mail
                    MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName != "Trash").ToList();
                }

                 //(Tùy chọn) Hiện thông báo nhỏ để biết đã xong
                 //MessageBox.Show("Đã cập nhật thư mới từ Gmail!");
            }
        }
        private void StartEmailSync()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromSeconds(5); // mỗi phút đồng bộ
            syncTimer.Tick += syncTimer_Tick;
            syncTimer.Start();
        }
        private void syncTimer_Tick(object sender,EventArgs e)
        {
            SyncAndReload();
        }
        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Cho phép kéo cửa sổ
            this.DragMove();

        }
        private void OPLogin(object sender, RoutedEventArgs e)
        {
            // Đảo ngược trạng thái: Đang đóng thì mở, đang mở thì đóng
            AccountPopup.IsOpen = !AccountPopup.IsOpen;

            // Nếu mở ra thì có thể load dữ liệu (Tùy chọn)
            if (AccountPopup.IsOpen)
            {
                // ListAccountControl.LoadDataFromSQL(); // Nếu cần refresh
            }

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
                    (email.Subject != null && email.Subject.ToLower().Contains(searchText)) || (email.AccountName != null && email.AccountName.ToLower().Contains(searchText))).ToList();

                // Hiển thị danh sách đã lọc
                MyEmailList.ItemsSource = filteredEmails;
            }
        }

        private void inbox(object sender, RoutedEventArgs e)
        {
            currentFolder = "Inbox";
            resetcolor();
            inboxbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName == "Inbox").ToList();
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;

            CloseEmailView();
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
            currentFolder = "Sent";
            resetcolor();
            sentbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName == "Sent").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;

            CloseEmailView();
        }

        private void spam(object sender, RoutedEventArgs e)
        {
            currentFolder = "Spam";
            resetcolor();
            spambt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName == "Spam").ToList();

            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;

            CloseEmailView();
        }

        private void drafts(object sender, RoutedEventArgs e)
        {
            currentFolder = "Draft";
            resetcolor();
            draftsbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName == "Draft").ToList();
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;

            CloseEmailView();
        }

        private void allmail(object sender, RoutedEventArgs e)
        {
            currentFolder = "AllMail";
            resetcolor();
            allmailbt.Background = colorSelected;
            // Hiển thị danh sách đã lọc
            var filteredEmails = list.listemail.Where(email => email.FolderName != "Trash").ToList();
            MyEmailList.ItemsSource = filteredEmails;

            CloseEmailView();
        }
        private void trash(object sender, RoutedEventArgs e)
        {
            currentFolder = "Trash";
            resetcolor();
            trashmailbt.Background = colorSelected;
            var filteredEmails = list.listemail.Where(email => email.FolderName == "Trash").ToList();
            // Hiển thị danh sách đã lọc
            MyEmailList.ItemsSource = filteredEmails;

            CloseEmailView();
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

        private Rect sizeWin;

        private void Maximize(object sender, RoutedEventArgs e)
        {
            // Kiểm tra: Nếu kích thước hiện tại đang bằng kích thước vùng làm việc -> Đang phóng to
            bool isMaximized = (this.Width >= SystemParameters.WorkArea.Width &&
                                this.Height >= SystemParameters.WorkArea.Height);

            if (isMaximized)
            {
                // === TRƯỜNG HỢP 1: ĐANG TO -> THU NHỎ LẠI ===

                // 1. Cho phép thay đổi kích thước lại
                this.ResizeMode = ResizeMode.CanResize;

                // 2. Khôi phục vị trí và kích thước cũ (lấy từ biến đã lưu)
                this.Left = sizeWin.Left;
                this.Top = sizeWin.Top;
                this.Width = sizeWin.Width;
                this.Height = sizeWin.Height;

                btnMaximize.Content = "\uE922";
            }
            else
            {
                // === TRƯỜNG HỢP 2: ĐANG NHỎ -> PHÓNG TO HẾT CỠ ===

                // 1. Lưu lại vị trí hiện tại trước khi phóng to
                sizeWin = new Rect(this.Left, this.Top, this.Width, this.Height);

                // 2. Set kích thước bằng ĐÚNG vùng làm việc (WorkArea = Màn hình - Taskbar)
                // Cách này đảm bảo 100% không đè Taskbar
                this.Left = SystemParameters.WorkArea.Left;
                this.Top = SystemParameters.WorkArea.Top;
                this.Width = SystemParameters.WorkArea.Width;
                this.Height = SystemParameters.WorkArea.Height;

                // 3. Khóa không cho người dùng kéo dãn khi đang full màn hình
                this.ResizeMode = ResizeMode.NoResize;

                btnMaximize.Content = "\uE923";
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

                if (emailToDelete != null)
                {
                    // 1. Đánh dấu thư là rác
                    emailToDelete.FolderName = "Trash";

                    // 2. Cập nhật lại giao diện dựa trên MÀN HÌNH ĐANG MỞ
                    switch (currentFolder)
                    {
                        case "Inbox":
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Inbox").ToList();
                            break;

                        case "Sent":
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Sent").ToList();
                            break;

                        case "Spam":
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Spam").ToList();
                            break;

                        case "Drafts":
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Draft").ToList();
                            break;

                        case "AllMail":
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName != "Trash").ToList();
                            break;

                        case "Trash":
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Trash").ToList();
                            break;

                        default:
                            MyEmailList.ItemsSource = list.listemail.Where(email => email.FolderName == "Inbox").ToList();
                            break;
                    }

                    // 3. Đóng khung đọc mail đi (để tránh lỗi hiển thị thư vừa xóa)
                    CloseEmailView();
                }
            }
        }


        private async void content(object sender, SelectionChangedEventArgs e)
        {
            // 1. Kiểm tra an toàn
            if (MyEmailList.SelectedIndex == -1 || MyEmailList.SelectedItem == null) return;

            mailcontent.Visibility = Visibility.Visible;
            await contentEmail.EnsureCoreWebView2Async();

            // Lấy email đang chọn từ giao diện (đây là object từ Database)
            var dbEmail = MyEmailList.SelectedItem as MailClient.Email;

            if (dbEmail != null)
            {
                // 2. CHUYỂN ĐỔI DỮ LIỆU (Mapping)
                // Vì hàm GenerateDisplayHtml yêu cầu đầu vào là 'ParsedEmail',
                // ta cần tạo một đối tượng ParsedEmail tạm từ dữ liệu Database.

                var parsedEmail = new ParsedEmail
                {
                    Subject = dbEmail.Subject,
                    From = dbEmail.From,
                    Date = dbEmail.DateSent,
                    // BodyText trong DB lúc này đã là HTML sạch (do GmailStore lưu)
                    BodyAsHtml = dbEmail.BodyText ?? "",

                    // Chuyển đổi danh sách người nhận (To)
                    // (Giả sử dbEmail.To là mảng string[], cần chuyển sang List<string>)
                    To = dbEmail.To != null ? new List<string>(dbEmail.To) : new List<string>()
                };

                // Nếu bạn có lưu File đính kèm vào DB hoặc thư mục, bạn có thể add vào parsedEmail.Attachments ở đây
                // Ví dụ: parsedEmail.Attachments.Add(new ParsedAttachmentInfo { FileName = "...", SizeInBytes = ... });

                // 3. GỌI HÀM TẠO GIAO DIỆN ĐẸP (Từ EmailParser)
                var parser = new EmailParser();

                // Tùy chọn: Truyền Logo vào nếu muốn (ví dụ logo Gmail)
                // string logoUrl = "https://upload.wikimedia.org/wikipedia/commons/7/7e/Gmail_icon_%282020%29.svg";
                string htmlDisplay = parser.GenerateDisplayHtml(parsedEmail, null);

                // 4. HIỂN THỊ LÊN WEBVIEW
                contentEmail.NavigateToString(htmlDisplay);
            }
        }


        private void returnMain(object sender, RoutedEventArgs e)
        {
            CloseEmailView();
        }

        private void returnmainW(object sender, MouseButtonEventArgs e)
        {
            CloseEmailView();
        }
        private void CloseEmailView()
        {

            // Ẩn giao diện đọc mail
            mailcontent.Visibility = Visibility.Collapsed;

            // Bỏ chọn list
            MyEmailList.SelectedIndex = -1;
            MyEmailList.UnselectAll();

            // Lấy lại Focus cho Window
            this.Focus();
        }
    } // <-- KẾT THÚC CLASS MAINWINDOW
}