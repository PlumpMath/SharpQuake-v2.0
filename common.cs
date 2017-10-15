using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Net.Sockets;
using OpenTK;

public static partial class game_engine
{
    public static IByteOrderConverter byteOrderConverter;
    public static cvar_t registered;
    public static cvar_t cmdline;
    public static string com_cachedir;
    public static string com_gamedir;
    public static List<searchpath_t> com_searchpaths;
    public static string[] com_argv;
    public static string com_cmdline;
    public static GameKind _GameKind;
    public static bool com_modified;
    public static bool static_registered;
    public static char[] _Slashes = new char[] { '/', '\\' };
    public static string com_token;
    
    
    public static void COM_Init(string path, string[] argv)
    {
        // set the byte swapping variables in a portable manner
        if (BitConverter.IsLittleEndian)
            byteOrderConverter = new LittleEndianConverter();
        else
            byteOrderConverter = new BigEndianConverter();

        com_searchpaths = new List<searchpath_t>();

        com_argv = argv;
        registered = new cvar_t("registered", "0");
        cmdline = new cvar_t("cmdline", "0", false, true);

        Cmd_AddCommand("path", COM_Path_f);

        COM_InitFilesystem();
        COM_CheckRegistered();
    }
    public static string Argv(int index)
    {
        return com_argv[index];
    }
    public static int COM_CheckParm(string parm)
    {
        for (int i = 1; i < com_argv.Length; i++)
        {
            if (com_argv[i].Equals(parm))
                return i;
        }
        return 0;
    }
    public static bool HasParam(string parm)
    {
        return (COM_CheckParm(parm) > 0);
    }
    public static void COM_InitArgv(string[] argv)
    {
        // reconstitute the command line for the cmdline externally visible cvar
        com_cmdline = String.Join(" ", argv);
        com_argv = new string[argv.Length];
        argv.CopyTo(com_argv, 0);

        bool safe = false;
        foreach (string arg in com_argv)
        {
            if (arg == "-safe")
            {
                safe = true;
                break;
            }
        }

        if (safe)
        {
            // force all the safe-mode switches. Note that we reserved extra space in
            // case we need to add these, so we don't need an overflow check
            string[] largv = new string[com_argv.Length + q_shared.safeargvs.Length];
            com_argv.CopyTo(largv, 0);
            q_shared.safeargvs.CopyTo(largv, com_argv.Length);
            com_argv = largv;
        }

        _GameKind = GameKind.StandardQuake;

        if (HasParam("-rogue"))
            _GameKind = GameKind.Rogue;

        if (HasParam("-hipnotic"))
            _GameKind = GameKind.Hipnotic;
    }
    public static string COM_Parse(string data)
    {
        com_token = String.Empty;

        if (String.IsNullOrEmpty(data))
            return null;

        // skip whitespace
        int i = 0;
        while (i < data.Length)
        {
            while (i < data.Length)
            {
                if (data[i] > ' ')
                    break;

                i++;
            }

            if (i >= data.Length)
                return null;

            // skip // comments
            if ((data[i] == '/') && (i + 1 < data.Length) && (data[i + 1] == '/'))
            {
                while (i < data.Length && data[i] != '\n')
                    i++;
            }
            else
                break;
        }

        if (i >= data.Length)
            return null;

        int i0 = i;

        // handle quoted strings specially
        if (data[i] == '\"')
        {
            i++;
            i0 = i;
            while (i < data.Length && data[i] != '\"')
                i++;

            if (i == data.Length)
            {
                com_token = data.Substring(i0, i - i0);
                return null;
            }
            else
            {
                com_token = data.Substring(i0, i - i0);
                return (i + 1 < data.Length ? data.Substring(i + 1) : null);
            }
        }

        // parse single characters
        char c = data[i];
        if (c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':')
        {
            com_token = data.Substring(i, 1);
            return (i + 1 < data.Length ? data.Substring(i + 1) : null);
        }

        // parse a regular word
        while (i < data.Length)
        {
            c = data[i];
            if (c <= 32 || c == '{' || c == '}' || c == ')' || c == '(' || c == '\'' || c == ':')
            {
                i--;
                break;
            }
            i++;
        }

        if (i == data.Length)
        {
            com_token = data.Substring(i0, i - i0);
            return null;
        }

        com_token = data.Substring(i0, i - i0 + 1);
        return (i + 1 < data.Length ? data.Substring(i + 1) : null);
    }
    public static byte[] COM_LoadFile(string path)
    {
        // look for it in the filesystem or pack files
        DisposableWrapper<BinaryReader> file;
        int length = COM_OpenFile(path, out file);
        if (file == null)
            return null;

        byte[] result = new byte[length];
        using (file)
        {
            Draw_BeginDisc();
            int left = length;
            while (left > 0)
            {
                int count = file.Object.Read(result, length - left, left);
                if (count == 0)
                    Sys_Error("COM_LoadFile: reading failed!");
                left -= count;
            }
            Draw_EndDisc();
        }
        return result;
    }
    public static pack_t COM_LoadPackFile(string packfile)
    {
        FileStream file = Sys_FileOpenRead(packfile);
        if (file == null)
            return null;

        dpackheader_t header = ReadStructure<dpackheader_t>(file);

        string id = Encoding.ASCII.GetString(header.id);
        if (id != "PACK")
            Sys_Error("{0} is not a packfile", packfile);

        header.dirofs = LittleLong(header.dirofs);
        header.dirlen = LittleLong(header.dirlen);

        int numpackfiles = header.dirlen / Marshal.SizeOf(typeof(dpackfile_t));

        if (numpackfiles > q_shared.MAX_FILES_IN_PACK)
            Sys_Error("{0} has {1} files", packfile, numpackfiles);

        //if (numpackfiles != PAK0_COUNT)
        //    _IsModified = true;    // not the original file

        file.Seek(header.dirofs, SeekOrigin.Begin);
        byte[] buf = new byte[header.dirlen];
        if (file.Read(buf, 0, buf.Length) != buf.Length)
        {
            Sys_Error("{0} buffering failed!", packfile);
        }
        List<dpackfile_t> info = new List<dpackfile_t>(q_shared.MAX_FILES_IN_PACK);
        GCHandle handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            int count = 0, structSize = Marshal.SizeOf(typeof(dpackfile_t));
            while (count < header.dirlen)
            {
                dpackfile_t tmp = (dpackfile_t)Marshal.PtrToStructure(ptr, typeof(dpackfile_t));
                info.Add(tmp);
                ptr = new IntPtr(ptr.ToInt64() + structSize);
                count += structSize;
            }
            if (numpackfiles != info.Count)
            {
                Sys_Error("{0} directory reading failed!", packfile);
            }
        }
        finally
        {
            handle.Free();
        }


        // crc the directory to check for modifications
        //ushort crc;
        //CRC.Init(out crc);
        //for (int i = 0; i < buf.Length; i++)
        //    CRC.ProcessByte(ref crc, buf[i]);
        //if (crc != PAK0_CRC)
        //    _IsModified = true;

        buf = null;

        // parse the directory
        packfile_t[] newfiles = new packfile_t[numpackfiles];
        for (int i = 0; i < numpackfiles; i++)
        {
            packfile_t pf = new packfile_t();
            pf.name = GetString(info[i].name);
            pf.filepos = LittleLong(info[i].filepos);
            pf.filelen = LittleLong(info[i].filelen);
            newfiles[i] = pf;
        }

        pack_t pack = new pack_t(packfile, new BinaryReader(file, Encoding.ASCII), newfiles);
        Con_Printf("Added packfile {0} ({1} files)\n", packfile, numpackfiles);
        return pack;
    }
    static void COM_CopyFile(string netpath, string cachepath)
    {
        using (Stream src = Sys_FileOpenRead(netpath), dest = Sys_FileOpenWrite(cachepath))
        {
            if (src == null)
            {
                Sys_Error("CopyFile: cannot open file {0}\n", netpath);
            }
            long remaining = src.Length;
            string dirName = Path.GetDirectoryName(cachepath);
            if (!Directory.Exists(dirName))
                Directory.CreateDirectory(dirName);

            byte[] buf = new byte[4096];
            while (remaining > 0)
            {
                int count = buf.Length;
                if (remaining < count)
                    count = (int)remaining;

                src.Read(buf, 0, count);
                dest.Write(buf, 0, count);
                remaining -= count;
            }
        }
    }
    static int COM_FindFile(string filename, out DisposableWrapper<BinaryReader> file, bool duplicateStream)
    {
        file = null;

        string cachepath = String.Empty;

        //
        // search through the path, one element at a time
        //
        foreach (searchpath_t sp in com_searchpaths)
        {
            // is the element a pak file?
            if (sp.pack != null)
            {
                // look through all the pak file elements
                pack_t pak = sp.pack;
                foreach (packfile_t pfile in pak.files)
                {
                    if (pfile.name.Equals(filename))
                    {
                        // found it!
                        Con_DPrintf("PackFile: {0} : {1}\n", sp.pack.filename, filename);
                        if (duplicateStream)
                        {
                            FileStream pfs = (FileStream)pak.stream.BaseStream;
                            FileStream fs = new FileStream(pfs.Name, FileMode.Open, FileAccess.Read, FileShare.Read);
                            file = new DisposableWrapper<BinaryReader>(new BinaryReader(fs, Encoding.ASCII), true);
                        }
                        else
                        {
                            file = new DisposableWrapper<BinaryReader>(pak.stream, false);
                        }

                        file.Object.BaseStream.Seek(pfile.filepos, SeekOrigin.Begin);
                        return pfile.filelen;
                    }
                }
            }
            else
            {
                // check a file in the directory tree
                if (!static_registered)
                {
                    // if not a registered version, don't ever go beyond base
                    if (filename.IndexOfAny(_Slashes) != -1) // strchr (filename, '/') || strchr (filename,'\\'))
                        continue;
                }

                string netpath = sp.filename + "/" + filename;  //sprintf (netpath, "%s/%s",search->filename, filename);
                DateTime findtime = Sys_FileTime(netpath);
                if (findtime == DateTime.MinValue)
                    continue;

                // see if the file needs to be updated in the cache
                if (String.IsNullOrEmpty(com_cachedir))// !com_cachedir[0])
                {
                    cachepath = netpath; //  strcpy(cachepath, netpath);
                }
                else
                {
                    if (IsWindows)
                    {
                        if (netpath.Length < 2 || netpath[1] != ':')
                            cachepath = com_cachedir + netpath;
                        else
                            cachepath = com_cachedir + netpath.Substring(2);
                    }
                    else
                    {
                        cachepath = com_cachedir + netpath;
                    }

                    DateTime cachetime = Sys_FileTime(cachepath);
                    if (cachetime < findtime)
                        COM_CopyFile(netpath, cachepath);
                    netpath = cachepath;
                }

                Con_DPrintf("FindFile: {0}\n", netpath);
                FileStream fs = Sys_FileOpenRead(netpath);
                if (fs == null)
                {
                    file = null;
                    return -1;
                }
                file = new DisposableWrapper<BinaryReader>(new BinaryReader(fs, Encoding.ASCII), true);
                return (int)fs.Length;
            }
        }

        Con_DPrintf("FindFile: can't find {0}\n", filename);
        return -1;
    }
    static int COM_OpenFile(string filename, out DisposableWrapper<BinaryReader> file)
    {
        return COM_FindFile(filename, out file, false);
    }
    public static int COM_FOpenFile(string filename, out DisposableWrapper<BinaryReader> file)
    {
        return COM_FindFile(filename, out file, true);
    }
    static void COM_Path_f()
    {
        Con_Printf("Current search path:\n");
        foreach (searchpath_t sp in com_searchpaths)
        {
            if (sp.pack != null)
            {
                Con_Printf("{0} ({1} files)\n", sp.pack.filename, sp.pack.files.Length);
            }
            else
            {
                Con_Printf("{0}\n", sp.filename);
            }
        }
    }
    static void COM_CheckRegistered()
    {
        static_registered = false;

        byte[] buf = COM_LoadFile("gfx/pop.lmp");
        if (buf == null || buf.Length < 256)
        {
            Con_Printf("Playing shareware version.\n");
            if (com_modified)
                Sys_Error("You must have the registered version to use modified games");
            return;
        }

        ushort[] check = new ushort[buf.Length / 2];
        Buffer.BlockCopy(buf, 0, check, 0, buf.Length);
        for (int i = 0; i < 128; i++)
        {
            if (q_shared._Pop[i] != (ushort)byteOrderConverter.BigShort((short)check[i]))
                Sys_Error("Corrupted data file.");
        }

        Cvar.Cvar_Set("cmdline", com_cmdline);
        Cvar.Cvar_Set("registered", "1");
        static_registered = true;
        Con_Printf("Playing registered version.\n");
    }
    static void COM_InitFilesystem()
    {
        //
        // -basedir <path>
        // Overrides the system supplied base directory (under GAMENAME)
        //
        string basedir = String.Empty;
        int i = COM_CheckParm("-basedir");
        if ((i > 0) && (i < com_argv.Length - 1))
        {
            basedir = com_argv[i + 1];
        }
        else
        {
            basedir = host_parms.basedir;
        }

        if (!String.IsNullOrEmpty(basedir))
        {
            basedir.TrimEnd('\\', '/');
        }

        //
        // -cachedir <path>
        // Overrides the system supplied cache directory (NULL or /qcache)
        // -cachedir - will disable caching.
        //
        i = COM_CheckParm("-cachedir");
        if ((i > 0) && (i < com_argv.Length - 1))
        {
            if (com_argv[i + 1][0] == '-')
                com_cachedir = String.Empty;
            else
                com_cachedir = com_argv[i + 1];
        }
        else if (!String.IsNullOrEmpty(host_parms.cachedir))
        {
            com_cachedir = host_parms.cachedir;
        }
        else
        {
            com_cachedir = String.Empty;
        }

        //
        // start up with GAMENAME by default (id1)
        //
        COM_AddGameDirectory(basedir + "/" + q_shared.GAMENAME);

        if (HasParam("-rogue"))
            COM_AddGameDirectory(basedir + "/rogue");
        if (HasParam("-hipnotic"))
            COM_AddGameDirectory(basedir + "/hipnotic");

        //
        // -game <gamedir>
        // Adds basedir/gamedir as an override game
        //
        i = COM_CheckParm("-game");
        if ((i > 0) && (i < com_argv.Length - 1))
        {
            com_modified = true;
            COM_AddGameDirectory(basedir + "/" + com_argv[i + 1]);
        }

        //
        // -path <dir or packfile> [<dir or packfile>] ...
        // Fully specifies the exact serach path, overriding the generated one
        //
        i = COM_CheckParm("-path");
        if (i > 0)
        {
            com_modified = true;
            com_searchpaths.Clear();
            while (++i < com_argv.Length)
            {
                if (String.IsNullOrEmpty(com_argv[i]) || com_argv[i][0] == '+' || com_argv[i][0] == '-')
                    break;

                com_searchpaths.Insert(0, new searchpath_t(com_argv[i]));
            }
        }
    }
    static void COM_AddGameDirectory(string dir)
    {
        com_gamedir = dir;

        //
        // add the directory to the search path
        //
        com_searchpaths.Insert(0, new searchpath_t(dir));

        //
        // add any pak files in the format pak0.pak pak1.pak, ...
        //
        for (int i = 0; ; i++)
        {
            string pakfile = String.Format("{0}/pak{1}.pak", dir, i);
            pack_t pak = COM_LoadPackFile(pakfile);
            if (pak == null)
                break;

            com_searchpaths.Insert(0, new searchpath_t(pak));
        }
    }

    public static int atoi(string s)
    {
        if (String.IsNullOrEmpty(s))
            return 0;

        int sign = 1;
        int result = 0;
        int offset = 0;
        if (s.StartsWith("-"))
        {
            sign = -1;
            offset++;
        }

        int i = -1;

        if (s.Length > 2)
        {
            i = s.IndexOf("0x", offset, 2);
            if (i == -1)
            {
                i = s.IndexOf("0X", offset, 2);
            }
        }

        if (i == offset)
        {
            int.TryParse(s.Substring(offset + 2), System.Globalization.NumberStyles.HexNumber, null, out result);
        }
        else
        {
            i = s.IndexOf('\'', offset, 1);
            if (i != -1)
            {
                result = (byte)s[i + 1];
            }
            else
                int.TryParse(s.Substring(offset), out result);
        }
        return sign * result;
    }
    public static float atof(string s)
    {
        float v;
        float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture.NumberFormat, out v);
        return v;
    }
    public static bool SameText(string a, string b)
    {
        return (String.Compare(a, b, true) == 0);
    }
    public static bool SameText(string a, string b, int count)
    {
        return (String.Compare(a, 0, b, 0, count, true) == 0);
    }
    public static short BigShort(short l)
    {
        return byteOrderConverter.BigShort(l);
    }
    public static short LittleShort(short l)
    {
        return byteOrderConverter.LittleShort(l);
    }
    public static int BigLong(int l)
    {
        return byteOrderConverter.BigLong(l);
    }
    public static int LittleLong(int l)
    {
        return byteOrderConverter.LittleLong(l);
    }
    public static float BigFloat(float l)
    {
        return byteOrderConverter.BigFloat(l);
    }
    public static float LittleFloat(float l)
    {
        return byteOrderConverter.LittleFloat(l);
    }
    public static Vector3 LittleVector(Vector3 src)
    {
        return new Vector3(byteOrderConverter.LittleFloat(src.X),
            byteOrderConverter.LittleFloat(src.Y), byteOrderConverter.LittleFloat(src.Z));
    }
    public static Vector3 LittleVector3(float[] src)
    {
        return new Vector3(byteOrderConverter.LittleFloat(src[0]),
            byteOrderConverter.LittleFloat(src[1]), byteOrderConverter.LittleFloat(src[2]));
    }
    public static Vector4 LittleVector4(float[] src, int offset)
    {
        return new Vector4(byteOrderConverter.LittleFloat(src[offset + 0]),
            byteOrderConverter.LittleFloat(src[offset + 1]),
            byteOrderConverter.LittleFloat(src[offset + 2]),
            byteOrderConverter.LittleFloat(src[offset + 3]));
    }
    public static void FillArray<T>(T[] dest, T value)
    {
        int elementSizeInBytes = Marshal.SizeOf(typeof(T));
        int blockSize = Math.Min(dest.Length, 4096 / elementSizeInBytes);
        for (int i = 0; i < blockSize; i++)
            dest[i] = value;

        int blockSizeInBytes = blockSize * elementSizeInBytes;
        int offset = blockSizeInBytes;
        int lengthInBytes = Buffer.ByteLength(dest);
        while (true)// offset + blockSize <= lengthInBytes)
        {
            int left = lengthInBytes - offset;
            if (left < blockSizeInBytes)
                blockSizeInBytes = left;

            if (blockSizeInBytes <= 0)
                break;

            Buffer.BlockCopy(dest, 0, dest, offset, blockSizeInBytes);
            offset += blockSizeInBytes;
        }
    }
    public static void ZeroArray<T>(T[] dest, int startIndex, int length)
    {
        int elementBytes = Marshal.SizeOf(typeof(T));
        int offset = startIndex * elementBytes;
        int sizeInBytes = dest.Length * elementBytes - offset;
        while (true)
        {
            int blockSize = sizeInBytes - offset;
            if (blockSize > q_shared.ZeroBytes.Length)
                blockSize = q_shared.ZeroBytes.Length;

            if (blockSize <= 0)
                break;

            Buffer.BlockCopy(q_shared.ZeroBytes, 0, dest, offset, blockSize);
            offset += blockSize;
        }
    }
    public static string Copy(string src, int maxLength)
    {
        if (src == null)
            return null;

        return (src.Length > maxLength ? src.Substring(1, maxLength) : src);
    }
    public static void Copy(float[] src, out Vector3 dest)
    {
        dest.X = src[0];
        dest.Y = src[1];
        dest.Z = src[2];
    }
    public static void Copy(ref Vector3 src, float[] dest)
    {
        dest[0] = src.X;
        dest[1] = src.Y;
        dest[2] = src.Z;
    }
    public static string GetString(byte[] src)
    {
        int count = 0;
        while (count < src.Length && src[count] != 0)
            count++;

        return (count > 0 ? Encoding.ASCII.GetString(src, 0, count) : String.Empty);
    }
    public static Vector3 ToVector(ref v3f v)
    {
        return new Vector3(v.x, v.y, v.z);
    }
    public static void WriteInt(byte[] dest, int offset, int value)
    {
        Union4b u = Union4b.Empty;
        u.i0 = value;
        dest[offset + 0] = u.b0;
        dest[offset + 1] = u.b1;
        dest[offset + 2] = u.b2;
        dest[offset + 3] = u.b3;
    }
}