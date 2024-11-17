using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace GLWinUI3Control
{
    /// <summary>
    /// Configuration settings for a GLWinUI3ControlSettings.  The properties here are a subset
    /// of the NativeWindowSettings properties, restricted to those that make
    /// sense in a WinUI3 environment.
    /// </summary>
    public class GLWinUI3ControlSettings : GLWinUI3ContextSettings
    {
        /// <summary>
        /// Gets the default settings for a <see cref="GLWinUI3ControlSettings"/>.
        /// </summary>
        public static new readonly GLWinUI3ControlSettings Default = new GLWinUI3ControlSettings();

        /// <summary>
        /// Gets or sets a value indicating the vsync mode to use.
        /// A pure NativeWindow supports <see cref="VSyncMode.Off"/> and <see cref="VSyncMode.On"/>.
        /// <see cref="GameWindow"/> adds support for <see cref="VSyncMode.Adaptive"/>,
        /// if you are not using <see cref="GameWindow"/> you will have to handle adaptive vsync yourself.
        /// </summary>
        public VSyncMode Vsync { get; set; } = VSyncMode.Off;

        /// <summary>
        /// Make a perfect shallow copy of this object.
        /// </summary>
        /// <returns>A perfect shallow copy of this GLWinUI3ControlSettings object.</returns>
        public new GLWinUI3ControlSettings Clone()
        {
            return new GLWinUI3ControlSettings()
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
                Vsync = Vsync
            };
        }

        /// <summary>
        /// Derive a NativeWindowSettings object from this GLWinUI3ControlSettings object.
        /// The NativeWindowSettings has all of our properties and more, but many of
        /// its properties cannot be reasonably configured by the user when a
        /// NativeWindow is being used as a child window.
        /// </summary>
        /// <returns>The NativeWindowSettings to use when constructing a new
        /// NativeWindow.</returns>
        public override NativeWindowSettings ToNativeWindowSettings()
        {
            var tmp = base.ToNativeWindowSettings();
            tmp.Vsync = Vsync;

            return tmp;
        }
    }
}
