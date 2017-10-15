using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;
using OpenTK;

public static partial class game_engine
{
    public static cvar_t gl_subdivide_size;
    public static byte[] mod_novis = new byte[q_shared.MAX_MAP_LEAFS / 8];

    public static model_t[] mod_known = new model_t[q_shared.MAX_MOD_KNOWN];
    public static int mod_numknown;

    public static model_t loadmodel;
    public static aliashdr_t pheader;

    public static stvert_t[] stverts = new stvert_t[q_shared.MAXALIASVERTS];
    public static dtriangle_t[] triangles = new dtriangle_t[q_shared.MAXALIASTRIS];
    public static int posenum;
    public static byte[] mod_base;
    public static trivertx_t[][] poseverts = new trivertx_t[q_shared.MAXALIASFRAMES][];
    public static byte[] decompressed = new byte[q_shared.MAX_MAP_LEAFS/8];
    
    
    public static void Mod_Init()
    {
        gl_subdivide_size = new cvar_t("gl_subdivide_size", "128", true);
            
        for (int i = 0; i < mod_known.Length; i++)
            mod_known[i] = new model_t();
                
        FillArray(mod_novis, (byte)0xff);
    }
    public static void Mod_ClearAll()
    {
        for (int i = 0; i < mod_numknown; i++)
        {
            model_t mod = mod_known[i];

            if (mod.type != modtype_t.mod_alias)
                mod.needload = true;
        }
    }
    public static model_t Mod_ForName(string name, bool crash)
    {
        model_t mod = Mod_FindName(name);

        return Mod_LoadModel(mod, crash);
    }
    public static aliashdr_t Mod_Extradata(model_t mod)
    {
        object r = Cache_Check(mod.cache);
        if (r != null)
            return (aliashdr_t)r;

        Mod_LoadModel(mod, true);

        if (mod.cache.data == null)
            Sys_Error("Mod_Extradata: caching failed");
        return (aliashdr_t)mod.cache.data;
    }
    public static void Mod_TouchModel(string name)
    {
        model_t mod = Mod_FindName(name);

        if (!mod.needload)
        {
            if (mod.type == modtype_t.mod_alias)
                Cache_Check(mod.cache);
        }
    }
    public static mleaf_t Mod_PointInLeaf(ref Vector3 p, model_t model)
    {
        if (model == null || model.nodes == null)
            Sys_Error("Mod_PointInLeaf: bad model");

        mleaf_t result = null;
        mnodebase_t node = model.nodes[0];
        while (true)
        {
            if (node.contents < 0)
            {
                result = (mleaf_t)node;
                break;
            }

            mnode_t n = (mnode_t)node;
            mplane_t plane = n.plane;
            float d = Vector3.Dot(p, plane.normal) - plane.dist;
            if (d > 0)
                node = n.children[0];
            else
                node = n.children[1];
        }

        return result;
    }
    public static byte[] Mod_LeafPVS(mleaf_t leaf, model_t model)
    {
        if (leaf == model.leafs[0])
            return mod_novis;
            
        return Mod_DecompressVis(leaf.compressed_vis, leaf.visofs, model);
    }
    public static byte[] Mod_DecompressVis(byte[] p, int startIndex, model_t model)
    {
        int row = (model.numleafs + 7) >> 3;
        int offset = 0;

        if (p == null)
        {
            // no vis info, so make all visible
            while (row != 0)
            {
                decompressed[offset++] = 0xff;
                row--;
            }
            return decompressed;
        }
        int srcOffset = startIndex;
        do
        {
            if (p[srcOffset] != 0)// (*in)
            {
                decompressed[offset++] = p[srcOffset++]; //  *out++ = *in++;
                continue;
            }

            int c = p[srcOffset + 1];// in[1];
            srcOffset += 2; // in += 2;
            while (c != 0)
            {
                decompressed[offset++] = 0; // *out++ = 0;
                c--;
            }
        } while (offset < row); // out - decompressed < row

        return decompressed;
    }
    public static void Mod_Print()
    {
        Con_Printf("Cached models:\n");
        for (int i = 0; i < mod_numknown; i++)
        {
            model_t mod = mod_known[i];
            Con_Printf("{0}\n", mod.name);
        }
    }
    public static model_t Mod_FindName(string name)
    {
        if (String.IsNullOrEmpty(name))
            Sys_Error("Mod_ForName: NULL name");

        //
        // search the currently loaded models
        //
        int i = 0;
        model_t mod;
        for (i = 0, mod = mod_known[0]; i < mod_numknown; i++, mod = mod_known[i])
        {
            if (mod.name == name)
                break;
        }

        if (i == mod_numknown)
        {
            if (mod_numknown == q_shared.MAX_MOD_KNOWN)
                Sys_Error("mod_numknown == MAX_MOD_KNOWN");
            mod.name = name;
            mod.needload = true;
            mod_numknown++;
        }

        return mod;
    }    
    public static model_t Mod_LoadModel(model_t mod, bool crash)
    {
        if (!mod.needload)
        {
            if (mod.type == modtype_t.mod_alias)
            {
                if (Cache_Check(mod.cache) != null)
                    return mod;
            }
            else
                return mod;		// not cached at all
        }

        //
        // load the file
        //
        byte[] buf = COM_LoadFile(mod.name);
        if (buf == null)
        {
            if (crash)
                Sys_Error("Mod_NumForName: {0} not found", mod.name);
            return null;
        }

        //
        // allocate a new model
        //
        loadmodel = mod;

        //
        // fill it in
        //

        // call the apropriate loader
        mod.needload = false;

        switch (BitConverter.ToUInt32(buf, 0))// LittleLong(*(unsigned *)buf))
        {
            case q_shared.IDPOLYHEADER:
                Mod_LoadAliasModel(mod, buf);
                break;

            case q_shared.IDSPRITEHEADER:
                Mod_LoadSpriteModel(mod, buf);
                break;

            default:
                Mod_LoadBrushModel(mod, buf);
                break;
        }

        return mod;
    }    
    public static void Mod_LoadAliasModel(model_t mod, byte[] buffer)
    {
        mdl_t pinmodel = BytesToStructure<mdl_t>(buffer, 0);

        int version = LittleLong(pinmodel.version);
        if (version != q_shared.ALIAS_VERSION)
            Sys_Error("{0} has wrong version number ({1} should be {2})",
                mod.name, version, q_shared.ALIAS_VERSION);

        //
        // allocate space for a working header, plus all the data except the frames,
        // skin and group info
        //
        pheader = new aliashdr_t();

        mod.flags = LittleLong(pinmodel.flags);

        //
        // endian-adjust and copy the data, starting with the alias model header
        //
        pheader.boundingradius = LittleFloat(pinmodel.boundingradius);
        pheader.numskins = LittleLong(pinmodel.numskins);
        pheader.skinwidth = LittleLong(pinmodel.skinwidth);
        pheader.skinheight = LittleLong(pinmodel.skinheight);

        if (pheader.skinheight > q_shared.MAX_LBM_HEIGHT)
            Sys_Error("model {0} has a skin taller than {1}", mod.name, q_shared.MAX_LBM_HEIGHT);

        pheader.numverts = LittleLong(pinmodel.numverts);

        if (pheader.numverts <= 0)
            Sys_Error("model {0} has no vertices", mod.name);

        if (pheader.numverts > q_shared.MAXALIASVERTS)
            Sys_Error("model {0} has too many vertices", mod.name);

        pheader.numtris = LittleLong(pinmodel.numtris);

        if (pheader.numtris <= 0)
            Sys_Error("model {0} has no triangles", mod.name);

        pheader.numframes = LittleLong(pinmodel.numframes);
        int numframes = pheader.numframes;
        if (numframes < 1)
            Sys_Error("Mod_LoadAliasModel: Invalid # of frames: {0}\n", numframes);

        pheader.size = LittleFloat(pinmodel.size) * q_shared.ALIAS_BASE_SIZE_RATIO;
        mod.synctype = (synctype_t)LittleLong((int)pinmodel.synctype);
        mod.numframes = pheader.numframes;

        pheader.scale = LittleVector(ToVector(ref pinmodel.scale));
        pheader.scale_origin = LittleVector(ToVector(ref pinmodel.scale_origin));
        pheader.eyeposition = LittleVector(ToVector(ref pinmodel.eyeposition));

        //
        // load the skins
        //
        int offset = Mod_LoadAllSkins(pheader.numskins, new ByteArraySegment(buffer, mdl_t.SizeInBytes));

        //
        // load base s and t vertices
        //
        int stvOffset = offset; // in bytes
        for (int i = 0; i < pheader.numverts; i++, offset += stvert_t.SizeInBytes)
        {
            stverts[i] = BytesToStructure<stvert_t>(buffer, offset);

            stverts[i].onseam = LittleLong(stverts[i].onseam);
            stverts[i].s = LittleLong(stverts[i].s);
            stverts[i].t = LittleLong(stverts[i].t);
        }

        //
        // load triangle lists
        //
        int triOffset = stvOffset + pheader.numverts * stvert_t.SizeInBytes;
        offset = triOffset;
        for (int i = 0; i < pheader.numtris; i++, offset += dtriangle_t.SizeInBytes)
        {
            triangles[i] = BytesToStructure<dtriangle_t>(buffer, offset);
            triangles[i].facesfront = LittleLong(triangles[i].facesfront);

            for (int j = 0; j < 3; j++)
                triangles[i].vertindex[j] = LittleLong(triangles[i].vertindex[j]);
        }

        //
        // load the frames
        //
        posenum = 0;
        int framesOffset = triOffset + pheader.numtris * dtriangle_t.SizeInBytes;

        pheader.frames = new maliasframedesc_t[pheader.numframes];

        for (int i = 0; i < numframes; i++)
        {
            aliasframetype_t frametype = (aliasframetype_t)BitConverter.ToInt32(buffer, framesOffset);
            framesOffset += 4;

            if (frametype == aliasframetype_t.ALIAS_SINGLE)
            {
                framesOffset = Mod_LoadAliasFrame(new ByteArraySegment(buffer, framesOffset), ref pheader.frames[i]);
            }
            else
            {
                framesOffset = Mod_LoadAliasGroup(new ByteArraySegment(buffer, framesOffset), ref pheader.frames[i]);
            }
        }

        pheader.numposes = posenum;

        mod.type = modtype_t.mod_alias;

        // FIXME: do this right
        mod.mins = -Vector3.One * 16.0f;
        mod.maxs = -mod.mins;

        //
        // build the draw lists
        //
        GL_MakeAliasModelDisplayLists(mod, pheader);

        //
        // move the complete, relocatable alias model to the cache
        //	
        mod.cache = Cache_Alloc(aliashdr_t.SizeInBytes * pheader.frames.Length * maliasframedesc_t.SizeInBytes, null);
        if (mod.cache == null)
            return;
        mod.cache.data = pheader;
    }    
    public static void Mod_LoadSpriteModel(model_t mod, byte[] buffer)
    {
        dsprite_t pin = BytesToStructure<dsprite_t>(buffer, 0);

        int version = LittleLong(pin.version);
        if (version != q_shared.SPRITE_VERSION)
            Sys_Error("{0} has wrong version number ({1} should be {2})",
                mod.name, version, q_shared.SPRITE_VERSION);

        int numframes = LittleLong(pin.numframes);

        msprite_t psprite = new msprite_t();

        // Uze: sprite models are not cached so
        mod.cache = new cache_user_t();
        mod.cache.data = psprite;

        psprite.type = LittleLong(pin.type);
        psprite.maxwidth = LittleLong(pin.width);
        psprite.maxheight = LittleLong(pin.height);
        psprite.beamlength = LittleFloat(pin.beamlength);
        mod.synctype = (synctype_t)LittleLong((int)pin.synctype);
        psprite.numframes = numframes;

        mod.mins.X = mod.mins.Y = -psprite.maxwidth / 2;
        mod.maxs.X = mod.maxs.Y = psprite.maxwidth / 2;
        mod.mins.Z = -psprite.maxheight / 2;
        mod.maxs.Z = psprite.maxheight / 2;

        //
        // load the frames
        //
        if (numframes < 1)
            Sys_Error("Mod_LoadSpriteModel: Invalid # of frames: {0}\n", numframes);

        mod.numframes = numframes;

        int frameOffset = dsprite_t.SizeInBytes;

        psprite.frames = new mspriteframedesc_t[numframes];

        for (int i = 0; i < numframes; i++)
        {
            spriteframetype_t frametype = (spriteframetype_t)BitConverter.ToInt32(buffer, frameOffset);
            frameOffset += 4;

            psprite.frames[i].type = frametype;

            if (frametype == spriteframetype_t.SPR_SINGLE)
            {
                frameOffset = LoadSpriteFrame(new ByteArraySegment(buffer, frameOffset), out psprite.frames[i].frameptr, i);
            }
            else
            {
                frameOffset = Mod_LoadSpriteGroup(new ByteArraySegment(buffer, frameOffset), out psprite.frames[i].frameptr, i);
            }
        }

        mod.type = modtype_t.mod_sprite;
    }    
    public static void Mod_LoadBrushModel(model_t mod, byte[] buffer)
    {
        mod.type = modtype_t.mod_brush;

        dheader_t header = BytesToStructure<dheader_t>(buffer, 0);

        int i = LittleLong(header.version);
        if (i != q_shared.BSPVERSION)
            Sys_Error("Mod_LoadBrushModel: {0} has wrong version number ({1} should be {2})", mod.name, i, q_shared.BSPVERSION);

        header.version = i;

        // swap all the lumps
        mod_base = buffer;

        for (i = 0; i < header.lumps.Length; i++)
        {
            header.lumps[i].filelen = LittleLong(header.lumps[i].filelen);
            header.lumps[i].fileofs = LittleLong(header.lumps[i].fileofs);
        }

        // load into heap

        Mod_LoadVertexes(ref header.lumps[q_shared.LUMP_VERTEXES]);
        Mod_LoadEdges(ref header.lumps[q_shared.LUMP_EDGES]);
        Mod_LoadSurfedges(ref header.lumps[q_shared.LUMP_SURFEDGES]);
        Mod_LoadTextures(ref header.lumps[q_shared.LUMP_TEXTURES]);
        Mod_LoadLighting(ref header.lumps[q_shared.LUMP_LIGHTING]);
        Mod_LoadPlanes(ref header.lumps[q_shared.LUMP_PLANES]);
        Mod_LoadTexinfo(ref header.lumps[q_shared.LUMP_TEXINFO]);
        Mod_LoadFaces(ref header.lumps[q_shared.LUMP_FACES]);
        Mod_LoadMarksurfaces(ref header.lumps[q_shared.LUMP_MARKSURFACES]);
        Mod_LoadVisibility(ref header.lumps[q_shared.LUMP_VISIBILITY]);
        Mod_LoadLeafs(ref header.lumps[q_shared.LUMP_LEAFS]);
        Mod_LoadNodes(ref header.lumps[q_shared.LUMP_NODES]);
        Mod_LoadClipnodes(ref header.lumps[q_shared.LUMP_CLIPNODES]);
        Mod_LoadEntities(ref header.lumps[q_shared.LUMP_ENTITIES]);
        Mod_LoadSubmodels(ref header.lumps[q_shared.LUMP_MODELS]);

        Mod_MakeHull0();

        mod.numframes = 2;	// regular and alternate animation

        //
        // set up the submodels (FIXME: this is confusing)
        //
        for (i = 0; i < mod.numsubmodels; i++)
        {
            SetupSubModel(mod, ref mod.submodels[i]);

            if (i < mod.numsubmodels - 1)
            {
                // duplicate the basic information
                string name = "*" + (i + 1).ToString();
                loadmodel = Mod_FindName(name);
                loadmodel.CopyFrom(mod); // *loadmodel = *mod;
                loadmodel.name = name; //strcpy (loadmodel->name, name);
                mod = loadmodel; //mod = loadmodel;
            }
        }
    }
    public static void SetupSubModel(model_t mod, ref dmodel_t submodel)
    {
        mod.hulls[0].firstclipnode = submodel.headnode[0];
        for (int j = 1; j < q_shared.MAX_MAP_HULLS; j++)
        {
            mod.hulls[j].firstclipnode = submodel.headnode[j];
            mod.hulls[j].lastclipnode = mod.numclipnodes - 1;
        }
        mod.firstmodelsurface = submodel.firstface;
        mod.nummodelsurfaces = submodel.numfaces;
        Copy(submodel.maxs, out mod.maxs); // mod.maxs = submodel.maxs;
        Copy(submodel.mins, out mod.mins); // mod.mins = submodel.mins;
        mod.radius = RadiusFromBounds(ref mod.mins, ref mod.maxs);
        mod.numleafs = submodel.visleafs;
    }
    public static int Mod_LoadAllSkins(int numskins, ByteArraySegment data)
    {
        if (numskins < 1 || numskins > q_shared.MAX_SKINS)
            Sys_Error("Mod_LoadAliasModel: Invalid # of skins: {0}\n", numskins);

        int offset = data.StartIndex;
        int skinOffset = data.StartIndex + daliasskintype_t.SizeInBytes; //  skin = (byte*)(pskintype + 1);
        int s = pheader.skinwidth * pheader.skinheight;

        daliasskintype_t pskintype = BytesToStructure<daliasskintype_t>(data.Data, offset);

        for (int i = 0; i < numskins; i++)
        {
            if (pskintype.type == aliasskintype_t.ALIAS_SKIN_SINGLE)
            {
                Mod_FloodFillSkin(new ByteArraySegment(data.Data, skinOffset), pheader.skinwidth, pheader.skinheight);

                // save 8 bit texels for the player model to remap
                byte[] texels = new byte[s]; // Hunk_AllocName(s, loadname);
                pheader.texels[i] = texels;// -(byte*)pheader;
                Buffer.BlockCopy(data.Data, offset + daliasskintype_t.SizeInBytes, texels, 0, s);

                // set offset to pixel data after daliasskintype_t block...
                offset += daliasskintype_t.SizeInBytes;

                string name = loadmodel.name + "_" + i.ToString();
                pheader.gl_texturenum[i, 0] =
                pheader.gl_texturenum[i, 1] =
                pheader.gl_texturenum[i, 2] =
                pheader.gl_texturenum[i, 3] =
                    GL_LoadTexture(name, pheader.skinwidth,
                    pheader.skinheight, new ByteArraySegment(data.Data, offset), true, false); // (byte*)(pskintype + 1)

                // set offset to next daliasskintype_t block...
                offset += s;
                pskintype = BytesToStructure<daliasskintype_t>(data.Data, offset);
            }
            else
            {
                // animating skin group.  yuck.
                offset += daliasskintype_t.SizeInBytes;
                daliasskingroup_t pinskingroup = BytesToStructure<daliasskingroup_t>(data.Data, offset);
                int groupskins = LittleLong(pinskingroup.numskins);
                offset += daliasskingroup_t.SizeInBytes;
                daliasskininterval_t pinskinintervals = BytesToStructure<daliasskininterval_t>(data.Data, offset);

                offset += daliasskininterval_t.SizeInBytes * groupskins;

                pskintype = BytesToStructure<daliasskintype_t>(data.Data, offset);
                int j;
                for (j = 0; j < groupskins; j++)
                {
                    Mod_FloodFillSkin(new ByteArraySegment(data.Data, skinOffset), pheader.skinwidth, pheader.skinheight);
                    if (j == 0)
                    {
                        byte[] texels = new byte[s]; // Hunk_AllocName(s, loadname);
                        pheader.texels[i] = texels;// -(byte*)pheader;
                        Buffer.BlockCopy(data.Data, offset, texels, 0, s);
                    }

                    string name = String.Format("{0}_{1}_{2}", loadmodel.name, i, j);
                    pheader.gl_texturenum[i, j & 3] =
                        GL_LoadTexture(name, pheader.skinwidth,
                        pheader.skinheight, new ByteArraySegment(data.Data, offset), true, false); //  (byte*)(pskintype)

                    offset += s;

                    pskintype = BytesToStructure<daliasskintype_t>(data.Data, offset);
                }
                int k = j;
                for (; j < 4; j++)
                    pheader.gl_texturenum[i, j & 3] = pheader.gl_texturenum[i, j - k];
            }
        }

        return offset;// (void*)pskintype;
    }
    public static int Mod_LoadAliasFrame (ByteArraySegment pin, ref maliasframedesc_t frame)
    {
        daliasframe_t pdaliasframe = BytesToStructure<daliasframe_t>(pin.Data, pin.StartIndex);

        frame.name = GetString(pdaliasframe.name);
        frame.firstpose = posenum;
        frame.numposes = 1;
        frame.bboxmin.Init();
        frame.bboxmax.Init();

        for (int i = 0; i < 3; i++)
        {
            // these are byte values, so we don't have to worry about
            // endianness
            frame.bboxmin.v[i] = pdaliasframe.bboxmin.v[i];
            frame.bboxmax.v[i] = pdaliasframe.bboxmax.v[i];
        }

        trivertx_t[] verts = new trivertx_t[pheader.numverts];
        int offset = pin.StartIndex + daliasframe_t.SizeInBytes; //pinframe = (trivertx_t*)(pdaliasframe + 1);
        for (int i = 0; i < verts.Length; i++, offset += trivertx_t.SizeInBytes)
        {
            verts[i] = BytesToStructure<trivertx_t>(pin.Data, offset);
        }
        poseverts[posenum] = verts;
        posenum++;

        return offset;
    }
    public static int Mod_LoadAliasGroup(ByteArraySegment pin, ref maliasframedesc_t frame)
    {
        int offset = pin.StartIndex;
        daliasgroup_t pingroup = BytesToStructure<daliasgroup_t>(pin.Data, offset);
        int numframes = LittleLong(pingroup.numframes);

        frame.Init();
        frame.firstpose = posenum;
        frame.numposes = numframes;

        for (int i = 0; i < 3; i++)
        {
            // these are byte values, so we don't have to worry about endianness
            frame.bboxmin.v[i] = pingroup.bboxmin.v[i];
            frame.bboxmin.v[i] = pingroup.bboxmax.v[i];
        }

        offset += daliasgroup_t.SizeInBytes;
        daliasinterval_t pin_intervals = BytesToStructure<daliasinterval_t>(pin.Data, offset); // (daliasinterval_t*)(pingroup + 1);

        frame.interval = LittleFloat(pin_intervals.interval);

        offset += numframes * daliasinterval_t.SizeInBytes;

        for (int i = 0; i < numframes; i++)
        {
            trivertx_t[] tris = new trivertx_t[pheader.numverts];
            int offset1 = offset + daliasframe_t.SizeInBytes;
            for (int j = 0; j < pheader.numverts; j++, offset1 += trivertx_t.SizeInBytes)
            {
                tris[j] = BytesToStructure<trivertx_t>(pin.Data, offset1);
            }
            poseverts[posenum] = tris;
            posenum++;

            offset += daliasframe_t.SizeInBytes + pheader.numverts * trivertx_t.SizeInBytes;
        }

        return offset;
    }
    public static int LoadSpriteFrame(ByteArraySegment pin, out object ppframe, int framenum)
    {
        dspriteframe_t pinframe = BytesToStructure<dspriteframe_t>(pin.Data, pin.StartIndex);

        int width = LittleLong(pinframe.width);
        int height = LittleLong(pinframe.height);
        int size = width * height;

        mspriteframe_t pspriteframe = new mspriteframe_t();

        ppframe = pspriteframe;

        pspriteframe.width = width;
        pspriteframe.height = height;
        int orgx = LittleLong(pinframe.origin[0]);
        int orgy = LittleLong(pinframe.origin[1]);

        pspriteframe.up = orgy;// origin[1];
        pspriteframe.down = orgy - height;
        pspriteframe.left = orgx;// origin[0];
        pspriteframe.right = width + orgx;// origin[0];

        string name = loadmodel.name + "_" + framenum.ToString();
        pspriteframe.gl_texturenum = GL_LoadTexture(name, width, height,
            new ByteArraySegment(pin.Data, pin.StartIndex + dspriteframe_t.SizeInBytes), true, true); //   (byte *)(pinframe + 1)

        return pin.StartIndex + dspriteframe_t.SizeInBytes + size;
    }
    public static int Mod_LoadSpriteGroup(ByteArraySegment pin, out object ppframe, int framenum)
    {
        dspritegroup_t pingroup = BytesToStructure<dspritegroup_t>(pin.Data, pin.StartIndex);

        int numframes = LittleLong(pingroup.numframes);
        mspritegroup_t pspritegroup = new mspritegroup_t();
        pspritegroup.numframes = numframes;
        pspritegroup.frames = new mspriteframe_t[numframes];
        ppframe = pspritegroup;// (mspriteframe_t*)pspritegroup;
        float[] poutintervals = new float[numframes];
        pspritegroup.intervals = poutintervals;

        int offset = pin.StartIndex + dspritegroup_t.SizeInBytes;
        for (int i = 0; i < numframes; i++, offset += dspriteinterval_t.SizeInBytes)
        {
            dspriteinterval_t interval = BytesToStructure<dspriteinterval_t>(pin.Data, offset);
            poutintervals[i] = LittleFloat(interval.interval);
            if (poutintervals[i] <= 0)
                Sys_Error("Mod_LoadSpriteGroup: interval<=0");
        }

        for (int i = 0; i < numframes; i++)
        {
            object tmp;
            offset = LoadSpriteFrame(new ByteArraySegment(pin.Data, offset), out tmp, framenum * 100 + i);
            pspritegroup.frames[i] = (mspriteframe_t)tmp;
        }

        return offset;
    }
    public static void Mod_LoadVertexes(ref lump_t l)
    {
        if ((l.filelen % dvertex_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dvertex_t.SizeInBytes;
        mvertex_t[] verts = new mvertex_t[count];

        loadmodel.vertexes = verts;
        loadmodel.numvertexes = count;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += dvertex_t.SizeInBytes)
        {
            dvertex_t src = BytesToStructure<dvertex_t>(mod_base, offset);
            verts[i].position = LittleVector3(src.point);
        }
    }
    public static void Mod_LoadEdges(ref lump_t l)
    {
        if ((l.filelen % dedge_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dedge_t.SizeInBytes;

        // Uze: Why count + 1 ?????
        medge_t[] edges = new medge_t[count]; // out = Hunk_AllocName ( (count + 1) * sizeof(*out), loadname);	
        loadmodel.edges = edges;
        loadmodel.numedges = count;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += dedge_t.SizeInBytes)
        {
            dedge_t src = BytesToStructure<dedge_t>(mod_base, offset);
            edges[i].v = new ushort[] {
                (ushort)LittleShort((short)src.v[0]),
                (ushort)LittleShort((short)src.v[1])
            };
        }
    }
    public static void Mod_LoadSurfedges(ref lump_t l)
    {
        if ((l.filelen % sizeof(int)) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / sizeof(int);
        int[] edges = new int[count];

        loadmodel.surfedges = edges;
        loadmodel.numsurfedges = count;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += 4)
        {
            int src = BitConverter.ToInt32(mod_base, offset);
            edges[i] = src; // Common.LittleLong(in[i]);
        }
    }
    public static void Mod_LoadTextures(ref lump_t l)
    {
        if (l.filelen == 0)
        {
            loadmodel.textures = null;
            return;
        }

        dmiptexlump_t m = BytesToStructure<dmiptexlump_t>(mod_base, l.fileofs);// (dmiptexlump_t *)(mod_base + l.fileofs);

        m.nummiptex = LittleLong(m.nummiptex);

        int[] dataofs = new int[m.nummiptex];

        Buffer.BlockCopy(mod_base, l.fileofs + dmiptexlump_t.SizeInBytes, dataofs, 0, dataofs.Length * sizeof(int));

        loadmodel.numtextures = m.nummiptex;
        loadmodel.textures = new texture_t[m.nummiptex]; // Hunk_AllocName (m->nummiptex * sizeof(*loadmodel->textures) , loadname);

        for (int i = 0; i < m.nummiptex; i++)
        {
            dataofs[i] = LittleLong(dataofs[i]);
            if (dataofs[i] == -1)
                continue;

            int mtOffset = l.fileofs + dataofs[i];
            miptex_t mt = BytesToStructure<miptex_t>(mod_base, mtOffset); //mt = (miptex_t *)((byte *)m + m.dataofs[i]);
            mt.width = (uint)LittleLong((int)mt.width);
            mt.height = (uint)LittleLong((int)mt.height);
            for (int j = 0; j < q_shared.MIPLEVELS; j++)
                mt.offsets[j] = (uint)LittleLong((int)mt.offsets[j]);

            if ((mt.width & 15) != 0 || (mt.height & 15) != 0)
                Sys_Error("Texture {0} is not 16 aligned", mt.name);

            int pixels = (int)(mt.width * mt.height / 64 * 85);
            texture_t tx = new texture_t();// Hunk_AllocName(sizeof(texture_t) + pixels, loadname);
            loadmodel.textures[i] = tx;

            tx.name = GetString(mt.name);//   memcpy (tx->name, mt->name, sizeof(tx.name));
            tx.width = mt.width;
            tx.height = mt.height;
            for (int j = 0; j < q_shared.MIPLEVELS; j++)
                tx.offsets[j] = (int)mt.offsets[j] - miptex_t.SizeInBytes;
            // the pixels immediately follow the structures
            tx.pixels = new byte[pixels];
            Buffer.BlockCopy(mod_base, mtOffset + miptex_t.SizeInBytes, tx.pixels, 0, pixels);

            if (tx.name != null && tx.name.StartsWith("sky"))// !Q_strncmp(mt->name,"sky",3))
                R_InitSky(tx);
            else
            {
                tx.gl_texturenum = GL_LoadTexture(tx.name, (int)tx.width, (int)tx.height,
                    new ByteArraySegment(tx.pixels), true, false);
            }
        }

        //
        // sequence the animations
        //
        texture_t[] anims = new texture_t[10];
        texture_t[] altanims = new texture_t[10];

        for (int i = 0; i < m.nummiptex; i++)
        {
            texture_t tx = loadmodel.textures[i];
            if (tx == null || !tx.name.StartsWith("+"))// [0] != '+')
                continue;
            if (tx.anim_next != null)
                continue;	// allready sequenced

            // find the number of frames in the animation
            Array.Clear(anims, 0, anims.Length);
            Array.Clear(altanims, 0, altanims.Length);

            int max = tx.name[1];
            int altmax = 0;
            if (max >= 'a' && max <= 'z')
                max -= 'a' - 'A';
            if (max >= '0' && max <= '9')
            {
                max -= '0';
                altmax = 0;
                anims[max] = tx;
                max++;
            }
            else if (max >= 'A' && max <= 'J')
            {
                altmax = max - 'A';
                max = 0;
                altanims[altmax] = tx;
                altmax++;
            }
            else
                Sys_Error("Bad animating texture {0}", tx.name);

            for (int j = i + 1; j < m.nummiptex; j++)
            {
                texture_t tx2 = loadmodel.textures[j];
                if (tx2 == null || !tx2.name.StartsWith("+"))// tx2->name[0] != '+')
                    continue;
                if (String.Compare(tx2.name, 2, tx.name, 2, Math.Min(tx.name.Length, tx2.name.Length)) != 0)// strcmp (tx2->name+2, tx->name+2))
                    continue;

                int num = tx2.name[1];
                if (num >= 'a' && num <= 'z')
                    num -= 'a' - 'A';
                if (num >= '0' && num <= '9')
                {
                    num -= '0';
                    anims[num] = tx2;
                    if (num + 1 > max)
                        max = num + 1;
                }
                else if (num >= 'A' && num <= 'J')
                {
                    num = num - 'A';
                    altanims[num] = tx2;
                    if (num + 1 > altmax)
                        altmax = num + 1;
                }
                else
                    Sys_Error("Bad animating texture {0}", tx2.name);
            }


            // link them all together
            for (int j = 0; j < max; j++)
            {
                texture_t tx2 = anims[j];
                if (tx2 == null)
                    Sys_Error("Missing frame {0} of {1}", j, tx.name);
                tx2.anim_total = max * q_shared.ANIM_CYCLE;
                tx2.anim_min = j * q_shared.ANIM_CYCLE;
                tx2.anim_max = (j + 1) * q_shared.ANIM_CYCLE;
                tx2.anim_next = anims[(j + 1) % max];
                if (altmax != 0)
                    tx2.alternate_anims = altanims[0];
            }
            for (int j = 0; j < altmax; j++)
            {
                texture_t tx2 = altanims[j];
                if (tx2 == null)
                    Sys_Error("Missing frame {0} of {1}", j, tx2.name);
                tx2.anim_total = altmax * q_shared.ANIM_CYCLE;
                tx2.anim_min = j * q_shared.ANIM_CYCLE;
                tx2.anim_max = (j + 1) * q_shared.ANIM_CYCLE;
                tx2.anim_next = altanims[(j + 1) % altmax];
                if (max != 0)
                    tx2.alternate_anims = anims[0];
            }
        }
    }
    public static void Mod_LoadLighting(ref lump_t l)
    {
        if (l.filelen == 0)
        {
            loadmodel.lightdata = null;
            return;
        }
        loadmodel.lightdata = new byte[l.filelen]; // Hunk_AllocName(l->filelen, loadname);
        Buffer.BlockCopy(mod_base, l.fileofs, loadmodel.lightdata, 0, l.filelen);
    }
    public static void Mod_LoadPlanes (ref lump_t l)
    {
        if ((l.filelen % dplane_t.SizeInBytes) != 0)
            Sys_Error ("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);
            
        int count = l.filelen / dplane_t.SizeInBytes;
        // Uze: Possible error! Why in original is out = Hunk_AllocName ( count*2*sizeof(*out), loadname)???
        mplane_t[] planes = new mplane_t[count];

        for (int i = 0; i < planes.Length; i++)
            planes[i] = new mplane_t();
            
        loadmodel.planes = planes;
        loadmodel.numplanes = count;

        for (int  i=0 ; i<count ; i++)
        {
            dplane_t src = BytesToStructure<dplane_t>(mod_base, l.fileofs + i * dplane_t.SizeInBytes);
            int bits = 0;
            planes[i].normal = LittleVector3(src.normal);
            if (planes[i].normal.X < 0)
                bits |= 1;
            if (planes[i].normal.Y < 0)
                bits |= 1 << 1;
            if (planes[i].normal.Z < 0)
                bits |= 1 << 2;
            planes[i].dist = LittleFloat(src.dist);
            planes[i].type = (byte)LittleLong(src.type);
            planes[i].signbits = (byte)bits;
        }
    }
    public static void Mod_LoadTexinfo(ref lump_t l)
    {
        //in = (void *)(mod_base + l->fileofs);
        if ((l.filelen % texinfo_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / texinfo_t.SizeInBytes;
        mtexinfo_t[] infos = new mtexinfo_t[count]; // out = Hunk_AllocName ( count*sizeof(*out), loadname);	

        for (int i = 0; i < infos.Length; i++)
            infos[i] = new mtexinfo_t();

        loadmodel.texinfo = infos;
        loadmodel.numtexinfo = count;

        for (int i = 0; i < count; i++)//, in++, out++)
        {
            texinfo_t src = BytesToStructure<texinfo_t>(mod_base, l.fileofs + i * texinfo_t.SizeInBytes);

            for (int j = 0; j < 2; j++)
                infos[i].vecs[j] = LittleVector4(src.vecs, j * 4);

            float len1 = infos[i].vecs[0].Length;
            float len2 = infos[i].vecs[1].Length;
            len1 = (len1 + len2) / 2;
            if (len1 < 0.32)
                infos[i].mipadjust = 4;
            else if (len1 < 0.49)
                infos[i].mipadjust = 3;
            else if (len1 < 0.99)
                infos[i].mipadjust = 2;
            else
                infos[i].mipadjust = 1;

            int miptex = LittleLong(src.miptex);
            infos[i].flags = LittleLong(src.flags);

            if (loadmodel.textures == null)
            {
                infos[i].texture = r_notexture_mip;	// checkerboard texture
                infos[i].flags = 0;
            }
            else
            {
                if (miptex >= loadmodel.numtextures)
                    Sys_Error("miptex >= loadmodel->numtextures");
                infos[i].texture = loadmodel.textures[miptex];
                if (infos[i].texture == null)
                {
                    infos[i].texture = r_notexture_mip; // texture not found
                    infos[i].flags = 0;
                }
            }
        }
    }
    public static void Mod_LoadFaces(ref lump_t l)
    {
        if ((l.filelen % dface_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dface_t.SizeInBytes;
        msurface_t[] dest = new msurface_t[count];

        for (int i = 0; i < dest.Length; i++)
            dest[i] = new msurface_t();

        loadmodel.surfaces = dest;
        loadmodel.numsurfaces = count;
        int offset = l.fileofs;
        for (int surfnum = 0; surfnum < count; surfnum++, offset += dface_t.SizeInBytes)
        {
            dface_t src = BytesToStructure<dface_t>(mod_base, offset);

            dest[surfnum].firstedge = LittleLong(src.firstedge);
            dest[surfnum].numedges = LittleShort(src.numedges);
            dest[surfnum].flags = 0;

            int planenum = LittleShort(src.planenum);
            int side = LittleShort(src.side);
            if (side != 0)
                dest[surfnum].flags |= q_shared.SURF_PLANEBACK;

            dest[surfnum].plane = loadmodel.planes[planenum];
            dest[surfnum].texinfo = loadmodel.texinfo[LittleShort(src.texinfo)];

            CalcSurfaceExtents(dest[surfnum]);

            // lighting info

            for (int i = 0; i < q_shared.MAXLIGHTMAPS; i++)
                dest[surfnum].styles[i] = src.styles[i];

            int i2 = LittleLong(src.lightofs);
            if (i2 == -1)
                dest[surfnum].sample_base = null;
            else
            {
                dest[surfnum].sample_base = loadmodel.lightdata;
                dest[surfnum].sampleofs = i2;
            }

            // set the drawing flags flag
            if (dest[surfnum].texinfo.texture.name != null)
            {
                if (dest[surfnum].texinfo.texture.name.StartsWith("sky"))	// sky
                {
                    dest[surfnum].flags |= (q_shared.SURF_DRAWSKY | q_shared.SURF_DRAWTILED);
                    GL_SubdivideSurface(dest[surfnum]);	// cut up polygon for warps
                    continue;
                }

                if (dest[surfnum].texinfo.texture.name.StartsWith("*"))		// turbulent
                {
                    dest[surfnum].flags |= (q_shared.SURF_DRAWTURB | q_shared.SURF_DRAWTILED);
                    for (int i = 0; i < 2; i++)
                    {
                        dest[surfnum].extents[i] = 16384;
                        dest[surfnum].texturemins[i] = -8192;
                    }
                    GL_SubdivideSurface(dest[surfnum]);	// cut up polygon for warps
                    continue;
                }
            }
        }
    }
    public static void Mod_LoadMarksurfaces(ref lump_t l)
    {
        if ((l.filelen % sizeof(short)) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / sizeof(short);
        msurface_t[] dest = new msurface_t[count];

        loadmodel.marksurfaces = dest;
        loadmodel.nummarksurfaces = count;

        for (int i = 0; i < count; i++)
        {
            int j = BitConverter.ToInt16(mod_base, l.fileofs + i * sizeof(short));
            if (j >= loadmodel.numsurfaces)
                Sys_Error("Mod_ParseMarksurfaces: bad surface number");
            dest[i] = loadmodel.surfaces[j];
        }
    }
    public static void Mod_LoadVisibility(ref lump_t l)
    {
        if (l.filelen == 0)
        {
            loadmodel.visdata = null;
            return;
        }
        loadmodel.visdata = new byte[l.filelen];
        Buffer.BlockCopy(mod_base, l.fileofs, loadmodel.visdata, 0, l.filelen);
    }
    public static void Mod_LoadLeafs(ref lump_t l)
    {
        if ((l.filelen % dleaf_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dleaf_t.SizeInBytes;
        mleaf_t[] dest = new mleaf_t[count];

        for (int i = 0; i < dest.Length; i++)
            dest[i] = new mleaf_t();
            
        loadmodel.leafs = dest;
        loadmodel.numleafs = count;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += dleaf_t.SizeInBytes)
        {
            dleaf_t src = BytesToStructure<dleaf_t>(mod_base, offset);

            dest[i].mins.X = LittleShort(src.mins[0]);
            dest[i].mins.Y = LittleShort(src.mins[1]);
            dest[i].mins.Z = LittleShort(src.mins[2]);
                
            dest[i].maxs.X = LittleShort(src.maxs[0]);
            dest[i].maxs.Y = LittleShort(src.maxs[1]);
            dest[i].maxs.Z = LittleShort(src.maxs[2]);

            int p = LittleLong(src.contents);
            dest[i].contents = p;

            dest[i].marksurfaces = loadmodel.marksurfaces;
            dest[i].firstmarksurface = LittleShort((short)src.firstmarksurface);
            dest[i].nummarksurfaces = LittleShort((short)src.nummarksurfaces);

            p = LittleLong(src.visofs);
            if (p == -1)
                dest[i].compressed_vis = null;
            else
            {
                dest[i].compressed_vis = loadmodel.visdata; // loadmodel->visdata + p;
                dest[i].visofs = p;
            }
            dest[i].efrags = null;

            for (int j = 0; j < 4; j++)
                dest[i].ambient_sound_level[j] = src.ambient_level[j];

            // gl underwater warp
            // Uze: removed underwater warp as too ugly
            //if (dest[i].contents != Contents.CONTENTS_EMPTY)
            //{
            //    for (int j = 0; j < dest[i].nummarksurfaces; j++)
            //        dest[i].marksurfaces[dest[i].firstmarksurface + j].flags |= Surf.SURF_UNDERWATER;
            //}
        }
    }
    public static void Mod_LoadNodes(ref lump_t l)
    {
        if ((l.filelen % dnode_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dnode_t.SizeInBytes;
        mnode_t[] dest = new mnode_t[count];

        for (int i = 0; i < dest.Length; i++)
            dest[i] = new mnode_t();

        loadmodel.nodes = dest;
        loadmodel.numnodes = count;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += dnode_t.SizeInBytes)
        {
            dnode_t src = BytesToStructure<dnode_t>(mod_base, offset);

            dest[i].mins.X = LittleShort(src.mins[0]);
            dest[i].mins.Y = LittleShort(src.mins[1]);
            dest[i].mins.Z = LittleShort(src.mins[2]);

            dest[i].maxs.X = LittleShort(src.maxs[0]);
            dest[i].maxs.Y = LittleShort(src.maxs[1]);
            dest[i].maxs.Z = LittleShort(src.maxs[2]);

            int p = LittleLong(src.planenum);
            dest[i].plane = loadmodel.planes[p];

            dest[i].firstsurface = (ushort)LittleShort((short)src.firstface);
            dest[i].numsurfaces = (ushort)LittleShort((short)src.numfaces);

            for (int j = 0; j < 2; j++)
            {
                p = LittleShort(src.children[j]);
                if (p >= 0)
                    dest[i].children[j] = loadmodel.nodes[p];
                else
                    dest[i].children[j] = loadmodel.leafs[-1 - p];
            }
        }

        Mod_SetParent(loadmodel.nodes[0], null);	// sets nodes and leafs
    }
    public static void Mod_LoadClipnodes(ref lump_t l)
    {
        if ((l.filelen % dclipnode_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dclipnode_t.SizeInBytes;
        dclipnode_t[] dest = new dclipnode_t[count];

        loadmodel.clipnodes = dest;
        loadmodel.numclipnodes = count;

        hull_t hull = loadmodel.hulls[1];
        hull.clipnodes = dest;
        hull.firstclipnode = 0;
        hull.lastclipnode = count - 1;
        hull.planes = loadmodel.planes;
        hull.clip_mins.X = -16;
        hull.clip_mins.Y = -16;
        hull.clip_mins.Z = -24;
        hull.clip_maxs.X = 16;
        hull.clip_maxs.Y = 16;
        hull.clip_maxs.Z = 32;

        hull = loadmodel.hulls[2];
        hull.clipnodes = dest;
        hull.firstclipnode = 0;
        hull.lastclipnode = count - 1;
        hull.planes = loadmodel.planes;
        hull.clip_mins.X = -32;
        hull.clip_mins.Y = -32;
        hull.clip_mins.Z = -24;
        hull.clip_maxs.X = 32;
        hull.clip_maxs.Y = 32;
        hull.clip_maxs.Z = 64;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += dclipnode_t.SizeInBytes)
        {
            dclipnode_t src = BytesToStructure<dclipnode_t>(mod_base, offset);

            dest[i].planenum = LittleLong(src.planenum); // Uze: changed from LittleShort
            dest[i].children = new short[2];
            dest[i].children[0] = LittleShort(src.children[0]);
            dest[i].children[1] = LittleShort(src.children[1]);
        }
    }
    public static void Mod_LoadEntities(ref lump_t l)
    {
        if (l.filelen == 0)
        {
            loadmodel.entities = null;
            return;
        }
        loadmodel.entities = Encoding.ASCII.GetString(mod_base, l.fileofs, l.filelen);
    }
    public static void Mod_LoadSubmodels(ref lump_t l)
    {
        if ((l.filelen % dmodel_t.SizeInBytes) != 0)
            Sys_Error("MOD_LoadBmodel: funny lump size in {0}", loadmodel.name);

        int count = l.filelen / dmodel_t.SizeInBytes;
        dmodel_t[] dest = new dmodel_t[count];

        loadmodel.submodels = dest;
        loadmodel.numsubmodels = count;

        for (int i = 0, offset = l.fileofs; i < count; i++, offset += dmodel_t.SizeInBytes)
        {
            dmodel_t src = BytesToStructure<dmodel_t>(mod_base, offset);

            dest[i].mins = new float[3];
            dest[i].maxs = new float[3];
            dest[i].origin = new float[3];

            for (int j = 0; j < 3; j++)
            {
                // spread the mins / maxs by a pixel
                dest[i].mins[j] = LittleFloat(src.mins[j]) - 1;
                dest[i].maxs[j] = LittleFloat(src.maxs[j]) + 1;
                dest[i].origin[j] = LittleFloat(src.origin[j]);
            }

            dest[i].headnode = new int[q_shared.MAX_MAP_HULLS];
            for (int j = 0; j < q_shared.MAX_MAP_HULLS; j++)
                dest[i].headnode[j] = LittleLong(src.headnode[j]);
                
            dest[i].visleafs = LittleLong(src.visleafs);
            dest[i].firstface = LittleLong(src.firstface);
            dest[i].numfaces = LittleLong(src.numfaces);
        }
    }
    public static void Mod_MakeHull0()
    {
        hull_t hull = loadmodel.hulls[0];
        mnode_t[] src = loadmodel.nodes;
        int count = loadmodel.numnodes;
        dclipnode_t[] dest = new dclipnode_t[count];

        hull.clipnodes = dest;
        hull.firstclipnode = 0;
        hull.lastclipnode = count - 1;
        hull.planes = loadmodel.planes;

        for (int i = 0; i < count; i++)
        {
            dest[i].planenum = Array.IndexOf(loadmodel.planes, src[i].plane); // todo: optimize this
            dest[i].children = new short[2];
            for (int j = 0; j < 2; j++)
            {
                mnodebase_t child = src[i].children[j];
                if (child.contents < 0)
                    dest[i].children[j] = (short)child.contents;
                else
                    dest[i].children[j] = (short)Array.IndexOf(loadmodel.nodes, (mnode_t)child); // todo: optimize this
            }
        }
    }
    public static float RadiusFromBounds(ref Vector3 mins, ref Vector3 maxs)
    {
        Vector3 corner;

        corner.X = Math.Max(Math.Abs(mins.X), Math.Abs(maxs.X));
        corner.Y = Math.Max(Math.Abs(mins.Y), Math.Abs(maxs.Y));
        corner.Z = Math.Max(Math.Abs(mins.Z), Math.Abs(maxs.Z));
            
        return corner.Length;
    }
    public static void CalcSurfaceExtents(msurface_t s)
    {
        float[] mins = new float[] { 999999, 999999 };
        float[] maxs = new float[] { -99999, -99999 };

        mtexinfo_t tex = s.texinfo;
        mvertex_t[] v = loadmodel.vertexes;

        for (int i = 0; i < s.numedges; i++)
        {
            int idx;
            int e = loadmodel.surfedges[s.firstedge + i];
            if (e >= 0)
                idx = loadmodel.edges[e].v[0];
            else
                idx = loadmodel.edges[-e].v[1];

            for (int j = 0; j < 2; j++)
            {
                float val = v[idx].position.X * tex.vecs[j].X +
                    v[idx].position.Y * tex.vecs[j].Y +
                    v[idx].position.Z * tex.vecs[j].Z +
                    tex.vecs[j].W;
                if (val < mins[j])
                    mins[j] = val;
                if (val > maxs[j])
                    maxs[j] = val;
            }
        }

        int[] bmins = new int[2];
        int[] bmaxs = new int[2];
        for (int i = 0; i < 2; i++)
        {
            bmins[i] = (int)Math.Floor(mins[i] / 16);
            bmaxs[i] = (int)Math.Ceiling(maxs[i] / 16);

            s.texturemins[i] = (short)(bmins[i] * 16);
            s.extents[i] = (short)((bmaxs[i] - bmins[i]) * 16);
            if ((tex.flags & q_shared.TEX_SPECIAL) == 0 && s.extents[i] > 512)
                Sys_Error("Bad surface extents");
        }
    }
    public static void Mod_SetParent(mnodebase_t node, mnode_t parent)
    {
        node.parent = parent;
        if (node.contents < 0)
            return;

        mnode_t n = (mnode_t)node;
        Mod_SetParent(n.children[0], n);
        Mod_SetParent(n.children[1], n);
    }
    public static void Mod_FloodFillSkin(ByteArraySegment skin, int skinwidth, int skinheight)
    {
        FloodFiller filler = new FloodFiller(skin, skinwidth, skinheight);
        filler.Perform();
    }
}