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
            var listView = sender as ListView;
            DatabaseHelper dp=new DatabaseHelper();
            var selectedAcc = listView.SelectedItem as MailClient.Account;
            if (selectedAcc == null || selectedAcc.AccountID == dp.GetCurrentAccountID()) return;

            var mainWindow = Application.Current.MainWindow as MainWindow;

            try
            {
                mainWindow.syncTimer.Stop();

                // 1. HỦY TIẾN TRÌNH CŨ NGAY LẬP TỨC
                App.GlobalSyncCts.Cancel();
                App.GlobalSyncCts.Dispose();
                App.GlobalSyncCts = new CancellationTokenSource();
                await Task.Delay(200);
                // 2. CẬP NHẬT ID NGAY ĐỂ CHẶN CÁC LỆNH LƯU DB CỦA ACC CŨ
                dp.SetCurrentAccountID(selectedAcc.AccountID);

                mainWindow.CloseAccountPopup();
                mainWindow.ShowLoading("Đang chuyển tài khoản...");

                // 3. XÓA SẠCH RAM & UI CŨ
                if (mainWindow.list != null)
                {
                    mainWindow.list.listemail.Clear(); // Xóa thư cũ trong bộ nhớ
                }
                mainWindow.MyEmailList.ItemsSource = null; // Ngắt kết nối giao diện tạm thời

                // 4. CHUẨN BỊ TÀI KHOẢN MỚI
                Account fullAccount = new Account(selectedAcc.AccountID);
                App.currentAccountService = new AccountService();
                await App.currentAccountService.LoadCredentialAsync(fullAccount.TokenJson);
                App.currentMailService = new MailService(App.currentAccountService);

                if (App.currentAccountService.IsSignedIn())
                {
                    // Reset trạng thái sync
                    mainWindow.isSyncing = false;

                    // Khởi tạo list mới và gán lại UI
                    Console.WriteLine(dp.GetCurrentAccountID());
                    mainWindow.list = new ListEmail(dp.GetCurrentAccountID());
                    mainWindow.MyEmailList.ItemsSource = mainWindow.list.listemail;

                    Console.WriteLine("da tao xong list");
                    //await mainWindow.SyncAndReload();
                }
                else
                {
                    MessageBox.Show("Phiên đăng nhập hết hạn.");
                }
            }
            catch (Exception ex)
            {
                if (!(ex is OperationCanceledException))
                    MessageBox.Show($"Lỗi: {ex.Message}");
            }
            finally
            {
                mainWindow.HideLoading();
                if (mainWindow.syncTimer != null) mainWindow.syncTimer.Start();
                listView.SelectedIndex = -1;
            }
        }
        public void Logout(object sender, EventArgs e)
        {
            MessageBoxResult result=MessageBox.Show("Đăng xuất",
                                                "Bạn có chắc muốn đăng xuất",
                                                MessageBoxButton.YesNo,
                                                MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;
            DatabaseHelper db= new DatabaseHelper();
            Account acc = new Account(db.GetCurrentAccountID());
            acc.DeleteAccount();
            var mainWindow = Application.Current.MainWindow as MainWindow;
            Logingg log = new Logingg();
            Application.Current.MainWindow = log;
            log.Show();
            mainWindow.Close();
        }
    }
}