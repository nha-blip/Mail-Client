using MailClient;
using MailClient.Core.Services;
using Microsoft.Win32;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
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
    /// Interaction logic for Compose.xaml
    /// </summary>
    public partial class Compose : UserControl
    {
        bool isminimize = false;
        public GmailStore _store;
        public AccountService accountService;
        public MailService mailService;
        public Account acc;
        private List<string> _attachmentFiles;
        public Compose()
        {
            InitializeComponent();

            // Cần phải khởi tạo _store ở đây. Lưu ý: Store này CHƯA ĐĂNG NHẬP.
            _store = new GmailStore();
            accountService = new AccountService(_store);
            mailService = new MailService(accountService);
            acc = new Account(App.CurrentAccountID); 
            _attachmentFiles = new List<string>();

            InitializeEditor();
        }

        private async void InitializeEditor()
        {
            await EditorWebView.EnsureCoreWebView2Async();

            // Mã HTML cơ bản cho phép chỉnh sửa (contenteditable)
            // Kèm theo hàm Javascript 'insertImageAtCursor' để C# gọi xuống
            string htmlEditor = @"
                <html>
                <head>
                    <style>
                        body { font-family: 'Arial', sans-serif; font-size: 14px; margin: 10px; outline: none; }
                        img { max-width: 100%; height: auto; margin: 5px 0; }
                    </style>
                    <script>
                        function insertImageAtCursor(base64Data) {
                            document.execCommand('insertImage', false, base64Data);
                        }
                    </script>
                </head>
                <body contenteditable='true'>
                    <p><br></p>
                </body>
                </html>";

            EditorWebView.NavigateToString(htmlEditor);
        }

        public void SetAuthenticatedStore(GmailStore authenticatedStore)
        {
            // Cập nhật trường _store bằng đối tượng đã được đăng nhập
            _store = authenticatedStore ?? throw new ArgumentNullException(nameof(authenticatedStore));

            // Khởi tạo lại các service với store mới
            accountService = new AccountService(_store);
            mailService = new MailService(accountService);

        }

        private void closecompose(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed; 
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            isminimize=true;
            this.Height = 40;
            this.Width = 200;
        }

        private void maximize(object sender, MouseButtonEventArgs e)
        {
            isminimize = false;
            this.Height = 400;
            this.Width = 500;
        }

        private void opfile(object sender, RoutedEventArgs e)
        {
            // 1. Khởi tạo hộp thoại chọn file
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // 2. Cho phép chọn nhiều file cùng lúc
            openFileDialog.Multiselect = true;

            // 3. Đặt tiêu đề cho hộp thoại
            openFileDialog.Title = "Chọn tệp đính kèm";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    _attachmentFiles.Add(file);

                    // Thêm tên file vào ListBox giao diện
                    //lbAttachments.Items.Add(System.IO.Path.GetFileName(file));
                }
            }
        }
        // Đảm bảo hàm được đánh dấu là async
        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (_store.Service == null)
            {
                MessageBox.Show("Vui lòng đăng nhập lại.", "Lỗi Xác thực");
                return;
            }

            try
            {
                // Lấy nội dung HTML từ WebView
                // Hàm ExecuteScriptAsync trả về chuỗi JSON (ví dụ: "\"<div>nội dung</div>\"")
                string rawJsonHtml = await EditorWebView.ExecuteScriptAsync("document.body.innerHTML");

                // Cần Deserialize để bỏ dấu ngoặc kép bao quanh
                string cleanHtml = JsonSerializer.Deserialize<string>(rawJsonHtml);

                Email model = new Email();
                model.From = accountService.GetCurrentUserEmail();
                model.AccountName = acc.Username;
                model.To = To.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (!model.To.Any()) { MessageBox.Show("Chưa nhập người nhận."); return; }

                model.Subject = Subject.Text;

                // Gán trực tiếp HTML lấy từ WebView vào BodyText
                model.BodyText = cleanHtml;

                model.DateSent = DateTime.Now;

                // Gán danh sách file đính kèm
                model.AttachmentPaths = new List<string>(_attachmentFiles);

                await mailService.SendEmailAsync(model);

                MessageBox.Show("Email đã gửi thành công!");

                // Reset form
                To.Text = ""; Subject.Text = "";
                _attachmentFiles.Clear(); lbAttachments.Items.Clear();
                EditorWebView.Reload(); // Làm mới trình soạn thảo
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi email: {ex.Message}");
            }
        }
    }
}
