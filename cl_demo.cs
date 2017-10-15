using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static partial class game_engine
{
    public static void CL_Record_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        int c = cmd_argc;
        if (c != 2 && c != 3 && c != 4)
        {
            Con_Printf("record <demoname> [<map> [cd track]]\n");
            return;
        }

        if (Cmd_Argv(1).Contains(".."))
        {
            Con_Printf("Relative pathnames are not allowed.\n");
            return;
        }

        if (c == 2 && cls.state == cactive_t.ca_connected)
        {
            Con_Printf("Can not record - already connected to server\nClient demo recording must be started before connecting\n");
            return;
        }

        // write the forced cd track number, or -1
        int track;
        if (c == 4)
        {
            track = atoi(Cmd_Argv(3));
            Con_Printf("Forcing CD track to {0}\n", track);
        }
        else
            track = -1;

        string name = Path.Combine(com_gamedir, Cmd_Argv(1));

        //
        // start the map up
        //
        if (c > 2)
            Cmd_ExecuteString(String.Format("map {0}", Cmd_Argv(2)), cmd_source_t.src_command);

        //
        // open the demo file
        //
        name = Path.ChangeExtension(name, ".dem");

        Con_Printf("recording to {0}.\n", name);
        FileStream fs = Sys_FileOpenWrite(name, true);
        if (fs == null)
        {
            Con_Printf("ERROR: couldn't open.\n");
            return;
        }
        BinaryWriter writer = new BinaryWriter(fs, Encoding.ASCII);
        cls.demofile = new DisposableWrapper<BinaryWriter>(writer, true);
        cls.forcetrack = track;
        byte[] tmp = Encoding.ASCII.GetBytes(cls.forcetrack.ToString());
        writer.Write(tmp);
        writer.Write('\n');
        cls.demorecording = true;
    }
    public static void CL_Stop_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        if (!cls.demorecording)
        {
            Con_Printf("Not recording a demo.\n");
            return;
        }

        // write a disconnect message to the demo file
        Message.Clear();
        Message.MSG_WriteByte(q_shared.svc_disconnect);
        CL_WriteDemoMessage();

        // finish up
        if (cls.demofile != null)
        {
            cls.demofile.Dispose();
            cls.demofile = null;
        }
        cls.demorecording = false;
        Con_Printf("Completed demo\n");
    }
    public static void CL_PlayDemo_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        if (cmd_argc != 2)
        {
            Con_Printf("play <demoname> : plays a demo\n");
            return;
        }

        //
        // disconnect from server
        //
        CL_Disconnect();

        //
        // open the demo file
        //
        string name = Path.ChangeExtension(Cmd_Argv(1), ".dem");

        Con_Printf("Playing demo from {0}.\n", name);
        if (cls.demofile != null)
        {
            cls.demofile.Dispose();
        }
        DisposableWrapper<BinaryReader> reader;
        COM_FOpenFile(name, out reader);
        cls.demofile = reader;
        if (cls.demofile == null)
        {
            Con_Printf("ERROR: couldn't open.\n");
            cls.demonum = -1;		// stop demo loop
            return;
        }

        cls.demoplayback = true;
        cls.state = cactive_t.ca_connected;
        cls.forcetrack = 0;

        BinaryReader s = reader.Object;
        int c;
        bool neg = false;
        while (true)
        {
            c = s.ReadByte();
            if (c == '\n')
                break;

            if (c == '-')
                neg = true;
            else
                cls.forcetrack = cls.forcetrack * 10 + (c - '0');
        }

        if (neg)
            cls.forcetrack = -cls.forcetrack;
        // ZOID, fscanf is evil
        //	fscanf (cls.demofile, "%i\n", &cls.forcetrack);
    }
    public static void CL_TimeDemo_f()
    {
        if (cmd_source != cmd_source_t.src_command)
            return;

        if (cmd_argc != 2)
        {
            Con_Printf("timedemo <demoname> : gets demo speeds\n");
            return;
        }

        CL_PlayDemo_f();

        // cls.td_starttime will be grabbed at the second frame of the demo, so
        // all the loading time doesn't get counted
        cls.timedemo = true;
        cls.td_startframe = host_framecount;
        cls.td_lastframe = -1;		// get a new message this frame
    }
    public static int CL_GetMessage()
    {
        if (cls.demoplayback)
        {
            // decide if it is time to grab the next message		
            if (cls.signon == q_shared.SIGNONS)	// allways grab until fully connected
            {
                if (cls.timedemo)
                {
                    if (host_framecount == cls.td_lastframe)
                        return 0;		// allready read this frame's message
                    cls.td_lastframe = host_framecount;
                    // if this is the second frame, grab the real td_starttime
                    // so the bogus time on the first frame doesn't count
                    if (host_framecount == cls.td_startframe + 1)
                        cls.td_starttime = (float)realtime;
                }
                else if (cl.time <= cl.mtime[0])
                {
                    return 0;	// don't need another message yet
                }
            }

            // get the next message
            BinaryReader reader = ((DisposableWrapper<BinaryReader>)cls.demofile).Object;
            int size = LittleLong(reader.ReadInt32());
            if (size > q_shared.MAX_MSGLEN)
                Sys_Error("Demo message > MAX_MSGLEN");

            cl.mviewangles[1] = cl.mviewangles[0];
            cl.mviewangles[0].X = LittleFloat(reader.ReadSingle());
            cl.mviewangles[0].Y = LittleFloat(reader.ReadSingle());
            cl.mviewangles[0].Z = LittleFloat(reader.ReadSingle());

            Message.FillFrom(reader.BaseStream, size);
            if (Message.Length < size)
            {
                CL_StopPlayback();
                return 0;
            }
            return 1;
        }

        int r;
        while (true)
        {
            r = NET_GetMessage(cls.netcon);

            if (r != 1 && r != 2)
                return r;

            // discard nop keepalive message
            if (Message.Length == 1 && Message.Data[0] == q_shared.svc_nop)
                Con_Printf("<-- server to client keepalive\n");
            else
                break;
        }

        if (cls.demorecording)
            CL_WriteDemoMessage();

        return r;
    }
    public static void CL_StopPlayback()
    {
        if (!cls.demoplayback)
            return;

        if (cls.demofile != null)
        {
            cls.demofile.Dispose();
            cls.demofile = null;
        }
        cls.demoplayback = false;
        cls.state = cactive_t.ca_disconnected;

        if (cls.timedemo)
            CL_FinishTimeDemo();
    }
    public static void CL_FinishTimeDemo()
    {
        cls.timedemo = false;

        // the first frame didn't count
        int frames = (host_framecount - cls.td_startframe) - 1;
        float time = (float)realtime - cls.td_starttime;
        if (time == 0)
            time = 1;
        Con_Printf("{0} frames {1:F5} seconds {2:F2} fps\n", frames, time, frames / time);
    }
    public static void CL_WriteDemoMessage()
    {
        int len = LittleLong(Message.Length);
        BinaryWriter writer = ((DisposableWrapper<BinaryWriter>)cls.demofile).Object;
        writer.Write(len);
        writer.Write(LittleFloat(cl.viewangles.X));
        writer.Write(LittleFloat(cl.viewangles.Y));
        writer.Write(LittleFloat(cl.viewangles.Z));
        writer.Write(Message.Data, 0, Message.Length);
        writer.Flush();
    }
}