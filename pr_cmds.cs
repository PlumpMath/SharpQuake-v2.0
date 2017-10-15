using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static builtin_t[] pr_builtins = new builtin_t[]
    {
        PF_Fixme,
        PF_makevectors,
        PF_setorigin,
        PF_setmodel,
        PF_setsize,
        PF_Fixme,
        PF_break,
        PF_random,
        PF_sound,
        PF_normalize,
        PF_errror,
        PF_objerror,
        PF_vlen,
        PF_vectoyaw,
        PF_Spawn,
        PF_Remove,
        PF_traceline,
        PF_checkclient,
        PF_Find,
        PF_precache_sound,	
        PF_precache_model,	
        PF_stuffcmd,
        PF_findradius,
        PF_bprint,
        PF_sprint,
        PF_dprint,
        PF_ftos,
        PF_vtos,
        PF_coredump,
        PF_traceon,
        PF_traceoff,
        PF_eprint,
        PF_walkmove,
        PF_Fixme,
        PF_droptofloor,
        PF_lightstyle,
        PF_rint,
        PF_floor,
        PF_ceil,
        PF_Fixme,
        PF_checkbottom,
        PF_pointcontents,
        PF_Fixme,
        PF_fabs,
        PF_aim,
        PF_cvar,
        PF_localcmd,
        PF_nextent,
        PF_particle,
        PF_changeyaw,
        PF_Fixme,
        PF_vectoangles,

        PF_WriteByte,
        PF_WriteChar,
        PF_WriteShort,
        PF_WriteLong,
        PF_WriteCoord,
        PF_WriteAngle,
        PF_WriteString,
        PF_WriteEntity,

        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,
        PF_Fixme,

        SV_MoveToGoal,
        PF_precache_file,
        PF_makestatic,

        PF_changelevel,
        PF_Fixme,

        PF_cvar_set,
        PF_centerprint,

        PF_ambientsound,

        PF_precache_model,
        PF_precache_sound,
        PF_precache_file,

        PF_setspawnparms
    };
    public static byte[] checkpvs = new byte[q_shared.MAX_MAP_LEAFS / 8];
    public static int _TempString = -1;
    public static int c_invis;
    public static int c_notvis;


    public static MsgWriter WriteDest
    {
        get
        {
            int dest = (int)G_FLOAT(q_shared.OFS_PARM0);
            switch (dest)
            {
                case q_shared.MSG_BROADCAST:
                    return sv.datagram;

                case q_shared.MSG_ONE:
                    edict_t ent = PROG_TO_EDICT(pr_global_struct.msg_entity);
                    int entnum = NUM_FOR_EDICT(ent);
                    if (entnum < 1 || entnum > svs.maxclients)
                        PR_RunError("WriteDest: not a client");
                    return svs.clients[entnum - 1].message;

                case q_shared.MSG_ALL:
                    return sv.reliable_datagram;

                case q_shared.MSG_INIT:
                    return sv.signon;

                default:
                    PR_RunError("WriteDest: bad destination");
                    break;
            }

            return null;
        }
    }
    public static void Execute(int num)
    {
        pr_builtins[num]();
    }
    public static int SetTempString(string value)
    {
        if (_TempString == -1)
        {
            _TempString = ED_NewString(value);
        }
        else
        {
            SetString(_TempString, value);
        }
        return _TempString;
    }
    public static void ClearState()
    {
        _TempString = -1;
    }
    public static unsafe void RETURN_EDICT(edict_t e)
    {
        int prog = EDICT_TO_PROG(e);
        G_INT(prog);
    }
    public static unsafe void G_INT(int value)
    {
        int* ptr = (int*)_GlobalStructAddr;
        ptr[q_shared.OFS_RETURN] = value;
    }
    public static unsafe void G_FLOAT(float value)
    {
        float* ptr = (float*)_GlobalStructAddr;
        ptr[q_shared.OFS_RETURN] = value;
    }
    public static unsafe void G_VECTOR(ref v3f value)
    {
        float* ptr = (float*)_GlobalStructAddr;
        ptr[q_shared.OFS_RETURN + 0] = value.x;
        ptr[q_shared.OFS_RETURN + 1] = value.y;
        ptr[q_shared.OFS_RETURN + 2] = value.z;
    }
    public static unsafe void G_VECTOR(ref Vector3 value)
    {
        float* ptr = (float*)_GlobalStructAddr;
        ptr[q_shared.OFS_RETURN + 0] = value.X;
        ptr[q_shared.OFS_RETURN + 1] = value.Y;
        ptr[q_shared.OFS_RETURN + 2] = value.Z;
    }
    public static unsafe string G_STRING(int parm)
    {
        int* ptr = (int*)_GlobalStructAddr;
        return GetString(ptr[parm]);
    }

    /// <summary>
    /// G_INT(o)
    /// </summary>
    public static unsafe int GetInt(int parm)
    {
        int* ptr = (int*)_GlobalStructAddr;
        return ptr[parm];
    }
    public static unsafe float G_FLOAT(int parm)
    {
        float* ptr = (float*)_GlobalStructAddr;
        return ptr[parm];
    }
    public static unsafe float* G_VECTOR(int parm)
    {
        float* ptr = (float*)_GlobalStructAddr;
        return &ptr[parm];
    }
    public static unsafe edict_t G_EDICT(int parm)
    {
        int* ptr = (int*)_GlobalStructAddr;
        edict_t ed = PROG_TO_EDICT(ptr[parm]);
        return ed;
    }
    static string PF_VarString(int first)
    {
        StringBuilder sb = new StringBuilder(256);
        for (int i = first; i < pr_argc; i++)
        {
            sb.Append(G_STRING(q_shared.OFS_PARM0 + i * 3));
        }
        return sb.ToString();
    }
    static unsafe void Copy(float* src, ref v3f dest)
    {
        dest.x = src[0];
        dest.y = src[1];
        dest.z = src[2];
    }
    static unsafe void Copy(float* src, out Vector3 dest)
    {
        dest.X = src[0];
        dest.Y = src[1];
        dest.Z = src[2];
    }
    static void PF_errror()
    {
        string s = PF_VarString(0);
        Con_Printf("======SERVER ERROR in {0}:\n{1}\n",
            GetString(pr_xfunction.s_name), s);
        edict_t ed = PROG_TO_EDICT(pr_global_struct.self);
        ED_Print(ed);
        Host_Error("Program error");
    }
    static void PF_objerror()
    {
        string s = PF_VarString(0);
        Con_Printf("======OBJECT ERROR in {0}:\n{1}\n",
            G_STRING(pr_xfunction.s_name), s);
        edict_t ed = PROG_TO_EDICT(pr_global_struct.self);
        ED_Print(ed);
        ED_Free(ed);
        Host_Error("Program error");
    }
    static unsafe void PF_makevectors()
    {
        float* av = G_VECTOR(q_shared.OFS_PARM0);
        Vector3 a = new Vector3(av[0], av[1], av[2]);
        Vector3 fw, right, up;
        Mathlib.AngleVectors(ref a, out fw, out right, out up);
        Mathlib.Copy(ref fw, out pr_global_struct.v_forward);
        Mathlib.Copy(ref right, out pr_global_struct.v_right);
        Mathlib.Copy(ref up, out pr_global_struct.v_up);
    }
    static unsafe void PF_setorigin()
    {
        edict_t e = G_EDICT(q_shared.OFS_PARM0);
        float* org = G_VECTOR(q_shared.OFS_PARM1);
        Copy(org, ref e.v.origin);

        SV_LinkEdict(e, false);
    }
    static void SetMinMaxSize(edict_t e, ref Vector3 min, ref Vector3 max, bool rotate)
    {
        if (min.X > max.X || min.Y > max.Y || min.Z > max.Z)
            PR_RunError("backwards mins/maxs");

        rotate = false;		// FIXME: implement rotation properly again

        Vector3 rmin = min, rmax = max;
        if (!rotate)
        {
            //rmin = min;
            //rmax = max;
        }
        else
        {
            // find min / max for rotations
            //angles = e.v.angles;

            //a = angles[1] / 180 * M_PI;

            //xvector[0] = cos(a);
            //xvector[1] = sin(a);
            //yvector[0] = -sin(a);
            //yvector[1] = cos(a);

            //VectorCopy(min, bounds[0]);
            //VectorCopy(max, bounds[1]);

            //rmin[0] = rmin[1] = rmin[2] = 9999;
            //rmax[0] = rmax[1] = rmax[2] = -9999;

            //for (i = 0; i <= 1; i++)
            //{
            //    base[0] = bounds[i][0];
            //    for (j = 0; j <= 1; j++)
            //    {
            //        base[1] = bounds[j][1];
            //        for (k = 0; k <= 1; k++)
            //        {
            //            base[2] = bounds[k][2];

            //            // transform the point
            //            transformed[0] = xvector[0] * base[0] + yvector[0] * base[1];
            //            transformed[1] = xvector[1] * base[0] + yvector[1] * base[1];
            //            transformed[2] = base[2];

            //            for (l = 0; l < 3; l++)
            //            {
            //                if (transformed[l] < rmin[l])
            //                    rmin[l] = transformed[l];
            //                if (transformed[l] > rmax[l])
            //                    rmax[l] = transformed[l];
            //            }
            //        }
            //    }
            //}
        }

        // set derived values
        Mathlib.Copy(ref rmin, out e.v.mins);
        Mathlib.Copy(ref rmax, out e.v.maxs);
        Vector3 s = max - min;
        Mathlib.Copy(ref s, out e.v.size);

        SV_LinkEdict(e, false);
    }
    static unsafe void PF_setsize()
    {
        edict_t e = G_EDICT(q_shared.OFS_PARM0);
        float* min = G_VECTOR(q_shared.OFS_PARM1);
        float* max = G_VECTOR(q_shared.OFS_PARM2);
        Vector3 vmin, vmax;
        Copy(min, out vmin);
        Copy(max, out vmax);
        SetMinMaxSize(e, ref vmin, ref vmax, false);
    }
    static void PF_setmodel()
    {
        edict_t e = G_EDICT(q_shared.OFS_PARM0);
        int m_idx = GetInt(q_shared.OFS_PARM1);
        string m = GetString(m_idx);

        // check to see if model was properly precached
        for (int i = 0; i < sv.model_precache.Length; i++)
        {
            string check = sv.model_precache[i];

            if (check == null)
                break;

            if (check == m)
            {
                e.v.model = m_idx; // m - pr_strings;
                e.v.modelindex = i;

                model_t mod = sv.models[(int)e.v.modelindex];

                if (mod != null)
                    SetMinMaxSize(e, ref mod.mins, ref mod.maxs, true);
                else
                    SetMinMaxSize(e, ref q_shared.ZeroVector, ref q_shared.ZeroVector, true);

                return;
            }
        }

        PR_RunError("no precache: {0}\n", m);
    }
    static void PF_bprint()
    {
        string s = PF_VarString(0);
        SV_BroadcastPrint(s);
    }
    static void PF_sprint()
    {
        int entnum = NUM_FOR_EDICT(G_EDICT(q_shared.OFS_PARM0));
        string s = PF_VarString(1);

        if (entnum < 1 || entnum > svs.maxclients)
        {
            Con_Printf("tried to sprint to a non-client\n");
            return;
        }

        client_t client = svs.clients[entnum - 1];

        client.message.MSG_WriteChar(q_shared.svc_print);
        client.message.MSG_WriteString(s);
    }
    static void PF_centerprint()
    {
        int entnum = NUM_FOR_EDICT(G_EDICT(q_shared.OFS_PARM0));
        string s = PF_VarString(1);

        if (entnum < 1 || entnum > svs.maxclients)
        {
            Con_Printf("tried to centerprint to a non-client\n");
            return;
        }

        client_t client = svs.clients[entnum - 1];

        client.message.MSG_WriteChar(q_shared.svc_centerprint);
        client.message.MSG_WriteString(s);
    }
    static unsafe void PF_normalize()
    {
        float* value1 = G_VECTOR(q_shared.OFS_PARM0);
        Vector3 tmp;
        Copy(value1, out tmp);
        Mathlib.Normalize(ref tmp);

        G_VECTOR(ref tmp);
    }
    static unsafe void PF_vlen()
    {
        float* v = G_VECTOR(q_shared.OFS_PARM0);
        float result = (float)Math.Sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2]);

        G_FLOAT(result);
    }
    static unsafe void PF_vectoyaw()
    {
        float* value1 = G_VECTOR(q_shared.OFS_PARM0);
        float yaw;
        if (value1[1] == 0 && value1[0] == 0)
            yaw = 0;
        else
        {
            yaw = (int)(Math.Atan2(value1[1], value1[0]) * 180 / Math.PI);
            if (yaw < 0)
                yaw += 360;
        }

        G_FLOAT(yaw);
    }
    static unsafe void PF_vectoangles()
    {
        float yaw, pitch, forward;
        float* value1 = G_VECTOR(q_shared.OFS_PARM0);

        if (value1[1] == 0 && value1[0] == 0)
        {
            yaw = 0;
            if (value1[2] > 0)
                pitch = 90;
            else
                pitch = 270;
        }
        else
        {
            yaw = (int)(Math.Atan2(value1[1], value1[0]) * 180 / Math.PI);
            if (yaw < 0)
                yaw += 360;

            forward = (float)Math.Sqrt(value1[0] * value1[0] + value1[1] * value1[1]);
            pitch = (int)(Math.Atan2(value1[2], forward) * 180 / Math.PI);
            if (pitch < 0)
                pitch += 360;
        }

        Vector3 result = new Vector3(pitch, yaw, 0);
        G_VECTOR(ref result);
    }
    static void PF_random()
    {
        float num = (Random() & 0x7fff) / ((float)0x7fff);
        G_FLOAT(num);
    }
    static unsafe void PF_particle()
    {
        float* org = G_VECTOR(q_shared.OFS_PARM0);
        float* dir = G_VECTOR(q_shared.OFS_PARM1);
        float color = G_FLOAT(q_shared.OFS_PARM2);
        float count = G_FLOAT(q_shared.OFS_PARM3);
        Vector3 vorg, vdir;
        Copy(org, out vorg);
        Copy(dir, out vdir);
        SV_StartParticle(ref vorg, ref vdir, (int)color, (int)count);
    }
    static unsafe void PF_ambientsound()
    {
        float* pos = G_VECTOR(q_shared.OFS_PARM0);
        string samp = G_STRING(q_shared.OFS_PARM1);
        float vol = G_FLOAT(q_shared.OFS_PARM2);
        float attenuation = G_FLOAT(q_shared.OFS_PARM3);

        // check to see if samp was properly precached
        for (int i = 0; i < sv.sound_precache.Length; i++)
        {
            if (sv.sound_precache[i] == null)
                break;

            if (samp == sv.sound_precache[i])
            {
                // add an svc_spawnambient command to the level signon packet
                MsgWriter msg = sv.signon;

                msg.MSG_WriteByte(q_shared.svc_spawnstaticsound);
                for (int i2 = 0; i2 < 3; i2++)
                    msg.MSG_WriteCoord(pos[i2]);

                msg.MSG_WriteByte(i);

                msg.MSG_WriteByte((int)(vol * 255));
                msg.MSG_WriteByte((int)(attenuation * 64));

                return;
            }
        }

        Con_Printf("no precache: {0}\n", samp);
    }
    static void PF_sound()
    {
        edict_t entity = G_EDICT(q_shared.OFS_PARM0);
        int channel = (int)G_FLOAT(q_shared.OFS_PARM1);
        string sample = G_STRING(q_shared.OFS_PARM2);
        int volume = (int)(G_FLOAT(q_shared.OFS_PARM3) * 255);
        float attenuation = G_FLOAT(q_shared.OFS_PARM4);

        SV_StartSound(entity, channel, sample, volume, attenuation);
    }
    static void PF_break()
    {
        Con_Printf("break statement\n");
        //*(int *)-4 = 0;	// dump to debugger
    }
    static unsafe void PF_traceline()
    {
        float* v1 = G_VECTOR(q_shared.OFS_PARM0);
        float* v2 = G_VECTOR(q_shared.OFS_PARM1);
        int nomonsters = (int)G_FLOAT(q_shared.OFS_PARM2);
        edict_t ent = G_EDICT(q_shared.OFS_PARM3);

        Vector3 vec1, vec2;
        Copy(v1, out vec1);
        Copy(v2, out vec2);
        trace_t trace = SV_Move(ref vec1, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref vec2, nomonsters, ent);

        pr_global_struct.trace_allsolid = trace.allsolid ? 1 : 0;
        pr_global_struct.trace_startsolid = trace.startsolid ? 1 : 0;
        pr_global_struct.trace_fraction = trace.fraction;
        pr_global_struct.trace_inwater = trace.inwater ? 1 : 0;
        pr_global_struct.trace_inopen = trace.inopen ? 1 : 0;
        Mathlib.Copy(ref trace.endpos, out pr_global_struct.trace_endpos);
        Mathlib.Copy(ref trace.plane.normal, out pr_global_struct.trace_plane_normal);
        pr_global_struct.trace_plane_dist = trace.plane.dist;
        if (trace.ent != null)
            pr_global_struct.trace_ent = EDICT_TO_PROG(trace.ent);
        else
            pr_global_struct.trace_ent = EDICT_TO_PROG(sv.edicts[0]);
    }
    static void PF_checkpos()
    {
    }
    static int PF_newcheckclient(int check)
    {
        // cycle to the next one

        if (check < 1)
            check = 1;
        if (check > svs.maxclients)
            check = svs.maxclients;

        int i = check + 1;
        if (check == svs.maxclients)
            i = 1;

        edict_t ent;
        for (; ; i++)
        {
            if (i == svs.maxclients + 1)
                i = 1;

            ent = EDICT_NUM(i);

            if (i == check)
                break;	// didn't find anything else

            if (ent.free)
                continue;
            if (ent.v.health <= 0)
                continue;
            if (((int)ent.v.flags & q_shared.FL_NOTARGET) != 0)
                continue;

            // anything that is a client, or has a client as an enemy
            break;
        }

        // get the PVS for the entity
        Vector3 org = ToVector(ref ent.v.origin) + ToVector(ref ent.v.view_ofs);
        mleaf_t leaf = Mod_PointInLeaf(ref org, sv.worldmodel);
        byte[] pvs = Mod_LeafPVS(leaf, sv.worldmodel);
        Buffer.BlockCopy(pvs, 0, checkpvs, 0, pvs.Length);

        return i;
    }    
    static void PF_checkclient()
    {
        // find a new check if on a new frame
        if (sv.time - sv.lastchecktime >= 0.1)
        {
            sv.lastcheck = PF_newcheckclient(sv.lastcheck);
            sv.lastchecktime = sv.time;
        }

        // return check if it might be visible	
        edict_t ent = EDICT_NUM(sv.lastcheck);
        if (ent.free || ent.v.health <= 0)
        {
            RETURN_EDICT(sv.edicts[0]);
            return;
        }

        // if current entity can't possibly see the check entity, return 0
        edict_t self = PROG_TO_EDICT(pr_global_struct.self);
        Vector3 view = ToVector(ref self.v.origin) + ToVector(ref self.v.view_ofs);
        mleaf_t leaf = Mod_PointInLeaf(ref view, sv.worldmodel);
        int l = Array.IndexOf(sv.worldmodel.leafs, leaf) - 1;
        if ((l < 0) || (checkpvs[l >> 3] & (1 << (l & 7))) == 0)
        {
            c_notvis++;
            RETURN_EDICT(sv.edicts[0]);
            return;
        }

        // might be able to see it
        c_invis++;
        RETURN_EDICT(ent);
    }
    static void PF_stuffcmd()
    {
        int entnum = NUM_FOR_EDICT(G_EDICT(q_shared.OFS_PARM0));
        if (entnum < 1 || entnum > svs.maxclients)
            PR_RunError("Parm 0 not a client");
        string str = G_STRING(q_shared.OFS_PARM1);

        client_t old = host_client;
        host_client = svs.clients[entnum - 1];
        Host_ClientCommands("{0}", str);
        host_client = old;
    }
    static void PF_localcmd()
    {
        string cmd = G_STRING(q_shared.OFS_PARM0);
        Cbuf_AddText(cmd);
    }
    static void PF_cvar()
    {
        string str = G_STRING(q_shared.OFS_PARM0);
        G_FLOAT(Cvar.Cvar_VariableValue(str));
    }
    static void PF_cvar_set()
    {
        Cvar.Cvar_Set(G_STRING(q_shared.OFS_PARM0), G_STRING(q_shared.OFS_PARM1));
    }
    static unsafe void PF_findradius()
    {
        edict_t chain = sv.edicts[0];

        float* org = G_VECTOR(q_shared.OFS_PARM0);
        float rad = G_FLOAT(q_shared.OFS_PARM1);

        Vector3 vorg;
        Copy(org, out vorg);

        for (int i = 1; i < sv.num_edicts; i++)
        {
            edict_t ent = sv.edicts[i];
            if (ent.free)
                continue;
            if (ent.v.solid == q_shared.SOLID_NOT)
                continue;

            Vector3 v = vorg - (ToVector(ref ent.v.origin) +
                (ToVector(ref ent.v.mins) + ToVector(ref ent.v.maxs)) * 0.5f);
            if (v.Length > rad)
                continue;

            ent.v.chain = EDICT_TO_PROG(chain);
            chain = ent;
        }

        RETURN_EDICT(chain);
    }
    static void PF_dprint()
    {
        Con_DPrintf(PF_VarString(0));
    }
    static void PF_ftos()
    {
        float v = G_FLOAT(q_shared.OFS_PARM0);

        if (v == (int)v)
            SetTempString(String.Format("{0}", (int)v));
        else
            SetTempString(String.Format("{0:F1}", v)); //  sprintf(pr_string_temp, "%5.1f", v);
        G_INT(_TempString);
    }
    static void PF_fabs()
    {
        float v = G_FLOAT(q_shared.OFS_PARM0);
        G_FLOAT(Math.Abs(v));
    }
    static unsafe void PF_vtos()
    {
        float* v = G_VECTOR(q_shared.OFS_PARM0);
        SetTempString(String.Format("'{0,5:F1} {1,5:F1} {2,5:F1}'", v[0], v[1], v[2]));
        G_INT(_TempString);
    }
    static void PF_Spawn()
    {
        edict_t ed = ED_Alloc();
        RETURN_EDICT(ed);
    }
    static void PF_Remove()
    {
        edict_t ed = G_EDICT(q_shared.OFS_PARM0);
        ED_Free(ed);
    }
    static void PF_Find()
    {
        int e = GetInt(q_shared.OFS_PARM0);
        int f = GetInt(q_shared.OFS_PARM1);
        string s = G_STRING(q_shared.OFS_PARM2);
        if (s == null)
            PR_RunError("PF_Find: bad search string");

        for (e++; e < sv.num_edicts; e++)
        {
            edict_t ed = EDICT_NUM(e);
            if (ed.free)
                continue;
            string t = GetString(ed.GetInt(f)); // E_STRING(ed, f);
            if (String.IsNullOrEmpty(t))
                continue;
            if (t == s)
            {
                RETURN_EDICT(ed);
                return;
            }
        }

        RETURN_EDICT(sv.edicts[0]);
    }
    static void CheckEmptyString(string s)
    {
        if (s == null || s.Length == 0 || s[0] <= ' ')
            PR_RunError("Bad string");
    }
    static void PF_precache_file()
    {
        // precache_file is only used to copy files with qcc, it does nothing
        G_INT(GetInt(q_shared.OFS_PARM0));
    }
    static void PF_precache_sound()
    {
        if (sv.state != server_state_t.Loading)
            PR_RunError("PF_Precache_*: Precache can only be done in spawn functions");

        string s = G_STRING(q_shared.OFS_PARM0);
        G_INT(GetInt(q_shared.OFS_PARM0));
        CheckEmptyString(s);

        for (int i = 0; i < q_shared.MAX_SOUNDS; i++)
        {
            if (sv.sound_precache[i] == null)
            {
                sv.sound_precache[i] = s;
                return;
            }
            if (sv.sound_precache[i] == s)
                return;
        }
        PR_RunError("PF_precache_sound: overflow");
    }
    static void PF_precache_model()
    {
        if (sv.state != server_state_t.Loading)
            PR_RunError("PF_Precache_*: Precache can only be done in spawn functions");

        string s = G_STRING(q_shared.OFS_PARM0);
        G_INT(GetInt(q_shared.OFS_PARM0));
        CheckEmptyString(s);

        for (int i = 0; i < q_shared.MAX_MODELS; i++)
        {
            if (sv.model_precache[i] == null)
            {
                sv.model_precache[i] = s;
                sv.models[i] = Mod_ForName(s, true);
                return;
            }
            if (sv.model_precache[i] == s)
                return;
        }
        PR_RunError("PF_precache_model: overflow");
    }
    static void PF_coredump()
    {
        ED_PrintEdicts();
    }
    static void PF_traceon()
    {
        pr_trace = true;
    }
    static void PF_traceoff()
    {
        pr_trace = false;
    }
    static void PF_eprint()
    {
        ED_PrintNum(NUM_FOR_EDICT(G_EDICT(q_shared.OFS_PARM0)));
    }
    static void PF_walkmove()
    {
        edict_t ent = PROG_TO_EDICT(pr_global_struct.self);
        float yaw = G_FLOAT(q_shared.OFS_PARM0);
        float dist = G_FLOAT(q_shared.OFS_PARM1);

        if (((int)ent.v.flags & (q_shared.FL_ONGROUND | q_shared.FL_FLY | q_shared.FL_SWIM)) == 0)
        {
            G_FLOAT((float)0);
            return;
        }

        yaw = (float)(yaw * Math.PI * 2.0 / 360.0);

        v3f move;
        move.x = (float)Math.Cos(yaw) * dist;
        move.y = (float)Math.Sin(yaw) * dist;
        move.z = 0;

        // save program state, because SV_movestep may call other progs
        dfunction_t oldf = pr_xfunction;
        int oldself = pr_global_struct.self;

        G_FLOAT((float)(SV_movestep(ent, ref move, true) ? 1 : 0));

        // restore program state
        pr_xfunction = oldf;
        pr_global_struct.self = oldself;
    }
    static void PF_droptofloor()
    {
        edict_t ent = PROG_TO_EDICT(pr_global_struct.self);

        Vector3 org, mins, maxs;
        Mathlib.Copy(ref ent.v.origin, out org);
        Mathlib.Copy(ref ent.v.mins, out mins);
        Mathlib.Copy(ref ent.v.maxs, out maxs);
        Vector3 end = org;
        end.Z -= 256;

        trace_t trace = SV_Move(ref org, ref mins, ref maxs, ref end, 0, ent);

        if (trace.fraction == 1 || trace.allsolid)
            G_FLOAT((float)0);
        else
        {
            Mathlib.Copy(ref trace.endpos, out ent.v.origin);
            SV_LinkEdict(ent, false);
            ent.v.flags = (int)ent.v.flags | q_shared.FL_ONGROUND;
            ent.v.groundentity = EDICT_TO_PROG(trace.ent);
            G_FLOAT((float)1);
        }
    }
    static void PF_lightstyle()
    {
        int style = (int)G_FLOAT(q_shared.OFS_PARM0); // Uze: ???
        string val = G_STRING(q_shared.OFS_PARM1);

        // change the string in sv
        sv.lightstyles[style] = val;

        // send message to all clients on this server
        if (!sv.active)
            return;

        for (int j = 0; j < svs.maxclients; j++)
        {
            client_t client = svs.clients[j];
            if (client.active || client.spawned)
            {
                client.message.MSG_WriteChar(q_shared.svc_lightstyle);
                client.message.MSG_WriteChar(style);
                client.message.MSG_WriteString(val);
            }
        }
    }
    static void PF_rint()
    {
        float f = G_FLOAT(q_shared.OFS_PARM0);
        if (f > 0)
            G_FLOAT((float)(int)(f + 0.5));
        else
            G_FLOAT((float)(int)(f - 0.5));
    }
    static void PF_floor()
    {
        G_FLOAT((float)Math.Floor(G_FLOAT(q_shared.OFS_PARM0)));
    }
    static void PF_ceil()
    {
        G_FLOAT((float)Math.Ceiling(G_FLOAT(q_shared.OFS_PARM0)));
    }
    static void PF_checkbottom()
    {
        edict_t ent = G_EDICT(q_shared.OFS_PARM0);
        G_FLOAT((float)(SV_CheckBottom(ent) ? 1 : 0));
    }
    static unsafe void PF_pointcontents()
    {
        float* v = G_VECTOR(q_shared.OFS_PARM0);
        Vector3 tmp;
        Copy(v, out tmp);
        G_FLOAT((float)SV_PointContents(ref tmp));
    }
    static void PF_nextent()
    {
        int i = NUM_FOR_EDICT(G_EDICT(q_shared.OFS_PARM0));
        while (true)
        {
            i++;
            if (i == sv.num_edicts)
            {
                RETURN_EDICT(sv.edicts[0]);
                return;
            }
            edict_t ent = EDICT_NUM(i);
            if (!ent.free)
            {
                RETURN_EDICT(ent);
                return;
            }
        }
    }
    static void PF_aim()
    {
        edict_t ent = G_EDICT(q_shared.OFS_PARM0);
        float speed = G_FLOAT(q_shared.OFS_PARM1);

        Vector3 start = ToVector(ref ent.v.origin);
        start.Z += 20;

        // try sending a trace straight
        Vector3 dir;
        Mathlib.Copy(ref pr_global_struct.v_forward, out dir);
        Vector3 end = start + dir * 2048;
        trace_t tr = SV_Move(ref start, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref end, 0, ent);
        if (tr.ent != null && tr.ent.v.takedamage == q_shared.DAMAGE_AIM &&
            (teamplay.value == 0 || ent.v.team <= 0 || ent.v.team != tr.ent.v.team))
        {
            G_VECTOR(ref pr_global_struct.v_forward);
            return;
        }

        // try all possible entities
        Vector3 bestdir = dir;
        float bestdist = sv_aim.value;
        edict_t bestent = null;

        for (int i = 1; i < sv.num_edicts; i++)
        {
            edict_t check = sv.edicts[i];
            if (check.v.takedamage != q_shared.DAMAGE_AIM)
                continue;
            if (check == ent)
                continue;
            if (teamplay.value != 0 && ent.v.team > 0 && ent.v.team == check.v.team)
                continue;	// don't aim at teammate

            v3f tmp;
            Mathlib.VectorAdd(ref check.v.mins, ref check.v.maxs, out tmp);
            Mathlib.VectorMA(ref check.v.origin, 0.5f, ref tmp, out tmp);
            Mathlib.Copy(ref tmp, out end);

            dir = end - start;
            Mathlib.Normalize(ref dir);
            float dist = Vector3.Dot(dir, ToVector(ref pr_global_struct.v_forward));
            if (dist < bestdist)
                continue;	// to far to turn
            tr = SV_Move(ref start, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref end, 0, ent);
            if (tr.ent == check)
            {	// can shoot at this one
                bestdist = dist;
                bestent = check;
            }
        }

        if (bestent != null)
        {
            v3f dir2, end2;
            Mathlib.VectorSubtract(ref bestent.v.origin, ref ent.v.origin, out dir2);
            float dist = Mathlib.DotProduct(ref dir2, ref pr_global_struct.v_forward);
            Mathlib.VectorScale(ref pr_global_struct.v_forward, dist, out end2);
            end2.z = dir2.z;
            Mathlib.Normalize(ref end2);
            G_VECTOR(ref end2);
        }
        else
        {
            G_VECTOR(ref bestdir);
        }
    }
    public static void PF_changeyaw()
    {
        edict_t ent = PROG_TO_EDICT(pr_global_struct.self);
        float current = Mathlib.anglemod(ent.v.angles.y);
        float ideal = ent.v.ideal_yaw;
        float speed = ent.v.yaw_speed;

        if (current == ideal)
            return;

        float move = ideal - current;
        if (ideal > current)
        {
            if (move >= 180)
                move = move - 360;
        }
        else
        {
            if (move <= -180)
                move = move + 360;
        }
        if (move > 0)
        {
            if (move > speed)
                move = speed;
        }
        else
        {
            if (move < -speed)
                move = -speed;
        }

        ent.v.angles.y = Mathlib.anglemod(current + move);
    }
    static void PF_WriteByte()
    {
        WriteDest.MSG_WriteByte((int)G_FLOAT(q_shared.OFS_PARM1));
    }
    static void PF_WriteChar()
    {
        WriteDest.MSG_WriteChar((int)G_FLOAT(q_shared.OFS_PARM1));
    }
    static void PF_WriteShort()
    {
        WriteDest.MSG_WriteShort((int)G_FLOAT(q_shared.OFS_PARM1));
    }
    static void PF_WriteLong()
    {
        WriteDest.MSG_WriteLong((int)G_FLOAT(q_shared.OFS_PARM1));
    }
    static void PF_WriteAngle()
    {
        WriteDest.MSG_WriteAngle(G_FLOAT(q_shared.OFS_PARM1));
    }
    static void PF_WriteCoord()
    {
        WriteDest.MSG_WriteCoord(G_FLOAT(q_shared.OFS_PARM1));
    }
    static void PF_WriteString()
    {
        WriteDest.MSG_WriteString(G_STRING(q_shared.OFS_PARM1));
    }
    static void PF_WriteEntity()
    {
        WriteDest.MSG_WriteShort(NUM_FOR_EDICT(G_EDICT(q_shared.OFS_PARM1)));
    }
    static void PF_makestatic()
    {
        edict_t ent = G_EDICT(q_shared.OFS_PARM0);
        MsgWriter msg = sv.signon;

        msg.MSG_WriteByte(q_shared.svc_spawnstatic);
        msg.MSG_WriteByte(SV_ModelIndex(GetString(ent.v.model)));
        msg.MSG_WriteByte((int)ent.v.frame);
        msg.MSG_WriteByte((int)ent.v.colormap);
        msg.MSG_WriteByte((int)ent.v.skin);
        for (int i = 0; i < 3; i++)
        {
            msg.MSG_WriteCoord(Mathlib.Comp(ref ent.v.origin, i));
            msg.MSG_WriteAngle(Mathlib.Comp(ref ent.v.angles, i));
        }

        // throw the entity away now
        ED_Free(ent);
    }
    static void PF_setspawnparms()
    {
        edict_t ent = G_EDICT(q_shared.OFS_PARM0);
        int i = NUM_FOR_EDICT(ent);
        if (i < 1 || i > svs.maxclients)
            PR_RunError("Entity is not a client");

        // copy spawn parms out of the client_t
        client_t client = svs.clients[i - 1];

        pr_global_struct.SetParams(client.spawn_parms);
    }
    static void PF_changelevel()
    {
        // make sure we don't issue two changelevels
        if (svs.changelevel_issued)
            return;

        svs.changelevel_issued = true;

        string s = G_STRING(q_shared.OFS_PARM0);
        Cbuf_AddText(String.Format("changelevel {0}\n", s));
    }
    static void PF_Fixme()
    {
        PR_RunError("unimplemented bulitin");
    }
}