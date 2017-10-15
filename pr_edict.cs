using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Globalization;
using System.IO;

public static partial class game_engine
{
    public static gefv_cache[] gefvCache = new gefv_cache[q_shared.GEFV_CACHESIZE];
    public static int _gefvPos;

    public static int[] type_size = new int[8]
    {
        1, sizeof(int)/4, 1, 3, 1, 1, sizeof(int)/4, IntPtr.Size/4
    };

    public static cvar_t nomonsters;
    public static cvar_t gamecfg;
    public static cvar_t scratch1;
    public static cvar_t scratch2;
    public static cvar_t scratch3;
    public static cvar_t scratch4;
    public static cvar_t savedgamecfg;
    public static cvar_t saved1;
    public static cvar_t saved2;
    public static cvar_t saved3;
    public static cvar_t saved4;

    public static dprograms_t progs;
    public static dfunction_t[] pr_functions;
    public static string pr_strings;
    public static ddef_t[] pr_fielddefs;
    public static ddef_t[] pr_globaldefs;
    public static dstatement_t[] pr_statements;
    public static globalvars_t pr_global_struct;
    public static float[] _Globals;
    public static int pr_edict_size;
    public static ushort pr_crc;
    public static GCHandle _HGlobalStruct;
    public static GCHandle _HGlobals;
    public static long _GlobalStructAddr;
    public static long _GlobalsAddr;
    public static List<string> _DynamicStrings = new List<string>(512);


    public static void PR_Init()
    {
        Cmd_AddCommand("edict", ED_PrintEdict_f);
        Cmd_AddCommand("edicts", ED_PrintEdicts);
        Cmd_AddCommand("edictcount", ED_Count);
        Cmd_AddCommand("profile", PR_Profile_f);
        Cmd_AddCommand("test5", Test5_f);

        nomonsters = new cvar_t("nomonsters", "0");
        gamecfg = new cvar_t("gamecfg", "0");
        scratch1 = new cvar_t("scratch1", "0");
        scratch2 = new cvar_t("scratch2", "0");
        scratch3 = new cvar_t("scratch3", "0");
        scratch4 = new cvar_t("scratch4", "0");
        savedgamecfg = new cvar_t("savedgamecfg", "0", true);
        saved1 = new cvar_t("saved1", "0", true);
        saved2 = new cvar_t("saved2", "0", true);
        saved3 = new cvar_t("saved3", "0", true);
        saved4 = new cvar_t("saved4", "0", true);
    }
    public static void Test5_f()
    {
        entity_t p = cl_entities[cl.viewentity];
        if (p == null)
            return;

        OpenTK.Vector3 org = p.origin;

        for (int i = 0; i < sv.edicts.Length; i++)
        {
            edict_t ed = sv.edicts[i];

            if (ed.free)
                continue;

            OpenTK.Vector3 vmin, vmax;
            Mathlib.Copy(ref ed.v.absmax, out vmax);
            Mathlib.Copy(ref ed.v.absmin, out vmin);

            if (org.X >= vmin.X && org.Y >= vmin.Y && org.Z >= vmin.Z &&
                org.X <= vmax.X && org.Y <= vmax.Y && org.Z <= vmax.Z)
            {
                Con_Printf("{0}\n", i);
            }
        }
    }
    public static void PR_LoadProgs()
    {
        FreeHandles();

        ClearState();
        _DynamicStrings.Clear();

        // flush the non-C variable lookup cache
        for (int i = 0; i < q_shared.GEFV_CACHESIZE; i++)
            gefvCache[i].field = null;

        CRC_Init(out pr_crc);

        byte[] buf = COM_LoadFile("progs.dat");

        progs = BytesToStructure<dprograms_t>(buf, 0);
        if (progs == null)
            Sys_Error("PR_LoadProgs: couldn't load progs.dat");
        Con_DPrintf("Programs occupy {0}K.\n", buf.Length / 1024);

        for (int i = 0; i < buf.Length; i++)
            CRC_ProcessByte(ref pr_crc, buf[i]);

        // byte swap the header
        progs.SwapBytes();

        if (progs.version != q_shared.PROG_VERSION)
            Sys_Error("progs.dat has wrong version number ({0} should be {1})", progs.version, q_shared.PROG_VERSION);
        if (progs.crc != q_shared.PROGHEADER_CRC)
            Sys_Error("progs.dat system vars have been modified, progdefs.h is out of date");

        // Functions
        pr_functions = new dfunction_t[progs.numfunctions];
        int offset = progs.ofs_functions;
        for (int i = 0; i < pr_functions.Length; i++, offset += dfunction_t.SizeInBytes)
        {
            pr_functions[i] = BytesToStructure<dfunction_t>(buf, offset);
            pr_functions[i].SwapBytes();
        }

        // strings
        offset = progs.ofs_strings;
        int str0 = offset;
        for (int i = 0; i < progs.numstrings; i++, offset++)
        {
            // count string length
            while (buf[offset] != 0)
                offset++;
        }
        int length = offset - str0;
        pr_strings = Encoding.ASCII.GetString(buf, str0, length);

        // Globaldefs
        pr_globaldefs = new ddef_t[progs.numglobaldefs];
        offset = progs.ofs_globaldefs;
        for (int i = 0; i < pr_globaldefs.Length; i++, offset += ddef_t.SizeInBytes)
        {
            pr_globaldefs[i] = BytesToStructure<ddef_t>(buf, offset);
            pr_globaldefs[i].SwapBytes();
        }

        // Fielddefs
        pr_fielddefs = new ddef_t[progs.numfielddefs];
        offset = progs.ofs_fielddefs;
        for (int i = 0; i < pr_fielddefs.Length; i++, offset += ddef_t.SizeInBytes)
        {
            pr_fielddefs[i] = BytesToStructure<ddef_t>(buf, offset);
            pr_fielddefs[i].SwapBytes();
            if ((pr_fielddefs[i].type & q_shared.DEF_SAVEGLOBAL) != 0)
                Sys_Error("PR_LoadProgs: pr_fielddefs[i].type & q_shared.DEF_SAVEGLOBAL");
        }

        // Statements
        pr_statements = new dstatement_t[progs.numstatements];
        offset = progs.ofs_statements;
        for (int i = 0; i < pr_statements.Length; i++, offset += dstatement_t.SizeInBytes)
        {
            pr_statements[i] = BytesToStructure<dstatement_t>(buf, offset);
            pr_statements[i].SwapBytes();
        }

        // Swap bytes inplace if needed
        if (!BitConverter.IsLittleEndian)
        {
            offset = progs.ofs_globals;
            for (int i = 0; i < progs.numglobals; i++, offset += 4)
            {
                SwapHelper.Swap4b(buf, offset);
            }
        }
        pr_global_struct = BytesToStructure<globalvars_t>(buf, progs.ofs_globals);
        _Globals = new float[progs.numglobals - globalvars_t.SizeInBytes / 4];
        Buffer.BlockCopy(buf, progs.ofs_globals + globalvars_t.SizeInBytes, _Globals, 0, _Globals.Length * 4);

        pr_edict_size = progs.entityfields * 4 + dedict_t.SizeInBytes - entvars_t.SizeInBytes;

        _HGlobals = GCHandle.Alloc(_Globals, GCHandleType.Pinned);
        _GlobalsAddr = _HGlobals.AddrOfPinnedObject().ToInt64();

        _HGlobalStruct = GCHandle.Alloc(pr_global_struct, GCHandleType.Pinned);
        _GlobalStructAddr = _HGlobalStruct.AddrOfPinnedObject().ToInt64();
    }
    public static void FreeHandles()
    {
        if (_HGlobals.IsAllocated)
        {
            _HGlobals.Free();
            _GlobalsAddr = 0;
        }
        if (_HGlobalStruct.IsAllocated)
        {
            _HGlobalStruct.Free();
            _GlobalStructAddr = 0;
        }
    }
    public static void ED_PrintEdict_f()
    {
        int i = atoi(Cmd_Argv(1));
        if (i >= sv.num_edicts)
        {
            Con_Printf("Bad edict number\n");
            return;
        }
        ED_PrintNum(i);
    }
    public static void ED_Count()
    {
        int active = 0, models = 0, solid = 0, step = 0;

        for (int i = 0; i < sv.num_edicts; i++)
        {
            edict_t ent = EDICT_NUM(i);
            if (ent.free)
                continue;
            active++;
            if (ent.v.solid != 0)
                solid++;
            if (ent.v.model != 0)
                models++;
            if (ent.v.movetype == q_shared.MOVETYPE_STEP)
                step++;
        }

        Con_Printf("num_edicts:{0}\n", sv.num_edicts);
        Con_Printf("active    :{0}\n", active);
        Con_Printf("view      :{0}\n", models);
        Con_Printf("touch     :{0}\n", solid);
        Con_Printf("step      :{0}\n", step);
    }
    public static void ED_PrintEdicts()
    {
        Con_Printf("{0} entities\n", sv.num_edicts);
        for (int i = 0; i < sv.num_edicts; i++)
            ED_PrintNum(i);
    }
    public static int StringOffset(string value)
    {
        string tmp = '\0' + value + '\0';
        int offset = pr_strings.IndexOf(tmp, StringComparison.Ordinal);
        if (offset != -1)
        {
            return MakeStingId(offset + 1, true);
        }

        for (int i = 0; i < _DynamicStrings.Count; i++)
        {
            if (_DynamicStrings[i] == value)
            {
                return MakeStingId(i, false);
            }
        }
        return -1;
    }
    public static void ED_LoadFromFile(string data)
    {
        edict_t ent = null;
        int inhibit = 0;
        pr_global_struct.time = (float)sv.time;

        // parse ents
        while (true)
        {
            // parse the opening brace	
            data = COM_Parse(data);
            if (data == null)
                break;

            if (com_token != "{")
                Sys_Error("ED_LoadFromFile: found {0} when expecting {", com_token);

            if (ent == null)
                ent = EDICT_NUM(0);
            else
                ent = ED_Alloc();
            data = ED_ParseEdict(data, ent);

            // remove things from different skill levels or deathmatch
            if (deathmatch.value != 0)
            {
                if (((int)ent.v.spawnflags & q_shared.SPAWNFLAG_NOT_DEATHMATCH) != 0)
                {
                    ED_Free(ent);
                    inhibit++;
                    continue;
                }
            }
            else if ((current_skill == 0 && ((int)ent.v.spawnflags & q_shared.SPAWNFLAG_NOT_EASY) != 0) ||
                (current_skill == 1 && ((int)ent.v.spawnflags & q_shared.SPAWNFLAG_NOT_MEDIUM) != 0) ||
                (current_skill >= 2 && ((int)ent.v.spawnflags & q_shared.SPAWNFLAG_NOT_HARD) != 0))
            {
                ED_Free(ent);
                inhibit++;
                continue;
            }

            //
            // immediately call spawn function
            //
            if (ent.v.classname == 0)
            {
                Con_Printf("No classname for:\n");
                ED_Print(ent);
                ED_Free(ent);
                continue;
            }

            // look for the spawn function
            int func = IndexOfFunction(GetString(ent.v.classname));
            if (func == -1)
            {
                Con_Printf("No spawn function for:\n");
                ED_Print(ent);
                ED_Free(ent);
                continue;
            }

            pr_global_struct.self = EDICT_TO_PROG(ent);
            PR_ExecuteProgram(func);
        }

        Con_DPrintf("{0} entities inhibited\n", inhibit);
    }
    static dfunction_t ED_FindFunction(string name)
    {
        int i = IndexOfFunction(name);
        if (i != -1)
            return pr_functions[i];

        return null;
    }
    static int IndexOfFunction(string name)
    {
        for (int i = 0; i < pr_functions.Length; i++)
        {
            if (SameName(pr_functions[i].s_name, name))
                return i;
        }
        return -1;
    }
    public static string ED_ParseEdict(string data, edict_t ent)
    {
        bool init = false;

        // clear it
        if (ent != sv.edicts[0])	// hack
            ent.Clear();

        // go through all the dictionary pairs
        bool anglehack;
        while (true)
        {
            // parse key
            data = COM_Parse(data);
            if (com_token.StartsWith("}"))
                break;

            if (data == null)
                Sys_Error("ED_ParseEntity: EOF without closing brace");

            string token = com_token;

            // anglehack is to allow QuakeEd to write single scalar angles
            // and allow them to be turned into vectors. (FIXME...)
            if (token == "angle")
            {
                token = "angles";
                anglehack = true;
            }
            else
                anglehack = false;

            // FIXME: change light to _light to get rid of this hack
            if (token == "light")
                token = "light_lev";	// hack for single light def

            string keyname = token.TrimEnd();

            // parse value	
            data = COM_Parse(data);
            if (data == null)
                Sys_Error("ED_ParseEntity: EOF without closing brace");

            if (com_token.StartsWith("}"))
                Sys_Error("ED_ParseEntity: closing brace without data");

            init = true;

            // keynames with a leading underscore are used for utility comments,
            // and are immediately discarded by quake
            if (keyname[0] == '_')
                continue;

            ddef_t key = ED_FindField(keyname);
            if (key == null)
            {
                Con_Printf("'{0}' is not a field\n", keyname);
                continue;
            }

            token = com_token;
            if (anglehack)
            {
                token = "0 " + token + " 0";
            }

            if (!ParsePair(ent, key, token))
                Host_Error("ED_ParseEdict: parse error");
        }

        if (!init)
            ent.free = true;

        return data;
    }
    static unsafe bool ParsePair(edict_t ent, ddef_t key, string s)
    {
        int offset1;
        if (ent.IsV(key.ofs, out offset1))
        {
            fixed (entvars_t* ptr = &ent.v)
            {
                return ED_ParseEpair((int*)ptr + offset1, key, s);
            }
        }
        else
            fixed (float* ptr = ent.fields)
            {
                return ED_ParseEpair(ptr + offset1, key, s);
            }
    }
    static unsafe bool ED_ParseEpair(void* value, ddef_t key, string s)
    {
        void* d = value;// (void *)((int *)base + key->ofs);

        switch ((etype_t)(key.type & ~q_shared.DEF_SAVEGLOBAL))
        {
            case etype_t.ev_string:
                *(int*)d = ED_NewString(s);// - pr_strings;
                break;

            case etype_t.ev_float:
                *(float*)d = atof(s);
                break;

            case etype_t.ev_vector:
                string[] vs = s.Split(' ');
                ((float*)d)[0] = atof(vs[0]);
                ((float*)d)[1] = (vs.Length > 1 ? atof(vs[1]) : 0);
                ((float*)d)[2] = (vs.Length > 2 ? atof(vs[2]) : 0);
                break;

            case etype_t.ev_entity:
                *(int*)d = EDICT_TO_PROG(EDICT_NUM(atoi(s)));
                break;

            case etype_t.ev_field:
                int f = IndexOfField(s);
                if (f == -1)
                {
                    Con_Printf("Can't find field {0}\n", s);
                    return false;
                }
                *(int*)d = GetInt32(pr_fielddefs[f].ofs);
                break;

            case etype_t.ev_function:
                int func = IndexOfFunction(s);
                if (func == -1)
                {
                    Con_Printf("Can't find function {0}\n", s);
                    return false;
                }
                *(int*)d = func;// - pr_functions;
                break;

            default:
                break;
        }
        return true;
    }
    static int IndexOfField(string name)
    {
        for (int i = 0; i < pr_fielddefs.Length; i++)
        {
            if (SameName(pr_fielddefs[i].s_name, name))
                return i;
        }
        return -1;
    }
    static bool IsGlobalStruct(int ofs, out int offset)
    {
        if (ofs < (globalvars_t.SizeInBytes >> 2))
        {
            offset = ofs;
            return true;
        }
        offset = ofs - (globalvars_t.SizeInBytes >> 2);
        return false;
    }
    static unsafe void* Get(int offset)
    {
        int offset1;
        if (IsGlobalStruct(offset, out offset1))
            return (int*)_GlobalStructAddr + offset1;
        return (int*)_GlobalsAddr + offset1;
    }
    static unsafe void Set(int offset, int value)
    {
        if (offset < (globalvars_t.SizeInBytes >> 2))
            *((int*)_GlobalStructAddr + offset) = value;
        else
            *((int*)_GlobalsAddr + offset - (globalvars_t.SizeInBytes >> 2)) = value;
    }
    static unsafe int GetInt32(int offset)
    {
        return *((int*)Get(offset));
    }
    static ddef_t ED_FindField(string name)
    {
        int i = IndexOfField(name);
        if (i != -1)
            return pr_fielddefs[i];

        return null;
    }
    public unsafe static void ED_Print(edict_t ed)
    {
        if (ed.free)
        {
            Con_Printf("FREE\n");
            return;
        }

        Con_Printf("\nEDICT {0}:\n", NUM_FOR_EDICT(ed));
        for (int i = 1; i < progs.numfielddefs; i++)
        {
            ddef_t d = pr_fielddefs[i];
            string name = GetString(d.s_name);

            if (name.Length > 2 && name[name.Length - 2] == '_')
                continue; // skip _x, _y, _z vars

            int type = d.type & ~q_shared.DEF_SAVEGLOBAL;
            int offset;
            if (ed.IsV(d.ofs, out offset))
            {
                fixed (void* ptr = &ed.v)
                {
                    int* v = (int*)ptr + offset;
                    if (IsEmptyField(type, v))
                        continue;

                    Con_Printf("{0,15} ", name);
                    Con_Printf("{0}\n", PR_ValueString((etype_t)d.type, (void*)v));
                }
            }
            else
            {
                fixed (void* ptr = ed.fields)
                {
                    int* v = (int*)ptr + offset;
                    if (IsEmptyField(type, v))
                        continue;

                    Con_Printf("{0,15} ", name);
                    Con_Printf("{0}\n", PR_ValueString((etype_t)d.type, (void*)v));
                }
            }
        }
    }
    static unsafe string PR_ValueString(etype_t type, void* val)
    {
        string result;
        type &= (etype_t)~q_shared.DEF_SAVEGLOBAL;

        switch (type)
        {
            case etype_t.ev_string:
                result = GetString(*(int*)val);
                break;

            case etype_t.ev_entity:
                result = "entity " + NUM_FOR_EDICT(PROG_TO_EDICT(*(int*)val));
                break;

            case etype_t.ev_function:
                dfunction_t f = pr_functions[*(int*)val];
                result = GetString(f.s_name) + "()";
                break;

            case etype_t.ev_field:
                ddef_t def = ED_FieldAtOfs(*(int*)val);
                result = "." + GetString(def.s_name);
                break;

            case etype_t.ev_void:
                result = "void";
                break;

            case etype_t.ev_float:
                result = (*(float*)val).ToString("F1", CultureInfo.InvariantCulture.NumberFormat);
                break;

            case etype_t.ev_vector:
                result = String.Format(CultureInfo.InvariantCulture.NumberFormat,
                    "{0,5:F1} {1,5:F1} {2,5:F1}", ((float*)val)[0], ((float*)val)[1], ((float*)val)[2]);
                break;

            case etype_t.ev_pointer:
                result = "pointer";
                break;

            default:
                result = "bad type " + type.ToString();
                break;
        }

        return result;
    }
    static int IndexOfField(int ofs)
    {
        for (int i = 0; i < pr_fielddefs.Length; i++)
        {
            if (pr_fielddefs[i].ofs == ofs)
                return i;
        }
        return -1;
    }
    static ddef_t ED_FieldAtOfs(int ofs)
    {
        int i = IndexOfField(ofs);
        if (i != -1)
            return pr_fielddefs[i];

        return null;
    }
    public static string GetString(int strId)
    {
        int offset;
        if (IsStaticString(strId, out offset))
        {
            int i0 = offset;
            while (offset < pr_strings.Length && pr_strings[offset] != 0)
                offset++;

            int length = offset - i0;
            if (length > 0)
                return pr_strings.Substring(i0, length);
        }
        else
        {
            if (offset < 0 || offset >= _DynamicStrings.Count)
            {
                throw new ArgumentException("Invalid string id!");
            }
            return _DynamicStrings[offset];
        }

        return String.Empty;
    }
    public static bool SameName(int name1, string name2)
    {
        int offset = name1;
        if (offset + name2.Length > pr_strings.Length)
            return false;

        for (int i = 0; i < name2.Length; i++, offset++)
            if (pr_strings[offset] != name2[i])
                return false;

        if (offset < pr_strings.Length && pr_strings[offset] != 0)
            return false;

        return true;
    }
    public static int ED_NewString(string s)
    {
        int id = AllocString();
        StringBuilder sb = new StringBuilder(s.Length);
        int len = s.Length;
        for (int i = 0; i < len; i++)
        {
            if (s[i] == '\\' && i < len - 1)
            {
                i++;
                if (s[i] == 'n')
                    sb.Append('\n');
                else
                    sb.Append('\\');
            }
            else
                sb.Append(s[i]);
        }
        SetString(id, sb.ToString());
        return id;
    }
    static ddef_t CachedSearch(edict_t ed, string field)
    {
        ddef_t def = null;
        for (int i = 0; i < q_shared.GEFV_CACHESIZE; i++)
        {
            if (field == gefvCache[i].field)
            {
                def = gefvCache[i].pcache;
                return def;
            }
        }

        def = ED_FindField(field);

        gefvCache[_gefvPos].pcache = def;
        gefvCache[_gefvPos].field = field;
        _gefvPos ^= 1;

        return def;
    }
    public static float GetEdictFieldFloat(edict_t ed, string field, float defValue = 0)
    {
        ddef_t def = CachedSearch(ed, field);
        if (def == null)
            return defValue;

        return ed.GetFloat(def.ofs);
    }
    public static bool SetEdictFieldFloat(edict_t ed, string field, float value)
    {
        ddef_t def = CachedSearch(ed, field);
        if (def != null)
        {
            ed.SetFloat(def.ofs, value);
            return true;
        }
        return false;
    }
    static int MakeStingId(int index, bool isStatic)
    {
        return ((isStatic ? 0 : 1) << 24) + (index & 0xFFFFFF);
    }
    static bool IsStaticString(int stringId, out int offset)
    {
        offset = stringId & 0xFFFFFF;
        return ((stringId >> 24) & 1) == 0;
    }
    public static int AllocString()
    {
        int id = _DynamicStrings.Count;
        _DynamicStrings.Add(String.Empty);
        return MakeStingId(id, false);
    }
    public static void SetString(int id, string value)
    {
        int offset;
        if (IsStaticString(id, out offset))
        {
            throw new ArgumentException("Static strings are read-only!");
        }
        if (offset < 0 || offset >= _DynamicStrings.Count)
        {
            throw new ArgumentException("Invalid string id!");
        }
        _DynamicStrings[offset] = value;
    }
    public unsafe static void ED_WriteGlobals(StreamWriter writer)
    {
        writer.WriteLine("{");
        for (int i = 0; i < progs.numglobaldefs; i++)
        {
            ddef_t def = pr_globaldefs[i];
            etype_t type = (etype_t)def.type;
            if ((def.type & q_shared.DEF_SAVEGLOBAL) == 0)
                continue;

            type &= (etype_t)~q_shared.DEF_SAVEGLOBAL;

            if (type != etype_t.ev_string && type != etype_t.ev_float && type != etype_t.ev_entity)
                continue;

            writer.Write("\"");
            writer.Write(GetString(def.s_name));
            writer.Write("\" \"");
            writer.Write(PR_UglyValueString(type, (eval_t*)Get(def.ofs)));
            writer.WriteLine("\"");
        }
        writer.WriteLine("}");
    }
    static unsafe string PR_UglyValueString(etype_t type, eval_t* val)
    {
        type &= (etype_t)~q_shared.DEF_SAVEGLOBAL;
        string result;

        switch (type)
        {
            case etype_t.ev_string:
                result = GetString(val->_string);
                break;

            case etype_t.ev_entity:
                result = NUM_FOR_EDICT(PROG_TO_EDICT(val->edict)).ToString();
                break;

            case etype_t.ev_function:
                dfunction_t f = pr_functions[val->function];
                result = GetString(f.s_name);
                break;

            case etype_t.ev_field:
                ddef_t def = ED_FieldAtOfs(val->_int);
                result = GetString(def.s_name);
                break;

            case etype_t.ev_void:
                result = "void";
                break;

            case etype_t.ev_float:
                result = val->_float.ToString("F6", CultureInfo.InvariantCulture.NumberFormat);
                break;

            case etype_t.ev_vector:
                result = String.Format(CultureInfo.InvariantCulture.NumberFormat,
                    "{0:F6} {1:F6} {2:F6}", val->vector[0], val->vector[1], val->vector[2]);
                break;

            default:
                result = "bad type " + type.ToString();
                break;
        }

        return result;
    }
    public unsafe static void ED_Write(StreamWriter writer, edict_t ed)
    {
        writer.WriteLine("{");

        if (ed.free)
        {
            writer.WriteLine("}");
            return;
        }

        for (int i = 1; i < progs.numfielddefs; i++)
        {
            ddef_t d = pr_fielddefs[i];
            string name = GetString(d.s_name);
            if (name != null && name.Length > 2 && name[name.Length - 2] == '_')// [strlen(name) - 2] == '_')
                continue;	// skip _x, _y, _z vars

            int type = d.type & ~q_shared.DEF_SAVEGLOBAL;
            int offset1;
            if (ed.IsV(d.ofs, out offset1))
            {
                fixed (void* ptr = &ed.v)
                {
                    int* v = (int*)ptr + offset1;
                    if (IsEmptyField(type, v))
                        continue;

                    writer.WriteLine("\"{0}\" \"{1}\"", name, PR_UglyValueString((etype_t)d.type, (eval_t*)v));
                }
            }
            else
            {
                fixed (void* ptr = ed.fields)
                {
                    int* v = (int*)ptr + offset1;
                    if (IsEmptyField(type, v))
                        continue;

                    writer.WriteLine("\"{0}\" \"{1}\"", name, PR_UglyValueString((etype_t)d.type, (eval_t*)v));
                }
            }
        }

        writer.WriteLine("}");
    }
    static unsafe bool IsEmptyField(int type, int* v)
    {
        for (int j = 0; j < type_size[type]; j++)
            if (v[j] != 0)
                return false;

        return true;
    }
    public static void ED_ParseGlobals(string data)
    {
        while (true)
        {
            // parse key
            data = COM_Parse(data);
            if (com_token.StartsWith("}"))
                break;

            if (String.IsNullOrEmpty(data))
                Sys_Error("ED_ParseEntity: EOF without closing brace");

            string keyname = com_token;

            // parse value	
            data = COM_Parse(data);
            if (String.IsNullOrEmpty(data))
                Sys_Error("ED_ParseEntity: EOF without closing brace");

            if (com_token.StartsWith("}"))
                Sys_Error("ED_ParseEntity: closing brace without data");

            ddef_t key = ED_FindGlobal(keyname);
            if (key == null)
            {
                Con_Printf("'{0}' is not a global\n", keyname);
                continue;
            }

            if (!ParseGlobalPair(key, com_token))
                Host_Error("ED_ParseGlobals: parse error");
        }
    }
    static ddef_t ED_FindGlobal(string name)
    {
        for (int i = 0; i < pr_globaldefs.Length; i++)
        {
            ddef_t def = pr_globaldefs[i];
            if (name == GetString(def.s_name))
                return def;
        }
        return null;
    }
    static unsafe bool ParseGlobalPair(ddef_t key, string value)
    {
        int offset;
        if (IsGlobalStruct(key.ofs, out offset))
        {
            return ED_ParseEpair((float*)_GlobalStructAddr + offset, key, value);
        }
        return ED_ParseEpair((float*)_GlobalsAddr + offset, key, value);
    }
    public static void ED_PrintNum(int ent)
    {
        ED_Print(EDICT_NUM(ent));
    }
    static unsafe string PR_GlobalString(int ofs)
    {
        string line = String.Empty;
        void* val = Get(ofs);// (void*)&pr_globals[ofs];
        ddef_t def = ED_GlobalAtOfs(ofs);
        if (def == null)
            line = String.Format("{0}(???)", ofs);
        else
        {
            string s = PR_ValueString((etype_t)def.type, val);
            line = String.Format("{0}({1}){2} ", ofs, GetString(def.s_name), s);
        }

        line = line.PadRight(20);

        return line;
    }
    static string PR_GlobalStringNoContents(int ofs)
    {
        string line = String.Empty;
        ddef_t def = ED_GlobalAtOfs(ofs);
        if (def == null)
            line = String.Format("{0}(???)", ofs);
        else
            line = String.Format("{0}({1}) ", ofs, GetString(def.s_name));

        line = line.PadRight(20);

        return line;
    }
    static ddef_t ED_GlobalAtOfs(int ofs)
    {
        for (int i = 0; i < pr_globaldefs.Length; i++)
        {
            ddef_t def = pr_globaldefs[i];
            if (def.ofs == ofs)
                return def;
        }
        return null;
    }
}