using System;
using OpenTK;

public static partial class game_engine
{
    public static cvar_t sv_friction;
    public static cvar_t edgefriction;
    public static cvar_t sv_stopspeed;
    public static cvar_t sv_gravity;
    public static cvar_t sv_maxvelocity;
    public static cvar_t sv_nostep;
    public static cvar_t sv_maxspeed;
    public static cvar_t sv_accelerate;
    public static cvar_t sv_aim;
    public static cvar_t sv_idealpitchscale;
    public static server_t sv = new server_t();
    public static server_static_t svs = new server_static_t();
    public static string[] localmodels = new string[q_shared.MAX_MODELS];
    public static int fatbytes;
    public static byte[] fatpvs = new byte[q_shared.MAX_MAP_LEAFS / 8];


    public static void SV_Init()
    {
        for (int i = 0; i < _BoxClipNodes.Length; i++)
            _BoxClipNodes[i].children = new short[2];
        for (int i = 0; i < _BoxPlanes.Length; i++)
            _BoxPlanes[i] = new mplane_t();
        for (int i = 0; i < sv_areanodes.Length; i++)
            sv_areanodes[i] = new areanode_t();

        sv_friction = new cvar_t("sv_friction", "4", false, true);
        edgefriction = new cvar_t("edgefriction", "2");
        sv_stopspeed = new cvar_t("sv_stopspeed", "100");
        sv_gravity = new cvar_t("sv_gravity", "800", false, true);
        sv_maxvelocity = new cvar_t("sv_maxvelocity", "2000");
        sv_nostep = new cvar_t("sv_nostep", "0");
        sv_maxspeed = new cvar_t("sv_maxspeed", "320", false, true);
        sv_accelerate = new cvar_t("sv_accelerate", "10");
        sv_aim = new cvar_t("sv_aim", "0.93");
        sv_idealpitchscale = new cvar_t("sv_idealpitchscale", "0.8");

        for (int i = 0; i < q_shared.MAX_MODELS; i++)
            localmodels[i] = "*" + i.ToString();
    }
    public static void SV_StartParticle(ref Vector3 org, ref Vector3 dir, int color, int count)
    {
        if (sv.datagram.Length > q_shared.MAX_DATAGRAM - 16)
            return;

        sv.datagram.MSG_WriteByte(q_shared.svc_particle);
        sv.datagram.MSG_WriteCoord(org.X);
        sv.datagram.MSG_WriteCoord(org.Y);
        sv.datagram.MSG_WriteCoord(org.Z);

        Vector3 max = Vector3.One * 127;
        Vector3 min = Vector3.One * -128;
        Vector3 v = Vector3.Clamp(dir * 16, min, max);
        sv.datagram.MSG_WriteChar((int)v.X);
        sv.datagram.MSG_WriteChar((int)v.Y);
        sv.datagram.MSG_WriteChar((int)v.Z);
        sv.datagram.MSG_WriteByte(count);
        sv.datagram.MSG_WriteByte(color);

    }
    public static void SV_StartSound(edict_t entity, int channel, string sample, int volume, float attenuation)
    {
        if (volume < 0 || volume > 255)
            Sys_Error("SV_StartSound: volume = {0}", volume);

        if (attenuation < 0 || attenuation > 4)
            Sys_Error("SV_StartSound: attenuation = {0}", attenuation);

        if (channel < 0 || channel > 7)
            Sys_Error("SV_StartSound: channel = {0}", channel);

        if (sv.datagram.Length > q_shared.MAX_DATAGRAM - 16)
            return;

        // find precache number for sound
        int sound_num;
        for (sound_num = 1; sound_num < q_shared.MAX_SOUNDS && sv.sound_precache[sound_num] != null; sound_num++)
            if (sample == sv.sound_precache[sound_num])
                break;

        if (sound_num == q_shared.MAX_SOUNDS || String.IsNullOrEmpty(sv.sound_precache[sound_num]))
        {
            Con_Printf("SV_StartSound: {0} not precacheed\n", sample);
            return;
        }

        int ent = NUM_FOR_EDICT(entity);

        channel = (ent << 3) | channel;

        int field_mask = 0;
        if (volume != q_shared.DEFAULT_SOUND_PACKET_VOLUME)
            field_mask |= q_shared.SND_VOLUME;
        if (attenuation != q_shared.DEFAULT_SOUND_PACKET_ATTENUATION)
            field_mask |= q_shared.SND_ATTENUATION;

        // directed messages go only to the entity the are targeted on
        sv.datagram.MSG_WriteByte(q_shared.svc_sound);
        sv.datagram.MSG_WriteByte(field_mask);
        if ((field_mask & q_shared.SND_VOLUME) != 0)
            sv.datagram.MSG_WriteByte(volume);
        if ((field_mask & q_shared.SND_ATTENUATION) != 0)
            sv.datagram.MSG_WriteByte((int)(attenuation * 64));
        sv.datagram.MSG_WriteShort(channel);
        sv.datagram.MSG_WriteByte(sound_num);
        v3f v;
        Mathlib.VectorAdd(ref entity.v.mins, ref entity.v.maxs, out v);
        Mathlib.VectorMA(ref entity.v.origin, 0.5f, ref v, out v);
        sv.datagram.MSG_WriteCoord(v.x);
        sv.datagram.MSG_WriteCoord(v.y);
        sv.datagram.MSG_WriteCoord(v.z);
    }
    public static void SV_DropClient(bool crash)
    {
        client_t client = host_client;

        if (!crash)
        {
            // send any final messages (don't check for errors)
            if (NET_CanSendMessage(client.netconnection))
            {
                MsgWriter msg = client.message;
                msg.MSG_WriteByte(q_shared.svc_disconnect);
                NET_SendMessage(client.netconnection, msg);
            }

            if (client.edict != null && client.spawned)
            {
                // call the prog function for removing a client
                // this will set the body to a dead frame, among other things
                int saveSelf = pr_global_struct.self;
                pr_global_struct.self = EDICT_TO_PROG(client.edict);
                PR_ExecuteProgram(pr_global_struct.ClientDisconnect);
                pr_global_struct.self = saveSelf;
            }

            Con_DPrintf("Client {0} removed\n", client.name);
        }

        // break the net connection
        NET_Close(client.netconnection);
        client.netconnection = null;

        // free the client (the body stays around)
        client.active = false;
        client.name = null;
        client.old_frags = -999999;
        net_activeconnections--;

        // send notification to all clients
        for (int i = 0; i < svs.maxclients; i++)
        {
            client_t cl = svs.clients[i];
            if (!cl.active)
                continue;

            cl.message.MSG_WriteByte(q_shared.svc_updatename);
            cl.message.MSG_WriteByte(ClientNum);
            cl.message.MSG_WriteString("");
            cl.message.MSG_WriteByte(q_shared.svc_updatefrags);
            cl.message.MSG_WriteByte(ClientNum);
            cl.message.MSG_WriteShort(0);
            cl.message.MSG_WriteByte(q_shared.svc_updatecolors);
            cl.message.MSG_WriteByte(ClientNum);
            cl.message.MSG_WriteByte(0);
        }
    }
    public static void SV_SendClientMessages()
    {
        // update frags, names, etc
        SV_UpdateToReliableMessages();

        // build individual updates
        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];

            if (!host_client.active)
                continue;

            if (host_client.spawned)
            {
                if (!SV_SendClientDatagram(host_client))
                    continue;
            }
            else
            {
                // the player isn't totally in the game yet
                // send small keepalive messages if too much time has passed
                // send a full message when the next signon stage has been requested
                // some other message data (name changes, etc) may accumulate 
                // between signon stages
                if (!host_client.sendsignon)
                {
                    if (realtime - host_client.last_message > 5)
                        SV_SendNop(host_client);
                    continue;	// don't send out non-signon messages
                }
            }

            // check for an overflowed message.  Should only happen
            // on a very fucked up connection that backs up a lot, then
            // changes level
            if (host_client.message.IsOveflowed)
            {
                SV_DropClient(true);
                host_client.message.IsOveflowed = false;
                continue;
            }

            if (host_client.message.Length > 0 || host_client.dropasap)
            {
                if (!NET_CanSendMessage(host_client.netconnection))
                    continue;

                if (host_client.dropasap)
                    SV_DropClient(false);	// went to another level
                else
                {
                    if (NET_SendMessage(host_client.netconnection, host_client.message) == -1)
                        SV_DropClient(true);	// if the message couldn't send, kick off
                    host_client.message.Clear();
                    host_client.last_message = realtime;
                    host_client.sendsignon = false;
                }
            }
        }

        // clear muzzle flashes
        SV_CleanupEnts();
    }
    public static void SV_CleanupEnts()
    {
        for (int i = 1; i < sv.num_edicts; i++)
        {
            edict_t ent = sv.edicts[i];
            ent.v.effects = (int)ent.v.effects & ~q_shared.EF_MUZZLEFLASH;
        }
    }
    public static void SV_SendNop(client_t client)
    {
        MsgWriter msg = new MsgWriter(4);
        msg.MSG_WriteChar(q_shared.svc_nop);

        if (NET_SendUnreliableMessage(client.netconnection, msg) == -1)
            SV_DropClient(true);	// if the message couldn't send, kick off
        client.last_message = realtime;
    }
    public static bool SV_SendClientDatagram(client_t client)
    {
        MsgWriter msg = new MsgWriter(q_shared.MAX_DATAGRAM); // Uze todo: make static?

        msg.MSG_WriteByte(q_shared.svc_time);
        msg.MSG_WriteFloat((float)sv.time);

        // add the client specific data to the datagram
        SV_WriteClientdataToMessage(client.edict, msg);

        SV_WriteEntitiesToClient(client.edict, msg);

        // copy the server datagram if there is space
        if (msg.Length + sv.datagram.Length < msg.Capacity)
            msg.Write(sv.datagram.Data, 0, sv.datagram.Length);

        // send the datagram
        if (NET_SendUnreliableMessage(client.netconnection, msg) == -1)
        {
            SV_DropClient(true);// if the message couldn't send, kick off
            return false;
        }

        return true;
    }
    public static void SV_WriteEntitiesToClient(edict_t clent, MsgWriter msg)
    {
        // find the client's PVS
        Vector3 org = ToVector(ref clent.v.origin) + ToVector(ref clent.v.view_ofs);
        byte[] pvs = SV_FatPVS(ref org);

        // send over all entities (except the client) that touch the pvs
        for (int e = 1; e < sv.num_edicts; e++)
        {
            edict_t ent = sv.edicts[e];
            // ignore if not touching a PV leaf
            if (ent != clent)	// clent is ALLWAYS sent
            {
                // ignore ents without visible models
                string mname = GetString(ent.v.model);
                if (String.IsNullOrEmpty(mname))
                    continue;

                int i;
                for (i = 0; i < ent.num_leafs; i++)
                    if ((pvs[ent.leafnums[i] >> 3] & (1 << (ent.leafnums[i] & 7))) != 0)
                        break;

                if (i == ent.num_leafs)
                    continue;		// not visible
            }

            if (msg.Capacity - msg.Length < 16)
            {
                Con_Printf("packet overflow\n");
                return;
            }

            // send an update
            int bits = 0;
            v3f miss;
            Mathlib.VectorSubtract(ref ent.v.origin, ref ent.baseline.origin, out miss);
            if (miss.x < -0.1f || miss.x > 0.1f) bits |= q_shared.U_ORIGIN1;
            if (miss.y < -0.1f || miss.y > 0.1f) bits |= q_shared.U_ORIGIN2;
            if (miss.z < -0.1f || miss.z > 0.1f) bits |= q_shared.U_ORIGIN3;

            if (ent.v.angles.x != ent.baseline.angles.x)
                bits |= q_shared.U_ANGLE1;

            if (ent.v.angles.y != ent.baseline.angles.y)
                bits |= q_shared.U_ANGLE2;

            if (ent.v.angles.z != ent.baseline.angles.z)
                bits |= q_shared.U_ANGLE3;

            if (ent.v.movetype == q_shared.MOVETYPE_STEP)
                bits |= q_shared.U_NOLERP;	// don't mess up the step animation

            if (ent.baseline.colormap != ent.v.colormap)
                bits |= q_shared.U_COLORMAP;

            if (ent.baseline.skin != ent.v.skin)
                bits |= q_shared.U_SKIN;

            if (ent.baseline.frame != ent.v.frame)
                bits |= q_shared.U_FRAME;

            if (ent.baseline.effects != ent.v.effects)
                bits |= q_shared.U_EFFECTS;

            if (ent.baseline.modelindex != ent.v.modelindex)
                bits |= q_shared.U_MODEL;

            if (e >= 256)
                bits |= q_shared.U_LONGENTITY;

            if (bits >= 256)
                bits |= q_shared.U_MOREBITS;

            //
            // write the message
            //
            msg.MSG_WriteByte(bits | q_shared.U_SIGNAL);

            if ((bits & q_shared.U_MOREBITS) != 0)
                msg.MSG_WriteByte(bits >> 8);
            if ((bits & q_shared.U_LONGENTITY) != 0)
                msg.MSG_WriteShort(e);
            else
                msg.MSG_WriteByte(e);

            if ((bits & q_shared.U_MODEL) != 0)
                msg.MSG_WriteByte((int)ent.v.modelindex);
            if ((bits & q_shared.U_FRAME) != 0)
                msg.MSG_WriteByte((int)ent.v.frame);
            if ((bits & q_shared.U_COLORMAP) != 0)
                msg.MSG_WriteByte((int)ent.v.colormap);
            if ((bits & q_shared.U_SKIN) != 0)
                msg.MSG_WriteByte((int)ent.v.skin);
            if ((bits & q_shared.U_EFFECTS) != 0)
                msg.MSG_WriteByte((int)ent.v.effects);
            if ((bits & q_shared.U_ORIGIN1) != 0)
                msg.MSG_WriteCoord(ent.v.origin.x);
            if ((bits & q_shared.U_ANGLE1) != 0)
                msg.MSG_WriteAngle(ent.v.angles.x);
            if ((bits & q_shared.U_ORIGIN2) != 0)
                msg.MSG_WriteCoord(ent.v.origin.y);
            if ((bits & q_shared.U_ANGLE2) != 0)
                msg.MSG_WriteAngle(ent.v.angles.y);
            if ((bits & q_shared.U_ORIGIN3) != 0)
                msg.MSG_WriteCoord(ent.v.origin.z);
            if ((bits & q_shared.U_ANGLE3) != 0)
                msg.MSG_WriteAngle(ent.v.angles.z);
        }
    }
    public static byte[] SV_FatPVS(ref Vector3 org)
    {
        fatbytes = (sv.worldmodel.numleafs + 31) >> 3;
        Array.Clear(fatpvs, 0, fatpvs.Length);
        SV_AddToFatPVS(ref org, sv.worldmodel.nodes[0]);
        return fatpvs;
    }
    public static void SV_AddToFatPVS(ref Vector3 org, mnodebase_t node)
    {
        while (true)
        {
            // if this is a leaf, accumulate the pvs bits
            if (node.contents < 0)
            {
                if (node.contents != q_shared.CONTENTS_SOLID)
                {
                    byte[] pvs = Mod_LeafPVS((mleaf_t)node, sv.worldmodel);
                    for (int i = 0; i < fatbytes; i++)
                        fatpvs[i] |= pvs[i];
                }
                return;
            }

            mnode_t n = (mnode_t)node;
            mplane_t plane = n.plane;
            float d = Vector3.Dot(org, plane.normal) - plane.dist;
            if (d > 8)
                node = n.children[0];
            else if (d < -8)
                node = n.children[1];
            else
            {	// go down both
                SV_AddToFatPVS(ref org, n.children[0]);
                node = n.children[1];
            }
        }
    }
    public static void SV_UpdateToReliableMessages()
    {
        // check for changes to be sent over the reliable streams
        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];
            if (host_client.old_frags != host_client.edict.v.frags)
            {
                for (int j = 0; j < svs.maxclients; j++)
                {
                    client_t client = svs.clients[j];
                    if (!client.active)
                        continue;

                    client.message.MSG_WriteByte(q_shared.svc_updatefrags);
                    client.message.MSG_WriteByte(i);
                    client.message.MSG_WriteShort((int)host_client.edict.v.frags);
                }

                host_client.old_frags = (int)host_client.edict.v.frags;
            }
        }

        for (int j = 0; j < svs.maxclients; j++)
        {
            client_t client = svs.clients[j];
            if (!client.active)
                continue;
            client.message.Write(sv.reliable_datagram.Data, 0, sv.reliable_datagram.Length);
        }

        sv.reliable_datagram.Clear();
    }
    public static void SV_ClearDatagram()
    {
        sv.datagram.Clear();
    }
    public static int SV_ModelIndex(string name)
    {
        if (String.IsNullOrEmpty(name))
            return 0;

        int i;
        for (i = 0; i < q_shared.MAX_MODELS && sv.model_precache[i] != null; i++)
            if (sv.model_precache[i] == name)
                return i;

        if (i == q_shared.MAX_MODELS || String.IsNullOrEmpty(sv.model_precache[i]))
            Sys_Error("SV_ModelIndex: model {0} not precached", name);
        return i;
    }
    public static void SV_ClientPrintf(string fmt, params object[] args)
    {
        string tmp = String.Format(fmt, args);
        host_client.message.MSG_WriteByte(q_shared.svc_print);
        host_client.message.MSG_WriteString(tmp);
    }
    public static void SV_BroadcastPrint(string fmt, params object[] args)
    {
        string tmp = args.Length > 0 ? String.Format(fmt, args) : fmt;
        for (int i = 0; i < svs.maxclients; i++)
            if (svs.clients[i].active && svs.clients[i].spawned)
            {
                MsgWriter msg = svs.clients[i].message;
                msg.MSG_WriteByte(q_shared.svc_print);
                msg.MSG_WriteString(tmp);
            }
    }
    public static void SV_WriteClientdataToMessage(edict_t ent, MsgWriter msg)
    {
        //
        // send a damage message
        //
        if (ent.v.dmg_take != 0 || ent.v.dmg_save != 0)
        {
            edict_t other = PROG_TO_EDICT(ent.v.dmg_inflictor);
            msg.MSG_WriteByte(q_shared.svc_damage);
            msg.MSG_WriteByte((int)ent.v.dmg_save);
            msg.MSG_WriteByte((int)ent.v.dmg_take);
            msg.MSG_WriteCoord(other.v.origin.x + 0.5f * (other.v.mins.x + other.v.maxs.x));
            msg.MSG_WriteCoord(other.v.origin.y + 0.5f * (other.v.mins.y + other.v.maxs.y));
            msg.MSG_WriteCoord(other.v.origin.z + 0.5f * (other.v.mins.z + other.v.maxs.z));

            ent.v.dmg_take = 0;
            ent.v.dmg_save = 0;
        }

        //
        // send the current viewpos offset from the view entity
        //
        SV_SetIdealPitch();		// how much to look up / down ideally

        // a fixangle might get lost in a dropped packet.  Oh well.
        if (ent.v.fixangle != 0)
        {
            msg.MSG_WriteByte(q_shared.svc_setangle);
            msg.MSG_WriteAngle(ent.v.angles.x);
            msg.MSG_WriteAngle(ent.v.angles.y);
            msg.MSG_WriteAngle(ent.v.angles.z);
            ent.v.fixangle = 0;
        }

        int bits = 0;

        if (ent.v.view_ofs.z != q_shared.DEFAULT_VIEWHEIGHT)
            bits |= q_shared.SU_VIEWHEIGHT;

        if (ent.v.idealpitch != 0)
            bits |= q_shared.SU_IDEALPITCH;

        // stuff the sigil bits into the high bits of items for sbar, or else
        // mix in items2
        float val = GetEdictFieldFloat(ent, "items2", 0);
        int items;
        if (val != 0)
            items = (int)ent.v.items | ((int)val << 23);
        else
            items = (int)ent.v.items | ((int)pr_global_struct.serverflags << 28);

        bits |= q_shared.SU_ITEMS;

        if (((int)ent.v.flags & q_shared.FL_ONGROUND) != 0)
            bits |= q_shared.SU_ONGROUND;

        if (ent.v.waterlevel >= 2)
            bits |= q_shared.SU_INWATER;

        if (ent.v.punchangle.x != 0) bits |= q_shared.SU_PUNCH1;
        if (ent.v.punchangle.y != 0) bits |= q_shared.SU_PUNCH2;
        if (ent.v.punchangle.z != 0) bits |= q_shared.SU_PUNCH3;

        if (ent.v.velocity.x != 0) bits |= q_shared.SU_VELOCITY1;
        if (ent.v.velocity.y != 0) bits |= q_shared.SU_VELOCITY2;
        if (ent.v.velocity.z != 0) bits |= q_shared.SU_VELOCITY3;

        if (ent.v.weaponframe != 0)
            bits |= q_shared.SU_WEAPONFRAME;

        if (ent.v.armorvalue != 0)
            bits |= q_shared.SU_ARMOR;

        //	if (ent.v.weapon)
        bits |= q_shared.SU_WEAPON;

        // send the data

        msg.MSG_WriteByte(q_shared.svc_clientdata);
        msg.MSG_WriteShort(bits);

        if ((bits & q_shared.SU_VIEWHEIGHT) != 0)
            msg.MSG_WriteChar((int)ent.v.view_ofs.z);

        if ((bits & q_shared.SU_IDEALPITCH) != 0)
            msg.MSG_WriteChar((int)ent.v.idealpitch);

        if ((bits & q_shared.SU_PUNCH1) != 0) msg.MSG_WriteChar((int)ent.v.punchangle.x);
        if ((bits & q_shared.SU_VELOCITY1) != 0) msg.MSG_WriteChar((int)(ent.v.velocity.x / 16));

        if ((bits & q_shared.SU_PUNCH2) != 0) msg.MSG_WriteChar((int)ent.v.punchangle.y);
        if ((bits & q_shared.SU_VELOCITY2) != 0) msg.MSG_WriteChar((int)(ent.v.velocity.y / 16));

        if ((bits & q_shared.SU_PUNCH3) != 0) msg.MSG_WriteChar((int)ent.v.punchangle.z);
        if ((bits & q_shared.SU_VELOCITY3) != 0) msg.MSG_WriteChar((int)(ent.v.velocity.z / 16));

        // always sent
        msg.MSG_WriteLong(items);

        if ((bits & q_shared.SU_WEAPONFRAME) != 0)
            msg.MSG_WriteByte((int)ent.v.weaponframe);
        if ((bits & q_shared.SU_ARMOR) != 0)
            msg.MSG_WriteByte((int)ent.v.armorvalue);
        if ((bits & q_shared.SU_WEAPON) != 0)
            msg.MSG_WriteByte(SV_ModelIndex(GetString(ent.v.weaponmodel)));

        msg.MSG_WriteShort((int)ent.v.health);
        msg.MSG_WriteByte((int)ent.v.currentammo);
        msg.MSG_WriteByte((int)ent.v.ammo_shells);
        msg.MSG_WriteByte((int)ent.v.ammo_nails);
        msg.MSG_WriteByte((int)ent.v.ammo_rockets);
        msg.MSG_WriteByte((int)ent.v.ammo_cells);

        if (_GameKind == GameKind.StandardQuake)
        {
            msg.MSG_WriteByte((int)ent.v.weapon);
        }
        else
        {
            for (int i = 0; i < 32; i++)
            {
                if ((((int)ent.v.weapon) & (1 << i)) != 0)
                {
                    msg.MSG_WriteByte(i);
                    break;
                }
            }
        }
    }
    public static void SV_CheckForNewClients()
    {
        //
        // check for new connections
        //
        while (true)
        {
            qsocket_t ret = NET_CheckNewConnections();
            if (ret == null)
                break;

            // 
            // init a new client structure
            //	
            int i;
            for (i = 0; i < svs.maxclients; i++)
                if (!svs.clients[i].active)
                    break;
            if (i == svs.maxclients)
                Sys_Error("Host_CheckForNewClients: no free clients");

            svs.clients[i].netconnection = ret;
            SV_ConnectClient(i);

            net_activeconnections++;
        }
    }
    public static void SV_ConnectClient(int clientnum)
    {
        client_t client = svs.clients[clientnum];

        Con_DPrintf("Client {0} connected\n", client.netconnection.address);

        int edictnum = clientnum + 1;
        edict_t ent = EDICT_NUM(edictnum);

        // set up the client_t
        qsocket_t netconnection = client.netconnection;

        float[] spawn_parms = new float[q_shared.NUM_SPAWN_PARMS];
        if (sv.loadgame)
        {
            Array.Copy(client.spawn_parms, spawn_parms, spawn_parms.Length);
        }

        client.Clear();
        client.netconnection = netconnection;
        client.name = "unconnected";
        client.active = true;
        client.spawned = false;
        client.edict = ent;
        client.message.AllowOverflow = true; // we can catch it
        client.privileged = false;

        if (sv.loadgame)
        {
            Array.Copy(spawn_parms, client.spawn_parms, spawn_parms.Length);
        }
        else
        {
            // call the progs to get default spawn parms for the new client
            PR_ExecuteProgram(pr_global_struct.SetNewParms);

            AssignGlobalSpawnparams(client);
        }

        SV_SendServerinfo(client);
    }
    public static void AssignGlobalSpawnparams(client_t client)
    {
        client.spawn_parms[0] = pr_global_struct.parm1;
        client.spawn_parms[1] = pr_global_struct.parm2;
        client.spawn_parms[2] = pr_global_struct.parm3;
        client.spawn_parms[3] = pr_global_struct.parm4;

        client.spawn_parms[4] = pr_global_struct.parm5;
        client.spawn_parms[5] = pr_global_struct.parm6;
        client.spawn_parms[6] = pr_global_struct.parm7;
        client.spawn_parms[7] = pr_global_struct.parm8;

        client.spawn_parms[8] = pr_global_struct.parm9;
        client.spawn_parms[9] = pr_global_struct.parm10;
        client.spawn_parms[10] = pr_global_struct.parm11;
        client.spawn_parms[11] = pr_global_struct.parm12;

        client.spawn_parms[12] = pr_global_struct.parm13;
        client.spawn_parms[13] = pr_global_struct.parm14;
        client.spawn_parms[14] = pr_global_struct.parm15;
        client.spawn_parms[15] = pr_global_struct.parm16;
    }
    public static void SV_SaveSpawnparms()
    {
        svs.serverflags = (int)pr_global_struct.serverflags;

        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];
            if (!host_client.active)
                continue;

            // call the progs to get default spawn parms for the new client
            pr_global_struct.self = EDICT_TO_PROG(host_client.edict);
            PR_ExecuteProgram(pr_global_struct.SetChangeParms);
            AssignGlobalSpawnparams(host_client);
        }
    }
    public static void SV_SpawnServer(string server)
    {
        // let's not have any servers with no name
        if (String.IsNullOrEmpty(hostname.@string))
            Cvar.Cvar_Set("hostname", "UNNAMED");

        scr_centertime_off = 0;

        Con_DPrintf("SpawnServer: {0}\n", server);
        svs.changelevel_issued = false;		// now safe to issue another

        //
        // tell all connected clients that we are going to a new level
        //
        if (sv.active)
        {
            SV_SendReconnect();
        }

        //
        // make cvars consistant
        //
        if (coop.value != 0)
            Cvar.Cvar_SetValue("deathmatch", 0);

        current_skill = (int)(skill.value + 0.5);
        if (current_skill < 0)
            current_skill = 0;
        if (current_skill > 3)
            current_skill = 3;

        Cvar.Cvar_SetValue("skill", (float)current_skill);

        //
        // set up the new server
        //
        Host_ClearMemory();

        sv.Clear();

        sv.name = server;

        // load progs to get entity field count
        PR_LoadProgs();

        // allocate server memory
        sv.max_edicts = q_shared.MAX_EDICTS;

        sv.edicts = new edict_t[sv.max_edicts];
        for (int i = 0; i < sv.edicts.Length; i++)
        {
            sv.edicts[i] = new edict_t();
        }

        // leave slots at start for clients only
        sv.num_edicts = svs.maxclients + 1;
        edict_t ent;
        for (int i = 0; i < svs.maxclients; i++)
        {
            ent = EDICT_NUM(i + 1);
            svs.clients[i].edict = ent;
        }

        sv.state = server_state_t.Loading;
        sv.paused = false;
        sv.time = 1.0;
        sv.modelname = String.Format("maps/{0}.bsp", server);
        sv.worldmodel = Mod_ForName(sv.modelname, false);
        if (sv.worldmodel == null)
        {
            Con_Printf("Couldn't spawn server {0}\n", sv.modelname);
            sv.active = false;
            return;
        }
        sv.models[1] = sv.worldmodel;

        //
        // clear world interaction links
        //
        SV_ClearWorld();

        sv.sound_precache[0] = String.Empty;
        sv.model_precache[0] = String.Empty;

        sv.model_precache[1] = sv.modelname;
        for (int i = 1; i < sv.worldmodel.numsubmodels; i++)
        {
            sv.model_precache[1 + i] = localmodels[i];
            sv.models[i + 1] = Mod_ForName(localmodels[i], false);
        }

        //
        // load the rest of the entities
        //	
        ent = EDICT_NUM(0);
        ent.Clear();
        ent.v.model = StringOffset(sv.worldmodel.name);
        if (ent.v.model == -1)
        {
            ent.v.model = ED_NewString(sv.worldmodel.name);
        }
        ent.v.modelindex = 1;		// world model
        ent.v.solid = q_shared.SOLID_BSP;
        ent.v.movetype = q_shared.MOVETYPE_PUSH;

        if (coop.value != 0)
            pr_global_struct.coop = 1; //coop.value;
        else
            pr_global_struct.deathmatch = deathmatch.value;

        int offset = ED_NewString(sv.name);
        pr_global_struct.mapname = offset;

        // serverflags are for cross level information (sigils)
        pr_global_struct.serverflags = svs.serverflags;

        ED_LoadFromFile(sv.worldmodel.entities);

        sv.active = true;

        // all setup is completed, any further precache statements are errors
        sv.state = server_state_t.Active;

        // run two frames to allow everything to settle
        host_framtime = 0.1;
        SV_Physics();
        SV_Physics();

        // create a baseline for more efficient communications
        SV_CreateBaseline();

        // send serverinfo to all connected clients
        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];
            if (host_client.active)
                SV_SendServerinfo(host_client);
        }

        GC.Collect();
        Con_DPrintf("Server spawned.\n");
    }
    public static void SV_SendServerinfo(client_t client)
    {
        MsgWriter writer = client.message;

        writer.MSG_WriteByte(q_shared.svc_print);
        writer.MSG_WriteString(String.Format("{0}\nVERSION {1,4:F2} SERVER ({2} CRC)", (char)2, q_shared.VERSION, pr_crc));

        writer.MSG_WriteByte(q_shared.svc_serverinfo);
        writer.MSG_WriteLong(q_shared.PROTOCOL_VERSION);
        writer.MSG_WriteByte(svs.maxclients);

        if (!(coop.value != 0) && deathmatch.value != 0)
            writer.MSG_WriteByte(q_shared.GAME_DEATHMATCH);
        else
            writer.MSG_WriteByte(q_shared.GAME_COOP);

        string message = GetString(sv.edicts[0].v.message);

        writer.MSG_WriteString(message);

        for (int i = 1; i < sv.model_precache.Length; i++)
        {
            string tmp = sv.model_precache[i];
            if (String.IsNullOrEmpty(tmp))
                break;
            writer.MSG_WriteString(tmp);
        }
        writer.MSG_WriteByte(0);

        for (int i = 1; i < sv.sound_precache.Length; i++)
        {
            string tmp = sv.sound_precache[i];
            if (tmp == null)
                break;
            writer.MSG_WriteString(tmp);
        }
        writer.MSG_WriteByte(0);

        // send music
        writer.MSG_WriteByte(q_shared.svc_cdtrack);
        writer.MSG_WriteByte((int)sv.edicts[0].v.sounds);
        writer.MSG_WriteByte((int)sv.edicts[0].v.sounds);

        // set view	
        writer.MSG_WriteByte(q_shared.svc_setview);
        writer.MSG_WriteShort(NUM_FOR_EDICT(client.edict));

        writer.MSG_WriteByte(q_shared.svc_signonnum);
        writer.MSG_WriteByte(1);

        client.sendsignon = true;
        client.spawned = false;		// need prespawn, spawn, etc
    }
    public static void SV_SendReconnect()
    {
        MsgWriter msg = new MsgWriter(128);

        msg.MSG_WriteChar(q_shared.svc_stufftext);
        msg.MSG_WriteString("reconnect\n");
        NET_SendToAll(msg, 5);

        if (cls.state != cactive_t.ca_dedicated)
            Cmd_ExecuteString("reconnect\n", cmd_source_t.src_command);
    }
    public static void SV_CreateBaseline()
    {
        for (int entnum = 0; entnum < sv.num_edicts; entnum++)
        {
            // get the current server version
            edict_t svent = EDICT_NUM(entnum);
            if (svent.free)
                continue;
            if (entnum > svs.maxclients && svent.v.modelindex == 0)
                continue;

            //
            // create entity baseline
            //
            svent.baseline.origin = svent.v.origin;
            svent.baseline.angles = svent.v.angles;
            svent.baseline.frame = (int)svent.v.frame;
            svent.baseline.skin = (int)svent.v.skin;
            if (entnum > 0 && entnum <= svs.maxclients)
            {
                svent.baseline.colormap = entnum;
                svent.baseline.modelindex = SV_ModelIndex("progs/player.mdl");
            }
            else
            {
                svent.baseline.colormap = 0;
                svent.baseline.modelindex = SV_ModelIndex(GetString(svent.v.model));
            }

            //
            // add to the message
            //
            sv.signon.MSG_WriteByte(q_shared.svc_spawnbaseline);
            sv.signon.MSG_WriteShort(entnum);

            sv.signon.MSG_WriteByte(svent.baseline.modelindex);
            sv.signon.MSG_WriteByte(svent.baseline.frame);
            sv.signon.MSG_WriteByte(svent.baseline.colormap);
            sv.signon.MSG_WriteByte(svent.baseline.skin);

            sv.signon.MSG_WriteCoord(svent.baseline.origin.x);
            sv.signon.MSG_WriteAngle(svent.baseline.angles.x);
            sv.signon.MSG_WriteCoord(svent.baseline.origin.y);
            sv.signon.MSG_WriteAngle(svent.baseline.angles.y);
            sv.signon.MSG_WriteCoord(svent.baseline.origin.z);
            sv.signon.MSG_WriteAngle(svent.baseline.angles.z);
        }
    }

    public static edict_t EDICT_NUM(int n)
    {
        if (n < 0 || n >= sv.max_edicts)
            Sys_Error("EDICT_NUM: bad number {0}", n);
        return sv.edicts[n];
    }
    public static edict_t ED_Alloc()
    {
        edict_t e;
        int i;
        for (i = svs.maxclients + 1; i < sv.num_edicts; i++)
        {
            e = EDICT_NUM(i);

            // the first couple seconds of server time can involve a lot of
            // freeing and allocating, so relax the replacement policy
            if (e.free && (e.freetime < 2 || sv.time - e.freetime > 0.5))
            {
                e.Clear();
                return e;
            }
        }

        if (i == q_shared.MAX_EDICTS)
            Sys_Error("ED_Alloc: no free edicts");

        sv.num_edicts++;
        e = EDICT_NUM(i);
        e.Clear();

        return e;
    }
    public static void ED_Free(edict_t ed)
    {
        SV_UnlinkEdict(ed);		// unlink from world bsp

        ed.free = true;
        ed.v.model = 0;
        ed.v.takedamage = 0;
        ed.v.modelindex = 0;
        ed.v.colormap = 0;
        ed.v.skin = 0;
        ed.v.frame = 0;
        ed.v.origin = default(v3f);
        ed.v.angles = default(v3f);
        ed.v.nextthink = -1;
        ed.v.solid = 0;

        ed.freetime = (float)sv.time;
    }
    public static int EDICT_TO_PROG(edict_t e)
    {
        return Array.IndexOf(sv.edicts, e); // todo: optimize this
    }
    public static edict_t PROG_TO_EDICT(int e)
    {
        if (e < 0 || e > sv.edicts.Length)
            Sys_Error("ProgToEdict: Bad prog!");
        return sv.edicts[e];
    }
    public static int NUM_FOR_EDICT(edict_t e)
    {
        int i = Array.IndexOf(sv.edicts, e); // todo: optimize this

        if (i < 0)
            Sys_Error("NUM_FOR_EDICT: bad pointer");
        return i;
    }
}