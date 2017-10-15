using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static void CL_Init()
    {
        CL_InitInput();
        CL_InitTEnts();
        
        cl_name = new cvar_t("_cl_name", "player", true);
        cl_color = new cvar_t("_cl_color", "0", true);
        cl_shownet = new cvar_t("cl_shownet", "0");	// can be 0, 1, or 2
        cl_nolerp = new cvar_t("cl_nolerp", "0");
        lookspring = new cvar_t("lookspring", "0", true);
        lookstrafe = new cvar_t("lookstrafe", "0", true);
        sensitivity = new cvar_t("sensitivity", "3", true);
        m_pitch = new cvar_t("m_pitch", "0.022", true);
        m_yaw = new cvar_t("m_yaw", "0.022", true);
        m_forward = new cvar_t("m_forward", "1", true);
        m_side = new cvar_t("m_side", "0.8", true);
        cl_upspeed = new cvar_t("cl_upspeed", "200");
        cl_forwardspeed = new cvar_t("cl_forwardspeed", "200", true);
        cl_backspeed = new cvar_t("cl_backspeed", "200", true);
        cl_sidespeed = new cvar_t("cl_sidespeed", "350");
        cl_movespeedkey = new cvar_t("cl_movespeedkey", "2.0");
        cl_yawspeed = new cvar_t("cl_yawspeed", "140");
        cl_pitchspeed = new cvar_t("cl_pitchspeed", "150");
        cl_anglespeedkey = new cvar_t("cl_anglespeedkey", "1.5");

        for (int i = 0; i < cl_efrags.Length; i++)
            cl_efrags[i] = new efrag_t();

        for (int i = 0; i < cl_entities.Length; i++)
            cl_entities[i] = new entity_t();

        for (int i = 0; i < cl_static_entities.Length; i++)
            cl_static_entities[i] = new entity_t();

        for (int i = 0; i < cl_dlights.Length; i++)
            cl_dlights[i] = new dlight_t();

        //
        // register our commands
        //
        Cmd_AddCommand("entities", CL_PrintEntities_f);
        Cmd_AddCommand("disconnect", CL_Disconnect_f);
        Cmd_AddCommand("record", CL_Record_f);
        Cmd_AddCommand("stop", CL_Stop_f);
        Cmd_AddCommand("playdemo", CL_PlayDemo_f);
        Cmd_AddCommand("timedemo", CL_TimeDemo_f);
    }
    public static void CL_EstablishConnection(string host)
    {
        if (cls.state == cactive_t.ca_dedicated)
            return;

        if (cls.demoplayback)
            return;

        CL_Disconnect();

        cls.netcon = NET_Connect(host);
        if (cls.netcon == null)
            Host_Error("CL_Connect: connect failed\n");

        Con_DPrintf("CL_EstablishConnection: connected to {0}\n", host);

        cls.demonum = -1;			// not in the demo loop now
        cls.state = cactive_t.ca_connected;
        cls.signon = 0;				// need all the signon messages before playing
    }
    public static void CL_NextDemo()
    {
        if (cls.demonum == -1)
            return;		// don't play demos

        SCR_BeginLoadingPlaque();

        if (String.IsNullOrEmpty(cls.demos[cls.demonum]) || cls.demonum == q_shared.MAX_DEMOS)
        {
            cls.demonum = 0;
            if (String.IsNullOrEmpty(cls.demos[cls.demonum]))
            {
                Con_Printf("No demos listed with startdemos\n");
                cls.demonum = -1;
                return;
            }
        }

        Cbuf_InsertText(String.Format("playdemo {0}\n", cls.demos[cls.demonum]));
        cls.demonum++;
    }
    public static dlight_t CL_AllocDlight(int key)
    {
        dlight_t dl;

        // first look for an exact key match
        if (key != 0)
        {
            for (int i = 0; i < q_shared.MAX_DLIGHTS; i++)
            {
                dl = cl_dlights[i];
                if (dl.key == key)
                {
                    dl.Clear();
                    dl.key = key;
                    return dl;
                }
            }
        }

        // then look for anything else
        //dl = cl_dlights;
        for (int i = 0; i < q_shared.MAX_DLIGHTS; i++)
        {
            dl = cl_dlights[i];
            if (dl.die < cl.time)
            {
                dl.Clear();
                dl.key = key;
                return dl;
            }
        }

        dl = cl_dlights[0];
        dl.Clear();
        dl.key = key;
        return dl;
    }
    public static void CL_DecayLights()
    {
        float time = (float)(cl.time - cl.oldtime);

        for (int i = 0; i < q_shared.MAX_DLIGHTS; i++)
        {
            dlight_t dl = cl_dlights[i];
            if (dl.die < cl.time || dl.radius == 0)
                continue;

            dl.radius -= time * dl.decay;
            if (dl.radius < 0)
                dl.radius = 0;
        }
    }
    public static void CL_PrintEntities_f()
    {
	    for (int i=0; i< cl.num_entities; i++)
	    {
            entity_t ent = cl_entities[i];
            Con_Printf("{0:d3}:", i);
		    if (ent.model == null)
		    {
			    Con_Printf("EMPTY\n");
			    continue;
		    }
		    Con_Printf("{0}:{1:d2}  ({2}) [{3}]\n", ent.model.name, ent.frame, ent.origin, ent.angles);
	    }
    }
    public static void CL_Disconnect_f()
    {
        CL_Disconnect();
        if (sv.active)
            Host_ShutdownServer(false);
    }
    public static void CL_SendCmd()
    {
        if (cls.state != cactive_t.ca_connected)
            return;

        if (cls.signon == q_shared.SIGNONS)
        {
            usercmd_t cmd = new usercmd_t();

            // get basic movement from keyboard
            CL_BaseMove(ref cmd);

            // allow mice or other external controllers to add to the move
            IN_Move(cmd);

            // send the unreliable message
            CL_SendMove(ref cmd);

        }

        if (cls.demoplayback)
        {
            cls.message.Clear();
            return;
        }

        // send the reliable message
        if (cls.message.IsEmpty)
            return;		// no message at all

        if (!NET_CanSendMessage(cls.netcon))
        {
            Con_DPrintf("CL_WriteToServer: can't send\n");
            return;
        }

        if (NET_SendMessage(cls.netcon, cls.message) == -1)
            Host_Error("CL_WriteToServer: lost server connection");

        cls.message.Clear();
    }
    public static int CL_ReadFromServer()
    {
        cl.oldtime = cl.time;
        cl.time += host_framtime;

        int ret;
        do
        {
            ret = CL_GetMessage();
            if (ret == -1)
                Host_Error("CL_ReadFromServer: lost server connection");
            if (ret == 0)
                break;

            cl.last_received_message = (float)realtime;
            CL_ParseServerMessage();
        } while (ret != 0 && cls.state == cactive_t.ca_connected);

        if (cl_shownet.value != 0)
            Con_Printf("\n");


        //
        // bring the links up to date
        //
        CL_RelinkEntities();
        CL_UpdateTEnts();

        return 0;
    }
    public static void CL_RelinkEntities()
    {
        // determine partial update time
        float frac = CL_LerpPoint();

        cl_numvisedicts = 0;

        //
        // interpolate player info
        //
        cl.velocity = cl.mvelocity[1] + frac * (cl.mvelocity[0] - cl.mvelocity[1]);

        if (cls.demoplayback)
        {
            // interpolate the angles
            Vector3 angleDelta = cl.mviewangles[0] - cl.mviewangles[1];
            Mathlib.CorrectAngles180(ref angleDelta);
            cl.viewangles = cl.mviewangles[1] + frac * angleDelta;
        }

        float bobjrotate = Mathlib.anglemod(100 * cl.time);
            
        // start on the entity after the world
        for (int i = 1; i < cl.num_entities; i++)
        {
            entity_t ent = cl_entities[i];
            if (ent.model == null)
            {
                // empty slot
                if (ent.forcelink)
                    R_RemoveEfrags(ent);	// just became empty
                continue;
            }

            // if the object wasn't included in the last packet, remove it
            if (ent.msgtime != cl.mtime[0])
            {
                ent.model = null;
                continue;
            }

            Vector3 oldorg = ent.origin;

            if (ent.forcelink)
            {	
                // the entity was not updated in the last message
                // so move to the final spot
                ent.origin = ent.msg_origins[0];
                ent.angles = ent.msg_angles[0];
            }
            else
            {
                // if the delta is large, assume a teleport and don't lerp
                float f = frac;
                Vector3 delta = ent.msg_origins[0] - ent.msg_origins[1];
                if (Math.Abs(delta.X) > 100 || Math.Abs(delta.Y) > 100 || Math.Abs(delta.Z) > 100)
                    f = 1; // assume a teleportation, not a motion

                // interpolate the origin and angles
                ent.origin = ent.msg_origins[1] + f * delta;
                Vector3 angleDelta = ent.msg_angles[0] - ent.msg_angles[1];
                Mathlib.CorrectAngles180(ref angleDelta);
                ent.angles = ent.msg_angles[1] + f * angleDelta;
            }

            // rotate binary objects locally
            if ((ent.model.flags & q_shared.EF_ROTATE) != 0)
                ent.angles.Y = bobjrotate;

            if ((ent.effects & q_shared.EF_BRIGHTFIELD) != 0)
                R_EntityParticles(ent);

            if ((ent.effects & q_shared.EF_MUZZLEFLASH) != 0)
            {
                dlight_t dl = CL_AllocDlight(i);
                dl.origin = ent.origin;
                dl.origin.Z += 16;
                Vector3 fv, rv, uv;
                Mathlib.AngleVectors(ref ent.angles, out fv, out rv, out uv);
                dl.origin += fv * 18;
                dl.radius = 200 + (Random() & 31);
                dl.minlight = 32;
                dl.die = (float)cl.time + 0.1f;
            }
            if ((ent.effects & q_shared.EF_BRIGHTLIGHT) != 0)
            {
                dlight_t dl = CL_AllocDlight(i);
                dl.origin = ent.origin;
                dl.origin.Z += 16;
                dl.radius = 400 + (Random() & 31);
                dl.die = (float)cl.time + 0.001f;
            }
            if ((ent.effects & q_shared.EF_DIMLIGHT) != 0)
            {
                dlight_t dl = CL_AllocDlight(i);
                dl.origin = ent.origin;
                dl.radius = 200 + (Random() & 31);
                dl.die = (float)cl.time + 0.001f;
            }

            if ((ent.model.flags & q_shared.EF_GIB) != 0)
                R_RocketTrail(ref oldorg, ref ent.origin, 2);
            else if ((ent.model.flags & q_shared.EF_ZOMGIB) != 0)
                R_RocketTrail(ref oldorg, ref ent.origin, 4);
            else if ((ent.model.flags & q_shared.EF_TRACER) != 0)
                R_RocketTrail(ref oldorg, ref ent.origin, 3);
            else if ((ent.model.flags & q_shared.EF_TRACER2) != 0)
                R_RocketTrail(ref oldorg, ref ent.origin, 5);
            else if ((ent.model.flags & q_shared.EF_ROCKET) != 0)
            {
                R_RocketTrail(ref oldorg, ref ent.origin, 0);
                dlight_t dl = CL_AllocDlight(i);
                dl.origin = ent.origin;
                dl.radius = 200;
                dl.die = (float)cl.time + 0.01f;
            }
            else if ((ent.model.flags & q_shared.EF_GRENADE) != 0)
                R_RocketTrail(ref oldorg, ref ent.origin, 1);
            else if ((ent.model.flags & q_shared.EF_TRACER3) != 0)
                R_RocketTrail(ref oldorg, ref ent.origin, 6);

            ent.forcelink = false;

            if (i == cl.viewentity && chase_active.value == 0)
                continue;

            if (cl_numvisedicts < q_shared.MAX_VISEDICTS)
            {
                cl_visedicts[cl_numvisedicts] = ent;
                cl_numvisedicts++;
            }
        }
    }
    public static void CL_SignonReply()
    {
        Con_DPrintf("CL_SignonReply: {0}\n", cls.signon);

        switch (cls.signon)
        {
            case 1:
                cls.message.MSG_WriteByte(q_shared.clc_stringcmd);
                cls.message.MSG_WriteString("prespawn");
                break;

            case 2:
                cls.message.MSG_WriteByte(q_shared.clc_stringcmd);
                cls.message.MSG_WriteString(String.Format("name \"{0}\"\n", cl_name.@string));

                cls.message.MSG_WriteByte(q_shared.clc_stringcmd);
                cls.message.MSG_WriteString(String.Format("color {0} {1}\n", ((int)cl_color.value) >> 4, ((int)cl_color.value) & 15));

                cls.message.MSG_WriteByte(q_shared.clc_stringcmd);
                cls.message.MSG_WriteString("spawn " + cls.spawnparms);
                break;

            case 3:
                cls.message.MSG_WriteByte(q_shared.clc_stringcmd);
                cls.message.MSG_WriteString("begin");
                Cache_Report();	// print remaining memory
                break;

            case 4:
                SCR_EndLoadingPlaque();		// allow normal screen updates
                break;
        }
    }
    public static void CL_ClearState()
    {
        if (!sv.active)
            Host_ClearMemory();

        // wipe the entire cl structure
        cl.Clear();

        cls.message.Clear();

        // clear other arrays
        foreach (efrag_t ef in cl_efrags)
            ef.Clear();
        foreach (entity_t et in cl_entities)
            et.Clear();

        foreach (dlight_t dl in cl_dlights)
            dl.Clear();
            
        Array.Clear(cl_lightstyle, 0, cl_lightstyle.Length);

        foreach (entity_t et in cl_temp_entities)
            et.Clear();

        foreach (beam_t b in cl_beams)
            b.Clear();

        //
        // allocate the efrags and chain together into a free list
        //
        cl.free_efrags = cl_efrags[0];// cl_efrags;
        for (int i = 0; i < q_shared.MAX_EFRAGS - 1; i++)
            cl_efrags[i].entnext = cl_efrags[i + 1];
        cl_efrags[q_shared.MAX_EFRAGS - 1].entnext = null;
    }
    public static void CL_Disconnect()
    {
        // stop sounds (especially looping!)
        S_StopAllSounds(true);

        // bring the console down and fade the colors back to normal
        //	SCR_BringDownConsole ();

        // if running a local server, shut it down
        if (cls.demoplayback)
            CL_StopPlayback();
        else if (cls.state == cactive_t.ca_connected)
        {
            if (cls.demorecording)
                CL_Stop_f();

            Con_DPrintf("Sending clc_disconnect\n");
            cls.message.Clear();
            cls.message.MSG_WriteByte(q_shared.clc_disconnect);
            NET_SendUnreliableMessage(cls.netcon, cls.message);
            cls.message.Clear();
            NET_Close(cls.netcon);

            cls.state = cactive_t.ca_disconnected;
            if (sv.active)
                Host_ShutdownServer(false);
        }

        cls.demoplayback = cls.timedemo = false;
        cls.signon = 0;
    }
    public static float CL_LerpPoint()
    {
        double f = cl.mtime[0] - cl.mtime[1];
        if (f == 0 || cl_nolerp.value != 0 || cls.timedemo || sv.active)
        {
            cl.time = cl.mtime[0];
            return 1;
        }

        if (f > 0.1)
        {	// dropped packet, or start of demo
            cl.mtime[1] = cl.mtime[0] - 0.1;
            f = 0.1;
        }
        double frac = (cl.time - cl.mtime[1]) / f;
        if (frac < 0)
        {
            if (frac < -0.01)
            {
                cl.time = cl.mtime[1];
            }
            frac = 0;
        }
        else if (frac > 1)
        {
            if (frac > 1.01)
            {
                cl.time = cl.mtime[0];
            }
            frac = 1;
        }
        return (float)frac;
    }
}