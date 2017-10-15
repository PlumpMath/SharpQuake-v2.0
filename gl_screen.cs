using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    public static viddef_t vid = new viddef_t();
    public static vrect_t scr_vrect;
    public static bool scr_disabled_for_loading;
    public static bool scr_drawloading;
    public static double scr_disabled_time;
    public static bool block_drawing = false;
    public static bool scr_drawdialog;
    public static bool scr_skipupdate;
    public static bool fullsbardraw;
    public static bool isPermedia;
    public static bool scr_initialized;
    public static bool src_inupdate;
    public static glpic_t scr_ram;
    public static glpic_t scr_net;
    public static glpic_t scr_turtle;
    public static int _TurtleCount;
    public static bool scr_copytop;
    public static bool scr_copyeverything;

    public static float scr_con_current;
    public static float scr_conlines;
    public static int clearconsole;
    public static int clearnotify;

    public static float oldscreensize;
    public static float oldfov;
    public static int glX;
    public static int glY;
    public static int glWidth;
    public static int glHeight;
    public static int scr_center_lines;
    public static int scr_erase_lines;
    public static float scr_centertime_start;
    public static float scr_centertime_off;
    public static string scr_centerstring;

    public static cvar_t scr_viewsize;
    public static cvar_t scr_fov;
    public static cvar_t scr_conspeed;
    public static cvar_t scr_centertime;
    public static cvar_t scr_showram;
    public static cvar_t scr_showturtle;
    public static cvar_t scr_showpause;
    public static cvar_t scr_printspeed;
    public static cvar_t gl_triplebuffer;

    public static string scr_notifystring;
    public static bool windowed_mouse;
    public static int scr_fullupdate;


    public static void SCR_Init()
    {
        scr_viewsize = new cvar_t("viewsize", "100", true);
        scr_fov = new cvar_t("fov", "90");	// 10 - 170
        scr_conspeed = new cvar_t("scr_conspeed", "3000");
        scr_centertime = new cvar_t("scr_centertime", "2");
        scr_showram = new cvar_t("showram", "1");
        scr_showturtle = new cvar_t("showturtle", "0");
        scr_showpause = new cvar_t("showpause", "1");
        scr_printspeed = new cvar_t("scr_printspeed", "8");
        gl_triplebuffer = new cvar_t("gl_triplebuffer", "1", true);

        //
        // register our commands
        //
        Cmd_AddCommand("screenshot", SCR_ScreenShot_f);
        Cmd_AddCommand("sizeup", SCR_SizeUp_f);
        Cmd_AddCommand("sizedown", SCR_SizeDown_f);

        scr_ram = Draw_PicFromWad("ram");
        scr_net = Draw_PicFromWad("net");
        scr_turtle = Draw_PicFromWad("turtle");

        if (HasParam("-fullsbar"))
            fullsbardraw = true;

        scr_initialized = true;
    }
    public static void SCR_UpdateScreen()
    {
        if (block_drawing || !scr_initialized || src_inupdate)
            return;

        src_inupdate = true;
        try
        {
            if (MainForm.Instance != null)
            {
                if ((MainForm.Instance.VSync == VSyncMode.On) != (vid_wait.value != 0))
                    MainForm.Instance.VSync = ((vid_wait.value != 0) ? VSyncMode.On : VSyncMode.Off);
            }

            vid.numpages = 2 + (int)gl_triplebuffer.value;

            scr_copytop = false;
            scr_copyeverything = false;

            if (scr_disabled_for_loading)
            {
                if ((realtime - scr_disabled_time) > 60)
                {
                    scr_disabled_for_loading = false;
                    Con_Printf("Load failed.\n");
                }
                else
                    return;
            }

            if (!con_initialized)
                return;	// not initialized yet

            GL_BeginRendering();

            //
            // determine size of refresh window
            //
            if (oldfov != scr_fov.value)
            {
                oldfov = scr_fov.value;
                vid.recalc_refdef = true;
            }

            if (oldscreensize != scr_viewsize.value)
            {
                oldscreensize = scr_viewsize.value;
                vid.recalc_refdef = true;
            }

            if (vid.recalc_refdef)
                SCR_CalcRefdef();

            //
            // do 3D refresh drawing, and then update the screen
            //
            SCR_SetUpToDrawConsole();

            game_engine.V_RenderView();

            GL_Set2D();

            //
            // draw any areas not covered by the refresh
            //
            SCR_TileClear();

            if (scr_drawdialog)
            {
                Sbar_Draw();
                Draw_FadeScreen();
                SCR_DrawNotifyString();
                scr_copyeverything = true;
            }
            else if (scr_drawloading)
            {
                SCR_DrawLoading();
                Sbar_Draw();
            }
            else if (cl.intermission == 1 && key_dest == keydest_t.key_game)
            {
                Sbar_IntermissionOverlay();
            }
            else if (cl.intermission == 2 && key_dest == keydest_t.key_game)
            {
                Sbar_FinaleOverlay();
                SCR_CheckDrawCenterString();
            }
            else
            {
                if (game_engine.crosshair.value > 0)
                    Draw_Character(scr_vrect.x + scr_vrect.width / 2, scr_vrect.y + scr_vrect.height / 2, '+');

                SCR_DrawRam();
                SCR_DrawNet();
                SCR_DrawTurtle();
                DrawPause();
                SCR_CheckDrawCenterString();
                Sbar_Draw();
                SCR_DrawConsole();
                Menu.M_Draw();
            }

            game_engine.V_UpdatePalette();
            GL_EndRendering();
        }
        finally
        {
            src_inupdate = false;
        }
    }
    public static void GL_BeginRendering()
    {
        glX = 0;
        glY = 0;
        glWidth = 0;
        glHeight = 0;

        INativeWindow window = MainForm.Instance;
        if (window != null)
        {
            Size size = window.ClientSize;
            glWidth = size.Width;
            glHeight = size.Height;
        }
    }
    public static void GL_EndRendering()
    {
        MainForm form = MainForm.Instance;
        if (form == null)
            return;

        if (!scr_skipupdate || block_drawing)
            form.SwapBuffers();

        // handle the mouse state
        if (_windowed_mouse.value == 0)
        {
            if (windowed_mouse)
            {
                IN_DeactivateMouse();
                IN_ShowMouse();
                windowed_mouse = false;
            }
        }
        else
        {
            windowed_mouse = true;
            if (key_dest == keydest_t.key_game && !mouseactive &&
                cls.state != cactive_t.ca_disconnected)// && ActiveApp)
            {
                IN_ActivateMouse();
                IN_HideMouse();
            }
            else if (mouseactive && key_dest != keydest_t.key_game)
            {
                IN_DeactivateMouse();
                IN_ShowMouse();
            }
        }

        if (fullsbardraw)
            Sbar_Changed();
    }
    public static void SCR_CalcRefdef()
    {
        scr_fullupdate = 0; // force a background redraw
        vid.recalc_refdef = false;

        // force the status bar to redraw
        Sbar_Changed();

        // bound viewsize
        if (scr_viewsize.value < 30)
            Cvar.Cvar_Set("viewsize", "30");
        if (scr_viewsize.value > 120)
            Cvar.Cvar_Set("viewsize", "120");

        // bound field of view
        if (scr_fov.value < 10)
            Cvar.Cvar_Set("fov", "10");
        if (scr_fov.value > 170)
            Cvar.Cvar_Set("fov", "170");

        // intermission is always full screen	
        float size;
        if (cl.intermission > 0)
            size = 120;
        else
            size = scr_viewsize.value;

        if (size >= 120)
            sb_lines = 0; // no status bar at all
        else if (size >= 110)
            sb_lines = 24; // no inventory
        else
            sb_lines = 24 + 16 + 8;

        bool full = false;
        if (scr_viewsize.value >= 100.0)
        {
            full = true;
            size = 100.0f;
        }
        else
            size = scr_viewsize.value;

        if (cl.intermission > 0)
        {
            full = true;
            size = 100;
            sb_lines = 0;
        }
        size /= 100.0f;

        int h = vid.height - sb_lines;

        refdef_t rdef = r_refdef;
        rdef.vrect.width = (int)(vid.width * size);
        if (rdef.vrect.width < 96)
        {
            size = 96.0f / rdef.vrect.width;
            rdef.vrect.width = 96;  // min for icons
        }

        rdef.vrect.height = (int)(vid.height * size);
        if (rdef.vrect.height > vid.height - sb_lines)
            rdef.vrect.height = vid.height - sb_lines;
        if (rdef.vrect.height > vid.height)
            rdef.vrect.height = vid.height;
        rdef.vrect.x = (vid.width - rdef.vrect.width) / 2;
        if (full)
            rdef.vrect.y = 0;
        else
            rdef.vrect.y = (h - rdef.vrect.height) / 2;

        rdef.fov_x = scr_fov.value;
        rdef.fov_y = CalcFov(rdef.fov_x, rdef.vrect.width, rdef.vrect.height);

        scr_vrect = rdef.vrect;
    }
    public static float CalcFov(float fov_x, float width, float height)
    {
        if (fov_x < 1 || fov_x > 179)
            Sys_Error("Bad fov: {0}", fov_x);

        double x = width / Math.Tan(fov_x / 360.0 * Math.PI);
        double a = Math.Atan(height / x);
        a = a * 360.0 / Math.PI;
        return (float)a;
    }
    public static void SCR_SetUpToDrawConsole()
    {
        Con_CheckResize();

        if (scr_drawloading)
            return;     // never a console with loading plaque

        // decide on the height of the console
        con_forcedup = (cl.worldmodel == null) || (cls.signon != q_shared.SIGNONS);

        if (con_forcedup)
        {
            scr_conlines = vid.height; // full screen
            scr_con_current = scr_conlines;
        }
        else if (key_dest == keydest_t.key_console)
            scr_conlines = vid.height / 2; // half screen
        else
            scr_conlines = 0; // none visible

        if (scr_conlines < scr_con_current)
        {
            scr_con_current -= (int)(scr_conspeed.value * host_framtime);
            if (scr_conlines > scr_con_current)
                scr_con_current = scr_conlines;
        }
        else if (scr_conlines > scr_con_current)
        {
            scr_con_current += (int)(scr_conspeed.value * host_framtime);
            if (scr_conlines < scr_con_current)
                scr_con_current = scr_conlines;
        }

        if (clearconsole++ < vid.numpages)
        {
            Sbar_Changed();
        }
        else if (clearnotify++ < vid.numpages)
        {
            //????????????
        }
        else
            con_notifylines = 0;
    }
    public static void SCR_TileClear()
    {
        refdef_t rdef = r_refdef;
        if (rdef.vrect.x > 0)
        {
            // left
            Draw_TileClear(0, 0, rdef.vrect.x, vid.height - sb_lines);
            // right
            Draw_TileClear(rdef.vrect.x + rdef.vrect.width, 0,
                vid.width - rdef.vrect.x + rdef.vrect.width,
                vid.height - sb_lines);
        }
        if (rdef.vrect.y > 0)
        {
            // top
            Draw_TileClear(rdef.vrect.x, 0, rdef.vrect.x + rdef.vrect.width, rdef.vrect.y);
            // bottom
            Draw_TileClear(rdef.vrect.x, rdef.vrect.y + rdef.vrect.height,
                rdef.vrect.width, vid.height - sb_lines - (rdef.vrect.height + rdef.vrect.y));
        }
    }
    public static void SCR_DrawNotifyString()
    {
        int offset = 0;
        int y = (int)(vid.height * 0.35);

        do
        {
            int end = scr_notifystring.IndexOf('\n', offset);
            if (end == -1)
                end = scr_notifystring.Length;
            if (end - offset > 40)
                end = offset + 40;

            int length = end - offset;
            if (length > 0)
            {
                int x = (vid.width - length * 8) / 2;
                for (int j = 0; j < length; j++, x += 8)
                    Draw_Character(x, y, scr_notifystring[offset + j]);

                y += 8;
            }
            offset = end + 1;
        } while (offset < scr_notifystring.Length);
    }
    public static void SCR_DrawLoading()
    {
        if (!scr_drawloading)
            return;

        glpic_t pic = Draw_CachePic("gfx/loading.lmp");
        Draw_Pic((vid.width - pic.width) / 2, (vid.height - 48 - pic.height) / 2, pic);
    }
    public static void SCR_CheckDrawCenterString()
    {
        scr_copytop = true;
        if (scr_center_lines > scr_erase_lines)
            scr_erase_lines = scr_center_lines;

        scr_centertime_off -= (float)host_framtime;

        if (scr_centertime_off <= 0 && cl.intermission == 0)
            return;
        if (key_dest != keydest_t.key_game)
            return;

        SCR_DrawCenterString();
    }
    public static void SCR_DrawRam()
    {
        if (scr_showram.value == 0)
            return;

        if (!r_cache_thrash)
            return;

        Draw_Pic(scr_vrect.x + 32, scr_vrect.y, scr_ram);
    }
    public static void SCR_DrawTurtle()
    {
        if (scr_showturtle.value == 0)
            return;

        if (host_framtime < 0.1)
        {
            _TurtleCount = 0;
            return;
        }

        _TurtleCount++;
        if (_TurtleCount < 3)
            return;

        Draw_Pic(scr_vrect.x, scr_vrect.y, scr_turtle);
    }
    public static void SCR_DrawNet()
    {
        if (realtime - cl.last_received_message < 0.3)
            return;
        if (cls.demoplayback)
            return;

        Draw_Pic(scr_vrect.x + 64, scr_vrect.y, scr_net);
    }
    public static void DrawPause()
    {
        if (scr_showpause.value == 0)	// turn off for screenshots
            return;

        if (!cl.paused)
            return;

        glpic_t pic = Draw_CachePic("gfx/pause.lmp");
        Draw_Pic((vid.width - pic.width) / 2, (vid.height - 48 - pic.height) / 2, pic);
    }
    public static void SCR_DrawConsole()
    {
        if (scr_con_current > 0)
        {
            scr_copyeverything = true;
            Con_DrawConsole((int)scr_con_current, true);
            clearconsole = 0;
        }
        else if (key_dest == keydest_t.key_game ||
            key_dest == keydest_t.key_message)
        {
            Con_DrawNotify();	// only draw notify in game
        }
    }
    public static void SCR_DrawCenterString()
    {
        int remaining;

        // the finale prints the characters one at a time
        if (cl.intermission > 0)
            remaining = (int)(scr_printspeed.value * (cl.time - scr_centertime_start));
        else
            remaining = 9999;

        int y = 48;
        if (scr_center_lines <= 4)
            y = (int)(vid.height * 0.35);

        string[] lines = scr_centerstring.Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].TrimEnd('\r');
            int x = (vid.width - line.Length * 8) / 2;

            for (int j = 0; j < line.Length; j++, x += 8)
            {
                Draw_Character(x, y, line[j]);
                if (remaining-- <= 0)
                    return;
            }
            y += 8;
        }
    }
    public static void SCR_CenterPrint(string str)
    {
        scr_centerstring = str;
        scr_centertime_off = scr_centertime.value;
        scr_centertime_start = (float)cl.time;

        // count the number of lines for centering
        scr_center_lines = 1;
        foreach (char c in scr_centerstring)
        {
            if (c == '\n')
                scr_center_lines++;
        }
    }
    public static void SCR_EndLoadingPlaque()
    {
        scr_disabled_for_loading = false;
        scr_fullupdate = 0;
        Con_ClearNotify();
    }
    public static void SCR_BeginLoadingPlaque()
    {
        S_StopAllSounds(true);

        if (cls.state != cactive_t.ca_connected)
            return;
        if (cls.signon != q_shared.SIGNONS)
            return;

        // redraw with no console and the loading plaque
        Con_ClearNotify();
        scr_centertime_off = 0;
        scr_con_current = 0;

        scr_drawloading = true;
        scr_fullupdate = 0;
        Sbar_Changed();
        SCR_UpdateScreen();
        scr_drawloading = false;

        scr_disabled_for_loading = true;
        scr_disabled_time = realtime;
        scr_fullupdate = 0;
    }
    public static bool SCR_ModalMessage(string text)
    {
        if (cls.state == cactive_t.ca_dedicated)
            return true;

        scr_notifystring = text;

        // draw a fresh screen
        scr_fullupdate = 0;
        scr_drawdialog = true;
        SCR_UpdateScreen();
        scr_drawdialog = false;

        S_ClearBuffer();		// so dma doesn't loop current sound

        do
        {
            key_count = -1;		// wait for a key down and up
            Sys_SendKeyEvents();
        } while (key_lastpress != 'y' && key_lastpress != 'n' && key_lastpress != q_shared.K_ESCAPE);

        scr_fullupdate = 0;
        SCR_UpdateScreen();

        return (key_lastpress == 'y');
    }

    public static void SCR_SizeUp_f()
    {
        Cvar.Cvar_SetValue("viewsize", scr_viewsize.value + 10);
        vid.recalc_refdef = true;
    }
    public static void SCR_SizeDown_f()
    {
        Cvar.Cvar_SetValue("viewsize", scr_viewsize.value - 10);
        vid.recalc_refdef = true;
    }
    public static void SCR_ScreenShot_f()
    {
        // 
        // find a file name to save it to 
        // 
        string path = null;
        int i;
        for (i = 0; i <= 999; i++)
        {
            path = Path.Combine(com_gamedir, String.Format("quake{0:D3}.tga", i));
            if (Sys_FileTime(path) == DateTime.MinValue)
                break;	// file doesn't exist
        }
        if (i == 100)
        {
            Con_Printf("SCR_ScreenShot_f: Couldn't create a file\n");
            return;
        }

        FileStream fs = Sys_FileOpenWrite(path, true);
        if (fs == null)
        {
            Con_Printf("SCR_ScreenShot_f: Couldn't create a file\n");
            return;
        }
        using (BinaryWriter writer = new BinaryWriter(fs))
        {
            // Write tga header (18 bytes)
            writer.Write((ushort)0);
            writer.Write((byte)2); //buffer[2] = 2; uncompressed type
            writer.Write((byte)0);
            writer.Write((uint)0);
            writer.Write((uint)0);
            writer.Write((byte)(glWidth & 0xff));
            writer.Write((byte)(glWidth >> 8));
            writer.Write((byte)(glHeight & 0xff));
            writer.Write((byte)(glHeight >> 8));
            writer.Write((byte)24); // pixel size
            writer.Write((ushort)0);

            byte[] buffer = new byte[glWidth * glHeight * 3];
            GL.ReadPixels(glX, glY, glWidth, glHeight, PixelFormat.Rgb, PixelType.UnsignedByte, buffer);

            // swap 012 to 102
            int c = glWidth * glHeight * 3;
            for (i = 0; i < c; i += 3)
            {
                byte temp = buffer[i + 0];
                buffer[i + 0] = buffer[i + 1];
                buffer[i + 1] = temp;
            }
            writer.Write(buffer, 0, buffer.Length);
        }
        Con_Printf("Wrote {0}\n", Path.GetFileName(path));
    }
}