using System;
using System.Collections.Generic;
using System.Text;

public static partial class game_engine
{
    public static cvar_t cl_upspeed;
    public static cvar_t cl_forwardspeed;
    public static cvar_t cl_backspeed;
    public static cvar_t cl_sidespeed;
    public static cvar_t cl_movespeedkey;
    public static cvar_t cl_yawspeed;
    public static cvar_t cl_pitchspeed;
    public static cvar_t cl_anglespeedkey;
    public static kbutton_t in_mlook;
    public static kbutton_t in_klook;
    public static kbutton_t in_left;
    public static kbutton_t in_right;
    public static kbutton_t in_forward;
    public static kbutton_t in_back;
    public static kbutton_t in_lookup;
    public static kbutton_t in_lookdown;
    public static kbutton_t in_moveleft;
    public static kbutton_t in_moveright;
    public static kbutton_t in_strafe;
    public static kbutton_t in_speed;
    public static kbutton_t in_use;
    public static kbutton_t in_jump;
    public static kbutton_t in_attack;
    public static kbutton_t in_up;
    public static kbutton_t in_down;
    public static int in_impulse;

    public static void CL_InitInput()
    {
        Cmd_AddCommand("+moveup", IN_UpDown);
        Cmd_AddCommand("-moveup", IN_UpUp);
        Cmd_AddCommand("+movedown", IN_DownDown);
        Cmd_AddCommand("-movedown", IN_DownUp);
        Cmd_AddCommand("+left", IN_LeftDown);
        Cmd_AddCommand("-left", IN_LeftUp);
        Cmd_AddCommand("+right", IN_RightDown);
        Cmd_AddCommand("-right", IN_RightUp);
        Cmd_AddCommand("+forward", IN_ForwardDown);
        Cmd_AddCommand("-forward", IN_ForwardUp);
        Cmd_AddCommand("+back", IN_BackDown);
        Cmd_AddCommand("-back", IN_BackUp);
        Cmd_AddCommand("+lookup", IN_LookupDown);
        Cmd_AddCommand("-lookup", IN_LookupUp);
        Cmd_AddCommand("+lookdown", IN_LookdownDown);
        Cmd_AddCommand("-lookdown", IN_LookdownUp);
        Cmd_AddCommand("+strafe", IN_StrafeDown);
        Cmd_AddCommand("-strafe", IN_StrafeUp);
        Cmd_AddCommand("+moveleft", IN_MoveleftDown);
        Cmd_AddCommand("-moveleft", IN_MoveleftUp);
        Cmd_AddCommand("+moveright", IN_MoverightDown);
        Cmd_AddCommand("-moveright", IN_MoverightUp);
        Cmd_AddCommand("+speed", IN_SpeedDown);
        Cmd_AddCommand("-speed", IN_SpeedUp);
        Cmd_AddCommand("+attack", IN_AttackDown);
        Cmd_AddCommand("-attack", IN_AttackUp);
        Cmd_AddCommand("+use", IN_UseDown);
        Cmd_AddCommand("-use", IN_UseUp);
        Cmd_AddCommand("+jump", IN_JumpDown);
        Cmd_AddCommand("-jump", IN_JumpUp);
        Cmd_AddCommand("impulse", IN_Impulse);
        Cmd_AddCommand("+klook", IN_KLookDown);
        Cmd_AddCommand("-klook", IN_KLookUp);
        Cmd_AddCommand("+mlook", IN_MLookDown);
        Cmd_AddCommand("-mlook", IN_MLookUp);
    }
    public static void CL_BaseMove(ref usercmd_t cmd)
    {
        if (cls.signon != q_shared.SIGNONS)
            return;

        CL_AdjustAngles();

        cmd.Clear();

        if (in_strafe.IsDown)
        {
            cmd.sidemove += cl_sidespeed.value * CL_KeyState(ref in_right);
            cmd.sidemove -= cl_sidespeed.value * CL_KeyState(ref in_left);
        }

        cmd.sidemove += cl_sidespeed.value * CL_KeyState(ref in_moveright);
        cmd.sidemove -= cl_sidespeed.value * CL_KeyState(ref in_moveleft);

        cmd.upmove += cl_upspeed.value * CL_KeyState(ref in_up);
        cmd.upmove -= cl_upspeed.value * CL_KeyState(ref in_down);

        if (!in_klook.IsDown)
        {
            cmd.forwardmove += cl_forwardspeed.value * CL_KeyState(ref in_forward);
            cmd.forwardmove -= cl_backspeed.value * CL_KeyState(ref in_back);
        }

        //
        // adjust for speed key
        //
        if (in_speed.IsDown)
        {
            cmd.forwardmove *= cl_movespeedkey.value;
            cmd.sidemove *= cl_movespeedkey.value;
            cmd.upmove *= cl_movespeedkey.value;
        }
    }
    public static void CL_AdjustAngles()
    {
        float speed = (float)host_framtime;

        if (in_speed.IsDown)
            speed *= cl_anglespeedkey.value;

        if (!in_strafe.IsDown)
        {
            cl.viewangles.Y -= speed * cl_yawspeed.value * CL_KeyState(ref in_right);
            cl.viewangles.Y += speed * cl_yawspeed.value * CL_KeyState(ref in_left);
            cl.viewangles.Y = Mathlib.anglemod(cl.viewangles.Y);
        }

        if (in_klook.IsDown)
        {
            game_engine.V_StopPitchDrift();
            cl.viewangles.X -= speed * cl_pitchspeed.value * CL_KeyState(ref in_forward);
            cl.viewangles.X += speed * cl_pitchspeed.value * CL_KeyState(ref in_back);
        }

        float up = CL_KeyState(ref in_lookup);
        float down = CL_KeyState(ref in_lookdown);

        cl.viewangles.X -= speed * cl_pitchspeed.value * up;
        cl.viewangles.X += speed * cl_pitchspeed.value * down;

        if (up != 0 || down != 0)
            game_engine.V_StopPitchDrift();

        if (cl.viewangles.X > 80)
            cl.viewangles.X = 80;
        if (cl.viewangles.X < -70)
            cl.viewangles.X = -70;

        if (cl.viewangles.Z > 50)
            cl.viewangles.Z = 50;
        if (cl.viewangles.Z < -50)
            cl.viewangles.Z = -50;
    }
    public static float CL_KeyState(ref kbutton_t key)
    {
        bool impulsedown = (key.state & 2) != 0;
        bool impulseup = (key.state & 4) != 0;
        bool down = key.IsDown;// ->state & 1;
        float val = 0;

        if (impulsedown && !impulseup)
            if (down)
                val = 0.5f;	// pressed and held this frame
            else
                val = 0;	//	I_Error ();
        if (impulseup && !impulsedown)
            if (down)
                val = 0;	//	I_Error ();
            else
                val = 0;	// released this frame
        if (!impulsedown && !impulseup)
            if (down)
                val = 1.0f;	// held the entire frame
            else
                val = 0;	// up the entire frame
        if (impulsedown && impulseup)
            if (down)
                val = 0.75f;	// released and re-pressed this frame
            else
                val = 0.25f;	// pressed and released this frame

        key.state &= 1;		// clear impulses

        return val;
    }
    public static void CL_SendMove(ref usercmd_t cmd)
    {
        cl.cmd = cmd; // cl.cmd = *cmd - struct copying!!!

        MsgWriter msg = new MsgWriter(128);

        //
        // send the movement message
        //
        msg.MSG_WriteByte(q_shared.clc_move);

        msg.MSG_WriteFloat((float)cl.mtime[0]);	// so server can get ping times

        msg.MSG_WriteAngle(cl.viewangles.X);
        msg.MSG_WriteAngle(cl.viewangles.Y);
        msg.MSG_WriteAngle(cl.viewangles.Z);

        msg.MSG_WriteShort((short)cmd.forwardmove);
        msg.MSG_WriteShort((short)cmd.sidemove);
        msg.MSG_WriteShort((short)cmd.upmove);

        //
        // send button bits
        //
        int bits = 0;

        if ((in_attack.state & 3) != 0)
            bits |= 1;
        in_attack.state &= ~2;

        if ((in_jump.state & 3) != 0)
            bits |= 2;
        in_jump.state &= ~2;

        msg.MSG_WriteByte(bits);

        msg.MSG_WriteByte(in_impulse);
        in_impulse = 0;

        //
        // deliver the message
        //
        if (cls.demoplayback)
            return;

        //
        // allways dump the first two message, because it may contain leftover inputs
        // from the last level
        //
        if (++cl.movemessages <= 2)
            return;

        if (NET_SendUnreliableMessage(cls.netcon, msg) == -1)
        {
            Con_Printf("CL_SendMove: lost server connection\n");
            CL_Disconnect();
        }
    }
    public static void KeyDown(ref kbutton_t b)
    {
        int k;
        string c = Cmd_Argv(1);
        if (!String.IsNullOrEmpty(c))
            k = int.Parse(c);
        else
            k = -1;	// typed manually at the console for continuous down

        if (k == b.down0 || k == b.down1)
            return;		// repeating key

        if (b.down0 == 0)
            b.down0 = k;
        else if (b.down1 == 0)
            b.down1 = k;
        else
        {
            Con_Printf("Three keys down for a button!\n");
            return;
        }

        if ((b.state & 1) != 0)
            return;	// still down
        b.state |= 1 + 2; // down + impulse down
    }
    public static void KeyUp(ref kbutton_t b)
    {
        int k;
        string c = Cmd_Argv(1);
        if (!String.IsNullOrEmpty(c))
            k = int.Parse(c);
        else
        {
            // typed manually at the console, assume for unsticking, so clear all
            b.down0 = b.down1 = 0;
            b.state = 4;	// impulse up
            return;
        }

        if (b.down0 == k)
            b.down0 = 0;
        else if (b.down1 == k)
            b.down1 = 0;
        else
            return;	// key up without coresponding down (menu pass through)

        if (b.down0 != 0 || b.down1 != 0)
            return;	// some other key is still holding it down

        if ((b.state & 1) == 0)
            return;		// still up (this should not happen)
        b.state &= ~1;		// now up
        b.state |= 4; 		// impulse up
    }

    public static void IN_KLookDown()
    {
        KeyDown(ref in_klook);
    }
    public static void IN_KLookUp()
    {
        KeyUp(ref in_klook);
    }
    public static void IN_MLookDown()
    {
        KeyDown(ref in_mlook);
    }
    public static void IN_MLookUp()
    {
        KeyUp(ref in_mlook);

        if ((in_mlook.state & 1) == 0 && (lookspring.value != 0))
            game_engine.V_StartPitchDrift();
    }
    public static void IN_UpDown()
    {
        KeyDown(ref in_up);
    }
    public static void IN_UpUp()
    {
        KeyUp(ref in_up);
    }
    public static void IN_DownDown()
    {
        KeyDown(ref in_down);
    }
    public static void IN_DownUp()
    {
        KeyUp(ref in_down);
    }
    public static void IN_LeftDown()
    {
        KeyDown(ref in_left);
    }
    public static void IN_LeftUp()
    {
        KeyUp(ref in_left);
    }
    public static void IN_RightDown()
    {
        KeyDown(ref in_right);
    }
    public static void IN_RightUp()
    {
        KeyUp(ref in_right);
    }
    public static void IN_ForwardDown()
    {
        KeyDown(ref in_forward);
    }
    public static void IN_ForwardUp()
    {
        KeyUp(ref in_forward);
    }
    public static void IN_BackDown()
    {
        KeyDown(ref in_back);
    }
    public static void IN_BackUp()
    {
        KeyUp(ref in_back);
    }
    public static void IN_LookupDown()
    {
        KeyDown(ref in_lookup);
    }
    public static void IN_LookupUp()
    {
        KeyUp(ref in_lookup);
    }
    public static void IN_LookdownDown()
    {
        KeyDown(ref in_lookdown);
    }
    public static void IN_LookdownUp()
    {
        KeyUp(ref in_lookdown);
    }
    public static void IN_MoveleftDown()
    {
        KeyDown(ref in_moveleft);
    }
    public static void IN_MoveleftUp()
    {
        KeyUp(ref in_moveleft);
    }
    public static void IN_MoverightDown()
    {
        KeyDown(ref in_moveright);
    }
    public static void IN_MoverightUp()
    {
        KeyUp(ref in_moveright);
    }
    public static void IN_SpeedDown()
    {
        KeyDown(ref in_speed);
    }
    public static void IN_SpeedUp()
    {
        KeyUp(ref in_speed);
    }
    public static void IN_StrafeDown()
    {
        KeyDown(ref in_strafe);
    }
    public static void IN_StrafeUp()
    {
        KeyUp(ref in_strafe);
    }
    public static void IN_AttackDown()
    {
        KeyDown(ref in_attack);
    }
    public static void IN_AttackUp()
    {
        KeyUp(ref in_attack);
    }
    public static void IN_UseDown()
    {
        KeyDown(ref in_use);
    }
    public static void IN_UseUp()
    {
        KeyUp(ref in_use);
    }
    public static void IN_JumpDown()
    {
        KeyDown(ref in_jump);
    }
    public static void IN_JumpUp()
    {
        KeyUp(ref in_jump);
    }
    public static void IN_Impulse()
    {
        in_impulse = game_engine.atoi(Cmd_Argv(1));
    }
}