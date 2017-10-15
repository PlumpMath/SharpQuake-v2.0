using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    public static float[] turbsin = new float[]
      {
        0f, 0.19633f, 0.392541f, 0.588517f, 0.784137f, 0.979285f, 1.17384f, 1.3677f,
        1.56072f, 1.75281f, 1.94384f, 2.1337f, 2.32228f, 2.50945f, 2.69512f, 2.87916f,
        3.06147f, 3.24193f, 3.42044f, 3.59689f, 3.77117f, 3.94319f, 4.11282f, 4.27998f,
        4.44456f, 4.60647f, 4.76559f, 4.92185f, 5.07515f, 5.22538f, 5.37247f, 5.51632f,
        5.65685f, 5.79398f, 5.92761f, 6.05767f, 6.18408f, 6.30677f, 6.42566f, 6.54068f,
        6.65176f, 6.75883f, 6.86183f, 6.9607f, 7.05537f, 7.14579f, 7.23191f, 7.31368f,
        7.39104f, 7.46394f, 7.53235f, 7.59623f, 7.65552f, 7.71021f, 7.76025f, 7.80562f,
        7.84628f, 7.88222f, 7.91341f, 7.93984f, 7.96148f, 7.97832f, 7.99036f, 7.99759f,
        8f, 7.99759f, 7.99036f, 7.97832f, 7.96148f, 7.93984f, 7.91341f, 7.88222f,
        7.84628f, 7.80562f, 7.76025f, 7.71021f, 7.65552f, 7.59623f, 7.53235f, 7.46394f,
        7.39104f, 7.31368f, 7.23191f, 7.14579f, 7.05537f, 6.9607f, 6.86183f, 6.75883f,
        6.65176f, 6.54068f, 6.42566f, 6.30677f, 6.18408f, 6.05767f, 5.92761f, 5.79398f,
        5.65685f, 5.51632f, 5.37247f, 5.22538f, 5.07515f, 4.92185f, 4.76559f, 4.60647f,
        4.44456f, 4.27998f, 4.11282f, 3.94319f, 3.77117f, 3.59689f, 3.42044f, 3.24193f,
        3.06147f, 2.87916f, 2.69512f, 2.50945f, 2.32228f, 2.1337f, 1.94384f, 1.75281f,
        1.56072f, 1.3677f, 1.17384f, 0.979285f, 0.784137f, 0.588517f, 0.392541f, 0.19633f,
        9.79717e-16f, -0.19633f, -0.392541f, -0.588517f, -0.784137f, -0.979285f, -1.17384f, -1.3677f,
        -1.56072f, -1.75281f, -1.94384f, -2.1337f, -2.32228f, -2.50945f, -2.69512f, -2.87916f,
        -3.06147f, -3.24193f, -3.42044f, -3.59689f, -3.77117f, -3.94319f, -4.11282f, -4.27998f,
        -4.44456f, -4.60647f, -4.76559f, -4.92185f, -5.07515f, -5.22538f, -5.37247f, -5.51632f,
        -5.65685f, -5.79398f, -5.92761f, -6.05767f, -6.18408f, -6.30677f, -6.42566f, -6.54068f,
        -6.65176f, -6.75883f, -6.86183f, -6.9607f, -7.05537f, -7.14579f, -7.23191f, -7.31368f,
        -7.39104f, -7.46394f, -7.53235f, -7.59623f, -7.65552f, -7.71021f, -7.76025f, -7.80562f,
        -7.84628f, -7.88222f, -7.91341f, -7.93984f, -7.96148f, -7.97832f, -7.99036f, -7.99759f,
        -8f, -7.99759f, -7.99036f, -7.97832f, -7.96148f, -7.93984f, -7.91341f, -7.88222f,
        -7.84628f, -7.80562f, -7.76025f, -7.71021f, -7.65552f, -7.59623f, -7.53235f, -7.46394f,
        -7.39104f, -7.31368f, -7.23191f, -7.14579f, -7.05537f, -6.9607f, -6.86183f, -6.75883f,
        -6.65176f, -6.54068f, -6.42566f, -6.30677f, -6.18408f, -6.05767f, -5.92761f, -5.79398f,
        -5.65685f, -5.51632f, -5.37247f, -5.22538f, -5.07515f, -4.92185f, -4.76559f, -4.60647f,
        -4.44456f, -4.27998f, -4.11282f, -3.94319f, -3.77117f, -3.59689f, -3.42044f, -3.24193f,
        -3.06147f, -2.87916f, -2.69512f, -2.50945f, -2.32228f, -2.1337f, -1.94384f, -1.75281f,
        -1.56072f, -1.3677f, -1.17384f, -0.979285f, -0.784137f, -0.588517f, -0.392541f, -0.19633f
      };

    static int solidskytexture;
    static int alphaskytexture;

    static msurface_t _WarpFace;
    public static void R_InitSky(texture_t mt)
    {
        byte[] src = mt.pixels;
        int offset = mt.offsets[0];

        // make an average value for the back to avoid
        // a fringe on the top level
        const int size = 128 * 128;
        uint[] trans = new uint[size];
        uint[] v8to24 = d_8to24table;
        int r = 0;
        int g = 0;
        int b = 0;
        Union4b rgba = Union4b.Empty;
        for (int i = 0; i < 128; i++)
            for (int j = 0; j < 128; j++)
            {
                int p = src[offset + i * 256 + j + 128];
                rgba.ui0 = v8to24[p];
                trans[(i * 128) + j] = rgba.ui0;
                r += rgba.b0;
                g += rgba.b1;
                b += rgba.b2;
            }

        rgba.b0 = (byte)(r / size);
        rgba.b1 = (byte)(g / size);
        rgba.b2 = (byte)(b / size);
        rgba.b3 = 0;

        uint transpix = rgba.ui0;

        if (solidskytexture == 0)
            solidskytexture = GenerateTextureNumber();
        GL_Bind(solidskytexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, gl_solid_format, 128, 128, 0, PixelFormat.Rgba, PixelType.UnsignedByte, trans);
        SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);

        for (int i = 0; i < 128; i++)
            for (int j = 0; j < 128; j++)
            {
                int p = src[offset + i * 256 + j];
                if (p == 0)
                    trans[(i * 128) + j] = transpix;
                else
                    trans[(i * 128) + j] = v8to24[p];
            }

        if (alphaskytexture == 0)
            alphaskytexture = GenerateTextureNumber();
        GL_Bind(alphaskytexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, gl_alpha_format, 128, 128, 0, PixelFormat.Rgba, PixelType.UnsignedByte, trans);
        SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);
    }
    public static void GL_SubdivideSurface(msurface_t fa)
    {
        _WarpFace = fa;

        //
        // convert edges back to a normal polygon
        //
        int numverts = 0;
        Vector3[] verts = new Vector3[fa.numedges + 1];
        for (int i = 0; i < fa.numedges; i++)
        {
            int lindex = loadmodel.surfedges[fa.firstedge + i];

            if (lindex > 0)
                verts[numverts] = loadmodel.vertexes[loadmodel.edges[lindex].v[0]].position;
            else
                verts[numverts] = loadmodel.vertexes[loadmodel.edges[-lindex].v[1]].position;

            numverts++;
        }

        SubdividePolygon(numverts, verts);
    }
    static void SubdividePolygon(int numverts, Vector3[] verts)
    {
        if (numverts > 60)
            Sys_Error("numverts = {0}", numverts);

        Vector3 mins, maxs;
        BoundPoly(numverts, verts, out mins, out maxs);

        float[] dist = new float[64];
        for (int i = 0; i < 3; i++)
        {
            double m = (Mathlib.Comp(ref mins, i) + Mathlib.Comp(ref maxs, i)) * 0.5;
            m = gl_subdivide_size.value * Math.Floor(m / gl_subdivide_size.value + 0.5);
            if (Mathlib.Comp(ref maxs, i) - m < 8)
                continue;

            if (m - Mathlib.Comp(ref mins, i) < 8)
                continue;

            for (int j = 0; j < numverts; j++)
                dist[j] = (float)(Mathlib.Comp(ref verts[j], i) - m);

            Vector3[] front = new Vector3[64];
            Vector3[] back = new Vector3[64];

            // cut it

            // wrap cases
            dist[numverts] = dist[0];
            verts[numverts] = verts[0]; // Uze: source array must be at least numverts + 1 elements long

            int f = 0, b = 0;
            for (int j = 0; j < numverts; j++)
            {
                if (dist[j] >= 0)
                {
                    front[f] = verts[j];
                    f++;
                }
                if (dist[j] <= 0)
                {
                    back[b] = verts[j];
                    b++;
                }
                if (dist[j] == 0 || dist[j + 1] == 0)
                    continue;
                if ((dist[j] > 0) != (dist[j + 1] > 0))
                {
                    // clip point
                    float frac = dist[j] / (dist[j] - dist[j + 1]);
                    front[f] = back[b] = verts[j] + (verts[j + 1] - verts[j]) * frac;
                    f++;
                    b++;
                }
            }

            SubdividePolygon(f, front);
            SubdividePolygon(b, back);
            return;
        }

        glpoly_t poly = new glpoly_t();
        poly.next = _WarpFace.polys;
        _WarpFace.polys = poly;
        poly.AllocVerts(numverts);
        for (int i = 0; i < numverts; i++)
        {
            Copy(ref verts[i], poly.verts[i]);
            float s = Vector3.Dot(verts[i], _WarpFace.texinfo.vecs[0].Xyz);
            float t = Vector3.Dot(verts[i], _WarpFace.texinfo.vecs[1].Xyz);
            poly.verts[i][3] = s;
            poly.verts[i][4] = t;
        }
    }
    static void BoundPoly(int numverts, Vector3[] verts, out Vector3 mins, out Vector3 maxs)
    {
        mins = Vector3.One * 9999;
        maxs = Vector3.One * -9999;
        for (int i = 0; i < numverts; i++)
        {
            Vector3.ComponentMin(ref verts[i], ref mins, out mins);
            Vector3.ComponentMax(ref verts[i], ref maxs, out maxs);
        }
    }
    static void EmitWaterPolys(msurface_t fa)
    {
        for (glpoly_t p = fa.polys; p != null; p = p.next)
        {
            GL.Begin(BeginMode.Polygon);
            for (int i = 0; i < p.numverts; i++)
            {
                float[] v = p.verts[i];
                float os = v[3];
                float ot = v[4];

                float s = os + turbsin[(int)((ot * 0.125 + realtime) * q_shared.TURBSCALE) & 255];
                s *= (1.0f / 64);

                float t = ot + turbsin[(int)((os * 0.125 + realtime) * q_shared.TURBSCALE) & 255];
                t *= (1.0f / 64);

                GL.TexCoord2(s, t);
                GL.Vertex3(v);
            }
            GL.End();
        }
    }
    static void EmitSkyPolys(msurface_t fa)
    {
        for (glpoly_t p = fa.polys; p != null; p = p.next)
        {
            GL.Begin(BeginMode.Polygon);
            for (int i = 0; i < p.numverts; i++)
            {
                float[] v = p.verts[i];
                Vector3 dir = new Vector3(v[0] - r_origin.X, v[1] - r_origin.Y, v[2] - r_origin.Z);
                dir.Z *= 3; // flatten the sphere

                dir.Normalize();
                dir *= 6 * 63;

                float s = (speedscale + dir.X) / 128.0f;
                float t = (speedscale + dir.Y) / 128.0f;

                GL.TexCoord2(s, t);
                GL.Vertex3(v);
            }
            GL.End();
        }
    }
    private static void R_DrawSkyChain(msurface_t s)
    {
        GL_DisableMultitexture();

        // used when gl_texsort is on
        GL_Bind(solidskytexture);
        speedscale = (float)realtime * 8;
        speedscale -= (int)speedscale & ~127;

        for (msurface_t fa = s; fa != null; fa = fa.texturechain)
            EmitSkyPolys(fa);

        GL.Enable(EnableCap.Blend);
        GL_Bind(alphaskytexture);
        speedscale = (float)realtime * 16;
        speedscale -= (int)speedscale & ~127;

        for (msurface_t fa = s; fa != null; fa = fa.texturechain)
            EmitSkyPolys(fa);

        GL.Disable(EnableCap.Blend);
    }
    private static void EmitBothSkyLayers(msurface_t fa)
    {
        GL_DisableMultitexture();

        GL_Bind(solidskytexture);
        speedscale = (float)realtime * 8;
        speedscale -= (int)speedscale & ~127;

        EmitSkyPolys(fa);

        GL.Enable(EnableCap.Blend);
        GL_Bind(alphaskytexture);
        speedscale = (float)realtime * 16;
        speedscale -= (int)speedscale & ~127;

        EmitSkyPolys(fa);

        GL.Disable(EnableCap.Blend);
    }
}