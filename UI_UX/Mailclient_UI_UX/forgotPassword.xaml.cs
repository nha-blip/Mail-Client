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
    /// Interaction logic for forgotPassword.xaml
    /// </summary>
    public partial class forgotPassword : UserControl
    {
        public forgotPassword()
        {
            InitializeComponent();
        }

        private void returnLogin(object sender, RoutedEventArgs e)
        {

            Login? parentWindow = Window.GetWindow(this) as Login;

            if (parentWindow != null)
            {
                parentWindow.ShowLoginView();
            }
        }
    }
}
