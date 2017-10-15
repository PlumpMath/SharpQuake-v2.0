using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static bool SV_movestep(edict_t ent, ref v3f move, bool relink)
    {
        trace_t trace;

        // try the move
        v3f oldorg = ent.v.origin;
        v3f neworg;
        Mathlib.VectorAdd(ref ent.v.origin, ref move, out neworg);

        // flying monsters don't step up
        if (((int)ent.v.flags & (q_shared.FL_SWIM | q_shared.FL_FLY)) != 0)
        {
            // try one move with vertical motion, then one without
            for (int i = 0; i < 2; i++)
            {
                Mathlib.VectorAdd(ref ent.v.origin, ref move, out neworg);
                edict_t enemy = PROG_TO_EDICT(ent.v.enemy);
                if (i == 0 && enemy != sv.edicts[0])
                {
                    float dz = ent.v.origin.z - enemy.v.origin.z;
                    if (dz > 40)
                        neworg.z -= 8;
                    if (dz < 30)
                        neworg.z += 8;
                }

                trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref neworg, 0, ent);
                if (trace.fraction == 1)
                {
                    if (((int)ent.v.flags & q_shared.FL_SWIM) != 0 &&
                        SV_PointContents(ref trace.endpos) == q_shared.CONTENTS_EMPTY)
                        return false;	// swim monster left water

                    Mathlib.Copy(ref trace.endpos, out ent.v.origin);
                    if (relink)
                        SV_LinkEdict(ent, true);
                    return true;
                }

                if (enemy == sv.edicts[0])
                    break;
            }

            return false;
        }

        // push down from a step height above the wished position
        neworg.z += q_shared.STEPSIZE;
        v3f end = neworg;
        end.z -= q_shared.STEPSIZE * 2;

        trace = Move(ref neworg, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent);

        if (trace.allsolid)
            return false;

        if (trace.startsolid)
        {
            neworg.z -= q_shared.STEPSIZE;
            trace = Move(ref neworg, ref ent.v.mins, ref ent.v.maxs, ref end, 0, ent);
            if (trace.allsolid || trace.startsolid)
                return false;
        }
        if (trace.fraction == 1)
        {
            // if monster had the ground pulled out, go ahead and fall
            if (((int)ent.v.flags & q_shared.FL_PARTIALGROUND) != 0)
            {
                Mathlib.VectorAdd(ref ent.v.origin, ref move, out ent.v.origin);
                if (relink)
                    SV_LinkEdict(ent, true);
                ent.v.flags = (int)ent.v.flags & ~q_shared.FL_ONGROUND;
                return true;
            }

            return false;		// walked off an edge
        }

        // check point traces down for dangling corners
        Mathlib.Copy(ref trace.endpos, out ent.v.origin);

        if (!SV_CheckBottom(ent))
        {
            if (((int)ent.v.flags & q_shared.FL_PARTIALGROUND) != 0)
            {
                // entity had floor mostly pulled out from underneath it
                // and is trying to correct
                if (relink)
                    SV_LinkEdict(ent, true);
                return true;
            }
            ent.v.origin = oldorg;
            return false;
        }

        if (((int)ent.v.flags & q_shared.FL_PARTIALGROUND) != 0)
        {
            ent.v.flags = (int)ent.v.flags & ~q_shared.FL_PARTIALGROUND;
        }
        ent.v.groundentity = EDICT_TO_PROG(trace.ent);

        // the move is ok
        if (relink)
            SV_LinkEdict(ent, true);
        return true;
    }
    public static bool SV_CheckBottom(edict_t ent)
    {
        v3f mins, maxs;
        Mathlib.VectorAdd(ref ent.v.origin, ref ent.v.mins, out mins);
        Mathlib.VectorAdd(ref ent.v.origin, ref ent.v.maxs, out maxs);

        // if all of the points under the corners are solid world, don't bother
        // with the tougher checks
        // the corners must be within 16 of the midpoint
        Vector3 start;
        start.Z = mins.z - 1;
        for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            {
                start.X = (x != 0 ? maxs.x : mins.x);
                start.Y = (y != 0 ? maxs.y : mins.y);
                if (SV_PointContents(ref start) != q_shared.CONTENTS_SOLID)
                    goto RealCheck;
            }

        return true;		// we got out easy

        RealCheck:

        //
        // check it for real...
        //
        start.Z = mins.z;

        // the midpoint must be within 16 of the bottom
        start.X = (mins.x + maxs.x) * 0.5f;
        start.Y = (mins.y + maxs.y) * 0.5f;
        Vector3 stop = start;
        stop.Z -= 2 * q_shared.STEPSIZE;
        trace_t trace = SV_Move(ref start, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref stop, 1, ent);

        if (trace.fraction == 1.0)
            return false;

        float mid = trace.endpos.Z;
        float bottom = mid;

        // the corners must be within 16 of the midpoint	
        for (int x = 0; x <= 1; x++)
            for (int y = 0; y <= 1; y++)
            {
                start.X = stop.X = (x != 0 ? maxs.x : mins.x);
                start.Y = stop.Y = (y != 0 ? maxs.y : mins.y);

                trace = SV_Move(ref start, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref stop, 1, ent);

                if (trace.fraction != 1.0 && trace.endpos.Z > bottom)
                    bottom = trace.endpos.Z;
                if (trace.fraction == 1.0 || mid - trace.endpos.Z > q_shared.STEPSIZE)
                    return false;
            }

        return true;
    }
    public static void SV_MoveToGoal()
    {
        edict_t ent = PROG_TO_EDICT(pr_global_struct.self);
        edict_t goal = PROG_TO_EDICT(ent.v.goalentity);
        float dist = G_FLOAT(q_shared.OFS_PARM0);

        if (((int)ent.v.flags & (q_shared.FL_ONGROUND | q_shared.FL_FLY | q_shared.FL_SWIM)) == 0)
        {
            G_FLOAT((float)0);
            return;
        }

        // if the next step hits the enemy, return immediately
        if (PROG_TO_EDICT(ent.v.enemy) != sv.edicts[0] && SV_CloseEnough(ent, goal, dist))
            return;

        // bump around...
        if ((Random() & 3) == 1 || !SV_StepDirection(ent, ent.v.ideal_yaw, dist))
        {
            SV_NewChaseDir(ent, goal, dist);
        }
    }
    public static bool SV_CloseEnough(edict_t ent, edict_t goal, float dist)
    {
        if (goal.v.absmin.x > ent.v.absmax.x + dist) return false;
        if (goal.v.absmin.y > ent.v.absmax.y + dist) return false;
        if (goal.v.absmin.z > ent.v.absmax.z + dist) return false;

        if (goal.v.absmax.x < ent.v.absmin.x - dist) return false;
        if (goal.v.absmax.y < ent.v.absmin.y - dist) return false;
        if (goal.v.absmax.z < ent.v.absmin.z - dist) return false;
            
        return true;
    }
    public static bool SV_StepDirection(edict_t ent, float yaw, float dist)
    {
        ent.v.ideal_yaw = yaw;
        PF_changeyaw();

        yaw = (float)(yaw * Math.PI * 2.0 / 360);
        v3f move;
        move.x = (float)Math.Cos(yaw) * dist;
        move.y = (float)Math.Sin(yaw) * dist;
        move.z = 0;

        v3f oldorigin = ent.v.origin;
        if (SV_movestep(ent, ref move, false))
        {
            float delta = ent.v.angles.y - ent.v.ideal_yaw;
            if (delta > 45 && delta < 315)
            {
                // not turned far enough, so don't take the step
                ent.v.origin = oldorigin;
            }
            SV_LinkEdict(ent, true);
            return true;
        }
        SV_LinkEdict(ent, true);

        return false;
    }
    public static void SV_NewChaseDir(edict_t actor, edict_t enemy, float dist)
    {
        float olddir = Mathlib.anglemod((int)(actor.v.ideal_yaw / 45) * 45);
        float turnaround = Mathlib.anglemod(olddir - 180);

        float deltax = enemy.v.origin.x - actor.v.origin.x;
        float deltay = enemy.v.origin.y - actor.v.origin.y;
        v3f d;
        if (deltax > 10)
            d.y = 0;
        else if (deltax < -10)
            d.y = 180;
        else
            d.y = q_shared.DI_NODIR;
        if (deltay < -10)
            d.z = 270;
        else if (deltay > 10)
            d.z = 90;
        else
            d.z = q_shared.DI_NODIR;

        // try direct route
        float tdir;
        if (d.y != q_shared.DI_NODIR && d.z != q_shared.DI_NODIR)
        {
            if (d.y == 0)
                tdir = (d.z == 90 ? 45 : 315);
            else
                tdir = (d.z == 90 ? 135 : 215);

            if (tdir != turnaround && SV_StepDirection(actor, tdir, dist))
                return;
        }

        // try other directions
        if (((Random() & 3) & 1) != 0 || Math.Abs(deltay) > Math.Abs(deltax))
        {
            tdir = d.y;
            d.y = d.z;
            d.z = tdir;
        }

        if (d.y != q_shared.DI_NODIR && d.y != turnaround && SV_StepDirection(actor, d.y, dist))
            return;

        if (d.z != q_shared.DI_NODIR && d.z != turnaround && SV_StepDirection(actor, d.z, dist))
            return;

        // there is no direct path to the player, so pick another direction

        if (olddir != q_shared.DI_NODIR && SV_StepDirection(actor, olddir, dist))
            return;

        if ((Random() & 1) != 0) 	//randomly determine direction of search
        {
            for (tdir = 0; tdir <= 315; tdir += 45)
                if (tdir != turnaround && SV_StepDirection(actor, tdir, dist))
                    return;
        }
        else
        {
            for (tdir = 315; tdir >= 0; tdir -= 45)
                if (tdir != turnaround && SV_StepDirection(actor, tdir, dist))
                    return;
        }

        if (turnaround != q_shared.DI_NODIR && SV_StepDirection(actor, turnaround, dist))
            return;

        actor.v.ideal_yaw = olddir;		// can't move

        // if a bridge was pulled out from underneath a monster, it may not have
        // a valid standing position at all

        if (!SV_CheckBottom(actor))
            SV_FixCheckBottom(actor);
    }
    public static void SV_FixCheckBottom(edict_t ent)
    {
        ent.v.flags = (int)ent.v.flags | q_shared.FL_PARTIALGROUND;
    }
}