using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Globalization;


public static partial class game_engine
{
    public static void Host_InitCommands()
    {
        Cmd_AddCommand("status", Host_Status_f);
        Cmd_AddCommand("quit", Host_Quit_f);
        Cmd_AddCommand("god", Host_God_f);
        Cmd_AddCommand("notarget", Host_Notarget_f);
        Cmd_AddCommand("fly", Host_Fly_f);
        Cmd_AddCommand("map", Host_Map_f);
        Cmd_AddCommand("restart", Host_Restart_f);
        Cmd_AddCommand("changelevel", Host_Changelevel_f);
        Cmd_AddCommand("connect", Host_Connect_f);
        Cmd_AddCommand("reconnect", Host_Reconnect_f);
        Cmd_AddCommand("name", Host_Name_f);
        Cmd_AddCommand("noclip", Host_Noclip_f);
        Cmd_AddCommand("version", Host_Version_f);
        Cmd_AddCommand("say", Host_Say_f);
        Cmd_AddCommand("say_team", Host_Say_Team_f);
        Cmd_AddCommand("tell", Host_Tell_f);
        Cmd_AddCommand("color", Host_Color_f);
        Cmd_AddCommand("kill", Host_Kill_f);
        Cmd_AddCommand("pause", Host_Pause_f);
        Cmd_AddCommand("spawn", Host_Spawn_f);
        Cmd_AddCommand("begin", Host_Begin_f);
        Cmd_AddCommand("prespawn", Host_PreSpawn_f);
        Cmd_AddCommand("kick", Host_Kick_f);
        Cmd_AddCommand("ping", Host_Ping_f);
        Cmd_AddCommand("load", Host_Loadgame_f);
        Cmd_AddCommand("save", Host_Savegame_f);
        Cmd_AddCommand("give", Host_Give_f);

        Cmd_AddCommand("startdemos", Host_Startdemos_f);
        Cmd_AddCommand("demos", Host_Demos_f);
        Cmd_AddCommand("stopdemo", Host_Stopdemo_f);

        Cmd_AddCommand("viewmodel", Host_Viewmodel_f);
        Cmd_AddCommand("viewframe", Host_Viewframe_f);
        Cmd_AddCommand("viewnext", Host_Viewnext_f);
        Cmd_AddCommand("viewprev", Host_Viewprev_f);

        Cmd_AddCommand("mcache", Mod_Print);
    }
    public static void Host_Quit_f()
    {
        if (key_dest != keydest_t.key_console && cls.state != cactive_t.ca_dedicated)
        {
            MenuBase.QuitMenu.Show();
            return;
        }
        CL_Disconnect();
        Host_ShutdownServer(false);
        Sys_Quit();
    }
    public static void Host_Status_f()
    {
        bool flag = true;
        if (cmd_source == cmd_source_t.src_command)
        {
            if (!sv.active)
            {
                Cmd_ForwardToServer();
                return;
            }
        }
        else
            flag = false;

        StringBuilder sb = new StringBuilder(256);
        sb.Append(String.Format("host:    {0}\n", Cvar.Cvar_VariableString("hostname")));
        sb.Append(String.Format("version: {0:F2}\n", q_shared.VERSION));
        if (NetTcpIp.Instance.IsInitialized)
        {
            sb.Append("tcp/ip:  ");
            sb.Append(my_tcpip_address);
            sb.Append('\n');
        }

        sb.Append("map:     ");
        sb.Append(sv.name);
        sb.Append('\n');
        sb.Append(String.Format("players: {0} active ({1} max)\n\n", net_activeconnections, svs.maxclients));
        for (int j = 0; j < svs.maxclients; j++)
        {
            client_t client = svs.clients[j];
            if (!client.active)
                continue;

            int seconds = (int)(net_time - client.netconnection.connecttime);
            int hours, minutes = seconds / 60;
            if (minutes > 0)
            {
                seconds -= (minutes * 60);
                hours = minutes / 60;
                if (hours > 0)
                    minutes -= (hours * 60);
            }
            else
                hours = 0;
            sb.Append(String.Format("#{0,-2} {1,-16}  {2}  {2}:{4,2}:{5,2}",
                j + 1, client.name, (int)client.edict.v.frags, hours, minutes, seconds));
            sb.Append("   ");
            sb.Append(client.netconnection.address);
            sb.Append('\n');
        }

        if (flag)
            Con_Printf(sb.ToString());
        else
            SV_ClientPrintf(sb.ToString());
    }
    public static void Host_God_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (pr_global_struct.deathmatch != 0 && !host_client.privileged)
            return;

        sv_player.v.flags = (int)sv_player.v.flags ^ q_shared.FL_GODMODE;
        if (((int)sv_player.v.flags & q_shared.FL_GODMODE) == 0)
            SV_ClientPrintf("godmode OFF\n");
        else
            SV_ClientPrintf("godmode ON\n");
    }
    public static void Host_Notarget_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (pr_global_struct.deathmatch != 0 && !host_client.privileged)
            return;

        sv_player.v.flags = (int)sv_player.v.flags ^ q_shared.FL_NOTARGET;
        if (((int)sv_player.v.flags & q_shared.FL_NOTARGET) == 0)
            SV_ClientPrintf("notarget OFF\n");
        else
            SV_ClientPrintf("notarget ON\n");
    }
    public static void Host_Noclip_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (pr_global_struct.deathmatch > 0 && !host_client.privileged)
            return;

        if (sv_player.v.movetype != q_shared.MOVETYPE_NOCLIP)
        {
            noclip_anglehack = true;
            sv_player.v.movetype = q_shared.MOVETYPE_NOCLIP;
            SV_ClientPrintf("noclip ON\n");
        }
        else
        {
            noclip_anglehack = false;
            sv_player.v.movetype = q_shared.MOVETYPE_WALK;
            SV_ClientPrintf("noclip OFF\n");
        }
    }
    public static void Host_Fly_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (pr_global_struct.deathmatch > 0 && !host_client.privileged)
            return;

        if (sv_player.v.movetype != q_shared.MOVETYPE_FLY)
        {
            sv_player.v.movetype = q_shared.MOVETYPE_FLY;
            SV_ClientPrintf("flymode ON\n");
        }
        else
        {
            sv_player.v.movetype = q_shared.MOVETYPE_WALK;
            SV_ClientPrintf("flymode OFF\n");
        }
    }
    public static void Host_Ping_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        SV_ClientPrintf("Client ping times:\n");
        for (int i = 0; i < svs.maxclients; i++)
        {
            client_t client = svs.clients[i];
            if (!client.active)
                continue;
            float total = 0;
            for (int j = 0; j < q_shared.NUM_PING_TIMES; j++)
                total += client.ping_times[j];
            total /= q_shared.NUM_PING_TIMES;
            SV_ClientPrintf("{0,4} {1}\n", (int)(total * 1000), client.name);
        }
    }
    public static void Host_Map_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        cls.demonum = -1;		// stop demo loop in case this fails

        CL_Disconnect();
        Host_ShutdownServer(false);

        key_dest = keydest_t.key_game;			// remove console or menu
        SCR_BeginLoadingPlaque();

        cls.mapstring = JoinArgv() + "\n";

        svs.serverflags = 0;			// haven't completed an episode yet
        string name = Cmd_Argv(1);
        SV_SpawnServer(name);

        

        if (!sv.active)
            return;

        if (cls.state != cactive_t.ca_dedicated)
        {
            cls.spawnparms = JoinArgv();
            Cmd_ExecuteString("connect local", cmd_source_t.src_command);
        }
    }
    public static void Host_Changelevel_f()
    {
        if (cmd_argc != 2)
        {
            Con_Printf("changelevel <levelname> : continue game on a new level\n");
            return;
        }
        if (!sv.active || cls.demoplayback)
        {
            Con_Printf("Only the server may changelevel\n");
            return;
        }
        SV_SaveSpawnparms();
        string level = Cmd_Argv(1);
        SV_SpawnServer(level);
    }
    public static void Host_Restart_f()
    {
        if (cls.demoplayback || !sv.active)
            return;

        if (cmd_source != cmd_source_t.src_command)
            return;

        string mapname = sv.name; // must copy out, because it gets cleared
                                    // in sv_spawnserver
        SV_SpawnServer(mapname);
    }
    public static void Host_Reconnect_f()
    {
        SCR_BeginLoadingPlaque();
        cls.signon = 0;		// need new connection messages
    }
    public static void Host_Connect_f()
    {
        cls.demonum = -1;		// stop demo loop in case this fails
        if (cls.demoplayback)
        {
            CL_StopPlayback();
            CL_Disconnect();
        }
        string name = Cmd_Argv(1);
        CL_EstablishConnection(name);
        Host_Reconnect_f();
    }
    public static string Host_SavegameComment()
    {
        string result = String.Format("{0} kills:{1,3}/{2,3}", cl.levelname,
            cl.stats[q_shared.STAT_MONSTERS], cl.stats[q_shared. STAT_TOTALMONSTERS]);
            
        // convert space to _ to make stdio happy
        result = result.Replace(' ', '_');

        if (result.Length < q_shared.SAVEGAME_COMMENT_LENGTH - 1)
            result = result.PadRight(q_shared.SAVEGAME_COMMENT_LENGTH - 1, '_');

        if (result.Length > q_shared.SAVEGAME_COMMENT_LENGTH - 1)
            result = result.Remove(q_shared.SAVEGAME_COMMENT_LENGTH - 2);

        return result + '\0';
    }
    public static void Host_Savegame_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        if (!sv.active)
        {
            Con_Printf("Not playing a local game.\n");
            return;
        }

        if (cl.intermission != 0)
        {
            Con_Printf("Can't save in intermission.\n");
            return;
        }

        if (svs.maxclients != 1)
        {
            Con_Printf("Can't save multiplayer games.\n");
            return;
        }

        if (cmd_argc != 2)
        {
            Con_Printf("save <savename> : save a game\n");
            return;
        }

        if (Cmd_Argv(1).Contains(".."))
        {
            Con_Printf("Relative pathnames are not allowed.\n");
            return;
        }

        for (int i = 0; i < svs.maxclients; i++)
        {
            if (svs.clients[i].active && (svs.clients[i].edict.v.health <= 0))
            {
                Con_Printf("Can't savegame with a dead player\n");
                return;
            }
        }

        string name = Path.ChangeExtension(Path.Combine(com_gamedir, Cmd_Argv(1)), ".sav");

        Con_Printf("Saving game to {0}...\n", name);
        FileStream fs = Sys_FileOpenWrite(name, true);
        if (fs == null)
        {
            Con_Printf("ERROR: couldn't open.\n");
            return;
        }
        using (StreamWriter writer = new StreamWriter(fs, Encoding.ASCII))
        {
            writer.WriteLine(q_shared.SAVEGAME_VERSION);
            writer.WriteLine(Host_SavegameComment());

            for (int i = 0; i < q_shared.NUM_SPAWN_PARMS; i++)
                writer.WriteLine(svs.clients[0].spawn_parms[i].ToString("F6",
                    CultureInfo.InvariantCulture.NumberFormat));

            writer.WriteLine(current_skill);
            writer.WriteLine(sv.name);
            writer.WriteLine(sv.time.ToString("F6",
                CultureInfo.InvariantCulture.NumberFormat));

            // write the light styles

            for (int i = 0; i < q_shared.MAX_LIGHTSTYLES; i++)
            {
                if (!String.IsNullOrEmpty(sv.lightstyles[i]))
                    writer.WriteLine(sv.lightstyles[i]);
                else
                    writer.WriteLine("m");
            }

            ED_WriteGlobals(writer);
            for (int i = 0; i < sv.num_edicts; i++)
            {
                ED_Write(writer, EDICT_NUM(i));
                writer.Flush();
            }
        }
        Con_Printf("done.\n");
    }
    public static void Host_Loadgame_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        if (cmd_argc != 2)
        {
            Con_Printf("load <savename> : load a game\n");
            return;
        }

        cls.demonum = -1;		// stop demo loop in case this fails

        string name = Path.ChangeExtension(Path.Combine(com_gamedir, Cmd_Argv(1)), ".sav");

        // we can't call SCR_BeginLoadingPlaque, because too much stack space has
        // been used.  The menu calls it before stuffing loadgame command
        //	SCR_BeginLoadingPlaque ();

        Con_Printf("Loading game from {0}...\n", name);
        FileStream fs = Sys_FileOpenRead(name);
        if (fs == null)
        {
            Con_Printf("ERROR: couldn't open.\n");
            return;
        }

        using (StreamReader reader = new StreamReader(fs, Encoding.ASCII))
        {
            string line = reader.ReadLine();
            int version = atoi(line);
            if (version != q_shared.SAVEGAME_VERSION)
            {
                Con_Printf("Savegame is version {0}, not {1}\n", version, q_shared.SAVEGAME_VERSION);
                return;
            }
            line = reader.ReadLine();

            float[] spawn_parms = new float[q_shared.NUM_SPAWN_PARMS];
            for (int i = 0; i < spawn_parms.Length; i++)
            {
                line = reader.ReadLine();
                spawn_parms[i] = atof(line);
            }
            // this silliness is so we can load 1.06 save files, which have float skill values
            line = reader.ReadLine();
            float tfloat = atof(line);
            current_skill = (int)(tfloat + 0.1);
            Cvar.Cvar_SetValue("skill", (float)current_skill);

            string mapname = reader.ReadLine();
            line = reader.ReadLine();
            float time = atof(line);

            CL_Disconnect_f();
            SV_SpawnServer(mapname);

            if (!sv.active)
            {
                Con_Printf("Couldn't load map\n");
                return;
            }
            sv.paused = true;		// pause until all clients connect
            sv.loadgame = true;

            // load the light styles

            for (int i = 0; i < q_shared.MAX_LIGHTSTYLES; i++)
            {
                line = reader.ReadLine();
                sv.lightstyles[i] = line;
            }

            // load the edicts out of the savegame file
            int entnum = -1;		// -1 is the globals
            StringBuilder sb = new StringBuilder(32768);
            while (!reader.EndOfStream)
            {
                line = reader.ReadLine();
                if (line == null)
                    Sys_Error("EOF without closing brace");

                sb.AppendLine(line);
                int idx = line.IndexOf('}');
                if (idx != -1)
                {
                    int length = 1 + sb.Length - (line.Length - idx);
                    string data = COM_Parse(sb.ToString(0, length));
                    if (String.IsNullOrEmpty(com_token))
                        break; // end of file
                    if (com_token != "{")
                        Sys_Error("First token isn't a brace");

                    if (entnum == -1)
                    {
                        // parse the global vars
                        ED_ParseGlobals(data);
                    }
                    else
                    {
                        // parse an edict
                        edict_t ent = EDICT_NUM(entnum);
                        ent.Clear();
                        ED_ParseEdict(data, ent);

                        // link it into the bsp tree
                        if (!ent.free)
                            SV_LinkEdict(ent, false);
                    }

                    entnum++;
                    sb.Remove(0, length);
                }
            }

            sv.num_edicts = entnum;
            sv.time = time;

            for (int i = 0; i < q_shared.NUM_SPAWN_PARMS; i++)
                svs.clients[0].spawn_parms[i] = spawn_parms[i];
        }

        if (cls.state != cactive_t.ca_dedicated)
        {
            CL_EstablishConnection("local");
            Host_Reconnect_f();
        }
    }    
    public static void Host_Name_f()
    {
        if (cmd_argc == 1)
        {
            Con_Printf("\"name\" is \"{0}\"\n", cl_name.@string);
            return;
        }

        string newName;
        if (cmd_argc == 2)
            newName = Cmd_Argv(1);
        else
            newName = cmd_args;

        if (newName.Length > 16)
            newName = newName.Remove(15);

        if (cmd_source == cmd_source_t.src_command)
        {
            if (cl_name.@string == newName)
                return;
            Cvar.Cvar_Set("_cl_name", newName);
            if (cls.state == cactive_t.ca_connected)
                Cmd_ForwardToServer();
            return;
        }

        if (!String.IsNullOrEmpty(host_client.name) && host_client.name != "unconnected")
            if (host_client.name != newName)
                Con_Printf("{0} renamed to {1}\n", host_client.name, newName);

        host_client.name = newName;
        host_client.edict.v.netname = ED_NewString(newName);

        // send notification to all clients
        MsgWriter msg = sv.reliable_datagram;
        msg.MSG_WriteByte(q_shared.svc_updatename);
        msg.MSG_WriteByte(ClientNum);
        msg.MSG_WriteString(newName);
    }
    public static void Host_Version_f()
    {
	    Con_Printf("Version {0}\n", q_shared. VERSION);
	    Con_Printf("Exe hash code: {0}\n", System.Reflection.Assembly.GetExecutingAssembly().GetHashCode());
    }
    public static void Host_Say(bool teamonly)
    {
        bool fromServer = false;
        if (cmd_source == cmd_source_t.src_command)
        {
            if (cls.state == cactive_t.ca_dedicated)
            {
                fromServer = true;
                teamonly = false;
            }
            else
            {
                Cmd_ForwardToServer();
                return;
            }
        }

        if (cmd_argc < 2)
            return;

        client_t save = host_client;

        string p = cmd_args;
        // remove quotes if present
        if (p.StartsWith("\""))
        {
            p = p.Substring(1, p.Length - 2);
        }

        // turn on color set 1
        string text;
        if (!fromServer)
            text = (char)1 + save.name + ": ";
        else
            text = (char)1 + "<" + hostname.@string + "> ";

        text += p + "\n";

        for (int j = 0; j < svs.maxclients; j++)
        {
            client_t client = svs.clients[j];
            if (client == null || !client.active || !client.spawned)
                continue;
            if (teamplay.value != 0 && teamonly && client.edict.v.team != save.edict.v.team)
                continue;
            host_client = client;
            SV_ClientPrintf(text);
        }
        host_client = save;
    }    
    public static void Host_Say_f()
    {
	    Host_Say(false);
    }
    public static void Host_Say_Team_f()
    {
	    Host_Say(true);
    }
    public static void Host_Tell_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (cmd_argc < 3)
            return;

        string text = host_client.name + ": ";
        string p = cmd_args;

        // remove quotes if present
        if (p.StartsWith("\""))
        {
            p = p.Substring(1, p.Length - 2);
        }

        text += p + "\n";

        client_t save = host_client;
        for (int j = 0; j < svs.maxclients; j++)
        {
            client_t client = svs.clients[j];
            if (!client.active || !client.spawned)
                continue;
            if (client.name == Cmd_Argv(1))
                continue;
            host_client = client;
            SV_ClientPrintf(text);
            break;
        }
        host_client = save;
    }
    public static void Host_Color_f()
    {
        if (cmd_argc == 1)
        {
            Con_Printf("\"color\" is \"{0} {1}\"\n", ((int)cl_color.value) >> 4, ((int)cl_color.value) & 0x0f);
            Con_Printf("color <0-13> [0-13]\n");
            return;
        }

        int top, bottom;
        if (cmd_argc == 2)
            top = bottom = atoi(Cmd_Argv(1));
        else
        {
            top = atoi(Cmd_Argv(1));
            bottom = atoi(Cmd_Argv(2));
        }

        top &= 15;
        if (top > 13)
            top = 13;
        bottom &= 15;
        if (bottom > 13)
            bottom = 13;

        int playercolor = top * 16 + bottom;

        if (cmd_source == cmd_source_t.src_command)
        {
            Cvar.Cvar_SetValue("_cl_color", playercolor);
            if (cls.state == cactive_t.ca_connected)
                Cmd_ForwardToServer();
            return;
        }


        host_client.colors = playercolor;
        host_client.edict.v.team = bottom + 1;

        // send notification to all clients
        MsgWriter msg = sv.reliable_datagram;
        msg.MSG_WriteByte(q_shared.svc_updatecolors);
        msg.MSG_WriteByte(ClientNum);
        msg.MSG_WriteByte(host_client.colors);
    }
    public static void Host_Kill_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (sv_player.v.health <= 0)
        {
            SV_ClientPrintf("Can't suicide -- allready dead!\n");
            return;
        }

        pr_global_struct.time = (float)sv.time;
        pr_global_struct.self = EDICT_TO_PROG(sv_player);
        PR_ExecuteProgram(pr_global_struct.ClientKill);
    }
    public static void Host_Pause_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }
        if (pausable.value == 0)
            SV_ClientPrintf("Pause not allowed.\n");
        else
        {
            sv.paused = !sv.paused;

            if (sv.paused)
            {
                SV_BroadcastPrint("{0} paused the game\n", GetString(sv_player.v.netname));
            }
            else
            {
                SV_BroadcastPrint("{0} unpaused the game\n", GetString(sv_player.v.netname));
            }

            // send notification to all clients
            sv.reliable_datagram.MSG_WriteByte(q_shared.svc_setpause);
            sv.reliable_datagram.MSG_WriteByte(sv.paused ? 1 : 0);
        }
    }
    public static void Host_PreSpawn_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Con_Printf("prespawn is not valid from the console\n");
            return;
        }

        if (host_client.spawned)
        {
            Con_Printf("prespawn not valid -- allready spawned\n");
            return;
        }

        MsgWriter msg = host_client.message;
        msg.Write(sv.signon.Data, 0, sv.signon.Length);
        msg.MSG_WriteByte(q_shared.svc_signonnum);
        msg.MSG_WriteByte(2);
        host_client.sendsignon = true;
    }
    public static void Host_Spawn_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Con_Printf("spawn is not valid from the console\n");
            return;
        }

        if (host_client.spawned)
        {
            Con_Printf("Spawn not valid -- allready spawned\n");
            return;
        }

        edict_t ent;

        // run the entrance script
        if (sv.loadgame)
        {
            // loaded games are fully inited allready
            // if this is the last client to be connected, unpause
            sv.paused = false;
        }
        else
        {
            // set up the edict
            ent = host_client.edict;

            ent.Clear(); //memset(&ent.v, 0, progs.entityfields * 4);
            ent.v.colormap = NUM_FOR_EDICT(ent);
            ent.v.team = (host_client.colors & 15) + 1;
            ent.v.netname = ED_NewString(host_client.name);

            // copy spawn parms out of the client_t
            pr_global_struct.SetParams(host_client.spawn_parms);

            // call the spawn function

            pr_global_struct.time = (float)sv.time;
            pr_global_struct.self = EDICT_TO_PROG(sv_player);
            PR_ExecuteProgram(pr_global_struct.ClientConnect);

            if ((Sys_FloatTime() - host_client.netconnection.connecttime) <= sv.time)
                Con_DPrintf("{0} entered the game\n", host_client.name);

            PR_ExecuteProgram(pr_global_struct.PutClientInServer);
        }


        // send all current names, colors, and frag counts
        MsgWriter msg = host_client.message;
        msg.Clear();

        // send time of update
        msg.MSG_WriteByte(q_shared.svc_time);
        msg.MSG_WriteFloat((float)sv.time);

        for (int i = 0; i < svs.maxclients; i++)
        {
            client_t client = svs.clients[i];
            msg.MSG_WriteByte(q_shared.svc_updatename);
            msg.MSG_WriteByte(i);
            msg.MSG_WriteString(client.name);
            msg.MSG_WriteByte(q_shared.svc_updatefrags);
            msg.MSG_WriteByte(i);
            msg.MSG_WriteShort(client.old_frags);
            msg.MSG_WriteByte(q_shared.svc_updatecolors);
            msg.MSG_WriteByte(i);
            msg.MSG_WriteByte(client.colors);
        }

        // send all current light styles
        for (int i = 0; i < q_shared.MAX_LIGHTSTYLES; i++)
        {
            msg.MSG_WriteByte(q_shared.svc_lightstyle);
            msg.MSG_WriteByte((char)i);
            msg.MSG_WriteString(sv.lightstyles[i]);
        }

        //
        // send some stats
        //
        msg.MSG_WriteByte(q_shared.svc_updatestat);
        msg.MSG_WriteByte(q_shared.STAT_TOTALSECRETS);
        msg.MSG_WriteLong((int)pr_global_struct.total_secrets);

        msg.MSG_WriteByte(q_shared.svc_updatestat);
        msg.MSG_WriteByte(q_shared.STAT_TOTALMONSTERS);
        msg.MSG_WriteLong((int)pr_global_struct.total_monsters);

        msg.MSG_WriteByte(q_shared.svc_updatestat);
        msg.MSG_WriteByte(q_shared.STAT_SECRETS);
        msg.MSG_WriteLong((int)pr_global_struct.found_secrets);

        msg.MSG_WriteByte(q_shared.svc_updatestat);
        msg.MSG_WriteByte(q_shared.STAT_MONSTERS);
        msg.MSG_WriteLong((int)pr_global_struct.killed_monsters);


        //
        // send a fixangle
        // Never send a roll angle, because savegames can catch the server
        // in a state where it is expecting the client to correct the angle
        // and it won't happen if the game was just loaded, so you wind up
        // with a permanent head tilt
        ent = EDICT_NUM(1 + ClientNum);
        msg.MSG_WriteByte(q_shared.svc_setangle);
        msg.MSG_WriteAngle(ent.v.angles.x);
        msg.MSG_WriteAngle(ent.v.angles.y);
        msg.MSG_WriteAngle(0);

        SV_WriteClientdataToMessage(sv_player, host_client.message);

        msg.MSG_WriteByte(q_shared.svc_signonnum);
        msg.MSG_WriteByte(3);
        host_client.sendsignon = true;
    }
    public static void Host_Begin_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Con_Printf("begin is not valid from the console\n");
            return;
        }

        host_client.spawned = true;
    }
    public static void Host_Kick_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            if (!sv.active)
            {
                Cmd_ForwardToServer();
                return;
            }
        }
        else if (pr_global_struct.deathmatch != 0 && !host_client.privileged)
            return;

        client_t save = host_client;
        bool byNumber = false;
        int i;
        if (cmd_argc > 2 && Cmd_Argv(1) == "#")
        {
            i = (int)atof(Cmd_Argv(2)) - 1;
            if (i < 0 || i >= svs.maxclients)
                return;
            if (!svs.clients[i].active)
                return;

            host_client = svs.clients[i];
            byNumber = true;
        }
        else
        {
            for (i = 0; i < svs.maxclients; i++)
            {
                host_client = svs.clients[i];
                if (!host_client.active)
                    continue;
                if (SameText(host_client.name, Cmd_Argv(1)))
                    break;
            }
        }

        if (i < svs.maxclients)
        {
            string who;
            if (cmd_source == cmd_source_t.src_command)
                if (cls.state == cactive_t.ca_dedicated)
                    who = "Console";
                else
                    who = cl_name.@string;
            else
                who = save.name;

            // can't kick yourself!
            if (host_client == save)
                return;

            string message = null;
            if (cmd_argc > 2)
            {
                message = COM_Parse(cmd_args);
                if (byNumber)
                {
                    message = message.Substring(1); // skip the #
                    message = message.Trim(); // skip white space
                    message = message.Substring(Cmd_Argv(2).Length);	// skip the number
                }
                message = message.Trim();
            }
            if (!String.IsNullOrEmpty(message))
                SV_ClientPrintf("Kicked by {0}: {1}\n", who, message);
            else
                SV_ClientPrintf("Kicked by {0}\n", who);
            SV_DropClient(false);
        }

        host_client = save;
    }
    public static void Host_Give_f()
    {
        if (cmd_source == cmd_source_t.src_command)
        {
            Cmd_ForwardToServer();
            return;
        }

        if (pr_global_struct.deathmatch != 0 && !host_client.privileged)
            return;

        string t = Cmd_Argv(1);
        int v = atoi(Cmd_Argv(2));

        if (String.IsNullOrEmpty(t))
            return;

        switch (t[0])
        {
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
            case '8':
            case '9':
                // MED 01/04/97 added hipnotic give stuff
                if (_GameKind == GameKind.Hipnotic)
                {
                    if (t[0] == '6')
                    {
                        if (t[1] == 'a')
                            sv_player.v.items = (int)sv_player.v.items | q_shared.HIT_PROXIMITY_GUN;
                        else
                            sv_player.v.items = (int)sv_player.v.items | q_shared.IT_GRENADE_LAUNCHER;
                    }
                    else if (t[0] == '9')
                        sv_player.v.items = (int)sv_player.v.items | q_shared.HIT_LASER_CANNON;
                    else if (t[0] == '0')
                        sv_player.v.items = (int)sv_player.v.items | q_shared.HIT_MJOLNIR;
                    else if (t[0] >= '2')
                        sv_player.v.items = (int)sv_player.v.items | (q_shared.IT_SHOTGUN << (t[0] - '2'));
                }
                else
                {
                    if (t[0] >= '2')
                        sv_player.v.items = (int)sv_player.v.items | (q_shared.IT_SHOTGUN << (t[0] - '2'));
                }
                break;

            case 's':
                if (_GameKind == GameKind.Rogue)
                    SetEdictFieldFloat(sv_player, "ammo_shells1", v);

                sv_player.v.ammo_shells = v;
                break;

            case 'n':
                if (_GameKind == GameKind.Rogue)
                {
                    if (SetEdictFieldFloat(sv_player, "ammo_nails1", v))
                        if (sv_player.v.weapon <= q_shared.IT_LIGHTNING)
                            sv_player.v.ammo_nails = v;
                }
                else
                    sv_player.v.ammo_nails = v;
                break;

            case 'l':
                if (_GameKind == GameKind.Rogue)
                {
                    if (SetEdictFieldFloat(sv_player, "ammo_lava_nails", v))
                        if (sv_player.v.weapon > q_shared.IT_LIGHTNING)
                            sv_player.v.ammo_nails = v;
                }
                break;

            case 'r':
                if (_GameKind == GameKind.Rogue)
                {
                    if (SetEdictFieldFloat(sv_player, "ammo_rockets1", v))
                        if (sv_player.v.weapon <= q_shared.IT_LIGHTNING)
                            sv_player.v.ammo_rockets = v;
                }
                else
                {
                    sv_player.v.ammo_rockets = v;
                }
                break;

            case 'm':
                if (_GameKind == GameKind.Rogue)
                {
                    if (SetEdictFieldFloat(sv_player, "ammo_multi_rockets", v))
                        if (sv_player.v.weapon > q_shared.IT_LIGHTNING)
                            sv_player.v.ammo_rockets = v;
                }
                break;

            case 'h':
                sv_player.v.health = v;
                break;

            case 'c':
                if (_GameKind == GameKind.Rogue)
                {
                    if (SetEdictFieldFloat(sv_player, "ammo_cells1", v))
                        if (sv_player.v.weapon <= q_shared.IT_LIGHTNING)
                            sv_player.v.ammo_cells = v;
                }
                else
                {
                    sv_player.v.ammo_cells = v;
                }
                break;

            case 'p':
                if (_GameKind == GameKind.Rogue)
                {
                    if (SetEdictFieldFloat(sv_player, "ammo_plasma", v))
                        if (sv_player.v.weapon > q_shared.IT_LIGHTNING)
                            sv_player.v.ammo_cells = v;
                }
                break;
        }
    }
    public static edict_t FindViewthing()
    {
        for (int i = 0; i < sv.num_edicts; i++)
        {
            edict_t e = EDICT_NUM(i);
            if (GetString(e.v.classname) == "viewthing")
                return e;
        }
        Con_Printf("No viewthing on map\n");
        return null;
    }
    public static void Host_Viewmodel_f()
    {
        edict_t e = FindViewthing();
        if (e == null)
            return;

        model_t m = Mod_ForName(Cmd_Argv(1), false);
        if (m == null)
        {
            Con_Printf("Can't load {0}\n", Cmd_Argv(1));
            return;
        }

        e.v.frame = 0;
        cl.model_precache[(int)e.v.modelindex] = m;
    }
    public static void Host_Viewframe_f()
    {
        edict_t e = FindViewthing();
        if (e == null)
            return;

        model_t m = cl.model_precache[(int)e.v.modelindex];

        int f = atoi(Cmd_Argv(1));
        if (f >= m.numframes)
            f = m.numframes - 1;

        e.v.frame = f;
    }
    public static void PrintFrameName(model_t m, int frame)
    {
        aliashdr_t hdr = Mod_Extradata(m);
        if (hdr == null)
            return;

        Con_Printf("frame {0}: {1}\n", frame, hdr.frames[frame].name);
    }
    public static void Host_Viewnext_f()
    {
        edict_t e = FindViewthing();
        if (e == null)
            return;

        model_t m = cl.model_precache[(int)e.v.modelindex];

        e.v.frame = e.v.frame + 1;
        if (e.v.frame >= m.numframes)
            e.v.frame = m.numframes - 1;

        PrintFrameName(m, (int)e.v.frame);
    }
    public static void Host_Viewprev_f()
    {
        edict_t e = FindViewthing();
        if (e == null)
            return;

        model_t m = cl.model_precache[(int)e.v.modelindex];

        e.v.frame = e.v.frame - 1;
        if (e.v.frame < 0)
            e.v.frame = 0;

        PrintFrameName(m, (int)e.v.frame);
    }
    public static void Host_Startdemos_f()
    {
        if (cls.state == cactive_t.ca_dedicated)
        {
            if (!sv.active)
                Cbuf_AddText("map start\n");
            return;
        }

        int c = cmd_argc - 1;
        if (c > q_shared.MAX_DEMOS)
        {
            Con_Printf("Max {0} demos in demoloop\n", q_shared.MAX_DEMOS);
            c = q_shared.MAX_DEMOS;
        }
        Con_Printf("{0} demo(s) in loop\n", c);

        for (int i = 1; i < c + 1; i++)
            cls.demos[i - 1] = Copy(Cmd_Argv(i), q_shared.MAX_DEMONAME);

        if (!sv.active && cls.demonum != -1 && !cls.demoplayback)
        {
            cls.demonum = 0;
            CL_NextDemo();
        }
        else
            cls.demonum = -1;
    }
    public static void Host_Demos_f()
    {
        if (cls.state == cactive_t.ca_dedicated)
            return;
        if (cls.demonum == -1)
            cls.demonum = 1;
        CL_Disconnect_f();
        CL_NextDemo();
    }
    public static void Host_Stopdemo_f()
    {
        if (cls.state == cactive_t.ca_dedicated)
            return;
        if (!cls.demoplayback)
            return;
        CL_StopPlayback();
        CL_Disconnect();
    }
}