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

            //書式設定、なぜかXAMLの方では設定が無視されてしまう
            MyStatusBarItemImageCount.ContentStringFormat = "Item数 = 0";

            Closed += PreviweWindow_Closed;
            this.Loaded += PreviweWindow_Loaded;
            this.PreviewKeyDown += PreviweWindow_PreviewKeyDown;//ショートカットキー

        }






        #region ショートカットキー
        private void PreviweWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (Keyboard.Modifiers)
            {
                case ModifierKeys.None:
                    switch (e.Key)
                    {
                        case Key.Delete:
                            RemoveItem();
                            break;

                        default:
                            break;
                    }
                    break;
                case ModifierKeys.Alt:
                    break;
                case ModifierKeys.Control:
                    switch (e.Key)
                    {
                        case Key.A:
                            MyListBox.SelectAll();
                            break;

                        case Key.C:
                            CopyImage();
                            break;

                        case Key.S:
                            SaveImage(MyListBox.SelectedItems);
                            break;

                        default:
                            break;
                    }
                    break;
                case ModifierKeys.Shift:
                    break;
                case ModifierKeys.Windows:
                    break;
                default:
                    break;
            }
        }

        #endregion ショートカットキー

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
            MyMainWindow.MyPreviewItems.Clear();
            MyMainWindow.MyPreviweWindow = null;
            //メモリの解放
            MyGCCollect();
        }

        //クリップボードに画像コピー    
        private void CopyImage()
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

                var item = (PreviewItem)MyListBox.SelectedItem;
                UpdateStatusText($"{item.Name}をクリップボードにコピーした");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"クリップボードにコピーできなかった");
                MessageBox.Show($"なんかのエラーでコピーできなかった、まれによくある\n{ex}", "エラー発生");
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

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var data = (PreviewItem)MyListBox.SelectedItem;
            if (data != null)
            {
                MyImage.Source = data.Image;
            }
            else MyImage.Source = null;
        }

        private void SaveImage(System.Collections.IList list)
        {
            foreach (PreviewItem item in list)
            {
                SaveImage(item);
            }
        }

        private void SaveImage(PreviewItem item)
        {
            if (item.IsSavedDone == true) return;
            try
            {
                MyMainWindow.SaveBitmap(item.Image, item.SavePath);
                item.IsSavedDone = true;
                UpdateStatusText($"{item.SavePath}に保存した");
            }
            catch (Exception ex)
            {
                UpdateStatusText($"{item.SavePath}の保存に失敗した");
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
                int maxId = 0;//削除アイテムの中で最大のインデックス
                foreach (PreviewItem item in removeList)
                {
                    int id = MyMainWindow.MyPreviewItems.IndexOf(item);
                    if (maxId < id) maxId = id;
                    temp.Add(item);
                }

                //削除
                int removeCount = removeList.Count;//削除数、削除する前にここで個数記録
                foreach (var item in temp)
                {
                    MyMainWindow.MyPreviewItems.Remove(item);
                    UpdateStatusText($"{item.Name}を削除した");
                }

                //次の選択アイテム決定
                //削除アイテムの中で一番下の一個下を選択するには
                //削除アイテムの中で最大のインデックス + 1 - 削除数
                int selectId = maxId + 1 - removeCount;
                if (selectId < 0) selectId = 0;
                if (selectId >= MyMainWindow.MyPreviewItems.Count)
                {
                    selectId = MyMainWindow.MyPreviewItems.Count - 1;
                }

                MyListBox.SelectedIndex = selectId;

                //メモリの解放
                MyGCCollect();

            }
        }
        //メモリの解放、これをしないとプレビューウィンドウを開き直してキャプチャするまで残り続ける
        private void MyGCCollect()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        //ステータスバーに表示している文字列を更新
        private void UpdateStatusText(string message)
        {
            MyStatusBarItem.Content = $"{GetNowText()} : {message}";
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


        #region メニュークリック

        //クリップボードに画像コピー
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            CopyImage();
        }

        //画像保存
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //選択アイテムが1つのときと複数では処理を変える
            //1つなら押されたボタンと関係のあるアイテムが対象
            //1つのときは押されたボタンと関連するアイテムと、選択アイテムが別の場合がある
            //複数なら選択されているアイテムが対象


            //選択中のアイテムからリスト作成
            System.Collections.IList saveList = MyListBox.SelectedItems;
            if (saveList == null) return;
            else if (saveList.Count == 1)
            {
                Button b = sender as Button;
                SaveImage((PreviewItem)b?.DataContext);
            }
            else
            {
                SaveImage(saveList);
            }
        }

        //Item削除
        private void MyMenuItemRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveItem();
        }

        #endregion メニュークリック


        #region 右クリックメニュー
        private void MyContextItemCopy_Click(object sender, RoutedEventArgs e)
        {
            CopyImage();
        }

        private void MyContextItemDelete_Click(object sender, RoutedEventArgs e)
        {
            RemoveItem();
        }

        private void MyContextItemSelectAll_Click(object sender, RoutedEventArgs e)
        {
            MyListBox.SelectAll();
        }

        private void MyContextItemSaveImage_Click(object sender, RoutedEventArgs e)
        {
            SaveImage(MyListBox.SelectedItems);
        }

        //画像の右クリックメニューの保存項目をアイテム保存状態に合わせて変更
        private void MyImageContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            var data = MyListBox.SelectedItem as PreviewItem;
            if (data?.IsSavedDone == true)
            {
                //非表示
                MyImageContextMenuSave.Visibility = Visibility.Collapsed;
            }
            else MyImageContextMenuSave.Visibility = Visibility.Visible;
        }


        #endregion 右クリックメニュー


    }






    public class MyConverterButtonVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = (bool)value;
            Visibility v;
            if (b == true)
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
