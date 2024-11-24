using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.UI;

namespace GLWinUI3Control
{
    public sealed partial class GLWinUI3Control : UserControl, IDisposable
    {
        private class ImageData
        {
            public byte[] Data { get; }
            public int Width { get; }
            public int Height { get; }

            public ImageData(byte[] data, int width, int height)
            {
                Data = data;
                Width = width;
                Height = height;
            }
        }

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
        private void OnInitialized()
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

            OnInitialized();
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

        /// <summary>
        /// canvasの描画更新時に呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CanvasControl_Draw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            CanvasBitmap bitmap;
            lock (_lock)
            {
                if (_imageData != null)
                {
                    bitmap = CanvasBitmap.CreateFromBytes(canvas.Device, _imageData.Data, _imageData.Width, _imageData.Height, Windows.Graphics.DirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized);
                }
                else
                {
                    return;
                }
            }
            args.DrawingSession.DrawImage(bitmap);
        }
        #endregion

        #region メンバ変数
        /// <summary>
        /// GL Context
        /// </summary>
        private GLWinUI3Context? _glContext;

        /// <summary>
        /// このコントロールを設定を保持します。
        /// </summary>
        private GLWinUI3ControlSettings _settings;

        /// <summary>
        /// 画面クリア時の色を定義します。
        /// </summary>
        private Color _clearColor;

        /// <summary>
        /// 描画可能領域を定義します。
        /// </summary>
        private Box2i _drawableArea = Box2i.Empty;

        /// <summary>
        /// Disposeが呼ばれたかを表します。
        /// </summary>
        private bool _isDisposed = false;

        /// <summary>
        /// 初期化済みかを表します。
        /// </summary>
        private bool _initialized = false;

        /// <summary>
        /// 作業領域を定義します。
        /// </summary>
        private IntPtr _tmpBuf = IntPtr.Zero;

        /// <summary>
        /// 作業領域の確保済みのサイズを定義します。
        /// </summary>
        private int _allocedSize = 0;

        /// <summary>
        /// 表示データを定義します。
        /// </summary>
        private ImageData? _imageData;

        /// <summary>
        /// ロックオブジェクトを定義します。
        /// </summary>
        private object _lock = new object();
        #endregion

        #region プロパティ
        /// <summary>
        /// 描画可能領域を取得します。
        /// </summary>
        public Box2i DrawableArea { get => _drawableArea; }

        /// <summary>
        /// 画面クリア時の色を設定取得します。
        /// </summary>
        public Color ClearColor
        {
            get
            {
                return _clearColor;
            }
            set
            {
                _clearColor = value;
                canvas.ClearColor = _clearColor;
            }
        }
        #endregion

        #region コンストラクタ
        public GLWinUI3Control() : this(null)
        {
        }

        public GLWinUI3Control(GLWinUI3ControlSettings? settings = null)
        {
            this.InitializeComponent();

            this._settings = settings ?? new GLWinUI3ControlSettings();

            this.Loaded += GLWinUI3Control_Loaded;
            this.Unloaded += GLWinUI3Control_Unloaded;
            this.SizeChanged += GLWinUI3Control_SizeChanged;
            this.LayoutUpdated += GLWinUI3Control_LayoutUpdated;
            this.canvas.Draw += CanvasControl_Draw;
        }

        ~GLWinUI3Control()
        {
            Dispose(false);
        }

        private unsafe void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
            }

            DestroyControl(disposing);
            if (_tmpBuf != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_tmpBuf);
                _allocedSize = 0;
            }

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
        public unsafe void SwapBuffers()
        {
            if (GetImageData())
            {
                canvas.Invalidate();
            }
        }

        /// <summary>
        /// OpenGLのレンダリング結果を取得します。
        /// </summary>
        /// <returns>成否</returns>
        private bool GetImageData()
        {
            if (_glContext == null)
            {
                Trace.WriteLine("ERROR: _glContext is null");
                return false;
            }

            int[] dims = new int[4];
            GL.GetInteger(GetPName.Viewport, dims);
            int width = dims[2];
            int height = dims[3];
            int requestSize = 4 * width * height;

            // 作業領域の確保
            if (requestSize != _allocedSize)
            {
                if (_tmpBuf != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(_tmpBuf);
                }

                _tmpBuf = Marshal.AllocHGlobal(requestSize);
                _allocedSize = requestSize;
            }

            // OpenGLのレンダリング結果を読み取る
            GL.ReadBuffer(ReadBufferMode.BackLeft);
            GL.ReadPixels(0, 0, width, height, PixelFormat.Rgba, PixelType.UnsignedInt8888, _tmpBuf);

            lock (_lock)
            {
                byte[] imageBytes;
                if (_imageData == null || 4 * _imageData.Width * _imageData.Height != requestSize)
                {
                    imageBytes = new byte[requestSize];
                }
                else
                {
                    imageBytes = _imageData.Data;
                }

                Marshal.Copy(_tmpBuf, imageBytes, 0, requestSize);

                // ABGRの並びをBGRAに変換する
                for (int i = 0; i < 4 * width * height; i += 4)
                {
                    byte a = imageBytes[i];
                    Array.Copy(imageBytes, i + 1, imageBytes, i, 3);
                    imageBytes[i + 3] = a;
                }

                _imageData = new ImageData(imageBytes, width, height);
            }

            return true;
        }

        /// <summary>
        /// OpenGLの描画エリアを作成します。
        /// </summary>
        /// <remarks>
        /// 描画エリアはOpenTKのNativeWindowを使用する
        /// NativeWindowは非表示にして、オフスクリーンの結果をcanvasに描画する
        /// </remarks>
        private void CreateControl()
        {
            if (!_initialized)
            {
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

                canvas.RemoveFromVisualTree();
                canvas = null;
            }
        }

        /// <summary>
        /// NativeWindowを作成します。
        /// </summary>
        private void CreateNativeWindow()
        {
            _glContext = new GLWinUI3Context(_settings);
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
        /// サイズやレイアウト変更に伴うウィンドウの更新を行います。
        /// </summary>
        private void UpdateWindowLayout()
        {
            if (!_initialized)
            {
                return;
            }

            double scale = 1.0;

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
        }
        #endregion
    }
}
