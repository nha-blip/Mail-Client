using MailClient;
using MailClient.Core.Services;
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
using static Google.Apis.Requests.BatchRequest;
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
        private bool isFirstLoad = true;

        private List<MailClient.Email> _currentConversation;

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

            IEnumerable<MailClient.Email> sourceList;
            // Lọc và hiển thị danh sách
            if (currentFolder == "AllMail")
            {
                sourceList = list.listemail
                                .Where(email => email.FolderName != "Trash" && email.FolderName != "Spam" && email.AccountID == App.CurrentAccountID);
            }
            else
            {
                sourceList = list.listemail
                                .Where(email => email.FolderName == currentFolder && email.AccountID == App.CurrentAccountID);
            }
            // GOM NHÓM THEO THREAD ID
            var groupedList = sourceList
                .GroupBy(email =>
                {
                    return email.ThreadId;
                })
                .Select(group =>
                {
                    // Lấy lá thư MỚI NHẤT trong nhóm để hiển thị
                    return group.OrderByDescending(e => e.DateSent).FirstOrDefault();
                })
                .OrderByDescending(e => e.DateSent)
                .ToList();

            MyEmailList.ItemsSource = groupedList;
            //MessageBox.Show(groupedList.Count);

            // Đổi màu nút bấm 
            resetcolor();
        }

        public async Task SyncAndReload()
        {
            if (isSyncing) return;

            isSyncing = true;

            if (isFirstLoad)
            {
                ShowLoading("Đang tải dữ liệu lần đầu...");
            }
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
                    if (isFirstLoad)
                    {
                        HideLoading();
                        isFirstLoad = false;
                    }
                }
            }
        }
        private void StartEmailSync()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromSeconds(5); 
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

        }
        public void CloseAccountPopup()
        {
            // Đặt IsOpen = false để đóng Popup
            if (AccountPopup != null)
            {
                AccountPopup.IsOpen = false;
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
                var searchResults = sourceList.Where(email =>
                    (email.Subject != null && email.Subject.ToLower().Contains(searchText)) ||
                    (email.AccountName != null && email.AccountName.ToLower().Contains(searchText)) ||
                    (email.From != null && email.From.ToLower().Contains(searchText))
                );

                var groupedResults = searchResults
                    .GroupBy(email => email.ThreadId != 0 ? email.ThreadId : email.UID)
                    .Select(group => group.OrderByDescending(e => e.DateSent).FirstOrDefault())
                    .OrderByDescending(e => e.DateSent)
                    .ToList();

                MyEmailList.ItemsSource = groupedResults;
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
        private void EnableBlur()
        {
            var windowHelper = new WindowInteropHelper(this);

            // Cấu hình độ mờ và màu sắc (0x66000000 là màu đen độ trong suốt 40%)
            var accent = new AccentPolicy
            {
                AccentState = 4, // 4 là ENABLE_ACRYLICBLURBEHIND
                GradientColor = unchecked((int)0x66000000)
            };

            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            Marshal.StructureToPtr(accent, accentPtr, false);

            var data = new WindowCompositionAttributeData
            {
                Attribute = 19, // 19 là WCA_ACCENT_POLICY
                SizeOfData = accentStructSize,
                Data = accentPtr
            };

            SetWindowCompositionAttribute(windowHelper.Handle, ref data);
            Marshal.FreeHGlobal(accentPtr);
        }
        [DllImport("user32.dll")]
        internal static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        [StructLayout(LayoutKind.Sequential)]
        internal struct WindowCompositionAttributeData
        {
            public int Attribute;   
            public IntPtr Data;
            public int SizeOfData;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct AccentPolicy
        {
            public int AccentState;  
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
                try
                {
                    string htmlDisplay = "";

                    // HIỂN THỊ CONVERSATION 
                    if (_currentReadingEmail.ThreadId != 0)
                    {
                        // Lấy toàn bộ hội thoại
                        _currentConversation = list.GetConversation(_currentReadingEmail.ThreadId);

                        // Load Attachment cho TẤT CẢ email trong hội thoại (để hiển thị link tải)
                        foreach (var email in _currentConversation)
                        {
                            email.TempAttachments = MailClient.Attachment.GetListAttachments(email.emailID);
                        }

                        // Tạo HTML gộp
                        htmlDisplay = GenerateConversationHtml(_currentConversation);
                    }
                    else
                    {
                        // Fallback: Nếu không có ThreadId, hiển thị lẻ như cũ
                        _currentConversation = new List<MailClient.Email> { _currentReadingEmail };

                        // Load attach cho email lẻ
                        _currentReadingEmail.TempAttachments = MailClient.Attachment.GetListAttachments(_currentReadingEmail.emailID);

                        var parser = new EmailParser();
                        htmlDisplay = parser.GenerateDisplayHtml(_currentReadingEmail, null);
                    }

                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "email_view.html");
                    System.IO.File.WriteAllText(tempPath, htmlDisplay);
                    contentEmail.CoreWebView2.Navigate(tempPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi hiển thị email: " + ex.Message);
                }
            }
        }

        private string GenerateConversationHtml(List<MailClient.Email> conversation)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<html><head><style>");
            // CSS cơ bản cho đẹp
            sb.Append("body { font-family: 'Segoe UI', Arial, sans-serif; padding: 20px; background-color: #f3f3f3; margin: 0; }");
            sb.Append(".email-card { background: white; border: 1px solid #e0e0e0; border-radius: 8px; margin-bottom: 15px; padding: 20px; box-shadow: 0 1px 3px rgba(0,0,0,0.1); }");
            sb.Append(".header { border-bottom: 1px solid #eee; padding-bottom: 10px; margin-bottom: 15px; display: flex; justify-content: space-between; align-items: center; }");
            sb.Append(".sender { font-weight: bold; color: #202124; font-size: 14px; }");
            sb.Append(".date { font-size: 12px; color: #5f6368; }");
            sb.Append(".content { color: #202124; line-height: 1.5; overflow-wrap: break-word; }");
            sb.Append(".attachments { margin-top: 15px; padding-top: 10px; border-top: 1px dashed #ccc; }");
            sb.Append(".att-link { display: inline-block; margin-right: 15px; color: #1a73e8; text-decoration: none; font-size: 13px; background: #f1f3f4; padding: 5px 10px; border-radius: 16px; }");
            sb.Append(".att-link:hover { background: #e8eaed; }");
            sb.Append("</style></head><body>");

            foreach (var email in conversation)
            {
                sb.Append("<div class='email-card'>");

                // --- Header ---
                sb.Append("<div class='header'>");
                // Hiển thị tên người gửi hoặc email gửi
                string senderName = !string.IsNullOrEmpty(email.AccountName) ? email.AccountName : email.From;
                sb.Append($"<div><span class='sender'>{System.Web.HttpUtility.HtmlEncode(senderName)}</span> <span style='color:#5f6368'>&lt;{System.Web.HttpUtility.HtmlEncode(email.From)}&gt;</span></div>");
                sb.Append($"<span class='date'>{email.DateSent:dd/MM/yyyy HH:mm}</span>");
                sb.Append("</div>");

                // --- Body ---
                sb.Append($"<div class='content'>{email.BodyText}</div>");

                // --- Attachments ---
                // Cần load file đính kèm cho từng thư trong hội thoại
                if (email.TempAttachments != null && email.TempAttachments.Count > 0)
                {
                    sb.Append("<div class='attachments'>");
                    foreach (var att in email.TempAttachments)
                    {
                        // Link download gửi message về C#
                        sb.Append($"<a href='#' class='att-link' onclick='window.chrome.webview.postMessage(\"DOWNLOAD:{att.Name}\")'>📎 {att.Name}</a>");
                    }
                    sb.Append("</div>");
                }

                sb.Append("</div>"); // End card
            }

            sb.Append("</body></html>");
            return sb.ToString();
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

                // Tìm file trong danh sách attachment của toàn hội thoại này
                MailClient.Attachment attachment = null;

                foreach (var email in _currentConversation)
                {
                    if (email.TempAttachments != null)
                    {
                        attachment = email.TempAttachments.FirstOrDefault(a => a.Name == fileNameToDownload);
                        if (attachment != null) break; // Tìm thấy thì dừng
                    }
                }

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
        private void BlockClick_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Bắt sự kiện click để người dùng không bấm xuyên qua vào nội dung bên dưới
            e.Handled = true;
        }

        // HÀM 1: Hiện Loading (Cho phép truyền nội dung tùy ý)
        public void ShowLoading(string message = "Đang xử lý...")
        {
            if (AppLoadingOverlay != null)
            {
                txtLoadingMessage.Text = message;
                AppLoadingOverlay.Visibility = Visibility.Visible;
            }
        }

        // HÀM 2: Ẩn Loading
        public void HideLoading()
        {
            if (AppLoadingOverlay != null)
            {
                AppLoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void selectall(object sender, RoutedEventArgs e)
        {
            foreach (var email in list.listemail)
            {
                email.IsFlag = true;
            }
        }

        private void deleteselect(object sender, RoutedEventArgs e)
        {

        }
    }
} 