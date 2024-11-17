using GLWinUI3Control.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GLWinUI3Control
{
    /// <summary>
    /// OpenGLの実行を独自のスレッドで行うクラス
    /// </summary>
    public class GLWinUI3ContextInvoke : GLWinUI3Context
    {
        #region メンバ変数
        /// <summary>
        /// スレッドの待機に使用します。
        /// </summary>
        private readonly ManualResetEvent _mre = new ManualResetEvent(false);

        /// <summary>
        /// スレッドに割り当てるコアを定義します。
        /// 0の場合は、自動的に割り当てます。
        /// </summary>
        private nuint _threadAffinityMask;

        /// <summary>
        /// invokeに使用するタスクを定義します。
        /// </summary>
        private Task? _invokeTask;

        /// <summary>
        /// ロックオブジェクトを定義します。
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// アクションキューを定義します。
        /// </summary>
        private Queue<Action> actions = new Queue<Action>();
        #endregion

        #region コンストラクタ
        public GLWinUI3ContextInvoke(GLWinUI3ContextSettings? settings = null, nuint threadAffinityMask = 0) : base(settings)
        {
            this._threadAffinityMask = threadAffinityMask;

            StartInvokeTask();
        }

        ~GLWinUI3ContextInvoke()
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
                _invokeTask?.Dispose();
                _invokeTask = null;
            }

            _isDisposed = true;
        }
        #endregion

        #region メソッド
        /// <summary>
        /// actionの実行をContextスレッドに譲渡します。
        /// </summary>
        /// <param name="action"></param>
        public void Invoke(Action action)
        {
            lock (_lock)
            {
                actions.Enqueue(action);

                _mre.Set();
            }
        }

        /// <summary>
        /// InvokeTaskを開始します。
        /// </summary>
        private void StartInvokeTask()
        {
            // メインスレッドのGLコンテキストを解除する
            MakeNoneCurrent();

            _invokeTask = Task.Run(() =>
            {
                // スレッドにGLコンテキストを設定する
                MakeCurrent();

                nuint allocatedCore = 0;

                try
                {
                    // Make this thread only run on one core, avoiding timing issues with context switching
                    allocatedCore = CoreManager.AllocateCore(_threadAffinityMask);

                    while (true)
                    {
                        // Invokeメソッドが呼ばれるまで待機
                        _mre.WaitOne();

                        MakeCurrent();

                        // queueが空になるまで繰り返す
                        while (true)
                        {
                            Action? action = null;
                            lock (_lock)
                            {
                                // queueが空の場合は、ResetEventフラグを落として、次のInvokeが呼び出されるまで待機
                                if (actions.Count == 0)
                                {
                                    _mre.Reset();
                                    break;
                                }
                                else
                                {
                                    action = actions.Dequeue();
                                }
                            }

                            action?.Invoke();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                }
                finally
                {
                    Trace.WriteLine("InvokeTask End");
                    CoreManager.FreeCore(allocatedCore);
                    _invokeTask = null;
                }
            });
        }
        #endregion
    }
}
