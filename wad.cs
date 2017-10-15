using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

public static partial class game_engine
{
    public static byte[] wad_base;
    public static Dictionary<string, lumpinfo_t> _Lumps;
    public static GCHandle _Handle;
    public static IntPtr _DataPtr;
    
    public static void W_LoadWadFile(string filename)
    {
        wad_base = COM_LoadFile(filename);
        if (wad_base == null)
            Sys_Error("Wad.LoadWadFile: couldn't load {0}", filename);

        if (_Handle.IsAllocated)
        {
            _Handle.Free();
        }
        _Handle = GCHandle.Alloc(wad_base, GCHandleType.Pinned);
        _DataPtr = _Handle.AddrOfPinnedObject();

        wadinfo_t header = BytesToStructure<wadinfo_t>(wad_base, 0);

        if (header.identification[0] != 'W' || header.identification[1] != 'A' ||
            header.identification[2] != 'D' || header.identification[3] != '2')
            Sys_Error("Wad file {0} doesn't have WAD2 id\n", filename);

        int numlumps = LittleLong(header.numlumps);
        int infotableofs = LittleLong(header.infotableofs);
        int lumpInfoSize = Marshal.SizeOf(typeof(lumpinfo_t));

        _Lumps = new Dictionary<string, lumpinfo_t>(numlumps);

        for (int i = 0; i < numlumps; i++)
        {
            IntPtr ptr = new IntPtr(_DataPtr.ToInt64() + infotableofs + i * lumpInfoSize);
            lumpinfo_t lump = (lumpinfo_t)Marshal.PtrToStructure(ptr, typeof(lumpinfo_t));
            lump.filepos = LittleLong(lump.filepos);
            lump.size = LittleLong(lump.size);
            if (lump.type == q_shared.TYP_QPIC)
            {
                ptr = new IntPtr(_DataPtr.ToInt64() + lump.filepos);
                dqpicheader_t pic = (dqpicheader_t)Marshal.PtrToStructure(ptr, typeof(dqpicheader_t));
                SwapPic(pic);
                Marshal.StructureToPtr(pic, ptr, true);
            }
            _Lumps.Add(Encoding.ASCII.GetString(lump.name).TrimEnd('\0').ToLower(), lump);
        }
    }
    public static lumpinfo_t W_GetLumpinfo(string name)
    {
        lumpinfo_t lump;
        if (_Lumps.TryGetValue(name, out lump))
        {
            return lump;
        }
        else
        {
            Sys_Error("W_GetLumpinfo: {0} not found", name);
        }
        // We must never be there
        throw new InvalidOperationException("W_GetLumpinfo: Unreachable code reached!");
    }
    public static int W_GetLumpName(string name)
    {
        return W_GetLumpinfo(name).filepos;
    }
    public static void SwapPic(dqpicheader_t pic)
    {
        pic.width = LittleLong(pic.width);
        pic.height = LittleLong(pic.height);
    }
}