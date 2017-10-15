using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static partial class game_engine
{
    static model_t aliasmodel;
    static aliashdr_t paliashdr;

    static byte[] _Used = new byte[q_shared.MAX_COMMANDS];
    static int[] commands = new int[q_shared.MAX_COMMANDS];
    static int numcommands;
    static int[] vertexorder = new int[q_shared.MAX_COMMANDS];
    static int numorder;

    static int allverts;
    static int alltris;

    static int[] stripverts = new int[q_shared.MAX_STRIP];
    static int[] striptris = new int[q_shared.MAX_STRIP];
    static int stripcount;

    public static void GL_MakeAliasModelDisplayLists(model_t m, aliashdr_t hdr)
    {
        aliasmodel = m;
        paliashdr = hdr;

        //
        // look for a cached version
        //
        string path = Path.ChangeExtension("glquake/" + Path.GetFileNameWithoutExtension(m.name), ".ms2");

        DisposableWrapper<BinaryReader> file;
        COM_FOpenFile(path, out file);
        if (file != null)
        {
            using (file)
            {
                BinaryReader reader = file.Object;
                numcommands = reader.ReadInt32();
                numorder = reader.ReadInt32();
                for (int i = 0; i < numcommands; i++)
                    commands[i] = reader.ReadInt32();
                for (int i = 0; i < numorder; i++)
                    vertexorder[i] = reader.ReadInt32();
            }
        }
        else
        {
            //
            // build it from scratch
            //
            Con_Printf("meshing {0}...\n", m.name);

            BuildTris();		// trifans or lists

            //
            // save out the cached version
            //
            string fullpath = Path.Combine(com_gamedir, path);
            Stream fs = Sys_FileOpenWrite(fullpath, true);
            if (fs != null)
                using (BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII))
                {
                    writer.Write(numcommands);
                    writer.Write(numorder);
                    for (int i = 0; i < numcommands; i++)
                        writer.Write(commands[i]);
                    for (int i = 0; i < numorder; i++)
                        writer.Write(vertexorder[i]);
                }
        }

        //
        // save the data out
        //
        paliashdr.poseverts = numorder;

        int[] cmds = new int[numcommands]; //Hunk_Alloc (numcommands * 4);
        paliashdr.commands = cmds; // in bytes??? // (byte*)cmds - (byte*)paliashdr;
        Buffer.BlockCopy(commands, 0, cmds, 0, numcommands * 4); //memcpy (cmds, commands, numcommands * 4);
        
        trivertx_t[] verts = new trivertx_t[paliashdr.numposes * paliashdr.poseverts]; // Hunk_Alloc (paliashdr->numposes * paliashdr->poseverts * sizeof(trivertx_t) );
        paliashdr.posedata = verts; // (byte*)verts - (byte*)paliashdr;
        int offset = 0;
        for (int i = 0; i < paliashdr.numposes; i++)
            for (int j = 0; j < numorder; j++)
            {
                verts[offset++] = poseverts[i][vertexorder[j]];  // *verts++ = poseverts[i][vertexorder[j]];
            }
    }
    static void BuildTris()
    {
        int[] bestverts = new int[1024];
        int[] besttris = new int[1024];

        // Uze
        // All references to pheader from model.c changed to _AliasHdr (former paliashdr)

        //
        // build tristrips
        //
        numorder = 0;
        numcommands = 0;
        Array.Clear(_Used, 0, _Used.Length); // memset (used, 0, sizeof(used));
        int besttype = 0, len;
        for (int i = 0; i < paliashdr.numtris; i++)
        {
            // pick an unused triangle and start the trifan
            if (_Used[i] != 0)
                continue;

            int bestlen = 0;
            for (int type = 0; type < 2; type++)
            {
                for (int startv = 0; startv < 3; startv++)
                {
                    if (type == 1)
                        len = StripLength(i, startv);
                    else
                        len = FanLength(i, startv);
                    if (len > bestlen)
                    {
                        besttype = type;
                        bestlen = len;
                        for (int j = 0; j < bestlen + 2; j++)
                            bestverts[j] = stripverts[j];
                        for (int j = 0; j < bestlen; j++)
                            besttris[j] = striptris[j];
                    }
                }
            }

            // mark the tris on the best strip as used
            for (int j = 0; j < bestlen; j++)
                _Used[besttris[j]] = 1;

            if (besttype == 1)
                commands[numcommands++] = (bestlen + 2);
            else
                commands[numcommands++] = -(bestlen + 2);

            Union4b uval = Union4b.Empty;
            for (int j = 0; j < bestlen + 2; j++)
            {
                // emit a vertex into the reorder buffer
                int k = bestverts[j];
                vertexorder[numorder++] = k;

                // emit s/t coords into the commands stream
                float s = stverts[k].s;
                float t = stverts[k].t;
                if (triangles[besttris[0]].facesfront == 0 && stverts[k].onseam != 0)
                    s += paliashdr.skinwidth / 2;	// on back side
                s = (s + 0.5f) / paliashdr.skinwidth;
                t = (t + 0.5f) / paliashdr.skinheight;

                uval.f0 = s;
                commands[numcommands++] = uval.i0;
                uval.f0 = t;
                commands[numcommands++] = uval.i0;
            }
        }

        commands[numcommands++] = 0;		// end of list marker

        Con_DPrintf("{0,3} tri {1,3} vert {2,3} cmd\n", paliashdr.numtris, numorder, numcommands);

        allverts += numorder;
        alltris += paliashdr.numtris;
    }
    static int StripLength(int starttri, int startv)
    {
        _Used[starttri] = 2;
        
        int[] vidx = triangles[starttri].vertindex; //last = &triangles[starttri];
        stripverts[0] = vidx[(startv) % 3];
        stripverts[1] = vidx[(startv + 1) % 3];
        stripverts[2] = vidx[(startv + 2) % 3];

        striptris[0] = starttri;
        stripcount = 1;

        int m1 = stripverts[2]; // last->vertindex[(startv + 2) % 3];
        int m2 = stripverts[1]; // last->vertindex[(startv + 1) % 3];
        int lastfacesfront = triangles[starttri].facesfront;

        // look for a matching triangle
    nexttri:
        for (int j = starttri + 1; j < paliashdr.numtris; j++)
        {
            if (triangles[j].facesfront != lastfacesfront)
                continue;

            vidx = triangles[j].vertindex;

            for (int k = 0; k < 3; k++)
            {
                if (vidx[k] != m1)
                    continue;
                if (vidx[(k + 1) % 3] != m2)
                    continue;

                // this is the next part of the fan

                // if we can't use this triangle, this tristrip is done
                if (_Used[j] != 0)
                    goto done;

                // the new edge
                if ((stripcount & 1) != 0)
                    m2 = vidx[(k + 2) % 3];
                else
                    m1 = vidx[(k + 2) % 3];

                stripverts[stripcount + 2] = triangles[j].vertindex[(k + 2) % 3];
                striptris[stripcount] = j;
                stripcount++;

                _Used[j] = 2;
                goto nexttri;
            }
        }
    done:

        // clear the temp used flags
        for (int j = starttri + 1; j < paliashdr.numtris; j++)
            if (_Used[j] == 2)
                _Used[j] = 0;

        return stripcount;
    }
    static int FanLength(int starttri, int startv)
    {
        _Used[starttri] = 2;
        
        int[] vidx = triangles[starttri].vertindex;

        stripverts[0] = vidx[(startv) % 3];
        stripverts[1] = vidx[(startv + 1) % 3];
        stripverts[2] = vidx[(startv + 2) % 3];

        striptris[0] = starttri;
        stripcount = 1;

        int m1 = vidx[(startv + 0) % 3];
        int m2 = vidx[(startv + 2) % 3];
        int lastfacesfront = triangles[starttri].facesfront;

        // look for a matching triangle
    nexttri:
        for (int j = starttri + 1; j < paliashdr.numtris; j++)//, check++)
        {
            vidx = triangles[j].vertindex;
            if (triangles[j].facesfront != lastfacesfront)
                continue;

            for (int k = 0; k < 3; k++)
            {
                if (vidx[k] != m1)
                    continue;
                if (vidx[(k + 1) % 3] != m2)
                    continue;

                // this is the next part of the fan

                // if we can't use this triangle, this tristrip is done
                if (_Used[j] != 0)
                    goto done;

                // the new edge
                m2 = vidx[(k + 2) % 3];

                stripverts[stripcount + 2] = m2;
                striptris[stripcount] = j;
                stripcount++;

                _Used[j] = 2;
                goto nexttri;
            }
        }
    done:

        // clear the temp used flags
        for (int j = starttri + 1; j < paliashdr.numtris; j++)
            if (_Used[j] == 2)
                _Used[j] = 0;

        return stripcount;
    }
}