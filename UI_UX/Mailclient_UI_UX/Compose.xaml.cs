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
    /// Interaction logic for Compose.xaml
    /// </summary>
    public partial class Compose : UserControl
    {
        bool isminimize = false;
        public Compose()
        {
            InitializeComponent();
        }

        private void closecompose(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed; 
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {
            isminimize=true;
            this.Height = 40;
            this.Width = 200;
        }

        private void maximize(object sender, MouseButtonEventArgs e)
        {
            isminimize = false;
            this.Height = 400;
            this.Width = 500;
        }

        private void opfile(object sender, RoutedEventArgs e)
        {

        }
    }
}
