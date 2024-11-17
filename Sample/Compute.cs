using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;

namespace Sample
{
    public class Compute
    {
        private const int NUM_PARTICLES = 100000;
        private const int WORK_GROUP_SIZE = 1000;

        private float[] a;
        private float[] b;
        private float[] c;

        private int aHandle;
        private int bHandle;
        private int cHandle;

        private int computeProgram;

        public Compute()
        {
            a = new float[NUM_PARTICLES];
            b = new float[NUM_PARTICLES];
            c = new float[NUM_PARTICLES];
            for (int i = 0; i < NUM_PARTICLES; i++)
            {
                a[i] = i;
                b[i] = i / 10.0f;
            }

            GL.GenBuffers(1, out aHandle);
            GLUtils.CheckError();
            GL.GenBuffers(1, out bHandle);
            GLUtils.CheckError();
            GL.GenBuffers(1, out cHandle);
            GLUtils.CheckError();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, aHandle);
            GLUtils.CheckError();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 4 * a.Length, a, BufferUsageHint.StaticDraw);
            GLUtils.CheckError();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, bHandle);
            GLUtils.CheckError();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 4 * b.Length, b, BufferUsageHint.StaticDraw);
            GLUtils.CheckError();
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, cHandle);
            GLUtils.CheckError();
            GL.BufferData(BufferTarget.ShaderStorageBuffer, 4 * c.Length, c, BufferUsageHint.StaticDraw);
            GLUtils.CheckError();

            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, aHandle);
            GLUtils.CheckError();
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 1, bHandle);
            GLUtils.CheckError();
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 2, cHandle);
            GLUtils.CheckError();

            computeProgram = GLUtils.LoadComputeShaders();
        }

        public void Calc()
        {
            var d = DateTime.Now;

            GL.UseProgram(computeProgram);
            GLUtils.CheckError();

            // Compute
            GL.DispatchCompute(NUM_PARTICLES / WORK_GROUP_SIZE, 1, 1);
            GLUtils.CheckError();

            // wait
            GL.Finish();
            GLUtils.CheckError();

            // get result
            float[] output = new float[NUM_PARTICLES];
            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, cHandle);
            GLUtils.CheckError();
            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, 0, output.Length, output);
            GLUtils.CheckError();

            Trace.WriteLine($"Process Time: {DateTime.Now - d}");

            for (int i = 0; i < 10; i++)
            {
                Trace.WriteLine($"{a[i]} + {b[i]} = {output[i]}");
            }
        }
    }
}
