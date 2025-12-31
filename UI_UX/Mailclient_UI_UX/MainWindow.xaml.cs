using MailClient;
using MailClient.Core.Services;
using ManagedNativeWifi;
using Microsoft.Web.WebView2.Wpf;
using Microsoft.Win32;
using Org.BouncyCastle.Pqc.Crypto.Lms;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
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
using System.Net.NetworkInformation;
namespace Mailclient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MailClient.ListEmail list;
        private SolidColorBrush? colorSelected = (SolidColorBrush)(new BrushConverter().ConvertFrom("#33FFFFFF"));
        public DispatcherTimer syncTimer;
        private string currentFolder = "Inbox";
        public MailClient.ListAccount listAcc;
        private MailClient.Email _currentReadingEmail;
        private MailService mailService;
        private bool isFirstLoad = true;
        private List<MailClient.Email> _currentConversation;
        public bool isSyncing = false;
        private bool _isLoadingOldMail = false;
        public DatabaseHelper db;

        public MainWindow()
        {
            InitializeComponent();
            mailService = App.currentMailService;
            InitializeWebView();
            db = new DatabaseHelper();
            listAcc = new MailClient.ListAccount();
            listAcc.AddAccount(new Account(db.GetCurrentAccountID()));
            list = new MailClient.ListEmail(db.GetCurrentAccountID());

            UpdateUI_CurrentFolder();
            StartEmailSync();
            SyncAndReload();
            StartWifiMonitor();
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                Loaded += async (_, __) =>
                {
                    await contentEmail.EnsureCoreWebView2Async();
                };
            }
        }

        private void UpdateUI_CurrentFolder()
        {
            if (list == null || list.listemail == null) return;

            IEnumerable<MailClient.Email> sourceList;
            // Lọc và hiển thị danh sách
            if (currentFolder == "AllMail")
            {
                sourceList = list.listemail
                                .Where(email => email.FolderName != "Trash" && email.FolderName != "Spam" && email.AccountID == db.GetCurrentAccountID());
            }
            else
            {
                sourceList = list.listemail
                                .Where(email => email.FolderName == currentFolder && email.AccountID == db.GetCurrentAccountID());
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
            // Lấy thông tin tài khoản hiện tại
            int targetAccountID = db.GetCurrentAccountID();
            var currentService = App.currentMailService;
            var token = App.GlobalSyncCts.Token;

            if (isSyncing) return;
            isSyncing = true;

            // Chỉ hiện loading ở lần đầu mở app, các lần sau sync ngầm
            if (isFirstLoad) ShowLoading("Đang tải dữ liệu...");

            try
            {
                if (App.currentAccountService != null && App.currentAccountService.IsSignedIn() && currentService != null)
                {
                    token.ThrowIfCancellationRequested();

                    // --- [LOGIC MỚI BẮT ĐẦU] ---

                    // 1. LƯU TRẠNG THÁI CŨ: Lấy UID của thư mới nhất trong Inbox hiện tại
                    string oldLatestUID = "";
                    var oldInboxTop = list.listemail
                                       .Where(e => e.FolderName == "Inbox" && e.AccountID == targetAccountID)
                                       .OrderByDescending(e => e.DateSent)
                                       .FirstOrDefault();

                    if (oldInboxTop != null) oldLatestUID = oldInboxTop.UID.ToString();

                    // ---------------------------

                    // 2. GỌI SERVER ĐỒNG BỘ (Tải thư mới về DB)
                    await currentService.SyncAllFoldersToDatabase(targetAccountID, token);

                    // 3. KIỂM TRA LẠI: Nếu người dùng chưa đổi tài khoản khác thì mới update UI
                    if (targetAccountID == db.GetCurrentAccountID() && !token.IsCancellationRequested)
                    {
                        // Làm mới danh sách trên RAM
                        list.Refresh(targetAccountID);

                        // Cập nhật giao diện (nếu không đang tìm kiếm)
                        if (string.IsNullOrEmpty(SearchBar.Text))
                        {
                            UpdateUI_CurrentFolder();
                        }

                        // 4. SO SÁNH ĐỂ BẮN THÔNG BÁO
                        // Chỉ thông báo nếu KHÔNG phải lần tải đầu tiên (tránh vừa mở app đã báo)
                        if (!isFirstLoad)
                        {
                            var newInboxTop = list.listemail
                                                  .Where(e => e.FolderName == "Inbox" && e.AccountID == targetAccountID)
                                                  .OrderByDescending(e => e.DateSent)
                                                  .FirstOrDefault();

                            // Nếu có thư mới nhất VÀ nó khác thư cũ
                            if (newInboxTop != null && newInboxTop.UID.ToString() != oldLatestUID)
                            {
                                // Play âm thanh hệ thống (Tùy chọn)
                                System.Media.SystemSounds.Asterisk.Play();

                                // Hiện thông báo
                                ShowNotification("Bạn có thư mới!", $"Từ: {newInboxTop.From}\nTiêu đề: {newInboxTop.Subject}");
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[Sync] Đã hủy tiến trình sync.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi Sync: " + ex.Message);
            }
            finally
            {
                isSyncing = false;
                if (isFirstLoad)
                {
                    HideLoading();
                    isFirstLoad = false;
                }
            }
        }
        private void StartEmailSync()
        {
            syncTimer = new DispatcherTimer();
            syncTimer.Interval = TimeSpan.FromSeconds(15);
            syncTimer.Tick += syncTimer_Tick;
            syncTimer.Start();
        }

        private async void syncTimer_Tick(object sender, EventArgs e)
        {
            await SyncAndReload();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }

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

            // Lấy ID tài khoản hiện tại để lọc
            int currentAccID = db.GetCurrentAccountID();

            if (string.IsNullOrEmpty(searchText))
            {
                UpdateUI_CurrentFolder();
            }
            else
            {
                IEnumerable<MailClient.Email> sourceList;

                if (currentFolder == "AllMail")
                {
                    // SỬA: Thêm điều kiện AccountID
                    sourceList = list.listemail.Where(email =>
                        email.FolderName != "Trash" &&
                        email.FolderName != "Spam" &&
                        email.AccountID == currentAccID);
                }
                else
                {
                    // SỬA: Thêm điều kiện AccountID
                    sourceList = list.listemail.Where(email =>
                        email.FolderName == currentFolder &&
                        email.AccountID == currentAccID);
                }

                // Tìm kiếm (Logic này giữ nguyên, chỉ thêm kiểm tra null an toàn hơn chút)
                var searchResults = sourceList.Where(email =>
                    (email.Subject != null && email.Subject.ToLower().Contains(searchText)) ||
                    (email.AccountName != null && email.AccountName.ToLower().Contains(searchText)) ||
                    (email.From != null && email.From.ToLower().Contains(searchText))
                );

                var groupedResults = searchResults
                    .GroupBy(email => email.ThreadId != 0 ? email.ThreadId : email.UID) // Lưu ý: UID có thể trùng nếu khác folder, nhưng ở đây đã lọc folder/account nên tạm ổn
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
            ResetScrollView();
            CloseEmailView();
            unselectall(null, null);
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
            ResetScrollView();
            CloseEmailView();
            unselectall(null, null);
        }

        private void spam(object sender, RoutedEventArgs e)
        {
            currentFolder = "Spam";
            UpdateUI_CurrentFolder();
            ResetScrollView();
            CloseEmailView();
            unselectall(null, null);
        }

        private void drafts(object sender, RoutedEventArgs e)
        {
            currentFolder = "Draft";
            UpdateUI_CurrentFolder();
            ResetScrollView();
            CloseEmailView();
            unselectall(null, null);
        }

        private void allmail(object sender, RoutedEventArgs e)
        {
            currentFolder = "AllMail";
            UpdateUI_CurrentFolder();
            ResetScrollView();
            CloseEmailView();
            unselectall(null, null);
        }
        private void trash(object sender, RoutedEventArgs e)
        {
            currentFolder = "Trash";
            UpdateUI_CurrentFolder();
            ResetScrollView();
            CloseEmailView();
            unselectall(null, null);
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
            SetComposeButtonState(false);
            composecontent.Visibility = Visibility.Collapsed;
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
                    string finalHtml = "";
                    string innerContent = "";

                    if (_currentReadingEmail.ThreadId != 0)
                    {
                        _currentConversation = list.GetConversation(_currentReadingEmail.ThreadId, _currentReadingEmail.AccountID);

                        // Load attach cho từng mail
                        foreach (var email in _currentConversation)
                            email.TempAttachments = MailClient.Attachment.GetListAttachments(email.emailID);

                        // Tạo nội dung gộp của nhiều thư (Dùng hàm GeneratePartialHtml của Parser cho từng thư)
                        var parser = new EmailParser();
                        StringBuilder sb = new StringBuilder();

                        string clean = System.Text.RegularExpressions.Regex.Replace(_currentReadingEmail.Subject, @"^((Re|Fw|Fwd):\s*)+", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        // Có thể thêm tiêu đề chung của hội thoại
                        sb.Append($"<h2 style='font-weight:400; font-size:22px; margin-bottom:20px'>{clean.Trim()}</h2>");

                        foreach (var email in _currentConversation)
                        {
                            sb.Append(parser.GeneratePartialHtml(email));
                        }
                        innerContent = sb.ToString();
                    }
                    else
                    {
                        _currentConversation = new List<MailClient.Email> { _currentReadingEmail };
                        _currentReadingEmail.TempAttachments = MailClient.Attachment.GetListAttachments(_currentReadingEmail.emailID);

                        var parser = new EmailParser();
                        innerContent = parser.GeneratePartialHtml(_currentReadingEmail);
                    }

                    // 3. BƯỚC GHÉP NỐI: Bọc nội dung vào khung chuẩn
                    finalHtml = ApplyMasterLayout(innerContent);

                    // 4. Lưu và hiển thị
                    string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "email_view.html");
                    System.IO.File.WriteAllText(tempPath, finalHtml);
                    contentEmail.CoreWebView2.Navigate(tempPath);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi hiển thị: " + ex.Message);
                }
            }
        }

        private string ApplyMasterLayout(string bodyContent)
        {
            // Đây là nơi duy nhất bạn chỉnh sửa CSS cho toàn bộ ứng dụng
            return $@"
                <!DOCTYPE html>
            <html lang='vi'>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <style> 
                    ::-webkit-scrollbar {{
                        width: 10px;
                        height: 10px;
                    }}
                    ::-webkit-scrollbar-track {{
                        background: transparent;
                    }}
                    ::-webkit-scrollbar-thumb {{
                        background-color: #c1c1c1;
                        border-radius: 6px;
                        border: 2px solid #fff; 
                    }}
                    ::-webkit-scrollbar-thumb:hover {{
                        background-color: #a8a8a8;
                    }}

                    body {{
                        font-family: 'Google Sans', Roboto, Helvetica, Arial, sans-serif;
                        background-color: #ffffff;
                        margin: 0;
                        padding: 20px;
                        color: #202124;
                        overflow-x: hidden;
                    }}
                    
                    /* CẬP NHẬT CSS CHO EMAIL BODY ĐỂ TRÁNH VỠ KHUNG */
                    .email-body {{
                        font-size: 14px;
                        line-height: 1.5;
                        color: #202124;
                        margin-bottom: 30px;
                        padding-left: 56px;
                        
                        /* QUAN TRỌNG: Ngăn email con tràn ra ngoài hoặc phá vỡ layout */
                        overflow-wrap: break-word; 
                        word-wrap: break-word;
                        max-width: 100%;
                        overflow-x: auto; /* Nếu có bảng quá rộng, hiện thanh cuộn ngang thay vì vỡ layout */
                    }}
                    
                    /* Reset style cho các thành phần bên trong email con */
                    .email-body p {{ margin-bottom: 1em; }}
                    .email-body img {{ max-width: 100%; height: auto; }}

                    .subject-header {{ margin-bottom: 20px; border-bottom: 1px solid transparent; }}
                    .subject-text {{ font-size: 22px; font-weight: 400; margin: 0; line-height: 1.5; color: #1f1f1f; }}
                    .sender-header {{ display: flex; align-items: flex-start; margin-bottom: 20px; }}
                    .avatar-container {{ width: 40px; height: 40px; margin-right: 16px; flex-shrink: 0; }}
                    .avatar-img {{ width: 100%; height: 100%; border-radius: 50%; object-fit: cover; }}
                    .avatar-text {{ width: 100%; height: 100%; border-radius: 50%; color: white; display: flex; align-items: center; justify-content: center; font-size: 18px; font-weight: 500; }}
                    .sender-info {{ flex-grow: 1; display: flex; flex-direction: column; justify-content: center; }}
                    .sender-line-1 {{ display: flex; align-items: baseline; flex-wrap: wrap; }}
                    .sender-name {{ font-weight: 700; font-size: 14px; color: #202124; margin-right: 8px; }}
                    .sender-email {{ font-size: 12px; color: #5f6368; }}
                    .to-me {{ font-size: 12px; color: #5f6368; margin-top: 2px; }}
                    .email-date {{ color: #5f6368; font-size: 12px; margin-left: auto; white-space: nowrap; }}
                    .attachments-area {{ padding-left: 56px; margin-bottom: 30px; border-top: 1px solid #f1f3f4; padding-top: 15px; }}
                    .attachments-title {{ font-weight: 500; color: #5f6368; margin-bottom: 12px; font-size: 13px; }}
                    .attachments-list {{ display: flex; flex-wrap: wrap; gap: 12px; }}
                    .attachment-card {{ display: inline-flex; width: 180px; border: 1px solid #dadce0; border-radius: 8px; overflow: hidden; background-color: #f5f5f5; cursor: pointer; flex-direction: column; transition: box-shadow 0.2s; }}
                    .attachment-card:hover {{ box-shadow: 0 1px 3px rgba(0,0,0,0.2); border-color: #c0c2c5; }}
                    .attachment-preview {{ height: 90px; background-color: #e0e0e0; display: flex; align-items: center; justify-content: center; color: #888; font-size: 16px; font-weight: bold; letter-spacing: 1px; text-transform: uppercase; }}
                    .attachment-footer {{ background-color: white; padding: 10px; border-top: 1px solid #dadce0; }}
                    .att-name {{ font-size: 13px; font-weight: 500; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; color: #3c4043; margin-bottom: 2px; }}
                    .att-size {{ font-size: 11px; color: #5f6368; }}
                    .footer {{ margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #888; text-align: center; }}
                    @media (max-width: 600px) {{ .email-body, .attachments-area {{ padding-left: 0; }} .avatar-container {{ display: none; }} }}
                    
                    /* Thêm hiệu ứng click để người dùng biết là nút bấm được */
                    .attachment-card:active {{
                        transform: scale(0.98);
                        background-color: #e8eaed;
                    }}
                </style>

                <script>
                    function sendDownloadRequest(fileName) {{
                        console.log('User clicked download: ' + fileName); // Log để kiểm tra
        
                        try {{
                            if (window.chrome && window.chrome.webview) {{
                                window.chrome.webview.postMessage('DOWNLOAD:' + fileName);
                            }} else {{
                                alert('Lỗi: Không tìm thấy kết nối tới ứng dụng!');
                            }}
                        }} catch (e) {{
                            console.error('Lỗi khi gửi tin nhắn: ' + e);
                        }}
                    }}
                </script>               

            </head>
            <body>
                {bodyContent}
        
                <div style='margin-top: 40px; padding-top: 20px; border-top: 1px solid #eee; font-size: 12px; color: #888; text-align: center;'>
                    Hiển thị bởi Mail Client
                </div>
            </body>
            </html>";
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
            MyEmailList.SelectionChanged -= content;
            // Ẩn giao diện đọc mail
            mailcontent.Visibility = Visibility.Collapsed;
            reply.Visibility = Visibility.Collapsed;
            SetComposeButtonState(true);
            // Bỏ chọn list
            MyEmailList.SelectedIndex = -1;
            MyEmailList.UnselectAll();
            MyEmailList.SelectionChanged += content;
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

        private void opreply(object sender, RoutedEventArgs e)
        {
            var selectedEmail = MyEmailList.SelectedItem as Email; 

            if (selectedEmail != null)
            {
                forward.Visibility = Visibility.Collapsed;
                reply.DataContext = selectedEmail;
                reply.SetReplyInfo(selectedEmail);
                reply.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một email để trả lời.");
            }
        }

        private void opforward(object sender, RoutedEventArgs e)
        {
            var selectedEmail = MyEmailList.SelectedItem as Email;

            if (selectedEmail != null)
            {
                // Ẩn form Reply (nếu đang mở)
                reply.Visibility = Visibility.Collapsed;
                forward.Visibility = Visibility.Visible;
                forward.SetForwardInfo(selectedEmail);

                // Reset ô nhập người nhận
                forward.txtTo.Text = "";
            }
            else
            {
                MessageBox.Show("Vui lòng chọn một thư để chuyển tiếp!");
            }
        }
        private async void MyEmailList_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SearchBar.Text))
            {
                return;
            }
            if (App.currentAccountService == null || App.currentMailService==null) return;
            var scrollViewer = e.OriginalSource as ScrollViewer;
            if (scrollViewer == null) return;

            // Công thức kiểm tra đã chạm đáy hay chưa:
            // VerticalOffset (Vị trí hiện tại) + ViewportHeight (Chiều cao khung nhìn) == ExtentHeight (Tổng chiều cao)

            // Dùng < 1.0 để so sánh số thực (chấp nhận sai số nhỏ)
            bool isAtBottom = scrollViewer.VerticalOffset + scrollViewer.ViewportHeight >= scrollViewer.ExtentHeight - 1.0;

            if (isAtBottom)
            {
                // Chỉ tải nếu: Không đang tải VÀ Danh sách đang có dữ liệu
                if (!_isLoadingOldMail && MyEmailList.Items.Count > 0)
                {
                    await LoadMoreEmails();
                }
            }
        }

        // Hàm thực hiện logic tải thêm
        private async Task LoadMoreEmails()
        {
            try
            {
                
                _isLoadingOldMail = true;
                ShowLoading("Đang tải thư cũ...");

                // A. LƯU VỊ TRÍ CUỘN HIỆN TẠI
                // Cần tìm ScrollViewer để lấy vị trí
                var scrollViewer = GetScrollViewer(MyEmailList);
                double currentOffset = scrollViewer != null ? scrollViewer.VerticalOffset : 0;

                // B. GỌI SERVICE TẢI THÊM 20 THƯ
                // (Hàm này bạn đã thêm vào MailService ở bước trước)
                await mailService.LoadOlderEmails(db.GetCurrentAccountID(), currentFolder, 20);

                // C. LÀM MỚI DANH SÁCH TRÊN RAM
                list.LoadMoreOldEmails(db.GetCurrentAccountID(), currentFolder);

                // D. CẬP NHẬT GIAO DIỆN
                UpdateUI_CurrentFolder();

                // E. KHÔI PHỤC VỊ TRÍ CUỘN
                // Giúp người dùng không bị đẩy về đầu trang, tiếp tục đọc mượt mà
                if (scrollViewer != null)
                {
                    // Dùng Dispatcher để đảm bảo UI đã render xong mới cuộn
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        scrollViewer.ScrollToVerticalOffset(currentOffset);
                    }, DispatcherPriority.Loaded);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi tải thêm thư: " + ex.Message);
            }
            finally
            {
                HideLoading();
                // Delay nhẹ 0.5s để tránh spam request
                await Task.Delay(500);
                _isLoadingOldMail = false;
            }
        }

        public static ScrollViewer GetScrollViewer(DependencyObject depObj)
        {
            if (depObj is ScrollViewer) return depObj as ScrollViewer;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);
                var result = GetScrollViewer(child);
                if (result != null) return result;
            }
            return null;
        }

        private void ResetScrollView()
        {
            var scrollViewer = GetScrollViewer(MyEmailList);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollToTop();
            }
        }
        private void SetComposeButtonState(bool isEnabled)
        {
            if (btncompose != null)
            {
                btncompose.IsEnabled = isEnabled;
                btncompose.Opacity = isEnabled ? 1.0 : 0.5;
            }
        }
        private void StartWifiMonitor()
        {
            DispatcherTimer wifiTimer = new DispatcherTimer();
            wifiTimer.Interval = TimeSpan.FromSeconds(1); 
            wifiTimer.Tick += (s, e) => UpdateWifiStatus();
            wifiTimer.Start();

            UpdateWifiStatus(); 
        }
        private async Task<long> GetPingAsync()
        {
            try
            {
                Ping myPing = new Ping();
                // Ping thử đến DNS của Google để kiểm tra tốc độ mạng thực tế
                PingReply reply = await myPing.SendPingAsync("8.8.8.8", 1000);

                if (reply.Status == IPStatus.Success)
                {
                    return reply.RoundtripTime; 
                }
            }
            catch { }
            return -1; 
        }
        private async void UpdateWifiStatus()
        {
            try
            {

                var connectedNetwork = NativeWifi.EnumerateConnectedNetworkSsids().FirstOrDefault();
                long pingTime = await GetPingAsync();

                if (connectedNetwork != null)
                {
                    bgwifi.Visibility = Visibility.Visible;

                    var availableNetworks = NativeWifi.EnumerateAvailableNetworks();
                    var currentNet = availableNetworks.FirstOrDefault(n => n.Ssid.ToString() == connectedNetwork.ToString());

                    if (currentNet != null)
                    {
                        int signalQuality = currentNet.SignalQuality;
                        
                        txtWifiStatus.Text = $"{pingTime} ms";

                        if (signalQuality > 80) wifiIcon.Text = "\uE701";
                        else if (signalQuality > 50) wifiIcon.Text = "\uE874";
                        else if (signalQuality > 20) wifiIcon.Text = "\uE873";
                        else wifiIcon.Text = "\uE872"; 
                    }
                }
                else
                {
                    txtWifiStatus.Text = "Disconnected";
                    bgwifi.Visibility = Visibility.Collapsed;
                    wifiIcon.Text = "\uF384";
                }
            }
            catch
            {
                txtWifiStatus.Text = "No Wi-Fi";
            }
        }

        private void selectall(object sender, RoutedEventArgs e)
        {
            // 1. Lấy danh sách ĐANG HIỂN THỊ trên UI (đã qua bộ lọc search/folder)
            var visibleList = MyEmailList.ItemsSource as IEnumerable<MailClient.Email>;

            if (visibleList != null)
            {
                // 2. Duyệt qua từng email đang nhìn thấy
                foreach (var visibleEmail in visibleList)
                {
                    // Đánh dấu chọn email hiển thị
                    visibleEmail.IsFlag = true;

                    // [QUAN TRỌNG - LOGIC GOM NHÓM]
                    // Vì bạn đang hiển thị theo Thread (Hội thoại), một dòng trên UI có thể đại diện cho nhiều email.
                    // Khi chọn dòng này, phải chọn luôn TẤT CẢ email con ẩn bên trong thread đó.
                    if (visibleEmail.ThreadId != 0)
                    {
                        // Tìm trong danh sách gốc các email cùng ThreadID và cùng Account
                        var childEmails = list.listemail.Where(x =>
                                            x.ThreadId == visibleEmail.ThreadId &&
                                            x.AccountID == visibleEmail.AccountID);

                        foreach (var child in childEmails)
                        {
                            child.IsFlag = true;
                        }
                    }
                }
            }

            // 3. Cập nhật giao diện để hiện dấu tick
            MyEmailList.Items.Refresh();
        }

        private void unselectall(object sender, RoutedEventArgs e)
        {
            if (SelectAll != null) SelectAll.IsChecked = false;

            // Cách nhanh nhất để bỏ chọn: Reset toàn bộ danh sách gốc
            // Vì bỏ chọn thừa cũng không sao (không nguy hiểm như chọn thừa để xóa)
            foreach (var email in list.listemail)
            {
                email.IsFlag = false;
            }

            MyEmailList.Items.Refresh();
        }

        private void deletmailselect(object sender, RoutedEventArgs e)
        {
            foreach (var email in list.listemail)
            {
                if(email.IsFlag == true)
                {
                    email.UpdateFolderEmail("Trash");
                    email.FolderName = "Trash";

                    UpdateUI_CurrentFolder();
                    CloseEmailView();
                }
            }
        }

        private void opsetting(object sender, RoutedEventArgs e)
        {
            settingpoup.IsOpen = !settingpoup.IsOpen;
        }
        public void ChangeAppBackground(ImageSource source)
        {
            if (source != null)
            {
                imageBackground.Source = source;
                imageBackground.Visibility = Visibility.Visible;
            }
            else
            {
                // Ẩn ảnh đi để lộ màu nền tối của Window
                imageBackground.Visibility = Visibility.Collapsed;
                imageBackground.Source = null;
            }
        }
        public void ShowNotification(string title, string message)
        {
            // Kiểm tra null để tránh lỗi nếu icon chưa khởi tạo xong
            if (MyNotifyIcon != null)
            {
                // Tham số: Tiêu đề, Nội dung, Icon (Info/Warning/Error)
                MyNotifyIcon.ShowBalloonTip(title, message, Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
            }
        }

        private void OpenApp_Click(object sender, RoutedEventArgs e)
        {
            this.Show(); 
            this.WindowState = WindowState.Normal; 
            this.Activate(); 
        }

        private void ExitApp_Click(object sender, RoutedEventArgs e)
        {

            if (MyNotifyIcon != null)
            {
                MyNotifyIcon.Dispose();
            }

            Application.Current.Shutdown();
        }

        private void OnBalloonClicked(object sender, RoutedEventArgs e)
        {
            // 1. Hiện cửa sổ lên (nếu đang ẩn)
            this.Show();

            // 2. Nếu đang bị thu nhỏ (minimized) thì trả về kích thước bình thường
            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            // 3. Đưa cửa sổ lên trên cùng để người dùng thấy ngay
            this.Activate();
            this.Focus();
        }
    }
}