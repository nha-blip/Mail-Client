using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
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
    /// Interaction logic for setting.xaml
    /// </summary>
    public partial class setting : UserControl
    {
        public setting()
        {
            InitializeComponent();
        }
        private void Color_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn?.Tag != null)
            {
                // 1. Lấy mã màu từ nút bấm trong bảng Setting
                string colorHex = btn.Tag.ToString();
                Color newColor = (Color)ColorConverter.ConvertFromString(colorHex);
                SolidColorBrush newBrush = new SolidColorBrush(newColor);

                // 2. Lấy cửa sổ MainWindow hiện tại
                var mainWin = Application.Current.MainWindow as MainWindow;

                if (mainWin != null)
                {

                    // 1. Nút Compose (x:Name="btncompose")
                    if (mainWin.btncompose != null)
                    {
                        mainWin.btncompose.Background = newBrush;
                    }

                    // 2. Nút Account (x:Name="OpAccount")
                    if (mainWin.OpAccount != null)
                    {
                        mainWin.OpAccount.Background = newBrush;
                    }
                    if (mainWin.pcbar != null)
                    {
                        mainWin.pcbar.Foreground = newBrush;
                    }
                }
            }
        }
        public static BitmapImage LoadImageFixOrientation(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);

            var frame = BitmapFrame.Create(
                stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            Transform transform = Transform.Identity;

            if (frame.Metadata is BitmapMetadata meta &&
                meta.ContainsQuery("System.Photo.Orientation"))
            {
                ushort orientation = (ushort)meta.GetQuery("System.Photo.Orientation");

                transform = orientation switch
                {
                    3 => new RotateTransform(180),
                    6 => new RotateTransform(90),
                    8 => new RotateTransform(270),
                    _ => Transform.Identity
                };
            }

            BitmapSource source = transform == Transform.Identity
                ? frame
                : new TransformedBitmap(frame, transform);

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(source));

            using var ms = new MemoryStream();
            encoder.Save(ms);
            ms.Position = 0;

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.StreamSource = ms;
            bmp.EndInit();
            bmp.Freeze();

            return bmp;
        }
        private void UploadBg_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpeg;*.jpg)|*.png;*.jpeg;*.jpg";

            if (openFileDialog.ShowDialog() == true)
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin != null)
                {
                    var bitmap = LoadImageFixOrientation(openFileDialog.FileName);
                    mainWin.ChangeAppBackground(bitmap);
                }
            }
        }

        private void ResetBg_Click(object sender, RoutedEventArgs e)
        {
            var mainWin = Application.Current.MainWindow as MainWindow;
            mainWin?.ChangeAppBackground(null);
        }
    }
}
