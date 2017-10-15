using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using OpenTK;

public static partial class game_engine
{
    public static cvar_t m_filter;
    public static float old_mouse_x;
    public static float old_mouse_y;
    public static float mouse_x;
    public static float mouse_y;
    public static float mx_accum;
    public static float my_accum;


    public static bool mouseactive;
    public static int mouse_buttons;
    public static int mouse_oldbuttonstate;
    public static bool mouseactivatetoggle;
    public static bool mouseshowtoggle = true;
    
    public static Point WindowCenter
    {
        get
        {
            Rectangle bounds = MainForm.Instance.Bounds;
            Point p = bounds.Location;
            p.Offset(bounds.Width / 2, bounds.Height / 2);
            return p;
        }
    }
    public static void IN_Init()
    {
        m_filter = new cvar_t("m_filter", "0");

        mouseactive = (MainForm.Instance.Mouse != null);
        if (mouseactive)
        {
            mouse_buttons = MainForm.Instance.Mouse.NumberOfButtons;
        }
    }
    public static void IN_Shutdown()
    {
        IN_DeactivateMouse();
        IN_ShowMouse();
    }
    public static void IN_Commands()
    {
        // joystick not supported
    }
    public static void IN_ActivateMouse()
    {
        mouseactivatetoggle = true;

        if (MainForm.Instance.Mouse != null)
        {
            //if (mouseparmsvalid)
            //    restore_spi = SystemParametersInfo (SPI_SETMOUSE, 0, newmouseparms, 0);

            Cursor.Position = WindowCenter;
                
            //SetCapture(mainwindow);
                
            Cursor.Clip = MainForm.Instance.Bounds;

            mouseactive = true;
        }
    }
    public static void IN_DeactivateMouse()
    {
        mouseactivatetoggle = false;

        Cursor.Clip = Screen.PrimaryScreen.Bounds;
        //ReleaseCapture ();

        mouseactive = false;
    }
    public static void IN_HideMouse()
    {
	    if (mouseshowtoggle)
	    {
		    Cursor.Hide();
            mouseshowtoggle = false;
	    }
    }
    public static void IN_ShowMouse()
    {
        if (!mouseshowtoggle)
        {
            if (!MainForm.IsFullscreen)
            {
                Cursor.Show();
            }
            mouseshowtoggle = true;
        }
    }
    public static void IN_Move(usercmd_t cmd)
    {
        if (!MainForm.Instance.Focused)
            return;

        if (MainForm.Instance.WindowState == WindowState.Minimized)
            return;

        IN_MouseMove(cmd);
    }
    public static void IN_ClearStates()
    {
        if (mouseactive)
        {
            mx_accum = 0;
            my_accum = 0;
            mouse_oldbuttonstate = 0;
        }
    }
    public static void IN_MouseEvent(int mstate)
    {
        if (mouseactive)
        {
            // perform button actions
            for (int i = 0; i < mouse_buttons; i++)
            {
                if ((mstate & (1 << i)) != 0 && (mouse_oldbuttonstate & (1 << i)) == 0)
                {
                    Key_Event(q_shared.K_MOUSE1 + i, true);
                }

                if ((mstate & (1 << i)) == 0 && (mouse_oldbuttonstate & (1 << i)) != 0)
                {
                    Key_Event(q_shared.K_MOUSE1 + i, false);
                }
            }

            mouse_oldbuttonstate = mstate;
        }
    }
    static void IN_MouseMove(usercmd_t cmd)
    {
        if (!mouseactive)
            return;

        Rectangle bounds = MainForm.Instance.Bounds;
        Point current_pos = Cursor.Position;
        Point window_center = WindowCenter;

        int mx = (int)(current_pos.X - window_center.X + mx_accum);
        int my = (int)(current_pos.Y - window_center.Y + my_accum);
        mx_accum = 0;
        my_accum = 0;


        if (m_filter.value != 0)
        {
            mouse_x = (mx + old_mouse_x) * 0.5f;
            mouse_y = (my + old_mouse_y) * 0.5f;
        }
        else
        {
            mouse_x = mx;
            mouse_y = my;
        }

        old_mouse_x = mx;
        old_mouse_y = my;

        mouse_x *= sensitivity.value;
        mouse_y *= sensitivity.value;

        // add mouse X/Y movement to cmd
        // HACK: remove the look stafe and mouse look button
        if (in_strafe.IsDown || ((lookstrafe.value != 0) && in_mlook.IsDown))
            cmd.sidemove += m_side.value * mouse_x;
        else
            cl.viewangles.Y -= m_yaw.value * mouse_x;

        if (in_mlook.IsDown)
            V_StopPitchDrift();

        if (in_mlook.IsDown && !in_strafe.IsDown)
        {
            cl.viewangles.X += m_pitch.value * mouse_y;
            if (cl.viewangles.X > 80)
                cl.viewangles.X = 80;
            if (cl.viewangles.X < -70)
                cl.viewangles.X = -70;
        }
        else
        {
            if (in_strafe.IsDown && noclip_anglehack)
                cmd.upmove -= m_forward.value * mouse_y;
            else
                cmd.forwardmove -= m_forward.value * mouse_y;
        }

        // if the mouse has moved, force it to the center, so there's room to move
        if (mx != 0 || my != 0)
        {
            Cursor.Position = window_center;
        }
    }
}