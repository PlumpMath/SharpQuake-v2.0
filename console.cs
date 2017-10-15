using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

public static partial class game_engine
{
    public static char[] con_text = new char[q_shared.CON_TEXTSIZE];
    public static int con_vislines;
    public static int con_totallines;
    public static int con_backscroll;
    public static int con_current;
    public static int con_x;
    public static int _CR;
    public static double[] con_times = new double[q_shared.NUM_CON_TIMES];
    public static int con_linewidth;
    public static bool con_debuglog;
    public static bool con_initialized;
    public static bool con_forcedup;
    public static int con_notifylines;
    public static cvar_t  con_notifytime;
    public static float con_cursorspeed = 4;
    public static FileStream _Log;
    

    public static void Con_Init()
    {
	    con_debuglog = (COM_CheckParm("-condebug") > 0);
	    if (con_debuglog)
	    {
            string path = Path.Combine(com_gamedir, q_shared.LOG_FILE_NAME);
            if (File.Exists(path))
                File.Delete(path);
                
            _Log = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
	    }

	    con_linewidth = -1;
	    Con_CheckResize();	
	        
        Con_Print("Console initialized.\n");

        //
        // register our commands
        //
        con_notifytime = new cvar_t("con_notifytime", "3");

	    Cmd_AddCommand("toggleconsole", Con_ToggleConsole_f);
	    Cmd_AddCommand("messagemode", Con_MessageMode_f);
	    Cmd_AddCommand("messagemode2", Con_MessageMode2_f);
	    Cmd_AddCommand("clear", Con_Clear_f);
	        
        con_initialized = true;
    }
    public static void Con_CheckResize()
    {
        int width = (vid.width >> 3) - 2;
	    if (width == con_linewidth)
		    return;

	    if (width < 1)	// video hasn't been initialized yet
	    {
		    width = 38;
            con_linewidth = width; // con_linewidth = width;
		    con_totallines = q_shared.CON_TEXTSIZE / con_linewidth;
            FillArray(con_text, ' '); // Q_memset (con_text, ' ', CON_TEXTSIZE);
	    }
	    else
	    {
		    int oldwidth = con_linewidth;
		    con_linewidth = width;
		    int oldtotallines = con_totallines;
		    con_totallines = q_shared.CON_TEXTSIZE / con_linewidth;
		    int numlines = oldtotallines;

		    if (con_totallines < numlines)
			    numlines = con_totallines;

		    int numchars = oldwidth;
	
		    if (con_linewidth < numchars)
			    numchars = con_linewidth;

            char[] tmp = con_text;
            con_text = new char[q_shared.CON_TEXTSIZE];
            FillArray(con_text, ' ');
		        
		    for (int i = 0; i < numlines; i++)
		    {
			    for (int j = 0; j < numchars; j++)
			    {
                    con_text[(con_totallines - 1 - i) * con_linewidth + j] = tmp[((con_current - i + oldtotallines) %
                                    oldtotallines) * oldwidth + j];
			    }
		    }

            Con_ClearNotify();
	    }

	    con_backscroll = 0;
	    con_current = con_totallines - 1;
    }
    public static void Con_DrawConsole(int lines, bool drawinput)
    {
        if (lines <= 0)
            return;

        // draw the background
        Draw_ConsoleBackground(lines);

        // draw the text
        con_vislines = lines;

        int rows = (lines - 16) >> 3;		// rows of text to draw
        int y = lines - 16 - (rows << 3);	// may start slightly negative

        for (int i = con_current - rows + 1; i <= con_current; i++, y += 8)
        {
            int j = i - con_backscroll;
            if (j < 0)
                j = 0;

            int offset = (j % con_totallines) * con_linewidth;

            for (int x = 0; x < con_linewidth; x++)
                Draw_Character((x + 1) << 3, y, con_text[offset + x]);
        }

        // draw the input prompt, user text, and cursor if desired
        if (drawinput)
            Con_DrawInput();
    }
    public static void Con_Printf(string fmt, params object[] args)
    {
        string msg = (args.Length > 0 ? String.Format(fmt, args) : fmt);
            
        // log all messages to file
        if (con_debuglog)
            Con_DebugLog(msg);

	    if (!con_initialized)
		    return;
		
	    if (cls.state == cactive_t.ca_dedicated)
		    return;		// no graphics mode

        // write it to the scrollable buffer
        Con_Print(msg);
	
        // update the screen if the console is displayed
        if (cls.signon != q_shared.SIGNONS && !scr_disabled_for_loading)
            SCR_UpdateScreen();
    }
    public static void Con_DebugLog(string msg)
    {
        if (_Log != null)
        {
            byte[] tmp = Encoding.UTF8.GetBytes(msg);
            _Log.Write(tmp, 0, tmp.Length);
        }
    }
    public static void Con_Shutdown()
    {
        if (_Log != null)
        {
            _Log.Flush();
            _Log.Dispose();
            _Log = null;
        }
    }
    public static void Con_Print(string txt)
    {
        if (String.IsNullOrEmpty(txt))
            return;

	    int mask, offset = 0;
	
	    con_backscroll = 0;

	    if (txt.StartsWith(((char)1).ToString()))// [0] == 1)
	    {
		    mask = 128;	// go to colored text
            S_LocalSound("misc/talk.wav"); // play talk wav
		    offset++;
	    }
	    else if (txt.StartsWith(((char)2).ToString())) //txt[0] == 2)
	    {
		    mask = 128;	// go to colored text
		    offset++;
	    }
	    else
		    mask = 0;

        while (offset < txt.Length)
	    {
            char c = txt[offset];
                
            int l;
	        // count word length
            for (l = 0; l < con_linewidth && offset + l < txt.Length; l++)
            {
                if (txt[offset + l] <= ' ')
                    break;
            }

	        // word wrap
		    if (l != con_linewidth && (con_x + l > con_linewidth))
			    con_x = 0;

		    offset++;

            if (_CR != 0)
            {
                con_current--;
                _CR = 0;
            }
		
		    if (con_x == 0)
		    {
			    Con_Linefeed();
		        // mark time for transparent overlay
			    if (con_current >= 0)
				    con_times[con_current % q_shared.NUM_CON_TIMES] = realtime; // realtime
		    }

		    switch (c)
		    {
		        case '\n':
			        con_x = 0;
			        break;

		        case '\r':
			        con_x = 0;
			        _CR = 1;
			        break;

		        default:	// display character and advance
			        int y = con_current % con_totallines;
			        con_text[y * con_linewidth + con_x] = (char)(c | mask);
			        con_x++;
			        if (con_x >= con_linewidth)
				        con_x = 0;
			        break;
		    }
	    }
    }
    public static void Con_DPrintf(string fmt, params object[] args)
    {
        // don't confuse non-developers with techie stuff...
	    if (developer != null ? developer.value != 0 : false)
            Con_Printf(fmt, args);
    }
    public static void Con_SafePrintf(string fmt, params object[] args)
    {
	    bool temp = scr_disabled_for_loading;
	    scr_disabled_for_loading = true;
	    Con_Printf(fmt, args);
	    scr_disabled_for_loading = temp;
    }
    public static void Con_Clear_f()
    {
        FillArray(con_text, ' ');
    }
    public static void Con_DrawNotify()
    {
        int v = 0;
        for (int i = con_current - q_shared.NUM_CON_TIMES + 1; i <= con_current; i++)
        {
            if (i < 0)
                continue;
            double time = con_times[i % q_shared.NUM_CON_TIMES];
            if (time == 0)
                continue;
            time = realtime - time;
            if (time > con_notifytime.value)
                continue;

            int textOffset = (i % con_totallines) * con_linewidth;

            clearnotify = 0;
            scr_copytop = true;

            for (int x = 0; x < con_linewidth; x++)
                Draw_Character((x + 1) << 3, v, con_text[textOffset + x]);

            v += 8;
        }

        if (key_dest == keydest_t.key_message)
        {
            clearnotify = 0;
            scr_copytop = true;

            int x = 0;

            Draw_String(8, v, "say:");
            string chat = chat_buffer.ToString();
            for (; x < chat.Length; x++)
            {
                Draw_Character((x + 5) << 3, v, chat[x]);
            }
            Draw_Character((x + 5) << 3, v, 10 + ((int)(realtime * con_cursorspeed) & 1));
            v += 8;
        }

        if (v > con_notifylines)
            con_notifylines = v;
    }
    public static void Con_ClearNotify()
    {
        for (int i = 0; i < q_shared.NUM_CON_TIMES; i++)
            con_times[i] = 0;
    }
    public static void Con_ToggleConsole_f()
    {
        if (key_dest == keydest_t.key_console)
        {
            if (cls.state == cactive_t. ca_connected)
            {
                key_dest = keydest_t.key_game;
                key_lines[edit_line][1] = '\0';	// clear any typing
                key_linepos = 1;
            }
            else
            {
                MenuBase.MainMenu.Show();
            }
        }
        else
            key_dest = keydest_t.key_console;

        SCR_EndLoadingPlaque();
        Array.Clear(con_times, 0, con_times.Length);
    }
    public static void Con_MessageMode_f()
    {
	    key_dest = keydest_t.key_message;
	    team_message = false;
    }
    public static void Con_MessageMode2_f()
    {
	    key_dest = keydest_t.key_message;
	    team_message = true;
    }
    public static void Con_Linefeed()
    {
	    con_x = 0;
	    con_current++;

        for (int i = 0; i < con_linewidth; i++)
        {
            con_text[(con_current % con_totallines) * con_linewidth + i] = ' ';
        }
    }
    public static void Con_DrawInput()
    {
        if (key_dest != keydest_t.key_console && !con_forcedup)
            return;		// don't draw anything

        // add the cursor frame
        key_lines[edit_line][key_linepos] = (char)(10 + ((int)(realtime * con_cursorspeed) & 1));

        // fill out remainder with spaces
        for (int i = key_linepos + 1; i < con_linewidth; i++)
            key_lines[edit_line][i] = ' ';

        //	prestep if horizontally scrolling
        int offset = 0;
        if (key_linepos >= con_linewidth)
            offset = 1 + key_linepos - con_linewidth;
        //text += 1 + key_linepos - con_linewidth;

        // draw it
        int y = con_vislines - 16;

        for (int i = 0; i < con_linewidth; i++)
            Draw_Character((i + 1) << 3, con_vislines - 16, key_lines[edit_line][offset + i]);

        // remove cursor
        key_lines[edit_line][key_linepos] = '\0';
    }
}