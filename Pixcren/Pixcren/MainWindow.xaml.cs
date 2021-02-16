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



using System.Runtime.InteropServices;//Imagingで使っている
using System.Windows.Interop;//CreateBitmapSourceFromHBitmapで使っている
//using System.Windows.Threading;//DispatcherTimerで使っている
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Collections.ObjectModel;
using Microsoft.Win32;

//スクショアプリできた！右クリックメニューを表示したエクセルもキャプチャできる - 午後わてんのブログ
//https://gogowaten.hatenablog.com/entry/2020/12/28/165619
//スクショアプリPixcrenの改善、ファイル名見本更新と日時書式一覧表 - 午後わてんのブログ
//https://gogowaten.hatenablog.com/entry/2020/12/31/182020


namespace Pixcren
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region WindowsAPI^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
        //グローバルホットキー登録用
        private const int WM_HOTKEY = 0x0312;
        [DllImport("user32.dll")]
        private static extern int RegisterHotKey(IntPtr hWnd, int id, int modkyey, int vKey);
        [DllImport("user32.dll")]
        private static extern int UnregisterHotKey(IntPtr hWnd, int id);


        //キーの入力取得
        //[DllImport("user32.dll")]
        //private static extern short GetAsyncKeyState(int vKey);

        //Rect取得用
        public struct RECT
        {
            //型はlongじゃなくてintが正解！！！！！！！！！！！！！！
            //longだとおかしな値になる
            public int left;
            public int top;
            public int right;
            public int bottom;
            public override string ToString()
            {
                return $"横:{right - left:0000}, 縦:{bottom - top:0000}  ({left}, {top}, {right}, {bottom})";
            }
        }
        //座標取得用
        private struct POINT
        {
            public int X;
            public int Y;
            public override string ToString()
            {
                return $"({X}, {Y})";
            }
        }

        //ウィンドウ情報用
        public struct WINDOWINFO
        {
            public int cbSize;
            public RECT rcWindow;
            public RECT rcClient;
            public uint dwStyle;
            public uint dwExStyle;
            public uint dwWindowStatus;
            public uint cxWindowBorders;
            public uint cyWindowBorders;
            public ushort atomWindowType;
            public short wCreatorVersion;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        //ウィンドウ名取得
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWin, StringBuilder lpString, int nMaxCount);

        //最前面ウィンドウのハンドル取得
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        //指定座標にあるウィンドウのハンドル取得
        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT pOINT);

        //ウィンドウのRect取得
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        //ウィンドウのクライアント領域のRect取得
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        //クライアント領域の座標を画面全体での座標に変換
        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hWnd, out POINT lpPoint);

        //DWM（Desktop Window Manager）
        //見た目通りのRectを取得できる、引数のdwAttributeにDWMWA_EXTENDED_FRAME_BOUNDSを渡す
        //引数のcbAttributeにはRECTのサイズ、Marshal.SizeOf(typeof(RECT))これを渡す
        //戻り値が0なら成功、0以外ならエラー値
        [DllImport("dwmapi.dll")]
        private static extern long DwmGetWindowAttribute(IntPtr hWnd, DWMWINDOWATTRIBUTE dwAttribute, out RECT rect, int cbAttribute);

        //ウィンドウ属性
        //列挙値の開始は0だとずれていたので1からにした
        enum DWMWINDOWATTRIBUTE
        {
            DWMWA_NCRENDERING_ENABLED = 1,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,//見た目通りのウィンドウのRect
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_LAST
        };

        //パレントウィンドウ取得
        [DllImport("user32.dll")]
        private static extern IntPtr GetParent(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, GETWINDOW_CMD uCmd);//本当のuCmdはuint型
        enum GETWINDOW_CMD
        {
            GW_CHILD = 5,
            //指定されたウィンドウが親ウィンドウである場合、取得されたハンドルは、Zオーダーの最上位にある子ウィンドウを識別します。それ以外の場合、取得されたハンドルはNULLです。この関数は、指定されたウィンドウの子ウィンドウのみを調べます。子孫ウィンドウは調べません。
            GW_ENABLEDPOPUP = 6,
            //取得されたハンドルは、指定されたウィンドウが所有する有効なポップアップウィンドウを識別します（検索では、GW_HWNDNEXTを使用して最初に見つかったそのようなウィンドウが使用されます）。それ以外の場合、有効なポップアップウィンドウがない場合、取得されるハンドルは指定されたウィンドウのハンドルです。
            GW_HWNDFIRST = 0,
            //取得されたハンドルは、Zオーダーで最も高い同じタイプのウィンドウを識別します。
            //指定されたウィンドウが最上位のウィンドウである場合、ハンドルは最上位のウィンドウを識別します。指定されたウィンドウがトップレベルウィンドウである場合、ハンドルはトップレベルウィンドウを識別します。指定されたウィンドウが子ウィンドウの場合、ハンドルは兄弟ウィンドウを識別します。

            GW_HWNDLAST = 1,
            //取得されたハンドルは、Zオーダーで最も低い同じタイプのウィンドウを識別します。
            //指定されたウィンドウが最上位のウィンドウである場合、ハンドルは最上位のウィンドウを識別します。指定されたウィンドウがトップレベルウィンドウである場合、ハンドルはトップレベルウィンドウを識別します。指定されたウィンドウが子ウィンドウの場合、ハンドルは兄弟ウィンドウを識別します。

            GW_HWNDNEXT = 2,
            //取得されたハンドルは、指定されたウィンドウの下のウィンドウをZオーダーで識別します。
            //指定されたウィンドウが最上位のウィンドウである場合、ハンドルは最上位のウィンドウを識別します。指定されたウィンドウがトップレベルウィンドウである場合、ハンドルはトップレベルウィンドウを識別します。指定されたウィンドウが子ウィンドウの場合、ハンドルは兄弟ウィンドウを識別します。

            GW_HWNDPREV = 3,
            //取得されたハンドルは、指定されたウィンドウの上のウィンドウをZオーダーで識別します。
            //指定されたウィンドウが最上位のウィンドウである場合、ハンドルは最上位のウィンドウを識別します。指定されたウィンドウがトップレベルウィンドウである場合、ハンドルはトップレベルウィンドウを識別します。指定されたウィンドウが子ウィンドウの場合、ハンドルは兄弟ウィンドウを識別します。

            GW_OWNER = 4,
            //取得されたハンドルは、指定されたウィンドウの所有者ウィンドウを識別します（存在する場合）。詳細については、「所有するWindows」を参照してください。
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetAncestor(IntPtr hWnd, AncestorType gaFlags);//本当のgaFlagsはuint型の1 2 3

        //GetAncestorのフラグ用
        enum AncestorType
        {
            GA_PARENT = 1,
            //親ウィンドウを取得します。GetParent関数の場合のように、これには所有者は含まれません。
            GA_ROOT = 2,
            //親ウィンドウのチェーンをたどってルートウィンドウを取得します。
            GA_ROOTOWNER = 3,
            //GetParent によって返された親ウィンドウと所有者ウィンドウのチェーンをたどって、所有されているルートウィンドウを取得します。
        }
        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr hWnd);



        [DllImport("user32.dll")]
        internal static extern int GetWindowInfo(IntPtr hWnd, ref WINDOWINFO info);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        /// <summary>
        /// 指定したWindowの一番上のChildWindowを返す
        /// </summary>
        /// <param name="hWnd">IntPtr.Zeroを指定すると一番上のWindowを返す</param>
        /// <returns>ChildWindowを持たない場合はnullを返す</returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetTopWindow(IntPtr hWnd);

        /// <summary>
        /// 指定したWindowのメニューのハンドルを返す
        /// </summary>
        /// <param name="hWnd">Windowのハンドル</param>
        /// <returns>Windowがメニューを持たない場合はnullを返す</returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetMenu(IntPtr hWnd);

        /// <summary>
        /// キーボードフォーカスを持つWindowのハンドルを返す
        /// </summary>
        /// <returns></returns>
        [DllImport("user32.dll")]
        internal static extern IntPtr GetFocus();

        [DllImport("user32.dll")]
        internal static extern IntPtr GetMenuBarInfo(IntPtr hWnd, MenuObjectId idObject, long idItem, MENUBARINFO pmbi);

        public struct MENUBARINFO
        {
            public long cbSize;
            public RECT rcBar;
            public IntPtr hMenu;
            public bool fBarFocused;
            public bool fFocused;
        }
        public enum MenuObjectId : long
        {
            OBJID_CLIENT = 0xFFFFFFFC,
            OBJID_MENU = 0xFFFFFFFD,
            OBJID_SYSMENU = 0xFFFFFFFF,
        }


        //        GetTitleBarInfo
        //https://forums.codeguru.com/showthread.php?443988-GetTitleBarInfo

        [DllImport("user32.dll")]
        internal static extern bool GetTitleBarInfo(IntPtr hWnd, ref TITLEBARINFO pti);
        public struct TITLEBARINFO
        {
            public int cbSize;
            public RECT rcTitleBar;
            public TitleBarButtonStates rgState;
        }
        public enum TitleState
        {
            STATE_SYSTEM_UNAVAILABLE = 1,
            STATE_SYSTEM_PRESSED = 8,
            STATE_SYSTEM_INVISIBLE = 32768,
            STATE_SYSTEM_OFFSCREEN = 65536,
            STATE_SYSTEM_FOCUSABLE = 1048576,
            STATE_SYSTEM_INVISIBLE_AND_FOCUSABLE = 0x00108000,
        }
        public struct TitleBarButtonStates
        {
            public TitleState TitleBarState;
            public TitleState Reseved;
            public TitleState MinState;
            public TitleState MaxState;
            public TitleState HelpState;
            public TitleState CloseState;
        }







        [DllImport("user32.dll")]
        internal static extern IntPtr GetMenuItemRect(IntPtr hWnd, IntPtr hMenu, uint uItem, out RECT rect);






        //DC取得
        //nullを渡すと画面全体のDCを取得、ウィンドウハンドルを渡すとそのウィンドウのクライアント領域DC
        //失敗した場合の戻り値はnull
        //使い終わったらReleaseDC
        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        //渡したDCに互換性のあるDC作成
        //失敗した場合の戻り値はnull
        //使い終わったらDeleteDC
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        //指定されたDCに関連付けられているデバイスと互換性のあるビットマップを作成
        //使い終わったらDeleteObject
        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int cx, int cy);

        //DCにオブジェクトを指定する、オブジェクトの種類はbitmap、brush、font、pen、Regionなど
        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        //画像転送
        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdc, int x, int y, int cx, int cy, IntPtr hdcSrc, int x1, int y1, uint rop);
        private const int SRCCOPY = 0x00cc0020;
        private const int SRCINVERT = 0x00660046;

        ////
        //[DllImport("user32.dll")]
        //private static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
        //private const uint nFrags_PW_CLIENTONLY = 0x00000001;

        //[DllImport("user32.dll")]
        //private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ho);




        //ウィンドウ系のAPI
        //Windows（Windowsおよびメッセージ）-Win32アプリ | Microsoft Docs
        // https://docs.microsoft.com/en-us/windows/win32/winmsg/windows





        #region マウスカーソル系API
        //マウスカーソル関係

        //[DllImport("user32.dll")]
        //private static extern IntPtr GetCursor();
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        //[DllImport("user32.dll")]
        //private static extern IntPtr DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);
        //[DllImport("user32.dll")]
        //private static extern IntPtr DrawIconEx(IntPtr hDC,
        //                                        int x,
        //                                        int y,
        //                                        IntPtr hIcon,
        //                                        int cxWidth,
        //                                        int cyWidth,
        //                                        int istepIfAniCur,
        //                                        IntPtr hbrFlickerFreeDraw,
        //                                        int diFlags);
        //private const int DI_DEFAULTSIZE = 0x0008;//cxWidth cyWidthが0に指定されている場合に規定サイズで描画する
        //private const int DI_NORMAL = 0x0003;//通常はこれを指定する、IMAGEとMASKの組み合わせ
        //private const int DI_IMAGE = 0x0002;//画像を使用して描画
        //private const int DI_MASK = 0x0001;//マスクを使用して描画
        //private const int DI_COMPAT = 0x0004;//このフラグは無視の意味
        //private const int DI_NOMIRROR = 0x0010;//ミラーリングされていないアイコンとし描画される
        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO pIconInfo);
        struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CURSORINFO pci);
        [StructLayout(LayoutKind.Sequential)]
        struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }
        //[DllImport("user32.dll")]
        //private static extern IntPtr CopyIcon(IntPtr hIcon);
        //[DllImport("user32.dll")]
        //private static extern bool DestroyIcon(IntPtr hIcon);//CopyIcon使ったあとに使う
        #endregion マウスカーソル系


        #endregion コピペ呪文ここまで^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        private string AppDir;//実行ファイルのパス
        private const string APP_CONFIG_FILE_NAME = "config.xml";
        //private BitmapSource MyBitmapScreen;//全画面画像

        private AppConfig MyAppConfig;
        //private int vHotKey;//ホットキーの仮想キーコード

        //マウスカーソル情報
        private POINT MyCursorPoint;//座標        
        private int MyCursorHotspotX;//ホットスポット
        private int MyCursorHotspotY;//ホットスポット
        private BitmapSource MyBitmapCursor;//画像
        private BitmapSource MyBitmapCursorMask;//マスク画像
        private bool IsMaskUse;//マスク画像使用の有無判定用


        //各Rect
        //private List<MyRectInfo> MyRects;
        //private Dictionary<CaptureRectType, MyRectRect> MyRectRects;
        //private Dictionary<CaptureRectType, string> MyDCRectName;
        //private Dictionary<CaptureRectType, Int32Rect> MyDictRectRect;


        //アプリ情報
        private const string APP_NAME = "Pixcren";
        private string AppVersion;

        //ホットキー
        private const int HOTKEY_ID1 = 0x0001;//ID
        private IntPtr MyWindowHandle;//アプリのハンドル

        //キャプチャ時の音
        private System.Media.SoundPlayer MySoundOrder;//指定の音
        private System.Media.SoundPlayer MySoundDefault;//規定の内蔵音源

        //日時の書式ウィンドウ表示してる？
        public bool IsDateformatShow;

        //datetime.tostringの書式、これを既定値にする
        private const string DATE_TIME_STRING_FORMAT = "yyyyMMdd'_'HHmmss'_'fff";
        //日時の書式一覧画像
        private BitmapSource MyDateTimeStringFormatBitmapSource;

        //メニューウィンドウ付きでキャプチャ時に使用
        private const int LOOP_LIMIT = 16;

        //プレビューウィンドウ
        internal PreviweWindow MyPreviweWindow;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            MyInitializeHotKey();
            MyInisializeComboBox();


            //var now = DateTime.Now;
            //now.ToString("")

            //            DateTimeFormatInfo.TimeSeparator プロパティ(System.Globalization) | Microsoft Docs
            //https://docs.microsoft.com/ja-jp/dotnet/api/system.globalization.datetimeformatinfo.timeseparator?view=net-5.0

            //var cul = CultureInfo.CurrentCulture;
            //var dtformat = cul.DateTimeFormat;
            //var dname = cul.DisplayName;
            //var ename = cul.EnglishName;
            //var cname = cul.Name;
            //var tinfo = cul.TextInfo;
            //var culture = CultureInfo.CreateSpecificCulture(cul.Name);
            //var dtfInfo = culture.DateTimeFormat;
            //dtfInfo.TimeSeparator = "_";
            //dtfInfo.DateSeparator = "-";
            //var mySeparate = now.ToString("F", dtfInfo);
            //var mySeparate2 = now.ToString("G", dtfInfo);


            //            カスタム日時形式文字列 | Microsoft Docs
            //https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/custom-date-and-time-format-strings

          

            //実行ファイルのあるディレクトリ取得
            AppDir = Environment.CurrentDirectory;//.NET5より使用可能            

            //鳴らす音設定、内蔵音源セット。指定音源は初期化
            //リソースから取り出す
            MySoundDefault = new System.Media.SoundPlayer(
                System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Pixcren.pekowave2.wav"));
            MySoundOrder = new System.Media.SoundPlayer();

            //日時の書式一覧画像をリソースから取り出して設定
            MyDateTimeStringFormatBitmapSource =
                BitmapFrame.Create(System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(
                    "Pixcren.DatetimeToStringFormat.png"), BitmapCreateOptions.None, BitmapCacheOption.Default);

            //設定ファイルが存在すれば読み込んで適用、なければ初期化して適用
            string configPath = AppDir + "\\" + APP_CONFIG_FILE_NAME;
            if (System.IO.File.Exists(configPath))
            {
                MyAppConfig = LoadConfig(configPath);
            }

            else
            {
                MyAppConfig = new AppConfig();
            }
            this.DataContext = MyAppConfig;


            //ホットキー登録
            ChangeHotKey(MyAppConfig.HotKey, HOTKEY_ID1);

            //
            if (MyAppConfig.DirList.Count == 0)
            {
                MyComboBoxSaveDirectory.Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }


            //実行ファイルのバージョン取得
            var cl = Environment.GetCommandLineArgs();
            AppVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(cl[0]).FileVersion;
            //タイトルをアプリの名前 + バージョン
            this.Title = APP_NAME + AppVersion;


        }

        private void MyComboBoxFileNameText_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            if (string.IsNullOrWhiteSpace(cb.Text)) return;
            //無効なファイル名なら枠色を赤にする
            if (CheckFileNameValidid(cb.Text))
            {
                cb.Foreground = SystemColors.ControlTextBrush;
            }
            else
            {
                cb.Foreground = Brushes.Red;
            }
            //見本ファイル名の表示更新
            UpdateFileNameSample();
        }

        /// <summary>
        /// ファイル名に使える文字列ならtrueを返す
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool CheckFileNameValidid(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            return name.IndexOfAny(invalid) < 0;
        }

        private void MyComboBoxFileNameText_LostFocus(object sender, RoutedEventArgs e)
        {
            UpdateFileNameSample();
        }





        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateFileNameSample();
        }


        //アプリ終了時
        private void MainWindow_Closed(object sender, EventArgs e)
        {
            //設定の保存
            SaveConfig(AppDir + "\\" + APP_CONFIG_FILE_NAME);

            //ホットキーの登録解除
            _ = UnregisterHotKey(MyWindowHandle, HOTKEY_ID1);
            ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;

            //音源開放
            MySoundOrder.Dispose();
        }

        private void MyInisializeComboBox()
        {
            List<double> vs = new() { 0, 1.5, 2.5, 3.5, 5 };
            MyComboBoxFileNameDateOrder.ItemsSource = vs;
            MyComboBoxFileNameSerialOrder.ItemsSource = vs;

            ComboBoxSaveFileType.ItemsSource = Enum.GetValues(typeof(ImageType));
            MyComboBoxCaputureRect.ItemsSource = new Dictionary<CaptureRectType, string>
            {
                { CaptureRectType.Screen, "全画面" },
                { CaptureRectType.Window, "ウィンドウ" },
                { CaptureRectType.WindowClient, "ウィンドウのクライアント領域" },
                { CaptureRectType.UnderCursor, "カーソル下のコントロール" },
                { CaptureRectType.UnderCursorClient, "カーソル下コントロールのクライアント領域" },
                { CaptureRectType.WindowWithMenu, "ウィンドウ特殊1(With枠外メニューウィンドウ)" },
                { CaptureRectType.WindowWithRelatedWindow, "ウィンドウ特殊1+(With関連ウィンドウ)" }
            };


            MyComboBoxHotKey.ItemsSource = Enum.GetValues(typeof(Key));


            MyComboBoxSoundType.ItemsSource = new Dictionary<MySoundPlay, string> {
                { MySoundPlay.None, "鳴らさない"},
                { MySoundPlay.PlayDefault, "既定の音" },
                { MySoundPlay.PlayOrder, "指定した音" }
            };
        }

        #region ホットキー
        private void MyInitializeHotKey()
        {
            MyWindowHandle = new WindowInteropHelper(this).Handle;
            ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;
        }

        //ホットキー動作
        private void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
        {
            if (msg.message != WM_HOTKEY) return;
            else if (msg.wParam.ToInt32() == HOTKEY_ID1)
            {
                //保存ディレクトリ取得、未指定ならマイドキュメントにする。存在しない場合はエラー表示
                string directory = MyComboBoxSaveDirectory.Text;
                if (string.IsNullOrWhiteSpace(directory))
                {
                    directory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                if (System.IO.Directory.Exists(directory) == false)
                {
                    MessageBox.Show($"指定されている保存場所は存在しないので保存できない", "注意");
                    return;
                }
                //キャプチャ処理

                //カーソル座標取得
                GetCursorPos(out MyCursorPoint);

                //カーソル画像取得
                if (MyAppConfig.IsDrawCursor == true)
                {
                    //取得できなかった場合は処理中断
                    if (SetCursorInfo() == false) return;
                }

                //画面全体画像取得
                BitmapSource screen = ScreenCapture();

                //RECT取得
                //Int32Rect rect;
                List<Int32Rect> myRectList = new();
                switch (MyAppConfig.RectType)
                {
                    case CaptureRectType.Screen:
                        //rect = new Int32Rect(0, 0, screen.PixelWidth, screen.PixelHeight);
                        myRectList.Add(new Int32Rect(0, 0, screen.PixelWidth, screen.PixelHeight));
                        break;

                    case CaptureRectType.Window:
                        //ウィンドウRECT
                        //ウィンドウハンドルの取得
                        IntPtr hWnd = GetParentWindowFromForegroundWindow();

                        //見た目通りのWindowRectを取得
                        RECT myRECT;
                        DwmGetWindowAttribute(hWnd,
                                              DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                                              out myRECT,
                                              Marshal.SizeOf(typeof(RECT)));
                        //RECTからクロップ用のInt32Rectを作成、登録
                        myRectList.Add(MakeCroppRectFromRECT(myRECT, screen.PixelWidth, screen.PixelHeight));
                        break;

                    case CaptureRectType.WindowClient:
                        //ウィンドウのクライアント領域のRECT
                        POINT myPOINT;
                        hWnd = GetParentWindowFromForegroundWindow();
                        ClientToScreen(hWnd, out myPOINT);
                        GetClientRect(hWnd, out myRECT);
                        myRectList.Add(MakeCroppRectFromClientRECT(myRECT, myPOINT, screen.PixelWidth, screen.PixelHeight));
                        break;

                    case CaptureRectType.UnderCursor:
                        //カーソル下のコントロールのRECT、WindowTextが無しならGetWindowRect、ありならEXTENDED_FRAMEを使って取得
                        hWnd = WindowFromPoint(MyCursorPoint);
                        var wndText = new StringBuilder(65535);
                        if (GetWindowText(hWnd, wndText, 65535) == 0)
                        {
                            GetWindowRect(hWnd, out myRECT);
                            //DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out myRECT, Marshal.SizeOf(typeof(RECT)));
                        }
                        else
                        {
                            //GetWindowRect(hWnd, out myRECT);
                            DwmGetWindowAttribute(hWnd, DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out myRECT, Marshal.SizeOf(typeof(RECT)));
                        }
                        myRectList.Add(MakeCroppRectFromRECT(myRECT, screen.PixelWidth, screen.PixelHeight));
                        break;

                    case CaptureRectType.UnderCursorClient:
                        //カーソル下のクライアント領域のRECT
                        POINT myPOINT2;
                        hWnd = WindowFromPoint(MyCursorPoint);
                        ClientToScreen(hWnd, out myPOINT2);
                        GetClientRect(hWnd, out myRECT);
                        myRectList.Add(MakeCroppRectFromClientRECT(myRECT, myPOINT2, screen.PixelWidth, screen.PixelHeight));

                        break;

                    case CaptureRectType.WindowWithMenu:
                        //アプリのウィンドウキャプチャで、枠外のメニューウィンドウもキャプチャ
                        //https://gogowaten.hatenablog.com/entry/2021/02/04/150711
                        //myRectList = MakeForeWindowWithMenuWindowRectList().Select(x => new Int32Rect((int)x.X, (int)x.Y, (int)x.Width, (int)x.Height)).ToList();
                        myRectList = GetRectListForeWindowWhitMenu(false)
                            .Select(
                            x => new Int32Rect((int)x.X, (int)x.Y, (int)x.Width, (int)x.Height))
                            .ToList();

                        break;

                    case CaptureRectType.WindowWithRelatedWindow:
                        //メニューウィンドウに加えて、関連ウィンドウもまとめてキャプチャ
                        myRectList = GetRectListForeWindowWhitMenu(true)
                            .Select(
                            x => new Int32Rect((int)x.X, (int)x.Y, (int)x.Width, (int)x.Height))
                            .ToList();

                        break;

                    default:
                        myRectList.Add(new Int32Rect(0, 0, screen.PixelWidth, screen.PixelHeight));
                        break;
                }

                //Rectの修正
                myRectList = FixInt32Rects(myRectList, screen.PixelWidth, screen.PixelHeight);

                //サイズが0のRectを除外
                myRectList = myRectList.Where(x => x.Width > 0 && x.Height > 0).ToList();

                //Rectが一つも取得できなかった場合や、サイズが0なら何もしないで終了、エラーメッセージを出したほうがいい？
                if (myRectList.Count == 0 || myRectList[0].Width == 0) return;

                //保存
                BitmapSource bitmap = MakeBitmapForSave(screen, myRectList);

                //プレビューウィンドウに表示
                if (MyPreviweWindow != null & bitmap != null)
                {
                    MyPreviweWindow.MyImage.Source = bitmap;
                }

                //クリップボードにコピー、BMPとPNG形式の両方
                //BMPはアルファ値が255になってしまう、PNGはアルファ値保持するけど、貼り付けはアプリの対応が必要
                if (MyCheckBoxIsOutputToClipboardOnly.IsChecked == true)
                {
                    try
                    {
                        //BMP
                        DataObject data = new();
                        data.SetData(typeof(BitmapSource), bitmap);
                        //PNG
                        PngBitmapEncoder enc = new();
                        enc.Frames.Add(BitmapFrame.Create(bitmap));
                        using var ms = new System.IO.MemoryStream();
                        enc.Save(ms);
                        data.SetData("PNG", ms);

                        Clipboard.SetDataObject(data, true);//true必須
                        //Clipboard.SetImage(bitmap);
                        PlayMySound();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"クリップボードにコピーできなかった\n" +
                            $"理由は不明、まれに起こる\n\n" +
                            $"エラーメッセージ\n" +
                            $"{ex.Message}", "エラー発生");
                    }
                }
                //ファイルに保存
                else
                {
                    //有効なファイル名なら続行
                    string fileName = MakeFileName();
                    if (CheckFileNameValidid(fileName))
                    {
                        string fullPath = MakeFullPath(directory, MakeFileName(), MyAppConfig.ImageType.ToString());

                        SaveBitmap(bitmap, fullPath);
                        //連番に加算
                        if (MyAppConfig.IsFileNameSerial) AddIncrementToSerial();
                        //音
                        PlayMySound();
                    }
                    else
                    {
                        MessageBox.Show("ファイル名に使えない文字が指定されていたので保存できなかった", "エラー発生");
                    }

                }

            }
        }

        //Rectの修正、デスクトップ領域外になっているのを調整
        private List<Int32Rect> FixInt32Rects(List<Int32Rect> rList, int w, int h)
        {
            List<Int32Rect> rl = new();
            foreach (var item in rList)
            {
                rl.Add(FixInt32Rect(item, w, h));
            }
            return rl;
        }

        private Int32Rect FixInt32Rect(Int32Rect r, int w, int h)
        {
            //値がマイナスなら、長さからマイナス分を切って、座標は0にする
            if (r.X < 0)
            {
                r.Width += r.X;
                r.X = 0;
            }
            if (r.Y < 0)
            {
                r.Height += r.Y;
                r.Y = 0;
            }

            //右端が枠外になる場合、枠外分を切る、切って幅がマイナスなら0にする
            int temp = r.X + r.Width;
            if (temp > w)
            {
                r.Width -= temp - w;
                if (r.Width < 0) r.Width = 0;
            }

            temp = r.Y + r.Height;
            if (temp > h)
            {
                r.Height -= temp - h;
                if (r.Height < 0) r.Height = 0;
            }

            return r;
        }

        #region メニューウィンドウのRect収集

        private List<Rect> GetRectListForeWindowWhitMenu(bool isRelatedParent)
        {
            List<Rect> R = new();

            var fore = GetWindowInfo(GetForegroundWindow());

            //textがなければエクセル系アプリと判断、下層から選別
            if (fore.Text == "")
            {
                MyWidndowInfo rootOwner = GetWindowInfo(
                    GetAncestor(fore.hWnd, AncestorType.GA_ROOTOWNER));
                MyWidndowInfo parent = GetWindowInfo(
                    GetParent(fore.hWnd));
                MyWidndowInfo popup = GetWindowInfo(
                    GetWindow(rootOwner.hWnd, GETWINDOW_CMD.GW_ENABLEDPOPUP));

                //Foreの下層にあるウィンドウハンドルをGetWindowのNEXTで収集
                List<MyWidndowInfo> next = GetWindowInfos(
                    GetCmdWindows(fore.hWnd, GETWINDOW_CMD.GW_HWNDNEXT, LOOP_LIMIT));

                //可視状態のものだけ残す
                next = next.Where(x => x.IsVisible == true).ToList();

                //RootOwnerがForeのRootOwnerと同じものだけ残す
                next = next.Where(x => rootOwner.Text == MyGetWindowText(GetAncestor(x.hWnd, AncestorType.GA_ROOTOWNER))).ToList();

                //見た目通りのRectを取得
                R = next.Select(x => GetWindowRectMitame(x.hWnd)).ToList();

                //ForeNEXTを上から順番にRectを見て、width = 0が見つかったらそれ以降は除外
                R = SelectNoneZeroRects(R);

                //popupウィンドウのRectを追加
                if (popup.Rect.Width != 0)
                {
                    R.Add(GetWindowRectMitame(popup.hWnd));
                }

                //ParentのTextが""ならParentは無いので、代わりにRootOwnerのRectを追加
                if (parent.Text == "")
                {
                    R.Add(GetWindowRectMitame(rootOwner.hWnd));
                }
                //関連ウィンドウを集める場合は、parentをさかのぼって追加
                else if (isRelatedParent)
                {
                    R.AddRange(
                        GetOwnerWindowsInfo(parent.hWnd, LOOP_LIMIT)
                        .Select(x => GetWindowRectMitame(x.hWnd)));
                }
                //関連ウィンドウを集めない場合、
                //ParentのTextがあればダイアログボックスウィンドウが最前面なので、そのRectを追加
                else
                {
                    R.Add(GetWindowRectMitame(parent.hWnd));
                }
            }

            //普通のアプリは、上下層から選別
            else
            {
                GetCursorPos(out POINT cp);
                MyWidndowInfo cursor = GetWindowInfo(WindowFromPoint(cp));

                List<MyWidndowInfo> prev = GetWindowInfos(
                    GetCmdWindows(cursor.hWnd, GETWINDOW_CMD.GW_HWNDPREV, LOOP_LIMIT));

                List<MyWidndowInfo> next = GetWindowInfos(
                    GetCmdWindows(cursor.hWnd, GETWINDOW_CMD.GW_HWNDNEXT, LOOP_LIMIT));

                R = SelectRects(prev).Union(SelectRects(next)).ToList();

                //重なり判定はForegroundのRectと、それ以外のRectを結合したRectで判定する
                //Rectの結合はGeometryGroupを使う
                GeometryGroup gg = new();
                for (int i = 0; i < R.Count; i++)
                {
                    gg.Children.Add(new RectangleGeometry(R[i]));
                }

                //重なり判定
                //重なりがなければメニューウィンドウは開かれていないと判定して
                //収集したRect全破棄
                if (IsOverlapping(gg, new RectangleGeometry(fore.Rect)) == false)
                {
                    R = new();
                }

                if (isRelatedParent)
                {
                    //関連ウィンドウを収集、追加
                    R.AddRange(
                        GetOwnerWindowsInfo(fore.hWnd, LOOP_LIMIT)
                        .Select(x => GetWindowRectMitame(x.hWnd)));
                }
                else
                {
                    //ForeのRectを追加
                    R.Add(GetWindowRectMitame(fore.hWnd));
                }

                //PopupのRectを追加
                MyWidndowInfo popup = GetWindowInfo(
                    GetWindow(fore.hWnd, GETWINDOW_CMD.GW_ENABLEDPOPUP));

                if (popup.Rect.Width != 0) R.Add(GetWindowRectMitame(popup.hWnd));

                var cParent = GetWindowInfo(GetParent(cursor.hWnd));
                var cOwner = GetWindowInfo(GetWindow(cursor.hWnd, GETWINDOW_CMD.GW_OWNER));
                var cRoot = GetWindowInfo(GetAncestor(cursor.hWnd, AncestorType.GA_ROOTOWNER));
                var act = GetWindowInfo(GetActiveWindow());


                //ForegroundのウィンドウRectだけでいい
            }
            return R;
        }
        /// <summary>
        /// 基準になる最初のウィンドウを指定してOwnerをさかのぼって情報収集
        /// </summary>
        /// <param name="hWnd">最初のウィンドウ、これもリストに入る</param>
        /// <param name="count">遡る上限数</param>
        /// <returns></returns>
        private List<MyWidndowInfo> GetOwnerWindowsInfo(IntPtr hWnd, int count)
        {
            List<MyWidndowInfo> infos = new();
            infos.Add(GetWindowInfo(hWnd));
            IntPtr temp = hWnd;
            for (int i = 0; i < count; i++)
            {
                MyWidndowInfo target = GetWindowInfo(GetWindow(temp, GETWINDOW_CMD.GW_OWNER));
                if (target.Text != "")
                {
                    infos.Add(target);
                    temp = target.hWnd;
                }
                else return infos;
            }
            return infos;
        }



        /// <summary>
        /// 最前面ウィンドウと、そのメニューや右クリックメニューウィンドウ群のRectリストを作成
        /// </summary>
        /// <returns></returns>
        //private List<Rect> MakeForeWindowWithMenuWindowRectList()
        //{
        //    List<Rect> result = new();
        //    //最前面アプリのウィンドウハンドル取得
        //    IntPtr fore = GetParentWindowFromForegroundWindow();// GetForegroundWindow();
        //    //var infoFore = GetWindowRectAndText(fore);//確認用
        //    //ForegroundのPopupハンドルとRect取得
        //    IntPtr popup = GetWindow(fore, GETWINDOW_CMD.GW_ENABLEDPOPUP);
        //    Rect popupRect = MyGetWindowRect(popup);
        //    var infoPop = GetWindowRectAndText(popup);//確認用

        //    //Popupが存在する(Rectが0じゃない)場合
        //    if (popupRect != new Rect(0, 0, 0, 0))
        //    {
        //        //メニューウィンドウがなくても、なぜかENABLEDPOPUPが取得できるアプリがある
        //        //そのRectはアプリのウィンドウRectと同じ数値なので、それで判別できる
        //        //座標が違う場合だけNEXTで収集
        //        var foreRect = MyGetWindowRect(fore);
        //        if (popupRect.X == foreRect.X || popupRect.Y == foreRect.Y)
        //        {
        //            //GetForegroundwindowの見た目通りのRectを追加
        //            result.Add(GetWindowRectMitame(fore));
        //        }
        //        else
        //        {
        //            //PopupのNEXT(下にあるウィンドウハンドル)を収集
        //            List<IntPtr> pops = GetCmdWindows(popup, GETWINDOW_CMD.GW_HWNDNEXT, LOOP_LIMIT);
        //            var infoPops = GetWindowRectAndTexts(pops);//確認用               

        //            //必要なRectだけを選別
        //            result = SelectRects(pops, fore);
        //            ////Textを持つウィンドウ以降を除去
        //            ////残ったウィンドウのRect取得
        //            ////ドロップシャドウウィンドウのRectを除去
        //            ////前後のRectが重なっているところまで選択して、以降は除外

        //            //GetForegroundwindowの見た目通りのRectを追加
        //            result.Add(GetWindowRectMitame(fore));
        //        }

        //    }
        //    //Popupが存在しない(Rectが0)場合
        //    else
        //    {
        //        //GetForegroundwindowの見た目通りのRectを追加
        //        Rect foreRect = GetWindowRectMitame(fore);
        //        result.Add(foreRect);

        //        //マウスカーソル下のウィンドウハンドル取得、これを基準にする
        //        GetCursorPos(out POINT cursorP);
        //        IntPtr cursor = WindowFromPoint(cursorP);
        //        //Rect cursorRect = GetWindowRectMitame(cursor);

        //        //カーソル下のウィンドウRectとForegroundのRect重なり判定
        //        //関係あるウィンドウなら、Textがない and Rectが重なっている
        //        //重なりはメニューウィンドウ全域と重なっていればおk判定にする
        //        List<Rect> rs = new();
        //        if (MyGetWindowText(cursor) == "")
        //        {
        //            //基準の上下それぞれのウィンドウハンドル取得
        //            List<IntPtr> prev = GetCmdWindows(cursor, GETWINDOW_CMD.GW_HWNDPREV, LOOP_LIMIT);//上
        //            List<IntPtr> next = GetCmdWindows(cursor, GETWINDOW_CMD.GW_HWNDNEXT, LOOP_LIMIT);//下
        //            //必要なRectだけを選別
        //            List<Rect> rsPrev = SelectRects(prev, fore);
        //            List<Rect> rsNext = SelectRects(next, fore);
        //            //前後のRectリストを統合
        //            rs = rsPrev.Union(rsNext).ToList();
        //        }

        //        //重なり判定はForegroundのRectと、それ以外のRectを結合したRectで判定する
        //        //Rectの結合はGeometryGroupを使う
        //        GeometryGroup gg = new();
        //        for (int i = 0; i < rs.Count; i++)
        //        {
        //            gg.Children.Add(new RectangleGeometry(rs[i]));
        //        }
        //        //重なり判定、重なっていたらForegroundのRect＋それ以外のRect
        //        if (IsOverlapping(gg, new RectangleGeometry(foreRect)))
        //        {
        //            result = result.Union(rs).ToList();
        //        }
        //        //重なっていない場合はメニューウィンドウは開かれていないと判定して
        //        //ForegroundのウィンドウRectだけでいい
        //    }
        //    return result;
        //}



        #region 通常アプリ系のRect取得
        private List<Rect> SelectRects(List<MyWidndowInfo> pList)
        {
            //可視状態のものだけ残す
            pList = pList.Where(x => x.IsVisible == true).ToList();


            //Textを持つウィンドウ以降を除去
            pList = DeleteWithTextWindow(pList);
            //残ったウィンドウの見た目通りのRect取得
            List<Rect> rs = pList.Select(x => GetWindowRectMitame(x.hWnd)).ToList();
            if (rs.Count == 0) return rs;
            //ドロップシャドウウィンドウのRectを除去
            rs = DeleteShadowRect(rs);
            //サイズが0のRectを除去
            rs = rs.Where(x => x.Size.Width != 0 && x.Size.Height != 0).ToList();
            //前後のRectが重なっているところまで選択して、以降は除外
            return SelectOverlappedRect(rs);
        }


        ///// <summary>
        ///// 前後のRectの重なりを判定、重なっていればリストに追加して返す。重なっていないRectが出た時点で終了
        ///// </summary>
        ///// <param name="rList"></param>
        ///// <returns></returns>
        //private List<Rect> SelectOverlappedRect(List<Rect> rList)
        //{
        //    List<Rect> result = new();
        //    if (rList.Count == 0) return result;

        //    result.Add(rList[0]);

        //    //順番に判定
        //    for (int i = 1; i < rList.Count; i++)
        //    {
        //        if (IsOverlapping(rList[i - 1], rList[i]))
        //        {
        //            //重なっていればリストに追加
        //            result.Add(rList[i]);
        //        }
        //        else
        //        {
        //            //途切れたら終了
        //            return result;
        //        }
        //    }
        //    return result;
        //}
        ///// <summary>
        ///// 2つのGeometryが一部でも重なっていたらTrueを返す
        ///// </summary>
        ///// <param name="g1"></param>
        ///// <param name="g2"></param>
        ///// <returns></returns>
        //private bool IsOverlapping(Geometry g1, Geometry g2)
        //{

        //    IntersectionDetail detail = g1.FillContainsWithDetail(g2);
        //    return detail != IntersectionDetail.Empty;
        //    //return (detail != IntersectionDetail.Empty || detail != IntersectionDetail.NotCalculated, detail);
        //}
        ///// <summary>
        ///// 2つのRectが一部でも重なっていたらtrueを返す
        ///// </summary>
        ///// <param name="r1"></param>
        ///// <param name="r2"></param>
        ///// <returns></returns>        
        //private bool IsOverlapping(Rect r1, Rect r2)
        //{
        //    return IsOverlapping(new RectangleGeometry(r1), new RectangleGeometry(r2));
        //}
        ////IntersectionDetail列挙型
        ////Empty             全く重なっていない
        ////FullyContains     r2はr1の領域に完全に収まっている
        ////FullyInside       r1はr2の領域に完全に収まっている
        ////Intersects        一部が重なっている
        ////NotCalculated     計算されません(よくわからん)




        /// <summary>
        /// Textがないものをリストに追加していって、Textをもつウィンドウが出た時点で終了、リストを返す
        /// </summary>
        /// <param name="wList"></param>
        /// <returns></returns>
        private List<MyWidndowInfo> DeleteWithTextWindow(List<MyWidndowInfo> wList)
        {
            for (int i = 0; i < wList.Count; i++)
            {
                if (wList[i].Text != "")
                {
                    wList.RemoveRange(i, wList.Count - i);
                    return wList;
                }
            }

            return wList;
        }

        #endregion 通常アプリ系のRect取得


        //RectのListを順番にwidthが0を探して、見つかったらそれ以降のRectは除外して返す
        private List<Rect> SelectNoneZeroRects(List<Rect> rl)
        {
            List<Rect> r = new();
            for (int i = 0; i < rl.Count; i++)
            {
                if (rl[i].Width == 0)
                {
                    return r;
                }
                else
                {
                    r.Add(rl[i]);
                }
            }
            return r;
        }

        private List<MyWidndowInfo> GetWindowInfos(List<IntPtr> hWnd)
        {
            List<MyWidndowInfo> l = new();
            foreach (var item in hWnd)
            {
                l.Add(GetWindowInfo(item));
            }
            return l;
        }
        private MyWidndowInfo GetWindowInfo(IntPtr hWnd)
        {
            return new MyWidndowInfo()
            {
                hWnd = hWnd,
                Rect = MyGetWindowRect(hWnd),
                Text = MyGetWindowText(hWnd),
                IsVisible = IsWindowVisible(hWnd)
            };

        }

        //指定したAPI.GETWINDOW_CMDを収集、自分自身も含む
        private List<IntPtr> GetCmdWindows
            (IntPtr hWnd, GETWINDOW_CMD cmd, int loopCount)
        {
            List<IntPtr> v = new();
            v.Add(hWnd);//自分自身

            IntPtr temp = GetWindow(hWnd, cmd);
            for (int i = 0; i < loopCount; i++)
            {
                v.Add(temp);
                temp = GetWindow(temp, cmd);
            }
            return v;
        }

        private List<Rect> SelectRects(List<IntPtr> pList, IntPtr app)
        {
            //Textを持つウィンドウ以降を除去
            List<IntPtr> noneText = DeleteWithTextWindow(pList, app);
            //残ったウィンドウの見た目通りのRect取得
            List<Rect> rs = noneText.Select(x => GetWindowRectMitame(x)).ToList();
            //ドロップシャドウウィンドウのRectを除去
            var result = DeleteShadowRect(rs);
            //サイズが0のRectを除去
            result = result.Where(x => x.Size.Width != 0 && x.Size.Height != 0).ToList();
            //前後のRectが重なっているところまで選択して、以降は除外
            return SelectOverlappedRect(result);
        }

        /// <summary>
        /// Textがないものを収集、例外はOWNERが最前面アプリの場合はTextありでも収集、それ以外でTextをもつウィンドウが出た時点で終了、リストを返す
        /// </summary>
        /// <param name="wList"></param>
        /// <param name="app">最前面アプリのウィンドウハンドル</param>
        /// <returns></returns>
        private List<IntPtr> DeleteWithTextWindow(List<IntPtr> wList, IntPtr app)
        {
            List<IntPtr> result = new();
            for (int i = 0; i < wList.Count; i++)
            {
                if (MyGetWindowText(wList[i]) == "" || (app == GetWindow(wList[i], GETWINDOW_CMD.GW_OWNER)))
                {
                    result.Add(wList[i]);
                }
                else
                {
                    return result;
                }
            }

            return result;
        }

        /// <summary>
        /// ドロップシャドウ用のウィンドウを判定して、取り除いて返す。前後のRectのtopleftが同じなら後のRectはドロップシャドウと判定する
        /// </summary>
        /// <param name="rList"></param>
        /// <returns></returns>       
        private List<Rect> DeleteShadowRect(List<Rect> rList)
        {
            List<Rect> result = new();
            result.Add(rList[0]);
            Rect preRect = rList[0];//前Rect
            for (int i = 0; i < rList.Count; i++)
            {
                //リストに加えて
                Rect imaRect = rList[i];//後Rect
                result.Add(imaRect);

                //前後の座標が同じ場合は
                if (imaRect.TopLeft == preRect.TopLeft)
                {
                    //サイズが大きい方を削除
                    if (imaRect.Size.Width < preRect.Size.Width)
                    {
                        result.Remove(rList[i - 1]);
                    }
                    else
                    {
                        result.Remove(rList[i]);
                    }
                }
                preRect = imaRect;//前Rectに後Rectを入れて次へ
            }
            return result;
        }

        //WPFのRectの重なり判定、RectangleGeometryにしてからFillContainsWithDetailメソッドでできた
        //https://gogowaten.hatenablog.com/entry/2021/01/28/124714
        /// <summary>
        /// 前後のRectの重なりを判定、重なっていればリストに追加して返す。重なっていないRectが出た時点で終了
        /// </summary>
        /// <param name="rList"></param>
        /// <returns></returns>
        private List<Rect> SelectOverlappedRect(List<Rect> rList)
        {
            List<Rect> result = new();
            if (rList.Count == 0) return result;

            result.Add(rList[0]);

            //順番に判定
            for (int i = 1; i < rList.Count; i++)
            {
                if (IsOverlapping(rList[i - 1], rList[i]))
                {
                    //重なっていればリストに追加
                    result.Add(rList[i]);
                }
                else
                {
                    //途切れたら終了
                    return result;
                }
            }
            return result;
        }
        /// <summary>
        /// 2つのGeometryが一部でも重なっていたらTrueを返す
        /// </summary>
        /// <param name="g1"></param>
        /// <param name="g2"></param>
        /// <returns></returns>
        private bool IsOverlapping(Geometry g1, Geometry g2)
        {

            IntersectionDetail detail = g1.FillContainsWithDetail(g2);
            return detail != IntersectionDetail.Empty;
            //return (detail != IntersectionDetail.Empty || detail != IntersectionDetail.NotCalculated, detail);
        }
        /// <summary>
        /// 2つのRectが一部でも重なっていたらtrueを返す
        /// </summary>
        /// <param name="r1"></param>
        /// <param name="r2"></param>
        /// <returns></returns>        
        private bool IsOverlapping(Rect r1, Rect r2)
        {
            return IsOverlapping(new RectangleGeometry(r1), new RectangleGeometry(r2));
        }
        //IntersectionDetail列挙型
        //Empty             全く重なっていない
        //FullyContains     r2はr1の領域に完全に収まっている
        //FullyInside       r1はr2の領域に完全に収まっている
        //Intersects        一部が重なっている
        //NotCalculated     計算されません(よくわからん)




        #endregion メニューウィンドウのRect収集


        //ウィンドウハンドルからText(タイトル名)やRECTを取得
        private (IntPtr, Rect re, string text) GetWindowRectAndText(IntPtr hWnd)
        {
            return (hWnd, MyGetWindowRect(hWnd), MyGetWindowText(hWnd));
        }
        private (List<IntPtr> ptrs, List<Rect> rs, List<string> strs)
            GetWindowRectAndTexts(List<IntPtr> pList)
        {
            List<IntPtr> ptrs = new();
            List<Rect> rs = new();
            List<string> strs = new();
            foreach (var item in pList)
            {
                ptrs.Add(item);
                rs.Add(MyGetWindowRect(item));
                strs.Add(MyGetWindowText(item));
            }
            return (ptrs, rs, strs);
        }
        //ウィンドウハンドルからText(タイトル名)やRECTを取得
        private (IntPtr, RECT re, string text) GetWindowAPI_RECTAndText(IntPtr hWnd)
        {
            return (hWnd, MyGetWindowAPI_RECT(hWnd), MyGetWindowText(hWnd));
        }
        private (List<IntPtr> ptrs, List<RECT> rs, List<string> strs)
            GetWindowAPI_RECTAndTexts(List<IntPtr> pList)
        {
            List<IntPtr> ptrs = new();
            List<RECT> rs = new();
            List<string> strs = new();
            foreach (var item in pList)
            {
                ptrs.Add(item);
                rs.Add(MyGetWindowAPI_RECT(item));
                strs.Add(MyGetWindowText(item));
            }
            return (ptrs, rs, strs);
        }

        private string MyGetWindowText(IntPtr hWnd)
        {
            var text = new StringBuilder(65535);
            _ = GetWindowText(hWnd, text, 65535);
            return text.ToString();
        }
        private RECT MyGetWindowAPI_RECT(IntPtr hWnd)
        {
            _ = GetWindowRect(hWnd, out RECT re);
            return re;
        }

        private Rect MyGetWindowRect(IntPtr hWnd)
        {
            RECT r = MyGetWindowAPI_RECT(hWnd);
            return new(r.left, r.top, r.right - r.left, r.bottom - r.top);
        }

        //ウィンドウの見た目通りのRect取得はDwmGetWindowAttribute
        //https://gogowaten.hatenablog.com/entry/2020/11/17/004505
        //見た目通りのRect取得
        private Rect GetWindowRectMitame(IntPtr hWnd)
        {
            //見た目通りのWindowRectを取得
            RECT myRECT;
            DwmGetWindowAttribute(
                hWnd,
                DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                out myRECT, System.Runtime.InteropServices.Marshal.SizeOf(typeof(RECT)));

            return MyConvertApiRectToRect(myRECT);
        }
        private Rect MyConvertApiRectToRect(RECT rect)
        {
            return new Rect(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
        }



        private void PlayMySound()
        {
            switch (MyComboBoxSoundType.SelectedValue)
            {
                case MySoundPlay.None:
                    break;
                case MySoundPlay.PlayDefault:
                    MySoundDefault.Play();
                    break;
                case MySoundPlay.PlayOrder:
                    MySoundOrder.Play();
                    break;
                default:
                    break;
            }
        }

        private IntPtr GetParentWindowFromForegroundWindow()
        {
            //最前面ウィンドウを起点にWindowTextがあるもの(GetWindowTextの戻り値が0以外)をGetParentで10回まで辿る            
            //見つからなかった場合は最前面ウィンドウのハンドルにする
            IntPtr hForeWnd = GetForegroundWindow();
            var wndText = new StringBuilder(65535);
            int count = 0;
            IntPtr hWnd = hForeWnd;
            while (GetWindowText(hWnd, wndText, 65535) == 0)
            {
                hWnd = GetParent(hWnd);
                count++;
                if (count > 10)
                {
                    hWnd = hForeWnd;
                    break;
                }
            }
            return hWnd;
        }
        private void ChangeHotKey(Key Key, int hotkeyId)
        {
            ChangeHotKey(KeyInterop.VirtualKeyFromKey(Key), hotkeyId);
        }
        private void ChangeHotKey(int vKey, int hotkeyId)
        {
            //上書きはできないので、古いのを削除してから登録
            _ = UnregisterHotKey(MyWindowHandle, hotkeyId);

            //int mod = GetModifierKeySum();
            int mod = GetModifierKeySum2();
            if (RegisterHotKey(MyWindowHandle, hotkeyId, mod, vKey) == 0)
            {
                MessageBox.Show("このキーの組み合わせは登録できなかった\n" +
                    "理由：他のアプリ、もしくはウィンドウズが使っている可能性が巨レ存\n" +
                    "winキーとの組み合わせはウィンドウズが使っていることが多い", "ホットキーの登録に失敗");
                MyGroupBoxHotKey.BorderBrush = Brushes.Red;
                //MyGroupBoxHotKey.Header = "無効なホットキー";
            }
            else
            {
                //MessageBox.Show("登録完了");
                MyGroupBoxHotKey.BorderBrush = SystemColors.ActiveBorderBrush;
                //MyGroupBoxHotKey.Header = "ホットキー";
            }
        }

        //ホットキーの修飾キーフラグの値取得
        private int GetModifierKeySum()
        {
            int mod = 0;
            if (MyAppConfig.HotkeyAlt) mod += (int)ModifierKeys.Alt;
            if (MyAppConfig.HotkeyCtrl) mod += (int)ModifierKeys.Control;
            if (MyAppConfig.HotkeyShift) mod += (int)ModifierKeys.Shift;
            if (MyAppConfig.HotkeyWin) mod += (int)ModifierKeys.Windows;
            return mod;
        }
        //フラグなので↑の足し算より、↓のorがいい
        private int GetModifierKeySum2()
        {
            int mod = 0;
            if (MyAppConfig.HotkeyAlt) mod |= (int)ModifierKeys.Alt;
            if (MyAppConfig.HotkeyCtrl) mod |= (int)ModifierKeys.Control;
            if (MyAppConfig.HotkeyShift) mod |= (int)ModifierKeys.Shift;
            if (MyAppConfig.HotkeyWin) mod |= (int)ModifierKeys.Windows;
            return mod;
        }

        #endregion


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            //MyComboBoxHotKey.SelectionChanged += (s, e) => { vHotKey = KeyInterop.VirtualKeyFromKey(MyAppConfig.HotKey); };
            MyComboBoxHotKey.SelectionChanged += (s, e) => { ChangeHotKey(MyAppConfig.HotKey, HOTKEY_ID1); };

            MyCheckAlt.Click += MyCheckModKey_Click;
            MyCheckCtrl.Click += MyCheckModKey_Click;
            MyCheckShift.Click += MyCheckModKey_Click;
            MyCheckWin.Click += MyCheckModKey_Click;


            //ファイル名の見本の表示更新
            UpdateFileNameSample();
        }

        private void MyCheckModKey_Click(object sender, RoutedEventArgs e)
        {
            ChangeHotKey(MyAppConfig.HotKey, HOTKEY_ID1);
        }



        private Int32Rect MakeCroppRectFromClientRECT(RECT cliectRECT, POINT myPOINT, int bmpWidth, int bmpHeight)
        {
            int width = cliectRECT.right;
            if (myPOINT.X + width > bmpWidth)
            {
                width = bmpWidth - myPOINT.X;
            }
            int height = cliectRECT.bottom;
            if (myPOINT.Y + height > bmpHeight)
            {
                height = bmpHeight - myPOINT.Y;
            }
            return new Int32Rect(myPOINT.X, myPOINT.Y, width, height);
        }
        private Int32Rect MakeCroppRectFromRECT(RECT myRECT, int bitmapWidth, int bitmapHeight)
        {
            int left = myRECT.left < 0 ? 0 : myRECT.left;
            int top = myRECT.top < 0 ? 0 : myRECT.top;
            int right = myRECT.right > bitmapWidth ? bitmapWidth : myRECT.right;
            int bottom = myRECT.bottom > bitmapHeight ? bitmapHeight : myRECT.bottom;
            return new Int32Rect(left, top, right - left, bottom - top);
        }

        /// <summary>
        /// マウスカーソルの情報をフィールドに格納
        /// </summary>
        private bool SetCursorInfo()
        {
            try
            {
                CURSORINFO cInfo = new CURSORINFO();
                cInfo.cbSize = Marshal.SizeOf(cInfo);
                GetCursorInfo(out cInfo);
                GetIconInfo(cInfo.hCursor, out ICONINFO iInfo);
                //カーソル画像
                MyBitmapCursor =
                    Imaging.CreateBitmapSourceFromHIcon(cInfo.hCursor,
                                                        Int32Rect.Empty,
                                                        BitmapSizeOptions.FromEmptyOptions());
                //カーソルマスク画像
                MyBitmapCursorMask =
                    Imaging.CreateBitmapSourceFromHBitmap(iInfo.hbmMask,
                                                          IntPtr.Zero,
                                                          Int32Rect.Empty,
                                                          BitmapSizeOptions.FromEmptyOptions());
                //マスク画像を使うかどうかの判定
                //2色画像 かつ 高さが幅の2倍ならマスク画像使用
                IsMaskUse = (MyBitmapCursorMask.Format == PixelFormats.Indexed1) &
                    (MyBitmapCursorMask.PixelHeight == MyBitmapCursorMask.PixelWidth * 2);

                //マスク画像のピクセルフォーマットはIndexed1なんだけど、計算しやすいようにBgra32に変換しておく
                MyBitmapCursorMask = new FormatConvertedBitmap(MyBitmapCursorMask,
                                                               PixelFormats.Bgra32,
                                                               null,
                                                               0);

                //ホットスポット保持
                MyCursorHotspotX = iInfo.xHotspot;
                MyCursorHotspotY = iInfo.yHotspot;

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //仮想画面全体の画像取得
        private BitmapSource ScreenCapture()
        {
            var screenDC = GetDC(IntPtr.Zero);//仮想画面全体のDC、コピー元
            var memDC = CreateCompatibleDC(screenDC);//コピー先DC作成
            int width = (int)SystemParameters.VirtualScreenWidth;
            int height = (int)SystemParameters.VirtualScreenHeight;
            var hBmp = CreateCompatibleBitmap(screenDC, width, height);//コピー先のbitmapオブジェクト作成
            SelectObject(memDC, hBmp);//コピー先DCにbitmapオブジェクトを指定

            //コピー元からコピー先へビットブロック転送
            //通常のコピーなのでSRCCOPYを指定
            BitBlt(memDC, 0, 0, width, height, screenDC, 0, 0, SRCCOPY);
            //bitmapオブジェクトからbitmapSource作成
            BitmapSource source =
                Imaging.CreateBitmapSourceFromHBitmap(
                    hBmp,
                    IntPtr.Zero,
                    Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions());

            //後片付け
            DeleteObject(hBmp);
            _ = ReleaseDC(IntPtr.Zero, screenDC);
            _ = ReleaseDC(IntPtr.Zero, memDC);

            //画像
            return source;


            ////PringWindowを使ったキャプチャはWindow7のウィンドウになるし、タイトル文字が透明
            //IntPtr bb = CreateCompatibleBitmap(screenDC, width, height);
            //SelectObject(memDC, bb);
            ////PrintWindow(GetForegroundWindow(), memDC,nFrags_PW_CLIENTONLY);//クライアント領域
            //PrintWindow(GetForegroundWindow(), memDC, 0);//ウィンドウ
            //var bmp = Imaging.CreateBitmapSourceFromHBitmap(bb, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
        }


        //private void SetCaptureRectType()
        //{
        //    //            C#のWPFでComboBoxにDictionaryをバインドする - Ararami Studio
        //    //https://araramistudio.jimdo.com/2019/02/05/c-%E3%81%AEwpf%E3%81%A7combobox%E3%81%ABdictionary%E3%82%92%E3%83%90%E3%82%A4%E3%83%B3%E3%83%89%E3%81%99%E3%82%8B/
        //    RectTyeps = new Dictionary<CaptureRectType, string>();
        //    RectTyeps.Add(CaptureRectType.Screen, "全画面");
        //    RectTyeps.Add(CaptureRectType.Window, "ウィンドウ");
        //    RectTyeps.Add(CaptureRectType.WindowClient, "ウィンドウのクライアント領域");
        //    RectTyeps.Add(CaptureRectType.UnderCursor, "カーソル下のコントロール");
        //    RectTyeps.Add(CaptureRectType.UnderCursorClient, "カーソル下のクライアント領域");
        //    MyComboBoxTest.ItemsSource = RectTyeps;
        //}

        //画像の上にカーソル画像を合成(マスクが必要なカーソルの場合)
        private BitmapSource DrawCursorOnBitmapWithMask(BitmapSource source)
        {
            //int width, height, stride;
            //byte[] pixels;
            //カーソルマスク画像と合成
            //マスク画像の2枚は上下に連結された状態なので、上下に分割
            int maskWidth = MyBitmapCursorMask.PixelWidth;
            int maskHeight = MyBitmapCursorMask.PixelHeight / 2;
            //分割
            var mask1Bitmap = new CroppedBitmap(MyBitmapCursorMask,
                                          new Int32Rect(0, 0, maskWidth, maskHeight));
            var mask2Bitmap = new CroppedBitmap(MyBitmapCursorMask,
                                          new Int32Rect(0, maskHeight, maskWidth, maskHeight));
            //画素をbyte配列で取得
            int maskStride = (maskWidth * 32 + 7) / 8;
            byte[] mask1Pixels = new byte[maskHeight * maskStride];
            byte[] mask2Pixels = new byte[maskHeight * maskStride];
            mask1Bitmap.CopyPixels(mask1Pixels, maskStride, 0);
            mask2Bitmap.CopyPixels(mask2Pixels, maskStride, 0);

            //キャプチャ画像をbyte配列で取得
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * 32 + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            //処理範囲の開始点と終了点設定、開始点はカーソルのホットスポットでオフセット
            int beginX = MyCursorPoint.X - MyCursorHotspotX;
            int beginY = MyCursorPoint.Y - MyCursorHotspotY;
            int endX = beginX + maskWidth;
            int endY = beginY + maskHeight;
            if (endX > width) endX = width;
            if (endY > height) endY = height;

            //最初にマスク画像上とAND合成、続けてマスク画像下とXOR
            int yCount = 0;
            for (int y = beginY; y < endY; y++)
            {
                int xCount = 0;
                for (int x = beginX; x < endX; x++)
                {
                    int p = (y * stride) + (x * 4);
                    int pp = (yCount * maskStride) + (xCount * 4);
                    //AND
                    pixels[p] &= mask1Pixels[pp];
                    pixels[p + 1] &= mask1Pixels[pp + 1];
                    pixels[p + 2] &= mask1Pixels[pp + 2];
                    //XOR
                    pixels[p] ^= mask2Pixels[pp];
                    pixels[p + 1] ^= mask2Pixels[pp + 1];
                    pixels[p + 2] ^= mask2Pixels[pp + 2];

                    xCount++;
                }
                yCount++;
            }
            return BitmapSource.Create(width,
                                       height,
                                       source.DpiX,
                                       source.DpiY,
                                       source.Format,
                                       source.Palette,
                                       pixels,
                                       stride);
        }

        //画像の上にカーソル画像を合成
        private BitmapSource DrawCursorOnBitmap(BitmapSource source)
        {
            //カーソル画像
            int cWidth = MyBitmapCursor.PixelWidth;
            int cHeight = MyBitmapCursor.PixelHeight;
            int maskStride = (cWidth * 32 + 7) / 8;
            byte[] cursorPixels = new byte[cHeight * maskStride];
            MyBitmapCursor.CopyPixels(cursorPixels, maskStride, 0);

            //キャプチャ画像
            int width = source.PixelWidth;
            int height = source.PixelHeight;
            int stride = (width * 32 + 7) / 8;
            byte[] pixels = new byte[height * stride];
            source.CopyPixels(pixels, stride, 0);

            //処理範囲の開始点と終了点設定
            int beginX = MyCursorPoint.X - MyCursorHotspotX;
            int beginY = MyCursorPoint.Y - MyCursorHotspotY;
            int endX = beginX + cWidth;
            int endY = beginY + cHeight;
            if (endX > width) endX = width;
            if (endY > height) endY = height;

            int yCount = 0;
            for (int y = beginY; y < endY; y++)
            {
                int xCount = 0;
                for (int x = beginX; x < endX; x++)
                {
                    int p = (y * stride) + (x * 4);
                    int pp = (yCount * maskStride) + (xCount * 4);
                    //アルファブレンド
                    //                    効果
                    //http://www.charatsoft.com/develop/otogema/page/05d3d/effect.html
                    //求める画素値 = もとの画素値 + ((カーソル画素値 - もとの画素値) * (カーソルのアルファ値 / 255))
                    double alpha = cursorPixels[pp + 3] / 255.0;
                    byte r = pixels[p + 2];
                    byte g = pixels[p + 1];
                    byte b = pixels[p];
                    pixels[p + 2] = (byte)(r + ((cursorPixels[pp + 2] - r) * alpha));
                    pixels[p + 1] = (byte)(g + ((cursorPixels[pp + 1] - g) * alpha));
                    pixels[p] = (byte)(b + ((cursorPixels[pp] - b) * alpha));

                    xCount++;
                }
                yCount++;
            }
            return BitmapSource.Create(width,
                                       height,
                                       source.DpiX,
                                       source.DpiY,
                                       source.Format,
                                       source.Palette,
                                       pixels,
                                       stride);
        }




        #region 設定保存と読み込み
        private void MyButtonSaveState_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig(AppDir + "\\" + APP_CONFIG_FILE_NAME);
        }

        //アプリの設定保存
        private bool SaveConfig(string path)
        {
            var serializer = new DataContractSerializer(typeof(AppConfig));
            XmlWriterSettings settings = new();
            settings.Encoding = new UTF8Encoding();
            try
            {
                using (var xw = XmlWriter.Create(path, settings))
                {
                    serializer.WriteObject(xw, MyAppConfig);
                }
                return true;
            }
            catch (Exception ex)
            {

                MessageBox.Show(
                    $"アプリの設定保存できなかった\n{ex.Message}",
                    $"{System.Reflection.Assembly.GetExecutingAssembly()}");
                return false;
            }
        }

        private void MyButtonLoadState_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dialog = new();
            dialog.Filter = "(xml)|*.xml";
            if (dialog.ShowDialog() == true)
            {
                var config = LoadConfig(dialog.FileName);
                if (config == null) return;
                MyAppConfig = config;
                this.DataContext = MyAppConfig;

                //ホットキー登録
                ChangeHotKey(MyAppConfig.HotKey, HOTKEY_ID1);

            }
            //AppConfig config = LoadConfig(AppDir + "\\" + APP_CONFIG_FILE_NAME);
            //if (config != null)
            //{
            //    MyAppConfig = config;
            //    this.DataContext = MyAppConfig;
            //}
        }

        //アプリの設定読み込み

        private AppConfig LoadConfig(string path)
        {
            var serealizer = new DataContractSerializer(typeof(AppConfig));
            try
            {
                using XmlReader xr = XmlReader.Create(path);
                return (AppConfig)serealizer.ReadObject(xr);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"読み込みできなかった\n{ex.Message}",
                    $"{System.Reflection.Assembly.GetExecutingAssembly().GetName()}");
                return null;
            }
        }

        //名前を付けて保存
        private void MyButtonSaveStateFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dialog = new();
            dialog.Filter = "(xml)|*.xml";
            if (dialog.ShowDialog() == true)
            {
                SaveConfig(dialog.FileName);
            }
        }


        #endregion


        private void MyTestButton_Click(object sender, RoutedEventArgs e)
        {
            //MyAppConfig.ImageType = ImageType.jpg;
            //MyAppConfig.DirList.Add("dummy dir");
            var neko = MyComboBoxCaputureRect.SelectedValue;
            //var unu = MyRadioButtonFileNameDate.IsChecked;
            var uma = MakeFileName();
            var tako = MyAppConfig;
            MessageBox.Show($"{AppDir}");
        }



        #region 保存先リスト追加と削除
        //保存フォルダをリストに追加
        private void ButtonSaveDirectoryAdd_Click(object sender, RoutedEventArgs e)
        {
            //フォルダ指定なし
            //FolderDialog dialog = new FolderDialog(this);

            //フォルダ指定あり
            string folderPath;
            folderPath = MyComboBoxSaveDirectory.Text;//表示しているテキスト

            FolderDialog dialog = new FolderDialog(folderPath, this);

            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string path = dialog.GetFullPath();
                //AddDir(path);
                AddTextToComboBox(path, MyAppConfig.DirList, MyComboBoxSaveDirectory);
            }
        }

        //ComboBoxのItemsSourceのBinding先のリストに文字列を追加
        private void AddTextToComboBox(string text, ObservableCollection<string> stringList, ComboBox combo)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            if (string.IsNullOrEmpty(text)) return;

            int itemIndex = stringList.IndexOf(text);
            //リストにないパスの場合は普通に追加
            if (itemIndex == -1)
            {
                stringList.Add(text);
                combo.SelectedIndex = stringList.Count - 1;
            }
            //リストにあるパスだったら、そのパスをリストの先頭に移動
            else
            {
                //リストのコピーを作って、そこから順に元リストに入れていく
                var list = stringList.ToList();
                stringList[0] = list[itemIndex];//先頭
                list.RemoveAt(itemIndex);
                //先頭以外を順に
                for (int i = 0; i < list.Count; i++)
                {
                    stringList[i + 1] = list[i];
                }
                combo.SelectedIndex = 0;
            }
        }

        //保存フォルダリスト、表示しているアイテム削除
        private void ButtonSaveDirectoryDelete_Click(object sender, RoutedEventArgs e)
        {
            RemoveComboBoxItem(MyComboBoxSaveDirectory, MyAppConfig.DirList);
        }
        private void RemoveComboBoxItem(ComboBox combo, ObservableCollection<string> list)
        {
            if (combo.Items.Contains(combo.Text) == false) return;
            int idx = combo.SelectedIndex;
            if (idx < 0) return;
            if (MessageBox.Show($"{combo.SelectedValue}を\nリストから削除します",
                                "確認",
                                MessageBoxButton.OKCancel)
                == MessageBoxResult.OK)
            {
                //削除
                list.RemoveAt(idx);
                //削除後に表示するitem
                if (idx == list.Count || list.Count == 0)
                {
                    //削除アイテムがリストの最後か最初なら、Index-1
                    combo.SelectedIndex = idx - 1;
                }
                else
                {
                    //中間だった場合は同じIndexでいい
                    combo.SelectedIndex = idx;
                }

            }
        }

        //保存場所を開く
        private void MyButtonOpenSaveFolder_Click(object sender, RoutedEventArgs e)
        {
            string dir = MyComboBoxSaveDirectory.Text;

            if (string.IsNullOrWhiteSpace(dir) || System.IO.Directory.Exists(dir) == false)
            {
                MessageBox.Show($"指定された保存場所\n{dir}\nは存在しない", "保存できなかった");
            }
            else
            {
                System.Diagnostics.Process.Start("EXPLORER.EXE", dir);
            }
        }

        #endregion


        #region 画像保存
        /// <summary>
        /// 複数Rect範囲を組み合わせた形にbitmapを切り抜く
        /// </summary>
        /// <param name="source">元の画像</param>
        /// <param name="rectList">Rectのコレクション</param>
        /// <returns></returns>
        private BitmapSource CroppedBitmapFromRects(BitmapSource source, List<Rect> rectList)
        {
            List<Int32Rect> re = new();
            foreach (var item in rectList)
            {
                re.Add(new Int32Rect((int)item.X, (int)item.Y, (int)item.Width, (int)item.Height));
            }

            return CroppedBitmapFromRects(source, re);
        }
        private BitmapSource CroppedBitmapFromRects(BitmapSource source, List<Int32Rect> rectList)
        {
            var dv = new DrawingVisual();

            using (DrawingContext dc = dv.RenderOpen())
            {
                //それぞれのRect範囲で切り抜いた画像を描画していく
                foreach (var rect in rectList)
                {
                    dc.DrawImage(new CroppedBitmap(source, rect), new Rect(rect.X, rect.Y, rect.Width, rect.Height));
                }
            }

            //描画位置調整
            dv.Offset = new Vector(-dv.ContentBounds.X, -dv.ContentBounds.Y);

            //bitmap作成、縦横サイズは切り抜き後の画像全体がピッタリ収まるサイズにする
            //PixelFormatsはPbgra32で決め打ち、これ以外だとエラーになるかも、
            //画像を読み込んだbitmapImageのPixelFormats.Bgr32では、なぜかエラーになった
            var bmp = new RenderTargetBitmap(
                (int)Math.Ceiling(dv.ContentBounds.Width),
                (int)Math.Ceiling(dv.ContentBounds.Height),
                96, 96, PixelFormats.Pbgra32);

            bmp.Render(dv);
            return bmp;
        }

        //RectからInt32Rect作成、小数点以下切り捨て編
        //Rectの数値は整数のはずだから、これでいいはず
        private Int32Rect RectToIntRectWith切り捨て(Rect re)
        {
            return new Int32Rect((int)re.X, (int)re.Y, (int)re.Width, (int)re.Height);
        }

        //画像にマウスカーソルを描画してからCropp
        private BitmapSource MakeBitmapForSave(BitmapSource source, List<Rect> reList)
        {
            BitmapSource bitmap;
            if (MyAppConfig.IsDrawCursor == true)
            {
                bitmap = DrawCursor(source);
            }
            else { bitmap = source; }

            return CroppedBitmapFromRects(bitmap, reList);
        }
        private BitmapSource MakeBitmapForSave(BitmapSource source, List<Int32Rect> reList)
        {
            List<Rect> re = new();
            try
            {
                foreach (var item in reList)
                {
                    re.Add(new Rect(item.X, item.Y, item.Width, item.Height));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"{ex}");
            }
            return MakeBitmapForSave(source, re);
        }

        private BitmapSource DrawCursor(BitmapSource source)
        {
            BitmapSource bitmap;
            if (IsMaskUse)
            {
                bitmap = DrawCursorOnBitmapWithMask(source);
            }
            else
            {
                bitmap = DrawCursorOnBitmap(source);
            }
            return bitmap;
        }

        private void SaveBitmap(BitmapSource bitmap, string fullPath)
        {
            //CroppedBitmapで切り抜いた画像でBitmapFrame作成して保存
            BitmapEncoder encoder = GetEncoder();
            //メタデータ作成、アプリ名記入
            BitmapMetadata meta = MakeMetadata();
            encoder.Frames.Add(BitmapFrame.Create(bitmap, null, meta, null));
            try
            {
                using var fs = new System.IO.FileStream(
                    fullPath, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                encoder.Save(fs);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存できなかった\n{ex}", "保存できなかった");
            }
        }

        //ファイル名の重複を回避、拡張子の前に"_"を付け足す
        private string MakeFullPath(string directory, string fileName, string extension)
        {
            var dir = System.IO.Path.Combine(directory, fileName);
            extension = "." + extension;
            var fullPath = dir;

            while (System.IO.File.Exists(fullPath + extension))
            {
                fullPath += "_";
            }
            return fullPath + extension;
        }

        //メタデータ作成
        private BitmapMetadata MakeMetadata()
        {
            BitmapMetadata data = null;
            string software = APP_NAME + "_" + AppVersion;
            switch (ComboBoxSaveFileType.SelectedValue)
            {
                case ImageType.png:
                    data = new BitmapMetadata("png");
                    data.SetQuery("/tEXt/Software", software);
                    break;
                case ImageType.jpg:
                    data = new BitmapMetadata("jpg");
                    data.SetQuery("/app1/ifd/{ushort=305}", software);
                    break;
                case ImageType.bmp:

                    break;
                case ImageType.gif:
                    data = new BitmapMetadata("Gif");
                    //data.SetQuery("/xmp/xmp:CreatorTool", "Pixtrim2");
                    //data.SetQuery("/XMP/XMP:CreatorTool", "Pixtrim2");
                    data.SetQuery("/XMP/XMP:CreatorTool", software);
                    break;
                case ImageType.tiff:
                    data = new BitmapMetadata("tiff")
                    {
                        ApplicationName = software
                    };
                    break;
                default:
                    break;
            }

            return data;
        }

        //画像ファイル形式によるEncoder取得
        private BitmapEncoder GetEncoder()
        {
            var type = MyAppConfig.ImageType;

            switch (type)
            {
                case ImageType.png:
                    return new PngBitmapEncoder();
                case ImageType.jpg:
                    var jpeg = new JpegBitmapEncoder
                    {
                        QualityLevel = MyAppConfig.JpegQuality
                    };
                    return jpeg;
                case ImageType.bmp:
                    return new BmpBitmapEncoder();
                case ImageType.gif:
                    return new GifBitmapEncoder();
                case ImageType.tiff:
                    return new TiffBitmapEncoder();
                default:
                    throw new Exception();
            }
        }

        //今の日時をStringで作成
        private string MakeStringNowTime()
        {
            DateTime dt = DateTime.Now;
            //string str = dt.ToString("yyyyMMdd");            
            //string str = dt.ToString("yyyyMMdd" + "_" + "HHmmssfff");
            string str = dt.ToString(DATE_TIME_STRING_FORMAT);
            //string str = dt.ToString("yyyyMMdd" + "_" + "HH" + "_" + "mm" + "_" + "ss" + "_" + "fff");
            return str;
        }


        private string MakeFileName()
        {
            double count = 0.0;
            string fileName = "";
            DateTime dateTime = DateTime.Now;
            bool isOverDate = false, isOverSerial = false;
            if (MyAppConfig.IsFileNameDate == false && MyAppConfig.IsFileNameSerial == false)
            {
                MyCheckBoxFileNameData.IsChecked = true;
            }
            if (MyAppConfig.IsFileNameDate == false) isOverDate = true;
            if (MyAppConfig.IsFileNameSerial == false) isOverSerial = true;
            MyOrder();

            if (MyAppConfig.IsFileNameText1) MyAddText(MyComboBoxFileNameText1);
            count += 1.5; MyOrder();

            if (MyAppConfig.IsFileNameText2) MyAddText(MyComboBoxFileNameText2);
            count++; MyOrder();

            if (MyAppConfig.IsFileNameText3) MyAddText(MyComboBoxFileNameText3);
            count++; MyOrder();

            if (MyAppConfig.IsFileNameText4) MyAddText(MyComboBoxFileNameText4);
            count += 1.5; MyOrder();

            if (string.IsNullOrWhiteSpace(fileName)) fileName = MakeStringNowTime();
            fileName = fileName.TrimStart();
            fileName = fileName.TrimEnd();
            return fileName;


            void MyOrder()
            {
                //日時
                if (isOverDate == false && MyAppConfig.FileNameDateOrder == count)
                {
                    var format = MyComboBoxFileNameDateFormat.Text;
                    if (string.IsNullOrEmpty(format))
                    {
                        fileName += MakeStringNowTime();
                    }
                    else
                    {
                        try
                        {
                            fileName += dateTime.ToString(MyComboBoxFileNameDateFormat.Text);
                            isOverDate = true;
                        }
                        catch (Exception)
                        {

                        }

                    }
                }

                //連番
                if (isOverSerial == false && MyAppConfig.FileNameSerialOrder == count)
                {
                    //fileName += MyNumericUpDownFileNameSerial.MyValue.ToString(MySerialFormat());
                    fileName += MyAppConfig.FileNameSerial.ToString(MySerialFormat());

                    isOverSerial = true;
                }
            }

            string MyAddText(ComboBox comboBox)
            {
                return fileName += comboBox.Text;
            }
            string MySerialFormat()
            {
                string str = "";
                for (int i = 0; i < MyAppConfig.FileNameSerialDigit; i++)
                {
                    str += "0";
                }
                return str;
            }

        }

        //連番に増加値を加算
        private void AddIncrementToSerial()
        {
            MyNumericUpDownFileNameSerial.MyValue += MyNumericUpDownFileNameSerialIncreace.MyValue;
        }

        #endregion 画像保存




        //コンボボックス上でキーを押し下げたとき
        //入力されたキー文字は無視
        private void MyComboBoxHotKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;//キーイベント無視む～し
        }
        //コンボボックス上でキーが上げられたとき
        //修飾キー以外なら、そのキーと同じキーをコンボボックスで選択する
        //文字は無視
        private void MyComboBoxHotKey_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            var key = e.Key;
            if ((key == Key.LeftAlt || key == Key.RightAlt ||
                key == Key.LeftCtrl || key == Key.RightCtrl ||
                key == Key.LeftShift || key == Key.RightShift ||
                key == Key.LWin || key == Key.RWin) == false)
            {
                MyComboBoxHotKey.SelectedValue = key;
            }

            e.Handled = true;
        }

        private void MyButtonSample_Click(object sender, RoutedEventArgs e)
        {
            UpdateFileNameSample();
        }
        private void UpdateFileNameSample()
        {
            string fileName = MakeFileName() + "." + MyAppConfig.ImageType.ToString();
            MyTextBoxFileNameSample.Text = fileName;
            if (CheckFileNameValidid(fileName))
            {
                MyTextBoxFileNameSample.Foreground = SystemColors.ControlTextBrush;
            }
            else
            {
                MyTextBoxFileNameSample.Foreground = Brushes.Red;
            }
        }


        private void MyButtonAddFileNameText1_Click(object sender, RoutedEventArgs e)
        {
            AddFileNameToComboBox(sender, MyAppConfig.FileNameText1List);
        }
        private void AddFileNameToComboBox(object sender, ObservableCollection<string> list)
        {
            var button = sender as Button;
            if (button.Tag is ComboBox cb)
            {
                if (CheckFileNameValidid(cb.Text))
                {
                    AddTextToComboBox(cb.Text, list, cb);
                }
                else
                {
                    MessageBox.Show("ファイル名に使えない文字列があったので追加できなかった", "リストに追加できなかった");
                }
            }



        }

        private void MyButtonAddFileNameText2_Click(object sender, RoutedEventArgs e)
        {
            AddFileNameToComboBox(sender, MyAppConfig.FileNameText2List);
        }

        private void MyButtonAddFileNameText3_Click(object sender, RoutedEventArgs e)
        {
            AddFileNameToComboBox(sender, MyAppConfig.FileNameText3List);
        }

        private void MyButtonAddFileNameText4_Click(object sender, RoutedEventArgs e)
        {
            AddFileNameToComboBox(sender, MyAppConfig.FileNameText4List);
        }


        private void RemoveComboBoxItem(object sender, ObservableCollection<string> list)
        {
            var b = sender as Button;
            if (b.Tag is ComboBox combo)
            {
                RemoveComboBoxItem(combo, list);
            }
        }
        private void MyButtonRemoveFileNameText1_Click(object sender, RoutedEventArgs e)
        {
            RemoveComboBoxItem(sender, MyAppConfig.FileNameText1List);
        }

        private void MyButtonRemoveFileNameText2_Click(object sender, RoutedEventArgs e)
        {
            RemoveComboBoxItem(sender, MyAppConfig.FileNameText2List);
        }

        private void MyButtonRemoveFileNameText3_Click(object sender, RoutedEventArgs e)
        {
            RemoveComboBoxItem(sender, MyAppConfig.FileNameText3List);
        }

        private void MyButtonRemoveFileNameText4_Click(object sender, RoutedEventArgs e)
        {
            RemoveComboBoxItem(sender, MyAppConfig.FileNameText4List);
        }


        private void MyButtonAddFileNameDateFromat_Click(object sender, RoutedEventArgs e)
        {
            if (CheckDateTimeStringFormat(MyComboBoxFileNameDateFormat.Text))
            {
                AddTextToComboBox(sender, MyAppConfig.FileNameDateFormatList);
            }
            else
            {
                MessageBox.Show($"いまいちな書式なのでリストに追加できなかった\n" +
                    $"いまいちな原因は\n\n" +
                    $"書式に半角の \\ \\ : * ? \" < > | が含まれている\n" +
                    $"もしくは\n" +
                    $"書式適用後にこれらの文字が含まれている", "リストに追加できなかった");
            }
        }

        private void AddTextToComboBox(object sender, ObservableCollection<string> list)
        {
            var button = sender as Button;
            if (button.Tag is ComboBox cb) AddTextToComboBox(cb.Text, list, cb);
        }

        private bool CheckDateTimeStringFormat(string text)
        {
            string fileName;
            try
            {
                fileName = DateTime.Now.ToString(text);
            }
            catch (Exception)
            {
                return false;
            }
            if (CheckFileNameValidid(fileName)) return true;
            else return false;
        }

        private void MyButtonRemoveFileNameDateFromat_Click(object sender, RoutedEventArgs e)
        {
            RemoveComboBoxItem(sender, MyAppConfig.FileNameDateFormatList);
        }


        #region キャプチャ時の音関係

        //キャプチャ時の音
        private void MyButtonRemoveSound_Click(object sender, RoutedEventArgs e)
        {
            //リストから削除
            RemoveComboBoxItem(sender, MyAppConfig.SoundFilePathList);
            //音の変更
            string path = MyComboBoxSoundFilePath.Text;
            ChangeSoundOrder(path);
        }

        private void MyButtonAddSound_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog { Filter = "(wav)|*.wav" };
            if (dialog.ShowDialog() == true)
            {
                AddTextToComboBox(dialog.FileName, MyAppConfig.SoundFilePathList, MyComboBoxSoundFilePath);
                ChangeSoundOrder(dialog.FileName);
            }

        }

        private void MyButtonSound_Click(object sender, RoutedEventArgs e)
        {
            switch (MyAppConfig.MySoundPlay)
            {
                case MySoundPlay.None: return;
                case MySoundPlay.PlayDefault:
                    MySoundDefault.Play();
                    break;
                case MySoundPlay.PlayOrder:
                    //if (MySound == null) return;
                    if (MySoundOrder == null || MySoundOrder.SoundLocation == string.Empty) return;
                    try
                    {
                        //MySound.Stream.Position = 0;
                        MySoundOrder.Play();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"再生できなかった\n" +
                            $"頭に無音が続くファイルは再生できないことが多い気がする\n" +
                            $"{ex.Message}", "再生できなかった");
                    }

                    break;
                default:
                    break;
            }

        }

        private void MyComboBoxSoundFilePath_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ChangeSoundOrder(MyAppConfig.SoundFilePath);
        }
        //指定の音の変更
        private void ChangeSoundOrder(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                MySoundOrder = null;
                //MySound.SoundLocation = string.Empty;//何故かエラーになる
                //MySound.Dispose();

            }
            else
            {
                if (MySoundOrder == null)
                {
                    MySoundOrder = new System.Media.SoundPlayer(filePath);
                    //MySound = new System.Media.SoundPlayer(new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read));
                }
                else
                {
                    MySoundOrder.SoundLocation = filePath;
                    //MySound.Stream = null;
                    //MySound.Stream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                }

            }
        }



        #endregion キャプチャ時の音関係

        #region ボタンクリックとかのイベント
        private void MyButtonHelpDateTimeStringformat_Click(object sender, RoutedEventArgs e)
        {
            if (IsDateformatShow) return;
            WindowDateTimeStringformat window = new WindowDateTimeStringformat(MyDateTimeStringFormatBitmapSource);
            window.Owner = this;
            window.Show();
            IsDateformatShow = true;
        }

        //日時書式入力時、見本を更新、無効な書式は赤文字にする
        private void MyComboBoxFileNameDateFormat_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            string cbText = cb.Text;
            if (string.IsNullOrWhiteSpace(cbText))
            {
                cbText = DATE_TIME_STRING_FORMAT;
            }
            var now = DateTime.Now;

            //無効なファイル名なら枠色を赤にする
            try
            {
                if (CheckFileNameValidid(now.ToString(cbText)))
                {
                    cb.Foreground = SystemColors.ControlTextBrush;
                }
                else
                {
                    cb.Foreground = Brushes.Red;
                }
                //見本ファイル名の表示更新
                UpdateFileNameSample();

            }
            catch (Exception)
            {
                cb.Foreground = Brushes.Red;
            }

        }

        private void MyButtonSerialReset_Click(object sender, RoutedEventArgs e)
        {
            MyNumericUpDownFileNameSerial.MyValue = 0m;
        }

        private void MyComboBoxFileNameText_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            cb.Text = (string)cb.SelectedItem;
            cb.Foreground = SystemColors.ControlTextBrush;
            UpdateFileNameSample();
        }

        private void MyComboBoxFileNameOrder_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateFileNameSample();
        }

        private void MyComboBoxFileNameDateFormat_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is not ComboBox cb) return;
            cb.Text = (string)cb.SelectedItem;
            cb.Foreground = SystemColors.ControlTextBrush;
            UpdateFileNameSample();
        }

        //プレビューウィンドウ
        private void MyMenuItemOpenPreviewWindow_Click(object sender, RoutedEventArgs e)
        {
            if (MyPreviweWindow == null)
            {
                MyPreviweWindow = new PreviweWindow(this);
                MyPreviweWindow.Owner = this;
                MyPreviweWindow.Show();
            }
        }


        #endregion ボタンクリックとかのイベント

        //ウィンドウハンドルからウィンドウの情報用
        //ウィンドウのハンドル、Rect、Text、IsVisible
        private struct MyWidndowInfo
        {
            public IntPtr hWnd;
            public Rect Rect;
            public bool IsVisible;
            public string Text;

            public override string ToString()
            {
                string visible = IsVisible == true ? "可視" : "不可視";
                //x16は書式で、xが16進数で表示、16が表示桁数
                return $"IntPtr({hWnd.ToString("x16")}), Rect({Rect}), {visible}, Text({Text})";
            }
        }

    }





    /// <summary>
    /// アプリの設定値用クラス
    /// </summary>
    [DataContract]
    public class AppConfig : System.ComponentModel.INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DataMember] public int JpegQuality { get; set; }//jpeg画質
        [DataMember] public double Top { get; set; }//アプリ
        [DataMember] public double Left { get; set; }//アプリ
        //保存先リスト
        [DataMember] public ObservableCollection<string> DirList { get; set; }
        [DataMember] public string Dir { get; set; }
        [DataMember] public int DirIndex { get; set; }

        [DataMember] public bool? IsDrawCursor { get; set; }//マウスカーソル描画の有無
        [DataMember] public bool IsOutputToClipboardOnly { get; set; }//出力はクリップボードだけ


        //ホットキー
        [DataMember] public bool HotkeyAlt { get; set; }
        [DataMember] public bool HotkeyCtrl { get; set; }
        [DataMember] public bool HotkeyShift { get; set; }
        [DataMember] public bool HotkeyWin { get; set; }
        [DataMember] public Key HotKey { get; set; }//キャプチャーキー

        //ファイルネーム        
        //[DataMember] public FileNameBaseType FileNameBaseType { get; set; }
        [DataMember] public bool IsFileNameDate { get; set; }
        [DataMember] public double FileNameDateOrder { get; set; }
        [DataMember] public string FileNameDataFormat { get; set; }
        [DataMember] public ObservableCollection<string> FileNameDateFormatList { get; set; } = new();

        [DataMember] public bool IsFileNameSerial { get; set; }
        [DataMember] public decimal FileNameSerial { get; set; }
        [DataMember] public double FileNameSerialOrder { get; set; }
        [DataMember] public decimal FileNameSerialDigit { get; set; }
        [DataMember] public decimal FileNameSerialIncreace { get; set; }

        [DataMember] public bool IsFileNameText1 { get; set; }
        [DataMember] public string FileNameText1 { get; set; }
        [DataMember] public ObservableCollection<string> FileNameText1List { get; set; } = new();

        [DataMember] public bool IsFileNameText2 { get; set; }
        [DataMember] public string FileNameText2 { get; set; }
        [DataMember] public ObservableCollection<string> FileNameText2List { get; set; } = new();

        [DataMember] public bool IsFileNameText3 { get; set; }
        [DataMember] public string FileNameText3 { get; set; }
        [DataMember] public ObservableCollection<string> FileNameText3List { get; set; } = new();

        [DataMember] public bool IsFileNameText4 { get; set; }
        [DataMember] public string FileNameText4 { get; set; }
        [DataMember] public ObservableCollection<string> FileNameText4List { get; set; } = new();

        //音
        [DataMember] public bool IsSoundPlay { get; set; }
        //[DataMember] public bool IsSoundDefault { get; set; }
        [DataMember] public ObservableCollection<string> SoundFilePathList { get; set; } = new();
        [DataMember] public string SoundFilePath { get; set; }
        [DataMember] public MySoundPlay MySoundPlay { get; set; }



        private ImageType _ImageType;//保存画像形式
        [DataMember]
        public ImageType ImageType
        {
            get => _ImageType;
            set
            {
                if (_ImageType == value) return;
                _ImageType = value;
                RaisePropertyChanged();
            }
        }

        private CaptureRectType _RectType;//切り出し範囲
        [DataMember]
        public CaptureRectType RectType
        {
            get => _RectType;
            set
            {
                if (_RectType == value) return;
                _RectType = value;
                RaisePropertyChanged();
            }
        }




        public AppConfig()
        {
            DirList = new ObservableCollection<string>();
            JpegQuality = 94;
            FileNameSerialIncreace = 1m;
            FileNameSerialDigit = 4m;
            HotKey = Key.PrintScreen;
            IsDrawCursor = false;
            IsFileNameDate = true;
        }


        //        c# - DataContract、デフォルトのDataMember値
        //https://stackoverrun.com/ja/q/2220925

        //初期値の設定
        [OnDeserialized]
        void OnDeserialized(System.Runtime.Serialization.StreamingContext c)
        {
            if (DirList == null) DirList = new();
            if (FileNameDateFormatList == null) FileNameDateFormatList = new();
            if (FileNameText1List == null) FileNameText1List = new();
            if (FileNameText2List == null) FileNameText2List = new();
            if (FileNameText3List == null) FileNameText3List = new();
            if (FileNameText4List == null) FileNameText4List = new();
            if (SoundFilePathList == null) SoundFilePathList = new();
        }
    }






    public enum ImageType
    {
        png,
        bmp,
        jpg,
        gif,
        tiff,

    }
    public enum CaptureRectType
    {
        Screen,
        Window,
        WindowClient,
        UnderCursor,
        UnderCursorClient,
        WindowWithMenu,
        WindowWithRelatedWindow,

    }

    ////ラジオボタンとenumのコンバーター
    //public class FileNameBaseConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        var paramString = parameter as string;
    //        if (paramString == null) { return DependencyProperty.UnsetValue; }

    //        if (!Enum.IsDefined(value.GetType(), value)) { return Binding.DoNothing; }
    //        //if (!Enum.IsDefined(value.GetType(), value)) { return DependencyProperty.UnsetValue; }

    //        var paramValue = Enum.Parse(value.GetType(), paramString);
    //        return paramValue.Equals(value);
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        var paramString = parameter as string;
    //        if (paramString == null) { return DependencyProperty.UnsetValue; }

    //        if (true.Equals(value)) { return Enum.Parse(targetType, paramString); }
    //        else return Binding.DoNothing;
    //        //else return DependencyProperty.UnsetValue;//こっちだとラジオボタンに赤枠がつく
    //    }
    //}
    //public enum FileNameBaseType
    //{
    //    Date,
    //    Serial,
    //}



    public class StringFormatDigitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string prefix = "開始：";
            int digit = decimal.ToInt32((decimal)value);
            string format = "";
            for (int i = 0; i < digit; i++)
            {
                format += "0";
            }
            format = prefix + format + ";" + prefix + "-" + format + ";" + prefix + format;
            return format;
            //"開始：0;開始：-0;開始：0"
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    public enum MySoundPlay
    {
        None,
        PlayDefault,
        PlayOrder
    }




    //    WPF/XAML : TextBox の入力内容を検証して不正入力の場合にエラーを表示する - i++
    //http://increment.hatenablog.com/entry/2015/08/09/172433

    //    ファイル名に使用できない文字列が含まれていないか調べる - .NET Tips(VB.NET, C#...)
    //https://dobon.net/vb/dotnet/file/invalidpathchars.html

    public class MyValidationRuleFileName : ValidationRule
    {
        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            string name = value as string;
            if (name.IndexOfAny(invalid) > 0)
            {
                return new ValidationResult(false, "Invalid FileName");
            }
            else
            {
                return new ValidationResult(true, null);
            }

        }
    }
}
