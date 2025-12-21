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
    /// Interaction logic for Forward.xaml
    /// </summary>
    public partial class Forward : UserControl
    {
        public Forward()
        {
            InitializeComponent();
        }
        public async void SetForwardContent(string originalBody)
        {
            // Đảm bảo WebView2 đã khởi tạo xong trước khi nạp nội dung
            await EditorWebView.EnsureCoreWebView2Async();

            // Tạo nội dung chuyển tiếp (thường có dòng kẻ hoặc thông tin thư cũ)
            string forwardHeader = "<br><br>---------- Forwarded message ----------<br>";
            string fullContent = forwardHeader + originalBody;

            // Nạp vào WebView2
            EditorWebView.NavigateToString(fullContent);

            // Hiện form lên
            this.Visibility = Visibility.Visible;
        }
        private void Send_Click(object sender, RoutedEventArgs e)
        {

        }

        private void opfile(object sender, RoutedEventArgs e)
        {

        }

        private void InsertImage_Click(object sender, RoutedEventArgs e)
        {

        }

        private void closeforward(object sender, RoutedEventArgs e)
        {
            if (EditorWebView != null && EditorWebView.CoreWebView2 != null)
            {
                EditorWebView.NavigateToString("");
            }
            txtTo.Text = string.Empty;
            this.Visibility = Visibility.Collapsed;
        }
    }
}
