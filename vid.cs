using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    public static ushort[] d_8to16table = new ushort[256];
    public static uint[] d_8to24table = new uint[256];
    public static byte[] d_15to8table = new byte[65536];

    public static mode_t[] vid_modes;
    public static int vid_modenum;

    public static cvar_t gl_ztrick;
    public static cvar_t vid_mode;
    public static cvar_t _vid_default_mode;
    public static cvar_t _vid_default_mode_win;
    public static cvar_t vid_wait;
    public static cvar_t vid_nopageflip;
    public static cvar_t _vid_wait_override;
    public static cvar_t vid_config_x;
    public static cvar_t vid_config_y;
    public static cvar_t vid_stretch_by_2;
    public static cvar_t _windowed_mouse;

    public static bool windowed;
    public static bool vid_initialized;
    public static float vid_gamma = 1.0f;
    public static int vid_default;
    public static bool gl_mtexable = false;

    public static string gl_vendor;
    public static string gl_renderer;
    public static string gl_version;
    public static string gl_extensions;
    

    public static void VID_Init(byte[] palette)
    {
        gl_ztrick = new cvar_t("gl_ztrick", "1");
        vid_mode = new cvar_t("vid_mode", "0", false);
        _vid_default_mode = new cvar_t("_vid_default_mode", "0", true);
        _vid_default_mode_win = new cvar_t("_vid_default_mode_win", "3", true);
        vid_wait = new cvar_t("vid_wait", "0");
        vid_nopageflip = new cvar_t("vid_nopageflip", "0", true);
        _vid_wait_override = new cvar_t("_vid_wait_override", "0", true);
        vid_config_x = new cvar_t("vid_config_x", "800", true);
        vid_config_y = new cvar_t("vid_config_y", "600", true);
        vid_stretch_by_2 = new cvar_t("vid_stretch_by_2", "1", true);
        _windowed_mouse = new cvar_t("_windowed_mouse", "1", true);

        Cmd_AddCommand("vid_nummodes", VID_NumModes_f);
        Cmd_AddCommand("vid_describecurrentmode", VID_DescribeCurrentMode_f);
        Cmd_AddCommand("vid_describemode", VID_DescribeMode_f);
        Cmd_AddCommand("vid_describemodes", VID_DescribeModes_f);

        DisplayDevice dev = MainForm.DisplayDevice;

        // Enumerate available modes, skip 8 bpp modes, and group by refresh rates
        List<mode_t> tmp = new List<mode_t>(dev.AvailableResolutions.Count);
        foreach (DisplayResolution res in dev.AvailableResolutions)
        {
            if (res.BitsPerPixel <= 8)
                continue;

            Predicate<mode_t> SameMode = delegate (mode_t m)
            {
                return (m.width == res.Width && m.height == res.Height && m.bpp == res.BitsPerPixel);
            };
            if (tmp.Exists(SameMode))
                continue;

            mode_t mode = new mode_t();
            mode.width = res.Width;
            mode.height = res.Height;
            mode.bpp = res.BitsPerPixel;
            mode.refreshRate = res.RefreshRate;
            tmp.Add(mode);
        }
        vid_modes = tmp.ToArray();

        mode_t mode1 = new mode_t();
        mode1.width = dev.Width;
        mode1.height = dev.Height;
        mode1.bpp = dev.BitsPerPixel;
        mode1.refreshRate = dev.RefreshRate;
        mode1.fullScreen = false;


        int width = dev.Width, height = dev.Height;
        int i = COM_CheckParm("-width");
        if (i > 0 && i < com_argv.Length - 1)
        {
            width = atoi(Argv(i + 1));

            foreach (DisplayResolution res in dev.AvailableResolutions)
            {
                if (res.Width == width)
                {
                    height = res.Height;
                    break;
                }
            }
        }

        i = COM_CheckParm("-height");
        if (i > 0 && i < com_argv.Length - 1)
            height = atoi(Argv(i + 1));

        mode1.width = width;
        mode1.height = height;

        // HACK: remove parameters and add the check in the host_init.
        if (HasParam("-window"))
        {
            windowed = true;
        }
        else
        {
            windowed = false;

            if (HasParam("-current"))
            {
                mode1.width = dev.Width;
                mode1.height = dev.Height;
            }
            else
            {
                int bpp = mode1.bpp;
                i = COM_CheckParm("-bpp");
                if (i > 0 && i < com_argv.Length - 1)
                {
                    bpp = atoi(Argv(i + 1));
                }
                mode1.bpp = bpp;
            }
        }

        vid_initialized = true;

        int i2 = COM_CheckParm("-conwidth");
        if (i2 > 0)
            vid.conwidth = atoi(Argv(i2 + 1));
        else
            vid.conwidth = 640;

        vid.conwidth &= 0xfff8; // make it a multiple of eight

        if (vid.conwidth < 320)
            vid.conwidth = 320;

        // pick a conheight that matches with correct aspect
        vid.conheight = vid.conwidth * 3 / 4;

        i2 = COM_CheckParm("-conheight");
        if (i2 > 0)
            vid.conheight = atoi(Argv(i2 + 1));
        if (vid.conheight < 200)
            vid.conheight = 200;

        vid.maxwarpwidth = q_shared.WARP_WIDTH;
        vid.maxwarpheight = q_shared.WARP_HEIGHT;
        vid.colormap = host_colormap;
        int v = BitConverter.ToInt32(host_colormap, 2048);
        vid.fullbright = 256 - LittleLong(v);

        Check_Gamma(palette);
        VID_SetPalette(palette);

        mode1.fullScreen = !windowed;

        vid_default = -1;
        for (i = 0; i < vid_modes.Length; i++)
        {
            mode_t m = vid_modes[i];
            if (m.width != mode1.width || m.height != mode1.height)
                continue;

            vid_default = i;

            if (m.bpp == mode1.bpp && m.refreshRate == mode1.refreshRate)
                break;
        }
        if (vid_default == -1)
            vid_default = 0;

        VID_SetMode(vid_default, palette);

        GL_Init();

        Directory.CreateDirectory(Path.Combine(com_gamedir, "glquake"));
    }
    public static void GL_Init()
    {
        gl_vendor = GL.GetString(StringName.Vendor);
        Con_Printf("GL_VENDOR: {0}\n", gl_vendor);
        gl_renderer = GL.GetString(StringName.Renderer);
        Con_Printf("GL_RENDERER: {0}\n", gl_renderer);

        gl_version = GL.GetString(StringName.Version);
        Con_Printf("GL_VERSION: {0}\n", gl_version);
        gl_extensions = GL.GetString(StringName.Extensions);
        Con_Printf("GL_EXTENSIONS: {0}\n", gl_extensions);

        if (gl_renderer.StartsWith("PowerVR", StringComparison.InvariantCultureIgnoreCase))
            fullsbardraw = true;

        if (gl_renderer.StartsWith("Permedia", StringComparison.InvariantCultureIgnoreCase))
            isPermedia = true;

        CheckTextureExtensions();
        CheckMultiTextureExtensions();

        GL.ClearColor(1, 0, 0, 0);
        GL.CullFace(CullFaceMode.Front);
        GL.Enable(EnableCap.Texture2D);

        GL.Enable(EnableCap.AlphaTest);
        GL.AlphaFunc(AlphaFunction.Greater, 0.666f);

        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        GL.ShadeModel(ShadingModel.Flat);

        SetTextureFilters(TextureMinFilter.Nearest, TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.BlendFunc(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);
    }
    public static void VID_Shutdown()
    {
        vid_initialized = false;
    }
    public static void VID_SetMode(int modenum, byte[] palette)
    {
        if (modenum < 0 || modenum >= vid_modes.Length)
            Sys_Error("Bad video mode\n");

        mode_t mode = vid_modes[modenum];

        // so Con_Printfs don't mess us up by forcing vid and snd updates
        bool temp = scr_disabled_for_loading;
        scr_disabled_for_loading = true;

        CDAudio_Pause();

        // Set either the fullscreen or windowed mode
        DisplayDevice dev = MainForm.DisplayDevice;
        MainForm form = MainForm.Instance;
        
        if(!windowed)
        {
            try
            {
                dev.ChangeResolution(mode.width, mode.height, mode.bpp, mode.refreshRate);
            }
            catch (Exception ex)
            {
                Sys_Error("Couldn't set video mode: " + ex.Message);
            }
            form.WindowState = WindowState.Fullscreen;
            form.WindowBorder = WindowBorder.Hidden;
        }
        else
        {
            form.WindowState = WindowState.Normal;
            form.WindowBorder = WindowBorder.Fixed;
            form.Size = new Size(mode.width, mode.height);
            form.Location = new Point((mode.width - form.Width) / 2, (mode.height - form.Height) / 2);
            if (_windowed_mouse.value != 0 && key_dest == keydest_t.key_game)
            {
                IN_ActivateMouse();
                IN_HideMouse();
            }
            else
            {
                IN_DeactivateMouse();
                IN_ShowMouse();
            }
        }
        
        if (vid.conheight > mode.height)
            vid.conheight = mode.height;
        if (vid.conwidth > mode.width)
            vid.conwidth = mode.width;

        vid.width = vid.conwidth;
        vid.height = vid.conheight;

        vid.numpages = 2;

        CDAudio_Resume();
        scr_disabled_for_loading = temp;

        vid_modenum = modenum;
        Cvar.Cvar_SetValue("vid_mode", vid_modenum);

        // fix the leftover Alt from any Alt-Tab or the like that switched us away
        ClearAllStates();

        Con_SafePrintf("Video mode {0} initialized.\n", VID_GetModeDescription(vid_modenum));

        VID_SetPalette(palette);

        vid.recalc_refdef = true;
    }
    public static void VID_NumModes_f()
    {
        int nummodes = vid_modes.Length;
        if (nummodes == 1)
            Con_Printf("{0} video mode is available\n", nummodes);
        else
            Con_Printf("{0} video modes are available\n", nummodes);
    }
    public static void VID_DescribeCurrentMode_f()
    {
        Con_Printf("{0}\n", GetExtModeDescription(vid_modenum));
    }
    public static void VID_DescribeMode_f()
    {
        int modenum = atoi(Cmd_Argv(1));

        Con_Printf("{0}\n", GetExtModeDescription(modenum));
    }
    public static void VID_DescribeModes_f()
    {
        for (int i = 0; i < vid_modes.Length; i++)
        {
            Con_Printf("{0}:{1}\n", i, GetExtModeDescription(i));
        }
    }
    public static string VID_GetModeDescription(int mode)
    {
        if (mode < 0 || mode >= vid_modes.Length)
            return String.Empty;

        mode_t m = vid_modes[mode];
        return String.Format("{0}x{1}x{2} {3}", m.width, m.height, m.bpp, windowed ? "windowed" : "fullscreen");
    }
    public static string GetExtModeDescription(int mode)
    {
        return VID_GetModeDescription(mode);
    }
    public static void Check_Gamma(byte[] pal)
    {
        int i = COM_CheckParm("-gamma");
        if (i == 0)
        {
            string renderer = GL.GetString(StringName.Renderer);
            string vendor = GL.GetString(StringName.Vendor);
            if (renderer.Contains("Voodoo") || vendor.Contains("3Dfx"))
                vid_gamma = 1;
            else
                vid_gamma = 0.7f; // default to 0.7 on non-3dfx hardware
        }
        else
            vid_gamma = float.Parse(Argv(i + 1));

        for (i = 0; i < pal.Length; i++)
        {
            double f = Math.Pow((pal[i] + 1) / 256.0, vid_gamma);
            double inf = f * 255 + 0.5;
            if (inf < 0)
                inf = 0;
            if (inf > 255)
                inf = 255;
            pal[i] = (byte)inf;
        }
    }
    public static void VID_SetPalette(byte[] palette)
    {
        //
        // 8 8 8 encoding
        //
        int offset = 0;
        byte[] pal = palette;
        uint[] table = d_8to24table;
        for (int i = 0; i < table.Length; i++)
        {
            uint r = pal[offset + 0];
            uint g = pal[offset + 1];
            uint b = pal[offset + 2];

            table[i] = ((uint)0xff << 24) + (r << 0) + (g << 8) + (b << 16);
            offset += 3;
        }

        table[255] &= 0xffffff;	// 255 is transparent

        // JACK: 3D distance calcs - k is last closest, l is the distance.
        // FIXME: Precalculate this and cache to disk.
        Union4b val = Union4b.Empty;
        for (uint i = 0; i < (1 << 15); i++)
        {
            // Maps
            // 000000000000000
            // 000000000011111 = Red  = 0x1F
            // 000001111100000 = Blue = 0x03E0
            // 111110000000000 = Grn  = 0x7C00
            uint r = (((i & 0x1F) << 3) + 4);
            uint g = (((i & 0x03E0) >> 2) + 4);
            uint b = (((i & 0x7C00) >> 7) + 4);
            uint k = 0;
            uint l = 10000 * 10000;
            for (uint v = 0; v < 256; v++)
            {
                val.ui0 = d_8to24table[v];
                uint r1 = r - val.b0;
                uint g1 = g - val.b1;
                uint b1 = b - val.b2;
                uint j = (r1 * r1) + (g1 * g1) + (b1 * b1);
                if (j < l)
                {
                    k = v;
                    l = j;
                }
            }
            d_15to8table[i] = (byte)k;
        }
    }
    public static void ClearAllStates()
    {
        // send an up event for each key, to make sure the server clears them all
        for (int i = 0; i < 256; i++)
        {
            Key_Event(i, false);
        }

        Key_ClearStates();
        IN_ClearStates();
    }
    public static void CheckTextureExtensions()
    {
        const string TEXTURE_EXT_STRING = "GL_EXT_texture_object";

        // check for texture extension
        bool texture_ext = gl_extensions.Contains(TEXTURE_EXT_STRING);
    }
    public static void CheckMultiTextureExtensions()
    {
        if (gl_extensions.Contains("GL_SGIS_multitexture ") && !HasParam("-nomtex"))
        {
            Con_Printf("Multitexture extensions found.\n");
            gl_mtexable = true;
        }
    }
}