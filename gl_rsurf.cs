using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    public static int lightmap_textures;
    public static int lightmap_bytes;
    public static mvertex_t[] r_pcurrentvertbase;
    public static model_t currentmodel;
    public static bool[] lightmap_modified = new bool[MAX_LIGHTMAPS];
    public static glpoly_t[] lightmap_polys = new glpoly_t[MAX_LIGHTMAPS];
    public static glRect_t[] lightmap_rectchange = new glRect_t[MAX_LIGHTMAPS];
    public static uint[] blocklights = new uint[18*18];
    public static int nColinElim;
    public static msurface_t skychain;
    public static msurface_t waterchain;
    public static entity_t _TempEnt = new entity_t(); // for DrawWorld
    public static byte[] lightmaps = new byte[4 * MAX_LIGHTMAPS * BLOCK_WIDTH * BLOCK_HEIGHT];

    public static void GL_BuildLightmaps()
    {
        Array.Clear(allocated, 0, allocated.Length);
        //memset (allocated, 0, sizeof(allocated));

        r_framecount = 1;		// no dlightcache

        if (lightmap_textures == 0)
            lightmap_textures = GenerateTextureNumberRange(MAX_LIGHTMAPS);

        gl_lightmap_format = PixelFormat.Luminance;// GL_LUMINANCE;

        // default differently on the Permedia
        if (isPermedia)
            gl_lightmap_format = PixelFormat.Rgba;

        if (HasParam("-lm_1"))
            gl_lightmap_format = PixelFormat.Luminance;

        if (HasParam("-lm_a"))
            gl_lightmap_format = PixelFormat.Alpha;

        //if (Common.HasParam("-lm_i"))
        //    Drawer.LightMapFormat = PixelFormat.Intensity;

        //if (Common.HasParam("-lm_2"))
        //    Drawer.LightMapFormat = PixelFormat.Rgba4;

        if (HasParam("-lm_4"))
            gl_lightmap_format = PixelFormat.Rgba;

        switch (gl_lightmap_format)
        {
            case PixelFormat.Rgba:
                lightmap_bytes = 4;
                break;

            //case PixelFormat.Rgba4:
            //_LightMapBytes = 2;
            //break;

            case PixelFormat.Luminance:
            //case PixelFormat.Intensity:
            case PixelFormat.Alpha:
                lightmap_bytes = 1;
                break;
        }

        for (int j = 1; j < q_shared.MAX_MODELS; j++)
        {
            model_t m = cl.model_precache[j];
            if (m == null)
                break;

            if (m.name != null && m.name.StartsWith("*"))
                continue;

            r_pcurrentvertbase = m.vertexes;
            currentmodel = m;
            for (int i = 0; i < m.numsurfaces; i++)
            {
                GL_CreateSurfaceLightmap(m.surfaces[i]);
                if ((m.surfaces[i].flags & q_shared.SURF_DRAWTURB) != 0)
                    continue;

                if ((m.surfaces[i].flags & q_shared.SURF_DRAWSKY) != 0)
                    continue;

                BuildSurfaceDisplayList(m.surfaces[i]);
            }
        }

        if (gl_texsort.value == 0)
            GL_SelectTexture(MTexTarget.TEXTURE1_SGIS);

        //
        // upload all lightmaps that were filled
        //
        GCHandle handle = GCHandle.Alloc(lightmaps, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            long lmAddr = ptr.ToInt64();
                
            for (int i = 0; i < MAX_LIGHTMAPS; i++)
            {
                if (allocated[i, 0] == 0)
                    break;		// no more used

                lightmap_modified[i] = false;
                lightmap_rectchange[i].l = BLOCK_WIDTH;
                lightmap_rectchange[i].t = BLOCK_HEIGHT;
                lightmap_rectchange[i].w = 0;
                lightmap_rectchange[i].h = 0;
                GL_Bind(lightmap_textures + i);
                SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);

                long addr = lmAddr + i * BLOCK_WIDTH * BLOCK_HEIGHT * lightmap_bytes;
                GL.TexImage2D(TextureTarget.Texture2D, 0, (PixelInternalFormat)lightmap_bytes,
                    BLOCK_WIDTH, BLOCK_HEIGHT, 0, gl_lightmap_format, PixelType.UnsignedByte, new IntPtr(addr));
            }
        }
        finally
        {
            handle.Free();
        }

        if (gl_texsort.value == 0)
            GL_SelectTexture(MTexTarget.TEXTURE0_SGIS);
    }
    public static void GL_CreateSurfaceLightmap (msurface_t surf)
    {
        if ((surf.flags & (q_shared.SURF_DRAWSKY | q_shared.SURF_DRAWTURB)) != 0)
            return;

        int smax = (surf.extents[0] >> 4) + 1;
        int tmax = (surf.extents[1] >> 4) + 1;

        surf.lightmaptexturenum = AllocBlock(smax, tmax, ref surf.light_s, ref surf.light_t);
        int offset = surf.lightmaptexturenum * lightmap_bytes * BLOCK_WIDTH * BLOCK_HEIGHT;
        offset += (surf.light_t * BLOCK_WIDTH + surf.light_s) * lightmap_bytes;
        R_BuildLightMap(surf, new ByteArraySegment(lightmaps, offset), BLOCK_WIDTH * lightmap_bytes);
    }
    public static void BuildSurfaceDisplayList(msurface_t fa)
    {
        // reconstruct the polygon
        medge_t[] pedges = currentmodel.edges;
        int lnumverts = fa.numedges;

        //
        // draw texture
        //
        glpoly_t poly = new glpoly_t();
        poly.AllocVerts(lnumverts);
        poly.next = fa.polys;
        poly.flags = fa.flags;
        fa.polys = poly;

        ushort[] r_pedge_v;
        Vector3 vec;

        for (int i = 0; i < lnumverts; i++)
        {
            int lindex = currentmodel.surfedges[fa.firstedge + i];
            if (lindex > 0)
            {
                r_pedge_v = pedges[lindex].v;
                vec = r_pcurrentvertbase[r_pedge_v[0]].position;
            }
            else
            {
                r_pedge_v = pedges[-lindex].v;
                vec = r_pcurrentvertbase[r_pedge_v[1]].position;
            }
            float s = Mathlib.DotProduct(ref vec, ref fa.texinfo.vecs[0]) + fa.texinfo.vecs[0].W;
            s /= fa.texinfo.texture.width;

            float t = Mathlib.DotProduct(ref vec, ref fa.texinfo.vecs[1]) + fa.texinfo.vecs[1].W;
            t /= fa.texinfo.texture.height;

            poly.verts[i][0] = vec.X;
            poly.verts[i][1] = vec.Y;
            poly.verts[i][2] = vec.Z;
            poly.verts[i][3] = s;
            poly.verts[i][4] = t;

            //
            // lightmap texture coordinates
            //
            s = Mathlib.DotProduct(ref vec, ref fa.texinfo.vecs[0]) + fa.texinfo.vecs[0].W;
            s -= fa.texturemins[0];
            s += fa.light_s * 16;
            s += 8;
            s /= BLOCK_WIDTH * 16;

            t = Mathlib.DotProduct(ref vec, ref fa.texinfo.vecs[1]) + fa.texinfo.vecs[1].W;
            t -= fa.texturemins[1];
            t += fa.light_t * 16;
            t += 8;
            t /= BLOCK_HEIGHT * 16;

            poly.verts[i][5] = s;
            poly.verts[i][6] = t;
        }

        //
        // remove co-linear points - Ed
        //
        if (gl_keeptjunctions.value == 0 && (fa.flags & q_shared.SURF_UNDERWATER) == 0)
        {
            for (int i = 0; i < lnumverts; ++i)
            {
                if (IsCollinear(poly.verts[(i + lnumverts - 1) % lnumverts],
                    poly.verts[i],
                    poly.verts[(i + 1) % lnumverts]))
                {
                    int j;
                    for (j = i + 1; j < lnumverts; ++j)
                    {
                        //int k;
                        for (int k = 0; k < q_shared.VERTEXSIZE; ++k)
                            poly.verts[j - 1][k] = poly.verts[j][k];
                    }
                    --lnumverts;
                    ++nColinElim;
                    // retry next vertex next time, which is now current vertex
                    --i;
                }
            }
        }
        poly.numverts = lnumverts;
    }
    public static bool IsCollinear(float[] prev, float[] cur, float[] next)
    {
        Vector3 v1 = new Vector3(cur[0] - prev[0], cur[1] - prev[1], cur[2] - prev[2]);
        Mathlib.Normalize(ref v1);
        Vector3 v2 = new Vector3(next[0] - prev[0], next[1] - prev[1], next[2] - prev[2]);
        Mathlib.Normalize(ref v2);
        v1 -= v2;
        return ((Math.Abs(v1.X) <= q_shared.COLINEAR_EPSILON) &&
            (Math.Abs(v1.Y) <= q_shared.COLINEAR_EPSILON) &&
            (Math.Abs(v1.Z) <= q_shared.COLINEAR_EPSILON));
    }
    public static int AllocBlock(int w, int h, ref int x, ref int y)
    {
        for (int texnum = 0; texnum < MAX_LIGHTMAPS; texnum++)
        {
            int best = BLOCK_HEIGHT;

            for (int i = 0; i < BLOCK_WIDTH - w; i++)
            {
                int j, best2 = 0;

                for (j = 0; j < w; j++)
                {
                    if (allocated[texnum, i + j] >= best)
                        break;
                    if (allocated[texnum, i + j] > best2)
                        best2 = allocated[texnum, i + j];
                }
                if (j == w)
                {
                    // this is a valid spot
                    x = i;
                    y = best = best2;
                }
            }

            if (best + h > BLOCK_HEIGHT)
                continue;

            for (int i = 0; i < w; i++)
                allocated[texnum, x + i] = best + h;

            return texnum;
        }

        Sys_Error("AllocBlock: full");
        return 0; // shut up compiler
    }
    public static void R_BuildLightMap(msurface_t surf, ByteArraySegment dest, int stride)
    {
        surf.cached_dlight = (surf.dlightframe == r_framecount);

        int smax = (surf.extents[0] >> 4) + 1;
        int tmax = (surf.extents[1] >> 4) + 1;
        int size = smax * tmax;

        int srcOffset = surf.sampleofs;
        byte[] lightmap = surf.sample_base;// surf.samples;

        // set to full bright if no light data
        if (r_fullbright.value != 0 || cl.worldmodel.lightdata == null)
        {
            for (int i = 0; i < size; i++)
                blocklights[i] = 255 * 256;
        }
        else
        {
            // clear to no light
            for (int i = 0; i < size; i++)
                blocklights[i] = 0;

            // add all the lightmaps
            if (lightmap != null)
                for (int maps = 0; maps < q_shared.MAXLIGHTMAPS && surf.styles[maps] != 255; maps++)
                {
                    int scale = d_lightstylevalue[surf.styles[maps]];
                    surf.cached_light[maps] = scale;	// 8.8 fraction
                    for (int i = 0; i < size; i++)
                        blocklights[i] += (uint)(lightmap[srcOffset + i] * scale);
                    srcOffset += size; // lightmap += size;	// skip to next lightmap
                }

            // add all the dynamic lights
            if (surf.dlightframe == r_framecount)
                R_AddDynamicLights(surf);
        }
        // bound, invert, and shift
        //store:
        int blOffset = 0;
        int destOffset = dest.StartIndex;
        byte[] data = dest.Data;
        switch (gl_lightmap_format)
        {
            case PixelFormat.Rgba:
                stride -= (smax << 2);
                for (int i = 0; i < tmax; i++, destOffset += stride) // dest += stride
                {
                    for (int j = 0; j < smax; j++)
                    {
                        uint t = blocklights[blOffset++];// *bl++;
                        t >>= 7;
                        if (t > 255)
                            t = 255;
                        data[destOffset + 3] = (byte)(255 - t); //dest[3] = 255 - t;
                        destOffset += 4;
                    }
                }
                break;

            case PixelFormat.Alpha:
            case PixelFormat.Luminance:
                //case GL_INTENSITY:
                for (int i = 0; i < tmax; i++, destOffset += stride)
                {
                    for (int j = 0; j < smax; j++)
                    {
                        uint t = blocklights[blOffset++];// *bl++;
                        t >>= 7;
                        if (t > 255)
                            t = 255;
                        data[destOffset + j] = (byte)(255 - t); // dest[j] = 255 - t;
                    }
                }
                break;

            default:
                Sys_Error("Bad lightmap format");
                break;
        }
    }
    public static void R_AddDynamicLights(msurface_t surf)
    {
        int smax = (surf.extents[0] >> 4) + 1;
        int tmax = (surf.extents[1] >> 4) + 1;
        mtexinfo_t tex = surf.texinfo;
        dlight_t[] dlights = cl_dlights;
            
        for (int lnum = 0; lnum < q_shared.MAX_DLIGHTS; lnum++)
        {
            if ((surf.dlightbits & (1 << lnum)) == 0)
                continue;		// not lit by this light

            float rad = dlights[lnum].radius;
            float dist = Vector3.Dot(dlights[lnum].origin, surf.plane.normal) - surf.plane.dist;
            rad -= Math.Abs(dist);
            float minlight = dlights[lnum].minlight;
            if (rad < minlight)
                continue;
            minlight = rad - minlight;

            Vector3 impact = dlights[lnum].origin - surf.plane.normal * dist;

            float local0 = Vector3.Dot(impact, tex.vecs[0].Xyz) + tex.vecs[0].W;
            float local1 = Vector3.Dot(impact, tex.vecs[1].Xyz) + tex.vecs[1].W;

            local0 -= surf.texturemins[0];
            local1 -= surf.texturemins[1];

            for (int t = 0; t < tmax; t++)
            {
                int td = (int)(local1 - t * 16);
                if (td < 0)
                    td = -td;
                for (int s = 0; s < smax; s++)
                {
                    int sd = (int)(local0 - s * 16);
                    if (sd < 0)
                        sd = -sd;
                    if (sd > td)
                        dist = sd + (td >> 1);
                    else
                        dist = td + (sd >> 1);
                    if (dist < minlight)
                        blocklights[t * smax + s] += (uint)((rad - dist) * 256);
                }
            }
        }
    }
    public static void R_DrawWaterSurfaces()
    {
        if (r_wateralpha.value == 1.0f && gl_texsort.value != 0)
            return;

        //
        // go back to the world matrix
        //
        GL.LoadMatrix(ref r_world_matrix);

        if (r_wateralpha.value < 1.0)
        {
            GL.Enable(EnableCap.Blend);
            GL.Color4(1, 1, 1, r_wateralpha.value);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
        }

        if (gl_texsort.value == 0)
        {
            if (waterchain == null)
                return;

            for (msurface_t s = waterchain; s != null; s = s.texturechain)
            {
                GL_Bind(s.texinfo.texture.gl_texturenum);
                EmitWaterPolys(s);
            }
            waterchain = null;
        }
        else
        {
            for (int i = 0; i < cl.worldmodel.numtextures; i++)
            {
                texture_t t = cl.worldmodel.textures[i];
                if (t == null)
                    continue;

                msurface_t s = t.texturechain;
                if (s == null)
                    continue;
                    
                if ((s.flags & q_shared.SURF_DRAWTURB) == 0)
                    continue;

                // set modulate mode explicitly

                GL_Bind(t.gl_texturenum);

                for (; s != null; s = s.texturechain)
                    EmitWaterPolys(s);

                t.texturechain = null;
            }
        }

        if (r_wateralpha.value < 1.0)
        {
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);
            GL.Color4(1f, 1, 1, 1);
            GL.Disable(EnableCap.Blend);
        }
    }
    public static void R_MarkLeaves()
    {
        if (r_oldviewleaf == r_viewleaf && r_novis.value == 0)
            return;

        if (mirror)
            return;

        r_visframecount++;
        r_oldviewleaf = r_viewleaf;
            
        byte[] vis;
        if (r_novis.value != 0)
        {
            vis = new byte[4096];
            FillArray<Byte>(vis, 0xff); // todo: add count parameter?
            //memset(solid, 0xff, (cl.worldmodel->numleafs + 7) >> 3);
        }
        else
            vis = Mod_LeafPVS(r_viewleaf, cl.worldmodel);

        model_t world = cl.worldmodel;
        for (int i = 0; i < world.numleafs; i++)
        {
            if (vis[i >> 3] != 0 & (1 << (i & 7)) != 0)
            {
                mnodebase_t node = world.leafs[i + 1];
                do
                {
                    if (node.visframe == r_visframecount)
                        break;
                    node.visframe = r_visframecount;
                    node = node.parent;
                } while (node != null);
            }
        }
    }
    public static void R_DrawWorld()
    {
        _TempEnt.Clear();
        _TempEnt.model = cl.worldmodel;

        modelorg = r_refdef.vieworg;
        currententity = _TempEnt;
        currenttexture = -1;

        GL.Color3(1f, 1, 1);

        Array.Clear(lightmap_polys, 0, lightmap_polys.Length);

        R_RecursiveWorldNode(_TempEnt.model.nodes[0]);

        DrawTextureChains();

        R_BlendLightmaps();
    }
    public static void R_BlendLightmaps()
    {
        if (r_fullbright.value != 0)
            return;
        if (gl_texsort.value == 0)
            return;

        GL.DepthMask(false); // don't bother writing Z

        if (gl_lightmap_format == PixelFormat.Luminance)
            GL.BlendFunc(BlendingFactorSrc.Zero, BlendingFactorDest.OneMinusSrcColor);
        //else if (gl_lightmap_format == GL_INTENSITY)
        //{
        //    glTexEnvf(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_MODULATE);
        //    glColor4f(0, 0, 0, 1);
        //    glBlendFunc(GL_SRC_ALPHA, GL_ONE_MINUS_SRC_ALPHA);
        //}

        if (r_lightmap.value == 0)
        {
            GL.Enable(EnableCap.Blend);
        }

        for (int i = 0; i < MAX_LIGHTMAPS; i++)
        {
            glpoly_t p = lightmap_polys[i];
            if (p == null)
                continue;

            GL_Bind(lightmap_textures + i);
            if (lightmap_modified[i])
                CommitLightmap(i);

            for (; p != null; p = p.chain)
            {
                if ((p.flags & q_shared.SURF_UNDERWATER) != 0)
                    DrawGLWaterPolyLightmap(p);
                else
                {
                    GL.Begin(BeginMode.Polygon);
                    for (int j = 0; j < p.numverts; j++)
                    {
                        float[] v = p.verts[j];
                        GL.TexCoord2(v[5], v[6]);
                        GL.Vertex3(v);
                    }
                    GL.End();
                }
            }
        }

        GL.Disable(EnableCap.Blend);
        if (gl_lightmap_format == PixelFormat.Luminance)
            GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        //else if (gl_lightmap_format == GL_INTENSITY)
        //{
        //    glTexEnvf(GL_TEXTURE_ENV, GL_TEXTURE_ENV_MODE, GL_REPLACE);
        //    glColor4f(1, 1, 1, 1);
        //}

        GL.DepthMask(true); // back to normal Z buffering
    }
    public static void DrawTextureChains()
    {
        if (gl_texsort.value == 0)
        {
            GL_DisableMultitexture();

            if (skychain != null)
            {
                R_DrawSkyChain(skychain);
                skychain = null;
            }
            return;
        }
        model_t world = cl.worldmodel;
        for (int i = 0; i < world.numtextures; i++)
        {
            texture_t t = world.textures[i];
            if (t == null)
                continue;

            msurface_t s = t.texturechain;
            if (s == null)
                continue;

            if (i == skytexturenum)
                R_DrawSkyChain(s);
            else if (i == mirrortexturenum && r_mirroralpha.value != 1.0f)
            {
                R_MirrorChain(s);
                continue;
            }
            else
            {
                if ((s.flags & q_shared.SURF_DRAWTURB) != 0 && r_wateralpha.value != 1.0f)
                    continue;	// draw translucent water later
                for (; s != null; s = s.texturechain)
                    R_RenderBrushPoly(s);
            }

            t.texturechain = null;
        }
    }
    public static void R_RenderBrushPoly(msurface_t fa)
    {
        c_brush_polys++;

        if ((fa.flags & q_shared. SURF_DRAWSKY) != 0)
        {	// warp texture, no lightmaps
            EmitBothSkyLayers(fa);
            return;
        }

        texture_t t = R_TextureAnimation(fa.texinfo.texture);
        GL_Bind(t.gl_texturenum);

        if ((fa.flags & q_shared. SURF_DRAWTURB) != 0)
        {	// warp texture, no lightmaps
            EmitWaterPolys(fa);
            return;
        }

        if ((fa.flags & q_shared.SURF_UNDERWATER) != 0)
            DrawGLWaterPoly(fa.polys);
        else
            DrawGLPoly(fa.polys);

        // add the poly to the proper lightmap chain

        fa.polys.chain = lightmap_polys[fa.lightmaptexturenum];
        lightmap_polys[fa.lightmaptexturenum] = fa.polys;

        // check for lightmap modification
        bool modified = false;
        for (int maps = 0; maps < q_shared.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++)
            if (d_lightstylevalue[fa.styles[maps]] != fa.cached_light[maps])
            {
                modified = true;
                break;
            }

        if (modified ||
            fa.dlightframe == r_framecount ||	// dynamic this frame
            fa.cached_dlight)			// dynamic previously
        {
            if (r_dynamic.value != 0)
            {
                lightmap_modified[fa.lightmaptexturenum] = true;
                UpdateRect(fa, ref lightmap_rectchange[fa.lightmaptexturenum]);
                int offset = fa.lightmaptexturenum * lightmap_bytes * BLOCK_WIDTH * BLOCK_HEIGHT;
                offset += fa.light_t * BLOCK_WIDTH * lightmap_bytes + fa.light_s * lightmap_bytes;
                R_BuildLightMap(fa, new ByteArraySegment(lightmaps, offset), BLOCK_WIDTH * lightmap_bytes);
            }
        }
    }
    public static void UpdateRect(msurface_t fa, ref glRect_t theRect)
    {
        if (fa.light_t < theRect.t)
        {
            if (theRect.h != 0)
                theRect.h += (byte)(theRect.t - fa.light_t);
            theRect.t = (byte)fa.light_t;
        }
        if (fa.light_s < theRect.l)
        {
            if (theRect.w != 0)
                theRect.w += (byte)(theRect.l - fa.light_s);
            theRect.l = (byte)fa.light_s;
        }
        int smax = (fa.extents[0] >> 4) + 1;
        int tmax = (fa.extents[1] >> 4) + 1;
        if ((theRect.w + theRect.l) < (fa.light_s + smax))
            theRect.w = (byte)((fa.light_s - theRect.l) + smax);
        if ((theRect.h + theRect.t) < (fa.light_t + tmax))
            theRect.h = (byte)((fa.light_t - theRect.t) + tmax);
    }
    public static void DrawGLPoly(glpoly_t p)
    {
        GL.Begin(BeginMode.Polygon);
        for (int i = 0; i < p.numverts; i++)
        {
            float[] v = p.verts[i];
            GL.TexCoord2(v[3], v[4]);
            GL.Vertex3(v);
        }
        GL.End();
    }
    public static void R_MirrorChain(msurface_t s)
    {
        if (mirror)
            return;
        mirror = true;
        mirror_plane = s.plane;
    }
    public static void R_RecursiveWorldNode(mnodebase_t node)
    {
        if (node.contents == q_shared.CONTENTS_SOLID)
            return;		// solid

        if (node.visframe != r_visframecount)
            return;
        if (R_CullBox(ref node.mins, ref node.maxs))
            return;

        int c;

        // if a leaf node, draw stuff
        if (node.contents < 0)
        {
            mleaf_t pleaf = (mleaf_t)node;
            msurface_t[] marks = pleaf.marksurfaces;
            int mark = pleaf.firstmarksurface;
            c = pleaf.nummarksurfaces;

            if (c != 0)
            {
                do
                {
                    marks[mark].visframe = r_framecount;
                    mark++;
                } while (--c != 0);
            }

            // deal with model fragments in this leaf
            if (pleaf.efrags != null)
                R_StoreEfrags(pleaf.efrags);

            return;
        }

        // node is just a decision point, so go down the apropriate sides

        mnode_t n = (mnode_t)node;

        // find which side of the node we are on
        mplane_t plane = n.plane;
        double dot;

        switch (plane.type)
        {
            case q_shared.PLANE_X:
                dot = modelorg.X - plane.dist;
                break;

            case q_shared.PLANE_Y:
                dot = modelorg.Y - plane.dist;
                break;

            case q_shared.PLANE_Z:
                dot = modelorg.Z - plane.dist;
                break;

            default:
                dot = Vector3.Dot(modelorg, plane.normal) - plane.dist;
                break;
        }

        int side = (dot >= 0 ? 0 : 1);

        // recurse down the children, front side first
        R_RecursiveWorldNode(n.children[side]);

        // draw stuff
        c = n.numsurfaces;

        if (c != 0)
        {
            msurface_t[] surf = cl.worldmodel.surfaces;
            int offset = n.firstsurface;

            if (dot < 0 - q_shared.BACKFACE_EPSILON)
                side = q_shared.SURF_PLANEBACK;
            else if (dot > q_shared.BACKFACE_EPSILON)
                side = 0;

            for (; c != 0; c--, offset++)
            {
                if (surf[offset].visframe != r_framecount)
                    continue;

                // don't backface underwater surfaces, because they warp
                if ((surf[offset].flags & q_shared.SURF_UNDERWATER) == 0 && ((dot < 0) ^ ((surf[offset].flags & q_shared.SURF_PLANEBACK) != 0)))
                    continue;		// wrong side

                // if sorting by texture, just store it out
                if (gl_texsort.value != 0)
                {
                    if (!mirror || surf[offset].texinfo.texture != cl.worldmodel.textures[mirrortexturenum])
                    {
                        surf[offset].texturechain = surf[offset].texinfo.texture.texturechain;
                        surf[offset].texinfo.texture.texturechain = surf[offset];
                    }
                }
                else if ((surf[offset].flags & q_shared.SURF_DRAWSKY) != 0)
                {
                    surf[offset].texturechain = skychain;
                    skychain = surf[offset];
                }
                else if ((surf[offset].flags & q_shared.SURF_DRAWTURB) != 0)
                {
                    surf[offset].texturechain = waterchain;
                    waterchain = surf[offset];
                }
                else
                    R_DrawSequentialPoly(surf[offset]);
            }
        }

        // recurse down the back side
        R_RecursiveWorldNode(n.children[side == 0 ? 1 : 0]);
    }
    public static void R_DrawSequentialPoly(msurface_t s)
    {
        //
        // normal lightmaped poly
        //
        if ((s.flags & (q_shared.SURF_DRAWSKY | q_shared.SURF_DRAWTURB | q_shared.SURF_UNDERWATER)) == 0)
        {
            R_RenderDynamicLightmaps(s);
            glpoly_t p = s.polys;
            texture_t t = R_TextureAnimation(s.texinfo.texture);
            if (gl_mtexable)
            {
                // Binds world to texture env 0
                GL_SelectTexture(MTexTarget.TEXTURE0_SGIS);
                GL_Bind(t.gl_texturenum);
                GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);

                // Binds lightmap to texenv 1
                GL_EnableMultitexture(); // Same as SelectTexture (TEXTURE1)
                GL_Bind(lightmap_textures + s.lightmaptexturenum);
                int i = s.lightmaptexturenum;
                if (lightmap_modified[i])
                    CommitLightmap(i);

                GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Blend);
                GL.Begin(BeginMode.Polygon);
                for (i = 0; i < p.numverts; i++)
                {
                    float[] v = p.verts[i];
                    GL.MultiTexCoord2(TextureUnit.Texture0, v[3], v[4]);
                    GL.MultiTexCoord2(TextureUnit.Texture1, v[5], v[6]);
                    GL.Vertex3(v);
                }
                GL.End();
                return;
            }
            else
            {
                GL_Bind(t.gl_texturenum);
                GL.Begin(BeginMode.Polygon);
                for (int i = 0; i < p.numverts; i++)
                {
                    float[] v = p.verts[i];
                    GL.TexCoord2(v[3], v[4]);
                    GL.Vertex3(v);
                }
                GL.End();

                GL_Bind(lightmap_textures + s.lightmaptexturenum);
                GL.Enable(EnableCap.Blend);
                GL.Begin(BeginMode.Polygon);
                for (int i = 0; i < p.numverts; i++)
                {
                    float[] v = p.verts[i];
                    GL.TexCoord2(v[5], v[6]);
                    GL.Vertex3(v);
                }
                GL.End();

                GL.Disable(EnableCap.Blend);
            }

            return;
        }

        //
        // subdivided water surface warp
        //

        if ((s.flags & q_shared.SURF_DRAWTURB) != 0)
        {
            GL_DisableMultitexture();
            GL_Bind(s.texinfo.texture.gl_texturenum);
            EmitWaterPolys(s);
            return;
        }

        //
        // subdivided sky warp
        //
        if ((s.flags & q_shared.SURF_DRAWSKY) != 0)
        {
            GL_DisableMultitexture();
            GL_Bind(solidskytexture);
            speedscale = (float)realtime * 8;
            speedscale -= (int)speedscale & ~127;

            EmitSkyPolys(s);

            GL.Enable(EnableCap.Blend);
            GL_Bind(alphaskytexture);
            speedscale = (float)realtime * 16;
            speedscale -= (int)speedscale & ~127;

            EmitSkyPolys(s);

            GL.Disable(EnableCap.Blend);
            return;
        }

        //
        // underwater warped with lightmap
        //
        R_RenderDynamicLightmaps(s);
        if (gl_mtexable)
        {
            texture_t t = R_TextureAnimation(s.texinfo.texture);
            GL_SelectTexture(MTexTarget.TEXTURE0_SGIS);
            GL_Bind(t.gl_texturenum);
            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);
            GL_EnableMultitexture();
            GL_Bind(lightmap_textures + s.lightmaptexturenum);
            int i = s.lightmaptexturenum;
            if (lightmap_modified[i])
                CommitLightmap(i);

            GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Blend);
            GL.Begin(BeginMode.TriangleFan);
            glpoly_t p = s.polys;
            float[] nv = new float[3];
            for (i = 0; i < p.numverts; i++)
            {
                float[] v = p.verts[i];
                GL.MultiTexCoord2(TextureUnit.Texture0, v[3], v[4]);
                GL.MultiTexCoord2(TextureUnit.Texture1, v[5], v[6]);

                nv[0] = (float)(v[0] + 8 * Math.Sin(v[1] * 0.05 + realtime) * Math.Sin(v[2] * 0.05 + realtime));
                nv[1] = (float)(v[1] + 8 * Math.Sin(v[0] * 0.05 + realtime) * Math.Sin(v[2] * 0.05 + realtime));
                nv[2] = v[2];

                GL.Vertex3(nv);
            }
            GL.End();
        }
        else
        {
            glpoly_t p = s.polys;

            texture_t t = R_TextureAnimation(s.texinfo.texture);
            GL_Bind(t.gl_texturenum);
            DrawGLWaterPoly(p);

            GL_Bind(lightmap_textures + s.lightmaptexturenum);
            GL.Enable(EnableCap.Blend);
            DrawGLWaterPolyLightmap(p);
            GL.Disable(EnableCap.Blend);
        }
    }
    public static void DrawGLWaterPolyLightmap(glpoly_t p)
    {
        GL_DisableMultitexture();

        float[] nv = new float[3];
        GL.Begin(BeginMode.TriangleFan);

        for (int i = 0; i < p.numverts; i++)
        {
            float[] v = p.verts[i];
            GL.TexCoord2(v[5], v[6]);

            nv[0] = (float)(v[0] + 8 * Math.Sin(v[1] * 0.05 + realtime) * Math.Sin(v[2] * 0.05 + realtime));
            nv[1] = (float)(v[1] + 8 * Math.Sin(v[0] * 0.05 + realtime) * Math.Sin(v[2] * 0.05 + realtime));
            nv[2] = v[2];

            GL.Vertex3(nv);
        }
        GL.End();
    }
    public static void DrawGLWaterPoly(glpoly_t p)
    {
        GL_DisableMultitexture();

        float[] nv = new float[3];
        GL.Begin(BeginMode.TriangleFan);
        for (int i = 0; i < p.numverts; i++)
        {
            float[] v = p.verts[i];

            GL.TexCoord2(v[3], v[4]);

            nv[0] = (float)(v[0] + 8 * Math.Sin(v[1] * 0.05 + realtime) * Math.Sin(v[2] * 0.05 + realtime));
            nv[1] = (float)(v[1] + 8 * Math.Sin(v[0] * 0.05 + realtime) * Math.Sin(v[2] * 0.05 + realtime));
            nv[2] = v[2];

            GL.Vertex3(nv);
        }
        GL.End();
    }
    public static void CommitLightmap(int i)
    {
        lightmap_modified[i] = false;
        glRect_t theRect = lightmap_rectchange[i];
        GCHandle handle = GCHandle.Alloc(lightmaps, GCHandleType.Pinned);
        try
        {
            long addr = handle.AddrOfPinnedObject().ToInt64() +
                (i * BLOCK_HEIGHT + theRect.t) * BLOCK_WIDTH * lightmap_bytes;
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, theRect.t,
                BLOCK_WIDTH, theRect.h, gl_lightmap_format,
                PixelType.UnsignedByte, new IntPtr(addr));
        }
        finally
        {
            handle.Free();
        }
        theRect.l = BLOCK_WIDTH;
        theRect.t = BLOCK_HEIGHT;
        theRect.h = 0;
        theRect.w = 0;
        lightmap_rectchange[i] = theRect;
    }
    public static texture_t R_TextureAnimation(texture_t t)
    {
        if (currententity.frame != 0)
        {
            if (t.alternate_anims != null)
                t = t.alternate_anims;
        }

        if (t.anim_total == 0)
            return t;

        int reletive = (int)(cl.time * 10) % t.anim_total;
        int count = 0;
        while (t.anim_min > reletive || t.anim_max <= reletive)
        {
            t = t.anim_next;
            if (t == null)
                Sys_Error("R_TextureAnimation: broken cycle");
            if (++count > 100)
                Sys_Error("R_TextureAnimation: infinite cycle");
        }

        return t;
    }
    public static void R_RenderDynamicLightmaps(msurface_t fa)
    {
        c_brush_polys++;

        if ((fa.flags & (q_shared.SURF_DRAWSKY | q_shared.SURF_DRAWTURB)) != 0)
            return;

        fa.polys.chain = lightmap_polys[fa.lightmaptexturenum];
        lightmap_polys[fa.lightmaptexturenum] = fa.polys;

        // check for lightmap modification
        bool flag = false;
        for (int maps = 0; maps < q_shared.MAXLIGHTMAPS && fa.styles[maps] != 255; maps++)
            if (d_lightstylevalue[fa.styles[maps]] != fa.cached_light[maps])
            {
                flag = true;
                break;
            }

        if (flag ||
            fa.dlightframe == r_framecount || // dynamic this frame
            fa.cached_dlight)	// dynamic previously
        {
            if (r_dynamic.value != 0)
            {
                lightmap_modified[fa.lightmaptexturenum] = true;
                UpdateRect(fa, ref lightmap_rectchange[fa.lightmaptexturenum]);
                int offset = fa.lightmaptexturenum * lightmap_bytes * BLOCK_WIDTH * BLOCK_HEIGHT +
                    fa.light_t * BLOCK_WIDTH * lightmap_bytes + fa.light_s * lightmap_bytes;
                R_BuildLightMap(fa, new ByteArraySegment(lightmaps, offset), BLOCK_WIDTH * lightmap_bytes);
            }
        }
    }
    public static void R_DrawBrushModel(entity_t e)
    {
        currententity = e;
        currenttexture = -1;

        model_t clmodel = e.model;
        bool rotated = false;
        Vector3 mins, maxs;
        if (e.angles.X != 0 || e.angles.Y != 0 || e.angles.Z != 0)
        {
            rotated = true;
            mins = e.origin;
            mins.X -= clmodel.radius;
            mins.Y -= clmodel.radius;
            mins.Z -= clmodel.radius;
            maxs = e.origin;
            maxs.X += clmodel.radius;
            maxs.Y += clmodel.radius;
            maxs.Z += clmodel.radius;
        }
        else
        {
            mins = e.origin + clmodel.mins;
            maxs = e.origin + clmodel.maxs;
        }

        if (R_CullBox(ref mins, ref maxs))
            return;

        GL.Color3(1f, 1, 1);
        Array.Clear(lightmap_polys, 0, lightmap_polys.Length);
        modelorg = r_refdef.vieworg - e.origin;
        if (rotated)
        {
            Vector3 temp = modelorg;
            Vector3 forward, right, up;
            Mathlib.AngleVectors(ref e.angles, out forward, out right, out up);
            modelorg.X = Vector3.Dot(temp, forward);
            modelorg.Y = -Vector3.Dot(temp, right);
            modelorg.Z = Vector3.Dot(temp, up);
        }

        // calculate dynamic lighting for bmodel if it's not an
        // instanced model
        if (clmodel.firstmodelsurface != 0 && gl_flashblend.value == 0)
        {
            for (int k = 0; k < q_shared.MAX_DLIGHTS; k++)
            {
                if ((cl_dlights[k].die < cl.time) || (cl_dlights[k].radius == 0))
                    continue;

                R_MarkLights(cl_dlights[k], 1 << k, clmodel.nodes[clmodel.hulls[0].firstclipnode]);
            }
        }

        GL.PushMatrix();
        e.angles.X = -e.angles.X;	// stupid quake bug
        R_RotateForEntity(e);
        e.angles.X = -e.angles.X;	// stupid quake bug

        int surfOffset = clmodel.firstmodelsurface;
        msurface_t[] psurf = clmodel.surfaces; //[clmodel.firstmodelsurface];

        //
        // draw texture
        //
        for (int i = 0; i < clmodel.nummodelsurfaces; i++, surfOffset++)
        {
            // find which side of the node we are on
            mplane_t pplane = psurf[surfOffset].plane;

            float dot = Vector3.Dot(modelorg, pplane.normal) - pplane.dist;

            // draw the polygon
            bool planeBack = (psurf[surfOffset].flags & q_shared.SURF_PLANEBACK) != 0;
            if ((planeBack && (dot < -q_shared.BACKFACE_EPSILON)) || (!planeBack && (dot > q_shared.BACKFACE_EPSILON)))
            {
                if (gl_texsort.value != 0)
                    R_RenderBrushPoly(psurf[surfOffset]);
                else
                    R_DrawSequentialPoly(psurf[surfOffset]);
            }
        }

        R_BlendLightmaps();

        GL.PopMatrix();
    }
}