using System;
using System.Collections.Generic;
using System.Globalization;
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
        //private System.Collections.ObjectModel.ObservableCollection<PreviewItem> MyPreviewItems;

        public PreviweWindow(MainWindow main, System.Collections.ObjectModel.ObservableCollection<PreviewItem> items)
        {
            InitializeComponent();

            MyMainWindow = main;
            //MyPreviewItems = items;



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

            //            WPFで、スクリーンの正確な解像度を取得する方法 | // もちぶろ
            //https://slash-mochi.net/?p=3370

            //ウィンドウサイズはデスクトップ解像度の半分
            Matrix displayScale = PresentationSource.FromVisual(MyMainWindow).CompositionTarget.TransformToDevice;
            //var vw = SystemParameters.VirtualScreenWidth;//マルチ画面のときはこれ
            //var psw = SystemParameters.PrimaryScreenWidth;//プリマリの解像度？
            this.Height = SystemParameters.PrimaryScreenHeight * displayScale.M11 / 2;
            this.Width = SystemParameters.PrimaryScreenWidth * displayScale.M22 / 2;


            this.VisualBitmapScalingMode = BitmapScalingMode.Fant;
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


                MyStatusBarItem.Content = $"{GetNowText()} : クリップボードにコピーした";
            }
            catch (Exception ex)
            {
                //var ima = DateTime.Now;
                MyStatusBarItem.Content = $"{GetNowText()} : クリップボードにコピーできなかった";
                MessageBox.Show($"なんかのエラーでコピーできなかった\n{ex}", "エラー発生");
            }

        }
        private string GetNowText()
        {
            var ima = DateTime.Now;
            return ima.ToString("yyyyMMdd_hhmmss");
        }

        //表示切り替え、原寸とウィンドウサイズに合わせる
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            //var v = MyScrollViewer.Visibility;
            //if (v == Visibility.Visible) MyScrollViewer.Visibility = Visibility.Collapsed;
            //else MyScrollViewer.Visibility = Visibility.Visible;

            //if (MyScrollViewer.Content == null)
            //{
            //    MyDockPanel.Children.Remove(MyImage);
            //    MyScrollViewer.Content = MyImage;
            //    MyImage.Stretch = Stretch.None;
            //}
            //else
            //{
            //    MyScrollViewer.Content = null;
            //    MyDockPanel.Children.Add(MyImage);
            //    MyImage.Stretch = Stretch.Uniform;
            //}
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var data = (PreviewItem)MyListBox.SelectedItem;
            if (data != null)
            {
                MyImage.Source = data.Image;
            }
            else MyImage.Source = null;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var b = (Button)sender;
            var data = (PreviewItem)b.DataContext;
            try
            {
                MyMainWindow.SaveBitmap(data.Image, data.SavePath);
                b.Visibility = Visibility.Collapsed;
                //data.IsSavedDone = true;
                MyStatusBarItem.Content = $"{GetNowText()} : {data.SavePath}に保存した";
            }
            catch (Exception ex)
            {
                MyStatusBarItem.Content = $"{GetNowText()} : {data.SavePath}の保存失敗した";
                MessageBox.Show($"セーブできなかった\n{ex}");
            }


        }

        //アイテム削除
        private void RemoveItem()
        {
            //削除するアイテムリスト
            System.Collections.IList removeList = MyListBox.SelectedItems;

            if (removeList == null) return;
            if (removeList.Count == MyMainWindow.MyPreviewItems.Count)
            {
                MyMainWindow.MyPreviewItems.Clear();
            }
            else
            {
                //ListBoxのSelectedItemsで得られるリストはLINQが使えないし、ObservableCollectionにも変換できないし、
                //foreachに使うとエラーになるので、別に新規リスト作成して対応してみた

                //新規アイテムリストに削除リストにないものだけを追加して、元のリストと入れ替えた後、再バインディング
                //再バインディングってのがイマイチな気がする
                //System.Collections.ObjectModel.ObservableCollection<PreviewItem> temp = new();
                //foreach (PreviewItem item in MyMainWindow.MyPreviewItems)
                //{
                //    if (removeList.Contains(item) == false)
                //    {
                //        temp.Add(item);
                //    }
                //}
                //MyMainWindow.MyPreviewItems = temp;
                //this.DataContext = MyMainWindow.MyPreviewItems;

//                [C#] 初心者が陥った例外・問題まとめ - Qiita
//https://qiita.com/nori0__/items/58d97201b479c3556e39

                //削除リストから新規削除アイテムリスト作成、それをもとに普通に削除
                //こっちのほうがエレガントな気がする
                System.Collections.ObjectModel.ObservableCollection<PreviewItem> temp = new();
                foreach (PreviewItem item in removeList)
                {
                    temp.Add(item);
                }

                foreach (var item in temp)
                {
                    MyMainWindow.MyPreviewItems.Remove(item);
                }
            }
        }

        //Item削除
        private void MyMenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveItem();
        }

        private void MyListBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (MyListBox.Items.Count == 0) return;
            int id = MyListBox.SelectedIndex;
            if (e.Delta < 0 && MyListBox.Items.Count > id)
            {
                MyListBox.SelectedIndex++;
            }
            else if (e.Delta > 0 && id > 0)
            {
                MyListBox.SelectedIndex--;
            }
            else
            {
                MyListBox.SelectedIndex = 0;
            }
            MyListBox.ScrollIntoView(MyListBox.Items[MyListBox.SelectedIndex]);
        }

    }






    public class MyConverterButtonVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            Visibility v;
            if (b)
            {
                v = Visibility.Collapsed;
            }
            else
            {
                v = Visibility.Visible;
            }
            return v;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
