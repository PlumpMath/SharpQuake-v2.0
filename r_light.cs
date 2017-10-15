using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    static int r_dlightframecount;
    static mplane_t lightplane;

    public static void R_PushDlights()
    {
        if (gl_flashblend.value != 0)
            return;

        r_dlightframecount = r_framecount + 1;	// because the count hasn't advanced yet for this frame

        for (int i = 0; i < q_shared.MAX_DLIGHTS; i++)
        {
            dlight_t l = cl_dlights[i];
            if (l.die < cl.time || l.radius == 0)
                continue;
            R_MarkLights(l, 1 << i, cl.worldmodel.nodes[0]);
        }
    }
    public static void R_MarkLights(dlight_t light, int bit, mnodebase_t node)
    {
        if (node.contents < 0)
            return;
            
        mnode_t n = (mnode_t)node;
        mplane_t splitplane = n.plane;
        float dist = Vector3.Dot(light.origin, splitplane.normal) - splitplane.dist;

        if (dist > light.radius)
        {
            R_MarkLights(light, bit, n.children[0]);
            return;
        }
        if (dist < -light.radius)
        {
            R_MarkLights(light, bit, n.children[1]);
            return;
        }

        // mark the polygons
        for (int i = 0; i < n.numsurfaces; i++)
        {
            msurface_t surf = cl.worldmodel.surfaces[n.firstsurface + i];
            if (surf.dlightframe != r_dlightframecount)
            {
                surf.dlightbits = 0;
                surf.dlightframe = r_dlightframecount;
            }
            surf.dlightbits |= bit;
        }

        R_MarkLights(light, bit, n.children[0]);
        R_MarkLights(light, bit, n.children[1]);
    }
    public static void R_RenderDlights()
    {
        //int i;
        //dlight_t* l;

        if (gl_flashblend.value == 0)
            return;

        r_dlightframecount = r_framecount + 1;	// because the count hasn't advanced yet for this frame

        GL.DepthMask(false);
        GL.Disable(EnableCap.Texture2D);
        GL.ShadeModel(ShadingModel.Smooth);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactorSrc.One, BlendingFactorDest.One);

        for (int i = 0; i < q_shared.MAX_DLIGHTS; i++)
        {
            dlight_t l = cl_dlights[i];
            if (l.die < cl.time || l.radius == 0)
                continue;
                
            R_RenderDlight(l);
        }

        GL.Color3(1f, 1, 1);
        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.Texture2D);
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        GL.DepthMask(true);
    }
    public static void R_AnimateLight()
    {
        //
        // light animations
        // 'm' is normal light, 'a' is no light, 'z' is double bright
        int i = (int)(cl.time * 10);
        for (int j = 0; j < q_shared.MAX_LIGHTSTYLES; j++)
        {
            if (String.IsNullOrEmpty(cl_lightstyle[j].map))
            {
                d_lightstylevalue[j] = 256;
                continue;
            }
            string map = cl_lightstyle[j].map;
            int k = i % map.Length;
            k = map[k] - 'a';
            k = k * 22;
            d_lightstylevalue[j] = k;
        }
    }
    public static int R_LightPoint(ref Vector3 p)
    {
        if (cl.worldmodel.lightdata == null)
            return 255;

        Vector3 end = p;
        end.Z -= 2048;

        int r = RecursiveLightPoint(cl.worldmodel.nodes[0], ref p, ref end);
        if (r == -1)
            r = 0;

        return r;
    }
    public static int RecursiveLightPoint(mnodebase_t node, ref Vector3 start, ref Vector3 end)
    {
        if (node.contents < 0)
            return -1;		// didn't hit anything

        mnode_t n = (mnode_t)node;

        // calculate mid point

        // FIXME: optimize for axial
        mplane_t plane = n.plane;
        float front = Vector3.Dot(start, plane.normal) - plane.dist;
        float back = Vector3.Dot(end, plane.normal) - plane.dist;
        int side = front < 0 ? 1 : 0;

        if ((back < 0 ? 1 : 0) == side)
            return RecursiveLightPoint(n.children[side], ref start, ref end);

        float frac = front / (front - back);
        Vector3 mid = start + (end - start) * frac;

        // go down front side	
        int r = RecursiveLightPoint(n.children[side], ref start, ref mid);
        if (r >= 0)
            return r;		// hit something

        if ((back < 0 ? 1 : 0) == side)
            return -1;		// didn't hit anuthing

        // check for impact on this node
        lightspot = mid;
        lightplane = plane;

        msurface_t[] surf = cl.worldmodel.surfaces;
        int offset = n.firstsurface;
        for (int i = 0; i < n.numsurfaces; i++, offset++)
        {
            if ((surf[offset].flags & q_shared.SURF_DRAWTILED) != 0)
                continue;	// no lightmaps

            mtexinfo_t tex = surf[offset].texinfo;

            int s = (int)(Vector3.Dot(mid, tex.vecs[0].Xyz) + tex.vecs[0].W);
            int t = (int)(Vector3.Dot(mid, tex.vecs[1].Xyz) + tex.vecs[1].W);

            if (s < surf[offset].texturemins[0] || t < surf[offset].texturemins[1])
                continue;

            int ds = s - surf[offset].texturemins[0];
            int dt = t - surf[offset].texturemins[1];

            if (ds > surf[offset].extents[0] || dt > surf[offset].extents[1])
                continue;

            if (surf[offset].sample_base == null)
                return 0;

            ds >>= 4;
            dt >>= 4;

            byte[] lightmap = surf[offset].sample_base;
            int lmOffset = surf[offset].sampleofs;
            short[] extents = surf[offset].extents;
            r = 0;
            if (lightmap != null)
            {
                lmOffset += dt * ((extents[0] >> 4) + 1) + ds;

                for (int maps = 0; maps < q_shared.MAXLIGHTMAPS && surf[offset].styles[maps] != 255; maps++)
                {
                    int scale = d_lightstylevalue[surf[offset].styles[maps]];
                    r += lightmap[lmOffset] * scale;
                    lmOffset += ((extents[0] >> 4) + 1) * ((extents[1] >> 4) + 1);
                }

                r >>= 8;
            }

            return r;
        }

        // go down back side
        return RecursiveLightPoint(n.children[side == 0 ? 1 : 0], ref mid, ref end);
    }
    public static void R_RenderDlight(dlight_t light)
    {
        float rad = light.radius * 0.35f;
        Vector3 v = light.origin - r_origin;
        if (v.Length < rad)
        {	// view is inside the dlight
            AddLightBlend(1, 0.5f, 0, light.radius * 0.0003f);
            return;
        }

        GL.Begin(BeginMode.TriangleFan);
        GL.Color3(0.2f, 0.1f, 0);
        v = light.origin - vpn * rad;
        GL.Vertex3(v);
        GL.Color3(0, 0, 0);
        for (int i = 16; i >= 0; i--)
        {
            double a = i / 16.0 * Math.PI * 2;
            v = light.origin + vright * (float)Math.Cos(a) * rad + vup * (float)Math.Sin(a) * rad;
            GL.Vertex3(v);
        }
        GL.End();
    }
    public static void AddLightBlend(float r, float g, float b, float a2)
    {
        game_engine.v_blend.A += a2 * (1 - game_engine.v_blend.A);

        float a = game_engine.v_blend.A;

        a2 = a2 / a;

        game_engine.v_blend.R = game_engine.v_blend.R * (1 - a2) + r * a2; // error? - v_blend[0] = v_blend[1] * (1 - a2) + r * a2;
        game_engine.v_blend.G = game_engine.v_blend.G * (1 - a2) + g * a2;
        game_engine.v_blend.B = game_engine.v_blend.B * (1 - a2) + b * a2;
    }
}