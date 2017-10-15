#if _WINDOWS

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

class CDAudioWinController : ICDAudioController
{
    IntPtr _DeviceID;
    bool _IsInitialized;
    bool _IsEnabled;
    bool _IsValidDisc;
    bool _IsPlaying;
    bool _IsLooping;
    bool _WasPlaying;
    byte[] _Remap;
    NotifyForm _Form;
    byte _PlayTrack;
    byte _MaxTrack;
    float _Volume; // cdvolume

    public IntPtr DeviceID
    {
        get { return _DeviceID; }
    }

    public CDAudioWinController()
    {
        _Remap = new byte[100];
        _Form = new NotifyForm(this);
    }
    public bool IsInitialized
    {
        get { return _IsInitialized; }
    }
    public bool IsEnabled
    {
        get { return _IsEnabled; }
        set { _IsEnabled = value; }
    }
    public bool IsPlaying
    {
        get { return _IsPlaying; }
    }
    public bool IsPaused
    {
        get { return _WasPlaying; }
    }
    public bool IsValidCD
    {
        get { return _IsValidDisc; }
    }
    public bool IsLooping
    {
        get { return _IsLooping; }
    }
    public byte[] Remap
    {
        get { return _Remap; }
    }
    public byte MaxTrack
    {
        get { return _MaxTrack; }
    }
    public byte CurrentTrack
    {
        get { return _PlayTrack; }
    }
    public float Volume
    {
        get { return _Volume; }
        set {  _Volume = value;  }
    }

    public void Init()
    {
        Mci.MCI_OPEN_PARMS parms = default(Mci.MCI_OPEN_PARMS);
        parms.lpstrDeviceType = "cdaudio";
        int ret = Mci.Open(IntPtr.Zero, Mci.MCI_OPEN, Mci.MCI_OPEN_TYPE | Mci.MCI_OPEN_SHAREABLE, ref parms);
        if (ret != 0)
        {
            game_engine.Con_Printf("CDAudio_Init: MCI_OPEN failed ({0})\n", ret);
            return;
        }
        _DeviceID = parms.wDeviceID;

        // Set the time format to track/minute/second/frame (TMSF).
        Mci.MCI_SET_PARMS sp = default(Mci.MCI_SET_PARMS);
        sp.dwTimeFormat = Mci.MCI_FORMAT_TMSF;
        ret = Mci.Set(_DeviceID, Mci.MCI_SET, Mci.MCI_SET_TIME_FORMAT, ref sp);
        if (ret != 0)
        {
            game_engine.Con_Printf("MCI_SET_TIME_FORMAT failed ({0})\n", ret);
            Mci.SendCommand(_DeviceID, Mci.MCI_CLOSE, 0, IntPtr.Zero);
            return;
        }

        for (byte n = 0; n < 100; n++)
            _Remap[n] = n;
            
        _IsInitialized = true;
        _IsEnabled = true;

        CDAudio_GetAudioDiskInfo();
        if (!_IsValidDisc)
            game_engine.Con_Printf("CDAudio_Init: No CD in player.\n");
    }
    public void Play(byte track, bool looping)
    {
        if (!_IsEnabled)
            return;

        if (!_IsValidDisc)
        {
            CDAudio_GetAudioDiskInfo();
            if (!_IsValidDisc)
                return;
        }

        track = _Remap[track];

        if (track < 1 || track > _MaxTrack)
        {
            game_engine.Con_DPrintf("CDAudio: Bad track number {0}.\n", track);
            return;
        }

        // don't try to play a non-audio track
        Mci.MCI_STATUS_PARMS sp = default(Mci.MCI_STATUS_PARMS);
        sp.dwItem = Mci.MCI_CDA_STATUS_TYPE_TRACK;
        sp.dwTrack = track;
        int ret = Mci.Status(_DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_TRACK | Mci.MCI_WAIT, ref sp);
        if (ret != 0)
        {
            game_engine.Con_DPrintf("MCI_STATUS failed ({0})\n", ret);
            return;
        }
        if (sp.dwReturn != Mci.MCI_CDA_TRACK_AUDIO)
        {
            game_engine.Con_Printf("CDAudio: track {0} is not audio\n", track);
            return;
        }

        // get the length of the track to be played
        sp.dwItem = Mci.MCI_STATUS_LENGTH;
        sp.dwTrack = track;
        ret = Mci.Status(_DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_TRACK | Mci.MCI_WAIT, ref sp);
        if (ret != 0)
        {
            game_engine.Con_DPrintf("MCI_STATUS failed ({0})\n", ret);
            return;
        }

        if (_IsPlaying)
        {
            if (_PlayTrack == track)
                return;
            Stop();
        }

        Mci.MCI_PLAY_PARMS pp;
        pp.dwFrom = Mci.MCI_MAKE_TMSF(track, 0, 0, 0);
        pp.dwTo = (sp.dwReturn << 8) | track;
        pp.dwCallback = _Form.Handle;
        ret = Mci.Play(_DeviceID, Mci.MCI_PLAY, Mci.MCI_NOTIFY | Mci.MCI_FROM | Mci.MCI_TO, ref pp);
        if (ret != 0)
        {
            game_engine.Con_DPrintf("CDAudio: MCI_PLAY failed ({0})\n", ret);
            return;
        }

        _IsLooping = looping;
        _PlayTrack = track;
        _IsPlaying = true;

        if (_Volume == 0)
            Pause();
    }
    public void Stop()
    {
        if (!_IsEnabled)
            return;

        if (!_IsPlaying)
            return;

        int ret = Mci.SendCommand(_DeviceID, Mci. MCI_STOP, 0, IntPtr.Zero);
        if (ret != 0)
            game_engine.Con_DPrintf("MCI_STOP failed ({0})", ret);

        _WasPlaying = false;
        _IsPlaying = false;
    }
    public void Pause()
    {
        if (!_IsEnabled)
            return;

        if (!_IsPlaying)
            return;

        Mci.MCI_GENERIC_PARMS gp = default(Mci.MCI_GENERIC_PARMS);
        int ret = Mci.SendCommand(_DeviceID, Mci.MCI_PAUSE, 0, ref gp);
        if (ret != 0)
            game_engine.Con_DPrintf("MCI_PAUSE failed ({0})", ret);

        _WasPlaying = _IsPlaying;
        _IsPlaying = false;
    }
    public void Resume()
    {
        if (!_IsEnabled)
            return;

        if (!_IsValidDisc)
            return;

        if (!_WasPlaying)
            return;

        Mci.MCI_PLAY_PARMS pp;
        pp.dwFrom = Mci. MCI_MAKE_TMSF(_PlayTrack, 0, 0, 0);
        pp.dwTo = Mci. MCI_MAKE_TMSF(_PlayTrack + 1, 0, 0, 0);
        pp.dwCallback = _Form.Handle;// (DWORD)mainwindow;
        int ret = Mci.Play(_DeviceID, Mci.MCI_PLAY, Mci. MCI_TO | Mci. MCI_NOTIFY, ref pp);
        if (ret != 0)
            game_engine.Con_DPrintf("CDAudio: MCI_PLAY failed ({0})\n", ret);

        _IsPlaying = (ret == 0);
    }
    public void Shutdown()
    {
        if (_Form != null)
        {
            _Form.Dispose();
            _Form = null;
        }
            
        if (!_IsInitialized)
            return;
            
        Stop();

        if (Mci.SendCommand(_DeviceID, Mci.MCI_CLOSE, Mci. MCI_WAIT, IntPtr.Zero) != 0)
            game_engine.Con_DPrintf("CDAudio_Shutdown: MCI_CLOSE failed\n");
    }
    public void Update()
    {
        if (!_IsEnabled)
            return;

        if (game_engine.bgmvolume.value != _Volume)
        {
            if (_Volume != 0)
            {
                Cvar.Cvar_SetValue("bgmvolume", 0f);
                _Volume = game_engine.bgmvolume.value;
                Pause();
            }
            else
            {
                Cvar.Cvar_SetValue("bgmvolume", 1f);
                _Volume = game_engine.bgmvolume.value;
                Resume();
            }
        }
    }
    public void CDAudio_GetAudioDiskInfo()
    {
        _IsValidDisc = false;

        Mci.MCI_STATUS_PARMS sp = default(Mci.MCI_STATUS_PARMS);
        sp.dwItem = Mci.MCI_STATUS_READY;
        int ret = Mci.Status(_DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_WAIT, ref sp);
        if (ret != 0)
        {
            game_engine.Con_DPrintf("CDAudio: drive ready test - get status failed\n");
            return;
        }
        if (sp.dwReturn == 0)
        {
            game_engine.Con_DPrintf("CDAudio: drive not ready\n");
            return;
        }

        sp.dwItem = Mci.MCI_STATUS_NUMBER_OF_TRACKS;
        ret = Mci.Status(_DeviceID, Mci.MCI_STATUS, Mci.MCI_STATUS_ITEM | Mci.MCI_WAIT, ref sp);
        if (ret != 0)
        {
            game_engine.Con_DPrintf("CDAudio: get tracks - status failed\n");
            return;
        }
        if (sp.dwReturn < 1)
        {
            game_engine.Con_DPrintf("CDAudio: no music tracks\n");
            return;
        }

        _IsValidDisc = true;
        _MaxTrack = (byte)sp.dwReturn;
    }
    public void CloseDoor()
    {
        int ret = Mci.SendCommand(_DeviceID, Mci. MCI_SET, Mci. MCI_SET_DOOR_CLOSED, IntPtr.Zero);
        if (ret != 0)
            game_engine.Con_DPrintf("MCI_SET_DOOR_CLOSED failed ({0})\n", ret);
    }
    public void Edject()
    {
        int ret = Mci.SendCommand(_DeviceID, Mci.MCI_SET, Mci.MCI_SET_DOOR_OPEN, IntPtr.Zero);
        if (ret != 0)
            game_engine.Con_DPrintf("MCI_SET_DOOR_OPEN failed ({0})\n", ret);
    }    
    public void MessageHandler(ref Message m)
    {
        if (m.LParam != _DeviceID)
            return;
            
        switch (m.WParam.ToInt32())
        {
            case Mci.MCI_NOTIFY_SUCCESSFUL:
                if (_IsPlaying)
                {
                    _IsPlaying = false;
                    if (_IsLooping)
                        Play(_PlayTrack, true);
                }
                break;

            case Mci.MCI_NOTIFY_ABORTED:
            case Mci.MCI_NOTIFY_SUPERSEDED:
                break;

            case Mci.MCI_NOTIFY_FAILURE:
                game_engine.Con_DPrintf("MCI_NOTIFY_FAILURE\n");
                Stop();
                _IsValidDisc = false;
                break;

            default:
                game_engine.Con_DPrintf("Unexpected MM_MCINOTIFY type ({0})\n", m.WParam);
                m.Result = new IntPtr(1);
                break;
        }
    }
}
class NotifyForm : Form
{
    CDAudioWinController _Controller;
    public NotifyForm(CDAudioWinController ctrl)
    {
        _Controller = ctrl;
        Visible = false;
    }
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == Mci.MM_MCINOTIFY)
        {
            _Controller.MessageHandler(ref m);
            return;
        }
            
        base.WndProc(ref m);
    }
}
#endif