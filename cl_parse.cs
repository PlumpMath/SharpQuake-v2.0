using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    const string ConsoleBar = "\n\n\u001D\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001E\u001F\n\n";
        
    static string[] svc_strings = new string[]
    {
	    "svc_bad",
	    "svc_nop",
	    "svc_disconnect",
	    "svc_updatestat",
	    "svc_version",		// [long] server version
	    "svc_setview",		// [short] entity number
	    "svc_sound",			// <see code>
	    "svc_time",			// [float] server time
	    "svc_print",			// [string] null terminated string
	    "svc_stufftext",		// [string] stuffed into client's console buffer
						    // the string should be \n terminated
	    "svc_setangle",		// [vec3] set the view angle to this absolute value
	
	    "svc_serverinfo",		// [long] version
						    // [string] signon string
						    // [string]..[0]model cache [string]...[0]sounds cache
						    // [string]..[0]item cache
	    "svc_lightstyle",		// [byte] [string]
	    "svc_updatename",		// [byte] [string]
	    "svc_updatefrags",	// [byte] [short]
	    "svc_clientdata",		// <shortbits + data>
	    "svc_stopsound",		// <see code>
	    "svc_updatecolors",	// [byte] [byte]
	    "svc_particle",		// [vec3] <variable>
	    "svc_damage",			// [byte] impact [byte] blood [vec3] from
	
	    "svc_spawnstatic",
	    "OBSOLETE svc_spawnbinary",
	    "svc_spawnbaseline",
	
	    "svc_temp_entity",		// <variable>
	    "svc_setpause",
	    "svc_signonnum",
	    "svc_centerprint",
	    "svc_killedmonster",
	    "svc_foundsecret",
	    "svc_spawnstaticsound",
	    "svc_intermission",
	    "svc_finale",			// [string] music [string] text
	    "svc_cdtrack",			// [byte] track [byte] looptrack
	    "svc_sellscreen",
	    "svc_cutscene"
    };

    static int[] bitcounts = new int[16];
    static object _MsgState; // used by KeepaliveMessage function
    static float lastmsg;
        
    static void CL_ParseServerMessage()
    {
        //
        // if recording demos, copy the message out
        //
        if (cl_shownet.value == 1)
            Con_Printf("{0} ", Message.Length);
        else if (cl_shownet.value == 2)
            Con_Printf("------------------\n");

        cl.onground = false;	// unless the server says otherwise	

        //
        // parse the message
        //
        Reader.MSG_BeginReading();
        int i;
        while (true)
        {
            if (Reader.msg_badread)
                Host_Error("CL_ParseServerMessage: Bad server message");

            int cmd = Reader.MSG_ReadByte();
            if (cmd == -1)
            {
                ShowNet("END OF MESSAGE");
                return;	// end of message
            }

            // if the high bit of the command byte is set, it is a fast update
            if ((cmd & 128) != 0)
            {
                ShowNet("fast update");
                CL_ParseUpdate(cmd & 127);
                continue;
            }

            ShowNet(svc_strings[cmd]);

            // other commands
            switch (cmd)
            {
                default:
                    Host_Error("CL_ParseServerMessage: Illegible server message\n");
                    break;

                case q_shared.svc_nop:
                    break;

                case q_shared.svc_time:
                    cl.mtime[1] = cl.mtime[0];
                    cl.mtime[0] = Reader.MSG_ReadFloat();
                    break;

                case q_shared.svc_clientdata:
                    i = Reader.MSG_ReadShort();
                    CL_ParseClientdata(i);
                    break;

                case q_shared.svc_version:
                    i = Reader.MSG_ReadLong();
                    if (i != q_shared.PROTOCOL_VERSION)
                        Host_Error("CL_ParseServerMessage: Server is protocol {0} instead of {1}\n", i, q_shared.PROTOCOL_VERSION);
                    break;

                case q_shared.svc_disconnect:
                    Host_EndGame("Server disconnected\n");
                    break;

                case q_shared.svc_print:
                    Con_Printf(Reader.MSG_ReadString());
                    break;

                case q_shared.svc_centerprint:
                    SCR_CenterPrint(Reader.MSG_ReadString());
                    break;

                case q_shared.svc_stufftext:
                    Cbuf_AddText(Reader.MSG_ReadString());
                    break;

                case q_shared.svc_damage:
                    game_engine.V_ParseDamage();
                    break;

                case q_shared.svc_serverinfo:
                    CL_ParseServerInfo();
                    vid.recalc_refdef = true;	// leave intermission full screen
                    break;

                case q_shared.svc_setangle:
                    cl.viewangles.X = Reader.MSG_ReadAngle();
                    cl.viewangles.Y = Reader.MSG_ReadAngle();
                    cl.viewangles.Z = Reader.MSG_ReadAngle();
                    break;

                case q_shared.svc_setview:
                    cl.viewentity = Reader.MSG_ReadShort();
                    break;

                case q_shared.svc_lightstyle:
                    i = Reader.MSG_ReadByte();
                    if (i >= q_shared.MAX_LIGHTSTYLES)
                        Sys_Error("svc_lightstyle > MAX_LIGHTSTYLES");
                    cl_lightstyle[i].map = Reader.MSG_ReadString();
                    break;

                case q_shared.svc_sound:
                    CL_ParseStartSoundPacket();
                    break;

                case q_shared.svc_stopsound:
                    i = Reader.MSG_ReadShort();
                    S_StopSound(i >> 3, i & 7);
                    break;

                case q_shared.svc_updatename:
                    Sbar_Changed();
                    i = Reader.MSG_ReadByte();
                    if (i >= cl.maxclients)
                        Host_Error("CL_ParseServerMessage: svc_updatename > MAX_SCOREBOARD");
                    cl.scores[i].name = Reader.MSG_ReadString();
                    break;

                case q_shared.svc_updatefrags:
                    Sbar_Changed();
                    i = Reader.MSG_ReadByte();
                    if (i >= cl.maxclients)
                        Host_Error("CL_ParseServerMessage: svc_updatefrags > MAX_SCOREBOARD");
                    cl.scores[i].frags = Reader.MSG_ReadShort();
                    break;

                case q_shared.svc_updatecolors:
                    Sbar_Changed();
                    i = Reader.MSG_ReadByte();
                    if (i >= cl.maxclients)
                        Host_Error("CL_ParseServerMessage: svc_updatecolors > MAX_SCOREBOARD");
                    cl.scores[i].colors = Reader.MSG_ReadByte();
                    CL_NewTranslation(i);
                    break;

                case q_shared.svc_particle:
                    R_ParseParticleEffect();
                    break;

                case q_shared.svc_spawnbaseline:
                    i = Reader.MSG_ReadShort();
                    // must use CL_EntityNum() to force cl.num_entities up
                    CL_ParseBaseline(CL_EntityNum(i));
                    break;

                case q_shared.svc_spawnstatic:
                    CL_ParseStatic();
                    break;

                case q_shared.svc_temp_entity:
                    CL_ParseTEnt();
                    break;

                case q_shared.svc_setpause:
                    {
                        cl.paused = Reader.MSG_ReadByte() != 0;

                        if (cl.paused)
                        {
                            CDAudio_Pause();
                        }
                        else
                        {
                            CDAudio_Resume();
                        }
                    }
                    break;

                case q_shared.svc_signonnum:
                    i = Reader.MSG_ReadByte();
                    if (i <= cls.signon)
                        Host_Error("Received signon {0} when at {1}", i, cls.signon);
                    cls.signon = i;
                    CL_SignonReply();
                    break;

                case q_shared.svc_killedmonster:
                    cl.stats[q_shared.STAT_MONSTERS]++;
                    break;

                case q_shared.svc_foundsecret:
                    cl.stats[q_shared.STAT_SECRETS]++;
                    break;

                case q_shared.svc_updatestat:
                    i = Reader.MSG_ReadByte();
                    if (i < 0 || i >= q_shared.MAX_CL_STATS)
                        Sys_Error("svc_updatestat: {0} is invalid", i);
                    cl.stats[i] = Reader.MSG_ReadLong();
                    break;

                case q_shared.svc_spawnstaticsound:
                    CL_ParseStaticSound();
                    break;

                case q_shared.svc_cdtrack:
                    cl.cdtrack = Reader.MSG_ReadByte();
                    cl.looptrack = Reader.MSG_ReadByte();
                    if ((cls.demoplayback || cls.demorecording) && (cls.forcetrack != -1))
                        CDAudio_Play((byte)cls.forcetrack, true);
                    else
                        CDAudio_Play((byte)cl.cdtrack, true);
                    break;

                case q_shared.svc_intermission:
                    cl.intermission = 1;
                    cl.completed_time = (int)cl.time;
                    vid.recalc_refdef = true;	// go to full screen
                    break;

                case q_shared.svc_finale:
                    cl.intermission = 2;
                    cl.completed_time = (int)cl.time;
                    vid.recalc_refdef = true;	// go to full screen
                    SCR_CenterPrint(Reader.MSG_ReadString());
                    break;

                case q_shared.svc_cutscene:
                    cl.intermission = 3;
                    cl.completed_time = (int)cl.time;
                    vid.recalc_refdef = true;	// go to full screen
                    SCR_CenterPrint(Reader.MSG_ReadString());
                    break;

                case q_shared.svc_sellscreen:
                    Cmd_ExecuteString("help", cmd_source_t.src_command);
                    break;
            }
        }
    }
    static void ShowNet(string s)
    {
        if (cl_shownet.value == 2)
            Con_Printf("{0,3}:{1}\n", Reader.msg_readcount - 1, s);
    }    
    static void CL_ParseUpdate(int bits)
    {
        int i;

        if (cls.signon == q_shared.SIGNONS - 1)
        {
            // first update is the final signon stage
            cls.signon = q_shared.SIGNONS;
            CL_SignonReply();
        }

        if ((bits & q_shared.U_MOREBITS) != 0)
        {
            i = Reader.MSG_ReadByte();
            bits |= (i << 8);
        }

        int num;

        if ((bits & q_shared.U_LONGENTITY) != 0)
            num = Reader.MSG_ReadShort();
        else
            num = Reader.MSG_ReadByte();

        entity_t ent = CL_EntityNum(num);
        for (i = 0; i < 16; i++)
            if ((bits & (1 << i)) != 0)
                bitcounts[i]++;

        bool forcelink = false;
        if (ent.msgtime != cl.mtime[1])
            forcelink = true;	// no previous frame to lerp from

        ent.msgtime = cl.mtime[0];
        int modnum;
        if ((bits & q_shared.U_MODEL) != 0)
        {
            modnum = Reader.MSG_ReadByte();
            if (modnum >= q_shared.MAX_MODELS)
                Host_Error("CL_ParseModel: bad modnum");
        }
        else
            modnum = ent.baseline.modelindex;

        model_t model = cl.model_precache[modnum];
        if (model != ent.model)
        {
            ent.model = model;
            // automatic animation (torches, etc) can be either all together
            // or randomized
            if (model != null)
            {
                if (model.synctype == synctype_t.ST_RAND)
                    ent.syncbase = (float)(Random() & 0x7fff) / 0x7fff;
                else
                    ent.syncbase = 0;
            }
            else
                forcelink = true;	// hack to make null model players work

            if (num > 0 && num <= cl.maxclients)
                R_TranslatePlayerSkin(num - 1);
        }

        if ((bits & q_shared.U_FRAME) != 0)
            ent.frame = Reader.MSG_ReadByte();
        else
            ent.frame = ent.baseline.frame;

        if ((bits & q_shared.U_COLORMAP) != 0)
            i = Reader.MSG_ReadByte();
        else
            i = ent.baseline.colormap;
        if (i == 0)
            ent.colormap = vid.colormap;
        else
        {
            if (i > cl.maxclients)
                Sys_Error("i >= cl.maxclients");
            ent.colormap = cl.scores[i - 1].translations;
        }

        int skin;
        if ((bits & q_shared.U_SKIN) != 0)
            skin = Reader.MSG_ReadByte();
        else
            skin = ent.baseline.skin;
        if (skin != ent.skinnum)
        {
            ent.skinnum = skin;
            if (num > 0 && num <= cl.maxclients)
                R_TranslatePlayerSkin(num - 1);
        }

        if ((bits & q_shared.U_EFFECTS) != 0)
            ent.effects = Reader.MSG_ReadByte();
        else
            ent.effects = ent.baseline.effects;

        // shift the known values for interpolation
        ent.msg_origins[1] = ent.msg_origins[0];
        ent.msg_angles[1] = ent.msg_angles[0];

        if ((bits & q_shared.U_ORIGIN1) != 0)
            ent.msg_origins[0].X = Reader.MSG_ReadCoord();
        else
            ent.msg_origins[0].X = ent.baseline.origin.x;
        if ((bits & q_shared.U_ANGLE1) != 0)
            ent.msg_angles[0].X = Reader.MSG_ReadAngle();
        else
            ent.msg_angles[0].X = ent.baseline.angles.x;

        if ((bits & q_shared.U_ORIGIN2) != 0)
            ent.msg_origins[0].Y = Reader.MSG_ReadCoord();
        else
            ent.msg_origins[0].Y = ent.baseline.origin.y;
        if ((bits & q_shared.U_ANGLE2) != 0)
            ent.msg_angles[0].Y = Reader.MSG_ReadAngle();
        else
            ent.msg_angles[0].Y = ent.baseline.angles.y;

        if ((bits & q_shared.U_ORIGIN3) != 0)
            ent.msg_origins[0].Z = Reader.MSG_ReadCoord();
        else
            ent.msg_origins[0].Z = ent.baseline.origin.z;
        if ((bits & q_shared.U_ANGLE3) != 0)
            ent.msg_angles[0].Z = Reader.MSG_ReadAngle();
        else
            ent.msg_angles[0].Z = ent.baseline.angles.z;

        if ((bits & q_shared.U_NOLERP) != 0)
            ent.forcelink = true;

        if (forcelink)
        {	// didn't have an update last message
            ent.msg_origins[1] = ent.msg_origins[0];
            ent.origin = ent.msg_origins[0];
            ent.msg_angles[1] = ent.msg_angles[0];
            ent.angles = ent.msg_angles[0];
            ent.forcelink = true;
        }
    }        
    static void CL_ParseClientdata(int bits)
    {
        if ((bits & q_shared.SU_VIEWHEIGHT) != 0)
            cl.viewheight = Reader.MSG_ReadChar();
        else
            cl.viewheight = q_shared.DEFAULT_VIEWHEIGHT;

        if ((bits & q_shared.SU_IDEALPITCH) != 0)
            cl.idealpitch = Reader.MSG_ReadChar();
        else
            cl.idealpitch = 0;

        cl.mvelocity[1] = cl.mvelocity[0];
        for (int i = 0; i < 3; i++)
        {
            if ((bits & (q_shared.SU_PUNCH1 << i)) != 0)
                Mathlib.SetComp(ref cl.punchangle, i, Reader.MSG_ReadChar());
            else
                Mathlib.SetComp(ref cl.punchangle, i, 0);
            if ((bits & (q_shared.SU_VELOCITY1 << i)) != 0)
                Mathlib.SetComp(ref cl.mvelocity[0], i, Reader.MSG_ReadChar() * 16);
            else
                Mathlib.SetComp(ref cl.mvelocity[0], i, 0);
        }

        // [always sent]	if (bits & SU_ITEMS)
        int i2 = Reader.MSG_ReadLong();

        if (cl.items != i2)
        {	// set flash times
            Sbar_Changed();
            for (int j = 0; j < 32; j++)
                if ((i2 & (1 << j)) != 0 && (cl.items & (1 << j)) == 0)
                    cl.item_gettime[j] = (float)cl.time;
            cl.items = i2;
        }

        cl.onground = (bits & q_shared.SU_ONGROUND) != 0;
        cl.inwater = (bits & q_shared.SU_INWATER) != 0;

        if ((bits & q_shared.SU_WEAPONFRAME) != 0)
            cl.stats[q_shared.STAT_WEAPONFRAME] = Reader.MSG_ReadByte();
        else
            cl.stats[q_shared.STAT_WEAPONFRAME] = 0;

        if ((bits & q_shared.SU_ARMOR) != 0)
            i2 = Reader.MSG_ReadByte();
        else
            i2 = 0;
        if (cl.stats[q_shared.STAT_ARMOR] != i2)
        {
            cl.stats[q_shared.STAT_ARMOR] = i2;
            Sbar_Changed();
        }

        if ((bits & q_shared.SU_WEAPON) != 0)
            i2 = Reader.MSG_ReadByte();
        else
            i2 = 0;
        if (cl.stats[q_shared.STAT_WEAPON] != i2)
        {
            cl.stats[q_shared.STAT_WEAPON] = i2;
            Sbar_Changed();
        }

        i2 = Reader.MSG_ReadShort();
        if (cl.stats[q_shared.STAT_HEALTH] != i2)
        {
            cl.stats[q_shared.STAT_HEALTH] = i2;
            Sbar_Changed();
        }

        i2 = Reader.MSG_ReadByte();
        if (cl.stats[q_shared.STAT_AMMO] != i2)
        {
            cl.stats[q_shared.STAT_AMMO] = i2;
            Sbar_Changed();
        }

        for (i2 = 0; i2 < 4; i2++)
        {
            int j = Reader.MSG_ReadByte();
            if (cl.stats[q_shared.STAT_SHELLS + i2] != j)
            {
                cl.stats[q_shared.STAT_SHELLS + i2] = j;
                Sbar_Changed();
            }
        }

        i2 = Reader.MSG_ReadByte();

        if (_GameKind == GameKind.StandardQuake)
        {
            if (cl.stats[q_shared.STAT_ACTIVEWEAPON] != i2)
            {
                cl.stats[q_shared.STAT_ACTIVEWEAPON] = i2;
                Sbar_Changed();
            }
        }
        else
        {
            if (cl.stats[q_shared.STAT_ACTIVEWEAPON] != (1 << i2))
            {
                cl.stats[q_shared.STAT_ACTIVEWEAPON] = (1 << i2);
                Sbar_Changed();
            }
        }
    }    
    static void CL_ParseServerInfo()
    {
        Con_DPrintf("Serverinfo packet received.\n");

        //
        // wipe the client_state_t struct
        //
        CL_ClearState();

        // parse protocol version number
        int i = Reader.MSG_ReadLong();
        if (i != q_shared.PROTOCOL_VERSION)
        {
            Con_Printf("Server returned version {0}, not {1}", i, q_shared.PROTOCOL_VERSION);
            return;
        }

        // parse maxclients
        cl.maxclients = Reader.MSG_ReadByte();
        if (cl.maxclients < 1 || cl.maxclients > q_shared.MAX_SCOREBOARD)
        {
            Con_Printf("Bad maxclients ({0}) from server\n", cl.maxclients);
            return;
        }
        cl.scores = new scoreboard_t[cl.maxclients];// Hunk_AllocName (cl.maxclients*sizeof(*cl.scores), "scores");
        for (i = 0; i < cl.scores.Length; i++)
            cl.scores[i] = new scoreboard_t();

        // parse gametype
        cl.gametype = Reader.MSG_ReadByte();

        // parse signon message
        string str = Reader.MSG_ReadString();
        cl.levelname = Copy(str, 40);

        // seperate the printfs so the server message can have a color
        Con_Printf(ConsoleBar);
        Con_Printf("{0}{1}\n", (char)2, str);

        //
        // first we go through and touch all of the precache data that still
        // happens to be in the cache, so precaching something else doesn't
        // needlessly purge it
        //

        // precache models
        Array.Clear(cl.model_precache, 0, cl.model_precache.Length);
        int nummodels;
        string[] model_precache = new string[q_shared.MAX_MODELS];
        for (nummodels = 1; ; nummodels++)
        {
            str = Reader.MSG_ReadString();
            if (String.IsNullOrEmpty(str))
                break;

            if (nummodels == q_shared.MAX_MODELS)
            {
                Con_Printf("Server sent too many model precaches\n");
                return;
            }
            model_precache[nummodels] = str;
            Mod_TouchModel(str);
        }

        // precache sounds
        Array.Clear(cl.sound_precache, 0, cl.sound_precache.Length);
        int numsounds;
        string[] sound_precache = new string[q_shared.MAX_SOUNDS];
        for (numsounds = 1; ; numsounds++)
        {
            str = Reader.MSG_ReadString();
            if (String.IsNullOrEmpty(str))
                break;
            if (numsounds == q_shared.MAX_SOUNDS)
            {
                Con_Printf("Server sent too many sound precaches\n");
                return;
            }
            sound_precache[numsounds] = str;
            S_TouchSound(str);
        }

        //
        // now we try to load everything else until a cache allocation fails
        //
        for (i = 1; i < nummodels; i++)
        {
            cl.model_precache[i] = Mod_ForName(model_precache[i], false);
            if (cl.model_precache[i] == null)
            {
                Con_Printf("Model {0} not found\n", model_precache[i]);
                return;
            }
            CL_KeepaliveMessage();
        }

        S_BeginPrecaching();
        for (i = 1; i < numsounds; i++)
        {
            cl.sound_precache[i] = S_PrecacheSound(sound_precache[i]);
            CL_KeepaliveMessage();
        }
        S_EndPrecaching();

        // local state
        cl_entities[0].model = cl.worldmodel = cl.model_precache[1];

        R_NewMap();

        noclip_anglehack = false; // noclip is turned off at start	

        GC.Collect();
    }
    static void CL_ParseStartSoundPacket()
    {
        int field_mask = Reader.MSG_ReadByte();
        int volume;
        float attenuation;

        if ((field_mask & q_shared.SND_VOLUME) != 0)
            volume = Reader.MSG_ReadByte();
        else
            volume = q_shared.DEFAULT_SOUND_PACKET_VOLUME;

        if ((field_mask & q_shared.SND_ATTENUATION) != 0)
            attenuation = Reader.MSG_ReadByte() / 64.0f;
        else
            attenuation = q_shared.DEFAULT_SOUND_PACKET_ATTENUATION;

        int channel = Reader.MSG_ReadShort();
        int sound_num = Reader.MSG_ReadByte();

        int ent = channel >> 3;
        channel &= 7;

        if (ent > q_shared.MAX_EDICTS)
            Host_Error("CL_ParseStartSoundPacket: ent = {0}", ent);

        Vector3 pos = Reader.ReadCoords();
        S_StartSound(ent, channel, cl.sound_precache[sound_num], ref pos, volume / 255.0f, attenuation);
    }
    static void CL_NewTranslation(int slot)
    {
        if (slot > cl.maxclients)
            Sys_Error("CL_NewTranslation: slot > cl.maxclients");

        byte[] dest = cl.scores[slot].translations;
        byte[] source = vid.colormap;
        Array.Copy(source, dest, dest.Length);

        int top = cl.scores[slot].colors & 0xf0;
        int bottom = (cl.scores[slot].colors & 15) << 4;

        R_TranslatePlayerSkin(slot);

        for (int i = 0, offset = 0; i < q_shared.VID_GRADES; i++)//, dest += 256, source+=256)
        {
            if (top < 128)	// the artists made some backwards ranges.  sigh.
                Buffer.BlockCopy(source, offset + top, dest, offset + TOP_RANGE, 16);  //memcpy (dest + Render.TOP_RANGE, source + top, 16);
            else
                for (int j = 0; j < 16; j++)
                    dest[offset + TOP_RANGE + j] = source[offset + top + 15 - j];

            if (bottom < 128)
                Buffer.BlockCopy(source, offset + bottom, dest, offset + BOTTOM_RANGE, 16); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
            else
                for (int j = 0; j < 16; j++)
                    dest[offset + BOTTOM_RANGE + j] = source[offset + bottom + 15 - j];

            offset += 256;
        }
    }
    static entity_t CL_EntityNum(int num)
    {
        if (num >= cl.num_entities)
        {
            if (num >= q_shared.MAX_EDICTS)
                Host_Error("CL_EntityNum: %i is an invalid number", num);
            while (cl.num_entities <= num)
            {
                cl_entities[cl.num_entities].colormap = vid.colormap;
                cl.num_entities++;
            }
        }

        return cl_entities[num];
    }
    static void CL_ParseBaseline(entity_t ent)
    {
        ent.baseline.modelindex = Reader.MSG_ReadByte();
        ent.baseline.frame = Reader.MSG_ReadByte();
        ent.baseline.colormap = Reader.MSG_ReadByte();
        ent.baseline.skin = Reader.MSG_ReadByte();
        ent.baseline.origin.x = Reader.MSG_ReadCoord();
        ent.baseline.angles.x = Reader.MSG_ReadAngle();
        ent.baseline.origin.y = Reader.MSG_ReadCoord();
        ent.baseline.angles.y = Reader.MSG_ReadAngle();
        ent.baseline.origin.z = Reader.MSG_ReadCoord();
        ent.baseline.angles.z = Reader.MSG_ReadAngle();
    }
    static void CL_ParseStatic()
    {
        int i = cl.num_statics;
        if (i >= q_shared.MAX_STATIC_ENTITIES)
            Host_Error("Too many static entities");

        entity_t ent = cl_static_entities[i];
        cl.num_statics++;
        CL_ParseBaseline(ent);

        // copy it to the current state
        ent.model = cl.model_precache[ent.baseline.modelindex];
        ent.frame = ent.baseline.frame;
        ent.colormap = vid.colormap;
        ent.skinnum = ent.baseline.skin;
        ent.effects = ent.baseline.effects;
        ent.origin = ToVector(ref ent.baseline.origin);
        ent.angles = ToVector(ref ent.baseline.angles);
        R_AddEfrags(ent);
    }
    static void CL_ParseStaticSound()
    {
        Vector3 org = Reader.ReadCoords();
        int sound_num = Reader.MSG_ReadByte();
        int vol = Reader.MSG_ReadByte();
        int atten = Reader.MSG_ReadByte();

        S_StaticSound(cl.sound_precache[sound_num], ref org, vol, atten);
    }
    static void CL_KeepaliveMessage()
    {
        if (sv.active)
            return;	// no need if server is local
        if (cls.demoplayback)
            return;

        // read messages from server, should just be nops
        Message.SaveState(ref _MsgState);

        int ret;
        do
        {
            ret = CL_GetMessage();
            switch (ret)
            {
                default:
                    Host_Error("CL_KeepaliveMessage: CL_GetMessage failed");
                    break;
                    
                case 0:
                    break;	// nothing waiting
                    
                case 1:
                    Host_Error("CL_KeepaliveMessage: received a message");
                    break;
                    
                case 2:
                    if (Reader.MSG_ReadByte() != q_shared.svc_nop)
                        Host_Error("CL_KeepaliveMessage: datagram wasn't a nop");
                    break;
            }
        } while (ret != 0);

        Message.RestoreState(_MsgState);

        // check time
        float time = (float)Sys_FloatTime();
        if (time - lastmsg < 5)
            return;
            
        lastmsg = time;

        // write out a nop
        Con_Printf("--> client to server keepalive\n");

        cls.message.MSG_WriteByte(q_shared.clc_nop);
        NET_SendMessage(cls.netcon, cls.message);
        cls.message.Clear();
    }
}