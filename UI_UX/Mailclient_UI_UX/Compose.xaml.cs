using MailClient;
using MailClient.Core.Services;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel; 
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mailclient
{
    /// <summary>
    /// Interaction logic for Compose.xaml
    /// </summary>
    public partial class Compose : UserControl
    {
        bool isminimize = false;
        public Account acc;

        // Dùng ObservableCollection để Binding dữ liệu lên ListBox
        private ObservableCollection<AttachmentItem> _attachmentList;

        public Compose()
        {
            InitializeComponent();
            DatabaseHelper dp = new DatabaseHelper();
            acc = new Account(dp.GetCurrentAccountID());

            // Khởi tạo danh sách và gán nguồn dữ liệu cho ListBox
            _attachmentList = new ObservableCollection<AttachmentItem>();
            lbAttachments.ItemsSource = _attachmentList;

            this.Loaded += Compose_Loaded;
        }

        private async void Compose_Loaded(object sender, RoutedEventArgs e)
        {
            // Hủy đăng ký để không chạy lại mỗi khi ẩn/hiện
            this.Loaded -= Compose_Loaded;
            await InitializeEditor();
        }

        private async Task InitializeEditor()
        {
            await EditorWebView.EnsureCoreWebView2Async();
            // HTML Editor giữ nguyên như cũ
            string htmlEditor = @"
                <html>
                <head>
                    <style>
                        ::-webkit-scrollbar { width: 8px; height: 8px; }
                        ::-webkit-scrollbar-track { background: transparent; }
                        ::-webkit-scrollbar-thumb { background-color: #c1c1c1; border-radius: 4px; border: 2px solid transparent; background-clip: content-box; }
                        ::-webkit-scrollbar-thumb:hover { background-color: #a8a8a8; }

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
                    <div dir=""ltr""></div>
                </body>
                </html>";

            if (EditorWebView.CoreWebView2 != null)
            {
                EditorWebView.NavigateToString(htmlEditor);
            }
        }

        private void closecompose(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
            // Reset dữ liệu khi đóng
            _attachmentList.Clear();
            To.Text = "";
            Subject.Text = "";
            if (EditorWebView != null && EditorWebView.CoreWebView2 != null) EditorWebView.Reload();
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            isminimize = true;
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
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Chọn tệp đính kèm";
            openFileDialog.Filter = "All Files|*.*";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    // Kiểm tra nếu file chưa có trong danh sách thì mới thêm
                    if (!_attachmentList.Any(x => x.FilePath == file))
                    {
                        var fileInfo = new FileInfo(file);

                        // Tạo đối tượng AttachmentItem để hiển thị
                        var item = new AttachmentItem
                        {
                            FilePath = file,
                            FileName = fileInfo.Name,
                            FileSize = FormatBytes(fileInfo.Length) // Tính dung lượng (KB/MB)
                        };

                        _attachmentList.Add(item);
                    }
                }
            }
        }

        // Hàm này được gọi từ sự kiện Click="RemoveAttachment_Click" trong XAML
        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var itemToRemove = button?.Tag as AttachmentItem; // Lấy đối tượng file từ Tag của nút

            if (itemToRemove != null)
            {
                _attachmentList.Remove(itemToRemove); // Xóa khỏi danh sách -> Giao diện tự cập nhật
            }
        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true;
            openFileDialog.Title = "Chọn hình ảnh để chèn";
            // Chỉ lọc các file ảnh
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp";

            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string file in openFileDialog.FileNames)
                {
                    // Gọi hàm chèn ảnh (Inline)
                    InsertImageToEditor(file);
                }
            }
        }

        private async void InsertImageToEditor(string filePath)
        {
            try
            {
                byte[] imageBytes = System.IO.File.ReadAllBytes(filePath);
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

        // SỬA HÀM GỬI 
        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            if (!App.currentAccountService.IsSignedIn())
            {
                MessageBox.Show("Vui lòng đăng nhập lại.", "Lỗi Xác thực");
                return;
            }

            try
            {
                string rawJsonHtml = await EditorWebView.ExecuteScriptAsync("document.body.innerHTML");
                string cleanHtml = JsonSerializer.Deserialize<string>(rawJsonHtml);

                Email model = new Email();
                model.From = App.currentAccountService._userEmail;
                model.AccountName = acc.Username;
                model.To = To.Text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

                if (!model.To.Any()) { MessageBox.Show("Chưa nhập người nhận."); return; }

                model.Subject = Subject.Text;
                model.BodyText = cleanHtml;
                model.DateSent = DateTime.Now;

                // Lấy danh sách đường dẫn file từ ObservableCollection
                model.AttachmentPaths = _attachmentList.Select(x => x.FilePath).ToList();

                await App.currentMailService.SendEmailAsync(model);

                MessageBox.Show("Email đã gửi thành công!");

                // Reset form
                To.Text = "";
                Subject.Text = "";
                _attachmentList.Clear(); // Xóa sạch danh sách file
                EditorWebView.Reload();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi gửi email: {ex.Message}");
            }
        }

        // Chuyển đổi Bytes sang KB, MB...
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

    public class AttachmentItem
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileSize { get; set; }
    }
}