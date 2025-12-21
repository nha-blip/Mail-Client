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
    /// Interaction logic for Reply.xaml
    /// </summary>
    public partial class Reply : UserControl
    {
        // ObservableCollection để quản lý danh sách file đính kèm (giống Compose)
        private ObservableCollection<AttachmentItem> _attachmentList;

        // Lưu thông tin email gốc để trích dẫn
        private Email _originalEmail;
        private string _originalBodyHtml;
        private string _subject;

        public Reply()
        {
            InitializeComponent();

            // Khởi tạo danh sách đính kèm
            _attachmentList = new ObservableCollection<AttachmentItem>();
            if (lbAttachments != null)
            {
                lbAttachments.ItemsSource = _attachmentList;
            }

            this.Loaded += Reply_Loaded;
        }

        private async void Reply_Loaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= Reply_Loaded;
            await InitializeEditor();
        }

        public void SetReplyInfo(Email originalEmail)
        {
            _originalEmail = originalEmail;

            // Điền người nhận (To) là người gửi của mail gốc           
            if (To != null) To.Text = originalEmail.From;

            // Điền Subject (Thêm tiền tố "Re: " nếu chưa có)
            string subject = originalEmail.Subject ?? "";
            if (!subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase))
            {
                subject = "Re: " + subject;
            }
            _subject = subject;

            // Chuẩn bị nội dung trích dẫn (Quoting)
            // Format theo kiểu Gmail: Hiển thị ngày tháng và người gửi cũ
            string dateStr = originalEmail.DateSent.ToString("g"); // Format ngày ngắn
            string senderName = originalEmail.AccountName ?? originalEmail.From;

            // Tạo khối trích dẫn an toàn
            _originalBodyHtml = $@"
                <br><br>
                <div class='gmail_quote'>
                    On {dateStr}, {senderName} wrote:
                    <blockquote class='gmail_quote' style='margin:0 0 0 .8ex;border-left:1px #ccc solid;padding-left:1ex'>
                        {originalEmail.BodyText}
                    </blockquote>
                </div>";
        }

        private async Task InitializeEditor()
        {
            // Giả sử WebView tên là 'EditorWebView'
            if (EditorWebView == null) return;

            await EditorWebView.EnsureCoreWebView2Async();

            // HTML Editor cơ bản 
            string htmlEditor = @"
                <html>
                <head>
                    <style>
                        ::-webkit-scrollbar {
                            width: 10px;
                            height: 10px;
                        }
                        ::-webkit-scrollbar-track {
                            background: transparent;
                        }
                        ::-webkit-scrollbar-thumb {
                            background-color: #c1c1c1;
                            border-radius: 6px;
                            border: 2px solid #fff; 
                        }
                        ::-webkit-scrollbar-thumb:hover {
                            background-color: #a8a8a8;
                        }
                        body { font-family: 'Arial', sans-serif; font-size: 14px; margin: 10px; }
                        blockquote { color: #505050; margin: 10px 0 10px 10px; border-left: 2px solid #ccc; padding-left: 10px; }
                        img { max-width: 100%; height: auto; }
                    </style>
                    <script>
                        function insertImageAtCursor(base64Data) {
                            document.execCommand('insertImage', false, base64Data);
                        }
                        function setContent(html) {
                            document.body.innerHTML = html;
                        }
                        function appendContent(html) {
                            document.body.insertAdjacentHTML('beforeend', html);
                        }
                    </script>
                </head>
                <body contenteditable='true'>
                    <div id='cursor-target'></div> 
                </body>
                </html>";

            EditorWebView.NavigateToString(htmlEditor);

            // Đợi load xong thì chèn nội dung email cũ vào
            EditorWebView.NavigationCompleted += async (s, e) =>
            {
                if (!string.IsNullOrEmpty(_originalBodyHtml))
                {
                    // Chèn nội dung cũ vào cuối editor
                    string safeJson = JsonSerializer.Serialize(_originalBodyHtml);
                    await EditorWebView.ExecuteScriptAsync($"appendContent({safeJson})");
                }
            };
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {

        }

        private void closecompose(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            // Reset dữ liệu khi đóng
            _attachmentList.Clear();
            To.Text = "";
            if (EditorWebView != null && EditorWebView.CoreWebView2 != null) EditorWebView.Reload();
        }

        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var itemToRemove = button?.Tag as AttachmentItem;
            if (itemToRemove != null)
            {
                _attachmentList.Remove(itemToRemove);
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            // Kiểm tra đăng nhập
            if (App.currentAccountService == null || !App.currentAccountService.IsSignedIn())
            {
                MessageBox.Show("Vui lòng đăng nhập lại.", "Lỗi");
                return;
            }

            try
            {
                // Lấy nội dung HTML từ Editor
                string rawJsonHtml = await EditorWebView.ExecuteScriptAsync("document.body.innerHTML");
                string cleanHtml = JsonSerializer.Deserialize<string>(rawJsonHtml);

                // Tạo object Email
                Email replyEmail = new Email
                {
                    From = App.currentAccountService._userEmail,
                    AccountName = App.currentAccountService._userName, // Hoặc lấy từ biến acc
                    Subject = _subject,
                    BodyText = cleanHtml, // Body đã bao gồm cả phần trích dẫn
                    DateSent = DateTime.Now,
                    AttachmentPaths = _attachmentList.Select(x => x.FilePath).ToList(),
                    InReplyTo = _originalEmail.MessageID,
                    References = string.IsNullOrEmpty(_originalEmail.References)
                        ? _originalEmail.MessageID
                        : _originalEmail.References + " " + _originalEmail.MessageID
                };

                // Xử lý người nhận (cắt chuỗi dấu phẩy/chấm phẩy)
                replyEmail.To = To.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                if (!replyEmail.To.Any())
                {
                    MessageBox.Show("Chưa có người nhận.");
                    return;
                }

                // Gửi thông qua MailService
                // Lưu ý: MailService hiện tại chưa hỗ trợ headers In-Reply-To/References
                // Nhưng gửi như thế này vẫn hoạt động như một email bình thường với tiêu đề Re:
                await App.currentMailService.SendEmailAsync(replyEmail);

                MessageBox.Show("Đã gửi phản hồi thành công!");

                // Đóng cửa sổ reply
                this.Visibility = Visibility.Collapsed;
                _attachmentList.Clear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gửi thất bại: {ex.Message}");
            }
        }

        private void opfile(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Title = "Chọn tệp đính kèm",
                Filter = "All Files|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    if (!_attachmentList.Any(x => x.FilePath == file))
                    {
                        var fileInfo = new FileInfo(file);
                        _attachmentList.Add(new AttachmentItem
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            FileSize = FormatBytes(fileInfo.Length)
                        });
                    }
                }
            }
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Multiselect = false,
                Title = "Chọn hình ảnh",
                Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                InsertImageToEditor(openFileDialog.FileName);
            }
        }

        private void maximize(object sender, MouseButtonEventArgs e)
        {

        }

        private async void InsertImageToEditor(string filePath)
        {
            try
            {
                byte[] imageBytes = File.ReadAllBytes(filePath);
                string base64String = Convert.ToBase64String(imageBytes);
                string extension = System.IO.Path.GetExtension(filePath).Replace(".", "");
                string imgSrc = $"data:image/{extension};base64,{base64String}";

                await EditorWebView.ExecuteScriptAsync($"insertImageAtCursor('{imgSrc}')");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi chèn ảnh: " + ex.Message);
            }
        }

        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1)
            {
                number = number / 1024;
                counter++;
            }
            return $"({number:n0} {suffixes[counter]})";
        }
    }
}
