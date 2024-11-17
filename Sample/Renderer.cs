using System;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;

namespace Sample
{
    public class Renderer
    {
        /// <summary>
        /// 頂点データ番号を定義します。
        /// </summary>
        private enum VertexAttribute
        {
            Position = 0,       // 頂点位置
        }

        /// <summary>
        /// シェーダプログラム
        /// </summary>
        private int program;

        /// <summary>
        /// 頂点配列オブジェクト
        /// </summary>
        private uint vertexArray;
        /// <summary>
        /// 頂点バッファオブジェクト
        /// </summary>
        private uint vertexBuffer;

        /// <summary>
        /// モデル行列
        /// </summary>
        private Matrix4 modelMatrix = Matrix4.Identity;

        /// <summary>
        /// モデル行列ハンドル
        /// </summary>
        private int modelMatrixHandle;

        private float[] vertexData;
        private Color4 clearColor;

        public Renderer(float[] vertexData, Color4 clearColor)
        {
            this.vertexData = vertexData;
            this.clearColor = clearColor;
        }

        public void SetupGL()
        {
            // シェーダのロード
            program = GLUtils.LoadGraphicShaders();

            // 1つの頂点配列オブジェクトの生成
            GL.GenVertexArrays(1, out vertexArray);
            GLUtils.CheckError();
            // 頂点配列オブジェクトの指定
            GL.BindVertexArray(vertexArray);
            GLUtils.CheckError();

            // 1つの頂点バッファオブジェクトの生成
            GL.GenBuffers(1, out vertexBuffer);
            GLUtils.CheckError();
            // 頂点バッファオブジェクトの指定
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GLUtils.CheckError();
            // 頂点バッファオブジェクトに頂点データを渡す
            GL.BufferData(BufferTarget.ArrayBuffer, (IntPtr)(vertexData.Length * sizeof(float)), vertexData, BufferUsageHint.StaticDraw);
            GLUtils.CheckError();

            // 頂点データのデータ構造を指定
            GL.EnableVertexAttribArray((int)VertexAttribute.Position);
            GLUtils.CheckError();
            GL.VertexAttribPointer((int)VertexAttribute.Position, 3, VertexAttribPointerType.Float, false, sizeof(float) * 3, new IntPtr(0));
            GLUtils.CheckError();

            // 行列ハンドルの取得
            modelMatrixHandle = GL.GetUniformLocation(program, "modelMatrix");
            GLUtils.CheckError();

            // 頂点配列オブジェクトの指定解除
            GL.BindVertexArray(0);
            GLUtils.CheckError();
        }

        int count = 0;
        public void Update(Box2i drawableArea)
        {
            modelMatrix = Matrix4.CreateRotationZ(2 * MathF.PI * (count / 360.0f));
            count += 1;
        }

        public void Draw(Box2i drawableArea)
        {
            GL.ClearColor(clearColor);
            GLUtils.CheckError();

            // //バッファのクリア
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            GLUtils.CheckError();

            // ビューポート設定
            GL.Viewport(drawableArea);
            GLUtils.CheckError();

            // シェーダプログラムを指定
            GL.UseProgram(program);
            GLUtils.CheckError();

            // 頂点配列オブジェクトを指定
            GL.BindVertexArray(vertexArray);
            GLUtils.CheckError();

            // 頂点データ以外に描画に必要な情報を設定する
            GL.UniformMatrix4(modelMatrixHandle, false, ref modelMatrix);
            GLUtils.CheckError();

            // 描画を行う
            GL.DrawArrays(PrimitiveType.TriangleStrip, 0, vertexData.Length / 3);
            GLUtils.CheckError();

            GL.Flush();
            GLUtils.CheckError();
        }
    }
}
