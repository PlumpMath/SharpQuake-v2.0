using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OpenTK;

public static partial class game_engine
{
    public static quakeparms_t host_parms;

    public static cvar_t sys_ticrate;
    public static cvar_t developer;
    public static cvar_t host_framerate;
    public static cvar_t host_speeds;
    public static cvar_t serverprofile;
    public static cvar_t fraglimit;
    public static cvar_t timelimit;
    public static cvar_t teamplay;
    public static cvar_t skill;
    public static cvar_t deathmatch;
    public static cvar_t coop;
    public static cvar_t pausable;

    public static bool host_initialized;
    public static int host_framecount;
    public static byte[] host_basepal;
    public static byte[] host_colormap;
    public static double host_framtime;
    public static double host_time;

    public static double realtime;
    public static double oldrealtime;
    public static int current_skill;
    public static bool noclip_anglehack;
    public static BinaryReader _VcrReader;
    public static BinaryWriter _VcrWriter;

    public static client_t host_client;
    public static double timetotal;
    public static int timecount;
    public static double time1 = 0;
    public static double time2 = 0;
    public static double time3 = 0;

    public static int _ShutdownDepth;
    public static int _ErrorDepth;
    public static int ClientNum
    {
        get { return Array.IndexOf(svs.clients, host_client); }
    }

    public static void Host_Init(quakeparms_t parms)
    {
        Con_DPrintf("Host.Init\n");

        host_parms = parms;

        Cache_Init(1024 * 1024 * 16); // debug
        Cbuf_Init();
        Cmd_Init();
        game_engine.V_Init();
        Chase_Init();
        InitVCR(parms);
        COM_Init(parms.basedir, parms.argv);
        Host_InitLocal();
        game_engine.W_LoadWadFile("gfx.wad");
        Key_Init();
        Con_Init();
        Menu.M_Init();
        PR_Init();
        Mod_Init();
        NET_Init();
        SV_Init();
        R_InitTextures();

        if (cls.state != cactive_t.ca_dedicated)
        {
            host_basepal = COM_LoadFile("gfx/palette.lmp");
            if (host_basepal == null)
                Sys_Error("Couldn't load gfx/palette.lmp");
            host_colormap = COM_LoadFile("gfx/colormap.lmp");
            if (host_colormap == null)
                Sys_Error("Couldn't load gfx/colormap.lmp");

            // on non win32, mouse comes before video for security reasons
            IN_Init();
            VID_Init(host_basepal);
            Draw_Init();
            SCR_Init();
            R_Init();
            S_Init();
            CDAudio_Init();
            Sbar_Init();
            CL_Init();
        }

        Cbuf_InsertText("exec quake.rc\n");

        host_initialized = true;

        Con_DPrintf("========Quake Initialized=========\n");
    }
    public static void Host_ClearMemory()
    {
        Con_DPrintf("Clearing memory\n");

        Mod_ClearAll();
        cls.signon = 0;
        sv.Clear();
        cl.Clear();
    }
    public static void Host_ServerFrame()
    {
        // run the world state	
        pr_global_struct.frametime = (float)host_framtime;

        // set the time and clear the general datagram
        SV_ClearDatagram();

        // check for new clients
        SV_CheckForNewClients();

        // read client messages
        SV_RunClients();

        // move things around and think
        // always pause in single player if in console or menus
        if (!sv.paused && (svs.maxclients > 1 || key_dest == keydest_t.key_game))
            SV_Physics();

        // send all messages to the clients
        SV_SendClientMessages();
    }
    public static void Host_Shutdown()
    {
        _ShutdownDepth++;
        try
        {
            if (_ShutdownDepth > 1)
                return;

            // keep Con_Printf from trying to update the screen
            scr_disabled_for_loading = true;

            Host_WriteConfiguration();

            CDAudio_Shutdown();
            NET_Shutdown();
            S_Shutdown();
            IN_Shutdown();

            if (_VcrWriter != null)
            {
                Con_Printf("Closing vcrfile.\n");
                _VcrWriter.Close();
                _VcrWriter = null;
            }
            if (_VcrReader != null)
            {
                Con_Printf("Closing vcrfile.\n");
                _VcrReader.Close();
                _VcrReader = null;
            }

            if (cls.state != cactive_t.ca_dedicated)
            {
                VID_Shutdown();
            }

            Con_Shutdown();
        }
        finally
        {
            _ShutdownDepth--;
        }
    }
    public static void Host_WriteConfiguration()
    {
        // dedicated servers initialize the host but don't parse and set the
        // config.cfg cvars
        if (host_initialized)
        {
            string path = Path.Combine(com_gamedir, "config.cfg");
            using (FileStream fs = Sys_FileOpenWrite(path, true))
            {
                if (fs != null)
                {
                    Key_WriteBindings(fs);
                    Cvar.Cvar_WriteVariables(fs);
                }
            }
        }
    }
    public static void Host_Error(string error, params object[] args)
    {
        _ErrorDepth++;
        try
        {
            if (_ErrorDepth > 1)
                Sys_Error("Host_Error: recursively entered. " + error, args);

            SCR_EndLoadingPlaque();		// reenable screen updates

            string message = (args.Length > 0 ? String.Format(error, args) : error);
            Con_Printf("Host_Error: {0}\n", message);

            if (sv.active)
                Host_ShutdownServer(false);

            if (cls.state == cactive_t.ca_dedicated)
                Sys_Error("Host_Error: {0}\n", message);	// dedicated servers exit

            CL_Disconnect();
            cls.demonum = -1;

            throw new Exception(); // longjmp (host_abortserver, 1);
        }
        finally
        {
            _ErrorDepth--;
        }
    }
    public static void Host_EndGame(string message, params object[] args)
    {
        string str = String.Format(message, args);
        Con_DPrintf("Host_EndGame: {0}\n", str);

        if (sv.active)
            Host_ShutdownServer(false);

        if (cls.state == cactive_t.ca_dedicated)
            Sys_Error("Host_EndGame: {0}\n", str);	// dedicated servers exit

        if (cls.demonum != -1)
            CL_NextDemo();
        else
            CL_Disconnect();

        throw new Exception();  //longjmp (host_abortserver, 1);
    }
    public static void Host_Frame(double time)
    {
        if (serverprofile.value == 0)
        {
            _Host_Frame(time);
            return;
        }

        double time1 = Sys_FloatTime();
        _Host_Frame(time);
        double time2 = Sys_FloatTime();

        timetotal += time2 - time1;
        timecount++;

        if (timecount < 1000)
            return;

        int m = (int)(timetotal * 1000 / timecount);
        timecount = 0;
        timetotal = 0;
        int c = 0;
        foreach (client_t cl in svs.clients)
        {
            if (cl.active)
                c++;
        }

        Con_Printf("serverprofile: {0,2:d} clients {1,2:d} msec\n", c, m);
    }
    public static void Host_ClientCommands(string fmt, params object[] args)
    {
        string tmp = String.Format(fmt, args);
        host_client.message.MSG_WriteByte(q_shared.svc_stufftext);
        host_client.message.MSG_WriteString(tmp);
    }
    public static void Host_ShutdownServer(bool crash)
    {
        if (!sv.active)
            return;

        sv.active = false;

        // stop all client sounds immediately
        if (cls.state == cactive_t.ca_connected)
            CL_Disconnect();

        // flush any pending messages - like the score!!!
        double start = Sys_FloatTime();
        int count;
        do
        {
            count = 0;
            for (int i = 0; i < svs.maxclients; i++)
            {
                host_client = svs.clients[i];
                if (host_client.active && !host_client.message.IsEmpty)
                {
                    if (NET_CanSendMessage(host_client.netconnection))
                    {
                        NET_SendMessage(host_client.netconnection, host_client.message);
                        host_client.message.Clear();
                    }
                    else
                    {
                        NET_GetMessage(host_client.netconnection);
                        count++;
                    }
                }
            }
            if ((Sys_FloatTime() - start) > 3.0)
                break;
        }
        while (count > 0);

        // make sure all the clients know we're disconnecting
        MsgWriter writer = new MsgWriter(4);
        writer.MSG_WriteByte(q_shared.svc_disconnect);
        count = NET_SendToAll(writer, 5);
        if (count != 0)
            Con_Printf("Host_ShutdownServer: NET_SendToAll failed for {0} clients\n", count);

        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];
            if (host_client.active)
                SV_DropClient(crash);
        }

        //
        // clear structures
        //
        sv.Clear();
        for (int i = 0; i < svs.clients.Length; i++)
            svs.clients[i].Clear();
    }
    public static void Host_InitLocal()
    {
        Host_InitCommands();

        sys_ticrate = new cvar_t("sys_ticrate", "0.05");
        developer = new cvar_t("developer", "1");
        host_framerate = new cvar_t("host_framerate", "0"); // set for slow motion
        host_speeds = new cvar_t("host_speeds", "0");	// set for running times
        serverprofile = new cvar_t("serverprofile", "0");
        fraglimit = new cvar_t("fraglimit", "0", false, true);
        timelimit = new cvar_t("timelimit", "0", false, true);
        teamplay = new cvar_t("teamplay", "0", false, true);
        skill = new cvar_t("skill", "1"); // 0 - 3
        deathmatch = new cvar_t("deathmatch", "0"); // 0, 1, or 2
        coop = new cvar_t("coop", "0"); // 0 or 1
        pausable = new cvar_t("pausable", "1");

        Host_FindMaxClients();

        host_time = 1.0;		// so a think at time 0 won't get called
    }
    public static void Host_FindMaxClients()
    {
        svs.maxclients = 1;

        int i = COM_CheckParm("-dedicated");
        if (i > 0)
        {
            cls.state = cactive_t.ca_dedicated;
            if (i != (com_argv.Length - 1))
            {
                svs.maxclients = atoi(Argv(i + 1));
            }
            else
                svs.maxclients = 8;
        }
        else
            cls.state = cactive_t.ca_disconnected;

        i = COM_CheckParm("-listen");
        if (i > 0)
        {
            if (cls.state == cactive_t.ca_dedicated)
                Sys_Error("Only one of -dedicated or -listen can be specified");
            if (i != (com_argv.Length - 1))
                svs.maxclients = atoi(Argv(i + 1));
            else
                svs.maxclients = 8;
        }
        if (svs.maxclients < 1)
            svs.maxclients = 8;
        else if (svs.maxclients > q_shared.MAX_SCOREBOARD)
            svs.maxclients = q_shared.MAX_SCOREBOARD;

        svs.maxclientslimit = svs.maxclients;
        if (svs.maxclientslimit < 4)
            svs.maxclientslimit = 4;
        svs.clients = new client_t[svs.maxclientslimit]; // Hunk_AllocName (svs.maxclientslimit*sizeof(client_t), "clients");
        for (i = 0; i < svs.clients.Length; i++)
            svs.clients[i] = new client_t();

        if (svs.maxclients > 1)
            Cvar.Cvar_SetValue("deathmatch", 1.0f);
        else
            Cvar.Cvar_SetValue("deathmatch", 0.0f);
    }
    public static void InitVCR(quakeparms_t parms)
    {
        if (HasParam("-playback"))
        {
            if (com_argv.Length != 2)
                Sys_Error("No other parameters allowed with -playback\n");

            Stream file = Sys_FileOpenRead("quake.vcr");
            if (file == null)
                Sys_Error("playback file not found\n");

            _VcrReader = new BinaryReader(file, Encoding.ASCII);
            int signature = _VcrReader.ReadInt32();  //Sys_FileRead(vcrFile, &i, sizeof(int));
            if (signature != q_shared.VCR_SIGNATURE)
                Sys_Error("Invalid signature in vcr file\n");

            int argc = _VcrReader.ReadInt32(); // Sys_FileRead(vcrFile, &com_argc, sizeof(int));
            string[] argv = new string[argc + 1];
            argv[0] = parms.argv[0];

            for (int i = 1; i < argv.Length; i++)
            {
                argv[i] = ReadString(_VcrReader);
            }
            com_argv = argv;
            parms.argv = argv;
        }

        int n = COM_CheckParm("-record");
        if (n != 0)
        {
            Stream file = Sys_FileOpenWrite("quake.vcr"); // vcrFile = Sys_FileOpenWrite("quake.vcr");
            _VcrWriter = new BinaryWriter(file, Encoding.ASCII);

            _VcrWriter.Write(q_shared.VCR_SIGNATURE); //  Sys_FileWrite(vcrFile, &i, sizeof(int));
            _VcrWriter.Write(com_argv.Length - 1);
            for (int i = 1; i < com_argv.Length; i++)
            {
                if (i == n)
                {
                    WriteString(_VcrWriter, "-playback");
                    continue;
                }
                WriteString(_VcrWriter, Argv(i));
            }
        }
    }
    public static void _Host_Frame(double time)
    {
        // keep the random time dependent
        Random();

        // decide the simulation time
        if (!Host_FilterTime(time))
            return;			// don't run too fast, or packets will flood out

        // get new key events
        Sys_SendKeyEvents();

        // allow mice or other external controllers to add commands
        IN_Commands();

        // process console commands
        Cbuf_Execute();

        NET_Poll();

        // if running the server locally, make intentions now
        if (sv.active)
            CL_SendCmd();

        //-------------------
        //
        // server operations
        //
        //-------------------

        // check for commands typed to the host
        Host_GetConsoleCommands();

        if (sv.active)
            Host_ServerFrame();

        //-------------------
        //
        // client operations
        //
        //-------------------

        // if running the server remotely, send intentions now after
        // the incoming messages have been read
        if (!sv.active)
            CL_SendCmd();

        host_time += host_framtime;

        // fetch results from server
        if (cls.state == cactive_t.ca_connected)
        {
            CL_ReadFromServer();
        }

        // update video
        if (host_speeds.value != 0)
            time1 = Sys_FloatTime();

        SCR_UpdateScreen();

        if (host_speeds.value != 0)
            time2 = Sys_FloatTime();

        // update audio
        if (cls.signon == q_shared.SIGNONS)
        {
            S_Update(ref r_origin, ref vpn, ref vright, ref vup);
            CL_DecayLights();
        }
        else
            S_Update(ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref q_shared.ZeroVector);

        CDAudio_Update();

        if (host_speeds.value != 0)
        {
            int pass1 = (int)((time1 - time3) * 1000);
            time3 = Sys_FloatTime();
            int pass2 = (int)((time2 - time1) * 1000);
            int pass3 = (int)((time3 - time2) * 1000);
            Con_Printf("{0,3} tot {1,3} server {2,3} gfx {3,3} snd\n", pass1 + pass2 + pass3, pass1, pass2, pass3);
        }

        host_framecount++;
    }
    public static bool Host_FilterTime(double time)
    {
        realtime += time;

        if (!cls.timedemo && realtime - oldrealtime < 1.0 / 72.0)
            return false;	// framerate is too high

        host_framtime = realtime - oldrealtime;
        oldrealtime = realtime;

        if (host_framerate.value > 0)
            host_framtime = host_framerate.value;
        else
        {	// don't allow really long or short frames
            if (host_framtime > 0.1)
                host_framtime = 0.1;
            if (host_framtime < 0.001)
                host_framtime = 0.001;
        }

        return true;
    }
    public static void Host_GetConsoleCommands()
    {
        while (true)
        {
            string cmd = Sys_ConsoleInput();
            if (String.IsNullOrEmpty(cmd))
                break;

            Cbuf_AddText(cmd);
        }
    }
}