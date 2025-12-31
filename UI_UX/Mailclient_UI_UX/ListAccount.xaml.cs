using MailClient;
using MailClient.Core.Services;
using System;
using System.Collections.ObjectModel;
using System.Threading;      // Thêm cái này cho CancellationTokenSource
using System.Threading.Tasks;// Thêm cái này cho Task.Delay
using System.Windows;
using System.Windows.Controls;

namespace Mailclient
{
    public partial class ListAccount : UserControl
    {
        // Danh sách này CHỈ CHỨA DỮ LIỆU (Model)
        public ObservableCollection<MailClient.Account> UiAccounts { get; set; }

        // Khai báo DatabaseHelper dùng chung cho tiện
        private DatabaseHelper dbHelper = new DatabaseHelper();

        public ListAccount()
        {
            InitializeComponent();

            // 1. Khởi tạo danh sách rỗng
            UiAccounts = new ObservableCollection<MailClient.Account>();

            // 2. Gán DataContext
            this.DataContext = this;

            // 3. Liên kết ListView
            AccountListView.ItemsSource = UiAccounts;

            // 4. Load dữ liệu và cập nhật trạng thái Active
            LoadAccounts();
        }

        // Tách hàm load ra cho gọn
        private void LoadAccounts()
        {
            MailClient.ListAccount dataHelper = new MailClient.ListAccount();

            if (dataHelper.listAccount != null)
            {
                foreach (MailClient.Account acc in dataHelper.listAccount)
                {
                    UiAccounts.Add(acc);
                }
            }

            // [QUAN TRỌNG] Gọi hàm này ngay khi mở app để tô màu acc đang dùng
            UpdateActiveStatus();
        }

        // --- [HÀM MỚI] CẬP NHẬT TRẠNG THÁI ACTIVE ---
        public void UpdateActiveStatus()
        {
            // Lấy ID đang lưu trong Database
            int currentID = dbHelper.GetCurrentAccountID();

            foreach (var acc in UiAccounts)
            {
                // Nếu ID trùng thì Active = true (XAML sẽ tự tô xanh), ngược lại false
                acc.IsActive = (acc.AccountID == currentID);
            }
        }
        // --------------------------------------------

        private void addAcc(object sender, RoutedEventArgs e)
        {
            Logingg login = new Logingg();
            login.Show();
            login.Activate();
        }

        // Xử lý khi chọn tài khoản khác
        private async void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var selectedAcc = listView.SelectedItem as MailClient.Account;

            // Nếu chưa chọn hoặc chọn trúng cái đang Active rồi thì thôi
            if (selectedAcc == null || selectedAcc.IsActive) return;

            var mainWindow = Application.Current.MainWindow as MainWindow;

            try
            {
                mainWindow.syncTimer.Stop();

                // 1. HỦY TIẾN TRÌNH CŨ
                if (App.GlobalSyncCts != null)
                {
                    App.GlobalSyncCts.Cancel();
                    App.GlobalSyncCts.Dispose();
                }
                App.GlobalSyncCts = new CancellationTokenSource();

                await Task.Delay(200);

                // 2. CẬP NHẬT ID TRONG DB
                dbHelper.SetCurrentAccountID(selectedAcc.AccountID);

                // --- [CẬP NHẬT UI LIST NGAY LẬP TỨC] ---
                UpdateActiveStatus();
                // ----------------------------------------

                mainWindow.CloseAccountPopup();
                mainWindow.ShowLoading("Đang chuyển tài khoản...");

                // 3. XÓA SẠCH RAM & UI CŨ
                if (mainWindow.list != null)
                {
                    mainWindow.list.listemail.Clear();
                }
                mainWindow.MyEmailList.ItemsSource = null;

                // 4. CHUẨN BỊ TÀI KHOẢN MỚI
                Account fullAccount = new Account(selectedAcc.AccountID);
                App.currentAccountService = new AccountService();
                await App.currentAccountService.LoadCredentialAsync(fullAccount.TokenJson);
                App.currentMailService = new MailService(App.currentAccountService);

                if (App.currentAccountService.IsSignedIn())
                {
                    mainWindow.isSyncing = false;

                    Console.WriteLine(dbHelper.GetCurrentAccountID());
                    mainWindow.list = new ListEmail(dbHelper.GetCurrentAccountID());
                    mainWindow.MyEmailList.ItemsSource = mainWindow.list.listemail;

                    Console.WriteLine("da tao xong list");
                    // await mainWindow.SyncAndReload(); // Uncomment nếu muốn sync ngay
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
                listView.SelectedIndex = -1; // Bỏ chọn dòng trên UI để không bị màu xám mặc định
            }
        }

        public void Logout(object sender, EventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Đăng xuất",
                                                      "Bạn có chắc muốn đăng xuất?",
                                                      MessageBoxButton.YesNo,
                                                      MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            // Xóa tài khoản hiện tại
            Account acc = new Account(dbHelper.GetCurrentAccountID());
            acc.DeleteAccount();

            // Chuyển về màn hình Login
            var mainWindow = Application.Current.MainWindow as MainWindow;
            Logingg log = new Logingg();
            Application.Current.MainWindow = log;
            log.Show();
            mainWindow.Close();
        }
    }
}