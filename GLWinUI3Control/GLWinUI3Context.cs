using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using System;
using System.Diagnostics.CodeAnalysis;

namespace GLWinUI3Control
{
    public class GLWinUI3Context : IDisposable
    {
        #region メンバ変数
        /// <summary>
        /// NativeWindowを定義します。
        /// </summary>
        private NativeWindow _nativeWindow;

        /// <summary>
        /// Disposeが呼ばれたかを表します。
        /// </summary>
        protected bool _isDisposed = false;
        #endregion

        #region プロパティ
        /// <summary>
        /// NativeWindowを取得します。
        /// </summary>
        public NativeWindow NativeWindow { get => _nativeWindow; }
        #endregion

        #region コンストラクタ
        public GLWinUI3Context(GLWinUI3ContextSettings? settings = null)
        {
            CreateContext(settings ?? new GLWinUI3ContextSettings());
        }

        ~GLWinUI3Context()
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
                DestroyContext();
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
            _nativeWindow.MakeCurrent();
        }

        /// <summary>
        /// 
        /// </summary>
        public void MakeNoneCurrent()
        {
            _nativeWindow.Context.MakeNoneCurrent();
        }

        /// <summary>
        /// Contextを作成します。
        /// </summary>
        /// <param name="settings"></param>
        [MemberNotNull(nameof(_nativeWindow))]
        private void CreateContext(GLWinUI3ContextSettings settings)
        {
            CreateNativeWindow(settings);
        }

        /// <summary>
        /// Contextを破棄します。
        /// </summary>
        private void DestroyContext()
        {
            DestroyNativeWindow();
        }

        /// <summary>
        /// NativeWindowを作成します。
        /// </summary>
        /// <param name="glContextSettings"></param>
        [MemberNotNull(nameof(_nativeWindow))]
        private void CreateNativeWindow(GLWinUI3ContextSettings glContextSettings)
        {
            NativeWindowSettings nativeWindowSettings = glContextSettings.ToNativeWindowSettings();

            _nativeWindow = new NativeWindow(nativeWindowSettings);

            // とりあえずのサイズ指定
            _nativeWindow.ClientRectangle = new Box2i(0, 0, 100, 100);

            // windowを見えなくする
            _nativeWindow.IsVisible = false;
        }

        /// <summary>
        /// NativeWindowを破棄します。
        /// </summary>
        private void DestroyNativeWindow()
        {
            _nativeWindow.Dispose();
#pragma warning disable CS8625 // null リテラルを null 非許容参照型に変換できません。
            _nativeWindow = null;
#pragma warning restore CS8625 // null リテラルを null 非許容参照型に変換できません。
        }
        #endregion
    }
}
