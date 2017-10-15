using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;

public static partial class game_engine
{
    public static cmd_source_t cmd_source;
    public static Dictionary<string, string> cmd_alias = new Dictionary<string, string>();
    public static Dictionary<string, xcommand_t> cmd_functions = new Dictionary<string, xcommand_t>();
    public static int cmd_argc;
    public static string[] cmd_argv;
    public static string cmd_args;
    public static StringBuilder cmd_text = new StringBuilder(8192);
    public static bool cmd_wait;
    
    public static void Cmd_Init()
    {
        //
        // register our commands
        //
        Cmd_AddCommand("stuffcmds", Cmd_StuffCmds_f);
        Cmd_AddCommand("exec", Cmd_Exec_f);
        Cmd_AddCommand("echo", Cmd_Echo_f);
        Cmd_AddCommand("alias", Cmd_Alias_f);
        Cmd_AddCommand("cmd", Cmd_ForwardToServer);
        Cmd_AddCommand("wait", Cmd_Wait_f);
    }
    public static void Cmd_AddCommand(string name, xcommand_t function)
    {
        // ??? because hunk allocation would get stomped
        if (host_initialized)
            Sys_Error("Cmd.Add after host initialized!");

        // fail if the command is a variable name
        if (Cvar.Exists(name))
        {
            Con_Printf("Cmd.Add: {0} already defined as a var!\n", name);
            return;
        }

        // fail if the command already exists
        if (Cmd_Exists(name))
        {
            Con_Printf("Cmd.Add: {0} already defined!\n", name);
            return;
        }

        cmd_functions.Add(name, function);
    }
    static xcommand_t Find(string name)
    {
        xcommand_t result;
        cmd_functions.TryGetValue(name, out result);
        return result;
    }
    static string FindAlias(string name)
    {
        string result;
        cmd_alias.TryGetValue(name, out result);
        return result;
    }
    public static string[] Cmd_CompleteCommand(string partial)
    {
        if (String.IsNullOrEmpty(partial))
            return null;
            
        List<string> result = new List<string>();
        foreach (string cmd in cmd_functions.Keys)
        {
            if (cmd.StartsWith(partial))
                result.Add(cmd);
        }
        return (result.Count > 0 ? result.ToArray() : null);
    }
    public static string Cmd_Argv(int arg)
    {
        if (arg < 0 || arg >= cmd_argc)
            return String.Empty;

        return cmd_argv[arg];
    }
    public static bool Cmd_Exists(string name)
    {
        return (Find(name) != null);
    }
    public static void Cmd_TokenizeString(string text)
    {
        // clear the args from the last string
        cmd_argc = 0;
        cmd_args = null;
        cmd_argv = null;

        List<string> argv = new List<string>(q_shared.MAX_ARGS);
        while (!String.IsNullOrEmpty(text))
        {
            if (cmd_argc == 1)
                cmd_args = text;

            text = COM_Parse(text);

            if (String.IsNullOrEmpty(com_token))
                break;
                
            if (cmd_argc < q_shared.MAX_ARGS)
            {
                argv.Add(com_token);
                cmd_argc++;
            }
        }
        cmd_argv = argv.ToArray();
    }
    public static void Cmd_ExecuteString(string text, cmd_source_t src)
    {
        cmd_source = src;
            
        Cmd_TokenizeString(text);

        // execute the command line
        if (cmd_argc <= 0)
            return;		// no tokens

        // check functions
        xcommand_t handler = Find(cmd_argv[0]); // must search with comparison like Q_strcasecmp()
        if (handler != null)
        {
            handler();
        }
        else
        {
            // check alias
            string alias = FindAlias(cmd_argv[0]); // must search with compare func like Q_strcasecmp
            if (!String.IsNullOrEmpty(alias))
            {
                Cbuf_InsertText(alias);
            }
            else
            {
                // check cvars
                if (!Cvar.Cvar_Command())
                    Con_Printf("Unknown command \"{0}\"\n", cmd_argv[0]);
            }
        }
    }
    public static void Cmd_ForwardToServer()
    {
        if (cls.state != cactive_t.ca_connected)
        {
            Con_Printf("Can't \"{0}\", not connected\n", Cmd_Argv(0));
            return;
        }

        if (cls.demoplayback)
            return;		// not really connected

        MsgWriter writer = cls.message;
        writer.MSG_WriteByte(q_shared.clc_stringcmd);
        if (!Cmd_Argv(0).Equals("cmd"))
        {
            writer.SZ_Print(Cmd_Argv(0) + " ");
        }
        if (cmd_argc > 1)
        {
            writer.SZ_Print(cmd_args);
        }
        else
        {
            writer.SZ_Print("\n");
        }
    }
    static void Cmd_StuffCmds_f()
    {
        if (cmd_argc != 1)
        {
            Con_Printf("stuffcmds : execute command line parameters\n");
            return;
        }

        // build the combined string to parse from
        StringBuilder sb = new StringBuilder(1024);
        for (int i = 1; i < cmd_argc; i++)
        {
            if (!String.IsNullOrEmpty(cmd_argv[i]))
            {
                sb.Append(cmd_argv[i]);
                if (i + 1 < cmd_argc)
                    sb.Append(" ");
            }
        }

        // pull out the commands
        string text = sb.ToString();
        sb.Length = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '+')
            {
                i++;

                int j = i;
                while ((j < text.Length) && (text[j] != '+') && (text[j] != '-'))
                {
                    j++;
                }

                sb.Append(text.Substring(i, j - i + 1));
                sb.AppendLine();
                i = j - 1;
            }
        }

        if (sb.Length > 0)
        {
            Cbuf_InsertText(sb.ToString());
        }
    }
    static void Cmd_Exec_f()
    {
	    if (cmd_argc != 2)
	    {
		    Con_Printf("exec <filename> : execute a script file\n");
		    return;
	    }

        byte[] bytes = COM_LoadFile(cmd_argv[1]);
        if (bytes == null)
        {
            Con_Printf("couldn't exec {0}\n", cmd_argv[1]);
            return;
        }
        string script = Encoding.ASCII.GetString(bytes);
	    Con_Printf("execing {0}\n", cmd_argv[1]);
	    Cbuf_InsertText(script);
    }
    static void Cmd_Echo_f()
    {
	    for (int i = 1; i < cmd_argc; i++)
        {
		    Con_Printf("{0} ", cmd_argv[i]);
        }
	    Con_Printf("\n");
    }
    static void Cmd_Alias_f()
    {
	    if (cmd_argc == 1)
	    {
		    Con_Printf("Current alias commands:\n");
            foreach(KeyValuePair<string, string> alias in cmd_alias)
            {
                Con_Printf("{0} : {1}\n", alias.Key, alias.Value);
            }
		    return;
	    }

	    string name = cmd_argv[1];
	    if (name.Length >= q_shared.MAX_ALIAS_NAME)
	    {
		    Con_Printf("Alias name is too long\n");
		    return;
	    }

        // copy the rest of the command line
        StringBuilder sb = new StringBuilder(1024);
        for (int i = 2; i < cmd_argc; i++)
        {
            sb.Append(cmd_argv[i]);
            if (i + 1 < cmd_argc)
                sb.Append(" ");
        }
        sb.AppendLine();
        cmd_alias[name] = sb.ToString();
    }
    public static void Cmd_Wait_f()
    {
        cmd_wait = true;
    }
    public static string JoinArgv()
    {
        return String.Join(" ", cmd_argv);
    }


    public static void Cbuf_Init()
    {
        // nothing to do
    }
    public static void Cbuf_AddText(string text)
    {
        if (String.IsNullOrEmpty(text))
            return;

        int len = text.Length;
        if (cmd_text.Length + len > cmd_text.Capacity)
        {
            Con_Printf("Cmd.AddText: overflow!\n");
        }
        else
        {
            cmd_text.Append(text);
        }
    }
    public static void Cbuf_InsertText(string text)
    {
        cmd_text.Insert(0, text);
    }
    public static void Cbuf_Execute()
    {
        while (cmd_text.Length > 0)
        {
            string text = cmd_text.ToString();

            // find a \n or ; line break
            int quotes = 0, i;
            for (i = 0; i < text.Length; i++)
            {
                if (text[i] == '"')
                    quotes++;

                if (((quotes & 1) == 0) && (text[i] == ';'))
                    break;  // don't break if inside a quoted string

                if (text[i] == '\n')
                    break;
            }

            string line = text.Substring(0, i).TrimEnd('\n', ';');

            // delete the text from the command buffer and move remaining commands down
            // this is necessary because commands (exec, alias) can insert data at the
            // beginning of the text buffer

            if (i == cmd_text.Length)
            {
                cmd_text.Length = 0;
            }
            else
            {
                cmd_text.Remove(0, i + 1);
            }

            // execute the command line
            if (!String.IsNullOrEmpty(line))
            {
                Cmd_ExecuteString(line, cmd_source_t.src_command);

                if (cmd_wait)
                {
                    // skip out while text still remains in buffer, leaving it
                    // for next frame
                    cmd_wait = false;
                    break;
                }
            }
        }
    }
}