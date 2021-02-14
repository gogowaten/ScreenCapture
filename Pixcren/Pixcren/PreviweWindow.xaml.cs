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
using System.Windows.Shapes;

namespace Pixcren
{
    /// <summary>
    /// PreviweWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class PreviweWindow : Window
    {
        private MainWindow MyMainWindow;
        public PreviweWindow(MainWindow main)
        {
            InitializeComponent();

            MyMainWindow = main;
            this.VisualBitmapScalingMode = BitmapScalingMode.Fant;

            Closed += PreviweWindow_Closed;
            this.Loaded += PreviweWindow_Loaded;

        }


        //起動直後
        private void PreviweWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //表示位置をメインウィンドウの少し下にする
            var main = MyMainWindow.PointToScreen(new Point());
            this.Top = main.Y + 130;
            this.Left = main.X;
        }


        private void PreviweWindow_Closed(object sender, EventArgs e)
        {
            MyMainWindow.MyPreviweWindow = null;
        }

        //クリップボードに画像コピー
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var bmp = (BitmapSource)MyImage.Source;
            if (bmp == null) return;
            try
            {
                DataObject data = new();
                data.SetData(typeof(BitmapSource), bmp);
                PngBitmapEncoder png = new();
                png.Frames.Add(BitmapFrame.Create(bmp));
                using var ms = new System.IO.MemoryStream();
                png.Save(ms);
                data.SetData("PNG", ms);
                Clipboard.SetDataObject(data, true);

                var ima = DateTime.Now;
                MyStatusBarItem.Content = $"コピーした({ima:yyyyMMdd_hhmmss})";
            }
            catch (Exception ex)
            {
                var ima = DateTime.Now;
                MyStatusBarItem.Content = $"コピーできなかった({ima:yyyyMMdd_hhmmss})";
                MessageBox.Show($"なんかのエラーでコピーできなかった\n{ex}", "エラー発生");
            }

        }

        //表示切り替え、原寸とウィンドウサイズに合わせる
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (MyScrollViewer.Content == null)
            {
                MyDockPanel.Children.Remove(MyImage);
                MyScrollViewer.Content = MyImage;
                MyImage.Stretch = Stretch.None;
            }
            else
            {
                MyScrollViewer.Content = null;
                MyDockPanel.Children.Add(MyImage);
                MyImage.Stretch = Stretch.Uniform;
            }
        }
    }
}
