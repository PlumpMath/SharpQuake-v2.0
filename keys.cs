using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

public static partial class game_engine
{
    public static keyname_t[] _KeyNames = new keyname_t[]
    {
        new keyname_t("TAB", q_shared.K_TAB),
        new keyname_t("ENTER", q_shared.K_ENTER),
        new keyname_t("ESCAPE", q_shared.K_ESCAPE),
        new keyname_t("SPACE", q_shared.K_SPACE),
        new keyname_t("BACKSPACE", q_shared.K_BACKSPACE),
        new keyname_t("UPARROW", q_shared.K_UPARROW),
        new keyname_t("DOWNARROW", q_shared.K_DOWNARROW),
        new keyname_t("LEFTARROW", q_shared.K_LEFTARROW),
        new keyname_t("RIGHTARROW", q_shared.K_RIGHTARROW),

        new keyname_t("ALT", q_shared.K_ALT),
        new keyname_t("CTRL", q_shared.K_CTRL),
        new keyname_t("SHIFT", q_shared.K_SHIFT),

        new keyname_t("F1", q_shared.K_F1),
        new keyname_t("F2", q_shared.K_F2),
        new keyname_t("F3", q_shared.K_F3),
        new keyname_t("F4", q_shared.K_F4),
        new keyname_t("F5", q_shared.K_F5),
        new keyname_t("F6", q_shared.K_F6),
        new keyname_t("F7", q_shared.K_F7),
        new keyname_t("F8", q_shared.K_F8),
        new keyname_t("F9", q_shared.K_F9),
        new keyname_t("F10", q_shared.K_F10),
        new keyname_t("F11", q_shared.K_F11),
        new keyname_t("F12", q_shared.K_F12),

        new keyname_t("INS", q_shared.K_INS),
        new keyname_t("DEL", q_shared.K_DEL),
        new keyname_t("PGDN", q_shared.K_PGDN),
        new keyname_t("PGUP", q_shared.K_PGUP),
        new keyname_t("HOME", q_shared.K_HOME),
        new keyname_t("END", q_shared.K_END),

        new keyname_t("MOUSE1", q_shared.K_MOUSE1),
        new keyname_t("MOUSE2", q_shared.K_MOUSE2),
        new keyname_t("MOUSE3", q_shared.K_MOUSE3),

        new keyname_t("JOY1", q_shared.K_JOY1),
        new keyname_t("JOY2", q_shared.K_JOY2),
        new keyname_t("JOY3", q_shared.K_JOY3),
        new keyname_t("JOY4", q_shared.K_JOY4),

        new keyname_t("AUX1", q_shared.K_AUX1),
        new keyname_t("AUX2", q_shared.K_AUX2),
        new keyname_t("AUX3", q_shared.K_AUX3),
        new keyname_t("AUX4", q_shared.K_AUX4),
        new keyname_t("AUX5", q_shared.K_AUX5),
        new keyname_t("AUX6", q_shared.K_AUX6),
        new keyname_t("AUX7", q_shared.K_AUX7),
        new keyname_t("AUX8", q_shared.K_AUX8),
        new keyname_t("AUX9", q_shared.K_AUX9),
        new keyname_t("AUX10", q_shared.K_AUX10),
        new keyname_t("AUX11", q_shared.K_AUX11),
        new keyname_t("AUX12", q_shared.K_AUX12),
        new keyname_t("AUX13", q_shared.K_AUX13),
        new keyname_t("AUX14", q_shared.K_AUX14),
        new keyname_t("AUX15", q_shared.K_AUX15),
        new keyname_t("AUX16", q_shared.K_AUX16),
        new keyname_t("AUX17", q_shared.K_AUX17),
        new keyname_t("AUX18", q_shared.K_AUX18),
        new keyname_t("AUX19", q_shared.K_AUX19),
        new keyname_t("AUX20", q_shared.K_AUX20),
        new keyname_t("AUX21", q_shared.K_AUX21),
        new keyname_t("AUX22", q_shared.K_AUX22),
        new keyname_t("AUX23", q_shared.K_AUX23),
        new keyname_t("AUX24", q_shared.K_AUX24),
        new keyname_t("AUX25", q_shared.K_AUX25),
        new keyname_t("AUX26", q_shared.K_AUX26),
        new keyname_t("AUX27", q_shared.K_AUX27),
        new keyname_t("AUX28", q_shared.K_AUX28),
        new keyname_t("AUX29", q_shared.K_AUX29),
        new keyname_t("AUX30", q_shared.K_AUX30),
        new keyname_t("AUX31", q_shared.K_AUX31),
        new keyname_t("AUX32", q_shared.K_AUX32),

        new keyname_t("PAUSE", q_shared.K_PAUSE),

        new keyname_t("MWHEELUP", q_shared.K_MWHEELUP),
        new keyname_t("MWHEELDOWN", q_shared.K_MWHEELDOWN),

        new keyname_t("SEMICOLON", ';'),	// because a raw semicolon seperates commands
    };

    public static char[][] key_lines = new char[32][];
    public static int key_linepos;
    public static bool shift_down;
    public static int key_lastpress;
    public static int edit_line;
    public static int history_line;
    public static keydest_t key_dest;
    public static int key_count;
    public static string[] keybindings = new string[256];
    public static bool[] consolekeys = new bool[256];
    public static bool[] menubound = new bool[256];
    public static int[] keyshift = new int[256];
    public static int[] key_repeats = new int[256];
    public static bool[] keydown = new bool[256];
    public static StringBuilder chat_buffer = new StringBuilder(32);
    public static bool team_message;


    public static void Key_Init()
    {
        for (int i = 0; i < 32; i++)
        {
            key_lines[i] = new char[q_shared.MAXCMDLINE];
            key_lines[i][0] = ']'; // key_lines[i][0] = ']'; key_lines[i][1] = 0;
        }
        key_linepos = 1;

        //
        // init ascii characters in console mode
        //
        for (int i = 32; i < 128; i++)
            consolekeys[i] = true;
        consolekeys[q_shared.K_ENTER] = true;
        consolekeys[q_shared.K_TAB] = true;
        consolekeys[q_shared.K_LEFTARROW] = true;
        consolekeys[q_shared.K_RIGHTARROW] = true;
        consolekeys[q_shared.K_UPARROW] = true;
        consolekeys[q_shared.K_DOWNARROW] = true;
        consolekeys[q_shared.K_BACKSPACE] = true;
        consolekeys[q_shared.K_PGUP] = true;
        consolekeys[q_shared.K_PGDN] = true;
        consolekeys[q_shared.K_SHIFT] = true;
        consolekeys[q_shared.K_MWHEELUP] = true;
        consolekeys[q_shared.K_MWHEELDOWN] = true;
        consolekeys['`'] = false;
        consolekeys['~'] = false;

        for (int i = 0; i < 256; i++)
            keyshift[i] = i;
        for (int i = 'a'; i <= 'z'; i++)
            keyshift[i] = i - 'a' + 'A';
        keyshift['1'] = '!';
        keyshift['2'] = '@';
        keyshift['3'] = '#';
        keyshift['4'] = '$';
        keyshift['5'] = '%';
        keyshift['6'] = '^';
        keyshift['7'] = '&';
        keyshift['8'] = '*';
        keyshift['9'] = '(';
        keyshift['0'] = ')';
        keyshift['-'] = '_';
        keyshift['='] = '+';
        keyshift[','] = '<';
        keyshift['.'] = '>';
        keyshift['/'] = '?';
        keyshift[';'] = ':';
        keyshift['\''] = '"';
        keyshift['['] = '{';
        keyshift[']'] = '}';
        keyshift['`'] = '~';
        keyshift['\\'] = '|';

        menubound[q_shared.K_ESCAPE] = true;
        for (int i = 0; i < 12; i++)
            menubound[q_shared.K_F1 + i] = true;

        //
        // register our functions
        //
        Cmd_AddCommand("bind", Key_Bind_f);
        Cmd_AddCommand("unbind", Key_Unbind_f);
        Cmd_AddCommand("unbindall", Key_Unbindall_f);
    }    
    public static void Key_Event(int key, bool down)
    {
        keydown[key] = down;

        if (!down)
            key_repeats[key] = 0;

        key_lastpress = key;
        key_count++;
        if (key_count <= 0)
            return;     // just catching keys for Con_NotifyBox

        // update auto-repeat status
        if (down)
        {
            key_repeats[key]++;
            if (key != q_shared.K_BACKSPACE && key != q_shared.K_PAUSE && key != q_shared.K_PGUP && key != q_shared.K_PGDN && key_repeats[key] > 1)
            {
                return; // ignore most autorepeats
            }

            if (key >= 200 && String.IsNullOrEmpty(keybindings[key]))
                Con_Printf("{0} is unbound, hit F4 to set.\n", Key_KeynumToString(key));
        }

        if (key == q_shared.K_SHIFT)
            shift_down = down;

        //
        // handle escape specialy, so the user can never unbind it
        //
        if (key == q_shared.K_ESCAPE)
        {
            if (!down)
                return;

            switch (key_dest)
            {
                case keydest_t.key_message:
                    Key_Message(key);
                    break;

                case keydest_t.key_menu:
                    Menu.M_Keydown(key);
                    break;

                case keydest_t.key_game:
                case keydest_t.key_console:
                    Menu.M_ToggleMenu_f();
                    break;

                default:
                    Sys_Error("Bad key_dest");
                    break;
            }
            return;
        }

        //
        // key up events only generate commands if the game key binding is
        // a button command (leading + sign).  These will occur even in console mode,
        // to keep the character from continuing an action started before a console
        // switch.  Button commands include the keynum as a parameter, so multiple
        // downs can be matched with ups
        //
        if (!down)
        {
            string kb = keybindings[key];
            if (!String.IsNullOrEmpty(kb) && kb.StartsWith("+"))
            {
                Cbuf_AddText(String.Format("-{0} {1}\n", kb.Substring(1), key));
            }
            if (keyshift[key] != key)
            {
                kb = keybindings[keyshift[key]];
                if (!String.IsNullOrEmpty(kb) && kb.StartsWith("+"))
                    Cbuf_AddText(String.Format("-{0} {1}\n", kb.Substring(1), key));
            }
            return;
        }

        //
        // during demo playback, most keys bring up the main menu
        //
        if (cls.demoplayback && down && consolekeys[key] && key_dest == keydest_t.key_game)
        {
            Menu.M_ToggleMenu_f();
            return;
        }

        //
        // if not a consolekey, send to the interpreter no matter what mode is
        //
        if ((key_dest == keydest_t.key_menu && menubound[key]) ||
            (key_dest == keydest_t.key_console && !consolekeys[key]) ||
            (key_dest == keydest_t.key_game && (!con_forcedup || !consolekeys[key])))
        {
            string kb = keybindings[key];
            if (!String.IsNullOrEmpty(kb))
            {
                if (kb.StartsWith("+"))
                {
                    // button commands add keynum as a parm
                    Cbuf_AddText(String.Format("{0} {1}\n", kb, key));
                }
                else
                {
                    Cbuf_AddText(kb);
                    Cbuf_AddText("\n");
                }
            }
            return;
        }

        if (!down)
            return;     // other systems only care about key down events

        if (shift_down)
        {
            key = keyshift[key];
        }

        switch (key_dest)
        {
            case keydest_t.key_message:
                Key_Message(key);
                break;

            case keydest_t.key_menu:
                Menu.M_Keydown(key);
                break;

            case keydest_t.key_game:
            case keydest_t.key_console:
                Key_Console(key);
                break;

            default:
                Sys_Error("Bad key_dest");
                break;
        }
    }    
    public static void Key_WriteBindings(Stream dest)
    {
        StringBuilder sb = new StringBuilder(4096);
        for (int i = 0; i < 256; i++)
        {
            if (!String.IsNullOrEmpty(keybindings[i]))
            {
                sb.Append("bind \"");
                sb.Append(Key_KeynumToString(i));
                sb.Append("\" \"");
                sb.Append(keybindings[i]);
                sb.AppendLine("\"");
            }
        }
        byte[] buf = Encoding.ASCII.GetBytes(sb.ToString());
        dest.Write(buf, 0, buf.Length);
    }    
    public static void Key_SetBinding(int keynum, string binding)
    {
        if (keynum != -1)
        {
            keybindings[keynum] = binding;
        }
    }
    public static void Key_ClearStates()
    {
        for (int i = 0; i < 256; i++)
        {
            keydown[i] = false;
            key_repeats[i] = 0;
        }
    }
    public static int Key_StringToKeynum(string str)
    {
        if (String.IsNullOrEmpty(str))
            return -1;
        if (str.Length == 1)
            return str[0];

        foreach (keyname_t keyname in _KeyNames)
        {
            if (SameText(keyname.name, str))
                return keyname.keynum;
        }
        return -1;
    }
    public static void Key_Unbind_f()
    {
        if (cmd_argc != 2)
        {
            Con_Printf("unbind <key> : remove commands from a key\n");
            return;
        }

        int b = Key_StringToKeynum(Cmd_Argv(1));
        if (b == -1)
        {
            Con_Printf("\"{0}\" isn't a valid key\n", Cmd_Argv(1));
            return;
        }

        Key_SetBinding(b, null);
    }
    public static void Key_Unbindall_f()
    {
        for (int i = 0; i < 256; i++)
            if (!String.IsNullOrEmpty(keybindings[i]))
                Key_SetBinding(i, null);
    }
    public static void Key_Bind_f()
    {
        int c = cmd_argc;
        if (c != 2 && c != 3)
        {
            Con_Printf("bind <key> [command] : attach a command to a key\n");
            return;
        }

        int b = Key_StringToKeynum(Cmd_Argv(1));
        if (b == -1)
        {
            Con_Printf("\"{0}\" isn't a valid key\n", Cmd_Argv(1));
            return;
        }

        if (c == 2)
        {
            if (!String.IsNullOrEmpty(keybindings[b]))// keybindings[b])
                Con_Printf("\"{0}\" = \"{1}\"\n", Cmd_Argv(1), keybindings[b]);
            else
                Con_Printf("\"{0}\" is not bound\n", Cmd_Argv(1));
            return;
        }

        // copy the rest of the command line
        // start out with a null string
        StringBuilder sb = new StringBuilder(1024);
        for (int i = 2; i < c; i++)
        {
            if (i > 2)
                sb.Append(" ");
            sb.Append(Cmd_Argv(i));
        }

        Key_SetBinding(b, sb.ToString());
    }
    public static string Key_KeynumToString(int keynum)
    {
        if (keynum == -1)
            return "<KEY NOT FOUND>";

        if (keynum > 32 && keynum < 127)
        {
            // printable ascii
            return ((char)keynum).ToString();
        }

        foreach (keyname_t kn in _KeyNames)
        {
            if (kn.keynum == keynum)
                return kn.name;
        }
        return "<UNKNOWN KEYNUM>";
    }
    public static void Key_Message(int key)
    {
        if (key == q_shared.K_ENTER)
        {
            if (team_message)
                Cbuf_AddText("say_team \"");
            else
                Cbuf_AddText("say \"");
            Cbuf_AddText(chat_buffer.ToString());
            Cbuf_AddText("\"\n");

            key_dest = keydest_t.key_game;
            chat_buffer.Length = 0;
            return;
        }

        if (key == q_shared.K_ESCAPE)
        {
            key_dest = keydest_t.key_game;
            chat_buffer.Length = 0;
            return;
        }

        if (key < 32 || key > 127)
            return;	// non printable

        if (key == q_shared.K_BACKSPACE)
        {
            if (chat_buffer.Length > 0)
            {
                chat_buffer.Length--;
            }
            return;
        }

        if (chat_buffer.Length == 31)
            return; // all full

        chat_buffer.Append((char)key);
    }
    public static void Key_Console(int key)
    {
        if (key == q_shared.K_ENTER)
        {
            string line = new String(key_lines[edit_line]).TrimEnd('\0', ' ');
            string cmd = line.Substring(1);
            Cbuf_AddText(cmd);	// skip the >
            Cbuf_AddText("\n");
            Con_Printf("{0}\n", line);
            edit_line = (edit_line + 1) & 31;
            history_line = edit_line;
            key_lines[edit_line][0] = ']';
            key_linepos = 1;
            if (cls.state == cactive_t.ca_disconnected)
                SCR_UpdateScreen();	// force an update, because the command
            // may take some time
            return;
        }

        if (key == q_shared.K_TAB)
        {
            // command completion
            string txt = new String(key_lines[edit_line], 1, q_shared.MAXCMDLINE - 1).TrimEnd('\0', ' ');
            string[] cmds = Cmd_CompleteCommand(txt);
            string[] vars = Cvar.Cvar_CompleteVariable(txt);
            string match = null;
            if (cmds != null)
            {
                if (cmds.Length > 1 || vars != null)
                {
                    Con_Printf("\nCommands:\n");
                    foreach (string s in cmds)
                        Con_Printf("  {0}\n", s);
                }
                else
                    match = cmds[0];
            }
            if (vars != null)
            {
                if (vars.Length > 1 || cmds != null)
                {
                    Con_Printf("\nVariables:\n");
                    foreach (string s in vars)
                        Con_Printf("  {0}\n", s);
                }
                else if (match == null)
                    match = vars[0];
            }
            if (!String.IsNullOrEmpty(match))
            {
                int len = Math.Min(match.Length, q_shared.MAXCMDLINE - 3);
                for (int i = 0; i < len; i++)
                {
                    key_lines[edit_line][i + 1] = match[i];
                }
                key_linepos = len + 1;
                key_lines[edit_line][key_linepos] = ' ';
                key_linepos++;
                key_lines[edit_line][key_linepos] = '\0';
                return;
            }
        }

        if (key == q_shared.K_BACKSPACE || key == q_shared.K_LEFTARROW)
        {
            if (key_linepos > 1)
                key_linepos--;
            return;
        }

        if (key == q_shared.K_UPARROW)
        {
            do
            {
                history_line = (history_line - 1) & 31;
            } while (history_line != edit_line && (key_lines[history_line][1] == 0));
            if (history_line == edit_line)
                history_line = (edit_line + 1) & 31;
            Array.Copy(key_lines[history_line], key_lines[edit_line], q_shared.MAXCMDLINE);
            key_linepos = 0;
            while (key_lines[edit_line][key_linepos] != '\0' && key_linepos < q_shared.MAXCMDLINE)
                key_linepos++;
            return;
        }

        if (key == q_shared.K_DOWNARROW)
        {
            if (history_line == edit_line) return;
            do
            {
                history_line = (history_line + 1) & 31;
            }
            while (history_line != edit_line && (key_lines[history_line][1] == '\0'));
            if (history_line == edit_line)
            {
                key_lines[edit_line][0] = ']';
                key_linepos = 1;
            }
            else
            {
                Array.Copy(key_lines[history_line], key_lines[edit_line], q_shared.MAXCMDLINE);
                key_linepos = 0;
                while (key_lines[edit_line][key_linepos] != '\0' && key_linepos < q_shared.MAXCMDLINE)
                    key_linepos++;
            }
            return;
        }

        if (key == q_shared.K_PGUP || key == q_shared.K_MWHEELUP)
        {
            con_backscroll += 2;
            if (con_backscroll > con_totallines - (vid.height >> 3) - 1)
                con_backscroll = con_totallines - (vid.height >> 3) - 1;
            return;
        }

        if (key == q_shared.K_PGDN || key == q_shared.K_MWHEELDOWN)
        {
            con_backscroll -= 2;
            if (con_backscroll < 0)
                con_backscroll = 0;
            return;
        }

        if (key == q_shared.K_HOME)
        {
            con_backscroll = con_totallines - (vid.height >> 3) - 1;
            return;
        }

        if (key == q_shared.K_END)
        {
            con_backscroll = 0;
            return;
        }

        if (key < 32 || key > 127)
            return;	// non printable

        if (key_linepos < q_shared.MAXCMDLINE - 1)
        {
            key_lines[edit_line][key_linepos] = (char)key;
            key_linepos++;
            key_lines[edit_line][key_linepos] = '\0';
        }
    }
}