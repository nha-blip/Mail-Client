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
    /// Interaction logic for Reply.xaml
    /// </summary>
    public partial class Reply : UserControl
    {
        // 1. Tạo biến lưu thông tin email cần trả lời
        private string _toEmail;
        private string _subject;

        public Reply()
        {
            InitializeComponent();
        }

        // 2. Tạo hàm để MainWindow gọi và truyền dữ liệu vào
        public void SetReplyInfo(string to, string subject)
        {
            _toEmail = to;

            // Tự động thêm "Re: " nếu chưa có
            if (!subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase))
            {
                _subject = "Re: " + subject;
            }
            else
            {
                _subject = subject;
            }

            // (Optional) Reset nội dung soạn thảo mỗi khi mở email mới
            if (EditorWebView != null && EditorWebView.CoreWebView2 != null)
            {
                EditorWebView.NavigateToString("");
            }
        }

        private void Minimize(object sender, RoutedEventArgs e)
        {

        }

        private void closecompose(object sender, RoutedEventArgs e)
        {
            this.Visibility = Visibility.Collapsed;
        }

        private void RemoveAttachment_Click(object sender, RoutedEventArgs e)
        {

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

        private void maximize(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
