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
        }

        // Thêm hàm xử lý khi click vào 1 dòng trong ListView (Thay cho MouseDown cũ)
        private async void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MessageBox.Show("thay doi tai khoan");
            var listView = sender as ListView;
            var selectedAcc = listView.SelectedItem as MailClient.Account;

            if (selectedAcc != null)
            {
                try
                {
                    // --- BƯỚC 1: LẤY FULL THÔNG TIN ACCOUNT (KÈM TOKEN) TỪ DB ---
                    Account fullAccount = new Account(selectedAcc.AccountID);

                    // --- BƯỚC 2: KHỞI TẠO STORE CẦU NỐI ---
                    // Để đưa TokenJson từ fullAccount vào Google API
                    var myStore = new AccountTokenStore(fullAccount);

                    // Đảm bảo biến toàn cục không null
                    App.currentAccountService=new AccountService();
                    await App.currentAccountService.LoadCredentialAsync(fullAccount.TokenJson);
                    App.currentMailService = new MailService(App.currentAccountService);

                    // --- BƯỚC 3: LOGIN LẠI (QUAN TRỌNG NHẤT) ---
                    // Vì fullAccount.TokenJson đã có dữ liệu từ DB, 
                    // hàm này sẽ chạy ngầm (Silent Login) và KHÔNG mở trình duyệt.
                    bool success = App.currentAccountService.IsSignedIn();

                    if (success)
                    {
                        // Cập nhật biến toàn cục sau khi Login thành công
                        App.CurrentAccountID = fullAccount.AccountID;

                        // --- BƯỚC 4: ĐỒNG BỘ GIAO DIỆN ---
                        var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
                        mainWindow.list.listemail.Clear();
                        mainWindow.list.soluong = 0;
                        mainWindow.list._latestDateSent = new DateTime(1789, 1, 1);
                        await mainWindow.SyncAndReload();
                        MessageBox.Show("đã đồng bộ");
                    }
                    else
                    {
                        MessageBox.Show("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi chuyển tài khoản: {ex.Message}");
                }
            }
        }
    }
}