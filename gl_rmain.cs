using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    // Hack: put this in q_shared.cs
    public const int MAXCLIPPLANES = 11;
    public const int TOP_RANGE = 16;
    public const int BOTTOM_RANGE = 96;
    public const float ONE_OVER_16 = 1.0f / 16.0f;
    public const int MAX_LIGHTMAPS = 64;
    public const int BLOCK_WIDTH = 128;
    public const int BLOCK_HEIGHT = 128;

    public static refdef_t r_refdef = new refdef_t();
    public static texture_t r_notexture_mip;

    public static cvar_t r_norefresh;
    public static cvar_t r_drawentities;
    public static cvar_t r_drawviewmodel;
    public static cvar_t r_speeds;
    public static cvar_t r_fullbright;
    public static cvar_t r_lightmap;
    public static cvar_t r_shadows;
    public static cvar_t r_mirroralpha;
    public static cvar_t r_wateralpha;
    public static cvar_t r_dynamic;
    public static cvar_t r_novis;

    public static cvar_t gl_finish;
    public static cvar_t gl_clear;
    public static cvar_t gl_cull;
    public static cvar_t gl_texsort;
    public static cvar_t gl_smoothmodels;
    public static cvar_t gl_affinemodels;
    public static cvar_t gl_polyblend;
    public static cvar_t gl_flashblend;
    public static cvar_t gl_playermip;
    public static cvar_t gl_nocolors;
    public static cvar_t gl_keeptjunctions;
    public static cvar_t gl_reporttjunctions;
    public static cvar_t gl_doubleeys;

    public static int playertextures;
    public static bool r_cache_thrash;

    //
    // view origin
    //
    public static Vector3 vup;
    public static Vector3 vpn;
    public static Vector3 vright;
    public static Vector3 r_origin;

    public static int[] d_lightstylevalue = new int[256];
    public static entity_t r_worldentity = new entity_t();
    public static entity_t currententity;

    public static mleaf_t r_viewleaf;
    public static mleaf_t r_oldviewleaf;

    public static int skytexturenum;
    public static int mirrortexturenum;

    public static int[,] allocated = new int[MAX_LIGHTMAPS,BLOCK_WIDTH];

    public static int r_visframecount;
    public static int r_framecount;
    public static bool mtexenabled;
    public static int c_brush_polys;
    public static int c_alias_polys;
    public static bool mirror; 
    public static mplane_t mirror_plane;
    public static float gldepthmin;
    public static float gldepthmax;
    public static int _TrickFrame; // static int trickframe from R_Clear()
    public static mplane_t[] frustum = new mplane_t[4];
    public static bool envmap = false;
    public static Matrix4 r_world_matrix;
    public static Matrix4 r_base_world_matrix;
    public static Vector3 modelorg;
    public static Vector3 r_entorigin;
    public static float speedscale;
    public static float shadelight;
    public static float ambientlight;
    public static float[] shadedots = anorm_dots[0];
    public static Vector3 shadevector;
    public static int lastposenum;
    public static Vector3 lightspot;
        

    public static void R_Init()
    {
        for (int i = 0; i < frustum.Length; i++)
            frustum[i] = new mplane_t();

        Cmd_AddCommand("timerefresh", R_TimeRefresh_f);
	    //Cmd.Add("envmap", Envmap_f);
	    //Cmd.Add("pointfile", ReadPointFile_f);

        r_norefresh = new cvar_t("r_norefresh", "0");
        r_drawentities = new cvar_t("r_drawentities", "1");
        r_drawviewmodel = new cvar_t("r_drawviewmodel", "1");
        r_speeds = new cvar_t("r_speeds", "0");
        r_fullbright = new cvar_t("r_fullbright", "0");
        r_lightmap = new cvar_t("r_lightmap", "0");
        r_shadows = new cvar_t("r_shadows", "0");
        r_mirroralpha = new cvar_t("r_mirroralpha", "1");
        r_wateralpha = new cvar_t("r_wateralpha", "1");
        r_dynamic = new cvar_t("r_dynamic", "1");
        r_novis = new cvar_t("r_novis", "0");

        gl_finish = new cvar_t("gl_finish", "0");
        gl_clear = new cvar_t("gl_clear", "0");
        gl_cull = new cvar_t("gl_cull", "1");
        gl_texsort = new cvar_t("gl_texsort", "1");
        gl_smoothmodels = new cvar_t("gl_smoothmodels", "1");
        gl_affinemodels = new cvar_t("gl_affinemodels", "0");
        gl_polyblend = new cvar_t("gl_polyblend", "1");
        gl_flashblend = new cvar_t("gl_flashblend", "1");
        gl_playermip = new cvar_t("gl_playermip", "0");
        gl_nocolors = new cvar_t("gl_nocolors", "0");
        gl_keeptjunctions = new cvar_t("gl_keeptjunctions", "0");
        gl_reporttjunctions = new cvar_t("gl_reporttjunctions", "0");
        gl_doubleeys = new cvar_t("gl_doubleeys", "1");

 	    if (gl_mtexable)
		    Cvar.Cvar_SetValue("gl_texsort", 0.0f);

        R_InitParticles();
        R_InitParticleTexture();

        // reserve 16 textures
        playertextures = GenerateTextureNumberRange(16);
    }
    public static void R_InitTextures()
    {
        // create a simple checkerboard texture for the default
        r_notexture_mip = new texture_t();
        r_notexture_mip.pixels = new byte[16 * 16 + 8 * 8 + 4 * 4 + 2 * 2];
        r_notexture_mip.width = r_notexture_mip.height = 16;
        int offset = 0;
        r_notexture_mip.offsets[0] = offset;
        offset += 16 * 16;
        r_notexture_mip.offsets[1] = offset;
        offset += 8 * 8;
        r_notexture_mip.offsets[2] = offset;
        offset += 4 * 4;
        r_notexture_mip.offsets[3] = offset;

        byte[] dest = r_notexture_mip.pixels;
        for (int m = 0; m < 4; m++)
        {
            offset = r_notexture_mip.offsets[m];
            for (int y = 0; y < (16 >> m); y++)
                for (int x = 0; x < (16 >> m); x++)
                {
                    if ((y < (8 >> m)) ^ (x < (8 >> m)))
                        dest[offset] = 0;
                    else
                        dest[offset] = 0xff;

                    offset++;
                }
        }	
    }
    public static void R_RenderView()
    {
        if (r_norefresh.value != 0)
            return;

        if (r_worldentity.model == null || cl.worldmodel == null)
            Sys_Error("R_RenderView: NULL worldmodel");

        double time1 = 0;
        if (r_speeds.value != 0)
        {
            GL.Finish();
            time1 = Sys_FloatTime();
            c_brush_polys = 0;
            c_alias_polys = 0;
        }

        mirror = false;

        if (gl_finish.value != 0)
            GL.Finish();

        R_Clear();

        // render normal view

        R_RenderScene();
        R_DrawViewModel();
        R_DrawWaterSurfaces();

        // render mirror view
        R_Mirror();

        R_PolyBlend();

        if (r_speeds.value != 0)
        {
            double time2 = Sys_FloatTime();
            Con_Printf("{0,3} ms  {1,4} wpoly {2,4} epoly\n", (int)((time2 - time1) * 1000), c_brush_polys, c_alias_polys);
        }
    }
    public static void R_PolyBlend()
    {
        if (gl_polyblend.value == 0)
            return;
            
        if (game_engine.v_blend.A == 0)
            return;

        GL_DisableMultitexture();

        GL.Disable(EnableCap.AlphaTest);
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.Texture2D);

        GL.LoadIdentity();

        GL.Rotate(-90f, 1, 0, 0);	    // put Z going up
        GL.Rotate(90f, 0, 0, 1);	    // put Z going up

        GL.Color4(game_engine.v_blend);
        GL.Begin(BeginMode.Quads);
        GL.Vertex3(10f, 100, 100);
        GL.Vertex3(10f, -100, 100);
        GL.Vertex3(10f, -100, -100);
        GL.Vertex3(10f, 100, -100);
        GL.End();

        GL.Disable(EnableCap.Blend);
        GL.Enable(EnableCap.Texture2D);
        GL.Enable(EnableCap.AlphaTest);
    }
    public static void R_Mirror()
    {
        if (!mirror)
            return;

        r_base_world_matrix = r_world_matrix;

        float d = Vector3.Dot(r_refdef.vieworg, mirror_plane.normal) - mirror_plane.dist;
        r_refdef.vieworg += mirror_plane.normal * -2 * d;

        d = Vector3.Dot(vpn, mirror_plane.normal);
        vpn += mirror_plane.normal * -2 * d;

        r_refdef.viewangles = new Vector3((float)(Math.Asin(vpn.Z) / Math.PI * 180.0),
            (float)(Math.Atan2(vpn.Y, vpn.X) / Math.PI * 180.0),
            -r_refdef.viewangles.Z);

        entity_t ent = cl_entities[cl.viewentity];
        if (cl_numvisedicts < q_shared.MAX_VISEDICTS)
        {
            cl_visedicts[cl_numvisedicts] = ent;
            cl_numvisedicts++;
        }

        gldepthmin = 0.5f;
        gldepthmax = 1;
        GL.DepthRange(gldepthmin, gldepthmax);
        GL.DepthFunc(DepthFunction.Lequal);

        R_RenderScene();
        R_DrawWaterSurfaces();

        gldepthmin = 0;
        gldepthmax = 0.5f;
        GL.DepthRange(gldepthmin, gldepthmax);
        GL.DepthFunc(DepthFunction.Lequal);

        // blend on top
        GL.Enable(EnableCap.Blend);
        GL.MatrixMode(MatrixMode.Projection);
        if (mirror_plane.normal.Z != 0)
            GL.Scale(1f, -1, 1);
        else
            GL.Scale(-1f, 1, 1);
        GL.CullFace(CullFaceMode.Front);
        GL.MatrixMode(MatrixMode.Modelview);

        GL.LoadMatrix(ref r_base_world_matrix);

        GL.Color4(1, 1, 1, r_mirroralpha.value);
        msurface_t s = cl.worldmodel.textures[mirrortexturenum].texturechain;
        for (; s != null; s = s.texturechain)
            R_RenderBrushPoly(s);
        cl.worldmodel.textures[mirrortexturenum].texturechain = null;
        GL.Disable(EnableCap.Blend);
        GL.Color4(1f, 1, 1, 1);
    }
    public static void R_DrawViewModel()
    {
        if (r_drawviewmodel.value == 0)
            return;

        if (chase_active.value != 0)
            return;

        if (envmap)
            return;

        if (r_drawentities.value == 0)
            return;

        if (cl.HasItems(q_shared.IT_INVISIBILITY))
            return;

        if (cl.stats[q_shared.STAT_HEALTH] <= 0)
            return;

        currententity = cl.viewent;
        if (currententity.model == null)
            return;

        int j = R_LightPoint(ref currententity.origin);

        if (j < 24)
            j = 24;		// allways give some light on gun
        ambientlight = j;
        shadelight = j;

        // add dynamic lights		
        for (int lnum = 0; lnum < q_shared.MAX_DLIGHTS; lnum++)
        {
            dlight_t dl = cl_dlights[lnum];
            if (dl.radius == 0)
                continue;
            if (dl.die < cl.time)
                continue;

            Vector3 dist = currententity.origin - dl.origin;
            float add = dl.radius - dist.Length;
            if (add > 0)
                ambientlight += add;
        }

        // hack the depth range to prevent view model from poking into walls
        GL.DepthRange(gldepthmin, gldepthmin + 0.3f * (gldepthmax - gldepthmin));
        R_DrawAliasModel(currententity);
        GL.DepthRange(gldepthmin, gldepthmax);
    }
    public static void R_RenderScene()
    {
        R_SetupFrame();
        R_SetFrustum();
        R_SetupGL();
        R_MarkLeaves();	// done here so we know if we're in water
        R_DrawWorld();		// adds static entities to the list
        S_ExtraUpdate();	// don't let sound get messed up if going slow
        R_DrawEntitiesOnList();
        GL_DisableMultitexture();
        R_RenderDlights();
        R_DrawParticles();
    }
    public static void R_DrawEntitiesOnList()
    {
        if (r_drawentities.value == 0)
            return;

        // draw sprites seperately, because of alpha blending
        for (int i = 0; i < cl_numvisedicts; i++)
        {
            currententity = cl_visedicts[i];

            switch (currententity.model.type)
            {
                case modtype_t.mod_alias:
                    R_DrawAliasModel(currententity);
                    break;

                case modtype_t.mod_brush:
                    R_DrawBrushModel(currententity);
                    break;

                default:
                    break;
            }
        }

        for (int i = 0; i < cl_numvisedicts; i++)
        {
            currententity = cl_visedicts[i];

            switch (currententity.model.type)
            {
                case modtype_t.mod_sprite:
                    R_DrawSpriteModel(currententity);
                    break;
            }
        }
    }
    public static void R_DrawSpriteModel(entity_t e)
    {
        // don't even bother culling, because it's just a single
        // polygon without a surface cache
        mspriteframe_t frame = R_GetSpriteFrame(e);
        msprite_t psprite = (msprite_t)e.model.cache.data;

        Vector3 v_forward, right, up;
        if (psprite.type == q_shared.SPR_ORIENTED)
        {
            // bullet marks on walls
            Mathlib.AngleVectors(ref e.angles, out v_forward, out right, out up); // Uze: changed from _CurrentEntity to e
        }
        else
        {	// normal sprite
            up = vup;// vup;
            right = vright;// vright;
        }

        GL.Color3(1f, 1, 1);

        GL_DisableMultitexture();

        GL_Bind(frame.gl_texturenum);

        GL.Enable(EnableCap.AlphaTest);
        GL.Begin(BeginMode.Quads);

        GL.TexCoord2(0f, 1);
        Vector3 point = e.origin + up * frame.down + right * frame.left;
        GL.Vertex3(point);

        GL.TexCoord2(0f, 0);
        point = e.origin + up * frame.up + right * frame.left;
        GL.Vertex3(point);

        GL.TexCoord2(1f, 0);
        point = e.origin + up * frame.up + right * frame.right;
        GL.Vertex3(point);

        GL.TexCoord2(1f, 1);
        point = e.origin + up * frame.down + right * frame.right;
        GL.Vertex3(point);

        GL.End();
        GL.Disable(EnableCap.AlphaTest);
    }
    public static mspriteframe_t R_GetSpriteFrame(entity_t currententity)
    {
        msprite_t psprite = (msprite_t)currententity.model.cache.data;
        int frame = currententity.frame;

        if ((frame >= psprite.numframes) || (frame < 0))
        {
            Con_Printf("R_DrawSprite: no such frame {0}\n", frame);
            frame = 0;
        }

        mspriteframe_t pspriteframe;
        if (psprite.frames[frame].type == spriteframetype_t.SPR_SINGLE)
        {
            pspriteframe = (mspriteframe_t)psprite.frames[frame].frameptr;
        }
        else
        {
            mspritegroup_t pspritegroup = (mspritegroup_t)psprite.frames[frame].frameptr;
            float[] pintervals = pspritegroup.intervals;
            int numframes = pspritegroup.numframes;
            float fullinterval = pintervals[numframes - 1];
            float time = (float)cl.time + currententity.syncbase;

            // when loading in Mod_LoadSpriteGroup, we guaranteed all interval values
            // are positive, so we don't have to worry about division by 0
            float targettime = time - ((int)(time / fullinterval)) * fullinterval;
            int i;
            for (i = 0; i < (numframes - 1); i++)
            {
                if (pintervals[i] > targettime)
                    break;
            }
            pspriteframe = pspritegroup.frames[i];
        }

        return pspriteframe;
    }
    public static void R_DrawAliasModel(entity_t e)
    {
        model_t clmodel = currententity.model;
        Vector3 mins = currententity.origin + clmodel.mins;
        Vector3 maxs = currententity.origin + clmodel.maxs;

        if (R_CullBox(ref mins, ref maxs))
            return;

        r_entorigin = currententity.origin;
        modelorg = r_origin - r_entorigin;

        //
        // get lighting information
        //

        ambientlight = shadelight = R_LightPoint(ref currententity.origin);

        // allways give the gun some light
        if (e == cl.viewent && ambientlight < 24)
            ambientlight = shadelight = 24;

        for (int lnum = 0; lnum < q_shared.MAX_DLIGHTS; lnum++)
        {
            if (cl_dlights[lnum].die >= cl.time)
            {
                Vector3 dist = currententity.origin - cl_dlights[lnum].origin;
                float add = cl_dlights[lnum].radius - dist.Length;
                if (add > 0)
                {
                    ambientlight += add;
                    //ZOID models should be affected by dlights as well
                    shadelight += add;
                }
            }
        }

        // clamp lighting so it doesn't overbright as much
        if (ambientlight > 128)
            ambientlight = 128;
        if (ambientlight + shadelight > 192)
            shadelight = 192 - ambientlight;

        // ZOID: never allow players to go totally black
        int playernum = Array.IndexOf(cl_entities, currententity, 0, cl.maxclients);
        if (playernum >= 1)// && i <= cl.maxclients)
            if (ambientlight < 8)
                ambientlight = shadelight = 8;

        // HACK HACK HACK -- no fullbright colors, so make torches full light
        if (clmodel.name == "progs/flame2.mdl" || clmodel.name == "progs/flame.mdl")
            ambientlight = shadelight = 256;

        shadedots = anorm_dots[((int)(e.angles.Y * (q_shared.SHADEDOT_QUANT / 360.0))) & (q_shared.SHADEDOT_QUANT - 1)];
        shadelight = shadelight / 200.0f;

        double an = e.angles.Y / 180.0 * Math.PI;
        shadevector.X = (float)Math.Cos(-an);
        shadevector.Y = (float)Math.Sin(-an);
        shadevector.Z = 1;
        Mathlib.Normalize(ref shadevector);

        //
        // locate the proper data
        //
        aliashdr_t paliashdr = Mod_Extradata(currententity.model);

        c_alias_polys += paliashdr.numtris;

        //
        // draw all the triangles
        //

        GL_DisableMultitexture();

        GL.PushMatrix();
        R_RotateForEntity(e);
        if (clmodel.name == "progs/eyes.mdl" && gl_doubleeys.value != 0)
        {
            Vector3 v = paliashdr.scale_origin;
            v.Z -= (22 + 8);
            GL.Translate(v);
            // double size of eyes, since they are really hard to see in gl
            GL.Scale(paliashdr.scale * 2.0f);
        }
        else
        {
            GL.Translate(paliashdr.scale_origin);
            GL.Scale(paliashdr.scale);
        }

        int anim = (int)(cl.time * 10) & 3;
        GL_Bind(paliashdr.gl_texturenum[currententity.skinnum, anim]);

        // we can't dynamically colormap textures, so they are cached
        // seperately for the players.  Heads are just uncolored.
        if (currententity.colormap != vid.colormap && gl_nocolors.value == 0 && playernum >= 1)
        {
            GL_Bind(playertextures - 1 + playernum);
        }

        if (gl_smoothmodels.value != 0)
            GL.ShadeModel(ShadingModel.Smooth);

        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);

        if (gl_affinemodels.value != 0)
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Fastest);

        R_SetupAliasFrame(currententity.frame, paliashdr);

        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);

        GL.ShadeModel(ShadingModel.Flat);
        if (gl_affinemodels.value != 0)
            GL.Hint(HintTarget.PerspectiveCorrectionHint, HintMode.Nicest);

        GL.PopMatrix();

        if (r_shadows.value != 0)
        {
            GL.PushMatrix();
            R_RotateForEntity(e);
            GL.Disable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Blend);
            GL.Color4(0, 0, 0, 0.5f);
            GL_DrawAliasShadow(paliashdr, lastposenum);
            GL.Enable(EnableCap.Texture2D);
            GL.Disable(EnableCap.Blend);
            GL.Color4(1f, 1, 1, 1);
            GL.PopMatrix();
        }
    }
    public static void GL_DrawAliasShadow(aliashdr_t paliashdr, int posenum)
    {
        float lheight = currententity.origin.Z - lightspot.Z;
        float height = 0;
        trivertx_t[] verts = paliashdr.posedata;
        int voffset = posenum * paliashdr.poseverts;
        int[] order = paliashdr.commands;

        height = -lheight + 1.0f;
        int orderOffset = 0;

        while (true)
        {
            // get the vertex count and primitive type
            int count = order[orderOffset++];
            if (count == 0)
                break;		// done

            if (count < 0)
            {
                count = -count;
                GL.Begin(BeginMode.TriangleFan);
            }
            else
                GL.Begin(BeginMode.TriangleStrip);

            do
            {
                // texture coordinates come from the draw list
                // (skipped for shadows) glTexCoord2fv ((float *)order);
                orderOffset += 2;

                // normals and vertexes come from the frame list
                Vector3 point = new Vector3(
                    verts[voffset].v[0] * paliashdr.scale.X + paliashdr.scale_origin.X,
                    verts[voffset].v[1] * paliashdr.scale.Y + paliashdr.scale_origin.Y,
                    verts[voffset].v[2] * paliashdr.scale.Z + paliashdr.scale_origin.Z
                );

                point.X -= shadevector.X * (point.Z + lheight);
                point.Y -= shadevector.Y * (point.Z + lheight);
                point.Z = height;

                GL.Vertex3(point);

                voffset++;
            } while (--count > 0);

            GL.End();
        }
    }
    public static void R_SetupAliasFrame(int frame, aliashdr_t paliashdr)
    {
        if ((frame >= paliashdr.numframes) || (frame < 0))
        {
            Con_DPrintf("R_AliasSetupFrame: no such frame {0}\n", frame);
            frame = 0;
        }

        int pose = paliashdr.frames[frame].firstpose;
        int numposes = paliashdr.frames[frame].numposes;

        if (numposes > 1)
        {
            float interval = paliashdr.frames[frame].interval;
            pose += (int)(cl.time / interval) % numposes;
        }

        GL_DrawAliasFrame(paliashdr, pose);
    }
    public static void GL_DrawAliasFrame(aliashdr_t paliashdr, int posenum)
    {
        lastposenum = posenum;

        trivertx_t[] verts = paliashdr.posedata;
        int vertsOffset = posenum * paliashdr.poseverts;
        int[] order = paliashdr.commands;
        int orderOffset = 0;

        while (true)
        {
            // get the vertex count and primitive type
            int count = order[orderOffset++];
            if (count == 0)
                break;		// done

            if (count < 0)
            {
                count = -count;
                GL.Begin(BeginMode.TriangleFan);
            }
            else
                GL.Begin(BeginMode.TriangleStrip);

            Union4b u1 = Union4b.Empty, u2 = Union4b.Empty;
            do
            {
                float[] v = { (float)verts[vertsOffset].v[0], verts[vertsOffset].v[1], verts[vertsOffset].v[2] };


                // texture coordinates come from the draw list
                u1.i0 = order[orderOffset + 0];
                u2.i0 = order[orderOffset + 1];
                orderOffset += 2;
                GL.TexCoord2(u1.f0, u2.f0);

                // normals and vertexes come from the frame list
                float l = shadedots[verts[vertsOffset].lightnormalindex] * shadelight;
                GL.Color3(l, l, l);
                GL.Vertex3(v[0], v[1], v[2]);
                vertsOffset++;
            } while (--count > 0);
            GL.End();
        }
    }
    public static void R_RotateForEntity(entity_t e)
    {
        GL.Translate(e.origin);

        GL.Rotate(e.angles.Y, 0, 0, 1);
        GL.Rotate(-e.angles.X, 0, 1, 0);
        GL.Rotate(e.angles.Z, 1, 0, 0);
    }
    public static void R_SetupGL()
    {
        //
        // set up viewpoint
        //
        GL.MatrixMode(MatrixMode.Projection);
        GL.LoadIdentity();
        int x = r_refdef.vrect.x * glWidth / vid.width;
        int x2 = (r_refdef.vrect.x + r_refdef.vrect.width) * glWidth / vid.width;
        int y = (vid.height - r_refdef.vrect.y) * glHeight / vid.height;
        int y2 = (vid.height - (r_refdef.vrect.y + r_refdef.vrect.height)) * glHeight / vid.height;

        // fudge around because of frac screen scale
        if (x > 0)
            x--;
        if (x2 < glWidth)
            x2++;
        if (y2 < 0)
            y2--;
        if (y < glHeight)
            y++;

        int w = x2 - x;
        int h = y - y2;

        if (envmap)
        {
            x = y2 = 0;
            w = h = 256;
        }

        GL.Viewport(glX + x, glY + y2, w, h);
        float screenaspect = (float)r_refdef.vrect.width / r_refdef.vrect.height;
        MYgluPerspective(r_refdef.fov_y, screenaspect, 4, 4096);

        if (mirror)
        {
            if (mirror_plane.normal.Z != 0)
                GL.Scale(1f, -1f, 1f);
            else
                GL.Scale(-1f, 1f, 1f);
            GL.CullFace(CullFaceMode.Back);
        }
        else
            GL.CullFace(CullFaceMode.Front);


        GL.MatrixMode(MatrixMode.Modelview);
        GL.LoadIdentity();

        GL.Rotate(-90f, 1, 0, 0);	    // put Z going up
        GL.Rotate(90f, 0, 0, 1);	    // put Z going up
        GL.Rotate(-r_refdef.viewangles.Z, 1, 0, 0);
        GL.Rotate(-r_refdef.viewangles.X, 0, 1, 0);
        GL.Rotate(-r_refdef.viewangles.Y, 0, 0, 1);
        GL.Translate(-r_refdef.vieworg.X, -r_refdef.vieworg.Y, -r_refdef.vieworg.Z);

        GL.GetFloat(GetPName.ModelviewMatrix, out r_world_matrix);

        //
        // set drawing parms
        //
        if (gl_cull.value != 0)
            GL.Enable(EnableCap.CullFace);
        else
            GL.Disable(EnableCap.CullFace);

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.AlphaTest);
        GL.Enable(EnableCap.DepthTest);
    }
    public static void MYgluPerspective(double fovy, double aspect, double zNear, double zFar)
    {
        double ymax = zNear * Math.Tan(fovy * Math.PI / 360.0);
        double ymin = -ymax;

        double xmin = ymin * aspect;
        double xmax = ymax * aspect;

        GL.Frustum(xmin, xmax, ymin, ymax, zNear, zFar);
    }
    public static void R_SetFrustum()
    {
        if (r_refdef.fov_x == 90)
        {
            // front side is visible
            frustum[0].normal = vpn + vright;
            frustum[1].normal = vpn - vright;

            frustum[2].normal = vpn + vup;
            frustum[3].normal = vpn - vup;
        }
        else
        {
            // rotate VPN right by FOV_X/2 degrees
            Mathlib.RotatePointAroundVector(out frustum[0].normal, ref vup, ref vpn, -(90 - r_refdef.fov_x / 2));
            // rotate VPN left by FOV_X/2 degrees
            Mathlib.RotatePointAroundVector(out frustum[1].normal, ref vup, ref vpn, 90 - r_refdef.fov_x / 2);
            // rotate VPN up by FOV_X/2 degrees
            Mathlib.RotatePointAroundVector(out frustum[2].normal, ref vright, ref vpn, 90 - r_refdef.fov_y / 2);
            // rotate VPN down by FOV_X/2 degrees
            Mathlib.RotatePointAroundVector(out frustum[3].normal, ref vright, ref vpn, -(90 - r_refdef.fov_y / 2));
        }

        for (int i = 0; i < 4; i++)
        {
            frustum[i].type = q_shared.PLANE_ANYZ;
            frustum[i].dist = Vector3.Dot(r_origin, frustum[i].normal);
            frustum[i].signbits = (byte)SignbitsForPlane(frustum[i]);
        }
    }
    public static int SignbitsForPlane (mplane_t p)
    {
	    // for fast box on planeside test
        int bits = 0;
        if (p.normal.X < 0) bits |= 1 << 0;
        if (p.normal.Y < 0) bits |= 1 << 1;
        if (p.normal.Z < 0) bits |= 1 << 2;
	    return bits;
    }
    public static void R_SetupFrame()
    {
        // don't allow cheats in multiplayer
        if (cl.maxclients > 1)
            Cvar.Cvar_Set("r_fullbright", "0");

        R_AnimateLight();

        r_framecount++;

        // build the transformation matrix for the given view angles
        r_origin = r_refdef.vieworg;

        Mathlib.AngleVectors(ref r_refdef.viewangles, out vpn, out vright, out vup);

        // current viewleaf
        r_oldviewleaf = r_viewleaf;
        r_viewleaf = Mod_PointInLeaf(ref r_origin, cl.worldmodel);

        game_engine.V_SetContentsColor(r_viewleaf.contents);
        game_engine.V_CalcBlend();

        r_cache_thrash = false;
        c_brush_polys = 0;
        c_alias_polys = 0;
    }
    public static void R_Clear()
    {
        if (r_mirroralpha.value != 1.0)
        {
            if (gl_clear.value != 0)
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            else
                GL.Clear(ClearBufferMask.DepthBufferBit);
            gldepthmin = 0;
            gldepthmax = 0.5f;
            GL.DepthFunc(DepthFunction.Lequal);
        }
        else if (gl_ztrick.value != 0)
        {
            if (gl_clear.value != 0)
                GL.Clear(ClearBufferMask.ColorBufferBit);

            _TrickFrame++;
            if ((_TrickFrame & 1) != 0)
            {
                gldepthmin = 0;
                gldepthmax = 0.49999f;
                GL.DepthFunc(DepthFunction.Lequal);
            }
            else
            {
                gldepthmin = 1;
                gldepthmax = 0.5f;
                GL.DepthFunc(DepthFunction.Gequal);
            }
        }
        else
        {
            if (gl_clear.value != 0)
            {
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                // Uze
                Sbar_Changed();
            }
            else
                GL.Clear(ClearBufferMask.DepthBufferBit);

            gldepthmin = 0;
            gldepthmax = 1;
            GL.DepthFunc(DepthFunction.Lequal);
        }

        GL.DepthRange(gldepthmin, gldepthmax);
    }
    public static void R_RemoveEfrags(entity_t ent)
    {
        efrag_t ef = ent.efrag;

        while (ef != null)
        {
            mleaf_t leaf = ef.leaf;
            while (true)
            {
                efrag_t walk = leaf.efrags;
                if (walk == null)
                    break;
                if (walk == ef)
                {
                    // remove this fragment
                    leaf.efrags = ef.leafnext;
                    break;
                }
                else
                    leaf = (mleaf_t)(object)walk.leafnext;
            }

            efrag_t old = ef;
            ef = ef.entnext;

            // put it on the free list
            old.entnext = cl.free_efrags;
            cl.free_efrags = old;
        }

        ent.efrag = null;
    }
    public static void R_TimeRefresh_f()
    {
        //GL.DrawBuffer(DrawBufferMode.Front);
        GL.Finish();

        double start = Sys_FloatTime();
        for (int i = 0; i < 128; i++)
        {
            r_refdef.viewangles.Y = (float)(i / 128.0 * 360.0);
            R_RenderView();
            MainForm.Instance.SwapBuffers();
        }

        GL.Finish();
        double stop = Sys_FloatTime();
        double time = stop - start;
        Con_Printf("{0:F} seconds ({1:F1} fps)\n", time, 128 / time);

        //GL.DrawBuffer(DrawBufferMode.Back);
        GL_EndRendering();
    }
    public static void R_TranslatePlayerSkin(int playernum)
    {
        GL_DisableMultitexture();

        int top = cl.scores[playernum].colors & 0xf0;
        int bottom = (cl.scores[playernum].colors & 15) << 4;

        byte[] translate = new byte[256];
        for (int i = 0; i < 256; i++)
            translate[i] = (byte)i;

        for (int i = 0; i < 16; i++)
        {
            if (top < 128)	// the artists made some backwards ranges.  sigh.
                translate[TOP_RANGE + i] = (byte)(top + i);
            else
                translate[TOP_RANGE + i] = (byte)(top + 15 - i);

            if (bottom < 128)
                translate[BOTTOM_RANGE + i] = (byte)(bottom + i);
            else
                translate[BOTTOM_RANGE + i] = (byte)(bottom + 15 - i);
        }

        //
        // locate the original skin pixels
        //
        currententity = cl_entities[1 + playernum];
        model_t model = currententity.model;
        if (model == null)
            return;		// player doesn't have a model yet
        if (model.type != modtype_t.mod_alias)
            return; // only translate skins on alias models

        aliashdr_t paliashdr = Mod_Extradata(model);
        int s = paliashdr.skinwidth * paliashdr.skinheight;
        if ((s & 3) != 0)
            Sys_Error("R_TranslateSkin: s&3");

        byte[] original;
        if (currententity.skinnum < 0 || currententity.skinnum >= paliashdr.numskins)
        {
            Con_Printf("({0}): Invalid player skin #{1}\n", playernum, currententity.skinnum);
            original = (byte[])paliashdr.texels[0];// (byte *)paliashdr + paliashdr.texels[0];
        }
        else
            original = (byte[])paliashdr.texels[currententity.skinnum];

        int inwidth = paliashdr.skinwidth;
        int inheight = paliashdr.skinheight;

        // because this happens during gameplay, do it fast
        // instead of sending it through gl_upload 8
        GL_Bind(playertextures + playernum);

        int scaled_width = (int)(gl_max_size.value < 512 ? gl_max_size.value : 512);
        int scaled_height = (int)(gl_max_size.value < 256 ? gl_max_size.value : 256);

        // allow users to crunch sizes down even more if they want
        scaled_width >>= (int)gl_playermip.value;
        scaled_height >>= (int)gl_playermip.value;

        uint fracstep, frac;
        int destOffset;

        uint[] translate32 = new uint[256];
        for (int i = 0; i < 256; i++)
            translate32[i] = d_8to24table[translate[i]];

        uint[] dest = new uint[512 * 256];
        destOffset = 0;
        fracstep = (uint)(inwidth * 0x10000 / scaled_width);
        for (int i = 0; i < scaled_height; i++, destOffset += scaled_width)
        {
            int srcOffset = inwidth * (i * inheight / scaled_height);
            frac = fracstep >> 1;
            for (int j = 0; j < scaled_width; j += 4)
            {
                dest[destOffset + j] = translate32[original[srcOffset + (frac >> 16)]];
                frac += fracstep;
                dest[destOffset + j + 1] = translate32[original[srcOffset + (frac >> 16)]];
                frac += fracstep;
                dest[destOffset + j + 2] = translate32[original[srcOffset + (frac >> 16)]];
                frac += fracstep;
                dest[destOffset + j + 3] = translate32[original[srcOffset + (frac >> 16)]];
                frac += fracstep;
            }
        }
        GCHandle handle = GCHandle.Alloc(dest, GCHandleType.Pinned);
        try
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, gl_solid_format, scaled_width, scaled_height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
        SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);
    }    
    public static void GL_DisableMultitexture() 
    {
        if (mtexenabled)
        {
            GL.Disable(EnableCap.Texture2D);
            GL_SelectTexture(MTexTarget.TEXTURE0_SGIS);
            mtexenabled = false;
        }
    }
    public static void GL_EnableMultitexture() 
    {
        if (gl_mtexable)
        {
            GL_SelectTexture(MTexTarget.TEXTURE1_SGIS);
            GL.Enable(EnableCap.Texture2D);
            mtexenabled = true;
        }
    }
    public static void R_NewMap()
    {
        for (int i = 0; i < 256; i++)
            d_lightstylevalue[i] = 264;		// normal light value

        r_worldentity.Clear();
        r_worldentity.model = cl.worldmodel;

        // clear out efrags in case the level hasn't been reloaded
        // FIXME: is this one short?
        for (int i = 0; i < cl.worldmodel.numleafs; i++)
            cl.worldmodel.leafs[i].efrags = null;

        r_viewleaf = null;
        R_ClearParticles();

        GL_BuildLightmaps();

        // identify sky texture
        skytexturenum = -1;
        mirrortexturenum = -1;
        model_t world = cl.worldmodel;
        for (int i = 0; i < world.numtextures; i++)
        {
            if (world.textures[i] == null)
                continue;
            if (world.textures[i].name != null)
            {
                if (world.textures[i].name.StartsWith("sky"))
                    skytexturenum = i;
                if (world.textures[i].name.StartsWith("window02_1"))
                    mirrortexturenum = i;
            }
            world.textures[i].texturechain = null;
        }
    }
    public static bool R_CullBox(ref Vector3 mins, ref Vector3 maxs)
    {
        for (int i = 0; i < 4; i++)
        {
            if (Mathlib.BoxOnPlaneSide(ref mins, ref maxs, frustum[i]) == 2)
                return true;
        }
        return false;
    }
}    