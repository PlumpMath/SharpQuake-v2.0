using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Text;
using System.Globalization;

public class cvar_t
{
    public string name;
    public string @string;
    public bool archive;
    public bool server;
    public float value;
    public cvar_t next;

    protected cvar_t()
    {
    }
    public cvar_t(string name, string value)
        : this(name, value, false)
    {
    }
    public cvar_t(string name, string value, bool archive)
        : this(name, value, archive, false)
    {
    }
    public cvar_t(string name, string value, bool archive, bool server)
    {
        if (String.IsNullOrEmpty(name))
        {
            throw new ArgumentNullException("name");
        }
        cvar_t var = Cvar.Cvar_FindVar(name);
        if (var != null)
        {
            throw new ArgumentException(String.Format("Can't register variable {0}, already defined!\n", name));
            //Con_Printf("Can't register variable %s, allready defined\n", variable->name);
            //return;
        }
        if (game_engine.Cmd_Exists(name))
        {
            throw new ArgumentException(String.Format("Can't register variable: {0} is a command!\n", name));
        }
        next = Cvar.First;
        Cvar.First = this;

        this.name = name;
        this.@string = value;
        this.archive = archive;
        this.server = server;
        this.value = game_engine.atof(@string);
    }
    public void Set(string value)
    {
        bool changed = (String.Compare(@string, value) != 0);
        if (!changed)
            return;

        this.@string = value;
        this.value = game_engine.atof(@string);

        if (server && game_engine.sv.active)
        {
            game_engine.SV_BroadcastPrint("\"{0}\" changed to \"{1}\"\n", name, @string);
        }
    }
}
public class Cvar
{
    public static cvar_t First;
    public static cvar_t Cvar_FindVar(string name)
    {
        cvar_t var = First;
        while (var != null)
        {
            if (var.name.Equals(name))
            {
                return var;
            }
            var = var.next;
        }
        return null;
    }
    public static bool Exists(string name)
    {
        return (Cvar_FindVar(name) != null);
    }
    public static float Cvar_VariableValue(string name)
    {
        float result = 0;
        cvar_t var = Cvar_FindVar(name);
        if (var != null)
        {
            result = game_engine.atof(var.@string);
        }
        return result;
    }
    public static string Cvar_VariableString(string name)
    {
        cvar_t var = Cvar_FindVar(name);
        if (var != null)
        {
            return var.@string;
        }
        return String.Empty;
    }
    public static string[] Cvar_CompleteVariable(string partial)
    {
        if (String.IsNullOrEmpty(partial))
            return null;

        List<string> result = new List<string>();
        cvar_t var = First;
        while (var != null)
        {
            if (var.name.StartsWith(partial))
                result.Add(var.name);

            var = var.next;
        }
        return (result.Count > 0 ? result.ToArray() : null);
    }
    public static void Cvar_Set(string name, string value)
    {
        cvar_t var = Cvar_FindVar(name);
        if (var == null)
        {
            // there is an error in C code if this happens
            game_engine.Con_Printf("Cvar.Set: variable {0} not found\n", name);
            return;
        }
        var.Set(value);
    }
    public static void Cvar_SetValue(string name, float value)
    {
	    Cvar_Set(name, value.ToString(CultureInfo.InvariantCulture.NumberFormat));
    }
    public static bool Cvar_Command()
    {
        // check variables
        cvar_t var = Cvar_FindVar(game_engine.Cmd_Argv(0));
        if (var == null)
            return false;

        // perform a variable print or set
        if (game_engine.cmd_argc == 1)
        {
            game_engine.Con_Printf("\"{0}\" is \"{1}\"\n", var.name, var.@string);
        }
        else
        {
            var.Set(game_engine.Cmd_Argv(1));
        }
	    return true;
    }
    public static void Cvar_WriteVariables(Stream dest)
    {
        StringBuilder sb = new StringBuilder(4096);
        cvar_t var = First;
        while (var != null)
        {
            if (var.archive)
            {
                sb.Append(var.name);
                sb.Append(" \"");
                sb.Append(var.@string);
                sb.AppendLine("\"");
            }
            var = var.next;
        }
        byte[] buf = Encoding.ASCII.GetBytes(sb.ToString());
        dest.Write(buf, 0, buf.Length);
    }
}