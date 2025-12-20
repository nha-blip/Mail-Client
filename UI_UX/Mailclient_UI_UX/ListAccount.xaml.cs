using MailClient;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using MailClient.Core.Services;

namespace Mailclient
{
    public partial class ListAccount : UserControl
    {
        // Danh sách này CHỈ CHỨA DỮ LIỆU (Model), không chứa giao diện
        public ObservableCollection<MailClient.Account> UiAccounts { get; set; }

        public ListAccount()
        {
            InitializeComponent();

            // 1. Khởi tạo danh sách rỗng
            UiAccounts = new ObservableCollection<MailClient.Account>();

            // 2. Gán DataContext để XAML hiểu
            this.DataContext = this;

            // 3. Liên kết ListView với danh sách dữ liệu
            // (Từ giờ, cứ thêm data vào UiAccounts là giao diện tự hiện)
            AccountListView.ItemsSource = UiAccounts;

            // 4. Lấy dữ liệu từ Database
            MailClient.ListAccount dataHelper = new MailClient.ListAccount();

            if (dataHelper.listAccount != null)
            {
                foreach (MailClient.Account acc in dataHelper.listAccount)
                {
                    UiAccounts.Add(acc);
                }
            }
        }

        private void addAcc(object sender, RoutedEventArgs e)
        {
            Logingg login = new Logingg();
            login.Show();
            login.Activate();
        }

        // Thêm hàm xử lý khi click vào 1 dòng trong ListView (Thay cho MouseDown cũ)
        private async void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var selectedAcc = listView.SelectedItem as MailClient.Account;
            if (selectedAcc == null) return;

            var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
            if (mainWindow == null) return;

            try
            {
                // 1. Đóng Popup & Hiện Loading ngay để che màn hình cũ đi
                mainWindow.CloseAccountPopup();
                mainWindow.ShowLoading("Đang chuyển tài khoản...");

                // 2. Chuẩn bị tài khoản mới
                Account fullAccount = new Account(selectedAcc.AccountID);
                App.currentAccountService = new AccountService();
                await App.currentAccountService.LoadCredentialAsync(fullAccount.TokenJson);
                App.currentMailService = new MailService(App.currentAccountService);

                if (App.currentAccountService.IsSignedIn())
                {
                    App.CurrentAccountID = fullAccount.AccountID;

                    // 3. QUAN TRỌNG: TẠO LIST MỚI (Không dùng list cũ để Clear)
                    // Constructor của ListEmail phải RỖNG (không chạy SQL) thì dòng này mới nhanh
                    mainWindow.list = new MailClient.ListEmail(App.CurrentAccountID);

                    // 4. GÁN LIST MỚI VÀO GIAO DIỆN NGAY
                    // Lúc này list mới chưa có thư, nhưng nhờ Loading che đi nên user không thấy trắng
                    mainWindow.MyEmailList.ItemsSource = mainWindow.list.listemail;

                    // 5. GỌI HÀM TẢI DỮ LIỆU (Code mới đã tối ưu Async)
                    // Hàm này sẽ tự lấy thư từ DB và điền vào list
                    await mainWindow.SyncAndReload();
                    mainWindow.HideLoading();
                }
                else
                {
                    MessageBox.Show("Phiên đăng nhập hết hạn.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}");
            }
            finally
            {
                // Tắt Loading -> Lúc này thư đã lên xong -> User thấy danh sách luôn
                mainWindow.HideLoading();
                listView.SelectedIndex = -1;
            }
        }
    }
}