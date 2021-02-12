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
            MyMainWindow = main;
            InitializeComponent();

            Closed += PreviweWindow_Closed;
        }

        private void PreviweWindow_Closed(object sender, EventArgs e)
        {
            MyMainWindow.MyPreviweWindow = null;
        }
    }
}
