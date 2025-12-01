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

        public ObservableCollection<MailClient.Account> Accounts { get; set; }

        public ListAccount()
        {
            InitializeComponent();

            this.DataContext = new MailClient.ListAccount();
        }
    }
}
