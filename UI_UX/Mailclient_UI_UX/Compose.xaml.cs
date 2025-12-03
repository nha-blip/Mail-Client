using MailClient;
using MailClient.Core.Services;
using Microsoft.Win32;
using Org.BouncyCastle.Utilities.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            // Kiểm tra xem đã triển khai AccountService hay chưa
            if (_store.Service == null)
            {
                MessageBox.Show("Phiên làm việc đã kết thúc hoặc chưa đăng nhập. Vui lòng đăng nhập lại.", "Lỗi Xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // SỬ DỤNG INSTANCE ĐÃ CÓ VÀ ĐÃ ĐĂNG NHẬP
            Email model = new Email();
            // 1. Thu thập và thiết lập dữ liệu cơ bản
            try
            {
                // 1.1. Thiết lập Người gửi (From)
                model.From = accountService.GetCurrentUserEmail();
                model.AccountName = acc.Username;
                // 1.2. Thiết lập Người nhận (To), tách chuỗi bằng dấu phẩy
                // .Split(',') tạo mảng string[], .ToList() chuyển thành List<string>
                model.To = To.Text.Split(","); // Loại bỏ khoảng trắng thừa
                model.AccountName = acc.Username;                   

                // Kiểm tra xem có người nhận nào không
                if (!model.To.Any())
                {
                    MessageBox.Show("Vui lòng nhập ít nhất một địa chỉ người nhận (To).", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }             

                // 1.4. Thiết lập Chủ đề và Nội dung
                model.Subject = Subject.Text;

                string rawContent = Body.Text;
                string htmlContent = rawContent.Replace("\r\n", "<br />").Replace("\n", "<br />");

                // Bọc trong thẻ div để set font mặc định
                model.BodyText = $@"<div style='font-family: Arial, sans-serif; font-size: 14px;'>{htmlContent}</div>";

                // 1.5. Thiết lập Ngày gửi
                model.DateSent = DateTime.Now;

                // 1.6. Thiết lập Tệp đính kèm (Giả sử bạn có List<string> chứa đường dẫn file)
                // Nếu bạn có một danh sách riêng cho đường dẫn file (ví dụ: model.Attachments đã được thêm vào trước đó)
                //model.AttachmentPaths = _attachmentFiles;

                // 2. Gửi Email
                // Bắt đầu thao tác gửi email bất đồng bộ
                await mailService.SendEmailAsync(model);

                // 3. Xử lý thành công
                MessageBox.Show("Email đã được gửi thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                // Bạn có thể thêm code để xóa nội dung form tại đây (ClearForm())
            }
            catch (InvalidOperationException ex)
            {
                // Xử lý lỗi nếu người dùng chưa đăng nhập (từ MailService)
                MessageBox.Show($"Lỗi xác thực: {ex.Message}\nVui lòng đăng nhập lại.", "Lỗi Gửi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                // Xử lý các lỗi khác (lỗi kết nối, lỗi server SMTP, v.v.)
                MessageBox.Show($"Đã xảy ra lỗi trong quá trình gửi email: {ex.Message}", "Lỗi Gửi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            To.Text = "";
            Subject.Text = "";
            Body.Text = "";
        }
    }
}
