using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using Windows.Foundation;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Shell;
using Windows.Win32.UI.Shell.Common;
using Windows.Win32.UI.WindowsAndMessaging;

namespace GLWinUI3Control
{
    public class GLWinUI3Control : Control, IDisposable
    {
        #region イベント
        /// <summary>
        /// コントロールの初期化完了時に発生します。
        /// </summary>
        public event Action? Initialized;
        #endregion

        #region イベント発行
        /// <summary>
        /// コントロールの初期化完了時に実行します。
        /// </summary>
        protected void OnInitialized()
        {
            Initialized?.Invoke();
        }
        #endregion

        #region イベント処理
        /// <summary>
        /// このコントロールのサイズ変更時に呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLWinUI3Control_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateWindowLayout();
        }

        /// <summary>
        /// このコントロールのサイズ変更時に呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLWinUI3Control_LayoutUpdated(object? sender, object e)
        {
            UpdateWindowLayout();
        }

        /// <summary>
        /// このコントロールがオブジェクトツリーに追加され準備ができたときに呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLWinUI3Control_Loaded(object sender, RoutedEventArgs e)
        {
            CreateControl();

            Initialize();
        }

        /// <summary>
        /// このコントロールがオブジェクトツリーに除去されたときに呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GLWinUI3Control_Unloaded(object sender, RoutedEventArgs e)
        {
            DestroyControl(true);
        }
        #endregion

        #region メンバ変数
        /// <summary>
        /// GL Context
        /// </summary>
        protected GLWinUI3Context? _glContext;
        /// <summary>
        /// このコントロールを設定を保持します。
        /// </summary>
        private GLWinUI3ControlSettings _settings;

        /// <summary>
        /// Win32 Windowのハンドルを保持します。
        /// </summary>
        private HWND _win32Window = HWND.Null;

        /// <summary>
        /// 描画可能領域を定義します。
        /// </summary>
        private Box2i _drawableArea = Box2i.Empty;

        /// <summary>
        /// Disposeが呼ばれたかを表します。
        /// </summary>
        protected bool _isDisposed = false;

        /// <summary>
        /// 初期化済みかを表します。
        /// </summary>
        private bool _initialized = false;
        #endregion

        #region プロパティ
        /// <summary>
        /// 描画可能領域を取得します。
        /// </summary>
        public Box2i DrawableArea { get => _drawableArea; }
        #endregion

        #region コンストラクタ
        public GLWinUI3Control() : this(null)
        {
        }

        public GLWinUI3Control(GLWinUI3ControlSettings? settings = null)
        {
            this.DefaultStyleKey = typeof(GLWinUI3Control);

            this._settings = settings ?? new GLWinUI3ControlSettings();

            this.Loaded += GLWinUI3Control_Loaded;
            this.Unloaded += GLWinUI3Control_Unloaded;
            this.SizeChanged += GLWinUI3Control_SizeChanged;
            this.LayoutUpdated += GLWinUI3Control_LayoutUpdated;
        }

        ~GLWinUI3Control()
        {
            Dispose(false);
        }

        protected virtual unsafe void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
            }

            DestroyControl(disposing);

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        #region メソッド
        /// <summary>
        /// 
        /// </summary>
        public void MakeCurrent()
        {
            if (_glContext == null)
            {
                Trace.WriteLine("ERROR: _glContext is null");
                return;
            }
            _glContext.MakeCurrent();
        }

        /// <summary>
        /// 
        /// </summary>
        public void MakeNoneCurrent()
        {
            if (_glContext == null)
            {
                Trace.WriteLine("ERROR: _glContext is null");
                return;
            }
            _glContext.MakeNoneCurrent();
        }

        /// <summary>
        /// 
        /// </summary>
        public void SwapBuffers()
        {
            if (_glContext == null)
            {
                Trace.WriteLine("ERROR: _glContext is null");
                return;
            }
            _glContext.NativeWindow.Context.SwapBuffers();
        }

        /// <summary>
        /// 初期化処理を行います。
        /// </summary>
        protected virtual void Initialize()
        {
            OnInitialized();
        }

        /// <summary>
        /// OpenGLの描画エリアを作成します。
        /// </summary>
        /// <remarks>
        /// 描画エリアはOpenTKのNativeWindowを使用する
        /// NativeWindowをUIElement上に配置するため、次の手順を行う
        /// 1. Win32APIを使用して、Windowを作成し、UIElement上に配置する
        ///     (正確には、UIElementで確保しているスペースを覆い隠すようにWindowのサイズと配置を調整して、実現している)
        /// 2. NativeWindowを作成し、Windowを親にする
        /// </remarks>
        /// <exception cref="NotSupportedException"></exception>
        private void CreateControl()
        {
            if (!_initialized)
            {
                HWND hWnd = GetWindowHandle();
                if (hWnd.IsNull)
                {
                    throw new NotSupportedException();
                }

                // 1.
                CreateWindowEx(hWnd);

                // 2.
                CreateNativeWindow();

                _initialized = true;

                UpdateWindowLayout();
            }
        }

        /// <summary>
        /// OpenGLの描画エリアを破棄します。
        /// </summary>
        /// <param name="disposing"></param>
        private void DestroyControl(bool disposing)
        {
            if (_initialized)
            {
                _initialized = false;

                DestroyNativeWindow(disposing);
                DestroyWindowEx();
            }
        }

        /// <summary>
        /// Win32APIを使用したWindowを作成します。
        /// </summary>
        /// <param name="hWnd">親になるウィンドウハンドル</param>
        private unsafe void CreateWindowEx(HWND hWnd)
        {
            const int opacity = 100;

            _win32Window = PInvoke.CreateWindowEx(WINDOW_EX_STYLE.WS_EX_TRANSPARENT | WINDOW_EX_STYLE.WS_EX_LAYERED, "Static", "", WINDOW_STYLE.WS_VISIBLE | WINDOW_STYLE.WS_CHILD, 0, 0, 100, 100, hWnd, null, null, null);

            PInvoke.SetLayeredWindowAttributes(_win32Window, new COLORREF(0), (byte)(255 * opacity / 100), LAYERED_WINDOW_ATTRIBUTES_FLAGS.LWA_ALPHA).ToString();

            PInvoke.ShowWindow(_win32Window, SHOW_WINDOW_CMD.SW_SHOWNORMAL).ToString();
        }

        /// <summary>
        /// Windowを破棄します。
        /// </summary>
        private void DestroyWindowEx()
        {
            if (!_win32Window.IsNull)
            {
                PInvoke.DestroyWindow(_win32Window);
                _win32Window = HWND.Null;
            }
        }

        /// <summary>
        /// NativeWindowを作成します。
        /// </summary>
        private void CreateNativeWindow()
        {
            _glContext = new GLWinUI3Context(_settings);

            SetParent(_glContext.NativeWindow);

            // And now show the child window, since it hasn't been made visible yet.
            _glContext.NativeWindow.IsVisible = true;
        }

        /// <summary>
        /// NativeWindowを破棄します。
        /// </summary>
        /// <param name="disposing"></param>
        private void DestroyNativeWindow(bool disposing)
        {
            if (disposing)
            {
                _glContext?.Dispose();
                _glContext = null;
            }
        }

        /// <summary>
        /// NativeWindowの親をWindowに設定します。
        /// </summary>
        /// <param name="nativeWindow">The NativeWindow that must become a child of win32window.</param>
        private unsafe void SetParent(NativeWindow nativeWindow)
        {
            HWND hWnd = new HWND(GLFW.GetWin32Window(nativeWindow.WindowPtr));

            // Reparent the real HWND under win32window.
            PInvoke.SetParent(hWnd, _win32Window);

            // Change the real HWND's window styles to be "WS_CHILD | WS_DISABLED" (i.e.,
            // a child of some container, with no input support), and turn off *all* the
            // other style bits (most of the rest of them could cause trouble).  In
            // particular, this turns off stuff like WS_BORDER and WS_CAPTION and WS_POPUP
            // and so on, any of which GLFW might have turned on for us.
            IntPtr style = (IntPtr)(long)(WINDOW_STYLE.WS_CHILD | WINDOW_STYLE.WS_DISABLED);
            PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_STYLE, style);

            // Change the real HWND's extended window styles to be "WS_EX_NOACTIVATE", and
            // turn off *all* the other extended style bits (most of the rest of them
            // could cause trouble).  We want WS_EX_NOACTIVATE because we don't want
            // Windows mistakenly giving the GLFW window the focus as soon as it's created,
            // regardless of whether it's a hidden window.
            style = (IntPtr)(long)WINDOW_EX_STYLE.WS_EX_NOACTIVATE;
            PInvoke.SetWindowLongPtr(hWnd, WINDOW_LONG_PTR_INDEX.GWL_EXSTYLE, style);
        }

        /// <summary>
        /// このコントロールに紐づくウィンドウハンドルを取得します。
        /// https://stackoverflow.com/questions/74273875/retrive-window-handle-in-class-library-winui3
        /// </summary>
        /// <returns>ウィンドウハンドル</returns>
        private HWND GetWindowHandle()
        {
            var windowId = this.XamlRoot?.ContentIslandEnvironment?.AppWindowId;
            if (!windowId.HasValue)
            {
                return HWND.Null;
            }

            return new HWND(Win32Interop.GetWindowFromWindowId(windowId.Value));
        }

        /// <summary>
        /// サイズやレイアウト変更に伴うウィンドウの更新を行います。
        /// </summary>
        private void UpdateWindowLayout()
        {
            if (!_initialized)
            {
                return;
            }

            // 最上位のFrameElementを取得(多くの場合、MainWindow.xaml直下のコントロール)
            FrameworkElement? element = this.Parent as FrameworkElement;
            FrameworkElement? oldElement = null;
            while (element != null)
            {
                oldElement = element;
                element = element.Parent as FrameworkElement;
            }

            // 最上位のFrameElementに対するこのコントロールの相対座標を取得
            Point pos = TransformToVisual(oldElement).TransformPoint(new Point(0, 0));

            // プライマリモニターの拡大率を取得
            DEVICE_SCALE_FACTOR scaleFactor = PInvoke.GetScaleFactorForDevice(DISPLAY_DEVICE_TYPE.DEVICE_PRIMARY);
            double scale = (int)scaleFactor / 100.0;

            // 描画可能範囲を計算
            // 計算結果を切り捨てると、コントロール表示に空洞ができてしまうため、四捨五入する
            _drawableArea = new Box2i(0, 0, (int)Math.Round(this.ActualWidth * scale), (int)Math.Round(this.ActualHeight * scale));
            if (_glContext != null)
            {
                _glContext.NativeWindow.ClientRectangle = _drawableArea;
            }
            else
            {
                Trace.WriteLine("ERROR: _glContext is null");
            }

            // _win32Windowのサイズと位置をこのコントロールのサイズと位置に合わせる
            PInvoke.MoveWindow(_win32Window, (int)Math.Round(pos.X * scale), (int)Math.Round(pos.Y * scale), (int)Math.Round(this.ActualWidth * scale), (int)Math.Round(this.ActualHeight * scale), false);
        }
        #endregion
    }
}
