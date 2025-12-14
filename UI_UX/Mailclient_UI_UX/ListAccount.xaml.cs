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
            var selectedAcc = listView.SelectedItem as MailClient.Account;

            if (selectedAcc != null)
            {
                
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

                try
                {
                    mainWindow.CloseAccountPopup();
                    // 1. HIỆN LOADING (Thêm mới)
                    if (mainWindow != null) mainWindow.ShowLoading("Đang chuyển tài khoản...");

                    // --- BƯỚC 1: LẤY FULL THÔNG TIN ACCOUNT (KÈM TOKEN) TỪ DB ---
                    Account fullAccount = new Account(selectedAcc.AccountID);

                    // --- BƯỚC 2: KHỞI TẠO STORE CẦU NỐI ---
                    var myStore = new AccountTokenStore(fullAccount);

                    // Đảm bảo biến toàn cục không null
                    App.currentAccountService = new AccountService();
                    await App.currentAccountService.LoadCredentialAsync(fullAccount.TokenJson);
                    App.currentMailService = new MailService(App.currentAccountService);

                    // --- BƯỚC 3: LOGIN LẠI (QUAN TRỌNG NHẤT) ---
                    bool success = App.currentAccountService.IsSignedIn();

                    if (success)
                    {
                        // Cập nhật biến toàn cục sau khi Login thành công
                        App.CurrentAccountID = fullAccount.AccountID;

                        // --- BƯỚC 4: ĐỒNG BỘ GIAO DIỆN ---
                        if (mainWindow != null)
                        {
                            mainWindow.list.listemail.Clear();
                            mainWindow.list.soluong = 0;
                            mainWindow.list._latestDateSent = new DateTime(1789, 1, 1);

                            mainWindow.ShowLoading("Đang tải dữ liệu...");
                            await mainWindow.SyncAndReload();
                            
                            mainWindow.HideLoading();
                        }
                    }
                    else
                    {
                        mainWindow.HideLoading(); 
                        MessageBox.Show("Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.");
                    }
                }
                catch (Exception ex)
                {
                    if (mainWindow != null) mainWindow.HideLoading(); 
                    MessageBox.Show($"Lỗi chuyển tài khoản: {ex.Message}");
                }
                finally
                {
                    if (mainWindow != null) mainWindow.HideLoading();

                    // Reset lựa chọn để lần sau click lại vẫn nhận (Optional)
                    listView.SelectedIndex = -1;
                }
            }
        }
    }
}