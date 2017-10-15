using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static void SV_Physics()
    {
        // let the progs know that a new frame has started
        pr_global_struct.self = EDICT_TO_PROG(sv.edicts[0]);
        pr_global_struct.other = pr_global_struct.self;
        pr_global_struct.time = (float)sv.time;
        PR_ExecuteProgram(pr_global_struct.StartFrame);

        //
        // treat each object in turn
        //
        for (int i = 0; i < sv.num_edicts; i++)
        {
            edict_t ent = sv.edicts[i];
            if (ent.free)
                continue;

            if (pr_global_struct.force_retouch != 0)
            {
                SV_LinkEdict(ent, true);	// force retouch even for stationary
            }

            if (i > 0 && i <= svs.maxclients)
                SV_Physics_Client(ent, i);
            else
                switch ((int)ent.v.movetype)
                {
                    case q_shared.MOVETYPE_PUSH:
                        SV_Physics_Pusher(ent);
                        break;

                    case q_shared.MOVETYPE_NONE:
                        SV_Physics_None(ent);
                        break;

                    case q_shared.MOVETYPE_NOCLIP:
                        SV_Physics_Noclip(ent);
                        break;

                    case q_shared.MOVETYPE_STEP:
                        SV_Physics_Step(ent);
                        break;

                    case q_shared.MOVETYPE_TOSS:
                    case q_shared.MOVETYPE_BOUNCE:
                    case q_shared.MOVETYPE_FLY:
                    case q_shared.MOVETYPE_FLYMISSILE:
                        SV_Physics_Toss(ent);
                        break;

                    default:
                        Sys_Error("SV_Physics: bad movetype {0}", (int)ent.v.movetype);
                        break;
                }
        }

        if (pr_global_struct.force_retouch != 0)
            pr_global_struct.force_retouch -= 1;

        sv.time += host_framtime;
    }
    public static void SV_Physics_Toss(edict_t ent)
    {
        // regular thinking
        if (!SV_RunThink(ent))
            return;

        // if onground, return without moving
        if (((int)ent.v.flags & q_shared.FL_ONGROUND) != 0)
            return;

        SV_CheckVelocity(ent);

        // add gravity
        if (ent.v.movetype != q_shared.MOVETYPE_FLY && ent.v.movetype != q_shared.MOVETYPE_FLYMISSILE)
            SV_AddGravity(ent);


        // move angles
        Mathlib.VectorMA(ref ent.v.angles, (float)host_framtime, ref ent.v.avelocity, out ent.v.angles);

        // move origin
        v3f move;
        Mathlib.VectorScale(ref ent.v.velocity, (float)host_framtime, out move);
        trace_t trace = PushEntity(ent, ref move);

        if (trace.fraction == 1)
            return;
        if (ent.free)
            return;

        float backoff;
        if (ent.v.movetype == q_shared.MOVETYPE_BOUNCE)
            backoff = 1.5f;
        else
            backoff = 1;

        ClipVelocity(ref ent.v.velocity, ref trace.plane.normal, out ent.v.velocity, backoff);

        // stop if on ground
        if (trace.plane.normal.Z > 0.7f)
        {
            if (ent.v.velocity.z < 60 || ent.v.movetype != q_shared.MOVETYPE_BOUNCE)
            {
                ent.v.flags = (int)ent.v.flags | q_shared.FL_ONGROUND;
                ent.v.groundentity = EDICT_TO_PROG(trace.ent);
                ent.v.velocity = default(v3f);
                ent.v.avelocity = default(v3f);
            }
        }

        // check for in water
        SV_CheckWaterTransition(ent);
    }
    public static int ClipVelocity(ref v3f src, ref Vector3 normal, out v3f dest, float overbounce)
    {
        int blocked = 0;
        if (normal.Z > 0)
            blocked |= 1;       // floor
        if (normal.Z == 0)
            blocked |= 2;       // step

        float backoff = (src.x * normal.X + src.y * normal.Y + src.z * normal.Z) * overbounce;

        dest.x = src.x - normal.X * backoff;
        dest.y = src.y - normal.Y * backoff;
        dest.z = src.z - normal.Z * backoff;

        if (dest.x > -q_shared.STOP_EPSILON && dest.x < q_shared.STOP_EPSILON) dest.x = 0;
        if (dest.y > -q_shared.STOP_EPSILON && dest.y < q_shared.STOP_EPSILON) dest.y = 0;
        if (dest.z > -q_shared.STOP_EPSILON && dest.z < q_shared.STOP_EPSILON) dest.z = 0;

        return blocked;
    }
    public static trace_t PushEntity(edict_t ent, ref v3f push)
    {
        v3f end;
        Mathlib.VectorAdd(ref ent.v.origin, ref push, out end);

        trace_t trace;
        if (ent.v.movetype == q_shared.MOVETYPE_FLYMISSILE)
            trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, q_shared.MOVE_MISSILE, ent);
        else if (ent.v.solid == q_shared.SOLID_TRIGGER || ent.v.solid == q_shared.SOLID_NOT)
            // only clip against bmodels
            trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, q_shared.MOVE_NOMONSTERS, ent);
        else
            trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, q_shared.MOVE_NORMAL, ent);

        Mathlib.Copy(ref trace.endpos, out ent.v.origin);
        SV_LinkEdict(ent, true);

        if (trace.ent != null)
            SV_Impact(ent, trace.ent);

        return trace;
    }
    public static void SV_CheckWaterTransition(edict_t ent)
    {
        Vector3 org = ToVector(ref ent.v.origin);
        int cont = SV_PointContents(ref org);

        if (ent.v.watertype == 0)
        {
            // just spawned here
            ent.v.watertype = cont;
            ent.v.waterlevel = 1;
            return;
        }

        if (cont <= q_shared.CONTENTS_WATER)
        {
            if (ent.v.watertype == q_shared.CONTENTS_EMPTY)
            {
                // just crossed into water
                SV_StartSound(ent, 0, "misc/h2ohit1.wav", 255, 1);
            }
            ent.v.watertype = cont;
            ent.v.waterlevel = 1;
        }
        else
        {
            if (ent.v.watertype != q_shared.CONTENTS_EMPTY)
            {
                // just crossed into water
                SV_StartSound(ent, 0, "misc/h2ohit1.wav", 255, 1);
            }
            ent.v.watertype = q_shared.CONTENTS_EMPTY;
            ent.v.waterlevel = cont;
        }
    }
    public static void SV_AddGravity(edict_t ent)
    {
        float val = GetEdictFieldFloat(ent, "gravity");
        if (val == 0)
            val = 1;
        ent.v.velocity.z -= (float)(val * sv_gravity.value * host_framtime);
    }
    public static void SV_Physics_Step(edict_t ent)
    {
        bool hitsound;

        // freefall if not onground
        if (((int)ent.v.flags & (q_shared.FL_ONGROUND | q_shared.FL_FLY | q_shared.FL_SWIM)) == 0)
        {
            if (ent.v.velocity.z < sv_gravity.value * -0.1)
                hitsound = true;
            else
                hitsound = false;

            SV_AddGravity(ent);
            SV_CheckVelocity(ent);
            SV_FlyMove(ent, (float)host_framtime, null);
            SV_LinkEdict(ent, true);

            if (((int)ent.v.flags & q_shared.FL_ONGROUND) != 0)	// just hit ground
            {
                if (hitsound)
                    SV_StartSound(ent, 0, "demon/dland2.wav", 255, 1);
            }
        }

        // regular thinking
        SV_RunThink(ent);

        SV_CheckWaterTransition(ent);
    }
    public static void SV_Physics_Noclip(edict_t ent)
    {
        // regular thinking
        if (!SV_RunThink(ent))
            return;

        Mathlib.VectorMA(ref ent.v.angles, (float)host_framtime, ref ent.v.avelocity, out ent.v.angles);
        Mathlib.VectorMA(ref ent.v.origin, (float)host_framtime, ref ent.v.velocity, out ent.v.origin);
        SV_LinkEdict(ent, false);
    }
    public static void SV_Physics_None(edict_t ent)
    {
        // regular thinking
        SV_RunThink(ent);
    }
    public static void SV_Physics_Pusher(edict_t ent)
    {
        float oldltime = ent.v.ltime;
        float thinktime = ent.v.nextthink;
        float movetime;
        if (thinktime < ent.v.ltime + host_framtime)
        {
            movetime = thinktime - ent.v.ltime;
            if (movetime < 0)
                movetime = 0;
        }
        else
            movetime = (float)host_framtime;

        if (movetime != 0)
        {
            SV_PushMove(ent, movetime);	// advances ent.v.ltime if not blocked
        }

        if (thinktime > oldltime && thinktime <= ent.v.ltime)
        {
            ent.v.nextthink = 0;
            pr_global_struct.time = (float)sv.time;
            pr_global_struct.self = EDICT_TO_PROG(ent);
            pr_global_struct.other = EDICT_TO_PROG(sv.edicts[0]);
            PR_ExecuteProgram(ent.v.think);
            if (ent.free)
                return;
        }
    }
    public static void SV_Physics_Client(edict_t ent, int num)
    {
        if (!svs.clients[num - 1].active)
            return;		// unconnected slot

        //
        // call standard client pre-think
        //	
        pr_global_struct.time = (float)sv.time;
        pr_global_struct.self = EDICT_TO_PROG(ent);
        PR_ExecuteProgram(pr_global_struct.PlayerPreThink);

        //
        // do a move
        //
        SV_CheckVelocity(ent);

        //
        // decide which move function to call
        //
        switch ((int)ent.v.movetype)
        {
            case q_shared.MOVETYPE_NONE:
                if (!SV_RunThink(ent))
                    return;
                break;

            case q_shared.MOVETYPE_WALK:
                if (!SV_RunThink(ent))
                    return;
                if (!SV_CheckWater(ent) && ((int)ent.v.flags & q_shared.FL_WATERJUMP) == 0)
                    SV_AddGravity(ent);
                SV_CheckStuck(ent);

                SV_WalkMove(ent);
                break;

            case q_shared.MOVETYPE_TOSS:
            case q_shared.MOVETYPE_BOUNCE:
                SV_Physics_Toss(ent);
                break;

            case q_shared.MOVETYPE_FLY:
                if (!SV_RunThink(ent))
                    return;
                SV_FlyMove(ent, (float)host_framtime, null);
                break;

            case q_shared.MOVETYPE_NOCLIP:
                if (!SV_RunThink(ent))
                    return;
                Mathlib.VectorMA(ref ent.v.origin, (float)host_framtime, ref ent.v.velocity, out ent.v.origin);
                break;

            default:
                Sys_Error("SV_Physics_client: bad movetype {0}", (int)ent.v.movetype);
                break;
        }

        //
        // call standard player post-think
        //		
        SV_LinkEdict(ent, true);

        pr_global_struct.time = (float)sv.time;
        pr_global_struct.self = EDICT_TO_PROG(ent);
        PR_ExecuteProgram(pr_global_struct.PlayerPostThink);
    }
    public static void SV_WalkMove(edict_t ent)
    {
        //
        // do a regular slide move unless it looks like you ran into a step
        //
        int oldonground = (int)ent.v.flags & q_shared.FL_ONGROUND;
        ent.v.flags = (int)ent.v.flags & ~q_shared.FL_ONGROUND;

        v3f oldorg = ent.v.origin;
        v3f oldvel = ent.v.velocity;
        trace_t steptrace = new trace_t();
        int clip = SV_FlyMove(ent, (float)host_framtime, steptrace);

        if ((clip & 2) == 0)
            return;		// move didn't block on a step

        if (oldonground == 0 && ent.v.waterlevel == 0)
            return;		// don't stair up while jumping

        if (ent.v.movetype != q_shared.MOVETYPE_WALK)
            return;		// gibbed by a trigger

        if (sv_nostep.value != 0)
            return;

        if (((int)sv_player.v.flags & q_shared.FL_WATERJUMP) != 0)
            return;

        v3f nosteporg = ent.v.origin;
        v3f nostepvel = ent.v.velocity;

        //
        // try moving up and forward to go up a step
        //
        ent.v.origin = oldorg;	// back to start pos

        v3f upmove = q_shared.ZeroVector3f;
        v3f downmove = upmove;
        upmove.z = q_shared.STEPSIZE;
        downmove.z = (float)(-q_shared.STEPSIZE + oldvel.z * host_framtime);

        // move up
        PushEntity(ent, ref upmove);	// FIXME: don't link?

        // move forward
        ent.v.velocity.x = oldvel.x;
        ent.v.velocity.y = oldvel.y;
        ent.v.velocity.z = 0;
        clip = SV_FlyMove(ent, (float)host_framtime, steptrace);

        // check for stuckness, possibly due to the limited precision of floats
        // in the clipping hulls
        if (clip != 0)
        {
            if (Math.Abs(oldorg.y - ent.v.origin.y) < 0.03125 && Math.Abs(oldorg.x - ent.v.origin.x) < 0.03125)
            {
                // stepping up didn't make any progress
                clip = SV_TryUnstick(ent, ref oldvel);
            }
        }

        // extra friction based on view angle
        if ((clip & 2) != 0)
            SV_WallFriction(ent, steptrace);

        // move down
        trace_t downtrace = PushEntity(ent, ref downmove);	// FIXME: don't link?

        if (downtrace.plane.normal.Z > 0.7)
        {
            if (ent.v.solid == q_shared.SOLID_BSP)
            {
                ent.v.flags = (int)ent.v.flags | q_shared.FL_ONGROUND;
                ent.v.groundentity = EDICT_TO_PROG(downtrace.ent);
            }
        }
        else
        {
            // if the push down didn't end up on good ground, use the move without
            // the step up.  This happens near wall / slope combinations, and can
            // cause the player to hop up higher on a slope too steep to climb	
            ent.v.origin = nosteporg;
            ent.v.velocity = nostepvel;
        }
    }
    public static int SV_TryUnstick(edict_t ent, ref v3f oldvel)
    {
        v3f oldorg = ent.v.origin;
        v3f dir = q_shared.ZeroVector3f;

        trace_t steptrace = new trace_t();
        for (int i = 0; i < 8; i++)
        {
            // try pushing a little in an axial direction
            switch (i)
            {
                case 0: dir.x = 2; dir.y = 0; break;
                case 1: dir.x = 0; dir.y = 2; break;
                case 2: dir.x = -2; dir.y = 0; break;
                case 3: dir.x = 0; dir.y = -2; break;
                case 4: dir.x = 2; dir.y = 2; break;
                case 5: dir.x = -2; dir.y = 2; break;
                case 6: dir.x = 2; dir.y = -2; break;
                case 7: dir.x = -2; dir.y = -2; break;
            }

            PushEntity(ent, ref dir);

            // retry the original move
            ent.v.velocity.x = oldvel.x;
            ent.v.velocity.y = oldvel.y;
            ent.v.velocity.z = 0;
            int clip = SV_FlyMove(ent, 0.1f, steptrace);

            if (Math.Abs(oldorg.y - ent.v.origin.y) > 4 || Math.Abs(oldorg.x - ent.v.origin.x) > 4)
            {
                return clip;
            }

            // go back to the original pos and try again
            ent.v.origin = oldorg;
        }

        ent.v.velocity = q_shared.ZeroVector3f;
        return 7;		// still not moving
    }
    public static void SV_WallFriction(edict_t ent, trace_t trace)
    {
        Vector3 forward, right, up, vangle = ToVector(ref ent.v.v_angle);
        Mathlib.AngleVectors(ref vangle, out forward, out right, out up);
        float d = Vector3.Dot(trace.plane.normal, forward);

        d += 0.5f;
        if (d >= 0)
            return;

        // cut the tangential velocity
        Vector3 vel = ToVector(ref ent.v.velocity);
        float i = Vector3.Dot(trace.plane.normal, vel);
        Vector3 into = trace.plane.normal * i;
        Vector3 side = vel - into;

        ent.v.velocity.x = side.X * (1 + d);
        ent.v.velocity.y = side.Y * (1 + d);
    }
    public static void SV_CheckStuck(edict_t ent)
    {
        if (SV_TestEntityPosition(ent) == null)
        {
            ent.v.oldorigin = ent.v.origin;
            return;
        }

        v3f org = ent.v.origin;
        ent.v.origin = ent.v.oldorigin;
        if (SV_TestEntityPosition(ent) == null)
        {
            Con_DPrintf("Unstuck.\n");
            SV_LinkEdict(ent, true);
            return;
        }

        for (int z = 0; z < 18; z++)
            for (int i = -1; i <= 1; i++)
                for (int j = -1; j <= 1; j++)
                {
                    ent.v.origin.x = org.x + i;
                    ent.v.origin.y = org.y + j;
                    ent.v.origin.z = org.z + z;
                    if (SV_TestEntityPosition(ent) == null)
                    {
                        Con_DPrintf("Unstuck.\n");
                        SV_LinkEdict(ent, true);
                        return;
                    }
                }

        ent.v.origin = org;
        Con_DPrintf("player is stuck.\n");
    }
    public static bool SV_CheckWater(edict_t ent)
    {
        Vector3 point;
        point.X = ent.v.origin.x;
        point.Y = ent.v.origin.y;
        point.Z = ent.v.origin.z + ent.v.mins.z + 1;

        ent.v.waterlevel = 0;
        ent.v.watertype = q_shared.CONTENTS_EMPTY;
        int cont = SV_PointContents(ref point);
        if (cont <= q_shared.CONTENTS_WATER)
        {
            ent.v.watertype = cont;
            ent.v.waterlevel = 1;
            point.Z = ent.v.origin.z + (ent.v.mins.z + ent.v.maxs.z) * 0.5f;
            cont = SV_PointContents(ref point);
            if (cont <= q_shared.CONTENTS_WATER)
            {
                ent.v.waterlevel = 2;
                point.Z = ent.v.origin.z + ent.v.view_ofs.z;
                cont = SV_PointContents(ref point);
                if (cont <= q_shared.CONTENTS_WATER)
                    ent.v.waterlevel = 3;
            }
        }

        return ent.v.waterlevel > 1;
    }
    public static bool SV_RunThink(edict_t ent)
    {
        float thinktime;

        thinktime = ent.v.nextthink;
        if (thinktime <= 0 || thinktime > sv.time + host_framtime)
            return true;

        if (thinktime < sv.time)
            thinktime = (float)sv.time;	// don't let things stay in the past.

        // it is possible to start that way
        // by a trigger with a local time.
        ent.v.nextthink = 0;
        pr_global_struct.time = thinktime;
        pr_global_struct.self = EDICT_TO_PROG(ent);
        pr_global_struct.other = EDICT_TO_PROG(sv.edicts[0]);
        PR_ExecuteProgram(ent.v.think);

        return !ent.free;
    }
    public static void SV_CheckVelocity(edict_t ent)
    {
        //
        // bound velocity
        //
        if (Mathlib.CheckNaN(ref ent.v.velocity, 0))
        {
            Con_Printf("Got a NaN velocity on {0}\n", GetString(ent.v.classname));
        }

        if (Mathlib.CheckNaN(ref ent.v.origin, 0))
        {
            Con_Printf("Got a NaN origin on {0}\n", GetString(ent.v.classname));
        }

        Vector3 max = Vector3.One * sv_maxvelocity.value;
        Vector3 min = -Vector3.One * sv_maxvelocity.value;
        Mathlib.Clamp(ref ent.v.velocity, ref min, ref max, out ent.v.velocity);
    }
    public static int SV_FlyMove(edict_t ent, float time, trace_t steptrace)
    {
        v3f original_velocity = ent.v.velocity;
        v3f primal_velocity = ent.v.velocity;

        int numbumps = 4;
        int blocked = 0;
        Vector3[] planes = new Vector3[q_shared.MAX_CLIP_PLANES];
        int numplanes = 0;
        float time_left = time;

        for (int bumpcount = 0; bumpcount < numbumps; bumpcount++)
        {
            if (ent.v.velocity.IsEmpty)
                break;

            v3f end;
            Mathlib.VectorMA(ref ent.v.origin, time_left, ref ent.v.velocity, out end);

            trace_t trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent);

            if (trace.allsolid)
            {	// entity is trapped in another solid
                ent.v.velocity = default(v3f);
                return 3;
            }

            if (trace.fraction > 0)
            {	// actually covered some distance
                Mathlib.Copy(ref trace.endpos, out ent.v.origin);
                original_velocity = ent.v.velocity;
                numplanes = 0;
            }

            if (trace.fraction == 1)
                break;		// moved the entire distance

            if (trace.ent == null)
                Sys_Error("SV_FlyMove: !trace.ent");

            if (trace.plane.normal.Z > 0.7)
            {
                blocked |= 1;		// floor
                if (trace.ent.v.solid == q_shared.SOLID_BSP)
                {
                    ent.v.flags = (int)ent.v.flags | q_shared.FL_ONGROUND;
                    ent.v.groundentity = EDICT_TO_PROG(trace.ent);
                }
            }

            if (trace.plane.normal.Z == 0)
            {
                blocked |= 2;		// step
                if (steptrace != null)
                    steptrace.CopyFrom(trace);	// save for player extrafriction
            }

            //
            // run the impact function
            //
            SV_Impact(ent, trace.ent);
            if (ent.free)
                break;		// removed by the impact function


            time_left -= time_left * trace.fraction;

            // cliped to another plane
            if (numplanes >= q_shared.MAX_CLIP_PLANES)
            {
                // this shouldn't really happen
                ent.v.velocity = default(v3f);
                return 3;
            }

            planes[numplanes] = trace.plane.normal;
            numplanes++;

            //
            // modify original_velocity so it parallels all of the clip planes
            //
            v3f new_velocity = default(v3f);
            int i, j;
            for (i = 0; i < numplanes; i++)
            {
                ClipVelocity(ref original_velocity, ref planes[i], out new_velocity, 1);
                for (j = 0; j < numplanes; j++)
                    if (j != i)
                    {
                        float dot = new_velocity.x * planes[j].X + new_velocity.y * planes[j].Y + new_velocity.z * planes[j].Z;
                        if (dot < 0)
                            break;	// not ok
                    }
                if (j == numplanes)
                    break;
            }

            if (i != numplanes)
            {
                // go along this plane
                ent.v.velocity = new_velocity;
            }
            else
            {
                // go along the crease
                if (numplanes != 2)
                {
                    ent.v.velocity = default(v3f);
                    return 7;
                }
                Vector3 dir = Vector3.Cross(planes[0], planes[1]);
                float d = dir.X * ent.v.velocity.x + dir.Y * ent.v.velocity.y + dir.Z * ent.v.velocity.z;
                Mathlib.Copy(ref dir, out ent.v.velocity);
                Mathlib.VectorScale(ref ent.v.velocity, d, out ent.v.velocity);
            }

            //
            // if original velocity is against the original velocity, stop dead
            // to avoid tiny occilations in sloping corners
            //
            if (Mathlib.DotProduct(ref ent.v.velocity, ref primal_velocity) <= 0)
            {
                ent.v.velocity = default(v3f);
                return blocked;
            }
        }

        return blocked;
    }
    public static trace_t Move(ref v3f start, ref v3f mins, ref v3f maxs, ref v3f end, int type, edict_t passedict)
    {
        Vector3 vstart, vmins, vmaxs, vend;
        Mathlib.Copy(ref start, out vstart);
        Mathlib.Copy(ref mins, out vmins);
        Mathlib.Copy(ref maxs, out vmaxs);
        Mathlib.Copy(ref end, out vend);
        return SV_Move(ref vstart, ref vmins, ref vmaxs, ref vend, type, passedict);
    }
    public static void SV_Impact(edict_t e1, edict_t e2)
    {
        int old_self = pr_global_struct.self;
        int old_other = pr_global_struct.other;

        pr_global_struct.time = (float)sv.time;
        if (e1.v.touch != 0 && e1.v.solid != q_shared.SOLID_NOT)
        {
            pr_global_struct.self = EDICT_TO_PROG(e1);
            pr_global_struct.other = EDICT_TO_PROG(e2);
            PR_ExecuteProgram(e1.v.touch);
        }

        if (e2.v.touch != 0 && e2.v.solid != q_shared.SOLID_NOT)
        {
            pr_global_struct.self = EDICT_TO_PROG(e2);
            pr_global_struct.other = EDICT_TO_PROG(e1);
            PR_ExecuteProgram(e2.v.touch);
        }

        pr_global_struct.self = old_self;
        pr_global_struct.other = old_other;
    }
    public static void SV_PushMove(edict_t pusher, float movetime)
    {
        if (pusher.v.velocity.IsEmpty)
        {
            pusher.v.ltime += movetime;
            return;
        }

        v3f move, mins, maxs;
        Mathlib.VectorScale(ref pusher.v.velocity, movetime, out move);
        Mathlib.VectorAdd(ref pusher.v.absmin, ref move, out mins);
        Mathlib.VectorAdd(ref pusher.v.absmax, ref move, out maxs);

        v3f pushorig = pusher.v.origin;

        edict_t[] moved_edict = new edict_t[q_shared.MAX_EDICTS];
        v3f[] moved_from = new v3f[q_shared.MAX_EDICTS];

        // move the pusher to it's final position

        Mathlib.VectorAdd(ref pusher.v.origin, ref move, out pusher.v.origin);
        pusher.v.ltime += movetime;
        SV_LinkEdict(pusher, false);


        // see if any solid entities are inside the final position
        int num_moved = 0;
        for (int e = 1; e < sv.num_edicts; e++)
        {
            edict_t check = sv.edicts[e];
            if (check.free)
                continue;
            if (check.v.movetype == q_shared.MOVETYPE_PUSH ||
                check.v.movetype == q_shared.MOVETYPE_NONE ||
                check.v.movetype == q_shared.MOVETYPE_NOCLIP)
                continue;

            // if the entity is standing on the pusher, it will definately be moved
            if (!(((int)check.v.flags & q_shared.FL_ONGROUND) != 0 && PROG_TO_EDICT(check.v.groundentity) == pusher))
            {
                if (check.v.absmin.x >= maxs.x || check.v.absmin.y >= maxs.y ||
                    check.v.absmin.z >= maxs.z || check.v.absmax.x <= mins.x ||
                    check.v.absmax.y <= mins.y || check.v.absmax.z <= mins.z)
                    continue;

                // see if the ent's bbox is inside the pusher's final position
                if (SV_TestEntityPosition(check) == null)
                    continue;
            }

            // remove the onground flag for non-players
            if (check.v.movetype != q_shared.MOVETYPE_WALK)
                check.v.flags = (int)check.v.flags & ~q_shared.FL_ONGROUND;

            v3f entorig = check.v.origin;
            moved_from[num_moved] = entorig;
            moved_edict[num_moved] = check;
            num_moved++;

            // try moving the contacted entity 
            pusher.v.solid = q_shared.SOLID_NOT;
            PushEntity(check, ref move);
            pusher.v.solid = q_shared.SOLID_BSP;

            // if it is still inside the pusher, block
            edict_t block = SV_TestEntityPosition(check);
            if (block != null)
            {
                // fail the move
                if (check.v.mins.x == check.v.maxs.x)
                    continue;
                if (check.v.solid == q_shared.SOLID_NOT || check.v.solid == q_shared.SOLID_TRIGGER)
                {
                    // corpse
                    check.v.mins.x = check.v.mins.y = 0;
                    check.v.maxs = check.v.mins;
                    continue;
                }

                check.v.origin = entorig;
                SV_LinkEdict(check, true);

                pusher.v.origin = pushorig;
                SV_LinkEdict(pusher, false);
                pusher.v.ltime -= movetime;

                // if the pusher has a "blocked" function, call it
                // otherwise, just stay in place until the obstacle is gone
                if (pusher.v.blocked != 0)
                {
                    pr_global_struct.self = EDICT_TO_PROG(pusher);
                    pr_global_struct.other = EDICT_TO_PROG(check);
                    PR_ExecuteProgram(pusher.v.blocked);
                }

                // move back any entities we already moved
                for (int i = 0; i < num_moved; i++)
                {
                    moved_edict[i].v.origin = moved_from[i];
                    SV_LinkEdict(moved_edict[i], false);
                }
                return;
            }
        }
    }
}