using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for Information.xaml
    /// </summary>
    public partial class Information : UserControl
    {
        public Information()
        {
            InitializeComponent();
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Cấu hình để mở link bằng trình duyệt mặc định của Windows
                var psi = new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true // Bắt buộc phải là true trong .NET Core/.NET 5+
                };
                Process.Start(psi);

                // Đánh dấu sự kiện đã được xử lý để không lan truyền tiếp
                e.Handled = true;
            }
            catch
            {
                MessageBox.Show("link không tồn tại hoặc đã sửa đổi!!");
            }
        }

        private void lienhe(object sender, RoutedEventArgs e)
        {
            var mainWindow = Application.Current.MainWindow as MainWindow;
            mainWindow.lienhe();
        }
    }
}
