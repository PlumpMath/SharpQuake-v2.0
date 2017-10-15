using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

public static partial class game_engine
{
    public static Stopwatch _StopWatch;
    public static Random _Random = new Random();

    public static bool IsWindows
    {
        get
        {
            PlatformID platform = Environment.OSVersion.Platform;
            return (platform == PlatformID.Win32Windows || platform == PlatformID.Win32NT || platform == PlatformID.WinCE || platform == PlatformID.Xbox);
        }
    }
    public static void Sys_Error(string fmt, params object[] args)
    {
        throw new Exception(args.Length > 0 ? String.Format(fmt, args) : fmt);
    }
    public static FileStream Sys_FileOpenRead(string path)
    {
        try
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
        catch (Exception)
        {
            return null;
        }
    }
    public static FileStream Sys_FileOpenWrite(string path, bool allowFail = false)
    {
        try
        {
            return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);
        }
        catch (Exception ex)
        {
            if (!allowFail)
            {
                Sys_Error("Error opening {0}: {1}", path, ex.Message);
                throw;
            }
        }
        return null;
    }
    public static double Sys_FloatTime()
    {
        if (_StopWatch == null)
        {
            _StopWatch = new Stopwatch();
            _StopWatch.Start();
        }
        return _StopWatch.Elapsed.TotalSeconds;
    }
    public static void WriteString(BinaryWriter dest, string value)
    {
        byte[] buf = Encoding.ASCII.GetBytes(value);
        dest.Write(buf.Length);
        dest.Write(buf);
    }
    public static string ReadString(BinaryReader src)
    {
        int length = src.ReadInt32();
        if (length <= 0)
        {
            throw new Exception("Invalid string length: " + length.ToString());
        }
        byte[] buf = new byte[length];
        src.Read(buf, 0, length);
        return Encoding.ASCII.GetString(buf);
    }
    public static DateTime Sys_FileTime(string path)
    {
        if (String.IsNullOrEmpty(path) || path.LastIndexOf('*') != -1)
            return DateTime.MinValue;
        try
        {
            DateTime result = File.GetLastWriteTimeUtc(path);
            if (result.Year == 1601)
                return DateTime.MinValue; // file does not exists

            return result.ToLocalTime();
        }
        catch (IOException)
        {
            return DateTime.MinValue;
        }
    }
    public static T ReadStructure<T>(Stream stream)
    {
        int count = Marshal.SizeOf(typeof(T));
        byte[] buf = new byte[count];
        if (stream.Read(buf, 0, count) < count)
        {
            throw new IOException("Stream reading error!");
        }
        return BytesToStructure<T>(buf, 0);
    }
    public static T BytesToStructure<T>(byte[] src, int startIndex)
    {
        GCHandle handle = GCHandle.Alloc(src, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = handle.AddrOfPinnedObject();
            if (startIndex != 0)
            {
                long ptr2 = ptr.ToInt64() + startIndex;
                ptr = new IntPtr(ptr2);
            }
            return (T)Marshal.PtrToStructure(ptr, typeof(T));
        }
        finally
        {
            handle.Free();
        }
    }
    public static byte[] StructureToBytes<T>(ref T src)
    {
        byte[] buf = new byte[Marshal.SizeOf(typeof(T))];
        GCHandle handle = GCHandle.Alloc(buf, GCHandleType.Pinned);
        try
        {
            Marshal.StructureToPtr(src, handle.AddrOfPinnedObject(), true);
        }
        finally
        {
            handle.Free();
        }
        return buf;
    }
    public static void StructureToBytes<T>(ref T src, byte[] dest, int offset)
    {
        GCHandle handle = GCHandle.Alloc(dest, GCHandleType.Pinned);
        try
        {
            long addr = handle.AddrOfPinnedObject().ToInt64() + offset;
            Marshal.StructureToPtr(src, new IntPtr(addr), true);
        }
        finally
        {
            handle.Free();
        }
    }
    public static int Random()
    {
        return _Random.Next();
    }
    public static int Random(int maxValue)
    {
        return _Random.Next(maxValue);
    }
    public static void Sys_SendKeyEvents()
    {
        scr_skipupdate = false;
        MainForm.Instance.ProcessEvents();
    }
    public static string Sys_ConsoleInput()
    {
        return null; // this is needed only for dedicated servers
    }
    public static void Sys_Quit()
    {
        if (MainForm.Instance != null)
        {
            MainForm.Instance.ConfirmExit = false;
            MainForm.Instance.Exit();
        }
    }
}