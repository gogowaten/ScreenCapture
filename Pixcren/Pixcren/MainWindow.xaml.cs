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

namespace Pixcren
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region WindowsAPI^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
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

        private string AppDir;
        private const string APP_CONFIG_FILE_NAME = "config.xml";
        private AppConfig MyAppConfig;
        private int vHotKey;//ホットキーの仮想キーコード

        //マウスカーソル情報
        private POINT MyCursorPoint;//座標        
        private int MyCursorHotspotX;//ホットスポット
        private int MyCursorHotspotY;//ホットスポット
        private BitmapSource MyBitmapCursor;//画像
        private BitmapSource MyBitmapCursorMask;//マスク画像
        private bool IsMaskUse;//マスク画像使用の有無判定用

        private BitmapSource MyBitmapScreen;//全画面画像

        //各Rect
        private List<MyRectInfo> MyRects;
        private Dictionary<CaptureRectType, MyRectRect> MyRectRects;
        private Dictionary<CaptureRectType, string> MyDCRectName;
        private Dictionary<CaptureRectType, Int32Rect> MyDCRectRect;


        //タイマー
        private System.Windows.Threading.DispatcherTimer MyTimer;


        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
            this.Closing += (s, e) => { MyTimer.Stop(); };


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
            MyComboBoxTest.ItemsSource = MyDCRectName;
            MyDCRectRect = new Dictionary<CaptureRectType, Int32Rect>();



            //MyRects = new List<MyRectInfo>() {
            //    new MyRectInfo(CaptureRectType.Screen, "全画面"),
            //    new MyRectInfo(CaptureRectType.Window, "ウィンドウ"),
            //    new MyRectInfo(CaptureRectType.WindowClient, "ウィンドウのクライアント領域"),
            //    new MyRectInfo(CaptureRectType.UnderCursor, "カーソル下のコントロール"),
            //    new MyRectInfo(CaptureRectType.UnderCursorClient, "カーソル下のクライアント領域"),
            //};
            //MyComboBoxTest.ItemsSource = MyRects;

            //MyRectRects = new Dictionary<CaptureRectType, MyRectRect>();
            //MyRectRects.Add(CaptureRectType.Screen, new MyRectRect("全画面"));
            //MyRectRects.Add(CaptureRectType.Window, new MyRectRect("ウィンドウ"));
            //MyRectRects.Add(CaptureRectType.WindowClient, new MyRectRect("ウィンドウのクライアント領域"));
            //MyRectRects.Add(CaptureRectType.UnderCursor, new MyRectRect("カーソル下のコントロール"));
            //MyRectRects.Add(CaptureRectType.UnderCursorClient, new MyRectRect("カーソル下のクライアント領域"));
            //MyRectRects[CaptureRectType.Screen].Rect = new Rect();

            MyComboBoxHotKey.ItemsSource = Enum.GetValues(typeof(Key));

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            MyTimer = new DispatcherTimer();
            MyTimer.Interval = new TimeSpan(0, 0, 0, 0, 100);
            MyTimer.Tick += MyTimer_Tick;
            MyTimer.Start();

            MyComboBoxHotKey.SelectionChanged += (s, e) => { vHotKey = KeyInterop.VirtualKeyFromKey(MyAppConfig.HotKey); };

        }

        private void MyTimer_Tick(object sender, EventArgs e)
        {
            short keystate = GetAsyncKeyState(vHotKey);

            if ((keystate & 1) == 1)
            {
                //カーソル座標取得
                GetCursorPos(out MyCursorPoint);

                //カーソル画像取得
                SetCursorInfo();

                //画面全体画像取得
                MyBitmapScreen = ScreenCapture();

                ////RECT取得
                SetRect();

                //UpdateImage();
            }
        }

       
        //ウィンドウのRECTを取得して保持
        private void SetRect()
        {
            //ウィンドウハンドルの取得
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
            //見た目通りのWindowRectを取得
            RECT myRECT;
            DwmGetWindowAttribute(hWnd,
                                  DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS,
                                  out myRECT,
                                  Marshal.SizeOf(typeof(RECT)));
            //RECTからクロップ用のInt32Rectを作成、登録
            MyDCRectRect[CaptureRectType.Window] =
                MakeCroppRectFromRECT(myRECT, MyBitmapScreen.PixelWidth, MyBitmapScreen.PixelHeight);


            //ウィンドウのクライアント領域のRECT
            POINT myPOINT;
            ClientToScreen(hWnd, out myPOINT);
            GetClientRect(hWnd, out myRECT);
            MyDCRectRect[CaptureRectType.WindowClient] =
                MakeCroppRectFromClientRECT(myRECT, myPOINT, MyBitmapScreen.PixelWidth, MyBitmapScreen.PixelHeight);

            //カーソル下のコントロールのRECT、WindowTextが無しならGetWindowRect、ありならEXTENDED_FRAMEを使って取得
            hWnd = WindowFromPoint(MyCursorPoint);
            wndText = new StringBuilder(65535);
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
            MyDCRectRect[CaptureRectType.UnderCursor] =
                MakeCroppRectFromRECT(myRECT, MyBitmapScreen.PixelWidth, MyBitmapScreen.PixelHeight);

            //カーソル下のクライアント領域のRECT
            POINT myPOINT2;
            ClientToScreen(hWnd, out myPOINT2);
            GetClientRect(hWnd, out myRECT);
            MyDCRectRect[CaptureRectType.UnderCursorClient] =
                MakeCroppRectFromClientRECT(myRECT, myPOINT2, MyBitmapScreen.PixelWidth, MyBitmapScreen.PixelHeight);

            var n1 = new CroppedBitmap(MyBitmapScreen, MyDCRectRect[CaptureRectType.Window]);
            var n2 = new CroppedBitmap(MyBitmapScreen, MyDCRectRect[CaptureRectType.WindowClient]);
            var n3 = new CroppedBitmap(MyBitmapScreen, MyDCRectRect[CaptureRectType.UnderCursor]);
            var n4 = new CroppedBitmap(MyBitmapScreen, MyDCRectRect[CaptureRectType.UnderCursorClient]);
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


            //PringWindowを使ったキャプチャはWindow7のウィンドウになるし、タイトル文字が透明
            IntPtr bb = CreateCompatibleBitmap(screenDC, width, height);
            SelectObject(memDC, bb);
            //PrintWindow(GetForegroundWindow(), memDC,nFrags_PW_CLIENTONLY);//クライアント領域
            PrintWindow(GetForegroundWindow(), memDC, 0);//ウィンドウ
            var bmp = Imaging.CreateBitmapSourceFromHBitmap(bb, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());




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


        #region 設定保存と読み込み
        private void MyButtonSaveState_Click(object sender, RoutedEventArgs e)
        {
            if (SaveConfig(AppDir + "\\" + APP_CONFIG_FILE_NAME))
            {
                MessageBox.Show("保存しました");
            }
            else { MessageBox.Show("保存できなかった"); };
        }

        //アプリの設定保存
        private bool SaveConfig(string path)
        {
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(AppConfig));
            try
            {
                using (var writer = new System.IO.StreamWriter(path, false, new UTF8Encoding(false)))
                {
                    serializer.Serialize(writer, MyAppConfig);
                };
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
            AppConfig config = LoadConfig(AppDir + "\\" + APP_CONFIG_FILE_NAME);
            if (config != null)
            {
                MyAppConfig = config;
                this.DataContext = MyAppConfig;
            }
        }

        //アプリの設定読み込み
        private AppConfig LoadConfig(string path)
        {
            var serealizer = new System.Xml.Serialization.XmlSerializer(typeof(AppConfig));
            try
            {
                using (var stream = new System.IO.StreamReader(path, new UTF8Encoding(false)))
                {
                    return (AppConfig)serealizer.Deserialize(stream);
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
            MyAppConfig.ImageType = ImageType.Jpeg;
            MyAppConfig.DirList.Add("dummy dir");
            var neko = MyComboBoxTest.SelectedValue;
            //this.DataContext = MyAppConfig;
        }

        #region 保存先リスト追加と削除
        //保存フォルダをリストに追加
        private void ButtonSaveDirectoryAdd_Click(object sender, RoutedEventArgs e)
        {
            //フォルダ指定あり
            string folderPath;
            folderPath = (string)ComboBoxSaveDirectory.SelectedValue;
            FolderDialog dialog = new FolderDialog(folderPath, this);

            //フォルダ指定なし
            //FolderDialog dialog = new FolderDialog(this);

            dialog.ShowDialog();
            if (dialog.DialogResult == true)
            {
                string path = dialog.GetFullPath();
                int itemIndex = MyAppConfig.DirList.IndexOf(path);
                //リストにないパスの場合は普通に追加
                if (itemIndex == -1)
                {
                    MyAppConfig.DirList.Add(path);
                    ComboBoxSaveDirectory.SelectedIndex = MyAppConfig.DirList.Count - 1;
                }
                //リストにあるパスだったら、そのパスをリストの先頭に移動
                else
                {
                    //リストのコピーを作って、そこから順に元リストに入れていく
                    var list = MyAppConfig.DirList.ToList();
                    MyAppConfig.DirList[0] = list[itemIndex];//先頭
                    list.RemoveAt(itemIndex);
                    //先頭以外を順に
                    for (int i = 0; i < list.Count; i++)
                    {
                        MyAppConfig.DirList[i + 1] = list[i];
                    }
                    ComboBoxSaveDirectory.SelectedIndex = 0;
                }
            }
        }

        //保存フォルダリスト、表示しているアイテム削除
        private void ButtonSaveDirectoryDelete_Click(object sender, RoutedEventArgs e)
        {
            int item = ComboBoxSaveDirectory.SelectedIndex;
            if (item < 0) return;
            if (MessageBox.Show($"{ComboBoxSaveDirectory.SelectedValue}を\nリストから削除します",
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
                    ComboBoxSaveDirectory.SelectedIndex = item - 1;
                }
                else
                {
                    //中間だった場合は同じIndexでいい
                    ComboBoxSaveDirectory.SelectedIndex = item;
                }
            }
        }
        #endregion





    }


    public class MyRectInfo
    {
        private Rect rect;

        public CaptureRectType CaptureRectType { get; set; }
        public Rect Rect { get; set; }

        public string RectName { get; set; }

        public MyRectInfo(CaptureRectType type, string name)
        {
            CaptureRectType = type;
            RectName = name;
        }
    }


    public class MyRectRect
    {
        public Rect Rect { get; set; }
        public string RectName { get; set; }
        public BitmapSource BitmapSource { get; set; }
        public MyRectRect(string name)
        {
            RectName = name;
            var a = new MyRectCollection();
            a.Add(CaptureRectType.Screen, new MyRectInfo(CaptureRectType.Screen, ""));
            var neko = a[CaptureRectType.Screen];

        }

    }
    public class MyRectCollection : Dictionary<CaptureRectType, MyRectInfo>
    {
        public Rect Rect;
        public void SetRect(Rect rect)
        {

        }
    }
    [Serializable]
    public class AppConfig : System.ComponentModel.INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void RaisePropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        public int JpegQuality { get; set; }//jpeg画質
        public double Top { get; set; }//アプリ
        public double Left { get; set; }//アプリ
        //保存先リスト
        public System.Collections.ObjectModel.ObservableCollection<string> DirList { get; set; }
        public string Dir { get; set; }
        public bool? IsDrawCursor { get; set; }//マウスカーソル描画の有無
        public Key HotKeyModifier1 { get; set; }//修飾キー1
        public Key HotKeyModifier2 { get; set; }//修飾キー2
        public Key HotKey { get; set; }//キャプチャーキー


        private ImageType _ImageType;//保存画像形式
        private CaptureRectType _RectType;//切り出し範囲





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
        Jpeg,
        gif,

    }
    public enum CaptureRectType
    {
        Screen,
        Window,
        WindowClient,
        UnderCursor,
        UnderCursorClient,

    }

}
