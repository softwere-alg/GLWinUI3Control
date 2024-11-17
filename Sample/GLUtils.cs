using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Compute.OpenCL;
using OpenTK.Graphics.OpenGL;

namespace Sample
{
    public static class GLUtils
    {
        public static void CheckError()
        {
            var errorCode = GL.GetError();
            if (errorCode != ErrorCode.NoError)
            {
                Trace.WriteLine(errorCode);
            }
        }

        private static string LoadResource(string name)
        {
            return File.ReadAllText(Path.Combine("Shader", name));
        }

        public static int LoadGraphicShaders()
        {
            int vertShader, fragShader;

            int program = GL.CreateProgram();
            CheckError();

            if (!CompileShader(ShaderType.VertexShader, LoadResource("Shader.vsh"), out vertShader))
            {
                Trace.WriteLine("Failed to compile vertex shader");
                return 0;
            }
            if (!CompileShader(ShaderType.FragmentShader, LoadResource("Shader.fsh"), out fragShader))
            {
                Trace.WriteLine("Failed to compile fragment shader");
                return 0;
            }

            GL.AttachShader(program, vertShader);
            CheckError();

            GL.AttachShader(program, fragShader);
            CheckError();

            if (!LinkProgram(program))
            {
                Trace.WriteLine($"Failed to link program: {program}");

                if (vertShader != 0)
                {
                    GL.DeleteShader(vertShader);
                    CheckError();
                }

                if (fragShader != 0)
                {
                    GL.DeleteShader(fragShader);
                    CheckError();
                }

                if (program != 0)
                {
                    GL.DeleteProgram(program);
                    CheckError();
                    program = 0;
                }
                return 0;
            }

            if (vertShader != 0)
            {
                GL.DetachShader(program, vertShader);
                CheckError();
                GL.DeleteShader(vertShader);
                CheckError();
            }
            if (fragShader != 0)
            {
                GL.DetachShader(program, fragShader);
                CheckError();
                GL.DeleteShader(fragShader);
                CheckError();
            }

            return program;
        }

        public static int LoadComputeShaders()
        {
            int compShader;

            int program = GL.CreateProgram();
            CheckError();

            if (!CompileShader(ShaderType.ComputeShader, LoadResource("Shader.comp"), out compShader))
            {
                Trace.WriteLine("Failed to compile vertex shader");
                return 0;
            }

            GL.AttachShader(program, compShader);
            CheckError();

            if (!LinkProgram(program))
            {
                Trace.WriteLine($"Failed to link program: {program}");

                if (compShader != 0)
                {
                    GL.DeleteShader(compShader);
                    CheckError();
                }

                if (program != 0)
                {
                    GL.DeleteProgram(program);
                    CheckError();
                    program = 0;
                }
                return 0;
            }

            if (compShader != 0)
            {
                GL.DetachShader(program, compShader);
                CheckError();
                GL.DeleteShader(compShader);
                CheckError();
            }

            return program;
        }

        private static bool CompileShader(ShaderType type, string src, out int shader)
        {
            shader = GL.CreateShader(type);
            CheckError();
            GL.ShaderSource(shader, src);
            CheckError();
            GL.CompileShader(shader);
            CheckError();

#if DEBUG
            int logLength = 0;
            GL.GetShader(shader, ShaderParameter.InfoLogLength, out logLength);
            CheckError();
            if (logLength > 0)
            {
                Trace.WriteLine("Shader compile log:\n{0}", GL.GetShaderInfoLog(shader));
            }
#endif

            int status = 0;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            CheckError();
            if (status == 0)
            {
                GL.DeleteShader(shader);
                CheckError();
                return false;
            }

            return true;
        }

        private static bool LinkProgram(int prog)
        {
            GL.LinkProgram(prog);
            CheckError();

#if DEBUG
            int logLength = 0;
            GL.GetProgram(prog, ProgramParameter.InfoLogLength, out logLength);
            CheckError();
            if (logLength > 0)
                Trace.WriteLine("Program link log:\n{0}", GL.GetProgramInfoLog(prog));
#endif
            int status = 0;
            GL.GetProgram(prog, ProgramParameter.LinkStatus, out status);
            CheckError();
            return status != 0;
        }
    }
}
