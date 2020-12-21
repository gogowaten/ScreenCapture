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
using System.Windows.Threading;//DispatcherTimerで使っている
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Collections.ObjectModel;

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
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        //Rect取得用
        private struct RECT
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        //ウィンドウ名取得
        [DllImport("user32.dll")]
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
        private static extern IntPtr GetAncestor(IntPtr hWnd, GETANCESTOR_FLAGS gaFlags);//本当のgaFlagsはuint型の1 2 3
        //GetAncestorのフラグ用
        enum GETANCESTOR_FLAGS
        {
            GA_PARENT = 1,
            //親ウィンドウを取得します。GetParent関数の場合のように、これには所有者は含まれません。
            GA_ROOT = 2,
            //親ウィンドウのチェーンをたどってルートウィンドウを取得します。
            GA_ROOTOWNER = 3,
            //GetParent によって返された親ウィンドウと所有者ウィンドウのチェーンをたどって、所有されているルートウィンドウを取得します。
        }


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

        //
        [DllImport("user32.dll")]
        private static extern bool PrintWindow(IntPtr hWnd, IntPtr hDC, uint nFlags);
        private const uint nFrags_PW_CLIENTONLY = 0x00000001;

        [DllImport("user32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr ho);




        //ウィンドウ系のAPI
        //Windows（Windowsおよびメッセージ）-Win32アプリ | Microsoft Docs
        // https://docs.microsoft.com/en-us/windows/win32/winmsg/windows





        #region マウスカーソル系API
        //マウスカーソル関係

        [DllImport("user32.dll")]
        private static extern IntPtr GetCursor();
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);
        [DllImport("user32.dll")]
        private static extern IntPtr DrawIcon(IntPtr hDC, int x, int y, IntPtr hIcon);
        [DllImport("user32.dll")]
        private static extern IntPtr DrawIconEx(IntPtr hDC,
                                                int x,
                                                int y,
                                                IntPtr hIcon,
                                                int cxWidth,
                                                int cyWidth,
                                                int istepIfAniCur,
                                                IntPtr hbrFlickerFreeDraw,
                                                int diFlags);
        private const int DI_DEFAULTSIZE = 0x0008;//cxWidth cyWidthが0に指定されている場合に規定サイズで描画する
        private const int DI_NORMAL = 0x0003;//通常はこれを指定する、IMAGEとMASKの組み合わせ
        private const int DI_IMAGE = 0x0002;//画像を使用して描画
        private const int DI_MASK = 0x0001;//マスクを使用して描画
        private const int DI_COMPAT = 0x0004;//このフラグは無視の意味
        private const int DI_NOMIRROR = 0x0010;//ミラーリングされていないアイコンとし描画される
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
        [DllImport("user32.dll")]
        private static extern IntPtr CopyIcon(IntPtr hIcon);
        [DllImport("user32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);//CopyIcon使ったあとに使う
        #endregion マウスカーソル系


        #endregion コピペ呪文ここまで^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^

        private string AppDir;//実行ファイルのパス
        private const string APP_CONFIG_FILE_NAME = "config.xml";
        //private BitmapSource MyBitmapScreen;//全画面画像

        private AppConfig MyAppConfig;
        private int vHotKey;//ホットキーの仮想キーコード

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
        private Dictionary<CaptureRectType, string> MyDCRectName;
        private Dictionary<CaptureRectType, Int32Rect> MyDictRectRect;


        //アプリ情報
        private const string AppName = "PixcrenShot山芋";
        private string AppVersion;

        //ホットキーID
        private const int HOTKEY_ID1 = 0x0001;

        private IntPtr MyWindowHandle;


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closed += MainWindow_Closed;
            MyInitializeHotKey();



            //            カスタム日時形式文字列 | Microsoft Docs
            //https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/custom-date-and-time-format-strings

            var now = DateTime.Now;
            var aa = now.ToLongDateString();
            var bb = now.ToLongTimeString();
            var cc = now.ToShortDateString();
            var dd = now.ToString();
            var ee = "fff";
            var ff = now.ToString(ee);
            now = new DateTime(2020, 1, 2, 3, 4, 5, 6);
            var gg = now.ToString(ee);
            var hh = now.ToString("F");
            var ii = now.ToString("FF");
            var jj = now.ToString("FFF");
            var kk = now.ToString("f");
            var ll = now.ToString("ff");
            var mm = now.ToString("ffantasy");
            var nn = now.ToString("mmx");
            var oo = now.ToString("f\\fan\\ta\\s\\y");
            var pp = now.ToString("HH'_'dd");
            var str1 = "指定文字列1";
            var str2 = "指定文字列2";
            var rr = str1 + now.ToString("HH'_'dd") + str2;
            var ss = now.ToString("O");
            var tt = now.ToString("t");

            //実行ファイルのバージョン取得
            var cl = Environment.GetCommandLineArgs();
            AppVersion = System.Diagnostics.FileVersionInfo.GetVersionInfo(cl[0]).FileVersion;

            AppDir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            MyAppConfig = new AppConfig();

            ComboBoxSaveFileType.ItemsSource = Enum.GetValues(typeof(ImageType));

            this.DataContext = MyAppConfig;


            var inu = Enum.Parse(typeof(ImageType), ImageType.png.ToString());


            //SetCaptureRectType();
            MyDCRectName = new Dictionary<CaptureRectType, string>
            {
                { CaptureRectType.Screen, "全画面" },
                { CaptureRectType.Window, "ウィンドウ" },
                { CaptureRectType.WindowClient, "ウィンドウのクライアント領域" },
                { CaptureRectType.UnderCursor, "カーソル下のコントロール" },
                { CaptureRectType.UnderCursorClient, "カーソル下のクライアント領域" },
            };
            MyComboBoxCaputureRect.ItemsSource = MyDCRectName;
            MyDictRectRect = new Dictionary<CaptureRectType, Int32Rect>();




            MyComboBoxHotKey.ItemsSource = Enum.GetValues(typeof(Key));

        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            //ホットキーの登録解除
            UnregisterHotKey(MyWindowHandle, HOTKEY_ID1);
            ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;
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
                string neko = MyComboBoxSaveDirectory.Text;
                if (neko == null) return;
                if (System.IO.Directory.Exists(neko) == false)
                {
                    MessageBox.Show($"指定されている保存場所は存在しないので保存できない");
                    return;
                }
                //キャプチャ処理

                //カーソル座標取得
                GetCursorPos(out MyCursorPoint);

                //カーソル画像取得
                if (MyAppConfig.IsDrawCursor == true)
                {
                    SetCursorInfo();
                }

                //画面全体画像取得
                var screen = ScreenCapture();

                //RECT取得
                Int32Rect rect;
                switch (MyAppConfig.RectType)
                {
                    case CaptureRectType.Screen:
                        rect = new Int32Rect(0, 0, screen.PixelWidth, screen.PixelHeight);
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
                        rect = MakeCroppRectFromRECT(myRECT, screen.PixelWidth, screen.PixelHeight);
                        break;

                    case CaptureRectType.WindowClient:
                        //ウィンドウのクライアント領域のRECT
                        POINT myPOINT;
                        hWnd = GetParentWindowFromForegroundWindow();
                        ClientToScreen(hWnd, out myPOINT);
                        GetClientRect(hWnd, out myRECT);
                        rect = MakeCroppRectFromClientRECT(myRECT, myPOINT, screen.PixelWidth, screen.PixelHeight);
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
                        rect = MakeCroppRectFromRECT(myRECT, screen.PixelWidth, screen.PixelHeight);
                        break;

                    case CaptureRectType.UnderCursorClient:
                        //カーソル下のクライアント領域のRECT
                        POINT myPOINT2;
                        hWnd = WindowFromPoint(MyCursorPoint);
                        ClientToScreen(hWnd, out myPOINT2);
                        GetClientRect(hWnd, out myRECT);
                        rect = MakeCroppRectFromClientRECT(myRECT, myPOINT2, screen.PixelWidth, screen.PixelHeight);

                        break;

                    default:
                        rect = new Int32Rect(0, 0, screen.PixelWidth, screen.PixelHeight);
                        break;
                }

                //保存
                BitmapSource bitmap = MakeBitmapForSave(screen, rect);
                string fullPath = MakeFullPath(neko, MakeStringNowTime(), MyAppConfig.ImageType.ToString());
                try
                {
                    SaveBitmap(bitmap, fullPath);
                    //MessageBox.Show("保存した");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"保存できなかった\n{ex}");
                }
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
            UnregisterHotKey(MyWindowHandle, hotkeyId);

            int mod = GetModifierKeySum();
            if (RegisterHotKey(MyWindowHandle, hotkeyId, mod, vKey) == 0)
            {
                MessageBox.Show("登録に失敗");
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

        private int GetModifierKeySum()
        {
            int mod = 0;
            if (MyAppConfig.HotkeyAlt) mod += (int)ModifierKeys.Alt;
            if (MyAppConfig.HotkeyCtrl) mod += (int)ModifierKeys.Control;
            if (MyAppConfig.HotkeyShift) mod += (int)ModifierKeys.Shift;
            if (MyAppConfig.HotkeyWin) mod += (int)ModifierKeys.Windows;
            return mod;
        }

        #endregion


        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //MyTimer = new DispatcherTimer();
            //MyTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            //MyTimer.Tick += MyTimer_Tick;
            //MyTimer.Start();

            //MyComboBoxHotKey.SelectionChanged += (s, e) => { vHotKey = KeyInterop.VirtualKeyFromKey(MyAppConfig.HotKey); };
            MyComboBoxHotKey.SelectionChanged += (s, e) => { ChangeHotKey(MyAppConfig.HotKey, HOTKEY_ID1); };

            MyCheckAlt.Click += MyCheckModKey_Click;
            MyCheckCtrl.Click += MyCheckModKey_Click;
            MyCheckShift.Click += MyCheckModKey_Click;
            MyCheckWin.Click += MyCheckModKey_Click;
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
        private void SetCursorInfo()
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


            ////PringWindowを使ったキャプチャはWindow7のウィンドウになるし、タイトル文字が透明
            //IntPtr bb = CreateCompatibleBitmap(screenDC, width, height);
            //SelectObject(memDC, bb);
            ////PrintWindow(GetForegroundWindow(), memDC,nFrags_PW_CLIENTONLY);//クライアント領域
            //PrintWindow(GetForegroundWindow(), memDC, 0);//ウィンドウ
            //var bmp = Imaging.CreateBitmapSourceFromHBitmap(bb, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());




            //後片付け
            DeleteObject(hBmp);
            ReleaseDC(IntPtr.Zero, screenDC);
            ReleaseDC(IntPtr.Zero, memDC);





            //画像
            return source;
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
            if (SaveConfig2(AppDir + "\\" + APP_CONFIG_FILE_NAME))
            {
                MessageBox.Show("保存しました");
            }
            else { MessageBox.Show("保存できなかった"); };
        }

        //アプリの設定保存

        private bool SaveConfig2(string path)
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
                MessageBox.Show($"保存できなかった\n{ex.Message}");
                return false;
            }
        }

        private void MyButtonLoadState_Click(object sender, RoutedEventArgs e)
        {
            AppConfig config = LoadConfig2(AppDir + "\\" + APP_CONFIG_FILE_NAME);
            if (config != null)
            {
                MyAppConfig = config;
                this.DataContext = MyAppConfig;
            }
        }

        //アプリの設定読み込み

        private AppConfig LoadConfig2(string path)
        {
            var serealizer = new DataContractSerializer(typeof(AppConfig));
            try
            {
                using (XmlReader xr = XmlReader.Create(path))
                {
                    return (AppConfig)serealizer.ReadObject(xr);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"読み込みできなかった\n{ex.Message}");
                return null;
            }
        }

        #endregion


        private void MyTestButton_Click(object sender, RoutedEventArgs e)
        {
            //MyAppConfig.ImageType = ImageType.jpg;
            //MyAppConfig.DirList.Add("dummy dir");
            var neko = MyComboBoxCaputureRect.SelectedValue;
            var unu = MyRadioButtonFileNameDate.IsChecked;
            var tako = MyAppConfig;
        }

        #region 保存先リスト追加と削除
        //保存フォルダをリストに追加
        private void ButtonSaveDirectoryAdd_Click(object sender, RoutedEventArgs e)
        {
            //フォルダ指定あり
            string folderPath;
            folderPath = MyComboBoxSaveDirectory.Text;//表示しているテキスト
                                                      //if (System.IO.Directory.Exists(folderPath))
                                                      //{
                                                      //    AddDir(folderPath);
                                                      //}

            FolderDialog dialog = new FolderDialog(folderPath, this);

            //フォルダ指定なし
            //FolderDialog dialog = new FolderDialog(this);

            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string path = dialog.GetFullPath();
                //AddDir(path);
                AddTextToComboBox(path, MyAppConfig.DirList, MyComboBoxSaveDirectory);
            }
        }
        //ComboBoxのItemsSourceのBinding先のリストにに文字列を追加
        private void AddTextToComboBox(string text, ObservableCollection<string> stringList, ComboBox combo)
        {
            Binding neko = BindingOperations.GetBinding(combo, ComboBox.ItemsSourceProperty);
            BindingExpression inu = BindingOperations.GetBindingExpression(combo, ItemsControl.ItemsSourceProperty);
            
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
            int item = MyComboBoxSaveDirectory.SelectedIndex;
            if (item < 0) return;
            if (MessageBox.Show($"{MyComboBoxSaveDirectory.SelectedValue}を\nリストから削除します",
                                "確認",
                                MessageBoxButton.OKCancel)
                == MessageBoxResult.OK)
            {
                //削除
                MyAppConfig.DirList.RemoveAt(item);
                //削除後に表示するitem
                if (item == MyAppConfig.DirList.Count || MyAppConfig.DirList.Count == 0)
                {
                    //削除アイテムがリストの最後か最初なら、Index-1
                    MyComboBoxSaveDirectory.SelectedIndex = item - 1;
                }
                else
                {
                    //中間だった場合は同じIndexでいい
                    MyComboBoxSaveDirectory.SelectedIndex = item;
                }
            }
        }
        #endregion

        #region 画像保存

        private BitmapSource MakeBitmapForSave(BitmapSource source, Int32Rect rect)
        {
            BitmapSource bitmap;
            if (MyAppConfig.IsDrawCursor == true)
            {
                if (IsMaskUse)
                {
                    bitmap = DrawCursorOnBitmapWithMask(source);
                }
                else
                {
                    bitmap = DrawCursorOnBitmap(source);
                }
            }
            else { bitmap = source; }

            return new CroppedBitmap(bitmap, rect);

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
                using (var fs = new System.IO.FileStream(
                    fullPath, System.IO.FileMode.Create, System.IO.FileAccess.Write))
                {
                    encoder.Save(fs);
                }
            }
            catch (Exception)
            {
                throw;
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
            string software = AppName + "_" + AppVersion;
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
                    data = new BitmapMetadata("tiff");
                    data.ApplicationName = software;
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
                    var jpeg = new JpegBitmapEncoder();
                    jpeg.QualityLevel = MyAppConfig.JpegQuality;
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
            string str = dt.ToString("yyyyMMdd" + "_" + "HH" + "_" + "mm" + "_" + "ss" + "_" + "fff");
            return str;
        }

        private string MakeFileName()
        {
            string fileName = "";
            if (MyAppConfig.FileNameBaseType == FileNameBaseType.Date)
            {
                //fileName = DateTime.Now.ToString(MyAppConfig.da;
            }
            else
            {

            }
            return fileName;
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

        private void MyButtonAddFileNamePrefix_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            ComboBox cb = button.Tag as ComboBox;
            if (cb == null) return;
            string text = cb.Text;
            AddTextToComboBox(text, MyAppConfig.FileNamePrefixList, cb);
        }
    }






    [DataContract]
    public class AppConfig : System.ComponentModel.INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DataMember]
        public int JpegQuality { get; set; }//jpeg画質
        [DataMember] public double Top { get; set; }//アプリ
        [DataMember] public double Left { get; set; }//アプリ
        //保存先リスト
        [DataMember] public System.Collections.ObjectModel.ObservableCollection<string> DirList { get; set; }
        [DataMember] public string Dir { get; set; }
        [DataMember] public int DirIndex { get; set; }

        [DataMember] public bool? IsDrawCursor { get; set; }//マウスカーソル描画の有無

        //ホットキー
        [DataMember] public bool HotkeyAlt { get; set; }
        [DataMember] public bool HotkeyCtrl { get; set; }
        [DataMember] public bool HotkeyShift { get; set; }
        [DataMember] public bool HotkeyWin { get; set; }
        [DataMember] public Key HotKey { get; set; }//キャプチャーキー

        //ファイルネーム        
        [DataMember] public FileNameBaseType FileNameBaseType { get; set; }

        [DataMember] public bool IsFileNamePrefix { get; set; }
        [DataMember] public string FileNamePrefix { get; set; }
        [DataMember] public ObservableCollection<string> FileNamePrefixList { get; set; } = new();

        [DataMember] public bool IsFileNamePrefixConnect { get; set; }
        [DataMember] public string FileNamePrefixConnect { get; set; }
        [DataMember] public ObservableCollection<string> FileNamePrefixConnectList { get; set; } = new();

        [DataMember] public bool IsFileNameSuffix { get; set; }
        [DataMember] public string FileNameSuffix { get; set; }
        [DataMember] public ObservableCollection<string> FileNameSuffixList { get; set; } = new();

        [DataMember] public bool IsFileNameSuffixConnect { get; set; }
        [DataMember] public string FileNameSuffixConnect { get; set; }
        [DataMember] public ObservableCollection<string> FileNameSuffixConnectList { get; set; } = new();



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
            DirList = new System.Collections.ObjectModel.ObservableCollection<string>();
            JpegQuality = 94;
            IsDrawCursor = true;

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

    }

    //ラジオボタンとenumのコンバーター
    public class FileNameBaseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramString = parameter as string;
            if (paramString == null) { return DependencyProperty.UnsetValue; }

            if (!Enum.IsDefined(value.GetType(), value)) { return Binding.DoNothing; }
            //if (!Enum.IsDefined(value.GetType(), value)) { return DependencyProperty.UnsetValue; }

            var paramValue = Enum.Parse(value.GetType(), paramString);
            return paramValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var paramString = parameter as string;
            if (paramString == null) { return DependencyProperty.UnsetValue; }

            if (true.Equals(value)) { return Enum.Parse(targetType, paramString); }
            else return Binding.DoNothing;
            //else return DependencyProperty.UnsetValue;//こっちだとラジオボタンに赤枠がつく
        }
    }
    public enum FileNameBaseType
    {
        Date,
        Serial,
    }


    public class StringFormatDigitConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {

            int digit = decimal.ToInt32((decimal)value);
            string format = "";
            for (int i = 0; i < digit; i++)
            {
                format += "0";
            }
            return format;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
