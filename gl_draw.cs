using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    public static void GL_Set2D()
    {
        GL.Viewport(glX, glY, glWidth, glHeight);

	    GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        GL.Ortho(0, vid.width, vid.height, 0, -99999, 99999);

        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();

        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.AlphaTest);

        GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
    }
}
