using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using OpenTK;
using OpenTK.Graphics;

public static partial class game_engine
{
    public static cvar_t lcd_x;
    public static cvar_t lcd_yaw;

    public static cvar_t scr_ofsx;
    public static cvar_t scr_ofsy;
    public static cvar_t scr_ofsz;

    public static cvar_t cl_rollspeed;
    public static cvar_t cl_rollangle;

    public static cvar_t cl_bob;
    public static cvar_t cl_bobcycle;
    public static cvar_t cl_bobup;

    public static cvar_t v_kicktime;
    public static cvar_t v_kickroll;
    public static cvar_t v_kickpitch;

    public static cvar_t v_iyaw_cycle;
    public static cvar_t v_iroll_cycle;
    public static cvar_t v_ipitch_cycle;
    public static cvar_t v_iyaw_level;
    public static cvar_t v_iroll_level;
    public static cvar_t v_ipitch_level;

    public static cvar_t v_idlescale;

    public static cvar_t crosshair;
    public static cvar_t cl_crossx;
    public static cvar_t cl_crossy;

    public static cvar_t gl_cshiftpercent;

    public static cvar_t gamma;
    public static cvar_t v_centermove;
    public static cvar_t v_centerspeed;

    public static byte[] _GammaTable; // [256];	// palette is sent through this
    public static cshift_t _CShift_empty;
    public static cshift_t _CShift_water;
    public static cshift_t _CShift_slime;
    public static cshift_t _CShift_lava;

    public static Color4 v_blend;
    public static byte[,] ramps = new byte[3, 256];

    // Used by view and sv_user
    public static Vector3 forward;
    public static Vector3 right;
    public static Vector3 up;

    public static float v_dmg_time;
    public static float v_dmg_roll;
    public static float v_dmg_pitch;

    public static float _OldZ = 0; // static oldz  from CalcRefdef()
    public static float _OldYaw = 0; // static oldyaw from CalcGunAngle
    public static float _OldPitch = 0; // static oldpitch from CalcGunAngle
    public static float _OldGammaValue; // static float oldgammavalue from CheckGamma

    
    public static void V_Init()
    {
        _GammaTable = new byte[256];

        _CShift_empty = new cshift_t(new[] { 130, 80, 50 }, 0);
        _CShift_water = new cshift_t(new[] { 130, 80, 50 }, 128);
        _CShift_slime = new cshift_t(new[] { 0, 25, 5 }, 150);
        _CShift_lava = new cshift_t(new[] { 255, 80, 0 }, 150);

        Cmd_AddCommand("v_cshift", V_cshift_f);
        Cmd_AddCommand("bf", V_BonusFlash_f);
        Cmd_AddCommand("centerview", V_StartPitchDrift);

        lcd_x = new cvar_t("lcd_x", "0");
        lcd_yaw = new cvar_t("lcd_yaw", "0");

        scr_ofsx = new cvar_t("scr_ofsx", "0", false);
        scr_ofsy = new cvar_t("scr_ofsy", "0", false);
        scr_ofsz = new cvar_t("scr_ofsz", "0", false);

        cl_rollspeed = new cvar_t("cl_rollspeed", "200");
        cl_rollangle = new cvar_t("cl_rollangle", "2.0");

        cl_bob = new cvar_t("cl_bob", "0.02", false);
        cl_bobcycle = new cvar_t("cl_bobcycle", "0.6", false);
        cl_bobup = new cvar_t("cl_bobup", "0.5", false);

        v_kicktime = new cvar_t("v_kicktime", "0.5", false);
        v_kickroll = new cvar_t("v_kickroll", "0.6", false);
        v_kickpitch = new cvar_t("v_kickpitch", "0.6", false);

        v_iyaw_cycle = new cvar_t("v_iyaw_cycle", "2", false);
        v_iroll_cycle = new cvar_t("v_iroll_cycle", "0.5", false);
        v_ipitch_cycle = new cvar_t("v_ipitch_cycle", "1", false);
        v_iyaw_level = new cvar_t("v_iyaw_level", "0.3", false);
        v_iroll_level = new cvar_t("v_iroll_level", "0.1", false);
        v_ipitch_level = new cvar_t("v_ipitch_level", "0.3", false);

        v_idlescale = new cvar_t("v_idlescale", "0", false);

        crosshair = new cvar_t("crosshair", "0", true);
        cl_crossx = new cvar_t("cl_crossx", "0", false);
        cl_crossy = new cvar_t("cl_crossy", "0", false);

        gl_cshiftpercent = new cvar_t("gl_cshiftpercent", "100", false);

        v_centermove = new cvar_t("v_centermove", "0.15", false);
        v_centerspeed = new cvar_t("v_centerspeed", "500");

        BuildGammaTable(1.0f);	// no gamma yet
        gamma = new cvar_t("gamma", "1", true);
    }
    public static void V_RenderView()
    {
        if (con_forcedup)
            return;

        // don't allow cheats in multiplayer
        if (cl.maxclients > 1)
        {
            Cvar.Cvar_Set("scr_ofsx", "0");
            Cvar.Cvar_Set("scr_ofsy", "0");
            Cvar.Cvar_Set("scr_ofsz", "0");
        }

        if (cl.intermission > 0)
        {
            // intermission / finale rendering
            V_CalcIntermissionRefdef();
        }
        else if (!cl.paused)
            V_CalcRefdef();

        R_PushDlights();

        if (lcd_x.value != 0)
        {
            //
            // render two interleaved views
            //
            refdef_t rdef = r_refdef;

            vid.rowbytes <<= 1;
            vid.aspect *= 0.5f;

            rdef.viewangles.Y -= lcd_yaw.value;
            rdef.vieworg -= right * lcd_x.value;

            R_RenderView();

            // ???????? vid.buffer += vid.rowbytes>>1;

            R_PushDlights();

            rdef.viewangles.Y += lcd_yaw.value * 2;
            rdef.vieworg += right * lcd_x.value * 2;

            R_RenderView();

            // ????????? vid.buffer -= vid.rowbytes>>1;

            rdef.vrect.height <<= 1;

            vid.rowbytes >>= 1;
            vid.aspect *= 2;
        }
        else
        {
            R_RenderView();
        }
    }
    public static float V_CalcRoll(ref Vector3 angles, ref Vector3 velocity)
    {
        Mathlib.AngleVectors(ref angles, out forward, out right, out up);
        float side = Vector3.Dot(velocity, right);
        float sign = side < 0 ? -1 : 1;
        side = Math.Abs(side);

        float value = cl_rollangle.value;
        if (side < cl_rollspeed.value)
            side = side * value / cl_rollspeed.value;
        else
            side = value;

        return side * sign;
    }
    public static void V_UpdatePalette()
    {
        V_CalcPowerupCshift();

        bool isnew = false;
        
        for (int i = 0; i < q_shared.NUM_CSHIFTS; i++)
        {
            if (cl.cshifts[i].percent != cl.prev_cshifts[i].percent)
            {
                isnew = true;
                cl.prev_cshifts[i].percent = cl.cshifts[i].percent;
            }
            for (int j = 0; j < 3; j++)
                if (cl.cshifts[i].destcolor[j] != cl.prev_cshifts[i].destcolor[j])
                {
                    isnew = true;
                    cl.prev_cshifts[i].destcolor[j] = cl.cshifts[i].destcolor[j];
                }
        }

        // drop the damage value
        cl.cshifts[q_shared.CSHIFT_DAMAGE].percent -= (int)(host_framtime * 150);
        if (cl.cshifts[q_shared.CSHIFT_DAMAGE].percent < 0)
            cl.cshifts[q_shared.CSHIFT_DAMAGE].percent = 0;

        // drop the bonus value
        cl.cshifts[q_shared.CSHIFT_BONUS].percent -= (int)(host_framtime * 100);
        if (cl.cshifts[q_shared.CSHIFT_BONUS].percent < 0)
            cl.cshifts[q_shared.CSHIFT_BONUS].percent = 0;

        bool force = V_CheckGamma();
        if (!isnew && !force)
            return;

        V_CalcBlend();

        float a = v_blend.A;
        float r = 255 * v_blend.R * a;
        float g = 255 * v_blend.G * a;
        float b = 255 * v_blend.B * a;

        a = 1 - a;
        for (int i = 0; i < 256; i++)
        {
            int ir = (int)(i * a + r);
            int ig = (int)(i * a + g);
            int ib = (int)(i * a + b);
            if (ir > 255)
                ir = 255;
            if (ig > 255)
                ig = 255;
            if (ib > 255)
                ib = 255;

            ramps[0, i] = _GammaTable[ir];
            ramps[1, i] = _GammaTable[ig];
            ramps[2, i] = _GammaTable[ib];
        }

        byte[] basepal = host_basepal;
        int offset = 0;
        byte[] newpal = new byte[768];

        for (int i = 0; i < 256; i++)
        {
            int ir = basepal[offset + 0];
            int ig = basepal[offset + 1];
            int ib = basepal[offset + 2];

            newpal[offset + 0] = ramps[0, ir];
            newpal[offset + 1] = ramps[1, ig];
            newpal[offset + 2] = ramps[2, ib];

            offset += 3;
        }

        VID_ShiftPalette(newpal);
    }
    static void BuildGammaTable(float g)
    {
        if (g == 1.0f)
        {
            for (int i = 0; i < 256; i++)
            {
                _GammaTable[i] = (byte)i;
            }
        }
        else
        {
            for (int i = 0; i < 256; i++)
            {
                int inf = (int)(255 * Math.Pow((i + 0.5) / 255.5, g) + 0.5);
                if (inf < 0) inf = 0;
                if (inf > 255) inf = 255;
                _GammaTable[i] = (byte)inf;
            }
        }
    }
    public static void V_StartPitchDrift()
    {
        if (cl.laststop == cl.time)
        {
            return; // something else is keeping it from drifting
        }
        if (cl.nodrift || cl.pitchvel == 0)
        {
            cl.pitchvel = v_centerspeed.value;
            cl.nodrift = false;
            cl.driftmove = 0;
        }
    }
    public static void V_StopPitchDrift()
    {
        cl.laststop = cl.time;
        cl.nodrift = true;
        cl.pitchvel = 0;
    }
    static void V_cshift_f()
    {
        int.TryParse(Cmd_Argv(1), out _CShift_empty.destcolor[0]);
        int.TryParse(Cmd_Argv(2), out _CShift_empty.destcolor[1]);
        int.TryParse(Cmd_Argv(3), out _CShift_empty.destcolor[2]);
        int.TryParse(Cmd_Argv(4), out _CShift_empty.percent);
    }
    static void V_BonusFlash_f()
    {
        cl.cshifts[q_shared.CSHIFT_BONUS].destcolor[0] = 215;
        cl.cshifts[q_shared.CSHIFT_BONUS].destcolor[1] = 186;
        cl.cshifts[q_shared.CSHIFT_BONUS].destcolor[2] = 69;
        cl.cshifts[q_shared.CSHIFT_BONUS].percent = 50;
    }
    static void V_CalcIntermissionRefdef()
    {
        // ent is the player model (visible when out of body)
        entity_t ent = cl_entities[cl.viewentity];

        // view is the weapon model (only visible from inside body)
        entity_t view = cl.viewent;

        refdef_t rdef = r_refdef;
        rdef.vieworg = ent.origin;
        rdef.viewangles = ent.angles;
        view.model = null;

        // allways idle in intermission
        V_AddIdle(1);
    }
    static void V_CalcRefdef()
    {
        V_DriftPitch();

        // ent is the player model (visible when out of body)
        entity_t ent = cl_entities[cl.viewentity];
        // view is the weapon model (only visible from inside body)
        entity_t view = cl.viewent;

        // transform the view offset by the model's matrix to get the offset from
        // model origin for the view
        ent.angles.Y = cl.viewangles.Y;	// the model should face the view dir
        ent.angles.X = -cl.viewangles.X;	// the model should face the view dir

        float bob = V_CalcBob();

        refdef_t rdef = r_refdef;

        // refresh position
        rdef.vieworg = ent.origin;
        rdef.vieworg.Z += cl.viewheight + bob;

        // never let it sit exactly on a node line, because a water plane can
        // dissapear when viewed with the eye exactly on it.
        // the server protocol only specifies to 1/16 pixel, so add 1/32 in each axis
        rdef.vieworg += q_shared.SmallOffset;
        rdef.viewangles = cl.viewangles;

        V_CalcViewRoll();
        V_AddIdle(v_idlescale.value);

        // offsets
        Vector3 angles = ent.angles;
        angles.X = -angles.X; // because entity pitches are actually backward

        Vector3 forward, right, up;
        Mathlib.AngleVectors(ref angles, out forward, out right, out up);

        rdef.vieworg += forward * scr_ofsx.value + right * scr_ofsy.value + up * scr_ofsz.value;

        V_BoundOffsets();

        // set up gun position
        view.angles = cl.viewangles;

        CalcGunAngle();

        view.origin = ent.origin;
        view.origin.Z += cl.viewheight;
        view.origin += forward * bob * 0.4f;
        view.origin.Z += bob;

        // fudge position around to keep amount of weapon visible
        // roughly equal with different FOV
        float viewSize = scr_viewsize.value; // scr_viewsize

        if (viewSize == 110)
            view.origin.Z += 1;
        else if (viewSize == 100)
            view.origin.Z += 2;
        else if (viewSize == 90)
            view.origin.Z += 1;
        else if (viewSize == 80)
            view.origin.Z += 0.5f;

        view.model = cl.model_precache[cl.stats[q_shared.STAT_WEAPON]];
        view.frame = cl.stats[q_shared.STAT_WEAPONFRAME];
        view.colormap = vid.colormap;

        // set up the refresh position
        rdef.viewangles += cl.punchangle;

        // smooth out stair step ups
        if (cl.onground && ent.origin.Z - _OldZ > 0)
        {
            float steptime = (float)(cl.time - cl.oldtime);
            if (steptime < 0)
                steptime = 0;

            _OldZ += steptime * 80;
            if (_OldZ > ent.origin.Z)
                _OldZ = ent.origin.Z;
            if (ent.origin.Z - _OldZ > 12)
                _OldZ = ent.origin.Z - 12;
            rdef.vieworg.Z += _OldZ - ent.origin.Z;
            view.origin.Z += _OldZ - ent.origin.Z;
        }
        else
            _OldZ = ent.origin.Z;

        if (chase_active.value != 0)
            Chase_Update();
    }
    static void V_AddIdle(float idleScale)
    {
        double time = cl.time;
        Vector3 v = new Vector3(
            (float)(Math.Sin(time * v_ipitch_cycle.value) * v_ipitch_level.value),
            (float)(Math.Sin(time * v_iyaw_cycle.value) * v_iyaw_level.value),
            (float)(Math.Sin(time * v_iroll_cycle.value) * v_iroll_level.value));
        r_refdef.viewangles += v * idleScale;
    }
    static void V_DriftPitch()
    {
        if (noclip_anglehack || !cl.onground || cls.demoplayback)
        {
            cl.driftmove = 0;
            cl.pitchvel = 0;
            return;
        }

        // don't count small mouse motion
        if (cl.nodrift)
        {
            if (Math.Abs(cl.cmd.forwardmove) < cl_forwardspeed.value)
                cl.driftmove = 0;
            else
                cl.driftmove += (float)host_framtime;

            if (cl.driftmove > v_centermove.value)
            {
                V_StartPitchDrift();
            }
            return;
        }

        float delta = cl.idealpitch - cl.viewangles.X;
        if (delta == 0)
        {
            cl.pitchvel = 0;
            return;
        }

        float move = (float)host_framtime * cl.pitchvel;
        cl.pitchvel += (float)host_framtime * v_centerspeed.value;

        if (delta > 0)
        {
            if (move > delta)
            {
                cl.pitchvel = 0;
                move = delta;
            }
            cl.viewangles.X += move;
        }
        else if (delta < 0)
        {
            if (move > -delta)
            {
                cl.pitchvel = 0;
                move = -delta;
            }
            cl.viewangles.X -= move;
        }
    }
    static float V_CalcBob()
    {
        float bobCycle = cl_bobcycle.value;
        float bobUp = cl_bobup.value;
        float cycle = (float)(cl.time - (int)(cl.time / bobCycle) * bobCycle);
        cycle /= bobCycle;
        if (cycle < bobUp)
            cycle = (float)Math.PI * cycle / bobUp;
        else
            cycle = (float)(Math.PI + Math.PI * (cycle - bobUp) / (1.0 - bobUp));

        // bob is proportional to velocity in the xy plane
        // (don't count Z, or jumping messes it up)
        Vector2 tmp = cl.velocity.Xy;
        double bob = tmp.Length * cl_bob.value;
        bob = bob * 0.3 + bob * 0.7 * Math.Sin(cycle);
        if (bob > 4)
            bob = 4;
        else if (bob < -7)
            bob = -7;
        return (float)bob;
    }
    static void V_CalcViewRoll()
    {
        refdef_t rdef = r_refdef;
        float side = V_CalcRoll(ref cl_entities[cl.viewentity].angles, ref cl.velocity);
        rdef.viewangles.Z += side;

        if (v_dmg_time > 0)
        {
            rdef.viewangles.Z += v_dmg_time / v_kicktime.value * v_dmg_roll;
            rdef.viewangles.X += v_dmg_time / v_kicktime.value * v_dmg_pitch;
            v_dmg_time -= (float)host_framtime;
        }

        if (cl.stats[q_shared.STAT_HEALTH] <= 0)
        {
            rdef.viewangles.Z = 80;	// dead view angle
            return;
        }
    }
    static void V_BoundOffsets()
    {
        entity_t ent = cl_entities[cl.viewentity];

        // absolutely bound refresh reletive to entity clipping hull
        // so the view can never be inside a solid wall
        refdef_t rdef = r_refdef;
        if (rdef.vieworg.X < ent.origin.X - 14)
            rdef.vieworg.X = ent.origin.X - 14;
        else if (rdef.vieworg.X > ent.origin.X + 14)
            rdef.vieworg.X = ent.origin.X + 14;

        if (rdef.vieworg.Y < ent.origin.Y - 14)
            rdef.vieworg.Y = ent.origin.Y - 14;
        else if (rdef.vieworg.Y > ent.origin.Y + 14)
            rdef.vieworg.Y = ent.origin.Y + 14;

        if (rdef.vieworg.Z < ent.origin.Z - 22)
            rdef.vieworg.Z = ent.origin.Z - 22;
        else if (rdef.vieworg.Z > ent.origin.Z + 30)
            rdef.vieworg.Z = ent.origin.Z + 30;
    }
    static void CalcGunAngle()
    {
        refdef_t rdef = r_refdef;
        float yaw = rdef.viewangles.Y;
        float pitch = -rdef.viewangles.X;

        yaw = angledelta(yaw - rdef.viewangles.Y) * 0.4f;
        if (yaw > 10)
            yaw = 10;
        if (yaw < -10)
            yaw = -10;
        pitch = angledelta(-pitch - rdef.viewangles.X) * 0.4f;
        if (pitch > 10)
            pitch = 10;
        if (pitch < -10)
            pitch = -10;
        float move = (float)host_framtime * 20;
        if (yaw > _OldYaw)
        {
            if (_OldYaw + move < yaw)
                yaw = _OldYaw + move;
        }
        else
        {
            if (_OldYaw - move > yaw)
                yaw = _OldYaw - move;
        }

        if (pitch > _OldPitch)
        {
            if (_OldPitch + move < pitch)
                pitch = _OldPitch + move;
        }
        else
        {
            if (_OldPitch - move > pitch)
                pitch = _OldPitch - move;
        }

        _OldYaw = yaw;
        _OldPitch = pitch;
        
        cl.viewent.angles.Y = rdef.viewangles.Y + yaw;
        cl.viewent.angles.X = -(rdef.viewangles.X + pitch);

        float idleScale = v_idlescale.value;
        cl.viewent.angles.Z -= (float)(idleScale * Math.Sin(cl.time * v_iroll_cycle.value) * v_iroll_level.value);
        cl.viewent.angles.X -= (float)(idleScale * Math.Sin(cl.time * v_ipitch_cycle.value) * v_ipitch_level.value);
        cl.viewent.angles.Y -= (float)(idleScale * Math.Sin(cl.time * v_iyaw_cycle.value) * v_iyaw_level.value);
    }
    static float angledelta(float a)
    {
        a = Mathlib.anglemod(a);
        if (a > 180)
            a -= 360;
        return a;
    }
    static void V_CalcPowerupCshift()
    {
        if (cl.HasItems(q_shared.IT_QUAD))
        {
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[0] = 0;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[1] = 0;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[2] = 255;
            cl.cshifts[q_shared.CSHIFT_POWERUP].percent = 30;
        }
        else if (cl.HasItems(q_shared.IT_SUIT))
        {
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[0] = 0;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[1] = 255;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[2] = 0;
            cl.cshifts[q_shared.CSHIFT_POWERUP].percent = 20;
        }
        else if (cl.HasItems(q_shared.IT_INVISIBILITY))
        {
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[0] = 100;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[1] = 100;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[2] = 100;
            cl.cshifts[q_shared.CSHIFT_POWERUP].percent = 100;
        }
        else if (cl.HasItems(q_shared.IT_INVULNERABILITY))
        {
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[0] = 255;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[1] = 255;
            cl.cshifts[q_shared.CSHIFT_POWERUP].destcolor[2] = 0;
            cl.cshifts[q_shared.CSHIFT_POWERUP].percent = 30;
        }
        else
            cl.cshifts[q_shared.CSHIFT_POWERUP].percent = 0;
    }
    static bool V_CheckGamma()
    {
        if (gamma.value == _OldGammaValue)
            return false;

        _OldGammaValue = gamma.value;

        BuildGammaTable(gamma.value);
        vid.recalc_refdef = true;	// force a surface cache flush

        return true;
    }
    public static void V_CalcBlend()
    {
        float r = 0;
        float g = 0;
        float b = 0;
        float a = 0;

        cshift_t[] cshifts = cl.cshifts;

        if (gl_cshiftpercent.value != 0)
        {
            for (int j = 0; j < q_shared.NUM_CSHIFTS; j++)
            {
                float a2 = ((cshifts[j].percent * gl_cshiftpercent.value) / 100.0f) / 255.0f;

                if (a2 == 0)
                    continue;

                a = a + a2 * (1 - a);

                a2 = a2 / a;
                r = r * (1 - a2) + cshifts[j].destcolor[0] * a2;
                g = g * (1 - a2) + cshifts[j].destcolor[1] * a2;
                b = b * (1 - a2) + cshifts[j].destcolor[2] * a2;
            }
        }

        v_blend.R = r / 255.0f;
        v_blend.G = g / 255.0f;
        v_blend.B = b / 255.0f;
        v_blend.A = a;
        if (v_blend.A > 1)
            v_blend.A = 1;
        if (v_blend.A < 0)
            v_blend.A = 0;
    }
    static void VID_ShiftPalette(byte[] palette)
    {
        //	VID_SetPalette (palette);
        //	gammaworks = SetDeviceGammaRamp (maindc, ramps);
    }
    public static void V_ParseDamage()
    {
        int armor = Reader.MSG_ReadByte();
        int blood = Reader.MSG_ReadByte();
        Vector3 from = Reader.ReadCoords();

        float count = blood * 0.5f + armor * 0.5f;
        if (count < 10)
            count = 10;
        
        cl.faceanimtime = (float)cl.time + 0.2f; // put sbar face into pain frame

        cl.cshifts[q_shared.CSHIFT_DAMAGE].percent += (int)(3 * count);
        if (cl.cshifts[q_shared.CSHIFT_DAMAGE].percent < 0)
            cl.cshifts[q_shared.CSHIFT_DAMAGE].percent = 0;
        if (cl.cshifts[q_shared.CSHIFT_DAMAGE].percent > 150)
            cl.cshifts[q_shared.CSHIFT_DAMAGE].percent = 150;

        if (armor > blood)
        {
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[0] = 200;
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[1] = 100;
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[2] = 100;
        }
        else if (armor != 0)
        {
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[0] = 220;
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[1] = 50;
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[2] = 50;
        }
        else
        {
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[0] = 255;
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[1] = 0;
            cl.cshifts[q_shared.CSHIFT_DAMAGE].destcolor[2] = 0;
        }

        //
        // calculate view angle kicks
        //
        entity_t ent = cl_entities[cl.viewentity];

        from -= ent.origin; //  VectorSubtract (from, ent->origin, from);
        Mathlib.Normalize(ref from);

        Vector3 forward, right, up;
        Mathlib.AngleVectors(ref ent.angles, out forward, out right, out up);

        float side = Vector3.Dot(from, right);

        v_dmg_roll = count * side * v_kickroll.value;

        side = Vector3.Dot(from, forward);
        v_dmg_pitch = count * side * v_kickpitch.value;

        v_dmg_time = v_kicktime.value;
    }
    public static void V_SetContentsColor(int contents)
    {
        switch (contents)
        {
            case q_shared.CONTENTS_EMPTY:
            case q_shared.CONTENTS_SOLID:
                cl.cshifts[q_shared.CSHIFT_CONTENTS] = _CShift_empty;
                break;

            case q_shared.CONTENTS_LAVA:
                cl.cshifts[q_shared.CSHIFT_CONTENTS] = _CShift_lava;
                break;

            case q_shared.CONTENTS_SLIME:
                cl.cshifts[q_shared.CSHIFT_CONTENTS] = _CShift_slime;
                break;

            default:
                cl.cshifts[q_shared.CSHIFT_CONTENTS] = _CShift_water;
                break;
        }
    }
}