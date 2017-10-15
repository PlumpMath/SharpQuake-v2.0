using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static edict_t sv_player;
    public static bool onground;

    public static usercmd_t cmd;

    //public static Vector3 forward;
    //public static Vector3 right;
    //public static Vector3 up;

    public static Vector3 wishdir;
    public static float wishspeed;

    public static void SV_RunClients()
    {
        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];
            if (!host_client.active)
                continue;

            sv_player = host_client.edict;

            if (!SV_ReadClientMessage())
            {
                SV_DropClient(false);   // client misbehaved...
                continue;
            }

            if (!host_client.spawned)
            {
                // clear client movement until a new packet is received
                host_client.cmd.Clear();
                continue;
            }

            // always pause in single player if in console or menus
            if (!sv.paused && (svs.maxclients > 1 || key_dest == keydest_t.key_game))
                SV_ClientThink();
        }
    }
    public static bool SV_ReadClientMessage()
    {
        while (true)
        {
            int ret = NET_GetMessage(host_client.netconnection);
            if (ret == -1)
            {
                Con_DPrintf("SV_ReadClientMessage: NET_GetMessage failed\n");
                return false;
            }
            if (ret == 0)
                return true;

            Reader.MSG_BeginReading();

            bool flag = true;
            while (flag)
            {
                if (!host_client.active)
                    return false;   // a command caused an error

                if (Reader.msg_badread)
                {
                    Con_DPrintf("SV_ReadClientMessage: badread\n");
                    return false;
                }

                int cmd = Reader.MSG_ReadChar();
                switch (cmd)
                {
                    case -1:
                        flag = false; // end of message
                        ret = 1;
                        break;

                    case q_shared.clc_nop:
                        break;

                    case q_shared.clc_stringcmd:
                        string s = Reader.MSG_ReadString();
                        if (host_client.privileged)
                            ret = 2;
                        else
                            ret = 0;
                        if (SameText(s, "status", 6))
                            ret = 1;
                        else if (SameText(s, "god", 3))
                            ret = 1;
                        else if (SameText(s, "notarget", 8))
                            ret = 1;
                        else if (SameText(s, "fly", 3))
                            ret = 1;
                        else if (SameText(s, "name", 4))
                            ret = 1;
                        else if (SameText(s, "noclip", 6))
                            ret = 1;
                        else if (SameText(s, "say", 3))
                            ret = 1;
                        else if (SameText(s, "say_team", 8))
                            ret = 1;
                        else if (SameText(s, "tell", 4))
                            ret = 1;
                        else if (SameText(s, "color", 5))
                            ret = 1;
                        else if (SameText(s, "kill", 4))
                            ret = 1;
                        else if (SameText(s, "pause", 5))
                            ret = 1;
                        else if (SameText(s, "spawn", 5))
                            ret = 1;
                        else if (SameText(s, "begin", 5))
                            ret = 1;
                        else if (SameText(s, "prespawn", 8))
                            ret = 1;
                        else if (SameText(s, "kick", 4))
                            ret = 1;
                        else if (SameText(s, "ping", 4))
                            ret = 1;
                        else if (SameText(s, "give", 4))
                            ret = 1;
                        else if (SameText(s, "ban", 3))
                            ret = 1;
                        if (ret == 2)
                            Cbuf_InsertText(s);
                        else if (ret == 1)
                            Cmd_ExecuteString(s, cmd_source_t.src_client);
                        else
                            Con_DPrintf("{0} tried to {1}\n", host_client.name, s);
                        break;

                    case q_shared.clc_disconnect:
                        return false;

                    case q_shared.clc_move:
                        SV_ReadClientMove(ref host_client.cmd);
                        break;

                    default:
                        Con_DPrintf("SV_ReadClientMessage: unknown command char\n");
                        return false;
                }
            }

            if (ret != 1)
                break;
        }

        return true;
    }
    public static void SV_ReadClientMove(ref usercmd_t move)
    {
        client_t client = host_client;

        // read ping time
        client.ping_times[client.num_pings % q_shared.NUM_PING_TIMES] = (float)(sv.time - Reader.MSG_ReadFloat());
        client.num_pings++;

        // read current angles	
        Vector3 angles = Reader.ReadAngles();
        Mathlib.Copy(ref angles, out client.edict.v.v_angle);

        // read movement
        move.forwardmove = Reader.MSG_ReadShort();
        move.sidemove = Reader.MSG_ReadShort();
        move.upmove = Reader.MSG_ReadShort();

        // read buttons
        int bits = Reader.MSG_ReadByte();
        client.edict.v.button0 = bits & 1;
        client.edict.v.button2 = (bits & 2) >> 1;

        int i = Reader.MSG_ReadByte();
        if (i != 0)
            client.edict.v.impulse = i;
    }
    public static void SV_SetIdealPitch()
    {
        if (((int)sv_player.v.flags & q_shared.FL_ONGROUND) == 0)
            return;

        double angleval = sv_player.v.angles.y * Math.PI * 2 / 360;
        double sinval = Math.Sin(angleval);
        double cosval = Math.Cos(angleval);
        float[] z = new float[q_shared.MAX_FORWARD];
        for (int i = 0; i < q_shared.MAX_FORWARD; i++)
        {
            v3f top = sv_player.v.origin;
            top.x += (float)(cosval * (i + 3) * 12);
            top.y += (float)(sinval * (i + 3) * 12);
            top.z += sv_player.v.view_ofs.z;

            v3f bottom = top;
            bottom.z -= 160;

            trace_t tr = Move(ref top, ref q_shared.ZeroVector3f, ref q_shared.ZeroVector3f, ref bottom, 1, sv_player);
            if (tr.allsolid)
                return; // looking at a wall, leave ideal the way is was

            if (tr.fraction == 1)
                return; // near a dropoff

            z[i] = top.z + tr.fraction * (bottom.z - top.z);
        }

        float dir = 0; // Uze: int in original code???
        int steps = 0;
        for (int j = 1; j < q_shared.MAX_FORWARD; j++)
        {
            float step = z[j] - z[j - 1]; // Uze: int in original code???
            if (step > -q_shared.ON_EPSILON && step < q_shared.ON_EPSILON) // Uze: comparing int with ON_EPSILON (0.1)???
                continue;

            if (dir != 0 && (step - dir > q_shared.ON_EPSILON || step - dir < -q_shared.ON_EPSILON))
                return;     // mixed changes

            steps++;
            dir = step;
        }

        if (dir == 0)
        {
            sv_player.v.idealpitch = 0;
            return;
        }

        if (steps < 2)
            return;
        sv_player.v.idealpitch = -dir * sv_idealpitchscale.value;
    }
    public static void SV_ClientThink()
    {
        if (sv_player.v.movetype == q_shared.MOVETYPE_NONE)
            return;

        onground = ((int)sv_player.v.flags & q_shared.FL_ONGROUND) != 0;

        DropPunchAngle();

        //
        // if dead, behave differently
        //
        if (sv_player.v.health <= 0)
            return;

        //
        // angles
        // show 1/3 the pitch angle and all the roll angle
        cmd = host_client.cmd;

        v3f v_angle;
        Mathlib.VectorAdd(ref sv_player.v.v_angle, ref sv_player.v.punchangle, out v_angle);
        Vector3 pang = ToVector(ref sv_player.v.angles);
        Vector3 pvel = ToVector(ref sv_player.v.velocity);
        sv_player.v.angles.z = game_engine.V_CalcRoll(ref pang, ref pvel) * 4;
        if (sv_player.v.fixangle == 0)
        {
            sv_player.v.angles.x = -v_angle.x / 3;
            sv_player.v.angles.y = v_angle.y;
        }

        if (((int)sv_player.v.flags & q_shared.FL_WATERJUMP) != 0)
        {
            SV_WaterJump();
            return;
        }
        //
        // walk
        //
        if ((sv_player.v.waterlevel >= 2) && (sv_player.v.movetype != q_shared.MOVETYPE_NOCLIP))
        {
            SV_WaterMove();
            return;
        }

        SV_AirMove();
    }
    public static void DropPunchAngle()
    {
        Vector3 v = ToVector(ref sv_player.v.punchangle);
        double len = Mathlib.Normalize(ref v) - 10 * host_framtime;
        if (len < 0)
            len = 0;
        v *= (float)len;
        Mathlib.Copy(ref v, out sv_player.v.punchangle);
    }
    public static void SV_WaterJump()
    {
        if (sv.time > sv_player.v.teleport_time || sv_player.v.waterlevel == 0)
        {
            sv_player.v.flags = (int)sv_player.v.flags & ~q_shared.FL_WATERJUMP;
            sv_player.v.teleport_time = 0;
        }
        sv_player.v.velocity.x = sv_player.v.movedir.x;
        sv_player.v.velocity.y = sv_player.v.movedir.y;
    }
    public static void SV_WaterMove()
    {
        //
        // user intentions
        //
        Vector3 pangle = ToVector(ref sv_player.v.v_angle);
        Mathlib.AngleVectors(ref pangle, out forward, out right, out up);
        Vector3 wishvel = forward * cmd.forwardmove + right * cmd.sidemove;

        if (cmd.forwardmove == 0 && cmd.sidemove == 0 && cmd.upmove == 0)
            wishvel.Z -= 60;        // drift towards bottom
        else
            wishvel.Z += cmd.upmove;

        float wishspeed = wishvel.Length;
        if (wishspeed > sv_maxspeed.value)
        {
            wishvel *= sv_maxspeed.value / wishspeed;
            wishspeed = sv_maxspeed.value;
        }
        wishspeed *= 0.7f;

        //
        // water friction
        //
        float newspeed, speed = Mathlib.Length(ref sv_player.v.velocity);
        if (speed != 0)
        {
            newspeed = (float)(speed - host_framtime * speed * sv_friction.value);
            if (newspeed < 0)
                newspeed = 0;
            Mathlib.VectorScale(ref sv_player.v.velocity, newspeed / speed, out sv_player.v.velocity);
        }
        else
            newspeed = 0;

        //
        // water acceleration
        //
        if (wishspeed == 0)
            return;

        float addspeed = wishspeed - newspeed;
        if (addspeed <= 0)
            return;

        Mathlib.Normalize(ref wishvel);
        float accelspeed = (float)(sv_accelerate.value * wishspeed * host_framtime);
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        wishvel *= accelspeed;
        sv_player.v.velocity.x += wishvel.X;
        sv_player.v.velocity.y += wishvel.Y;
        sv_player.v.velocity.z += wishvel.Z;
    }
    public static void SV_AirMove()
    {
        Vector3 pangles = ToVector(ref sv_player.v.angles);
        Mathlib.AngleVectors(ref pangles, out forward, out right, out up);

        float fmove = cmd.forwardmove;
        float smove = cmd.sidemove;

        // hack to not let you back into teleporter
        if (sv.time < sv_player.v.teleport_time && fmove < 0)
            fmove = 0;

        Vector3 wishvel = forward * fmove + right * smove;

        if ((int)sv_player.v.movetype != q_shared.MOVETYPE_WALK)
            wishvel.Z = cmd.upmove;
        else
            wishvel.Z = 0;

        wishdir = wishvel;
        wishspeed = Mathlib.Normalize(ref wishdir);
        if (wishspeed > sv_maxspeed.value)
        {
            wishvel *= sv_maxspeed.value / wishspeed;
            wishspeed = sv_maxspeed.value;
        }

        if (sv_player.v.movetype == q_shared.MOVETYPE_NOCLIP)
        {
            // noclip
            Mathlib.Copy(ref wishvel, out sv_player.v.velocity);
        }
        else if (onground)
        {
            SV_UserFriction();
            SV_Accelerate();
        }
        else
        {   // not on ground, so little effect on velocity
            SV_AirAccelerate(wishvel);
        }
    }
    public static void SV_UserFriction()
    {
        float speed = Mathlib.LengthXY(ref sv_player.v.velocity);
        if (speed == 0)
            return;

        // if the leading edge is over a dropoff, increase friction
        Vector3 start, stop;
        start.X = stop.X = sv_player.v.origin.x + sv_player.v.velocity.x / speed * 16;
        start.Y = stop.Y = sv_player.v.origin.y + sv_player.v.velocity.y / speed * 16;
        start.Z = sv_player.v.origin.z + sv_player.v.mins.z;
        stop.Z = start.Z - 34;

        trace_t trace = SV_Move(ref start, ref q_shared.ZeroVector, ref q_shared.ZeroVector, ref stop, 1, sv_player);
        float friction = sv_friction.value;
        if (trace.fraction == 1.0)
            friction *= edgefriction.value;

        // apply friction	
        float control = speed < sv_stopspeed.value ? sv_stopspeed.value : speed;
        float newspeed = (float)(speed - host_framtime * control * friction);

        if (newspeed < 0)
            newspeed = 0;
        newspeed /= speed;

        Mathlib.VectorScale(ref sv_player.v.velocity, newspeed, out sv_player.v.velocity);
    }
    public static void SV_Accelerate()
    {
        float currentspeed = Vector3.Dot(ToVector(ref sv_player.v.velocity), wishdir);
        float addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;

        float accelspeed = (float)(sv_accelerate.value * host_framtime * wishspeed);
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        sv_player.v.velocity.x += wishdir.X * accelspeed;
        sv_player.v.velocity.y += wishdir.Y * accelspeed;
        sv_player.v.velocity.z += wishdir.Z * accelspeed;
    }
    public static void SV_AirAccelerate(Vector3 wishveloc)
    {
        float wishspd = Mathlib.Normalize(ref wishveloc);
        if (wishspd > 30)
            wishspd = 30;
        float currentspeed = Vector3.Dot(ToVector(ref sv_player.v.velocity), wishveloc);
        float addspeed = wishspd - currentspeed;
        if (addspeed <= 0)
            return;
        float accelspeed = (float)(sv_accelerate.value * wishspeed * host_framtime);
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        wishveloc *= accelspeed;
        sv_player.v.velocity.x += wishveloc.X;
        sv_player.v.velocity.y += wishveloc.Y;
        sv_player.v.velocity.z += wishveloc.Z;
    }
}
