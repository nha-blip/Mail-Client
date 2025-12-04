using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

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
        private void AccountListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var selectedAcc = listView.SelectedItem as MailClient.Account;

            if (selectedAcc != null)
            {
                MessageBox.Show($"Bạn đã chọn ID: {selectedAcc.AccountID} - Email: {selectedAcc.Email}");
                App.CurrentAccountID = selectedAcc.AccountID;
                var mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;

                // 2. Kiểm tra xem có lấy được không (để tránh lỗi null)
                if (mainWindow != null)
                {
                    // 3. Gọi hàm public của nó
                    mainWindow.SyncAndReload();
                }

                // Reset lại lựa chọn để lần sau click lại vẫn nhận
                listView.SelectedItem = null;
            }
        }
    }
}