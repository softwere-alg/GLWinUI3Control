using GLWinUI3Control;
using Microsoft.UI.Xaml;
using System;
using OpenTK.Mathematics;
using System.Timers;

namespace Sample
{
    public sealed partial class MainWindow : Microsoft.UI.Xaml.Window
    {
        private readonly float[] triangleData = {
			// positionX, positionY, positionZ
            -1.0f, -1.0f,  0.0f, // 左下
             1.0f, -1.0f,  0.0f, // 右下
             0.0f,  1.0f,  0.0f  // 中央上
        };

        private readonly float[] rectangleData = {
			// positionX, positionY, positionZ
            -1.0f, -0.5f,  0.0f, // 左下
            -1.0f,  0.5f,  0.0f, // 左上
             1.0f, -0.5f,  0.0f, // 右下
             1.0f,  0.5f,  0.0f  // 右上
        };

        private readonly float[] pentagonData = {
			// positionX, positionY, positionZ
             0.0f,  1.0f,  0.0f,
            -1.0f,  0.5f,  0.0f,
             1.0f,  0.5f,  0.0f,
            -0.5f, -1.0f,  0.0f,
             0.5f, -1.0f,  0.0f
        };

        private Renderer renderer1;
        private Renderer renderer2;
        private Renderer renderer3;
        private Timer timer;

        private GLWinUI3Context context;
        private Compute compute;
        private GLWinUI3ContextInvoke contextInvoke;
        private Compute computeInvoke;

        public MainWindow()
        {
            this.InitializeComponent();

            renderer1 = new Renderer(triangleData, Color4.AliceBlue);
            renderer2 = new Renderer(rectangleData, Color4.AntiqueWhite);
            renderer3 = new Renderer(pentagonData, Color4.BlueViolet);

            // OpenGL by RenderLoop
            glControl1.Initialized += () =>
            {
                renderer1.SetupGL();
            };
            glControl1.UpdateFrame += (FrameEventExArgs args) =>
            {
                renderer1.Update(args.DrawableArea);
            };
            glControl1.RenderFrame += (FrameEventExArgs args) =>
            {
                renderer1.Draw(args.DrawableArea);
            };
            glControl1.UpdateFrequency = 60;
            glControl1.ResumeLoop();

            // OpenGL by RenderLoop
            glControl2.Initialized += () =>
            {
                renderer2.SetupGL();
            };
            glControl2.UpdateFrame += (FrameEventExArgs args) =>
            {
                renderer2.Update(args.DrawableArea);
            };
            glControl2.RenderFrame += (FrameEventExArgs args) =>
            {
                renderer2.Draw(args.DrawableArea);
            };
            glControl2.UpdateFrequency = 30;
            glControl2.ResumeLoop();

            // OpenGL by ManualLoop
            timer = new Timer(100);
            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            glControl3.Initialized += () =>
            {
                renderer3.SetupGL();

                timer.Enabled = true;
            };

            // Compute Shader on MainThread
            context = new GLWinUI3Context(new GLWinUI3ContextSettings() { APIVersion = new Version(4, 3) });
            compute = new Compute();

            // Compute Shader on ContextInvoke
            contextInvoke = new GLWinUI3ContextInvoke(new GLWinUI3ContextSettings() { APIVersion = new Version(4, 3) });
            contextInvoke.Invoke(() =>
            {
                computeInvoke = new Compute();
            });
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () =>
            {
                glControl3.MakeCurrent();

                renderer3.Update(glControl3.DrawableArea);
                renderer3.Draw(glControl3.DrawableArea);

                glControl3.SwapBuffers();
            });
        }

        private void mainButton_Click(object sender, RoutedEventArgs e)
        {
            context.MakeCurrent();
            compute.Calc();
        }
        
        private void invokeButton_Click(object sender, RoutedEventArgs e)
        {
            contextInvoke.Invoke(() =>
            {
                computeInvoke.Calc();
            });
        }
    }
}
