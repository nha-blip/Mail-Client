using MailClient;
using Microsoft.Win32;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using MailClient.Core.Services;
namespace Mailclient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MailClient.ListEmail list;
        private SolidColorBrush? colorSelected = (SolidColorBrush)(new BrushConverter().ConvertFrom("#33FFFFFF"));
        private DispatcherTimer syncTimer;
        private string currentFolder = "Inbox";
        public MailClient.ListAccount listAcc;
        private MailClient.Email _currentReadingEmail;
        private MailService mailService;

        private bool isSyncing = false;

        public MainWindow()
        {
            InitializeComponent();
            mailService = App.currentMailService;
            InitializeWebView();

            listAcc = new MailClient.ListAccount();
            listAcc.AddAccount(new Account(App.CurrentAccountID));
            list = new MailClient.ListEmail(App.CurrentAccountID);

            UpdateUI_CurrentFolder();
            StartEmailSync();
            SyncAndReload();
        }

        private void UpdateUI_CurrentFolder()
        {
            if (list == null || list.listemail == null) return;

            System.Collections.IEnumerable filtered = null;
            // Lọc và hiển thị danh sách
            if (currentFolder == "AllMail")
            {
                // Nếu là All Mail: Lấy tất cả trừ Thùng rác
                filtered = list.listemail
                                   .Where(email => email.FolderName != "Trash" && email.FolderName != "Spam")
                                   .OrderByDescending(e => e.DateSent) // Sắp xếp mới nhất
                                   .ToList();
            }
            else
            {
                // Logic chung cho: Inbox, Sent, Draft, Trash, Spam...
                filtered = list.listemail
                                   .Where(email => email.FolderName == currentFolder)
                                   .OrderByDescending(e => e.DateSent)
                                   .ToList();
            }
            MyEmailList.ItemsSource = filtered;

            // Đổi màu nút bấm 
            resetcolor();
        }

        public async Task SyncAndReload()
        {
            if (isSyncing) return;

            isSyncing = true;
            // Kiểm tra xem có đang đăng nhập Google không
            if (App.currentAccountService.IsSignedIn())
            {
                try
                {
                    // Tải TẤT CẢ thư mục từ Google -> Lưu vào SQL
                    await mailService.SyncAllFoldersToDatabase(App.CurrentAccountID);

                    // Đọc lại Database lên RAM để lấy dữ liệu mới nhất
                    list.Refresh(App.CurrentAccountID);

                    // Cập nhật lại giao diện (đang đứng ở folder nào thì refresh folder đó)
                    UpdateUI_CurrentFolder();
                }
                catch (Exception ex)
                {
                    // Có thể log lỗi vào file hoặc console thay vì hiện MessageBox liên tục gây phiền
                    Console.WriteLine("Lỗi Sync: " + ex.Message);
                }
                finally
                {
                    // Mở khóa để lần sau sync tiếp
                    isSyncing = false;
                    // this.Title = "MailClient";
                }
            }
        }
        private void StartEmailSync()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromSeconds(30); 
            syncTimer.Tick += syncTimer_Tick;
            syncTimer.Start();
        }

        private async void syncTimer_Tick(object sender, EventArgs e)
        {
            await SyncAndReload();
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

        // HÀM TÌM KIẾM
        private void TextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string searchText = SearchBar.Text.ToLower();

            if (string.IsNullOrEmpty(searchText))
            {
                // Nếu xóa text tìm kiếm, load lại theo folder hiện tại
                UpdateUI_CurrentFolder();
            }
            else
            {
                // Lấy danh sách nguồn dựa trên Folder hiện tại
                IEnumerable<MailClient.Email> sourceList;

                if (currentFolder == "AllMail")
                {
                    // Nếu đang ở All Mail: Lấy tất cả trừ Trash và Spam
                    sourceList = list.listemail.Where(email => email.FolderName != "Trash" && email.FolderName != "Spam");
                }
                else
                {
                    // Các trường hợp khác (Inbox, Sent, Draft...): Lấy đúng theo tên folder
                    sourceList = list.listemail.Where(email => email.FolderName == currentFolder);
                }

                // Tìm kiếm text trong danh sách nguồn đó
                var filteredEmails = sourceList.Where(email =>
                    (email.Subject != null && email.Subject.ToLower().Contains(searchText)) ||
                    (email.AccountName != null && email.AccountName.ToLower().Contains(searchText)) ||
                    (email.From != null && email.From.ToLower().Contains(searchText)) // Nên tìm thêm cả người gửi
                ).OrderByDescending(e => e.DateSent).ToList();

                MyEmailList.ItemsSource = filteredEmails;
            }
        }

        private void inbox(object sender, RoutedEventArgs e)
        {
            currentFolder = "Inbox";
            UpdateUI_CurrentFolder();
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

            switch (currentFolder)
            {
                case "Inbox": inboxbt.Background = colorSelected; break;
                case "Sent": sentbt.Background = colorSelected; break;
                case "Draft": draftsbt.Background = colorSelected; break; 
                case "Spam": spambt.Background = colorSelected; break;
                case "AllMail": allmailbt.Background = colorSelected; break;
                case "Trash": trashmailbt.Background = colorSelected; break;
            }
        }
        private void sent(object sender, RoutedEventArgs e)
        {
            currentFolder = "Sent";
            UpdateUI_CurrentFolder();
            CloseEmailView();
        }

        private void spam(object sender, RoutedEventArgs e)
        {
            currentFolder = "Spam";
            UpdateUI_CurrentFolder();
            CloseEmailView();
        }

        private void drafts(object sender, RoutedEventArgs e)
        {
            currentFolder = "Draft";
            UpdateUI_CurrentFolder();
            CloseEmailView();
        }

        private void allmail(object sender, RoutedEventArgs e)
        {
            currentFolder = "AllMail";
            UpdateUI_CurrentFolder();
            CloseEmailView();
        }
        private void trash(object sender, RoutedEventArgs e)
        {
            currentFolder = "Trash";
            UpdateUI_CurrentFolder();
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
                    // Đánh dấu thư là rác
                    emailToDelete.UpdateFolderEmail("Trash");
                    emailToDelete.FolderName = "Trash";

                    // Cập nhật lại giao diện dựa trên MÀN HÌNH ĐANG MỞ
                    UpdateUI_CurrentFolder();

                    // 3. Đóng khung đọc mail đi (để tránh lỗi hiển thị thư vừa xóa)
                    CloseEmailView();
                }
            }
        }

        private async void content(object sender, SelectionChangedEventArgs e)
        {
            // Kiểm tra an toàn
            if (MyEmailList.SelectedIndex == -1 || MyEmailList.SelectedItem == null) return;

            var selectedEmail = MyEmailList.SelectedItem as MailClient.Email;
            _currentReadingEmail = selectedEmail;

            mailcontent.Visibility = Visibility.Visible;

            if (contentEmail.CoreWebView2 == null)
            {
                await contentEmail.EnsureCoreWebView2Async();
            }

            // Lấy đối tượng Email từ giao diện
            if (_currentReadingEmail != null)
            {
                // Lấy danh sách file đính kèm cho biến _currentReadingEmail
                List<MailClient.Attachment> attachments = MailClient.Attachment.GetListAttachments(_currentReadingEmail.emailID);
                _currentReadingEmail.TempAttachments = attachments;

                var parser = new EmailParser();
                // Dùng biến _currentReadingEmail để tạo HTML
                string htmlDisplay = parser.GenerateDisplayHtml(_currentReadingEmail, null);
                try
                {
                    // Tạo đường dẫn file tạm trong thư mục Temp của máy tính
                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "email_view.html");

                    // Lưu chuỗi HTML vào file đó
                    System.IO.File.WriteAllText(tempPath, htmlDisplay);

                    // Điều hướng WebView tới file vừa tạo
                    contentEmail.CoreWebView2.Navigate(tempPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi hiển thị email: " + ex.Message);
                }
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
        private async void ContentEmail_WebMessageReceived(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            string message = e.TryGetWebMessageAsString();

            if (!string.IsNullOrEmpty(message) && message.StartsWith("DOWNLOAD:"))
            {
                string fileNameToDownload = message.Substring("DOWNLOAD:".Length);

                if (_currentReadingEmail == null || _currentReadingEmail.TempAttachments == null)
                {
                    MessageBox.Show("Dữ liệu email đã bị mất, vui lòng mở lại thư.", "Lỗi");
                    return;
                }

                // Tìm file trong danh sách attachment của email này
                var attachment = _currentReadingEmail.TempAttachments.FirstOrDefault(a => a.Name == fileNameToDownload);

                if (attachment != null)
                {
                    // Hỏi người dùng muốn lưu đâu
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.FileName = attachment.Name;
                    if (saveFileDialog.ShowDialog() == true)
                    {
                        string destinationPath = saveFileDialog.FileName;

                        try
                        {
                            // 1. Xác định đường dẫn file trong Cache
                            string cacheFolder = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Attachments");
                            // Tên file trong cache phải đúng quy tắc đã lưu: {ID}_{Name}
                            string cachedFileName = $"{attachment.Name}";
                            string sourcePath = System.IO.Path.Combine(cacheFolder, cachedFileName);

                            // 2. Kiểm tra xem file có trong Cache không
                            if (File.Exists(sourcePath))
                            {
                                // Copy từ Cache ra chỗ người dùng chọn
                                File.Copy(sourcePath, destinationPath, true);
                                MessageBox.Show("Đã lưu file thành công!", "Thông báo");
                            }
                            else
                            {
                                // Trường hợp hiếm: File trong cache bị xóa mất
                                MessageBox.Show($"Không tìm thấy file gốc tại: {sourcePath}\nCó thể bạn đã xóa bộ nhớ đệm.", "Lỗi File Missing");
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Lỗi khi lưu file: " + ex.Message);
                        }
                    }
                }
            }
        }

        private async void InitializeWebView()
        {
            // Đảm bảo WebView2 đã sẵn sàng
            await contentEmail.EnsureCoreWebView2Async();

            // Đăng ký sự kiện lắng nghe (chỉ 1 lần)
            contentEmail.WebMessageReceived += ContentEmail_WebMessageReceived;

            // 2. [THÊM MỚI] Đăng ký sự kiện chặn link để mở ra trình duyệt ngoài
            contentEmail.NavigationStarting += ContentEmail_NavigationStarting;

            // 3. [THÊM MỚI] Sự kiện click link mở tab mới (target="_blank")
            contentEmail.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        }

        private void ContentEmail_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            // Kiểm tra xem đường dẫn có phải là Link Web không (http hoặc https)
            if (e.Uri != null && (e.Uri.StartsWith("http://") || e.Uri.StartsWith("https://")))
            {
                // 1. HỦY việc load trang web đè lên nội dung email
                e.Cancel = true;

                // 2. Mở đường dẫn bằng trình duyệt mặc định của Windows
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = e.Uri,
                        UseShellExecute = true // Quan trọng: Để Windows tự chọn trình duyệt
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không thể mở liên kết: " + ex.Message);
                }
            }
        }

        private void CoreWebView2_NewWindowRequested(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NewWindowRequestedEventArgs e)
        {
            // 1. [QUAN TRỌNG] Chặn WebView2 không cho tự mở cửa sổ popup
            e.Handled = true;

            // 2. Lấy đường dẫn (Uri) mà người dùng muốn mở
            string url = e.Uri;

            // 3. Kiểm tra và mở bằng trình duyệt ngoài
            if (!string.IsNullOrEmpty(url) && (url.StartsWith("http://") || url.StartsWith("https://")))
            {
                try
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = url,
                        UseShellExecute = true // Bắt buộc để Windows tự chọn trình duyệt mặc định
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi mở link: " + ex.Message);
                }
            }
        }
        
    }
} 