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
    /// WindowDateTimeStringformat.xaml の相互作用ロジック
    /// </summary>
    public partial class WindowDateTimeStringformat : Window
    {
        public WindowDateTimeStringformat(BitmapSource bitmapSource)
        {
            InitializeComponent();
            this.Title = "日時の書式";
            this.Closing += WindowDateTimeStringformat_Closing;
            DateTime now = DateTime.Now;
            List<string> formatList;
            formatList = new List<string>() { "年月日", "y_", "yy", "yyy", "yyyy", "yyyyy", "M (0-12)", "MM (00-12)", "MMM", "MMMM", "d_", "dd", "ddd", "dddd" };
            MyListBoxYMD.DataContext = MakeListItem(now, formatList);
            formatList = new List<string>() { "時分秒ミリ秒", "h (0-12)", "hh (00-12)", "H (0-24)", "HH (00-24)", "m (0-59)", "mm (00-59)", "s (0-59)", "ss (00-59)", "f_", "ff", "fff", "ffff", "fffff", "ffffff", "fffffff", "tt" };
            MyListBoxHMS.DataContext = MakeListItem(now, formatList);
            formatList = new List<string>() { "D", "M", "Y", "gg" };
            MyListBox.DataContext = MakeListItem(now, formatList);

            formatList = new List<string>() { "使えない", "f", "F", "g", "G", "O", "R", "s", "t", "T", "u", "U", "d", "K_" };
            MyListBoxNG.DataContext = MakeListItem(now, formatList);
            formatList = new List<string>() { "いまいち", "F_", "FF", "FFF", "FFFF", "FFFFF", "FFFFFF", "FFFFFFF", "z_", "zz", "t_" };
            MyListBoxNG2.DataContext = MakeListItem(now, formatList);

            //画像の設定
            MyImage.Source = bitmapSource;
            
        }

        //閉じるときにフラグをMainWindowに通知
        private void WindowDateTimeStringformat_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var owner = this.Owner as MainWindow;
            owner.IsDateformatShow = false;
        }

        private Dictionary<string, string> MakeListItem(DateTime time, List<string> formatList)
        {
            var list = new Dictionary<string, string>();
            for (int i = 0; i < formatList.Count; i++)
            {
                list.Add(formatList[i], time.ToString(formatList[i]));
            }
            return list;
        }
        
    }

   
}
