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
    /// Interaction logic for VerificationCode.xaml
    /// </summary>
    public partial class VerificationCode : UserControl
    {
        public VerificationCode()
        {
            InitializeComponent();
        }

        private void returnLogin(object sender, MouseButtonEventArgs e)
        {
            Login? parentWindow = Window.GetWindow(this) as Login;

            // 2. Gọi hàm "ShowLoginView()" của cha
            if (parentWindow != null)
            {
                parentWindow.ShowLoginView();
            }
        }
    }
}
