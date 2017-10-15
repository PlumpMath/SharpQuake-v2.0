using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK;

public static partial class game_engine
{
    public static cvar_t bgmvolume;
    public static cvar_t volume;
    public static cvar_t nosound;
    public static cvar_t precache;
    public static cvar_t loadas8bit;
    public static cvar_t bgmbuffer;
    public static cvar_t ambient_level;
    public static cvar_t ambient_fade;
    public static cvar_t snd_noextraupdate;
    public static cvar_t snd_show;
    public static cvar_t _snd_mixahead;

    public static ISoundController sound_controller = new OpenALController();
    public static bool snd_initialized;

    public static sfx_t[] _KnownSfx = new sfx_t[q_shared.MAX_SFX];
    public static int num_sfx; 
    public static sfx_t[] ambient_sfx = new sfx_t[q_shared.NUM_AMBIENTS];
    public static bool snd_ambient = true; 
    public static dma_t shm = new dma_t();
     
    public static channel_t[] channels = new channel_t[q_shared.MAX_CHANNELS];
    public static int total_channels;
     
    public static float sound_nominal_clip_dist = 1000.0f;
    public static Vector3 listener_origin;
    public static Vector3 listener_forward;
    public static Vector3 listener_right;
    public static Vector3 listener_up;
     
    public static int soundtime;
    public static int paintedtime;
    public static bool sound_started;
    public static int snd_blocked = 0;
    public static int _OldSamplePos; // oldsamplepos from GetSoundTime()
    public static int _Buffers; // buffers from GetSoundTime()
    public static int _PlayHash = 345; // hash from S_Play()
    public static int _PlayVolHash = 543; // hash S_PlayVol

    
    public static void S_Init()
    {
        Con_Printf("\nSound Initialization\n");

        for (int i = 0; i < _KnownSfx.Length; i++)
            _KnownSfx[i] = new sfx_t();

        bgmvolume = new cvar_t("bgmvolume", "1", true);
        volume = new cvar_t("volume", "0.7", true);
        nosound = new cvar_t("nosound", "0");
        precache = new cvar_t("precache", "1");
        loadas8bit = new cvar_t("loadas8bit", "0");
        bgmbuffer = new cvar_t("bgmbuffer", "4096");
        ambient_level = new cvar_t("ambient_level", "0.3");
        ambient_fade = new cvar_t("ambient_fade", "100");
        snd_noextraupdate = new cvar_t("snd_noextraupdate", "0");
        snd_show = new cvar_t("snd_show", "0");
        _snd_mixahead = new cvar_t("_snd_mixahead", "0.1", true);

        if (HasParam("-nosound"))
		    return;

        for (int i = 0; i < channels.Length; i++)
            channels[i] = new channel_t();

        Cmd_AddCommand("play", S_Play);
	    Cmd_AddCommand("playvol", S_PlayVol);
	    Cmd_AddCommand("stopsound", S_StopAllSoundsC);
	    Cmd_AddCommand("soundlist", S_SoundList);
	    Cmd_AddCommand("soundinfo", S_SoundInfo_f);

	    snd_initialized = true;

        S_Startup();

        SND_InitScaletable();

        num_sfx = 0;

	    Con_Printf("Sound sampling rate: {0}\n", shm.speed);

	    // provides a tick sound until washed clean
	    ambient_sfx[q_shared.AMBIENT_WATER] = S_PrecacheSound ("ambience/water1.wav");
	    ambient_sfx[q_shared.AMBIENT_SKY] = S_PrecacheSound ("ambience/wind2.wav");

	    S_StopAllSounds(true);
    }
    public static void S_AmbientOff()
    {
        snd_ambient = false;
    }
    public static void S_AmbientOn()
    {
        snd_ambient = true;
    }
    public static void S_Shutdown()
    {
        if (!sound_controller.IsInitialized)
            return;

        if (shm != null)
            shm.gamealive = false;

        sound_controller.Shutdown();
        shm = null;
    }
    public static void S_TouchSound(string sample)
    {
        if (!sound_controller.IsInitialized)
            return;

        sfx_t sfx = S_FindName(sample);
        Cache_Check(sfx.cache);
    }
    public static void S_ClearBuffer()
    {
        if (!sound_controller.IsInitialized || shm == null || shm.buffer == null)
            return;

        sound_controller.ClearBuffer();
    }
    public static void S_StaticSound(sfx_t sfx, ref Vector3 origin, float vol, float attenuation)
    {
        if (sfx == null)
            return;

        if (total_channels == q_shared.MAX_CHANNELS)
        {
            Con_Printf("total_channels == MAX_CHANNELS\n");
            return;
        }

        channel_t ss = channels[total_channels];
        total_channels++;

        sfxcache_t sc = S_LoadSound(sfx);
        if (sc == null)
            return;

        if (sc.loopstart == -1)
        {
            Con_Printf("Sound {0} not looped\n", sfx.name);
            return;
        }

        ss.sfx = sfx;
        ss.origin = origin;
        ss.master_vol = (int)vol;
        ss.dist_mult = (attenuation / 64) / sound_nominal_clip_dist;
        ss.end = paintedtime + sc.length;

        SND_Spatialize(ss);
    }
    public static void S_StartSound(int entnum, int entchannel, sfx_t sfx, ref Vector3 origin, float fvol, float attenuation)
    {
        if (!sound_started || sfx == null)
            return;

        if (nosound.value != 0)
            return;

        int vol = (int)(fvol * 255);

        // pick a channel to play on
        channel_t target_chan = SND_PickChannel(entnum, entchannel);
        if (target_chan == null)
            return;

        // spatialize
        //memset (target_chan, 0, sizeof(*target_chan));
        target_chan.origin = origin;
        target_chan.dist_mult = attenuation / sound_nominal_clip_dist;
        target_chan.master_vol = vol;
        target_chan.entnum = entnum;
        target_chan.entchannel = entchannel;
        SND_Spatialize(target_chan);

        if (target_chan.leftvol == 0 && target_chan.rightvol == 0)
            return;		// not audible at all

        // new channel
        sfxcache_t sc = S_LoadSound(sfx);
        if (sc == null)
        {
            target_chan.sfx = null;
            return;		// couldn't load the sound's data
        }

        target_chan.sfx = sfx;
        target_chan.pos = 0;
        target_chan.end = paintedtime + sc.length;

        // if an identical sound has also been started this frame, offset the pos
        // a bit to keep it from just making the first one louder
        for (int i = q_shared.NUM_AMBIENTS; i < q_shared.NUM_AMBIENTS + q_shared.MAX_DYNAMIC_CHANNELS; i++)
        {
            channel_t check = channels[i];
            if (check == target_chan)
                continue;

            if (check.sfx == sfx && check.pos == 0)
            {
                int skip = Random((int)(0.1 * shm.speed));// rand() % (int)(0.1 * shm->speed);
                if (skip >= target_chan.end)
                    skip = target_chan.end - 1;
                target_chan.pos += skip;
                target_chan.end -= skip;
                break;
            }
        }
    }
    public static void S_StopSound(int entnum, int entchannel)
    {
        for (int i = 0; i < q_shared.MAX_DYNAMIC_CHANNELS; i++)
        {
            if (channels[i].entnum == entnum &&
                channels[i].entchannel == entchannel)
            {
                channels[i].end = 0;
                channels[i].sfx = null;
                return;
            }
        }
    }
    public static sfx_t S_PrecacheSound(string sample)
    {
        if (!snd_initialized || nosound.value != 0)
            return null;

        sfx_t sfx = S_FindName(sample);

        // cache it in
        if (precache.value != 0)
            S_LoadSound(sfx);

        return sfx;
    }
    public static void S_ClearPrecache()
    {
        // nothing to do
    }
    public static void S_Update(ref Vector3 origin, ref Vector3 forward, ref Vector3 right, ref Vector3 up)
    {
        if (!snd_initialized || (snd_blocked > 0))
            return;

        listener_origin = origin;
        listener_forward = forward;
        listener_right = right;
        listener_up = up;

        // update general area ambient sound sources
        S_UpdateAmbientSounds();

        channel_t combine = null;

        // update spatialization for static and dynamic sounds	
        //channel_t ch = channels + NUM_AMBIENTS;
        for (int i = q_shared.NUM_AMBIENTS; i < total_channels; i++)
        {
            channel_t ch = channels[i];// channels + NUM_AMBIENTS;
            if (ch.sfx == null)
                continue;
                
            SND_Spatialize(ch);  // respatialize channel
            if (ch.leftvol == 0 && ch.rightvol == 0)
                continue;

            // try to combine static sounds with a previous channel of the same
            // sound effect so we don't mix five torches every frame
            if (i >= q_shared.MAX_DYNAMIC_CHANNELS + q_shared.NUM_AMBIENTS)
            {
                // see if it can just use the last one
                if (combine != null && combine.sfx == ch.sfx)
                {
                    combine.leftvol += ch.leftvol;
                    combine.rightvol += ch.rightvol;
                    ch.leftvol = ch.rightvol = 0;
                    continue;
                }
                // search for one
                combine = channels[q_shared.MAX_DYNAMIC_CHANNELS + q_shared.NUM_AMBIENTS];// channels + MAX_DYNAMIC_CHANNELS + NUM_AMBIENTS;
                int j;
                for (j = q_shared.MAX_DYNAMIC_CHANNELS + q_shared.NUM_AMBIENTS; j < i; j++)
                {
                    combine = channels[j];
                    if (combine.sfx == ch.sfx)
                        break;
                }

                if (j == total_channels)
                {
                    combine = null;
                }
                else
                {
                    if (combine != ch)
                    {
                        combine.leftvol += ch.leftvol;
                        combine.rightvol += ch.rightvol;
                        ch.leftvol = ch.rightvol = 0;
                    }
                    continue;
                }
            }
        }

        //
        // debugging output
        //
        if (snd_show.value != 0)
        {
            int total = 0;
            for (int i = 0; i < total_channels; i++)
            {
                channel_t ch = channels[i];
                if (ch.sfx != null && (ch.leftvol > 0 || ch.rightvol > 0))
                {
                    total++;
                }
            }
            Con_Printf("----({0})----\n", total);
        }

        // mix some sound
        S_Update_();
    }
    public static void S_StopAllSounds(bool clear)
    {
        if (!sound_controller.IsInitialized)
            return;

        total_channels = q_shared.MAX_DYNAMIC_CHANNELS + q_shared.NUM_AMBIENTS;	// no statics

        for (int i = 0; i < q_shared.MAX_CHANNELS; i++)
            if (channels[i].sfx != null)
                channels[i].Clear();

        if (clear)
            S_ClearBuffer();
    }
    public static void S_BeginPrecaching()
    {
        // nothing to do
    }
    public static void S_EndPrecaching()
    {
        // nothing to do
    }
    public static void S_ExtraUpdate()
    {
        if (!snd_initialized)
            return;
#if _WIN32
	    IN_Accumulate ();
#endif

	    if (snd_noextraupdate.value != 0)
		    return;		// don't pollute timings

        S_Update_();
    }
    public static void S_LocalSound(string sound)
    {
        if (nosound.value != 0)
            return;
            
        if (!sound_controller.IsInitialized)
            return;

        sfx_t sfx = S_PrecacheSound(sound);
        if (sfx == null)
        {
            Con_Printf("S_LocalSound: can't cache {0}\n", sound);
            return;
        }
        S_StartSound(cl.viewentity, -1, sfx, ref q_shared.ZeroVector, 1, 1);
    }
    static void S_Play()
    {
        for (int i = 1; i < cmd_argc; i++)
        {
            string name = Cmd_Argv(i);
            int k = name.IndexOf('.');
            if (k == -1)
                name += ".wav";

            sfx_t sfx = S_PrecacheSound(name);
            S_StartSound(_PlayHash++, 0, sfx, ref listener_origin, 1.0f, 1.0f);
        }
    }
    static void S_PlayVol()
    {
        for (int i = 1; i < cmd_argc; i += 2)
        {
            string name = Cmd_Argv(i);
            int k = name.IndexOf('.');
            if (k == -1)
                name += ".wav";

            sfx_t sfx = S_PrecacheSound(name);
            float vol = float.Parse(Cmd_Argv(i + 1));
            S_StartSound(_PlayVolHash++, 0, sfx, ref listener_origin, vol, 1.0f);
        }
    }
    static void S_SoundList()
    {
        int total = 0;
        for (int i = 0; i < num_sfx; i++ )
        {
            sfx_t sfx = _KnownSfx[i];
            sfxcache_t sc = (sfxcache_t)Cache_Check(sfx.cache);
            if (sc == null)
                continue;
                
            int size = sc.length * sc.width * (sc.stereo + 1);
            total += size;
            if (sc.loopstart >= 0)
                Con_Printf("L");
            else
                Con_Printf(" ");
            Con_Printf("({0:d2}b) {1:g6} : {2}\n", sc.width * 8, size, sfx.name);
        }
        Con_Printf("Total resident: {0}\n", total);
    }
    static void S_SoundInfo_f()
    {
        if (!sound_controller.IsInitialized || shm == null)
        {
            Con_Printf("sound system not started\n");
            return;
        }

        Con_Printf("{0:d5} stereo\n", shm.channels - 1);
        Con_Printf("{0:d5} samples\n", shm.samples);
        Con_Printf("{0:d5} samplepos\n", shm.samplepos);
        Con_Printf("{0:d5} samplebits\n", shm.samplebits);
        Con_Printf("{0:d5} submission_chunk\n", shm.submission_chunk);
        Con_Printf("{0:d5} speed\n", shm.speed);
        //Con.Print("0x%x dma buffer\n", _shm.buffer);
        Con_Printf("{0:d5} total_channels\n", total_channels);
    }
    static void S_StopAllSoundsC()
    {
	    S_StopAllSounds(true);
    }
    public static void S_Startup()
    {
        if (snd_initialized && !sound_controller.IsInitialized)
        {
            sound_controller.Init();
            sound_started = sound_controller.IsInitialized;
        }
    }
    static sfx_t S_FindName(string name)
    {
        if (String.IsNullOrEmpty(name))
            Sys_Error("S_FindName: NULL or empty\n");

        if (name.Length >= q_shared.MAX_QPATH)
            Sys_Error("Sound name too long: {0}", name);

        // see if already loaded
        for (int i = 0; i < num_sfx; i++)
        {
            if (_KnownSfx[i].name == name)// !Q_strcmp(known_sfx[i].name, name))
                return _KnownSfx[i];
        }

        if (num_sfx == q_shared.MAX_SFX)
            Sys_Error("S_FindName: out of sfx_t");

        sfx_t sfx = _KnownSfx[num_sfx];
        sfx.name = name;

        num_sfx++;
        return sfx;
    }
    static void SND_Spatialize(channel_t ch)
    {
        // anything coming from the view entity will allways be full volume
        if (ch.entnum == cl.viewentity)
        {
            ch.leftvol = ch.master_vol;
            ch.rightvol = ch.master_vol;
            return;
        }

        // calculate stereo seperation and distance attenuation
        sfx_t snd = ch.sfx;
        Vector3 source_vec = ch.origin - listener_origin;

        float dist = Mathlib.Normalize(ref source_vec) * ch.dist_mult;
        float dot = Vector3.Dot(listener_right, source_vec);
            
        float rscale, lscale;
        if (shm.channels == 1)
        {
            rscale = 1.0f;
            lscale = 1.0f;
        }
        else
        {
            rscale = 1.0f + dot;
            lscale = 1.0f - dot;
        }

        // add in distance effect
        float scale = (1.0f - dist) * rscale;
        ch.rightvol = (int)(ch.master_vol * scale);
        if (ch.rightvol < 0)
            ch.rightvol = 0;

        scale = (1.0f - dist) * lscale;
        ch.leftvol = (int)(ch.master_vol * scale);
        if (ch.leftvol < 0)
            ch.leftvol = 0;
    }
    static sfxcache_t S_LoadSound(sfx_t s)
    {
        // see if still in memory
        sfxcache_t sc = (sfxcache_t)Cache_Check(s.cache);
        if (sc != null)
            return sc;

        // load it in
        string namebuffer = "sound/" + s.name;

        byte[] data = COM_LoadFile(namebuffer);
	    if (data == null)
	    {
		    Con_Printf("Couldn't load {0}\n", namebuffer);
		    return null;
	    }

        wavinfo_t info = GetWavInfo(s.name, data);
	    if (info.channels != 1)
	    {
		    Con_Printf("{0} is a stereo sample\n", s.name);
		    return null;
	    }

        float stepscale = info.rate / (float)shm.speed;
	    int len = (int)(info.samples / stepscale);

	    len *= info.width * info.channels;

        s.cache = Cache_Alloc(len, s.name);
        if (s.cache == null)
            return null;
            
        sc = new sfxcache_t();
	    sc.length = info.samples;
	    sc.loopstart = info.loopstart;
	    sc.speed = info.rate;
	    sc.width = info.width;
	    sc.stereo = info.channels;
        s.cache.data = sc;
	        
        ResampleSfx(s, sc.speed, sc.width, new ByteArraySegment(data, info.dataofs));

	    return sc;
    }
    static channel_t SND_PickChannel(int entnum, int entchannel)
    {
        // Check for replacement sound, or find the best one to replace
        int first_to_die = -1;
        int life_left = 0x7fffffff;
        for (int ch_idx = q_shared.NUM_AMBIENTS; ch_idx < q_shared.NUM_AMBIENTS + q_shared.MAX_DYNAMIC_CHANNELS; ch_idx++)
        {
            if (entchannel != 0		// channel 0 never overrides
                && channels[ch_idx].entnum == entnum
                && (channels[ch_idx].entchannel == entchannel || entchannel == -1))
            {
                // allways override sound from same entity
                first_to_die = ch_idx;
                break;
            }

            // don't let monster sounds override player sounds
            if (channels[ch_idx].entnum == cl.viewentity && entnum != cl.viewentity && channels[ch_idx].sfx != null)
                continue;

            if (channels[ch_idx].end - paintedtime < life_left)
            {
                life_left = channels[ch_idx].end - paintedtime;
                first_to_die = ch_idx;
            }
        }

	    if (first_to_die == -1)
		    return null;

	    if (channels[first_to_die].sfx != null)
		    channels[first_to_die].sfx = null;

        return channels[first_to_die];
    }
    static void S_UpdateAmbientSounds()
    {
        if (!snd_ambient)
            return;

        // calc ambient sound levels
        if (cl.worldmodel == null)
            return;

        mleaf_t l = Mod_PointInLeaf(ref listener_origin, cl.worldmodel);
        if (l == null || ambient_level.value == 0)
        {
            for (int i = 0; i < q_shared.NUM_AMBIENTS; i++)
                channels[i].sfx = null;
            return;
        }

        for (int i = 0; i < q_shared.NUM_AMBIENTS; i++)
        {
            channel_t chan = channels[i];
            chan.sfx = ambient_sfx[i];

            float vol = ambient_level.value * l.ambient_sound_level[i];
            if (vol < 8)
                vol = 0;

            // don't adjust volume too fast
            if (chan.master_vol < vol)
            {
                chan.master_vol += (int)(host_framtime * ambient_fade.value);
                if (chan.master_vol > vol)
                    chan.master_vol = (int)vol;
            }
            else if (chan.master_vol > vol)
            {
                chan.master_vol -= (int)(host_framtime * ambient_fade.value);
                if (chan.master_vol < vol)
                    chan.master_vol = (int)vol;
            }

            chan.leftvol = chan.rightvol = chan.master_vol;
        }
    }
    static void S_Update_()
    {
	    if (!sound_started || (snd_blocked > 0))
		    return;

        // Updates DMA time
	    GetSoundtime();

        // check to make sure that we haven't overshot
	    if (paintedtime < soundtime)
		    paintedtime = soundtime;

        // mix ahead of current position
	    int endtime = (int)(soundtime + _snd_mixahead.value * shm.speed);
	    int samps = shm.samples >> (shm.channels - 1);
	    if (endtime - soundtime > samps)
		    endtime = soundtime + samps;

	    S_PaintChannels(endtime);
    }
    static void GetSoundtime()
    {
	    int fullsamples = shm.samples / shm.channels;
        int samplepos = sound_controller.GetPosition();
	    if (samplepos < _OldSamplePos)
	    {
		    _Buffers++;	// buffer wrapped
		
		    if (paintedtime > 0x40000000)
		    {
                // time to chop things off to avoid 32 bit limits
			    _Buffers = 0;
			    paintedtime = fullsamples;
                S_StopAllSounds(true);
		    }
	    }
	    _OldSamplePos = samplepos;
        soundtime = _Buffers * fullsamples + samplepos / shm.channels;
    }
    public static void S_BlockSound()
    {
        snd_blocked++;

        if (snd_blocked == 1)
        {
            sound_controller.ClearBuffer();
        }
    }
    public static void S_UnblockSound()
    {
        snd_blocked--;
    }
}