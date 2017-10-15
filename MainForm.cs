using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;

public class MainForm : GameWindow
{
    static byte[] _KeyTable = new byte[130]
    {
        0, q_shared.K_SHIFT, q_shared.K_SHIFT, q_shared.K_CTRL, q_shared.K_CTRL, q_shared.K_ALT, q_shared.K_ALT, 0, // 0 - 7
        0, 0, q_shared.K_F1, q_shared.K_F2, q_shared.K_F3, q_shared.K_F4, q_shared.K_F5, q_shared.K_F6, // 8 - 15
        q_shared.K_F7, q_shared.K_F8, q_shared.K_F9, q_shared.K_F10, q_shared.K_F11, q_shared.K_F12, 0, 0, // 16 - 23
        0, 0, 0, 0, 0, 0, 0, 0, // 24 - 31
        0, 0, 0, 0, 0, 0, 0, 0, // 32 - 39
        0, 0, 0, 0, 0, q_shared.K_UPARROW, q_shared.K_DOWNARROW, q_shared.K_LEFTARROW, // 40 - 47
        q_shared.K_RIGHTARROW, q_shared.K_ENTER, q_shared.K_ESCAPE, q_shared.K_SPACE, q_shared.K_TAB, q_shared.K_BACKSPACE, q_shared.K_INS, q_shared.K_DEL, // 48 - 55
        q_shared.K_PGUP, q_shared.K_PGDN, q_shared.K_HOME, q_shared.K_END, 0, 0, 0, q_shared.K_PAUSE, // 56 - 63
        0, 0, 0, q_shared.K_INS, q_shared.K_END, q_shared.K_DOWNARROW, q_shared.K_PGDN, q_shared.K_LEFTARROW, // 64 - 71
        0, q_shared.K_RIGHTARROW, q_shared.K_HOME, q_shared.K_UPARROW, q_shared.K_PGUP, (byte)'/', (byte)'*', (byte)'-', // 72 - 79
        (byte)'+', (byte)'.', q_shared.K_ENTER, (byte)'a', (byte)'b', (byte)'c', (byte)'d', (byte)'e', // 80 - 87
        (byte)'f', (byte)'g', (byte)'h', (byte)'i', (byte)'j', (byte)'k', (byte)'l', (byte)'m', // 88 - 95
        (byte)'n', (byte)'o', (byte)'p', (byte)'q', (byte)'r', (byte)'s', (byte)'t', (byte)'u', // 96 - 103
        (byte)'v', (byte)'w', (byte)'x', (byte)'y', (byte)'z', (byte)'0', (byte)'1', (byte)'2', // 104 - 111
        (byte)'3', (byte)'4', (byte)'5', (byte)'6', (byte)'7', (byte)'8', (byte)'9', (byte)'`', // 112 - 119
        (byte)'-', (byte)'+', (byte)'[', (byte)']', (byte)';', (byte)'\'', (byte)',', (byte)'.', // 120 - 127
        (byte)'/', (byte)'\\' // 128 - 129
    };

    static WeakReference _Instance;
    static DisplayDevice _DisplayDevice;

    int _MouseBtnState;
    Stopwatch _Swatch;
    public bool ConfirmExit = true;

    public static MainForm Instance
    {
        get { return (MainForm)_Instance.Target; }
    }
    public static DisplayDevice DisplayDevice
    {
        get { return _DisplayDevice; }
        set { _DisplayDevice = value; }
    }
    public static bool IsFullscreen
    {
        get { return (Instance.WindowState == WindowState.Fullscreen); }
    }


    private MainForm(Size size, GraphicsMode mode, bool fullScreen)
        : base(size.Width, size.Height, mode, "SharpQuake", fullScreen ? GameWindowFlags.Fullscreen : GameWindowFlags.Default)
    {
        _Instance = new WeakReference(this);
        _Swatch = new Stopwatch();
        this.VSync = VSyncMode.Off;
        this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        if (this.Keyboard != null)
        {
            this.Keyboard.KeyRepeat = true;
            this.Keyboard.KeyDown += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyDown);
            this.Keyboard.KeyUp += new EventHandler<OpenTK.Input.KeyboardKeyEventArgs>(Keyboard_KeyUp);
        }
        if (this.Mouse != null)
        {
            this.Mouse.Move += new EventHandler<OpenTK.Input.MouseMoveEventArgs>(Mouse_Move);
            this.Mouse.ButtonDown += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(Mouse_ButtonEvent);
            this.Mouse.ButtonUp += new EventHandler<OpenTK.Input.MouseButtonEventArgs>(Mouse_ButtonEvent);
            this.Mouse.WheelChanged += new EventHandler<OpenTK.Input.MouseWheelEventArgs>(Mouse_WheelChanged);
        }
    }
    void Mouse_WheelChanged(object sender, OpenTK.Input.MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            game_engine.Key_Event(q_shared.K_MWHEELUP, true);
            game_engine.Key_Event(q_shared.K_MWHEELUP, false);

        }
        else
        {
            game_engine.Key_Event(q_shared.K_MWHEELDOWN, true);
            game_engine.Key_Event(q_shared.K_MWHEELDOWN, false);
        }
    }
    void Mouse_ButtonEvent(object sender, OpenTK.Input.MouseButtonEventArgs e)
    {
        _MouseBtnState = 0;

        if (e.Button == MouseButton.Left && e.IsPressed)
            _MouseBtnState |= 1;
            
        if (e.Button == MouseButton.Right && e.IsPressed)
            _MouseBtnState |= 2;

        if (e.Button == MouseButton.Middle && e.IsPressed)
            _MouseBtnState |= 4;

        game_engine.IN_MouseEvent(_MouseBtnState);
    }
    void Mouse_Move(object sender, OpenTK.Input.MouseMoveEventArgs e)
    {
        game_engine.IN_MouseEvent(_MouseBtnState);
    }
    private int MapKey(OpenTK.Input.Key srcKey)
    {
        int key = (int)srcKey;
        key &= 255;

        if (key >= _KeyTable.Length)
            return 0;
            
        if (_KeyTable[key] == 0)
            game_engine.Con_DPrintf("key 0x{0:X} has no translation\n", key);
            
        return _KeyTable[key];
    }
    void Keyboard_KeyUp(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
    {
        game_engine.Key_Event(MapKey(e.Key), false);
    }
    void Keyboard_KeyDown(object sender, OpenTK.Input.KeyboardKeyEventArgs e)
    {
        game_engine.Key_Event(MapKey(e.Key), true);
    }
    protected override void OnFocusedChanged(EventArgs e)
    {
        base.OnFocusedChanged(e);

        if (this.Focused)
            game_engine.S_UnblockSound();
        else
            game_engine.S_BlockSound();
    }
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (this.ConfirmExit)
        {
            e.Cancel = (MessageBox.Show("Are you sure you want to quit?", "Confirm Exit", MessageBoxButtons.YesNo) != DialogResult.Yes);
        }
        base.OnClosing(e);
    }        
    static MainForm CreateInstance(Size size, GraphicsMode mode, bool fullScreen)
    {
        if (_Instance != null)
        {
            throw new Exception("MainForm instance is already created!");
        }
        return new MainForm(size, mode, fullScreen);
    }
    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        try
        {
            if (this.WindowState == OpenTK.WindowState.Minimized || game_engine.block_drawing)
                game_engine.scr_skipupdate = true;	// no point in bothering to draw

            _Swatch.Stop();
            double ts = _Swatch.Elapsed.TotalSeconds;
            _Swatch.Reset();
            _Swatch.Start();
            game_engine.Host_Frame(ts);
        }
        catch (Exception ex)
        {
            // nothing to do
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        // select display device
        _DisplayDevice = DisplayDevice.Default;
        quakeparms_t parms = new quakeparms_t();
        parms.basedir = Application.StartupPath;
        string[] args2 = new string[args.Length + 1];
        args2[0] = String.Empty;
        args.CopyTo(args2, 1);
        game_engine.COM_InitArgv(args2);
        parms.argv = new string[game_engine.com_argv.Length];
        game_engine.com_argv.CopyTo(parms.argv, 0);

        if (game_engine.HasParam("-dedicated"))
            throw new Exception("Dedicated server mode not supported!");

        Size size = new Size(640, 480);
        GraphicsMode mode = new GraphicsMode();
        bool fullScreen = false;
        using (MainForm form = MainForm.CreateInstance(size, mode, fullScreen))
        {
            game_engine.Host_Init(parms);
            form.Run();
        }
        game_engine.Host_Shutdown();
        return 0; // all Ok
    }
}