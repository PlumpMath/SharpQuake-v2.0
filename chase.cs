using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static cvar_t chase_back;
    public static cvar_t chase_up;
    public static cvar_t chase_right;
    public static cvar_t chase_active;
    public static Vector3 chase_dest;

    public static void Chase_Init()
    {
        chase_back = new cvar_t("chase_back", "100");
        chase_up = new cvar_t("chase_up", "16");
        chase_right = new cvar_t("chase_right", "0");
        chase_active = new cvar_t("chase_active", "0");
    }
    public static void Chase_Reset()
    {
        // for respawning and teleporting
        //	start position 12 units behind head
    }
    public static void Chase_Update()
    {
        // if can't see player, reset
        Vector3 forward, up, right;
        Mathlib.AngleVectors(ref cl.viewangles, out forward, out right, out up);

        // calc exact destination
        chase_dest = r_refdef.vieworg - forward * chase_back.value - right * chase_right.value;
        chase_dest.Z = r_refdef.vieworg.Z + chase_up.value;

        // find the spot the player is looking at
        Vector3 dest = r_refdef.vieworg + forward * 4096;

        Vector3 stop;
        TraceLine(ref r_refdef.vieworg, ref dest, out stop);

        // calculate pitch to look at the same spot from camera
        stop -= r_refdef.vieworg;
        float dist;
        Vector3.Dot(ref stop, ref forward, out dist);
        if (dist < 1)
            dist = 1;

        r_refdef.viewangles.X = (float)(-Math.Atan(stop.Z / dist) / Math.PI * 180.0);
        //r_refdef.viewangles[PITCH] = -atan(stop[2] / dist) / M_PI * 180;

        // move towards destination
        r_refdef.vieworg = chase_dest; //VectorCopy(chase_dest, r_refdef.vieworg);
    }
    public static void TraceLine(ref Vector3 start, ref Vector3 end, out Vector3 impact)
    {
        trace_t trace = new trace_t();

        SV_RecursiveHullCheck(cl.worldmodel.hulls[0], 0, 0, 1, ref start, ref end, trace);

        impact = trace.endpos;
    }
}