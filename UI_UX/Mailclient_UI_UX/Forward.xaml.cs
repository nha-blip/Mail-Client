using MailClient;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
    /// Interaction logic for Forward.xaml
    /// </summary>
    public partial class Forward : UserControl
    {
        // Quản lý danh sách file đính kèm
        private ObservableCollection<AttachmentItem> _attachmentList;
        private string _forwardHeaderHtml;
        private string _subject;

        public Forward()
        {
            InitializeComponent();

            _attachmentList = new ObservableCollection<AttachmentItem>();
            if (lbAttachments != null)
            {
                lbAttachments.ItemsSource = _attachmentList;
            }

            this.Loaded += Forward_Loaded;
        }

        private async void Forward_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= Forward_Loaded;
            await InitializeEditor();
        }

        public void SetForwardInfo(Email originalEmail)
        {
            // Reset Form
            if (txtTo != null) txtTo.Text = ""; // Forward thì người nhận để trống
            _attachmentList.Clear();

            // Điền Subject (Thêm tiền tố "Fwd: " nếu chưa có)
            string subject = originalEmail.Subject ?? "";
            if (!subject.StartsWith("Fwd:", StringComparison.OrdinalIgnoreCase))
            {
                subject = "Fwd: " + subject;
            }
            _subject = subject;

            // Sao chép File đính kèm từ email cũ 
            if (originalEmail.AttachmentPaths != null)
            {
                foreach (string path in originalEmail.AttachmentPaths)
                {
                    if (File.Exists(path))
                    {
                        var fi = new FileInfo(path);
                        _attachmentList.Add(new AttachmentItem
                        {
                            FilePath = path,
                            FileName = fi.Name,
                            FileSize = FormatBytes(fi.Length)
                        });
                    }
                }
            }

            // Tạo Header trích dẫn chuẩn cho Forward
            string dateStr = originalEmail.DateSent.ToString("g");
            string fromStr = originalEmail.From;
            string toStr = string.Join(", ", originalEmail.To);
            string subjectStr = originalEmail.Subject;

            _forwardHeaderHtml = $@"
                <br><br>
                <div class=""gmail_quote"">
                    ---------- Forwarded message ---------<br>
                    From: <strong class=""gmail_sendername"" dir=""auto"">{originalEmail.AccountName}</strong> <span dir=""ltr"">&lt;{fromStr}&gt;</span><br>
                    Date: {dateStr}<br>
                    Subject: {subjectStr}<br>
                    To: {toStr}<br>
                    <br>
                </div>
                {originalEmail.BodyText}";

            // 5. Nạp vào Editor
            if (EditorWebView != null && EditorWebView.CoreWebView2 != null)
            {
                var _ = InitializeEditor(); // Reload lại editor để xóa nội dung cũ
            }

            this.Visibility = Visibility.Visible;
        }

        private async Task InitializeEditor()
        {
            await EditorWebView.EnsureCoreWebView2Async();

            // Setup HTML Editor (Height 100% để sửa lỗi không focus được)
            string htmlEditor = @"
                <html>
                <head>
                    <style>
                        html, body { height: 100%; margin: 0; padding: 10px; font-family: 'Arial', sans-serif; font-size: 14px; outline: none; }
                        blockquote { color: #505050; border-left: 1px solid #ccc; padding-left: 10px; } 
                        img { max-width: 100%; height: auto; }
                    </style>
                    <script>
                        function insertImageAtCursor(base64Data) {
                            document.execCommand('insertImage', false, base64Data);
                        }
                        function setContent(html) {
                            document.body.innerHTML = html;
                        }
                        window.onload = function() { document.body.focus(); };
                    </script>
                </head>
                <body contenteditable='true'>
                    <div id='cursor-target'><br></div> 
                </body>
                </html>";

            EditorWebView.NavigateToString(htmlEditor);

            EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> handler = null;
            handler = (s, e) =>
            {
                EditorWebView.NavigationCompleted -= handler;
                InjectContent();
            };
            EditorWebView.NavigationCompleted += handler;
        }

        private async void InjectContent()
        {
            if (!string.IsNullOrEmpty(_forwardHeaderHtml) && EditorWebView.CoreWebView2 != null)
            {
                // Chèn nội dung cũ vào bên dưới
                string safeJson = JsonSerializer.Serialize(_forwardHeaderHtml);
                // Dùng insertAdjacentHTML 'beforeend' để con trỏ chuột nằm ở đầu thư
                await EditorWebView.ExecuteScriptAsync($"document.body.insertAdjacentHTML('beforeend', {safeJson})");
                _forwardHeaderHtml = null;
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!App.currentAccountService.IsSignedIn())
            {
                MessageBox.Show("Vui lòng đăng nhập lại.");
                return;
            }

            try
            {
                // Lấy nội dung
                string rawJsonHtml = await EditorWebView.ExecuteScriptAsync("document.body.innerHTML");
                string cleanHtml = JsonSerializer.Deserialize<string>(rawJsonHtml);

                Email forwardEmail = new Email
                {
                    From = App.currentAccountService._userEmail,
                    AccountName = App.currentAccountService._userName,

                    // Người nhận lấy từ TextBox nhập tay
                    To = txtTo.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries),

                    Subject = _subject,
                    BodyText = cleanHtml,
                    DateSent = DateTime.Now,
                    AttachmentPaths = _attachmentList.Select(x => x.FilePath).ToList()

                    // LƯU Ý: Forward thường được coi là một email mới hoàn toàn,
                    // nên không nhất thiết phải set InReplyTo hay References 
                    // trừ khi bạn muốn nó nối vào thread cũ trong UI của chính mình.
                };

                if (!forwardEmail.To.Any()) { MessageBox.Show("Chưa nhập người nhận."); return; }

                // Gửi mail
                await App.currentMailService.SendEmailAsync(forwardEmail);

                MessageBox.Show("Đã chuyển tiếp thành công!");
                closeforward(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi: {ex.Message}");
            }
        }

        private void opfile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Multiselect = true, Filter = "All Files|*.*" };
            if (dlg.ShowDialog() == true)
            {
                foreach (string file in dlg.FileNames)
                {
                    if (!_attachmentList.Any(x => x.FilePath == file))
                    {
                        var fi = new FileInfo(file);
                        _attachmentList.Add(new AttachmentItem { FilePath = file, FileName = fi.Name, FileSize = FormatBytes(fi.Length) });
                    }
                }
            }
        }

        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is AttachmentItem item)
            {
                _attachmentList.Remove(item);
            }
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Image Files|*.jpg;*.png;*.bmp" };
            if (dlg.ShowDialog() == true)
            {
                InsertImageToEditor(dlg.FileName);
            }
        }

        private async void InsertImageToEditor(string filePath)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(filePath);
                string base64 = Convert.ToBase64String(bytes);
                string ext = System.IO.Path.GetExtension(filePath).Replace(".", "");
                string src = $"data:image/{ext};base64,{base64}";
                await EditorWebView.ExecuteScriptAsync($"insertImageAtCursor('{src}')");
            }
            catch { }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1) { number /= 1024; counter++; }
            return $"{number:n0} {suffixes[counter]}";
        }

        private void closeforward(object sender, RoutedEventArgs e)
        {
            // Reset dữ liệu
            if (EditorWebView != null && EditorWebView.CoreWebView2 != null) EditorWebView.NavigateToString("");
            if (txtTo != null) txtTo.Text = string.Empty;
            _attachmentList.Clear();

            this.Visibility = Visibility.Collapsed;
        }
    }
}
