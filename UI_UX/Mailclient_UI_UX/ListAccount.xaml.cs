using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// Interaction logic for ListAccount.xaml
    /// </summary>
    public partial class ListAccount : UserControl
    {

        public ObservableCollection<MailClient.Account> UiAccounts { get; set; }

        public ListAccount()
        {
            InitializeComponent();
            UiAccounts = new ObservableCollection<MailClient.Account>();

            // Dòng này quan trọng để Binding hoạt động
            AccountListView.ItemsSource = UiAccounts;

            this.DataContext = this;
        }
        public void AddNewAccountToUI(MailClient.Account acc)
        {
            if (acc != null && UiAccounts != null)
            {
                // Thêm vào danh sách -> Giao diện tự động hiện thêm dòng mới
                UiAccounts.Add(acc);
            }
        }

        private void addAcc(object sender, RoutedEventArgs e)
        {
            Logingg login = new Logingg();
            login.Show();
        }
    }
}
