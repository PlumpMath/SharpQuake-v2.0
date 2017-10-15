using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Runtime.InteropServices;

public static partial class game_engine
{
    public static readonly PollProcedure _SlistSendProcedure = new PollProcedure(null, 0.0, Slist_Send, null);
    public static readonly PollProcedure _SlistPollProcedure = new PollProcedure(null, 0.0, Slist_Poll, null);

    public static INetDriver[] net_drivers;
    public static INetLanDriver[] net_landrivers;
    public static bool recording;
    public static int DEFAULTnet_hostport = 26000;
    public static int net_hostport;
    public static bool listening;
    public static List<qsocket_t> net_freeSockets;
    public static List<qsocket_t> net_activeSockets;
    public static int net_activeconnections;
    public static double net_time;

    public static MsgWriter Message; // sizebuf_t net_message
    public static MsgReader Reader; // reads from net_message

    public static string my_tcpip_address;
    public static int _MessagesSent = 0;
    public static int _MessagesReceived = 0;
    public static int _UnreliableMessagesSent = 0;
    public static int _UnreliableMessagesReceived = 0;

    public static cvar_t net_messagetimeout;
    public static cvar_t hostname;

    public static PollProcedure pollProcedureList;

    public static hostcache_t[] hostcache = new hostcache_t[q_shared.HOSTCACHESIZE];
    public static int hostCacheCount;

    public static bool slistInProgress;
    public static bool SlistSilent;
    public static bool SlistLocal = true;
    public static int slistLastShown;
    public static double slistStartTime;

    public static int net_driverlevel;
    public static int net_landriverlevel;

    public static VcrRecord vcrConnect = new VcrRecord();
    public static VcrRecord2 vcrGetMessage = new VcrRecord2();
    public static VcrRecord2 vcrSendMessage = new VcrRecord2();
    
    



    public static void NET_Init()
    {
        for (int i2 = 0; i2 < hostcache.Length; i2++)
            hostcache[i2] = new hostcache_t();

        if (net_drivers == null)
        {
            if (HasParam("-playback"))
            {
                net_drivers = new INetDriver[]
                {
                    new NetVcr()
                };
            }
            else
            {
                net_drivers = new INetDriver[]
                {
                    new NetLoop(),
                    NetDatagram.Instance
                };
            }
        }

        if (net_landrivers == null)
        {
            net_landrivers = new INetLanDriver[]
            {
                NetTcpIp.Instance
            };
        }

        if (HasParam("-record"))
            recording = true;

        int i = COM_CheckParm("-port");
        if (i == 0)
            i = COM_CheckParm("-udpport");
        if (i == 0)
            i = COM_CheckParm("-ipxport");

        if (i > 0)
        {
            if (i < com_argv.Length - 1)
                DEFAULTnet_hostport = atoi(Argv(i + 1));
            else
                Sys_Error("Net.Init: you must specify a number after -port!");
        }
        net_hostport = DEFAULTnet_hostport;

        if (HasParam("-listen") || cls.state == cactive_t.ca_dedicated)
            listening = true;
        int numsockets = svs.maxclientslimit;
        if (cls.state != cactive_t.ca_dedicated)
            numsockets++;

        net_freeSockets = new List<qsocket_t>(numsockets);
        net_activeSockets = new List<qsocket_t>(numsockets);

        for (i = 0; i < numsockets; i++)
            net_freeSockets.Add(new qsocket_t());

        SetNetTime();

        // allocate space for network message buffer
        Message = new MsgWriter(q_shared.NET_MAXMESSAGE); // SZ_Alloc (&net_message, NET_MAXMESSAGE);
        Reader = new MsgReader(Message);
        
        net_messagetimeout = new cvar_t("net_messagetimeout", "300");
        hostname = new cvar_t("hostname", "UNNAMED");

        Cmd_AddCommand("slist", NET_Slist_f);
        Cmd_AddCommand("listen", NET_Listen_f);
        Cmd_AddCommand("maxplayers", MaxPlayers_f);
        Cmd_AddCommand("port", NET_Port_f);

        // initialize all the drivers
        net_driverlevel = 0;
        foreach (INetDriver driver in net_drivers)
        {
            driver.Init();
            if (driver.IsInitialized && listening)
            {
                driver.Datagram_Listen(true);
            }
            net_driverlevel++;
        }

        //if (*my_ipx_address)
        //    Con_DPrintf("IPX address %s\n", my_ipx_address);
        if (!String.IsNullOrEmpty(my_tcpip_address))
            Con_DPrintf("TCP/IP address {0}\n", my_tcpip_address);
    }
    public static void NET_Shutdown()
    {
        SetNetTime();

        if (net_activeSockets != null)
        {
            qsocket_t[] tmp = net_activeSockets.ToArray();
            foreach (qsocket_t sock in tmp)
                NET_Close(sock);
        }

        //
        // shutdown the drivers
        //
        if (net_drivers != null)
        {
            for (net_driverlevel = 0; net_driverlevel < net_drivers.Length; net_driverlevel++)
            {
                if (net_drivers[net_driverlevel].IsInitialized)
                    net_drivers[net_driverlevel].Datagram_Shutdown();
            }
        }
    }
    public static qsocket_t NET_CheckNewConnections()
    {
        SetNetTime();

        for (net_driverlevel = 0; net_driverlevel < net_drivers.Length; net_driverlevel++)
        {
            if (!net_drivers[net_driverlevel].IsInitialized)
                continue;

            if (net_driverlevel > 0 && !listening)
                continue;

            qsocket_t ret = net_drivers[net_driverlevel].Datagram_CheckNewConnections();
            if (ret != null)
            {
                if (recording)
                {
                    vcrConnect.time = host_time;
                    vcrConnect.op = q_shared.VCR_OP_CONNECT;
                    vcrConnect.session = 1; // (long)ret; // Uze: todo: make it work on 64bit systems
                    byte[] buf = StructureToBytes(ref vcrConnect);
                    _VcrWriter.Write(buf, 0, buf.Length);
                    buf = Encoding.ASCII.GetBytes(ret.address);
                    int count = Math.Min(buf.Length, q_shared.NET_NAMELEN);
                    int extra = q_shared.NET_NAMELEN - count;
                    _VcrWriter.Write(buf, 0, count);
                    for (int i = 0; i < extra; i++)
                        _VcrWriter.Write((byte)0);
                }
                return ret;
            }
        }

        if (recording)
        {
            vcrConnect.time = host_time;
            vcrConnect.op = q_shared.VCR_OP_CONNECT;
            vcrConnect.session = 0;
            byte[] buf = StructureToBytes(ref vcrConnect);
            _VcrWriter.Write(buf, 0, buf.Length);
        }

        return null;
    }
    public static qsocket_t NET_Connect(string host)
    {
        int numdrivers = net_drivers.Length;// net_numdrivers;

        SetNetTime();

        if (String.IsNullOrEmpty(host))
            host = null;

        if (host != null)
        {
            if (SameText(host, "local"))
            {
                numdrivers = 1;
                goto JustDoIt;
            }

            if (hostCacheCount > 0)
            {
                foreach (hostcache_t hc in hostcache)
                {
                    if (SameText(hc.name, host))
                    {
                        host = hc.cname;
                        goto JustDoIt;
                    }
                }
            }
        }

        SlistSilent = (host != null);
        NET_Slist_f();

        while (slistInProgress)
            NET_Poll();

        if (host == null)
        {
            if (hostCacheCount != 1)
                return null;
            host = hostcache[0].cname;
            Con_Printf("Connecting to...\n{0} @ {1}\n\n", hostcache[0].name, host);
        }

        net_driverlevel = 0;
        foreach (hostcache_t hc in hostcache)
        {
            if (SameText(host, hc.name))
            {
                host = hc.cname;
                break;
            }
            net_driverlevel++;
        }

        JustDoIt:
        net_driverlevel = 0;
        foreach (INetDriver drv in net_drivers)
        {
            if (!drv.IsInitialized)
                continue;
            qsocket_t ret = drv.Datagram_Connect(host);
            if (ret != null)
                return ret;
            net_driverlevel++;
        }

        if (host != null)
        {
            Con_Printf("\n");
            PrintSlistHeader();
            PrintSlist();
            PrintSlistTrailer();
        }

        return null;
    }
    public static void PrintSlistHeader()
    {
        Con_Printf("Server          Map             Users\n");
        Con_Printf("--------------- --------------- -----\n");
        slistLastShown = 0;
    }
    public static void PrintSlist()
    {
        int i;
        for (i = slistLastShown; i < hostCacheCount; i++)
        {
            hostcache_t hc = hostcache[i];
            if (hc.maxusers != 0)
                Con_Printf("{0,-15} {1,-15}\n {2,2}/{3,2}\n", Copy(hc.name, 15), Copy(hc.map, 15), hc.users, hc.maxusers);
            else
                Con_Printf("{0,-15} {1,-15}\n", Copy(hc.name, 15), Copy(hc.map, 15));
        }
        slistLastShown = i;
    }
    public static void PrintSlistTrailer()
    {
        if (hostCacheCount != 0)
            Con_Printf("== end list ==\n\n");
        else
            Con_Printf("No Quake servers found.\n\n");
    }
    public static bool NET_CanSendMessage(qsocket_t sock)
    {
        if (sock == null)
            return false;

        if (sock.disconnected)
            return false;

        SetNetTime();

        bool r = net_drivers[sock.driver].Datagram_CanSendMessage(sock);

        if (recording)
        {
            vcrSendMessage.time = host_time;
            vcrSendMessage.op = q_shared.VCR_OP_CANSENDMESSAGE;
            vcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
            vcrSendMessage.ret = r ? 1 : 0;
            byte[] buf = StructureToBytes(ref vcrSendMessage);
            _VcrWriter.Write(buf, 0, buf.Length);
        }

        return r;
    }
    public static int NET_GetMessage(qsocket_t sock)
    {
        //int ret;

        if (sock == null)
            return -1;

        if (sock.disconnected)
        {
            Con_Printf("NET_GetMessage: disconnected socket\n");
            return -1;
        }

        SetNetTime();

        int ret = net_drivers[sock.driver].GetMessage(sock);

        // see if this connection has timed out
        if (ret == 0 && sock.driver != 0)
        {
            if (net_time - sock.lastMessageTime > net_messagetimeout.value)
            {
                NET_Close(sock);
                return -1;
            }
        }

        if (ret > 0)
        {
            if (sock.driver != 0)
            {
                sock.lastMessageTime = net_time;
                if (ret == 1)
                    _MessagesReceived++;
                else if (ret == 2)
                    _UnreliableMessagesReceived++;
            }

            if (recording)
            {
                vcrGetMessage.time = host_time;
                vcrGetMessage.op = q_shared.VCR_OP_GETMESSAGE;
                vcrGetMessage.session = 1;// (long)sock; Uze todo: write somethisng meaningful
                vcrGetMessage.ret = ret;
                byte[] buf = StructureToBytes(ref vcrGetMessage);
                _VcrWriter.Write(buf, 0, buf.Length);
                _VcrWriter.Write(Message.Length);
                _VcrWriter.Write(Message.Data, 0, Message.Length);
            }
        }
        else
        {
            if (recording)
            {
                vcrGetMessage.time = host_time;
                vcrGetMessage.op = q_shared.VCR_OP_GETMESSAGE;
                vcrGetMessage.session = 1; // (long)sock; Uze todo: fix this
                vcrGetMessage.ret = ret;
                byte[] buf = StructureToBytes(ref vcrGetMessage);
                _VcrWriter.Write(buf, 0, buf.Length);
            }
        }

        return ret;
    }
    public static int NET_SendMessage(qsocket_t sock, MsgWriter data)
    {
        if (sock == null)
            return -1;

        if (sock.disconnected)
        {
            Con_Printf("NET_SendMessage: disconnected socket\n");
            return -1;
        }

        SetNetTime();

        int r = net_drivers[sock.driver].Datagram_SendMessage(sock, data);
        if (r == 1 && sock.driver != 0)
            _MessagesSent++;

        if (recording)
        {
            vcrSendMessage.time = host_time;
            vcrSendMessage.op = q_shared.VCR_OP_SENDMESSAGE;
            vcrSendMessage.session = 1; // (long)sock; Uze: todo: do something?
            vcrSendMessage.ret = r;
            byte[] buf = StructureToBytes(ref vcrSendMessage);
            _VcrWriter.Write(buf, 0, buf.Length);

        }

        return r;
    }
    public static int NET_SendUnreliableMessage(qsocket_t sock, MsgWriter data)
    {
        if (sock == null)
            return -1;

        if (sock.disconnected)
        {
            Con_Printf("NET_SendMessage: disconnected socket\n");
            return -1;
        }

        SetNetTime();

        int r = net_drivers[sock.driver].Datagram_SendUnreliableMessage(sock, data);
        if (r == 1 && sock.driver != 0)
            _UnreliableMessagesSent++;

        if (recording)
        {
            vcrSendMessage.time = host_time;
            vcrSendMessage.op = q_shared.VCR_OP_SENDMESSAGE;
            vcrSendMessage.session = 1;// (long)sock; Uze todo: ???????
            vcrSendMessage.ret = r;
            byte[] buf = StructureToBytes(ref vcrSendMessage);
            _VcrWriter.Write(buf);
        }

        return r;
    }
    public static int NET_SendToAll(MsgWriter data, int blocktime)
    {
        bool[] state1 = new bool[q_shared.MAX_SCOREBOARD];
        bool[] state2 = new bool[q_shared.MAX_SCOREBOARD];

        int count = 0;
        for (int i = 0; i < svs.maxclients; i++)
        {
            host_client = svs.clients[i];
            if (host_client.netconnection == null)
                continue;

            if (host_client.active)
            {
                if (host_client.netconnection.driver == 0)
                {
                    NET_SendMessage(host_client.netconnection, data);
                    state1[i] = true;
                    state2[i] = true;
                    continue;
                }
                count++;
                state1[i] = false;
                state2[i] = false;
            }
            else
            {
                state1[i] = true;
                state2[i] = true;
            }
        }

        double start = Sys_FloatTime();
        while (count > 0)
        {
            count = 0;
            for (int i = 0; i < svs.maxclients; i++)
            {
                host_client = svs.clients[i];
                if (!state1[i])
                {
                    if (NET_CanSendMessage(host_client.netconnection))
                    {
                        state1[i] = true;
                        NET_SendMessage(host_client.netconnection, data);
                    }
                    else
                    {
                        NET_GetMessage(host_client.netconnection);
                    }
                    count++;
                    continue;
                }

                if (!state2[i])
                {
                    if (NET_CanSendMessage(host_client.netconnection))
                    {
                        state2[i] = true;
                    }
                    else
                    {
                        NET_GetMessage(host_client.netconnection);
                    }
                    count++;
                    continue;
                }
            }
            if ((Sys_FloatTime() - start) > blocktime)
                break;
        }
        return count;
    }
    public static void NET_Close(qsocket_t sock)
    {
        if (sock == null)
            return;

        if (sock.disconnected)
            return;

        SetNetTime();

        // call the driver_Close function
        net_drivers[sock.driver].Datagram_Close(sock);

        NET_FreeQSocket(sock);
    }
    public static void NET_FreeQSocket(qsocket_t sock)
    {
        // remove it from active list
        if (!net_activeSockets.Remove(sock))
            Sys_Error("NET_FreeQSocket: not active\n");

        // add it to free list
        net_freeSockets.Add(sock);
        sock.disconnected = true;
    }
    public static void NET_Poll()
    {
        SetNetTime();

        for (PollProcedure pp = pollProcedureList; pp != null; pp = pp.next)
        {
            if (pp.nextTime > net_time)
                break;

            pollProcedureList = pp.next;
            pp.procedure(pp.arg);
        }
    }
    public static double SetNetTime()
    {
        net_time = Sys_FloatTime();
        return net_time;
    }
    public static void NET_Slist_f()
    {
        if (slistInProgress)
            return;

        if (!SlistSilent)
        {
            Con_Printf("Looking for Quake servers...\n");
            PrintSlistHeader();
        }

        slistInProgress = true;
        slistStartTime = Sys_FloatTime();

        SchedulePollProcedure(_SlistSendProcedure, 0.0);
        SchedulePollProcedure(_SlistPollProcedure, 0.1);

        hostCacheCount = 0;
    }
    public static void SchedulePollProcedure(PollProcedure proc, double timeOffset)
    {
        proc.nextTime = Sys_FloatTime() + timeOffset;
        PollProcedure pp, prev;
        for (pp = pollProcedureList, prev = null; pp != null; pp = pp.next)
        {
            if (pp.nextTime >= proc.nextTime)
                break;
            prev = pp;
        }

        if (prev == null)
        {
            proc.next = pollProcedureList;
            pollProcedureList = proc;
            return;
        }

        proc.next = pp;
        prev.next = proc;
    }
    public static void NET_Listen_f()
    {
        if (cmd_argc != 2)
        {
            Con_Printf("\"listen\" is \"{0}\"\n", listening ? 1 : 0);
            return;
        }

        listening = (atoi(Cmd_Argv(1)) != 0);

        foreach (INetDriver driver in net_drivers)
        {
            if (driver.IsInitialized)
            {
                driver.Datagram_Listen(listening);
            }
        }
    }
    public static void MaxPlayers_f()
    {
        if (cmd_argc != 2)
        {
            Con_Printf("\"maxplayers\" is \"%u\"\n", svs.maxclients);
            return;
        }

        if (sv.active)
        {
            Con_Printf("maxplayers can not be changed while a server is running.\n");
            return;
        }

        int n = atoi(Cmd_Argv(1));
        if (n < 1)
            n = 1;
        if (n > svs.maxclientslimit)
        {
            n = svs.maxclientslimit;
            Con_Printf("\"maxplayers\" set to \"{0}\"\n", n);
        }

        if (n == 1 && listening)
            Cbuf_AddText("listen 0\n");

        if (n > 1 && !listening)
            Cbuf_AddText("listen 1\n");

        svs.maxclients = n;
        if (n == 1)
            Cvar.Cvar_Set("deathmatch", "0");
        else
            Cvar.Cvar_Set("deathmatch", "1");
    }
    public static void NET_Port_f()
    {
        if (cmd_argc != 2)
        {
            Con_Printf("\"port\" is \"{0}\"\n", net_hostport);
            return;
        }

        int n = atoi(Cmd_Argv(1));
        if (n < 1 || n > 65534)
        {
            Con_Printf("Bad value, must be between 1 and 65534\n");
            return;
        }

        DEFAULTnet_hostport = n;
        net_hostport = n;

        if (listening)
        {
            // force a change to the new port
            Cbuf_AddText("listen 0\n");
            Cbuf_AddText("listen 1\n");
        }
    }    
    public static qsocket_t NET_NewQSocket()
    {
        if (net_freeSockets.Count == 0)
            return null;

        if (net_activeconnections >= svs.maxclients)
            return null;

        // get one from free list
        int i = net_freeSockets.Count - 1;
        qsocket_t sock = net_freeSockets[i];
        net_freeSockets.RemoveAt(i);

        // add it to active list
        net_activeSockets.Add(sock);

        sock.disconnected = false;
        sock.connecttime = net_time;
        sock.address = "UNSET ADDRESS";
        sock.driver = net_driverlevel;
        sock.socket = null;
        sock.driverdata = null;
        sock.canSend = true;
        sock.sendNext = false;
        sock.lastMessageTime = net_time;
        sock.ackSequence = 0;
        sock.sendSequence = 0;
        sock.unreliableSendSequence = 0;
        sock.sendMessageLength = 0;
        sock.receiveSequence = 0;
        sock.unreliableReceiveSequence = 0;
        sock.receiveMessageLength = 0;

        return sock;
    }
    public static void Slist_Send(object arg)
    {
        for (net_driverlevel = 0; net_driverlevel < net_drivers.Length; net_driverlevel++)
        {
            if (!SlistLocal && net_driverlevel == 0)
                continue;
            if (!net_drivers[net_driverlevel].IsInitialized)
                continue;

            net_drivers[net_driverlevel].Datagram_SearchForHosts(true);
        }

        if ((Sys_FloatTime() - slistStartTime) < 0.5)
            SchedulePollProcedure(_SlistSendProcedure, 0.75);
    }
    public static void Slist_Poll(object arg)
    {
        for (net_driverlevel = 0; net_driverlevel < net_drivers.Length; net_driverlevel++)
        {
            if (!SlistLocal && net_driverlevel == 0)
                continue;
            if (!net_drivers[net_driverlevel].IsInitialized)
                continue;

            net_drivers[net_driverlevel].Datagram_SearchForHosts(false);
        }

        if (!SlistSilent)
            PrintSlist();

        if ((Sys_FloatTime() - slistStartTime) < 1.5)
        {
            SchedulePollProcedure(_SlistPollProcedure, 0.1);
            return;
        }

        if (!SlistSilent)
            PrintSlistTrailer();

        slistInProgress = false;
        SlistSilent = false;
        SlistLocal = true;
    }
}