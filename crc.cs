using System;
using System.Collections.Generic;
using System.Text;

public static partial class game_engine
{    
    public static void CRC_Init(out ushort crcvalue)
    {
        crcvalue = q_shared.CRC_INIT_VALUE;
    }
    public static void CRC_ProcessByte(ref ushort crcvalue, byte data)
    {
        int result = (crcvalue << 8) ^ q_shared._CrcTable[(crcvalue >> 8) ^ data];
        crcvalue = (ushort)result;
    }
    public static ushort CRC_Value(ushort crcvalue)
    {
        return (ushort)(crcvalue ^ q_shared.CRC_XOR_VALUE);
    }
}