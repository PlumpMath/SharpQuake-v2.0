using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;

public static partial class game_engine
{
    static int[,] _ScaleTable = new int[32,256];
    static portable_samplepair_t[] paintbuffer = new portable_samplepair_t[q_shared.PAINTBUFFER_SIZE];

    static void SND_InitScaletable()
    {
        for (int i = 0; i < 32; i++)
            for (int j = 0; j < 256; j++)
                _ScaleTable[i, j] = ((sbyte)j) * i * 8;
    }    
    static void S_PaintChannels(int endtime)
    {
        while (paintedtime < endtime)
        {
            // if paintbuffer is smaller than DMA buffer
            int end = endtime;
            if (endtime - paintedtime > q_shared.PAINTBUFFER_SIZE)
                end = paintedtime + q_shared.PAINTBUFFER_SIZE;

            // clear the paint buffer
            Array.Clear(paintbuffer, 0, end - paintedtime);

            // paint in the channels.
            for (int i = 0; i < total_channels; i++)
            {
                channel_t ch = channels[i];
                    
                if (ch.sfx == null)
                    continue;
                if (ch.leftvol == 0 && ch.rightvol == 0)
                    continue;

                sfxcache_t sc = S_LoadSound(ch.sfx);
                if (sc == null)
                    continue;

                int count, ltime = paintedtime;

                while (ltime < end)
                {	
                    // paint up to end
                    if (ch.end < end)
                        count = ch.end - ltime;
                    else
                        count = end - ltime;

                    if (count > 0)
                    {
                        if (sc.width == 1)
                            SND_PaintChannelFrom8(ch, sc, count);
                        else
                            SND_PaintChannelFrom16(ch, sc, count);

                        ltime += count;
                    }

                    // if at end of loop, restart
                    if (ltime >= ch.end)
                    {
                        if (sc.loopstart >= 0)
                        {
                            ch.pos = sc.loopstart;
                            ch.end = ltime + sc.length - ch.pos;
                        }
                        else
                        {	// channel just stopped
                            ch.sfx = null;
                            break;
                        }
                    }
                }

            }

            // transfer out according to DMA format
            S_TransferPaintBuffer(end);
            paintedtime = end;
        }
    }    
    static void SND_PaintChannelFrom8(channel_t ch, sfxcache_t sc, int count)
    {
        if (ch.leftvol > 255)
            ch.leftvol = 255;
        if (ch.rightvol > 255)
            ch.rightvol = 255;

        int lscale = ch.leftvol >> 3;
        int rscale = ch.rightvol >> 3;
        byte[] sfx = sc.data;
        int offset = ch.pos;

        for (int i = 0; i < count; i++)
        {
            int data = sfx[offset + i];
            paintbuffer[i].left += _ScaleTable[lscale, data];
            paintbuffer[i].right += _ScaleTable[rscale, data];
        }
        ch.pos += count;
    }    
    static void SND_PaintChannelFrom16(channel_t ch, sfxcache_t sc, int count)
    {
        int leftvol = ch.leftvol;
        int rightvol = ch.rightvol;
        byte[] sfx = sc.data;
        int offset = ch.pos * 2; // sfx = (signed short *)sc->data + ch->pos;

        for (int i = 0; i < count; i++)
        {
            int data = (short)((ushort)sfx[offset] + ((ushort)sfx[offset + 1] << 8)); // Uze: check is this is right!!!
            int left = (data * leftvol) >> 8;
            int right = (data * rightvol) >> 8;
            paintbuffer[i].left += left;
            paintbuffer[i].right += right;
            offset += 2;
        }

        ch.pos += count;
    }
    static void S_TransferPaintBuffer(int endtime)
    {
	    if (shm.samplebits == 16 && shm.channels == 2)
	    {
		    S_TransferStereo16(endtime);
		    return;
	    }
	
	    int count = (endtime - paintedtime) * shm.channels;
	    int out_mask = shm.samples - 1; 
	    int out_idx = 0; //_PaintedTime * _shm.channels & out_mask;
	    int step = 3 - shm.channels;
	    int snd_vol = (int)(volume.value*256);
        byte[] buffer = sound_controller.LockBuffer();
        Union4b uval = Union4b.Empty;
        int val, srcIndex = 0;
        bool useLeft = true;
        int destCount = (count * (shm.samplebits >> 3)) & out_mask;

        if (shm.samplebits == 16)
	    {
		    while (count-- > 0)
		    {
                if (useLeft)
                    val = (paintbuffer[srcIndex].left * snd_vol) >> 8;
                else
                    val = (paintbuffer[srcIndex].right * snd_vol) >> 8;
			    if (val > 0x7fff)
				    val = 0x7fff;
			    else if (val < q_shared.C8000)// (short)0x8000)
				    val = q_shared.C8000;// (short)0x8000;

                uval.i0 = val;
                buffer[out_idx * 2] = uval.b0;
                buffer[out_idx * 2 + 1] = uval.b1;

                if (shm.channels == 2 && useLeft)
                {
                    useLeft = false;
                    out_idx += 2;
                }
                else
                {
                    useLeft = true;
                    srcIndex++;
                    out_idx = (out_idx + 1) & out_mask;
                }
		    }
	    }
	    else if (shm.samplebits == 8)
	    {
		    while (count-- > 0)
		    {
                if (useLeft)
                    val = (paintbuffer[srcIndex].left * snd_vol) >> 8;
                else
                    val = (paintbuffer[srcIndex].right * snd_vol) >> 8;
			    if (val > 0x7fff)
				    val = 0x7fff;
			    else if (val < q_shared.C8000)//(short)0x8000)
				    val = q_shared.C8000;//(short)0x8000;

                buffer[out_idx] = (byte)((val >> 8) + 128);
                out_idx = (out_idx + 1) & out_mask;

                if (shm.channels == 2 && useLeft)
                    useLeft = false;
                else
                {
                    useLeft = true;
                    srcIndex++;
                }
		    }
	    }

        sound_controller.UnlockBuffer(destCount);
    }
    static void S_TransferStereo16(int endtime)
    {
        int snd_vol = (int)(volume.value * 256);
	    int lpaintedtime = paintedtime;
        byte[] buffer = sound_controller.LockBuffer();
        int srcOffset = 0;
        int destCount = 0;//uze
        int destOffset = 0;
        Union4b uval = Union4b.Empty;
            
        while (lpaintedtime < endtime)
	    {
	        // handle recirculating buffer issues
            int lpos = lpaintedtime & ((shm.samples >> 1) - 1);
            //int destOffset = (lpos << 2); // in bytes!!!
		    int snd_linear_count = (shm.samples>>1) - lpos; // in portable_samplepair_t's!!!
		    if (lpaintedtime + snd_linear_count > endtime)
			    snd_linear_count = endtime - lpaintedtime;

            // beginning of Snd_WriteLinearBlastStereo16
	        // write a linear blast of samples
            for (int i = 0; i < snd_linear_count; i++)
            {
                int val1 = (paintbuffer[srcOffset + i].left * snd_vol) >> 8;
                int val2 = (paintbuffer[srcOffset + i].right * snd_vol) >> 8;

                if (val1 > 0x7fff)
                    val1 = 0x7fff;
                else if (val1 < q_shared.C8000)
                    val1 = q_shared.C8000;
                        
                if (val2 > 0x7fff)
                    val2 = 0x7fff;
                else if (val2 < q_shared.C8000)
                    val2 = q_shared.C8000;
                        
                uval.s0 = (short)val1;
                uval.s1 = (short)val2;
                buffer[destOffset + 0] = uval.b0;
                buffer[destOffset + 1] = uval.b1;
                buffer[destOffset + 2] = uval.b2;
                buffer[destOffset + 3] = uval.b3;

                destOffset += 4;
            }
		    // end of Snd_WriteLinearBlastStereo16 ();
                
            // Uze
            destCount += snd_linear_count * 4;
                
            srcOffset += snd_linear_count; // snd_p += snd_linear_count;
            lpaintedtime += (snd_linear_count);// >> 1);
	    }

        sound_controller.UnlockBuffer(destCount);
    }
}