using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

//クリップボードの更新を監視、AddClipboardFormatListener - 午後わてんのブログ
//https://gogowaten.hatenablog.com/entry/2019/09/22/143931

namespace Pixcren
{
    /// <summary>
    /// AddClipboardFormatListenerを使ったクリップボード監視
    /// クリップボード更新されたらDrawClipboardイベント起動
    /// </summary>
    public class ClipboardWatcher
    {
        [DllImport("user32.dll")]
        private static extern bool AddClipboardFormatListener(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern bool RemoveClipboardFormatListener(IntPtr hWnd);

        private const int WM_DRAWCLIPBOARD = 0x031D;

        IntPtr handle;
        HwndSource hwndSource = null;

        //イベント登録
        public event EventHandler DrawClipboard;

        //イベント起動
        private void RaiseDrawClipboard()
        {
            DrawClipboard?.Invoke(this, EventArgs.Empty);
        }
        //↑は↓と同じ意味
        //private void raiseDrawClipboard()
        //{
        //    if (DrawClipboard != null)
        //    {
        //        DrawClipboard(this, EventArgs.Empty);
        //    }
        //}


        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            //更新通知が来たらイベント起動
            if (msg == WM_DRAWCLIPBOARD)
            {
                this.RaiseDrawClipboard();//イベント起動
                handled = true;//コレがよくわからん
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="handle">System.Windows.Interop.WindowInteropHelper(this).Handleとかで取得</param>
        public ClipboardWatcher(IntPtr handle)
        {
            hwndSource = HwndSource.FromHwnd(handle);
            hwndSource.AddHook(WndProc);
            this.handle = handle;
        }

        //クリップボード監視開始
        public void Start()
        {
            AddClipboardFormatListener(handle);
        }

        //クリップボード監視停止
        public void Stop()
        {
            RemoveClipboardFormatListener(handle);
        }
    }
}

