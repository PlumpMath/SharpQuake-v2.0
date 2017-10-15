using System;
using System.Collections.Generic;
using System.Text;

public static partial class game_engine
{
#if _WINDOWS
    static ICDAudioController _Controller = new CDAudioWinController();
#else
    static ICDAudioController _Controller = new NullCDAudioController();
#endif

    public static bool CDAudio_Init()
    {
        if (cls.state == cactive_t.ca_dedicated)
            return false;

        if (HasParam("-nocdaudio"))
            return false;

        _Controller.Init();

        if (_Controller.IsInitialized)
        {
            Cmd_AddCommand("cd", CD_f);
            Con_Printf("CD Audio Initialized\n");
        }

        return _Controller.IsInitialized;
    }
    public static void CDAudio_Play(byte track, bool looping)
    {
        _Controller.Play(track, looping);
    }
    public static void CDAudio_Stop()
    {
        _Controller.Stop();
    }
    public static void CDAudio_Pause()
    {
        _Controller.Pause();
    }
    public static void CDAudio_Resume()
    {
        _Controller.Resume();
    }
    public static void CDAudio_Shutdown()
    {
        _Controller.Shutdown();
    }
    public static void CDAudio_Update()
    {
        _Controller.Update();
    }
    static void CD_f()
    {
	    if (cmd_argc < 2)
		    return;

	    string command = Cmd_Argv(1);

	    if (SameText(command, "on"))
	    {
		    _Controller.IsEnabled = true;
		    return;
	    }

        if (SameText(command, "off"))
	    {
		    if (_Controller.IsPlaying)
			    _Controller.Stop();
		    _Controller.IsEnabled = false;
		    return;
	    }

        if (SameText(command, "reset"))
	    {
            _Controller.IsEnabled = true;
		    if (_Controller.IsPlaying)
			    _Controller.Stop();

            _Controller.CDAudio_GetAudioDiskInfo();
		    return;
	    }

        if (SameText(command, "remap"))
	    {
		    int ret = cmd_argc - 2;
            byte[] remap = _Controller.Remap;
		    if (ret <= 0)
		    {
			    for (int n = 1; n < 100; n++)
				    if (remap[n] != n)
					    Con_Printf("  {0} -> {1}\n", n, remap[n]);
			    return;
		    }
            for (int n = 1; n <= ret; n++)
                remap[n] = (byte)atoi(Cmd_Argv(n + 1));
		    return;
	    }

        if (SameText(command, "close"))
	    {
		    _Controller.CloseDoor();
		    return;
	    }

	    if (!_Controller.IsValidCD)
	    {
		    _Controller.CDAudio_GetAudioDiskInfo();
		    if (!_Controller.IsValidCD)
		    {
			    Con_Printf("No CD in player.\n");
			    return;
		    }
	    }

        if (SameText(command, "play"))
	    {
		    _Controller.Play((byte)atoi(Cmd_Argv(2)), false);
		    return;
	    }

        if (SameText(command, "loop"))
	    {
            _Controller.Play((byte)atoi(Cmd_Argv(2)), true);
		    return;
	    }

        if (SameText(command, "stop"))
	    {
            _Controller.Stop();
		    return;
	    }

        if (SameText(command, "pause"))
	    {
            _Controller.Pause();
		    return;
	    }

        if (SameText(command, "resume"))
	    {
            _Controller.Resume();
		    return;
	    }

        if (SameText(command, "eject"))
	    {
		    if (_Controller.IsPlaying)
			    _Controller.Stop();
		    _Controller.Edject();
		    return;
	    }

        if (SameText(command, "info"))
	    {
		    Con_Printf("%u tracks\n", _Controller.MaxTrack);
		    if (_Controller.IsPlaying)
			    Con_Printf("Currently {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack);
		    else if (_Controller.IsPaused)
			    Con_Printf("Paused {0} track {1}\n", _Controller.IsLooping ? "looping" : "playing", _Controller.CurrentTrack);
		    Con_Printf("Volume is {0}\n", _Controller.Volume);
		    return;
	    }
    }
}