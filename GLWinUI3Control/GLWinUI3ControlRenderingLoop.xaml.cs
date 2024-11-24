using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.UI;

namespace GLWinUI3Control
{
    /// <summary>
    /// Defines the arguments for frame events.
    /// </summary>
    public readonly struct FrameEventExArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FrameEventExArgs"/> struct.
        /// </summary>
        /// <param name="elapsed">The amount of time that has elapsed since the previous event, in seconds.</param>
        /// <param name="drawableArea">描画可能領域</param>
        public FrameEventExArgs(double elapsed, Box2i drawableArea)
        {
            Time = elapsed;
            DrawableArea = drawableArea;
        }

        /// <summary>
        /// Gets how many seconds of time elapsed since the previous event.
        /// </summary>
        public double Time { get; }

        /// <summary>
        /// 描画可能領域
        /// </summary>
        public Box2i DrawableArea { get; }
    }

    public sealed partial class GLWinUI3ControlRL : UserControl, IDisposable
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

        /// <summary>
        /// Occurs when it is time to update a frame. This is invoked before <see cref="RenderFrame"/>.
        /// </summary>
        public event Action<FrameEventExArgs>? UpdateFrame;

        /// <summary>
        /// Occurs when it is time to render a frame. This is invoked after <see cref="UpdateFrequency"/>.
        /// </summary>
        public event Action<FrameEventExArgs>? RenderFrame;
        #endregion

        #region イベント発行
        /// <summary>
        /// コントロールの初期化完了時に実行します。
        /// </summary>
        private void OnInitialized()
        {
            Initialized?.Invoke();
        }

        /// <summary>
        /// Run when the control is ready to update. This is called before <see cref="OnRenderFrame(FrameEventExArgs)"/>.
        /// </summary>
        /// <param name="args">The event arguments for this frame.</param>
        private void OnUpdateFrame(FrameEventExArgs args)
        {
            UpdateFrame?.Invoke(args);
        }

        /// <summary>
        /// Run when the control is ready to render. This is called after <see cref="OnUpdateFrame(FrameEventExArgs)"/>.
        /// </summary>
        /// <param name="args">The event arguments for this frame.</param>
        private void OnRenderFrame(FrameEventExArgs args)
        {
            RenderFrame?.Invoke(args);
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

            // メインスレッドのGLコンテキストを解除する
            MakeNoneCurrent();

            // インスタンス生成時に一時停止にしているため、初期化完了時にpauseの状態を反映する
            this.canvas.Paused = _pause;
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
        /// canvasの描画更新前に呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void Canvas_Update(ICanvasAnimatedControl sender, CanvasAnimatedUpdateEventArgs args)
        {
            OnUpdateFrame(new FrameEventExArgs(args.Timing.ElapsedTime.TotalSeconds, DrawableArea));
        }

        /// <summary>
        /// canvasの描画更新時に呼ばれます。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void CanvasControl_Draw(ICanvasAnimatedControl sender, CanvasAnimatedDrawEventArgs args)
        {
            // loopスレッドにGLコンテキストを設定する
            MakeCurrent();

            OnRenderFrame(new FrameEventExArgs(args.Timing.ElapsedTime.TotalSeconds, DrawableArea));

            GetImageData();

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

        #region 定数
        /// <summary>
        /// Frequency cap for RenderFrame events.
        /// </summary>
        private const double MaxFrequency = 500.0;
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

        /// <summary>
        /// 更新間隔を定義します。
        /// </summary>
        private double _updateFrequency = 60.0;

        /// <summary>
        /// レンダリングループが中断中かのフラグを定義します。
        /// </summary>
        private bool _pause = false;
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

        /// <summary>
        /// Gets or sets a double representing the update frequency, in hertz.
        /// </summary>
        /// <remarks>
        ///  <para>
        /// A value of 0.0 indicates that UpdateFrame events are generated at the maximum possible frequency (i.e. only
        /// limited by the hardware's capabilities).
        ///  </para>
        ///  <para>Values lower than 1.0Hz are clamped to 0.0. Values higher than 500.0Hz are clamped to 500.0Hz.</para>
        /// </remarks>
        public double UpdateFrequency
        {
            get { return _updateFrequency; }
            set
            {
                if (value < 0)
                {
                    _updateFrequency = 0;
                }
                else if (value <= MaxFrequency)
                {
                    _updateFrequency = value;
                }
                else
                {
                    Debug.Print("Target render frequency clamped to {0}Hz.", MaxFrequency);
                    _updateFrequency = MaxFrequency;
                }
                canvas.TargetElapsedTime = TimeSpan.FromMilliseconds(_updateFrequency == 0 ? 0 : (1000 / _updateFrequency));
            }
        }
        #endregion

        #region コンストラクタ
        public GLWinUI3ControlRL() : this(null)
        {
        }

        public GLWinUI3ControlRL(GLWinUI3ControlSettings? settings = null)
        {
            this.InitializeComponent();

            this._settings = settings ?? new GLWinUI3ControlSettings();

            this.Loaded += GLWinUI3Control_Loaded;
            this.Unloaded += GLWinUI3Control_Unloaded;
            this.SizeChanged += GLWinUI3Control_SizeChanged;
            this.LayoutUpdated += GLWinUI3Control_LayoutUpdated;
            this.canvas.Update += Canvas_Update;
            this.canvas.Draw += CanvasControl_Draw;
            this.canvas.Paused = true;
        }

        ~GLWinUI3ControlRL()
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
        /// Rendering Loopを再開します。
        /// </summary>
        public void ResumeLoop()
        {
            _pause = false;
            canvas.Paused = false;
        }

        /// <summary>
        /// Rendering Loopを中断します。
        /// </summary>
        public void PauseLoop()
        {
            _pause = true;
            canvas.Paused = true;
        }

        /// <summary>
        /// 
        /// </summary>
        private void MakeCurrent()
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
        private void MakeNoneCurrent()
        {
            if (_glContext == null)
            {
                Trace.WriteLine("ERROR: _glContext is null");
                return;
            }
            _glContext.MakeNoneCurrent();
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
