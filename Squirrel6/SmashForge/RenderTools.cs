using System;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using SFGraphics.GLObjects.Textures;
using SFGraphics.Cameras;

namespace SmashForge.Rendering {
    static class RenderTools {
        public static Texture2D defaultTex;
        public static Texture2D floorTexture;
        public static Texture2D backgroundTexture;


        public static Texture2D uvTestPattern;
        public static Texture2D boneWeightGradient;
        public static Texture2D boneWeightGradient2;

        public static TextureCubeMap diffusePbr;
        public static TextureCubeMap specularPbr;

        public static void LoadTextures() {



        }

        public static void SetUp3DFixedFunctionRendering(Matrix4 mvpMatrix) {
            GL.UseProgram(0);

            // Manually set up the matrix for immediate mode.
            Matrix4 matrix = mvpMatrix;
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadMatrix(ref matrix);

            GL.Enable(EnableCap.LineSmooth); // This is Optional 
            GL.Enable(EnableCap.Normalize);  // This is critical to have
            GL.Enable(EnableCap.RescaleNormal);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.BlendEquation(BlendEquationMode.FuncAdd);

            GL.Enable(EnableCap.DepthTest);
            GL.DepthFunc(DepthFunction.Lequal);

            GL.Enable(EnableCap.AlphaTest);
            GL.AlphaFunc(AlphaFunction.Gequal, 0.1f);

            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Front);

            GL.Enable(EnableCap.LineSmooth);

            GL.Enable(EnableCap.StencilTest);
            GL.StencilOp(StencilOp.Keep, StencilOp.Keep, StencilOp.Replace);
        }

        public static byte[] DXT5ScreenShot(GLControl gc, int x, int y, int width, int height) {
            int newtex;
            //x = gc.Width - x - width;
            y = gc.Height - y - height;
            GL.GenTextures(1, out newtex);
            GL.BindTexture(TextureTarget.Texture2D, newtex);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.CompressedRgba, width, height, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);

            GL.CopyTexImage2D(TextureTarget.Texture2D, 0, InternalFormat.CompressedRgba, x, y, width, height, 0);

            int size;
            GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureCompressedImageSize, out size);

            byte[] data = new byte[size];
            GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();
            GL.GetCompressedTexImage(TextureTarget.Texture2D, 0, pointer);
            pinnedArray.Free();

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.DeleteTexture(newtex);

            return data;
        }

        public static void DrawPhotoshoot(GLControl glControl1, float shootX, float shootY, float shootWidth, float shootHeight) {
            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(0, glControl1.Width, glControl1.Height, 0, -1, 1);

            GL.Disable(EnableCap.DepthTest);

            GL.Color4(1f, 1f, 1f, 0.5f);
            GL.Begin(PrimitiveType.Quads);

            // top
            GL.Vertex2(0, 0);
            GL.Vertex2(glControl1.Width, 0);
            GL.Vertex2(glControl1.Width, shootY);
            GL.Vertex2(0, shootY);

            //bottom
            GL.Vertex2(0, shootY + shootHeight);
            GL.Vertex2(glControl1.Width, shootY + shootHeight);
            GL.Vertex2(glControl1.Width, glControl1.Height);
            GL.Vertex2(0, glControl1.Height);

            // left
            GL.Vertex2(0, 0);
            GL.Vertex2(shootX, 0);
            GL.Vertex2(shootX, glControl1.Height);
            GL.Vertex2(0, glControl1.Height);

            // right
            GL.Vertex2(shootX + shootWidth, 0);
            GL.Vertex2(glControl1.Width, 0);
            GL.Vertex2(glControl1.Width, glControl1.Height);
            GL.Vertex2(shootX + shootWidth, glControl1.Height);

            GL.End();
        }

    }
}
