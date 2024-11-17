using GLWinUI3Control.Utils;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Win32;

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

    public class GLWinUI3ControlRL : GLWinUI3Control
    {
        #region イベント
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

        #region 定数
        /// <summary>
        /// Frequency cap for RenderFrame events.
        /// </summary>
        private const double MaxFrequency = 500.0;
        #endregion

        #region メンバ変数
        /// <summary>
        /// レンダリングのループに使用するストップウォッチを定義します。
        /// </summary>
        private readonly Stopwatch _watchUpdate = new Stopwatch();

        /// <summary>
        /// レンダリングループの更新間隔を定義します。
        /// </summary>
        private double _updateFrequency = 0.0;

        /// <summary>Counter for how many updates in StartLoop() where slow.</summary>
        private int _slowUpdates = 0;

        /// <summary>
        /// レンダリングループが中断中かのフラグを定義します。
        /// </summary>
        private bool _pause = true;

        /// <summary>
        /// レンダリングループを停止状態にするフラグを定義します。
        /// </summary>
        private bool _stop = false;

        /// <summary>
        /// レンダリングループの中断に使用します。
        /// </summary>
        private readonly ManualResetEvent _mre = new ManualResetEvent(false);

        /// <summary>
        /// レンダリングループのタスクを定義します。
        /// </summary>
        private Task? _loopTask;

        /// <summary>
        /// レンダリングループのスレッドに割り当てるコアを定義します。
        /// 0の場合は、自動的に割り当てます。
        /// </summary>
        private nuint _threadAffinityMask;
        #endregion

        #region プロパティ
        /// <summary>
        /// Gets a value indicating whether or not UpdatePeriod has consistently failed to reach TargetUpdatePeriod.
        /// This can be used to do things such as decreasing visual quality if the user's computer isn't powerful enough
        /// to handle the application.
        /// </summary>
        protected bool IsRunningSlowly { get; private set; }

        /// <summary>
        /// Gets a double representing the time spent in the UpdateFrame function, in seconds.
        /// </summary>
        public double UpdateTime { get; private set; }

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
            get => _updateFrequency;

            set
            {
                if (value < 1.0)
                {
                    _updateFrequency = 0.0;
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
            }
        }
        #endregion

        #region コンストラクタ
        public GLWinUI3ControlRL() : this(null, 0)
        {
        }

        public GLWinUI3ControlRL(GLWinUI3ControlSettings? settings = null, nuint threadAffinityMask = 0) : base(settings)
        {
            this._threadAffinityMask = threadAffinityMask;
        }

        ~GLWinUI3ControlRL()
        {
            Dispose(false);
        }

        protected override unsafe void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            base.Dispose(disposing);

            if (disposing)
            {
                StopLoop();
            }

            _isDisposed = true;
        }
        #endregion

        #region メソッド
        /// <summary>
        /// 初期化処理を行います。
        /// </summary>
        protected override void Initialize()
        {
            StartLoop();
        }

        /// <summary>
        /// Resets the time since the last update.
        /// This function is useful when implementing updates on resize using windows.
        /// </summary>
        public void ResetTimeSinceLastUpdate()
        {
            _watchUpdate.Restart();
        }

        /// <summary>
        /// Rendering Loopを再開します。
        /// </summary>
        public void ResumeLoop()
        {
            // 実行順に注意
            // _mre.Set()を先に実行すると、_pauseフラグを落とす前に、while判定に入る可能性がある
            _pause = false;
            _mre.Set();
        }

        /// <summary>
        /// Rendering Loopを中断します。
        /// </summary>
        public void PauseLoop()
        {
            // _pauseフラグを先に立てた方が、即時中断できる可能性が上がる
            _pause = true;
            _mre.Reset();
        }

        /// <summary>
        /// Rendering Loopを停止します。
        /// </summary>
        private void StopLoop()
        {
            _pause = true;
            _mre.Reset();
            _stop = true;
        }

        /// <summary>
        /// 独自スレッドによるレンダリングを開始します。
        /// </summary>
        /// <remarks>
        /// On windows this function calls <c>timeBeginPeriod(8)</c> to get better sleep timings, which can increase power usage.
        /// </para>
        /// </remarks>
        private void StartLoop()
        {
            // メインスレッドのGLコンテキストを解除する
            MakeNoneCurrent();

            _loopTask = Task.Run(() =>
            {
                // loopスレッドにGLコンテキストを設定する
                MakeCurrent();

                nuint allocatedCore = 0;

                try
                {
                    // 8 is a good compromise between accuracy and power consumption
                    // according to: https://chromium-review.googlesource.com/c/chromium/src/+/2265402
                    const int TIME_PERIOD = 8;

                    // Make this thread only run on one core, avoiding timing issues with context switching
                    allocatedCore = CoreManager.AllocateCore(_threadAffinityMask);

                    // Make Thread.Sleep more accurate.
                    // FIXME: We probably only care about this if we are not event driven.
                    PInvoke.timeBeginPeriod(TIME_PERIOD);
                    int expectedSchedulerPeriod = TIME_PERIOD;

                    OnInitialized();

                    while (!_stop)
                    {
                        // resumeが呼ばれるまで待機
                        _mre.WaitOne();

                        _watchUpdate.Start();
                        while (!_pause && !_stop)
                        {
                            MakeCurrent();

                            double updatePeriod = UpdateFrequency == 0 ? 0 : 1 / UpdateFrequency;

                            double elapsed = _watchUpdate.Elapsed.TotalSeconds;
                            if (elapsed > updatePeriod)
                            {
                                _watchUpdate.Restart();

                                UpdateTime = elapsed;
                                OnUpdateFrame(new FrameEventExArgs(elapsed, DrawableArea));
                                OnRenderFrame(new FrameEventExArgs(elapsed, DrawableArea));

                                const int MaxSlowUpdates = 80;
                                const int SlowUpdatesThreshold = 45;

                                double time = _watchUpdate.Elapsed.TotalSeconds;
                                if (updatePeriod < time)
                                {
                                    _slowUpdates++;
                                    if (_slowUpdates > MaxSlowUpdates)
                                    {
                                        _slowUpdates = MaxSlowUpdates;
                                    }
                                }
                                else
                                {
                                    _slowUpdates--;
                                    if (_slowUpdates < 0)
                                    {
                                        _slowUpdates = 0;
                                    }
                                }

                                IsRunningSlowly = _slowUpdates > SlowUpdatesThreshold;

                                if (_glContext?.NativeWindow.API != ContextAPI.NoAPI)
                                {
                                    if (_glContext?.NativeWindow.VSync == VSyncMode.Adaptive)
                                    {
                                        GLFW.SwapInterval(IsRunningSlowly ? 0 : 1);
                                    }
                                }
                            }

                            // The time we have left to the next update.
                            double timeToNextUpdate = updatePeriod - _watchUpdate.Elapsed.TotalSeconds;

                            if (timeToNextUpdate > 0)
                            {
                                OpenTK.Core.Utils.AccurateSleep(timeToNextUpdate, expectedSchedulerPeriod);
                            }

                            SwapBuffers();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
                finally
                {
                    Trace.WriteLine("LoopTask End");
                    CoreManager.FreeCore(allocatedCore);
                    _loopTask = null;
                }
            });
        }
        #endregion
    }
}
