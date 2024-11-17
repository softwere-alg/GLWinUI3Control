using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using System;

namespace GLWinUI3Control
{
    /// <summary>
    /// Configuration settings for a GLWinUI3ContextSettings.  The properties here are a subset
    /// of the NativeWindowSettings properties, restricted to those that make
    /// sense in a WinUI3 environment.
    /// </summary>
    public class GLWinUI3ContextSettings
    {
        /// <summary>
        /// Gets the default settings for a <see cref="GLWinUI3ContextSettings"/>.
        /// </summary>
        public static readonly GLWinUI3ContextSettings Default = new GLWinUI3ContextSettings();

        /// <summary>
        /// Gets or sets a value representing the current version of the graphics API.
        /// </summary>
        /// <remarks>
        /// <para>
        /// OpenGL 3.3 is selected by default, and runs on almost any hardware made within the last ten years.
        /// </para>
        /// <para>
        /// OpenGL 4.1 is suggested for modern apps meant to run on more modern hardware.
        /// </para>
        /// <para>
        /// OpenGL 4.6 is suggested for modern apps meant to run on most modern hardware.
        /// </para>
        /// <para>
        /// Note that if you choose an API other than base OpenGL, this will need to be updated accordingly,
        /// as the versioning of OpenGL and OpenGL ES do not match.
        /// </para>
        /// </remarks>
        public Version APIVersion { get; set; } = new Version(3, 3, 0, 0);

        /// <summary>
        /// Gets or sets a value indicating whether or not OpenGL bindings should be automatically loaded
        /// when the window is created.
        /// </summary>
        public bool AutoLoadBindings { get; set; } = true;

        /// <summary>
        /// Gets or sets a value representing the current graphics profile flags.
        /// </summary>
        public ContextFlags Flags { get; set; } = ContextFlags.Default;

        /// <summary>
        /// Gets or sets a value representing the current graphics API profile.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This only has an effect on OpenGL 3.2 and higher. On older versions, this setting does nothing.
        /// </para>
        /// </remarks>
        public ContextProfile Profile { get; set; } = ContextProfile.Core;

        /// <summary>
        /// Gets or sets a value representing the current graphics API.
        /// </summary>
        /// <remarks>
        /// <para>
        /// If this is changed, you'll have to modify the API version as well, as the versioning of OpenGL and OpenGL ES
        /// do not match.
        /// </para>
        /// </remarks>
        public ContextAPI API { get; set; } = ContextAPI.OpenGL;

        /// <summary>
        /// Gets or sets the context to share.
        /// </summary>
        public IGLFWGraphicsContext? SharedContext { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of samples that should be used.
        /// </summary>
        /// <remarks>
        /// <c>0</c> indicates that no multisampling should be used;
        /// otherwise multisampling is used if available. The actual number of samples is the closest matching the given number that is supported.
        /// </remarks>
        public int NumberOfSamples { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of stencil bits used for OpenGL context creation.
        /// </summary>
        /// <remarks>
        /// Default value is 8.
        /// </remarks>
        public int? StencilBits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of depth bits used for OpenGL context creation.
        /// </summary>
        /// <remarks>
        /// Default value is 24.
        /// </remarks>
        public int? DepthBits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of red bits used for OpenGL context creation.
        /// </summary>
        /// <remarks>
        /// Default value is 8.
        /// </remarks>
        public int? RedBits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of green bits used for OpenGL context creation.
        /// </summary>
        /// <remarks>
        /// Default value is 8.
        /// </remarks>
        public int? GreenBits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of blue bits used for OpenGL context creation.
        /// </summary>
        /// <remarks>
        /// Default value is 8.
        /// </remarks>
        public int? BlueBits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating the number of alpha bits used for OpenGL context creation.
        /// </summary>
        /// <remarks>
        /// Default value is 8.
        /// </remarks>
        public int? AlphaBits { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the backbuffer should be sRGB capable.
        /// </summary>
        public bool SrgbCapable { get; set; }

        /// <summary>
        /// Make a perfect shallow copy of this object.
        /// </summary>
        /// <returns>A perfect shallow copy of this GLWinUI3ContextSettings object.</returns>
        public GLWinUI3ContextSettings Clone()
        {
            return new GLWinUI3ContextSettings()
            {
                APIVersion = APIVersion,
                AutoLoadBindings = AutoLoadBindings,
                Flags = Flags,
                Profile = Profile,
                API = API,
                SharedContext = SharedContext,
                NumberOfSamples = NumberOfSamples,
                StencilBits = StencilBits,
                DepthBits = DepthBits,
                RedBits = RedBits,
                GreenBits = GreenBits,
                BlueBits = BlueBits,
                AlphaBits = AlphaBits,
                SrgbCapable = SrgbCapable,
            };
        }

        /// <summary>
        /// Derive a NativeWindowSettings object from this GLWinUI3ContextSettings object.
        /// The NativeWindowSettings has all of our properties and more, but many of
        /// its properties cannot be reasonably configured by the user when a
        /// NativeWindow is being used as a child window.
        /// </summary>
        /// <returns>The NativeWindowSettings to use when constructing a new
        /// NativeWindow.</returns>
        public virtual NativeWindowSettings ToNativeWindowSettings()
        {
            return new NativeWindowSettings()
            {
                APIVersion = APIVersion,
                AutoLoadBindings = AutoLoadBindings,
                Flags = Flags,
                Profile = Profile,
                API = API,
                SharedContext = SharedContext,
                NumberOfSamples = NumberOfSamples,
                StencilBits = StencilBits,
                DepthBits = DepthBits,
                RedBits = RedBits,
                GreenBits = GreenBits,
                BlueBits = BlueBits,
                AlphaBits = AlphaBits,
                SrgbCapable = SrgbCapable,

                StartFocused = false,
                StartVisible = false,
                WindowBorder = WindowBorder.Hidden,
                WindowState = WindowState.Normal,
            };
        }
    }
}
