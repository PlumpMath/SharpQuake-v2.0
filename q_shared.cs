using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK;
using System.Net;
using System.Net.Sockets;
using System.IO;
using OpenTK.Graphics.OpenGL;
using OpenTK.Audio;
using OpenTK.Audio.OpenAL;
using string_t = System.Int32;
using func_t = System.Int32;

public delegate void xcommand_t();
public delegate void PollHandler(object arg);
public delegate void builtin_t();

public class q_shared
{
    // anorm_dots.cs
    public const int SHADEDOT_QUANT = 16;

    // bspfile.cs
    public const int MAX_MAP_HULLS = 4;
    public const int MAX_MAP_MODELS = 256;
    public const int MAX_MAP_BRUSHES = 4096;
    public const int MAX_MAP_ENTITIES = 1024;
    public const int MAX_MAP_ENTSTRING = 65536;
    public const int MAX_MAP_PLANES = 32767;
    public const int MAX_MAP_NODES = 32767;
    public const int MAX_MAP_CLIPNODES = 32767;
    public const int MAX_MAP_LEAFS = 8192;
    public const int MAX_MAP_VERTS = 65535;
    public const int MAX_MAP_FACES = 65535;
    public const int MAX_MAP_MARKSURFACES = 65535;
    public const int MAX_MAP_TEXINFO = 4096;
    public const int MAX_MAP_EDGES = 256000;
    public const int MAX_MAP_SURFEDGES = 512000;
    public const int MAX_MAP_TEXTURES = 512;
    public const int MAX_MAP_MIPTEX = 0x200000;
    public const int MAX_MAP_LIGHTING = 0x100000;
    public const int MAX_MAP_VISIBILITY = 0x100000;
    public const int MAX_MAP_PORTALS = 65536;
    public const int MAX_KEY = 32;
    public const int MAX_VALUE = 1024;
    public const int MAXLIGHTMAPS = 4;
    public const int BSPVERSION = 29;
    public const int TOOLVERSION = 2;
    public const int HEADER_LUMPS = 15;
    public const int MIPLEVELS = 4;
    public const int TEX_SPECIAL = 1;
    public const int LUMP_ENTITIES = 0;
    public const int LUMP_PLANES = 1;
    public const int LUMP_TEXTURES = 2;
    public const int LUMP_VERTEXES = 3;
    public const int LUMP_VISIBILITY = 4;
    public const int LUMP_NODES = 5;
    public const int LUMP_TEXINFO = 6;
    public const int LUMP_FACES = 7;
    public const int LUMP_LIGHTING = 8;
    public const int LUMP_CLIPNODES = 9;
    public const int LUMP_LEAFS = 10;
    public const int LUMP_MARKSURFACES = 11;
    public const int LUMP_EDGES = 12;
    public const int LUMP_SURFEDGES = 13;
    public const int LUMP_MODELS = 14;
    public const int PLANE_X = 0;
    public const int PLANE_Y = 1;
    public const int PLANE_Z = 2;
    public const int PLANE_ANYX = 3;
    public const int PLANE_ANYY = 4;
    public const int PLANE_ANYZ = 5;
    public const int CONTENTS_EMPTY = -1;
    public const int CONTENTS_SOLID = -2;
    public const int CONTENTS_WATER = -3;
    public const int CONTENTS_SLIME = -4;
    public const int CONTENTS_LAVA = -5;
    public const int CONTENTS_SKY = -6;
    public const int CONTENTS_ORIGIN = -7;
    public const int CONTENTS_CLIP = -8;
    public const int CONTENTS_CURRENT_0 = -9;
    public const int CONTENTS_CURRENT_90 = -10;
    public const int CONTENTS_CURRENT_180 = -11;
    public const int CONTENTS_CURRENT_270 = -12;
    public const int CONTENTS_CURRENT_UP = -13;
    public const int CONTENTS_CURRENT_DOWN = -14;
    public const int AMBIENT_WATER = 0;
    public const int AMBIENT_SKY = 1;
    public const int AMBIENT_SLIME = 2;
    public const int AMBIENT_LAVA = 3;
    public const int NUM_AMBIENTS = 4;

    // client.cs
    public const int SIGNONS = 4;
    public const int MAX_DLIGHTS = 32;
    public const int MAX_BEAMS = 24;
    public const int MAX_EFRAGS = 640;
    public const int MAX_MAPSTRING = 2048;
    public const int MAX_DEMOS = 8;
    public const int MAX_DEMONAME = 16;
    public const int MAX_VISEDICTS = 256;
    public const int MAX_TEMP_ENTITIES = 64;
    public const int MAX_STATIC_ENTITIES = 128;
    public const int MAX_EDICTS = 600;          // FIXME: ouch! ouch! ouch!
    public const int MAX_LIGHTSTYLES = 64;
    public const int CSHIFT_CONTENTS = 0;
    public const int CSHIFT_DAMAGE = 1;
    public const int CSHIFT_BONUS = 2;
    public const int CSHIFT_POWERUP = 3;
    public const int NUM_CSHIFTS = 4;

    // cmd.cs
    public const int MAX_ALIAS_NAME = 32;
    public const int MAX_ARGS = 80;

    // modeltypes.cs
    public const int EF_ROCKET = 1;			// leave a trail
    public const int EF_GRENADE = 2;			// leave a trail
    public const int EF_GIB = 4;			// leave a trail
    public const int EF_ROTATE = 8;			// rotate (bonus items)
    public const int EF_TRACER = 16;			// green split trail
    public const int EF_ZOMGIB = 32;			// small blood trail
    public const int EF_TRACER2 = 64;			// orange split trail + rotate
    public const int EF_TRACER3 = 128;			// purple trail
    public const int SPR_VP_PARALLEL_UPRIGHT = 0;
    public const int SPR_FACING_UPRIGHT = 1;
    public const int SPR_VP_PARALLEL = 2;
    public const int SPR_ORIENTED = 3;
    public const int SPR_VP_PARALLEL_ORIENTED = 4;
    public const int SURF_PLANEBACK = 2;
    public const int SURF_DRAWSKY = 4;
    public const int SURF_DRAWSPRITE = 8;
    public const int SURF_DRAWTURB = 0x10;
    public const int SURF_DRAWTILED = 0x20;
    public const int SURF_DRAWBACKGROUND = 0x40;
    public const int SURF_UNDERWATER = 0x80;
    public const int SIDE_FRONT = 0;
    public const int SIDE_BACK = 1;
    public const int SIDE_ON = 2;
    public static int EF_BRIGHTFIELD = 1;
    public static int EF_MUZZLEFLASH = 2;
    public static int EF_BRIGHTLIGHT = 4;
    public static int EF_DIMLIGHT = 8;

    // net.cs
    public const int NETFLAG_LENGTH_MASK = 0x0000ffff;
    public const int NETFLAG_DATA = 0x00010000;
    public const int NETFLAG_ACK = 0x00020000;
    public const int NETFLAG_NAK = 0x00040000;
    public const int NETFLAG_EOM = 0x00080000;
    public const int NETFLAG_UNRELIABLE = 0x00100000;
    public const int NETFLAG_CTL = -2147483648;// 0x80000000;
    public const int CCREQ_CONNECT = 0x01;
    public const int CCREQ_SERVER_INFO = 0x02;
    public const int CCREQ_PLAYER_INFO = 0x03;
    public const int CCREQ_RULE_INFO = 0x04;
    public const int CCREP_ACCEPT = 0x81;
    public const int CCREP_REJECT = 0x82;
    public const int CCREP_SERVER_INFO = 0x83;
    public const int CCREP_PLAYER_INFO = 0x84;
    public const int CCREP_RULE_INFO = 0x85;
    public const int NET_PROTOCOL_VERSION = 3;
    public const int HOSTCACHESIZE = 8;
    public const int NET_NAMELEN = 64;
    public const int NET_MAXMESSAGE = 8192;
    public const int NET_HEADERSIZE = 2 * sizeof(uint);
    public const int NET_DATAGRAMSIZE = q_shared.MAX_DATAGRAM + NET_HEADERSIZE;

    // netvcr.cs
    public const int VCR_OP_CONNECT = 1;
    public const int VCR_OP_GETMESSAGE = 2;
    public const int VCR_OP_SENDMESSAGE = 3;
    public const int VCR_OP_CANSENDMESSAGE = 4;
    public const int VCR_MAX_MESSAGE = 4;

    // modelgen.cs
    public const int ALIAS_VERSION = 6;
    public const int IDPOLYHEADER = (('O' << 24) + ('P' << 16) + ('D' << 8) + 'I'); // little-endian "IDPO"

    // spritegn.cs
    public const int SPRITE_VERSION = 1;
    public const int IDSPRITEHEADER = (('P' << 24) + ('S' << 16) + ('D' << 8) + 'I'); // little-endian "IDSP"

    // gl_model.cs
    public const int VERTEXSIZE = 7;
    public const int MAX_SKINS = 32;
    public const int MAXALIASVERTS = 1024;
    public const int MAXALIASFRAMES = 256;
    public const int MAXALIASTRIS = 2048;
    public const int MAX_MOD_KNOWN = 512;
    public const int MAX_LBM_HEIGHT = 480;
    public const int ANIM_CYCLE = 2;
    public static float ALIAS_BASE_SIZE_RATIO = (1.0f / 11.0f);

    // common.cs
    public const int MAX_FILES_IN_PACK = 2048;
    public const int PAK0_COUNT = 339;
    public const int PAK0_CRC = 32981;
    public static Vector3 ZeroVector = Vector3.Zero;
    public static v3f ZeroVector3f = default(v3f);
    public static readonly byte[] ZeroBytes = new byte[4096];
    public static ushort[] _Pop = new ushort[]
    {
            0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x6600,0x0000,0x0000,0x0000,0x6600,0x0000
        ,0x0000,0x0066,0x0000,0x0000,0x0000,0x0000,0x0067,0x0000
        ,0x0000,0x6665,0x0000,0x0000,0x0000,0x0000,0x0065,0x6600
        ,0x0063,0x6561,0x0000,0x0000,0x0000,0x0000,0x0061,0x6563
        ,0x0064,0x6561,0x0000,0x0000,0x0000,0x0000,0x0061,0x6564
        ,0x0064,0x6564,0x0000,0x6469,0x6969,0x6400,0x0064,0x6564
        ,0x0063,0x6568,0x6200,0x0064,0x6864,0x0000,0x6268,0x6563
        ,0x0000,0x6567,0x6963,0x0064,0x6764,0x0063,0x6967,0x6500
        ,0x0000,0x6266,0x6769,0x6a68,0x6768,0x6a69,0x6766,0x6200
        ,0x0000,0x0062,0x6566,0x6666,0x6666,0x6666,0x6562,0x0000
        ,0x0000,0x0000,0x0062,0x6364,0x6664,0x6362,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0062,0x6662,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0061,0x6661,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0000,0x6500,0x0000,0x0000,0x0000
        ,0x0000,0x0000,0x0000,0x0000,0x6400,0x0000,0x0000,0x0000
    };
    public static string[] safeargvs = new string[]
    {
        "-stdvid", "-nolan", "-nosound", "-nocdaudio", "-nojoy", "-nomouse", "-dibonly"
    };

    // keys.cs
    //
    // these are the key numbers that should be passed to Key_Event
    //
    public const int K_TAB = 9;
    public const int K_ENTER = 13;
    public const int K_ESCAPE = 27;
    public const int K_SPACE = 32;

    // normal keys should be passed as lowercased ascii

    public const int K_BACKSPACE = 127;
    public const int K_UPARROW = 128;
    public const int K_DOWNARROW = 129;
    public const int K_LEFTARROW = 130;
    public const int K_RIGHTARROW = 131;

    public const int K_ALT = 132;
    public const int K_CTRL = 133;
    public const int K_SHIFT = 134;
    public const int K_F1 = 135;
    public const int K_F2 = 136;
    public const int K_F3 = 137;
    public const int K_F4 = 138;
    public const int K_F5 = 139;
    public const int K_F6 = 140;
    public const int K_F7 = 141;
    public const int K_F8 = 142;
    public const int K_F9 = 143;
    public const int K_F10 = 144;
    public const int K_F11 = 145;
    public const int K_F12 = 146;
    public const int K_INS = 147;
    public const int K_DEL = 148;
    public const int K_PGDN = 149;
    public const int K_PGUP = 150;
    public const int K_HOME = 151;
    public const int K_END = 152;
    public const int K_PAUSE = 255;
    public const int K_MOUSE1 = 200;
    public const int K_MOUSE2 = 201;
    public const int K_MOUSE3 = 202;
    public const int K_JOY1 = 203;
    public const int K_JOY2 = 204;
    public const int K_JOY3 = 205;
    public const int K_JOY4 = 206;
    public const int K_AUX1 = 207;
    public const int K_AUX2 = 208;
    public const int K_AUX3 = 209;
    public const int K_AUX4 = 210;
    public const int K_AUX5 = 211;
    public const int K_AUX6 = 212;
    public const int K_AUX7 = 213;
    public const int K_AUX8 = 214;
    public const int K_AUX9 = 215;
    public const int K_AUX10 = 216;
    public const int K_AUX11 = 217;
    public const int K_AUX12 = 218;
    public const int K_AUX13 = 219;
    public const int K_AUX14 = 220;
    public const int K_AUX15 = 221;
    public const int K_AUX16 = 222;
    public const int K_AUX17 = 223;
    public const int K_AUX18 = 224;
    public const int K_AUX19 = 225;
    public const int K_AUX20 = 226;
    public const int K_AUX21 = 227;
    public const int K_AUX22 = 228;
    public const int K_AUX23 = 229;
    public const int K_AUX24 = 230;
    public const int K_AUX25 = 231;
    public const int K_AUX26 = 232;
    public const int K_AUX27 = 233;
    public const int K_AUX28 = 234;
    public const int K_AUX29 = 235;
    public const int K_AUX30 = 236;
    public const int K_AUX31 = 237;
    public const int K_AUX32 = 238;
    public const int K_MWHEELUP = 239;
    public const int K_MWHEELDOWN = 240;
    public const int MAXCMDLINE = 256;

    // console.cs
    public const string LOG_FILE_NAME = "qconsole.log";
    public const int CON_TEXTSIZE = 16384;
    public const int NUM_CON_TIMES = 4;

    // gl_draw.cs
    public const int MAX_GLTEXTURES = 1024;
    public const int MAX_CACHED_PICS = 128;
    public const int MAX_SCRAPS = 2;
    public const int BLOCK_WIDTH = 256;
    public const int BLOCK_HEIGHT = 256;

    // gl_mesh.cs
    public const int MAX_COMMANDS = 8192;
    public const int MAX_STRIP = 128;

    // host.cs
    public const int VCR_SIGNATURE = 0x56435231;
    public const int SAVEGAME_VERSION = 5;

    // NetTcpIp.cs
    public const int WSAEWOULDBLOCK = 10035;
    public const int WSAECONNREFUSED = 10061;

    // protocol.cs
    public const int PROTOCOL_VERSION = 15;
    public const int U_MOREBITS = (1 << 0);
    public const int U_ORIGIN1 = (1 << 1);
    public const int U_ORIGIN2 = (1 << 2);
    public const int U_ORIGIN3 = (1 << 3);
    public const int U_ANGLE2 = (1 << 4);
    public const int U_NOLERP = (1 << 5);
    public const int U_FRAME = (1 << 6);
    public const int U_SIGNAL = (1 << 7);
    public const int U_ANGLE1 = (1 << 8);
    public const int U_ANGLE3 = (1 << 9);
    public const int U_MODEL = (1 << 10);
    public const int U_COLORMAP = (1 << 11);
    public const int U_SKIN = (1 << 12);
    public const int U_EFFECTS = (1 << 13);
    public const int U_LONGENTITY = (1 << 14);
    public const int SU_VIEWHEIGHT = (1 << 0);
    public const int SU_IDEALPITCH = (1 << 1);
    public const int SU_PUNCH1 = (1 << 2);
    public const int SU_PUNCH2 = (1 << 3);
    public const int SU_PUNCH3 = (1 << 4);
    public const int SU_VELOCITY1 = (1 << 5);
    public const int SU_VELOCITY2 = (1 << 6);
    public const int SU_VELOCITY3 = (1 << 7);
    public const int SU_ITEMS = (1 << 9);
    public const int SU_ONGROUND = (1 << 10);
    public const int SU_INWATER = (1 << 11);
    public const int SU_WEAPONFRAME = (1 << 12);
    public const int SU_ARMOR = (1 << 13);
    public const int SU_WEAPON = (1 << 14);
    public const int SND_VOLUME = (1 << 0);
    public const int SND_ATTENUATION = (1 << 1);
    public const int SND_LOOPING = (1 << 2);
    public const int DEFAULT_VIEWHEIGHT = 22;
    public const int GAME_COOP = 0;
    public const int GAME_DEATHMATCH = 1;
    public const int svc_bad = 0;
    public const int svc_nop = 1;
    public const int svc_disconnect = 2;
    public const int svc_updatestat = 3;
    public const int svc_version = 4;
    public const int svc_setview = 5;
    public const int svc_sound = 6;
    public const int svc_time = 7;
    public const int svc_print = 8;
    public const int svc_stufftext = 9;
    public const int svc_setangle = 10;
    public const int svc_serverinfo = 11;
    public const int svc_lightstyle = 12;
    public const int svc_updatename = 13;
    public const int svc_updatefrags = 14;
    public const int svc_clientdata = 15;
    public const int svc_stopsound = 16;
    public const int svc_updatecolors = 17;
    public const int svc_particle = 18;
    public const int svc_damage = 19;
    public const int svc_spawnstatic = 20;
    public const int svc_spawnbaseline = 22;
    public const int svc_temp_entity = 23;
    public const int svc_setpause = 24;
    public const int svc_signonnum = 25;
    public const int svc_centerprint = 26;
    public const int svc_killedmonster = 27;
    public const int svc_foundsecret = 28;
    public const int svc_spawnstaticsound = 29;
    public const int svc_intermission = 30;
    public const int svc_finale = 31;
    public const int svc_cdtrack = 32;
    public const int svc_sellscreen = 33;
    public const int svc_cutscene = 34;
    public const int clc_bad = 0;
    public const int clc_nop = 1;
    public const int clc_disconnect = 2;
    public const int clc_move = 3;
    public const int clc_stringcmd = 4;
    public const int TE_SPIKE = 0;
    public const int TE_SUPERSPIKE = 1;
    public const int TE_GUNSHOT = 2;
    public const int TE_EXPLOSION = 3;
    public const int TE_TAREXPLOSION = 4;
    public const int TE_LIGHTNING1 = 5;
    public const int TE_LIGHTNING2 = 6;
    public const int TE_WIZSPIKE = 7;
    public const int TE_KNIGHTSPIKE = 8;
    public const int TE_LIGHTNING3 = 9;
    public const int TE_LAVASPLASH = 10;
    public const int TE_TELEPORT = 11;
    public const int TE_EXPLOSION2 = 12;
    public const int TE_BEAM = 13;

    // progs.cs
    public const int DEF_SAVEGLOBAL = (1 << 15);
    public const int MAX_PARMS = 8;
    public const int MAX_ENT_LEAFS = 16;
    public const int PROG_VERSION = 6;
    public const int PROGHEADER_CRC = 5927;
    public const int OFS_NULL = 0;
    public const int OFS_RETURN = 1;
    public const int OFS_PARM0 = 4;
    public const int OFS_PARM1 = 7;
    public const int OFS_PARM2 = 10;
    public const int OFS_PARM3 = 13;
    public const int OFS_PARM4 = 16;
    public const int OFS_PARM5 = 19;
    public const int OFS_PARM6 = 22;
    public const int OFS_PARM7 = 25;
    public const int RESERVED_OFS = 28;

    // progs_edict.cs
    public const int MAX_FIELD_LEN = 64;
    public const int GEFV_CACHESIZE = 2;

    // gl_warp.cs
    public const double TURBSCALE = (256.0 / (2 * Math.PI));

    // progs_exec.cs
    public const int MAX_STACK_DEPTH = 32;
    public const int LOCALSTACK_SIZE = 2048;

    // pr_cmds.cs
    public const int MAX_CHECK = 16;
    public const int MSG_BROADCAST = 0;
    public const int MSG_ONE = 1;
    public const int MSG_ALL = 2;
    public const int MSG_INIT = 3;

    // world.cs
    public const float DIST_EPSILON = 0.03125f;
    public const int MOVE_NORMAL = 0;
    public const int MOVE_NOMONSTERS = 1;
    public const int MOVE_MISSILE = 2;
    public const int AREA_DEPTH = 4;
    public const int AREA_NODES = 32;
    public const float STOP_EPSILON = 0.1f;
    public const int MAX_CLIP_PLANES = 5;
    public const float STEPSIZE = 18;

    // sv_move.cs
    public const float DI_NODIR = -1;

    // sv
    public const int NUM_PING_TIMES = 16;
    public const int NUM_SPAWN_PARMS = 16;

    // movetype
    public const int MOVETYPE_NONE = 0;		// never moves
    public const int MOVETYPE_ANGLENOCLIP = 1;
    public const int MOVETYPE_ANGLECLIP = 2;
    public const int MOVETYPE_WALK = 3;		// gravity
    public const int MOVETYPE_STEP = 4;		// gravity, special edge handling
    public const int MOVETYPE_FLY = 5;
    public const int MOVETYPE_TOSS = 6;		// gravity
    public const int MOVETYPE_PUSH = 7;		// no clip to world, push and crush
    public const int MOVETYPE_NOCLIP = 8;
    public const int MOVETYPE_FLYMISSILE = 9;		// extra size to monsters
    public const int MOVETYPE_BOUNCE = 10;

    // solids
    public const int SOLID_NOT = 0;		// no interaction with other objects
    public const int SOLID_TRIGGER = 1;		// touch on edge, but not blocking
    public const int SOLID_BBOX = 2;		// touch on edge, block
    public const int SOLID_SLIDEBOX = 3;		// touch on edge, but not an onground
    public const int SOLID_BSP = 4;     // bsp clip, touch on edge, block

    // DeadFlags
    public const int DEAD_NO = 0;
    public const int DEAD_DYING = 1;
    public const int DEAD_DEAD = 2;

    // Damages
    public const int DAMAGE_NO = 0;
    public const int DAMAGE_YES = 1;
    public const int DAMAGE_AIM = 2;


    // EdictFlags
    public const int FL_FLY = 1;
    public const int FL_SWIM = 2;
    public const int FL_CONVEYOR = 4;
    public const int FL_CLIENT = 8;
    public const int FL_INWATER = 16;
    public const int FL_MONSTER = 32;
    public const int FL_GODMODE = 64;
    public const int FL_NOTARGET = 128;
    public const int FL_ITEM = 256;
    public const int FL_ONGROUND = 512;
    public const int FL_PARTIALGROUND = 1024;
    public const int FL_WATERJUMP = 2048;
    public const int FL_JUMPRELEASED = 4096;

    // SpawnFlags
    public const int SPAWNFLAG_NOT_EASY = 256;
    public const int SPAWNFLAG_NOT_MEDIUM = 512;
    public const int SPAWNFLAG_NOT_HARD = 1024;
    public const int SPAWNFLAG_NOT_DEATHMATCH = 2048;

    // sv_user.cs
    public const int MAX_FORWARD = 6;

    // sound.cs
    public const int DEFAULT_SOUND_PACKET_VOLUME = 255;
    public const float DEFAULT_SOUND_PACKET_ATTENUATION = 1.0f;
    public const int MAX_CHANNELS = 128;
    public const int MAX_DYNAMIC_CHANNELS = 8;
    public const int MAX_SFX = 512;

    // snd_mix.cs
    public const int PAINTBUFFER_SIZE = 512;
    public const short C8000 = -32768;

    // snd_openal.cs
    public const int AL_BUFFER_COUNT = 24;
    public const int BUFFER_SIZE = 0x10000;

    // quakedef.cs
    public const float VERSION = 1.09f;
    public const float CSQUAKE_VERSION = 1.20f;
    public const float GLQUAKE_VERSION = 1.00f;
    public const float D3DQUAKE_VERSION = 0.01f;
    public const float WINQUAKE_VERSION = 0.996f;
    public const float LINUX_VERSION = 1.30f;
    public const float X11_VERSION = 1.10f;
    public const string GAMENAME = "id1";
    public const int MAX_NUM_ARGVS = 50;
    public const int PITCH = 0;
    public const int YAW = 1;
    public const int ROLL = 2;
    public const int MAX_QPATH = 64;
    public const int MAX_OSPATH = 128;
    public const float ON_EPSILON = 0.1f;
    public const int MAX_MSGLEN = 8000;
    public const int MAX_DATAGRAM = 1024;
    public const int MAX_MODELS = 256;
    public const int MAX_SOUNDS = 256;
    public const int SAVEGAME_COMMENT_LENGTH = 39;
    public const int MAX_STYLESTRING = 64;
    public const int MAX_SCOREBOARD = 16;
    public const int MAX_SCOREBOARDNAME = 32;
    public const int SOUND_CHANNELS = 8;
    public const double BACKFACE_EPSILON = 0.01;

    public static int MAX_CL_STATS = 32;
    public static int STAT_HEALTH = 0;
    public static int STAT_FRAGS = 1;
    public static int STAT_WEAPON = 2;
    public static int STAT_AMMO = 3;
    public static int STAT_ARMOR = 4;
    public static int STAT_WEAPONFRAME = 5;
    public static int STAT_SHELLS = 6;
    public static int STAT_NAILS = 7;
    public static int STAT_ROCKETS = 8;
    public static int STAT_CELLS = 9;
    public static int STAT_ACTIVEWEAPON = 10;
    public static int STAT_TOTALSECRETS = 11;
    public static int STAT_TOTALMONSTERS = 12;
    public static int STAT_SECRETS = 13;
    public static int STAT_MONSTERS = 14;

    public static int IT_SHOTGUN = 1;
    public static int IT_SUPER_SHOTGUN = 2;
    public static int IT_NAILGUN = 4;
    public static int IT_SUPER_NAILGUN = 8;
    public static int IT_GRENADE_LAUNCHER = 16;
    public static int IT_ROCKET_LAUNCHER = 32;
    public static int IT_LIGHTNING = 64;
    public static int IT_SUPER_LIGHTNING = 128;
    public static int IT_SHELLS = 256;
    public static int IT_NAILS = 512;
    public static int IT_ROCKETS = 1024;
    public static int IT_CELLS = 2048;
    public static int IT_AXE = 4096;
    public static int IT_ARMOR1 = 8192;
    public static int IT_ARMOR2 = 16384;
    public static int IT_ARMOR3 = 32768;
    public static int IT_SUPERHEALTH = 65536;
    public static int IT_KEY1 = 131072;
    public static int IT_KEY2 = 262144;
    public static int IT_INVISIBILITY = 524288;
    public static int IT_INVULNERABILITY = 1048576;
    public static int IT_SUIT = 2097152;
    public static int IT_QUAD = 4194304;
    public static int IT_SIGIL1 = (1 << 28);
    public static int IT_SIGIL2 = (1 << 29);
    public static int IT_SIGIL3 = (1 << 30);
    public static int IT_SIGIL4 = (1 << 31);

    public static int RIT_SHELLS = 128;
    public static int RIT_NAILS = 256;
    public static int RIT_ROCKETS = 512;
    public static int RIT_CELLS = 1024;
    public static int RIT_AXE = 2048;
    public static int RIT_LAVA_NAILGUN = 4096;
    public static int RIT_LAVA_SUPER_NAILGUN = 8192;
    public static int RIT_MULTI_GRENADE = 16384;
    public static int RIT_MULTI_ROCKET = 32768;
    public static int RIT_PLASMA_GUN = 65536;
    public static int RIT_ARMOR1 = 8388608;
    public static int RIT_ARMOR2 = 16777216;
    public static int RIT_ARMOR3 = 33554432;
    public static int RIT_LAVA_NAILS = 67108864;
    public static int RIT_PLASMA_AMMO = 134217728;
    public static int RIT_MULTI_ROCKETS = 268435456;
    public static int RIT_SHIELD = 536870912;
    public static int RIT_ANTIGRAV = 1073741824;
    public static int RIT_SUPERHEALTH = -2147483648;

    public static int HIT_PROXIMITY_GUN_BIT = 16;
    public static int HIT_MJOLNIR_BIT = 7;
    public static int HIT_LASER_CANNON_BIT = 23;
    public static int HIT_PROXIMITY_GUN = (1 << HIT_PROXIMITY_GUN_BIT);
    public static int HIT_MJOLNIR = (1 << HIT_MJOLNIR_BIT);
    public static int HIT_LASER_CANNON = (1 << HIT_LASER_CANNON_BIT);
    public static int HIT_WETSUIT = (1 << (23 + 2));
    public static int HIT_EMPATHY_SHIELDS = (1 << (23 + 3));

    // gl_rmain.cs

    // anorms.cs & r_part.cs
    public const int MAX_PARTICLES = 2048;
    public const int ABSOLUTE_MIN_PARTICLES = 512;
    public const int NUMVERTEXNORMALS = 162;

    // gl_rsurf.cs
    public const double COLINEAR_EPSILON = 0.001;

    // vid.cs
    public const int WARP_WIDTH = 320;
    public const int WARP_HEIGHT = 200;
    public const int VID_CBITS = 6;
    public const int VID_GRADES = (1 << VID_CBITS);
    public const int VID_ROW_SIZE = 3;

    // view.cs
    public static readonly Vector3 SmallOffset = Vector3.One / 32f;

    // wad.cs
    public const int CMP_NONE = 0;
    public const int CMP_LZSS = 1;
    public const int TYP_NONE = 0;
    public const int TYP_LABEL = 1;
    public const int TYP_LUMPY = 64;
    public const int TYP_PALETTE = 64;
    public const int TYP_QTEX = 65;
    public const int TYP_QPIC = 66;
    public const int TYP_SOUND = 67;
    public const int TYP_MIPTEX = 68;

    // crc.cs

    public const ushort CRC_INIT_VALUE = 0xffff;
    public const ushort CRC_XOR_VALUE = 0x0000;
    public static readonly ushort[] _CrcTable = new ushort[]
    {
        0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50a5, 0x60c6, 0x70e7,
        0x8108, 0x9129, 0xa14a, 0xb16b, 0xc18c, 0xd1ad, 0xe1ce, 0xf1ef,
        0x1231, 0x0210, 0x3273, 0x2252, 0x52b5, 0x4294, 0x72f7, 0x62d6,
        0x9339, 0x8318, 0xb37b, 0xa35a, 0xd3bd, 0xc39c, 0xf3ff, 0xe3de,
        0x2462, 0x3443, 0x0420, 0x1401, 0x64e6, 0x74c7, 0x44a4, 0x5485,
        0xa56a, 0xb54b, 0x8528, 0x9509, 0xe5ee, 0xf5cf, 0xc5ac, 0xd58d,
        0x3653, 0x2672, 0x1611, 0x0630, 0x76d7, 0x66f6, 0x5695, 0x46b4,
        0xb75b, 0xa77a, 0x9719, 0x8738, 0xf7df, 0xe7fe, 0xd79d, 0xc7bc,
        0x48c4, 0x58e5, 0x6886, 0x78a7, 0x0840, 0x1861, 0x2802, 0x3823,
        0xc9cc, 0xd9ed, 0xe98e, 0xf9af, 0x8948, 0x9969, 0xa90a, 0xb92b,
        0x5af5, 0x4ad4, 0x7ab7, 0x6a96, 0x1a71, 0x0a50, 0x3a33, 0x2a12,
        0xdbfd, 0xcbdc, 0xfbbf, 0xeb9e, 0x9b79, 0x8b58, 0xbb3b, 0xab1a,
        0x6ca6, 0x7c87, 0x4ce4, 0x5cc5, 0x2c22, 0x3c03, 0x0c60, 0x1c41,
        0xedae, 0xfd8f, 0xcdec, 0xddcd, 0xad2a, 0xbd0b, 0x8d68, 0x9d49,
        0x7e97, 0x6eb6, 0x5ed5, 0x4ef4, 0x3e13, 0x2e32, 0x1e51, 0x0e70,
        0xff9f, 0xefbe, 0xdfdd, 0xcffc, 0xbf1b, 0xaf3a, 0x9f59, 0x8f78,
        0x9188, 0x81a9, 0xb1ca, 0xa1eb, 0xd10c, 0xc12d, 0xf14e, 0xe16f,
        0x1080, 0x00a1, 0x30c2, 0x20e3, 0x5004, 0x4025, 0x7046, 0x6067,
        0x83b9, 0x9398, 0xa3fb, 0xb3da, 0xc33d, 0xd31c, 0xe37f, 0xf35e,
        0x02b1, 0x1290, 0x22f3, 0x32d2, 0x4235, 0x5214, 0x6277, 0x7256,
        0xb5ea, 0xa5cb, 0x95a8, 0x8589, 0xf56e, 0xe54f, 0xd52c, 0xc50d,
        0x34e2, 0x24c3, 0x14a0, 0x0481, 0x7466, 0x6447, 0x5424, 0x4405,
        0xa7db, 0xb7fa, 0x8799, 0x97b8, 0xe75f, 0xf77e, 0xc71d, 0xd73c,
        0x26d3, 0x36f2, 0x0691, 0x16b0, 0x6657, 0x7676, 0x4615, 0x5634,
        0xd94c, 0xc96d, 0xf90e, 0xe92f, 0x99c8, 0x89e9, 0xb98a, 0xa9ab,
        0x5844, 0x4865, 0x7806, 0x6827, 0x18c0, 0x08e1, 0x3882, 0x28a3,
        0xcb7d, 0xdb5c, 0xeb3f, 0xfb1e, 0x8bf9, 0x9bd8, 0xabbb, 0xbb9a,
        0x4a75, 0x5a54, 0x6a37, 0x7a16, 0x0af1, 0x1ad0, 0x2ab3, 0x3a92,
        0xfd2e, 0xed0f, 0xdd6c, 0xcd4d, 0xbdaa, 0xad8b, 0x9de8, 0x8dc9,
        0x7c26, 0x6c07, 0x5c64, 0x4c45, 0x3ca2, 0x2c83, 0x1ce0, 0x0cc1,
        0xef1f, 0xff3e, 0xcf5d, 0xdf7c, 0xaf9b, 0xbfba, 0x8fd9, 0x9ff8,
        0x6e17, 0x7e36, 0x4e55, 0x5e74, 0x2e93, 0x3eb2, 0x0ed1, 0x1ef0
    };

    // sbar.cs
    public const int STAT_MINUS = 10;  // num frame for '-' stats digit
    public const int SBAR_HEIGHT = 24;
}


#region Classes
public static class Mci
{
    [DllImport("winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true)]
    public static extern int Open(IntPtr device, int cmd, int flags, ref MCI_OPEN_PARMS p);

    [DllImport("winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true)]
    public static extern int Set(IntPtr device, int cmd, int flags, ref MCI_SET_PARMS p);

    [DllImport("winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true)]
    public static extern int Play(IntPtr device, int cmd, int flags, ref MCI_PLAY_PARMS p);

    [DllImport("winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true)]
    public static extern int Status(IntPtr device, int cmd, int flags, ref MCI_STATUS_PARMS p);

    [DllImport("winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true)]
    public static extern int SendCommand(IntPtr device, int cmd, int flags, IntPtr p);

    [DllImport("winmm.dll", EntryPoint = "mciSendCommandA", ExactSpelling = true)]
    public static extern int SendCommand(IntPtr device, int cmd, int flags, ref MCI_GENERIC_PARMS p);

    public const int MCI_OPEN = 0x0803;
    public const int MCI_CLOSE = 0x0804;
    public const int MCI_PLAY = 0x0806;
    public const int MCI_STOP = 0x0808;
    public const int MCI_PAUSE = 0x0809;
    public const int MCI_SET = 0x080D;
    public const int MCI_STATUS = 0x0814;
    public const int MCI_RESUME = 0x0855;

    // Flags for MCI Play command
    public const int MCI_OPEN_SHAREABLE = 0x00000100;
    public const int MCI_OPEN_ELEMENT = 0x00000200;
    public const int MCI_OPEN_TYPE_ID = 0x00001000;
    public const int MCI_OPEN_TYPE = 0x00002000;

    // Constants used to specify MCI time formats
    public const int MCI_FORMAT_TMSF = 10;

    // Flags for MCI Set command
    public const int MCI_SET_DOOR_OPEN = 0x00000100;
    public const int MCI_SET_DOOR_CLOSED = 0x00000200;
    public const int MCI_SET_TIME_FORMAT = 0x00000400;

    // Flags for MCI commands
    public const int MCI_NOTIFY = 0x00000001;
    public const int MCI_WAIT = 0x00000002;
    public const int MCI_FROM = 0x00000004;
    public const int MCI_TO = 0x00000008;
    public const int MCI_TRACK = 0x00000010;

    // Flags for MCI Status command
    public const int MCI_STATUS_ITEM = 0x00000100;
    public const int MCI_STATUS_LENGTH = 0x00000001;
    public const int MCI_STATUS_POSITION = 0x00000002;
    public const int MCI_STATUS_NUMBER_OF_TRACKS = 0x00000003;
    public const int MCI_STATUS_MODE = 0x00000004;
    public const int MCI_STATUS_MEDIA_PRESENT = 0x00000005;
    public const int MCI_STATUS_TIME_FORMAT = 0x00000006;
    public const int MCI_STATUS_READY = 0x00000007;
    public const int MCI_STATUS_CURRENT_TRACK = 0x00000008;

    public const int MCI_CD_OFFSET = 1088;

    public const int MCI_CDA_STATUS_TYPE_TRACK = 0x00004001;
    public const int MCI_CDA_TRACK_AUDIO = MCI_CD_OFFSET + 0;
    public const int MCI_CDA_TRACK_OTHER = MCI_CD_OFFSET + 1;

    public const int MCI_NOTIFY_SUCCESSFUL = 1;
    public const int MCI_NOTIFY_SUPERSEDED = 2;
    public const int MCI_NOTIFY_ABORTED = 4;
    public const int MCI_NOTIFY_FAILURE = 8;

    public const int MM_MCINOTIFY = 0x3B9;

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct MCI_OPEN_PARMS
    {
        public IntPtr dwCallback;
        public IntPtr wDeviceID;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpstrDeviceType;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpstrElementName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string lpstrAlias;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MCI_SET_PARMS
    {
        public IntPtr dwCallback;
        public uint dwTimeFormat;
        public uint dwAudio;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MCI_GENERIC_PARMS
    {
        public IntPtr dwCallback;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MCI_PLAY_PARMS
    {
        public IntPtr dwCallback;
        public uint dwFrom;
        public uint dwTo;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MCI_STATUS_PARMS
    {
        public IntPtr dwCallback;
        public uint dwReturn;
        public uint dwItem;
        public uint dwTrack;
    }
    public static uint MCI_MAKE_TMSF(int t, int m, int s, int f)
    {
        return (uint)(((byte)t | ((uint)m << 8)) | (((uint)(byte)s | ((uint)f << 8)) << 16));
    }
}

public class NullCDAudioController : ICDAudioController
{
    byte[] _Remap;

    public NullCDAudioController()
    {
        _Remap = new byte[100];
    }
    public bool IsInitialized
    {
        get { return true; }
    }
    public bool IsEnabled
    {
        get { return true; }
        set { }
    }
    public bool IsPlaying
    {
        get { return false; }
    }
    public bool IsPaused
    {
        get { return false; }
    }
    public bool IsValidCD
    {
        get { return false; }
    }
    public bool IsLooping
    {
        get { return false; }
    }
    public byte[] Remap
    {
        get { return _Remap; }
    }
    public byte MaxTrack
    {
        get { return 0; }
    }
    public byte CurrentTrack
    {
        get { return 0; }
    }
    public float Volume
    {
        get { return 0; }
        set
        {
        }
    }
    public void Init()
    {
    }
    public void Play(byte track, bool looping)
    {
    }
    public void Stop()
    {
    }
    public void Pause()
    {
    }
    public void Resume()
    {
    }
    public void Shutdown()
    {
    }
    public void Update()
    {
    }
    public void CDAudio_GetAudioDiskInfo()
    {
    }
    public void CloseDoor()
    {
    }
    public void Edject()
    {
    }
}

public class scoreboard_t
{
    public string name;
    public int frags;
    public int colors;
    public byte[] translations;

    public scoreboard_t()
    {
        this.translations = new byte[q_shared.VID_GRADES * 256];
    }
}

public class cshift_t
{
    public int[] destcolor; // [3];
    public int percent;		// 0-256

    public cshift_t()
    {
        destcolor = new int[3];
    }

    public cshift_t(int[] destColor, int percent)
    {
        if (destColor.Length != 3)
        {
            throw new ArgumentException("destColor must have length of 3 elements!");
        }
        this.destcolor = destColor;
        this.percent = percent;
    }

    public void Clear()
    {
        this.destcolor[0] = 0;
        this.destcolor[1] = 0;
        this.destcolor[2] = 0;
        this.percent = 0;
    }
}

public class dlight_t
{
    public Vector3 origin;
    public float radius;
    public float die;				// stop lighting after this time
    public float decay;				// drop this each second
    public float minlight;			// don't add when contributing less
    public int key;

    public void Clear()
    {
        this.origin = Vector3.Zero;
        this.radius = 0;
        this.die = 0;
        this.decay = 0;
        this.minlight = 0;
        this.key = 0;
    }
}

public class beam_t
{
    public int entity;
    public model_t model;
    public float endtime;
    public Vector3 start, end;

    public void Clear()
    {
        this.entity = 0;
        this.model = null;
        this.endtime = 0;
        this.start = Vector3.Zero;
        this.end = Vector3.Zero;
    }
}

public class client_static_t
{
    public cactive_t state;

    // personalization data sent to server	
    public string mapstring; // [MAX_QPATH];
    public string spawnparms;//[MAX_MAPSTRING];	// to restart a level

    // demo loop control
    public int demonum;		// -1 = don't play demos
    public string[] demos; // [MAX_DEMOS][MAX_DEMONAME];		// when not playing

    // demo recording info must be here, because record is started before
    // entering a map (and clearing client_state_t)
    public bool demorecording;
    public bool demoplayback;
    public bool timedemo;
    public int forcetrack;			// -1 = use normal cd track
    public IDisposable demofile; // DisposableWrapper<BinaryReader|BinaryWriter> // FILE*
    public int td_lastframe;		// to meter out one message a frame
    public int td_startframe;		// host_framecount at start
    public float td_starttime;		// realtime at second frame of timedemo


    // connection information
    public int signon;			// 0 to SIGNONS
    public qsocket_t netcon; // qsocket_t	*netcon;
    public MsgWriter message; // sizebuf_t	message;		// writing buffer to send to server

    public client_static_t()
    {
        this.demos = new string[q_shared.MAX_DEMOS];
        this.message = new MsgWriter(1024); // like in Client_Init()
    }
}

public class client_state_t
{
    public int movemessages;	// since connecting to this server
    // throw out the first couple, so the player
    // doesn't accidentally do something the 
    // first frame
    public usercmd_t cmd;			// last command sent to the server

    // information for local display
    public int[] stats; //[MAX_CL_STATS];	// health, etc
    public int items;			// inventory bit flags
    public float[] item_gettime; //[32];	// cl.time of aquiring item, for blinking
    public float faceanimtime;	// use anim frame if cl.time < this

    public cshift_t[] cshifts; //[NUM_CSHIFTS];	// color shifts for damage, powerups
    public cshift_t[] prev_cshifts; //[NUM_CSHIFTS];	// and content types

    // the client maintains its own idea of view angles, which are
    // sent to the server each frame.  The server sets punchangle when
    // the view is temporarliy offset, and an angle reset commands at the start
    // of each level and after teleporting.
    public Vector3[] mviewangles; //[2];	// during demo playback viewangles is lerped
    // between these
    public Vector3 viewangles;
    public Vector3[] mvelocity; //[2];	// update by server, used for lean+bob
    // (0 is newest)
    public Vector3 velocity;		// lerped between mvelocity[0] and [1]
    public Vector3 punchangle;		// temporary offset

    // pitch drifting vars
    public float idealpitch;
    public float pitchvel;
    public bool nodrift;
    public float driftmove;
    public double laststop;

    public float viewheight;
    public float crouch;			// local amount for smoothing stepups

    public bool paused;			// send over by server
    public bool onground;
    public bool inwater;

    public int intermission;	// don't change view angle, full screen, etc
    public int completed_time;	// latched at intermission start

    public double[] mtime; //[2];		// the timestamp of last two messages	
    public double time;			// clients view of time, should be between
    // servertime and oldservertime to generate
    // a lerp point for other data
    public double oldtime;		// previous cl.time, time-oldtime is used
    // to decay light values and smooth step ups


    public float last_received_message;	// (realtime) for net trouble icon

    //
    // information that is static for the entire time connected to a server
    //
    public model_t[] model_precache; // [MAX_MODELS];
    public sfx_t[] sound_precache; // [MAX_SOUNDS];

    public string levelname; // char[40];	// for display on solo scoreboard
    public int viewentity;		// cl_entitites[cl.viewentity] = player
    public int maxclients;
    public int gametype;

    // refresh related state
    public model_t worldmodel;	// cl_entitites[0].model
    public efrag_t free_efrags; // first free efrag in list
    public int num_entities;	// held in cl_entities array
    public int num_statics;	// held in cl_staticentities array
    public entity_t viewent;			// the gun model

    public int cdtrack, looptrack;	// cd audio

    // frag scoreboard
    public scoreboard_t[] scores;		// [cl.maxclients]

    public client_state_t()
    {
        this.stats = new int[q_shared.MAX_CL_STATS];
        this.item_gettime = new float[32]; // ???????????

        this.cshifts = new cshift_t[q_shared.NUM_CSHIFTS];
        for (int i = 0; i < q_shared.NUM_CSHIFTS; i++)
            this.cshifts[i] = new cshift_t();

        this.prev_cshifts = new cshift_t[q_shared.NUM_CSHIFTS];
        for (int i = 0; i < q_shared.NUM_CSHIFTS; i++)
            this.prev_cshifts[i] = new cshift_t();

        this.mviewangles = new Vector3[2]; //??????
        this.mvelocity = new Vector3[2];
        this.mtime = new double[2];
        this.model_precache = new model_t[q_shared.MAX_MODELS];
        this.sound_precache = new sfx_t[q_shared.MAX_SOUNDS];
        this.viewent = new entity_t();
    }

    public bool HasItems(int item)
    {
        return (this.items & item) == item;
    }

    public void Clear()
    {
        this.movemessages = 0;
        this.cmd.Clear();
        Array.Clear(this.stats, 0, this.stats.Length);
        this.items = 0;
        Array.Clear(this.item_gettime, 0, this.item_gettime.Length);
        this.faceanimtime = 0;

        foreach (cshift_t cs in this.cshifts)
            cs.Clear();
        foreach (cshift_t cs in this.prev_cshifts)
            cs.Clear();

        this.mviewangles[0] = Vector3.Zero;
        this.mviewangles[1] = Vector3.Zero;
        this.viewangles = Vector3.Zero;
        this.mvelocity[0] = Vector3.Zero;
        this.mvelocity[1] = Vector3.Zero;
        this.velocity = Vector3.Zero;
        this.punchangle = Vector3.Zero;

        this.idealpitch = 0;
        this.pitchvel = 0;
        this.nodrift = false;
        this.driftmove = 0;
        this.laststop = 0;

        this.viewheight = 0;
        this.crouch = 0;

        this.paused = false;
        this.onground = false;
        this.inwater = false;

        this.intermission = 0;
        this.completed_time = 0;

        this.mtime[0] = 0;
        this.mtime[1] = 0;
        this.time = 0;
        this.oldtime = 0;
        this.last_received_message = 0;

        Array.Clear(this.model_precache, 0, this.model_precache.Length);
        Array.Clear(this.sound_precache, 0, this.sound_precache.Length);

        this.levelname = null;
        this.viewentity = 0;
        this.maxclients = 0;
        this.gametype = 0;

        this.worldmodel = null;
        this.free_efrags = null;
        this.num_entities = 0;
        this.num_statics = 0;
        this.viewent.Clear();

        this.cdtrack = 0;
        this.looptrack = 0;

        this.scores = null;
    }
}

public class model_t
{
    public string name; // char		name[MAX_QPATH];
    public bool needload;		// bmodels and sprites don't cache normally

    public modtype_t type;
    public int numframes;
    public synctype_t synctype;

    public int flags;

    //
    // volume occupied by the model graphics
    //		
    public Vector3 mins, maxs;
    public float radius;

    //
    // solid volume for clipping 
    //
    public bool clipbox;
    public Vector3 clipmins, clipmaxs;

    //
    // brush model
    //
    public int firstmodelsurface, nummodelsurfaces;

    public int numsubmodels;
    public dmodel_t[] submodels;

    public int numplanes;
    public mplane_t[] planes; // mplane_t*

    public int numleafs;		// number of visible leafs, not counting 0
    public mleaf_t[] leafs; // mleaf_t*

    public int numvertexes;
    public mvertex_t[] vertexes; // mvertex_t*

    public int numedges;
    public medge_t[] edges; // medge_t*

    public int numnodes;
    public mnode_t[] nodes; // mnode_t *nodes;

    public int numtexinfo;
    public mtexinfo_t[] texinfo;

    public int numsurfaces;
    public msurface_t[] surfaces;

    public int numsurfedges;
    public int[] surfedges; // int *surfedges;

    public int numclipnodes;
    public dclipnode_t[] clipnodes; // public dclipnode_t* clipnodes;

    public int nummarksurfaces;
    public msurface_t[] marksurfaces; // msurface_t **marksurfaces;

    public hull_t[] hulls; // [MAX_MAP_HULLS];

    public int numtextures;
    public texture_t[] textures; // texture_t	**textures;

    public byte[] visdata; // byte *visdata;
    public byte[] lightdata; // byte		*lightdata;
    public string entities; // char		*entities

    //
    // additional model data
    //
    public cache_user_t cache; // cache_user_t	cache		// only access through Mod_Extradata

    public model_t()
    {
        this.hulls = new hull_t[q_shared.MAX_MAP_HULLS];
        for (int i = 0; i < this.hulls.Length; i++)
            this.hulls[i] = new hull_t();
    }

    public void Clear()
    {
        this.name = null;
        this.needload = false;
        this.type = 0;
        this.numframes = 0;
        this.synctype = 0;
        this.flags = 0;
        this.mins = Vector3.Zero;
        this.maxs = Vector3.Zero;
        this.radius = 0;
        this.clipbox = false;
        this.clipmins = Vector3.Zero;
        this.clipmaxs = Vector3.Zero;
        this.firstmodelsurface = 0;
        this.nummodelsurfaces = 0;

        this.numsubmodels = 0;
        this.submodels = null;

        this.numplanes = 0;
        this.planes = null;

        this.numleafs = 0;
        this.leafs = null;

        this.numvertexes = 0;
        this.vertexes = null;

        this.numedges = 0;
        this.edges = null;

        this.numnodes = 0;
        this.nodes = null;

        this.numtexinfo = 0;
        this.texinfo = null;

        this.numsurfaces = 0;
        this.surfaces = null;

        this.numsurfedges = 0;
        this.surfedges = null;

        this.numclipnodes = 0;
        this.clipnodes = null;

        this.nummarksurfaces = 0;
        this.marksurfaces = null;

        foreach (hull_t h in this.hulls)
            h.Clear();

        this.numtextures = 0;
        this.textures = null;

        this.visdata = null;
        this.lightdata = null;
        this.entities = null;

        this.cache = null;
    }

    public void CopyFrom(model_t src)
    {
        this.name = src.name;
        this.needload = src.needload;
        this.type = src.type;
        this.numframes = src.numframes;
        this.synctype = src.synctype;
        this.flags = src.flags;
        this.mins = src.mins;
        this.maxs = src.maxs;
        this.radius = src.radius;
        this.clipbox = src.clipbox;
        this.clipmins = src.clipmins;
        this.clipmaxs = src.clipmaxs;
        this.firstmodelsurface = src.firstmodelsurface;
        this.nummodelsurfaces = src.nummodelsurfaces;

        this.numsubmodels = src.numsubmodels;
        this.submodels = src.submodels;

        this.numplanes = src.numplanes;
        this.planes = src.planes;

        this.numleafs = src.numleafs;
        this.leafs = src.leafs;

        this.numvertexes = src.numvertexes;
        this.vertexes = src.vertexes;

        this.numedges = src.numedges;
        this.edges = src.edges;

        this.numnodes = src.numnodes;
        this.nodes = src.nodes;

        this.numtexinfo = src.numtexinfo;
        this.texinfo = src.texinfo;

        this.numsurfaces = src.numsurfaces;
        this.surfaces = src.surfaces;

        this.numsurfedges = src.numsurfedges;
        this.surfedges = src.surfedges;

        this.numclipnodes = src.numclipnodes;
        this.clipnodes = src.clipnodes;

        this.nummarksurfaces = src.nummarksurfaces;
        this.marksurfaces = src.marksurfaces;

        for (int i = 0; i < src.hulls.Length; i++)
        {
            this.hulls[i].CopyFrom(src.hulls[i]);
        }

        this.numtextures = src.numtextures;
        this.textures = src.textures;

        this.visdata = src.visdata;
        this.lightdata = src.lightdata;
        this.entities = src.entities;

        this.cache = src.cache;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class aliashdr_t
{
    public int ident;
    public int version;
    public Vector3 scale;
    public Vector3 scale_origin;
    public float boundingradius;
    public Vector3 eyeposition;
    public int numskins;
    public int skinwidth;
    public int skinheight;
    public int numverts;
    public int numtris;
    public int numframes;
    public synctype_t synctype;
    public int flags;
    public float size;

    public int numposes;
    public int poseverts;
    /// <summary>
    /// Changed from int offset from this header to posedata to
    /// trivertx_t array
    /// </summary>
    public trivertx_t[] posedata;	// numposes*poseverts trivert_t
    /// <summary>
    /// Changed from int offset from this header to commands data
    /// to commands array
    /// </summary>
    public int[] commands;	// gl command list with embedded s/t
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = (q_shared.MAX_SKINS * 4))]
    public int[,] gl_texturenum; // int gl_texturenum[MAX_SKINS][4];
    /// <summary>
    /// Changed from integers (offsets from this header start) to objects to hold pointers to arrays of byte
    /// </summary>
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.MAX_SKINS)]
    public object[] texels; // int texels[MAX_SKINS];	// only for player skins
    public maliasframedesc_t[] frames; // maliasframedesc_t	frames[1];	// variable sized

    public static int SizeInBytes = Marshal.SizeOf(typeof(aliashdr_t));

    public aliashdr_t()
    {
        this.gl_texturenum = new int[q_shared.MAX_SKINS, 4];//[];
        this.texels = new object[q_shared.MAX_SKINS];
    }
}

public class mplane_t
{
    public Vector3 normal;
    public float dist;
    public byte type;			// for texture axis selection and fast side tests
    public byte signbits;		// signx + signy<<1 + signz<<1
}

public class texture_t
{
    public string name; // char[16];
    public uint width, height;
    public int gl_texturenum;
    public msurface_t texturechain;	// for gl_texsort drawing
    public int anim_total;				// total tenths in sequence ( 0 = no)
    public int anim_min, anim_max;		// time for this frame min <=time< max
    public texture_t anim_next;		// in the animation sequence
    public texture_t alternate_anims;	// bmodels in frmae 1 use these
    public int[] offsets; //[MIPLEVELS];		// four mip maps stored
    public byte[] pixels; // added by Uze

    public texture_t()
    {
        offsets = new int[q_shared.MIPLEVELS];
    }
}

public class mtexinfo_t
{
    public Vector4[] vecs; //public float[][] vecs; //[2][4];
    public float mipadjust;
    public texture_t texture;
    public int flags;

    public mtexinfo_t()
    {
        vecs = new Vector4[2];// float[2][] { new float[4], new float[4] };
    }
}

public class glpoly_t
{
    public glpoly_t next;
    public glpoly_t chain;
    public int numverts;
    public int flags;			// for SURF_UNDERWATER
    /// <summary>
    /// Changed! Original Quake glpoly_t has 4 vertex inplace and others immidiately after this struct
    /// Now all vertices are in verts array of size [numverts,VERTEXSIZE]
    /// </summary>
    public float[][] verts; //[4][VERTEXSIZE];	// variable sized (xyz s1t1 s2t2)

    public void Clear()
    {
        this.next = null;
        this.chain = null;
        this.numverts = 0;
        this.flags = 0;
        this.verts = null;
    }

    public void AllocVerts(int count)
    {
        this.numverts = count;
        this.verts = new float[count][];
        for (int i = 0; i < count; i++)
            this.verts[i] = new float[q_shared.VERTEXSIZE];
    }
}

public class msurface_t
{
    public int visframe;		// should be drawn when node is crossed

    public mplane_t plane;
    public int flags;

    public int firstedge;	// look up in model->surfedges[], negative numbers
    public int numedges;	// are backwards edges

    public short[] texturemins; //[2];
    public short[] extents; //[2];

    public int light_s, light_t;	// gl lightmap coordinates

    public glpoly_t polys;			// multiple if warped
    public msurface_t texturechain;

    public mtexinfo_t texinfo;

    // lighting info
    public int dlightframe;
    public int dlightbits;

    public int lightmaptexturenum;
    public byte[] styles; //[MAXLIGHTMAPS];
    public int[] cached_light; //[MAXLIGHTMAPS];	// values currently used in lightmap
    public bool cached_dlight;				// true if dynamic light in cache
    /// <summary>
    /// Former "samples" field. Use in pair with sampleofs field!!!
    /// </summary>
    public byte[] sample_base;		// [numstyles*surfsize]
    public int sampleofs; // added by Uze. In original Quake samples = loadmodel->lightdata + offset;
    // now samples = loadmodel->lightdata;

    public msurface_t()
    {
        texturemins = new short[2];
        extents = new short[2];
        styles = new byte[q_shared.MAXLIGHTMAPS];
        cached_light = new int[q_shared.MAXLIGHTMAPS];
        // samples is allocated when needed
    }
}

public class mnodebase_t
{
    public int contents;		// 0 for mnode_t and negative for mleaf_t
    public int visframe;		// node needs to be traversed if current
    public Vector3 mins;
    public Vector3 maxs;
    //public float[] minmaxs; //[6];		// for bounding box culling
    public mnode_t parent;

    //public mnodebase_t()
    //{
    //    this.minmaxs = new float[6];
    //}
}

public class mnode_t : mnodebase_t
{
    // node specific
    public mplane_t plane;
    public mnodebase_t[] children; //[2];	

    public ushort firstsurface;
    public ushort numsurfaces;

    public mnode_t()
    {
        this.children = new mnodebase_t[2];
    }
}

public class mleaf_t : mnodebase_t
{
    // leaf specific
    /// <summary>
    /// loadmodel->visdata
    /// Use in pair with visofs!
    /// </summary>
    public byte[] compressed_vis; // byte*
    public int visofs; // added by Uze
    public efrag_t efrags;

    /// <summary>
    /// loadmodel->marksurfaces
    /// </summary>
    public msurface_t[] marksurfaces;
    public int firstmarksurface; // msurface_t	**firstmarksurface;
    public int nummarksurfaces;
    //public int key;			// BSP sequence number for leaf's contents
    public byte[] ambient_sound_level; // [NUM_AMBIENTS];

    public mleaf_t()
    {
        this.ambient_sound_level = new byte[q_shared.NUM_AMBIENTS];
    }
}

public class hull_t
{
    public dclipnode_t[] clipnodes;
    public mplane_t[] planes;
    public int firstclipnode;
    public int lastclipnode;
    public Vector3 clip_mins;
    public Vector3 clip_maxs;

    public void Clear()
    {
        this.clipnodes = null;
        this.planes = null;
        this.firstclipnode = 0;
        this.lastclipnode = 0;
        this.clip_mins = Vector3.Zero;
        this.clip_maxs = Vector3.Zero;
    }

    public void CopyFrom(hull_t src)
    {
        this.clipnodes = src.clipnodes;
        this.planes = src.planes;
        this.firstclipnode = src.firstclipnode;
        this.lastclipnode = src.lastclipnode;
        this.clip_mins = src.clip_mins;
        this.clip_maxs = src.clip_maxs;
    }
}

public class mspriteframe_t
{
    public int width;
    public int height;
    public float up, down, left, right;
    public int gl_texturenum;
}

public class mspritegroup_t
{
    public int numframes;
    public float[] intervals; // float*
    public mspriteframe_t[] frames; // mspriteframe_t	*frames[1];
}

public class msprite_t
{
    public int type;
    public int maxwidth;
    public int maxheight;
    public int numframes;
    public float beamlength;		// remove?
    //void				*cachespot;		// remove?
    public mspriteframedesc_t[] frames; // mspriteframedesc_t	frames[1];
}

public class qsocket_t
{
    public double connecttime;
    public double lastMessageTime;
    public double lastSendTime;

    public bool disconnected;
    public bool canSend;
    public bool sendNext;

    public int driver;
    public int landriver;
    public Socket socket;
    public object driverdata;

    public uint ackSequence;
    public uint sendSequence;
    public uint unreliableSendSequence;

    public int sendMessageLength;
    public byte[] sendMessage;

    public uint receiveSequence;
    public uint unreliableReceiveSequence;

    public int receiveMessageLength;
    public byte[] receiveMessage;

    public EndPoint addr;
    public string address;

    public qsocket_t()
    {
        this.sendMessage = new byte[q_shared.NET_MAXMESSAGE];
        this.receiveMessage = new byte[q_shared.NET_MAXMESSAGE];
        disconnected = true;
    }

    public void ClearBuffers()
    {
        this.sendMessageLength = 0;
        this.receiveMessageLength = 0;
    }

    public INetLanDriver LanDriver
    {
        get { return game_engine.net_landrivers[this.landriver]; }
    }

    public int Read(byte[] buf, int len, ref EndPoint ep)
    {
        return this.LanDriver.Read(this.socket, buf, len, ref ep);
    }

    public int Write(byte[] buf, int len, EndPoint ep)
    {
        return this.LanDriver.Write(this.socket, buf, len, ep);
    }
}

public class PollProcedure
{
    public PollProcedure next;
    public double nextTime;
    public PollHandler procedure; // void (*procedure)();
    public object arg; // void *arg

    public PollProcedure(PollProcedure next, double nextTime, PollHandler handler, object arg)
    {
        this.next = next;
        this.nextTime = nextTime;
        this.procedure = handler;
        this.arg = arg;
    }
}

public class hostcache_t
{
    public string name;
    public string map;
    public string cname;
    public int users;
    public int maxusers;
    public int driver;
    public int ldriver;
    public EndPoint addr;
}

public class NetVcr : INetDriver
{
    VcrRecord _Next;
    bool _IsInitialized;

    #region INetDriver Members

    public string Name
    {
        get { return "VCR"; }
    }

    public bool IsInitialized
    {
        get { return _IsInitialized; }
    }

    public void Init()
    {
        _Next = game_engine.ReadStructure<VcrRecord>(game_engine._VcrReader.BaseStream);
        _IsInitialized = true;
    }

    public void Datagram_Listen(bool state)
    {
        // nothing to do
    }

    public void Datagram_SearchForHosts(bool xmit)
    {
        // nothing to do
    }

    public qsocket_t Datagram_Connect(string host)
    {
        return null;
    }

    public qsocket_t Datagram_CheckNewConnections()
    {
        if (game_engine.host_time != _Next.time || _Next.op != q_shared.VCR_OP_CONNECT)
            game_engine.Sys_Error("VCR missmatch");

        if (_Next.session == 0)
        {
            ReadNext();
            return null;
        }

        qsocket_t sock = game_engine.NET_NewQSocket();
        sock.driverdata = _Next.session;

        byte[] buf = new byte[q_shared.NET_NAMELEN];
        game_engine._VcrReader.Read(buf, 0, buf.Length);
        sock.address = Encoding.ASCII.GetString(buf);

        ReadNext();

        return sock;
    }

    public int GetMessage(qsocket_t sock)
    {
        if (game_engine.host_time != _Next.time || _Next.op != q_shared.VCR_OP_GETMESSAGE || _Next.session != SocketToSession(sock))
            game_engine.Sys_Error("VCR missmatch");

        int ret = game_engine._VcrReader.ReadInt32();
        if (ret != 1)
        {
            ReadNext();
            return ret;
        }

        int length = game_engine._VcrReader.ReadInt32();
        game_engine.Message.FillFrom(game_engine._VcrReader.BaseStream, length);

        ReadNext();

        return 1;
    }

    /// <summary>
    /// VCR_ReadNext
    /// </summary>
    private void ReadNext()
    {
        try
        {
            _Next = game_engine.ReadStructure<VcrRecord>(game_engine._VcrReader.BaseStream);
        }
        catch (IOException)
        {
            _Next = new VcrRecord();
            _Next.op = 255;
            game_engine.Sys_Error("=== END OF PLAYBACK===\n");
        }
        if (_Next.op < 1 || _Next.op > q_shared.VCR_MAX_MESSAGE)
            game_engine.Sys_Error("VCR_ReadNext: bad op");
    }

    public int Datagram_SendMessage(qsocket_t sock, MsgWriter data)
    {
        if (game_engine.host_time != _Next.time || _Next.op != q_shared.VCR_OP_SENDMESSAGE || _Next.session != SocketToSession(sock))
            game_engine.Sys_Error("VCR missmatch");

        int ret = game_engine._VcrReader.ReadInt32();

        ReadNext();

        return ret;
    }

    public int Datagram_SendUnreliableMessage(qsocket_t sock, MsgWriter data)
    {
        throw new NotImplementedException();
    }

    public bool Datagram_CanSendMessage(qsocket_t sock)
    {
        if (game_engine.host_time != _Next.time || _Next.op != q_shared.VCR_OP_CANSENDMESSAGE || _Next.session != SocketToSession(sock))
            game_engine.Sys_Error("VCR missmatch");

        int ret = game_engine._VcrReader.ReadInt32();

        ReadNext();

        return ret != 0;

    }

    public bool Datagram_CanSendUnreliableMessage(qsocket_t sock)
    {
        return true;
    }

    public void Datagram_Close(qsocket_t sock)
    {
        // nothing to do
    }

    public void Datagram_Shutdown()
    {
        // nothing to do
    }

    #endregion

    public long SocketToSession(qsocket_t sock)
    {
        return (long)sock.driverdata;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class VcrRecord
{
    public double time;
    public int op;
    public long session;

    public static int SizeInBytes = Marshal.SizeOf(typeof(VcrRecord));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class VcrRecord2 : VcrRecord
{
    public int ret;
}

public class efrag_t
{
    public mleaf_t leaf;
    public efrag_t leafnext;
    public entity_t entity;
    public efrag_t entnext;

    public void Clear()
    {
        this.leaf = null;
        this.leafnext = null;
        this.entity = null;
        this.entnext = null;
    }
}

public class entity_t
{
    public bool forcelink;		// model changed
    public int update_type;
    public entity_state_t baseline;		// to fill in defaults in updates
    public double msgtime;		// time of last update
    public Vector3[] msg_origins; //[2];	// last two updates (0 is newest)	
    public Vector3 origin;
    public Vector3[] msg_angles; //[2];	// last two updates (0 is newest)
    public Vector3 angles;
    public model_t model;			// NULL = no model
    public efrag_t efrag;			// linked list of efrags
    public int frame;
    public float syncbase;		// for client-side animations
    public byte[] colormap;
    public int effects;		// light, particals, etc
    public int skinnum;		// for Alias models
    public int visframe;		// last frame this entity was
    //  found in an active leaf

    public int dlightframe;	// dynamic lighting
    public int dlightbits;

    // FIXME: could turn these into a union
    public int trivial_accept;
    public mnode_t topnode;		// for bmodels, first world node
    //  that splits bmodel, or NULL if
    //  not split

    public entity_t()
    {
        msg_origins = new Vector3[2];
        msg_angles = new Vector3[2];
    }

    public void Clear()
    {
        this.forcelink = false;
        this.update_type = 0;

        this.baseline = entity_state_t.Empty;

        this.msgtime = 0;
        this.msg_origins[0] = Vector3.Zero;
        this.msg_origins[1] = Vector3.Zero;

        this.origin = Vector3.Zero;
        this.msg_angles[0] = Vector3.Zero;
        this.msg_angles[1] = Vector3.Zero;
        this.angles = Vector3.Zero;
        this.model = null;
        this.efrag = null;
        this.frame = 0;
        this.syncbase = 0;
        this.colormap = null;
        this.effects = 0;
        this.skinnum = 0;
        this.visframe = 0;

        this.dlightframe = 0;
        this.dlightbits = 0;

        this.trivial_accept = 0;
        this.topnode = null;

    }
}

public class refdef_t
{
    public vrect_t vrect;
    public Vector3 vieworg;
    public Vector3 viewangles;
    public float fov_x, fov_y;
}

public class CacheEntry : cache_user_t
{
    CacheEntry _Prev;
    CacheEntry _Next;
    CacheEntry _LruPrev;
    CacheEntry _LruNext;
    int _Size;

    public CacheEntry Next
    {
        get { return _Next; }
    }
    public CacheEntry Prev
    {
        get { return _Prev; }
    }
    public CacheEntry LruPrev
    {
        get { return _LruPrev; }
    }
    public CacheEntry LruNext
    {
        get { return _LruNext; }
    }

    ~CacheEntry()
    {
        game_engine._BytesAllocated -= _Size;
    }
    public CacheEntry(bool isHead = false)
    {
        if (isHead)
        {
            _Next = this;
            _Prev = this;
            _LruNext = this;
            _LruPrev = this;
        }
    }
    public CacheEntry(int size)
    {
        _Size = size;
        game_engine._BytesAllocated += _Size;
    }
    public void Cache_UnlinkLRU()
    {
        if (_LruNext == null || _LruPrev == null)
            game_engine.Sys_Error("Cache_UnlinkLRU: NULL link");

        _LruNext._LruPrev = _LruPrev;
        _LruPrev._LruNext = _LruNext;
        _LruPrev = _LruNext = null;
    }
    public void LRUInstertAfter(CacheEntry prev)
    {
        if (_LruNext != null || _LruPrev != null)
            game_engine.Sys_Error("Cache_MakeLRU: active link");

        prev._LruNext._LruPrev = this;
        _LruNext = prev._LruNext;
        _LruPrev = prev;
        prev._LruNext = this;
    }
    public void InsertBefore(CacheEntry next)
    {
        _Next = next;
        if (next._Prev != null)
            _Prev = next._Prev;
        else
            _Prev = next;

        if (next._Prev != null)
            next._Prev._Next = this;
        else
            next._Prev = this;
        next._Prev = this;

        if (next._Next == null)
            next._Next = this;
    }
    public void Remove()
    {
        _Prev._Next = _Next;
        _Next._Prev = _Prev;
        _Next = _Prev = null;

        data = null;
        game_engine._BytesAllocated -= _Size;
        _Size = 0;

        Cache_UnlinkLRU();
    }
}

public class cache_user_t
{
    public object data;
}

public class State
{
    public byte[] Buffer;
    public int Count;
}

public class MsgWriter
{

    byte[] _Buffer;
    int _Count;
    Union4b _Val = Union4b.Empty;

    public byte[] Data
    {
        get { return _Buffer; }
    }
    public bool IsEmpty
    {
        get { return (_Count == 0); }
    }
    public int Length
    {
        get { return _Count; }
    }
    public bool AllowOverflow { get; set; }
    public bool IsOveflowed { get; set; }
    public int Capacity
    {
        get { return _Buffer.Length; }
        set { SetBufferSize(value); }
    }


    public MsgWriter()
        : this(0)
    {
    }
    public MsgWriter(int capacity)
    {
        SetBufferSize(capacity);
        this.AllowOverflow = false;
    }
    protected void NeedRoom(int bytes)
    {
        if (_Count + bytes > _Buffer.Length)
        {
            if (!this.AllowOverflow)
                game_engine.Sys_Error("MsgWriter: overflow without allowoverflow set!");

            this.IsOveflowed = true;
            _Count = 0;
            if (bytes > _Buffer.Length)
                game_engine.Sys_Error("MsgWriter: Requested more than whole buffer has!");
        }
    }
    public object GetState()
    {
        object st = null;
        SaveState(ref st);
        return st;
    }
    public void SaveState(ref object state)
    {
        if (state == null)
        {
            state = new State();
        }
        State st = GetState(state);
        if (st.Buffer == null || st.Buffer.Length != _Buffer.Length)
        {
            st.Buffer = new byte[_Buffer.Length];
        }
        Buffer.BlockCopy(_Buffer, 0, st.Buffer, 0, _Buffer.Length);
        st.Count = _Count;
    }
    private State GetState(object state)
    {
        if (state == null)
        {
            throw new ArgumentNullException();
        }
        State st = state as State;
        if (st == null)
        {
            throw new ArgumentException("Passed object is not a state!");
        }
        return st;
    }
    public void RestoreState(object state)
    {
        State st = GetState(state);
        SetBufferSize(st.Buffer.Length);
        Buffer.BlockCopy(st.Buffer, 0, _Buffer, 0, _Buffer.Length);
        _Count = st.Count;
    }
    private void SetBufferSize(int value)
    {
        if (_Buffer != null)
        {
            if (_Buffer.Length == value)
                return;

            Array.Resize(ref _Buffer, value);

            if (_Count > _Buffer.Length)
                _Count = _Buffer.Length;
        }
        else
            _Buffer = new byte[value];
    }
    public void MSG_WriteChar(int c)
    {
#if PARANOID
        if (c < -128 || c > 127)
            Sys.Error("MSG_WriteChar: range error");
#endif
        NeedRoom(1);
        _Buffer[_Count++] = (byte)c;
    }
    public void MSG_WriteByte(int c)
    {
#if PARANOID
        if (c < 0 || c > 255)
            Sys.Error("MSG_WriteByte: range error");
#endif
        NeedRoom(1);
        _Buffer[_Count++] = (byte)c;
    }
    public void MSG_WriteShort(int c)
    {
#if PARANOID
        if (c < short.MinValue || c > short.MaxValue)
            Sys.Error("MSG_WriteShort: range error");
#endif
        NeedRoom(2);
        _Buffer[_Count++] = (byte)(c & 0xff);
        _Buffer[_Count++] = (byte)(c >> 8);
    }
    public void MSG_WriteLong(int c)
    {
        NeedRoom(4);
        _Buffer[_Count++] = (byte)(c & 0xff);
        _Buffer[_Count++] = (byte)((c >> 8) & 0xff);
        _Buffer[_Count++] = (byte)((c >> 16) & 0xff);
        _Buffer[_Count++] = (byte)(c >> 24);

    }
    public void MSG_WriteFloat(float f)
    {
        NeedRoom(4);
        _Val.f0 = f;
        _Val.i0 = game_engine.LittleLong(_Val.i0);

        _Buffer[_Count++] = _Val.b0;
        _Buffer[_Count++] = _Val.b1;
        _Buffer[_Count++] = _Val.b2;
        _Buffer[_Count++] = _Val.b3;
    }
    public void MSG_WriteString(string s)
    {
        int count = 1;
        if (!String.IsNullOrEmpty(s))
            count += s.Length;

        NeedRoom(count);
        for (int i = 0; i < count - 1; i++)
            _Buffer[_Count++] = (byte)s[i];
        _Buffer[_Count++] = 0;
    }
    public void SZ_Print(string s)
    {
        if (_Count > 0 && _Buffer[_Count - 1] == 0)
            _Count--; // remove previous trailing 0
        MSG_WriteString(s);
    }
    public void MSG_WriteCoord(float f)
    {
        MSG_WriteShort((int)(f * 8));
    }
    public void MSG_WriteAngle(float f)
    {
        MSG_WriteByte(((int)f * 256 / 360) & 255);
    }
    public void Write(byte[] src, int offset, int count)
    {
        if (count > 0)
        {
            NeedRoom(count);
            Buffer.BlockCopy(src, offset, _Buffer, _Count, count);
            _Count += count;
        }
    }
    public void Clear()
    {
        _Count = 0;
    }
    public void FillFrom(Stream src, int count)
    {
        Clear();
        NeedRoom(count);
        while (_Count < count)
        {
            int r = src.Read(_Buffer, _Count, count - _Count);
            if (r == 0)
                break;
            _Count += r;
        }
    }
    public void FillFrom(byte[] src, int startIndex, int count)
    {
        Clear();
        NeedRoom(count);
        Buffer.BlockCopy(src, startIndex, _Buffer, 0, count);
        _Count = count;
    }
    public int FillFrom(Socket socket, ref EndPoint ep)
    {
        Clear();
        int result = game_engine.net_landrivers[game_engine.net_landriverlevel].Read(socket, _Buffer, _Buffer.Length, ref ep);
        if (result >= 0)
            _Count = result;
        return result;
    }
    public void AppendFrom(byte[] src, int startIndex, int count)
    {
        NeedRoom(count);
        Buffer.BlockCopy(src, startIndex, _Buffer, _Count, count);
        _Count += count;
    }
}

public class MsgReader
{
    MsgWriter _Source;
    public bool msg_badread;
    public int msg_readcount;
    Union4b _Val;
    char[] _Tmp;

    public MsgReader(MsgWriter source)
    {
        _Source = source;
        _Val = Union4b.Empty;
        _Tmp = new char[2048];
    }
    private bool HasRoom(int bytes)
    {
        if (msg_readcount + bytes > _Source.Length)
        {
            msg_badread = true;
            return false;
        }
        return true;
    }
    public void MSG_BeginReading()
    {
        msg_badread = false;
        msg_readcount = 0;
    }
    public int MSG_ReadChar()
    {
        if (!HasRoom(1))
            return -1;

        return (sbyte)_Source.Data[msg_readcount++];
    }
    public int MSG_ReadByte()
    {
        if (!HasRoom(1))
            return -1;

        return (byte)_Source.Data[msg_readcount++];
    }
    public int MSG_ReadShort()
    {
        if (!HasRoom(2))
            return -1;

        int c = (short)(_Source.Data[msg_readcount + 0] + (_Source.Data[msg_readcount + 1] << 8));
        msg_readcount += 2;
        return c;
    }
    public int MSG_ReadLong()
    {
        if (!HasRoom(4))
            return -1;

        int c = _Source.Data[msg_readcount + 0] +
            (_Source.Data[msg_readcount + 1] << 8) +
            (_Source.Data[msg_readcount + 2] << 16) +
            (_Source.Data[msg_readcount + 3] << 24);

        msg_readcount += 4;
        return c;
    }
    public float MSG_ReadFloat()
    {
        if (!HasRoom(4))
            return 0;

        _Val.b0 = _Source.Data[msg_readcount + 0];
        _Val.b1 = _Source.Data[msg_readcount + 1];
        _Val.b2 = _Source.Data[msg_readcount + 2];
        _Val.b3 = _Source.Data[msg_readcount + 3];

        msg_readcount += 4;

        _Val.i0 = game_engine.LittleLong(_Val.i0);
        return _Val.f0;
    }
    public string MSG_ReadString()
    {
        int l = 0;
        do
        {
            int c = MSG_ReadChar();
            if (c == -1 || c == 0)
                break;
            _Tmp[l] = (char)c;
            l++;
        } while (l < _Tmp.Length - 1);

        return new String(_Tmp, 0, l);
    }
    public float MSG_ReadCoord()
    {
        return MSG_ReadShort() * (1.0f / 8);
    }
    public float MSG_ReadAngle()
    {
        return MSG_ReadChar() * (360.0f / 256);
    }
    public Vector3 ReadCoords()
    {
        Vector3 result;
        result.X = MSG_ReadCoord();
        result.Y = MSG_ReadCoord();
        result.Z = MSG_ReadCoord();
        return result;
    }
    public Vector3 ReadAngles()
    {
        Vector3 result;
        result.X = MSG_ReadAngle();
        result.Y = MSG_ReadAngle();
        result.Z = MSG_ReadAngle();
        return result;
    }
}

public class sfx_t
{
    public string name; // char[MAX_QPATH];
    public cache_user_t cache; // cache_user_t

    public void Clear()
    {
        this.name = null;
        cache = null;
    }
}

public class FloodFiller
{
    // must be a power of 2
    const int FLOODFILL_FIFO_SIZE = 0x1000;
    const int FLOODFILL_FIFO_MASK = FLOODFILL_FIFO_SIZE - 1;

    ByteArraySegment _Skin;
    floodfill_t[] _Fifo;
    int _Width;
    int _Height;
    //int _Offset;
    int _X;
    int _Y;
    int _Fdc;
    byte _FillColor;
    int _Inpt;

    public FloodFiller(ByteArraySegment skin, int skinwidth, int skinheight)
    {
        _Skin = skin;
        _Width = skinwidth;
        _Height = skinheight;
        _Fifo = new floodfill_t[FLOODFILL_FIFO_SIZE];
        _FillColor = _Skin.Data[_Skin.StartIndex]; // *skin; // assume this is the pixel to fill
    }
    public void Perform()
    {
        int filledcolor = 0;
        // attempt to find opaque black
        uint[] t8to24 = game_engine.d_8to24table;
        for (int i = 0; i < 256; ++i)
            if (t8to24[i] == (255 << 0)) // alpha 1.0
            {
                filledcolor = i;
                break;
            }

        // can't fill to filled color or to transparent color (used as visited marker)
        if ((_FillColor == filledcolor) || (_FillColor == 255))
        {
            return;
        }

        int outpt = 0;
        _Inpt = 0;
        _Fifo[_Inpt].x = 0;
        _Fifo[_Inpt].y = 0;
        _Inpt = (_Inpt + 1) & FLOODFILL_FIFO_MASK;

        while (outpt != _Inpt)
        {
            _X = _Fifo[outpt].x;
            _Y = _Fifo[outpt].y;
            _Fdc = filledcolor;
            int offset = _X + _Width * _Y;

            outpt = (outpt + 1) & FLOODFILL_FIFO_MASK;

            if (_X > 0)
                Step(offset - 1, -1, 0);
            if (_X < _Width - 1)
                Step(offset + 1, 1, 0);
            if (_Y > 0)
                Step(offset - _Width, 0, -1);
            if (_Y < _Height - 1)
                Step(offset + _Width, 0, 1);

            _Skin.Data[_Skin.StartIndex + offset] = (byte)_Fdc;
        }
    }
    private void Step(int offset, int dx, int dy)
    {
        byte[] pos = _Skin.Data;
        int off = _Skin.StartIndex + offset;

        if (pos[off] == _FillColor)
        {
            pos[off] = 255;
            _Fifo[_Inpt].x = (short)(_X + dx);
            _Fifo[_Inpt].y = (short)(_Y + dy);
            _Inpt = (_Inpt + 1) & FLOODFILL_FIFO_MASK;
        }
        else if (pos[off] != 255)
            _Fdc = pos[off];
    }
}

public class DisposableWrapper<T> : IDisposable where T : class, IDisposable
{
    T _Object;
    bool _Owned;

    public T Object
    {
        get { return _Object; }
    }

    public DisposableWrapper(T obj, bool dispose)
    {
        _Object = obj;
        _Owned = dispose;
    }

    ~DisposableWrapper()
    {
        Dispose(false);
    }

    private void Dispose(bool disposing)
    {
        if (_Object != null && _Owned)
        {
            _Object.Dispose();
            _Object = null;
        }
    }

    #region IDisposable Members

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    #endregion
}

public class ByteArraySegment
{
    ArraySegment<byte> _Segment;

    public byte[] Data
    {
        get { return _Segment.Array; }
    }
    public int StartIndex
    {
        get { return _Segment.Offset; }
    }
    public int Length
    {
        get { return _Segment.Count; }
    }

    public ByteArraySegment(byte[] array)
        : this(array, 0, -1)
    {
    }

    public ByteArraySegment(byte[] array, int startIndex)
        : this(array, startIndex, -1)
    {
    }

    public ByteArraySegment(byte[] array, int startIndex, int length)
    {
        if (array == null)
        {
            throw new ArgumentNullException("array");
        }
        if (length == -1)
        {
            length = array.Length - startIndex;
        }
        if (length <= 0)
        {
            throw new ArgumentException("Invalid length!");
        }
        _Segment = new ArraySegment<byte>(array, startIndex, length);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class link_t
{
    link_t _Prev, _Next;
    object _Owner;

    public link_t Prev
    {
        get { return _Prev; }
    }
    public link_t Next
    {
        get { return _Next; }
    }
    public object Owner
    {
        get { return _Owner; }
    }

    public link_t(object owner)
    {
        _Owner = owner;
    }

    public void Clear()
    {
        _Prev = _Next = this;
    }

    public void ClearToNulls()
    {
        _Prev = _Next = null;
    }

    public void Remove()
    {
        _Next._Prev = _Prev;
        _Prev._Next = _Next;
        _Next = null;
        _Prev = null;
    }

    public void InsertBefore(link_t before)
    {
        _Next = before;
        _Prev = before._Prev;
        _Prev._Next = this;
        _Next._Prev = this;
    }

    public void InsertAfter(link_t after)
    {
        _Next = after.Next;
        _Prev = after;
        _Prev._Next = this;
        _Next._Prev = this;
    }
}

public class packfile_t
{
    public string name; // [MAX_QPATH];
    public int filepos, filelen;

    public override string ToString()
    {
        return String.Format("{0}, at {1}, {2} bytes}", this.name, this.filepos, this.filelen);
    }
}

public class pack_t
{
    public string filename; // [MAX_OSPATH];
    public BinaryReader stream; //int handle;
                                //int numfiles;
    public packfile_t[] files;

    public pack_t(string filename, BinaryReader reader, packfile_t[] files)
    {
        this.filename = filename;
        this.stream = reader;
        this.files = files;
    }
}

public class searchpath_t
{
    public string filename; // char[MAX_OSPATH];
    public pack_t pack;          // only one of filename / pack will be used

    public searchpath_t(string path)
    {
        if (path.EndsWith(".pak"))
        {
            this.pack = game_engine.COM_LoadPackFile(path);
            if (this.pack == null)
                game_engine.Sys_Error("Couldn't load packfile: {0}", path);
        }
        else
            this.filename = path;
    }

    public searchpath_t(pack_t pak)
    {
        this.pack = pak;
    }
}

public class glmode_t
{
    public string name;
    public TextureMinFilter minimize;
    public TextureMagFilter maximize;

    public glmode_t(string name, TextureMinFilter minFilter, TextureMagFilter magFilter)
    {
        this.name = name;
        this.minimize = minFilter;
        this.maximize = magFilter;
    }
}

public class gltexture_t
{
    public int texnum;
    public string identifier;
    public int width, height;
    public bool mipmap;
}

public class glpic_t
{
    public int width, height;
    public int texnum;
    public float sl, tl, sh, th;

    public glpic_t()
    {
        sl = 0;
        sh = 1;
        tl = 0;
        th = 1;
    }
}

public class cachepic_t
{
    public string name;
    public glpic_t pic;
}

public class edict_t
{
    public bool free;
    public link_t area; // linked to a division node or leaf

    public int num_leafs;
    public short[] leafnums; // [MAX_ENT_LEAFS];

    public entity_state_t baseline;

    public float freetime;			// sv.time when the object was freed
    public entvars_t v;					// C exported fields from progs
    public float[] fields; // other fields from progs

    public edict_t()
    {
        this.area = new link_t(this);
        this.leafnums = new short[q_shared.MAX_ENT_LEAFS];
        this.fields = new float[(game_engine.pr_edict_size - entvars_t.SizeInBytes) >> 2];
    }

    public void Clear()
    {
        this.v = default(entvars_t);
        if (this.fields != null)
            Array.Clear(this.fields, 0, this.fields.Length);
        this.free = false;
    }

    public bool IsV(int offset, out int correctedOffset)
    {
        if (offset < (entvars_t.SizeInBytes >> 2))
        {
            correctedOffset = offset;
            return true;
        }
        correctedOffset = offset - (entvars_t.SizeInBytes >> 2);
        return false;
    }

    public unsafe void LoadInt(int offset, eval_t* result)
    {
        int offset1;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                result->_int = a->_int;
            }
        }
        else
        {
            fixed (void* pv = this.fields)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                result->_int = a->_int;
            }
        }
    }

    public unsafe void StoreInt(int offset, eval_t* value)
    {
        int offset1;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                a->_int = value->_int;
            }
        }
        else
        {
            fixed (void* pv = this.fields)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                a->_int = value->_int;
            }
        }
    }

    public unsafe void LoadVector(int offset, eval_t* result)
    {
        int offset1;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                result->vector[0] = a->vector[0];
                result->vector[1] = a->vector[1];
                result->vector[2] = a->vector[2];
            }
        }
        else
        {
            fixed (void* pf = this.fields)
            {
                eval_t* a = (eval_t*)((int*)pf + offset1);
                result->vector[0] = a->vector[0];
                result->vector[1] = a->vector[1];
                result->vector[2] = a->vector[2];
            }
        }
    }

    public unsafe void StoreVector(int offset, eval_t* value)
    {
        int offset1;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                a->vector[0] = value->vector[0];
                a->vector[1] = value->vector[1];
                a->vector[2] = value->vector[2];
            }
        }
        else
        {
            fixed (void* pf = this.fields)
            {
                eval_t* a = (eval_t*)((int*)pf + offset1);
                a->vector[0] = value->vector[0];
                a->vector[1] = value->vector[1];
                a->vector[2] = value->vector[2];
            }
        }
    }

    public unsafe int GetInt(int offset)
    {
        int offset1, result;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                result = a->_int;
            }
        }
        else
        {
            fixed (void* pv = this.fields)
            {
                eval_t* a = (eval_t*)((int*)pv + offset1);
                result = a->_int;
            }
        }
        return result;
    }

    public unsafe float GetFloat(int offset)
    {
        int offset1;
        float result;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((float*)pv + offset1);
                result = a->_float;
            }
        }
        else
        {
            fixed (void* pv = this.fields)
            {
                eval_t* a = (eval_t*)((float*)pv + offset1);
                result = a->_float;
            }
        }
        return result;
    }

    public unsafe void SetFloat(int offset, float value)
    {
        int offset1;
        if (IsV(offset, out offset1))
        {
            fixed (void* pv = &this.v)
            {
                eval_t* a = (eval_t*)((float*)pv + offset1);
                a->_float = value;
            }
        }
        else
        {
            fixed (void* pv = this.fields)
            {
                eval_t* a = (eval_t*)((float*)pv + offset1);
                a->_float = value;
            }
        }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class ddef_t
{
    public ushort type;		// if DEF_SAVEGLOBGAL bit is set
    // the variable needs to be saved in savegames
    public ushort ofs;
    public int s_name;

    public static int SizeInBytes = Marshal.SizeOf(typeof(ddef_t));

    public void SwapBytes()
    {
        this.type = (ushort)game_engine.LittleShort((short)this.type);
        this.ofs = (ushort)game_engine.LittleShort((short)this.ofs);
        this.s_name = game_engine.LittleLong(this.s_name);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class dfunction_t
{
    public int first_statement;	// negative numbers are builtins
    public int parm_start;
    public int locals;				// total ints of parms + locals

    public int profile;		// runtime

    public int s_name;
    public int s_file;			// source file defined in

    public int numparms;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.MAX_PARMS)]
    public byte[] parm_size; // [MAX_PARMS];

    public static int SizeInBytes = Marshal.SizeOf(typeof(dfunction_t));

    public string FileName
    {
        get { return game_engine.GetString(this.s_file); }
    }
    public string Name
    {
        get { return game_engine.GetString(this.s_name); }
    }

    public void SwapBytes()
    {
        this.first_statement = game_engine.LittleLong(this.first_statement);
        this.parm_start = game_engine.LittleLong(this.parm_start);
        this.locals = game_engine.LittleLong(this.locals);
        this.s_name = game_engine.LittleLong(this.s_name);
        this.s_file = game_engine.LittleLong(this.s_file);
        this.numparms = game_engine.LittleLong(this.numparms);
    }

    public override string ToString()
    {
        return String.Format("{{{0}: {1}()}}", this.FileName, this.Name);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class dprograms_t
{
    public int version;
    public int crc;			// check of header file

    public int ofs_statements;
    public int numstatements;	// statement 0 is an error

    public int ofs_globaldefs;
    public int numglobaldefs;

    public int ofs_fielddefs;
    public int numfielddefs;

    public int ofs_functions;
    public int numfunctions;	// function 0 is an empty

    public int ofs_strings;
    public int numstrings;		// first string is a null string

    public int ofs_globals;
    public int numglobals;

    public int entityfields;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dprograms_t));

    public void SwapBytes()
    {
        this.version = game_engine.LittleLong(this.version);
        this.crc = game_engine.LittleLong(this.crc);
        this.ofs_statements = game_engine.LittleLong(this.ofs_statements);
        this.numstatements = game_engine.LittleLong(this.numstatements);
        this.ofs_globaldefs = game_engine.LittleLong(this.ofs_globaldefs);
        this.numglobaldefs = game_engine.LittleLong(this.numglobaldefs);
        this.ofs_fielddefs = game_engine.LittleLong(this.ofs_fielddefs);
        this.numfielddefs = game_engine.LittleLong(this.numfielddefs);
        this.ofs_functions = game_engine.LittleLong(this.ofs_functions);
        this.numfunctions = game_engine.LittleLong(this.numfunctions);
        this.ofs_strings = game_engine.LittleLong(this.ofs_strings);
        this.numstrings = game_engine.LittleLong(this.numstrings);
        this.ofs_globals = game_engine.LittleLong(this.ofs_globals);
        this.numglobals = game_engine.LittleLong(this.numglobals);
        this.entityfields = game_engine.LittleLong(this.entityfields);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class globalvars_t
{
    pad_int28 pad;
    public int self;
    public int other;
    public int world;
    public float time;
    public float frametime;
    public float force_retouch;
    public string_t mapname;
    public float deathmatch;
    public float coop;
    public float teamplay;
    public float serverflags;
    public float total_secrets;
    public float total_monsters;
    public float found_secrets;
    public float killed_monsters;
    public float parm1;
    public float parm2;
    public float parm3;
    public float parm4;
    public float parm5;
    public float parm6;
    public float parm7;
    public float parm8;
    public float parm9;
    public float parm10;
    public float parm11;
    public float parm12;
    public float parm13;
    public float parm14;
    public float parm15;
    public float parm16;
    public v3f v_forward;
    public v3f v_up;
    public v3f v_right;
    public float trace_allsolid;
    public float trace_startsolid;
    public float trace_fraction;
    public v3f trace_endpos;
    public v3f trace_plane_normal;
    public float trace_plane_dist;
    public int trace_ent;
    public float trace_inopen;
    public float trace_inwater;
    public int msg_entity;
    public func_t main;
    public func_t StartFrame;
    public func_t PlayerPreThink;
    public func_t PlayerPostThink;
    public func_t ClientKill;
    public func_t ClientConnect;
    public func_t PutClientInServer;
    public func_t ClientDisconnect;
    public func_t SetNewParms;
    public func_t SetChangeParms;

    public static int SizeInBytes = Marshal.SizeOf(typeof(globalvars_t));

    public void SetParams(float[] src)
    {
        if (src.Length < q_shared.NUM_SPAWN_PARMS)
            throw new ArgumentException(String.Format("There must be {0} parameters!", q_shared.NUM_SPAWN_PARMS));

        this.parm1 = src[0];
        this.parm2 = src[1];
        this.parm3 = src[2];
        this.parm4 = src[3];
        this.parm5 = src[4];
        this.parm6 = src[5];
        this.parm7 = src[6];
        this.parm8 = src[7];
        this.parm9 = src[8];
        this.parm10 = src[9];
        this.parm11 = src[10];
        this.parm12 = src[11];
        this.parm13 = src[12];
        this.parm14 = src[13];
        this.parm15 = src[14];
        this.parm16 = src[15];
    }
}

public class trace_t
{
    public bool allsolid;	// if true, plane is not valid
    public bool startsolid;	// if true, the initial point was in a solid area
    public bool inopen, inwater;
    public float fraction;		// time completed, 1.0 = didn't hit anything
    public Vector3 endpos;			// final position
    public plane_t plane;			// surface normal at impact
    public edict_t ent;			// entity the surface is on

    public void CopyFrom(trace_t src)
    {
        this.allsolid = src.allsolid;
        this.startsolid = src.startsolid;
        this.inopen = src.inopen;
        this.inwater = src.inwater;
        this.fraction = src.fraction;
        this.endpos = src.endpos;
        this.plane = src.plane;
        this.ent = src.ent;
    }
}

public class moveclip_t
{
    public Vector3 boxmins, boxmaxs;// enclose the test object along entire move
    public Vector3 mins, maxs;  // size of the moving object
    public Vector3 mins2, maxs2;    // size when clipping against mosnters
    public Vector3 start, end;
    public trace_t trace;
    public int type;
    public edict_t passedict;
}

public class areanode_t
{
    public int axis;		// -1 = leaf node
    public float dist;
    public areanode_t[] children; // [2];
    public link_t trigger_edicts;
    public link_t solid_edicts;

    public areanode_t()
    {
        this.children = new areanode_t[2];
        this.trigger_edicts = new link_t(this);
        this.solid_edicts = new link_t(this);
    }

    public void Clear()
    {
        this.axis = 0;
        this.dist = 0;
        this.children[0] = null;
        this.children[1] = null;
        this.trigger_edicts.ClearToNulls();
        this.solid_edicts.ClearToNulls();
    }
}

public class server_static_t
{
    public int maxclients;
    public int maxclientslimit;
    public client_t[] clients;
    public int serverflags;
    public bool changelevel_issued;
}

public class server_t
{
    public bool active;             // false if only a net client
    public bool paused;
    public bool loadgame;           // handle connections specially
    public double time;
    public int lastcheck;           // used by PF_checkclient
    public double lastchecktime;
    public string name;// char		name[64];			// map name
    public string modelname;// char		modelname[64];		// maps/<name>.bsp, for model_precache[0]
    public model_t worldmodel;
    public string[] model_precache; //[MAX_MODELS];	// NULL terminated
    public model_t[] models; //[MAX_MODELS];
    public string[] sound_precache; //[MAX_SOUNDS];	// NULL terminated
    public string[] lightstyles; // [MAX_LIGHTSTYLES];
    public int num_edicts;
    public int max_edicts;
    public edict_t[] edicts;        // can NOT be array indexed, because
                                    // edict_t is variable sized, but can
                                    // be used to reference the world ent
    public server_state_t state;			// some actions are only valid during load

    public MsgWriter datagram;
    public MsgWriter reliable_datagram; // copied to all clients at end of frame
    public MsgWriter signon;

    public server_t()
    {
        this.model_precache = new string[q_shared.MAX_MODELS];
        this.models = new model_t[q_shared.MAX_MODELS];
        this.sound_precache = new string[q_shared.MAX_SOUNDS];
        this.lightstyles = new string[q_shared.MAX_LIGHTSTYLES];
        this.datagram = new MsgWriter(q_shared.MAX_DATAGRAM);
        this.reliable_datagram = new MsgWriter(q_shared.MAX_DATAGRAM);
        this.signon = new MsgWriter(8192);
    }

    public void Clear()
    {
        this.active = false;
        this.paused = false;
        this.loadgame = false;
        this.time = 0;
        this.lastcheck = 0;
        this.lastchecktime = 0;
        this.name = null;
        this.modelname = null;
        this.worldmodel = null;
        Array.Clear(this.model_precache, 0, this.model_precache.Length);
        Array.Clear(this.models, 0, this.models.Length);
        Array.Clear(this.sound_precache, 0, this.sound_precache.Length);
        Array.Clear(this.lightstyles, 0, this.lightstyles.Length);
        this.num_edicts = 0;
        this.max_edicts = 0;
        this.edicts = null;
        this.state = 0;
        this.datagram.Clear();
        this.reliable_datagram.Clear();
        this.signon.Clear();
    }
}

public class client_t
{
    public bool active;             // false = client is free
    public bool spawned;            // false = don't send datagrams
    public bool dropasap;           // has been told to go to another level
    public bool privileged;         // can execute any host command
    public bool sendsignon;         // only valid before spawned

    public double last_message;     // reliable messages must be sent
                                    // periodically
    public qsocket_t netconnection; // communications handle

    public usercmd_t cmd;               // movement
    public Vector3 wishdir;			// intended motion calced from cmd

    public MsgWriter message;
    //public sizebuf_t		message;			// can be added to at any time,
    // copied and clear once per frame
    //public byte[] msgbuf;//[MAX_MSGLEN];

    public edict_t edict; // edict_t *edict	// EDICT_NUM(clientnum+1)
    public string name;//[32];			// for printing to other people
    public int colors;

    public float[] ping_times;//[NUM_PING_TIMES];
    public int num_pings;           // ping_times[num_pings%NUM_PING_TIMES]

    // spawn parms are carried from level to level
    public float[] spawn_parms;//[NUM_SPAWN_PARMS];

    // client known data for deltas	
    public int old_frags;

    public client_t()
    {
        this.ping_times = new float[q_shared.NUM_PING_TIMES];
        this.spawn_parms = new float[q_shared.NUM_SPAWN_PARMS];
        this.message = new MsgWriter(q_shared.MAX_MSGLEN);
    }

    public void Clear()
    {
        this.active = false;
        this.spawned = false;
        this.dropasap = false;
        this.privileged = false;
        this.sendsignon = false;
        this.last_message = 0;
        this.netconnection = null;
        this.cmd.Clear();
        this.wishdir = Vector3.Zero;
        this.message.Clear();
        this.edict = null;
        this.name = null;
        this.colors = 0;
        Array.Clear(this.ping_times, 0, this.ping_times.Length);
        this.num_pings = 0;
        Array.Clear(this.spawn_parms, 0, this.spawn_parms.Length);
        this.old_frags = 0;
    }
}

public class sfxcache_t
{
    public int length;
    public int loopstart;
    public int speed;
    public int width;
    public int stereo;
    public byte[] data; // [1];		// variable sized
}

public class dma_t
{
    public bool gamealive;
    public bool soundalive;
    public bool splitbuffer;
    public int channels;
    public int samples;             // mono samples in buffer
    public int submission_chunk;        // don't mix less than this #
    public int samplepos;               // in mono samples
    public int samplebits;
    public int speed;
    public byte[] buffer;
}

[StructLayout(LayoutKind.Sequential)]
public class channel_t
{
    public sfx_t sfx;			// sfx number
    public int leftvol;		// 0-255 volume
    public int rightvol;		// 0-255 volume
    public int end;			// end time in global paintsamples
    public int pos;			// sample position in sfx
    public int looping;		// where to loop, -1 = no looping
    public int entnum;			// to allow overriding a specific sound
    public int entchannel;		//
    public Vector3 origin;			// origin of sound effect
    public float dist_mult;		// distance multiplier (attenuation/clipK)
    public int master_vol;		// 0-255 master volume

    public void Clear()
    {
        sfx = null;
        leftvol = 0;
        rightvol = 0;
        end = 0;
        pos = 0;
        looping = 0;
        entnum = 0;
        entchannel = 0;
        origin = Vector3.Zero;
        dist_mult = 0;
        master_vol = 0;
    }
}

//[StructLayout(LayoutKind.Sequential)]
public class wavinfo_t
{
    public int rate;
    public int width;
    public int channels;
    public int loopstart;
    public int samples;
    public int dataofs;		// chunk starts this many bytes from file start
}

public class OpenALController : ISoundController
{
    bool _IsInitialized;
    AudioContext _Context;
    int _Source;
    int[] _Buffers;
    int[] _BufferBytes;
    ALFormat _BufferFormat;
    int _SamplesSent;
    Queue<int> _FreeBuffers;

    private void FreeContext()
    {
        if (_Source != 0)
        {
            AL.SourceStop(_Source);
            AL.DeleteSource(_Source);
            _Source = 0;
        }
        if (_Buffers != null)
        {
            AL.DeleteBuffers(_Buffers);
            _Buffers = null;
        }
        if (_Context != null)
        {
            _Context.Dispose();
            _Context = null;
        }
    }
    public bool IsInitialized
    {
        get { return _IsInitialized; }
    }
    public void Init()
    {
        FreeContext();

        _Context = new AudioContext();
        _Source = AL.GenSource();
        _Buffers = new int[q_shared.AL_BUFFER_COUNT];
        _BufferBytes = new int[q_shared.AL_BUFFER_COUNT];
        _FreeBuffers = new Queue<int>(q_shared.AL_BUFFER_COUNT);

        for (int i = 0; i < _Buffers.Length; i++)
        {
            _Buffers[i] = AL.GenBuffer();
            _FreeBuffers.Enqueue(_Buffers[i]);
        }

        AL.SourcePlay(_Source);
        AL.Source(_Source, ALSourceb.Looping, false);

        game_engine.shm.channels = 2;
        game_engine.shm.samplebits = 16;
        game_engine.shm.speed = 11025;
        game_engine.shm.buffer = new byte[q_shared.BUFFER_SIZE];
        game_engine.shm.soundalive = true;
        game_engine.shm.splitbuffer = false;
        game_engine.shm.samples = game_engine.shm.buffer.Length / (game_engine.shm.samplebits / 8);
        game_engine.shm.samplepos = 0;
        game_engine.shm.submission_chunk = 1;

        if (game_engine.shm.samplebits == 8)
        {
            if (game_engine.shm.channels == 2)
                _BufferFormat = ALFormat.Stereo8;
            else
                _BufferFormat = ALFormat.Mono8;
        }
        else
        {
            if (game_engine.shm.channels == 2)
                _BufferFormat = ALFormat.Stereo16;
            else
                _BufferFormat = ALFormat.Mono16;
        }

        _IsInitialized = true;
    }
    public void Shutdown()
    {
        FreeContext();
        _IsInitialized = false;
    }
    public void ClearBuffer()
    {
        AL.SourceStop(_Source);
    }
    public byte[] LockBuffer()
    {
        return game_engine.shm.buffer;
    }
    public void UnlockBuffer(int bytes)
    {
        int processed;
        AL.GetSource(_Source, ALGetSourcei.BuffersProcessed, out processed);
        if (processed > 0)
        {
            int[] bufs = AL.SourceUnqueueBuffers(_Source, processed);
            foreach (int buffer in bufs)
            {
                if (buffer == 0)
                    continue;

                int idx = Array.IndexOf(_Buffers, buffer);
                if (idx != -1)
                {
                    _SamplesSent += _BufferBytes[idx] >> ((game_engine.shm.samplebits / 8) - 1);
                    _SamplesSent &= (game_engine.shm.samples - 1);
                    _BufferBytes[idx] = 0;
                }
                if (!_FreeBuffers.Contains(buffer))
                    _FreeBuffers.Enqueue(buffer);
            }
        }

        if (_FreeBuffers.Count == 0)
        {
            game_engine.Con_DPrintf("UnlockBuffer: No free buffers!\n");
            return;
        }

        int buf = _FreeBuffers.Dequeue();
        if (buf != 0)
        {
            AL.BufferData(buf, _BufferFormat, game_engine.shm.buffer, bytes, game_engine.shm.speed);
            AL.SourceQueueBuffer(_Source, buf);

            int idx = Array.IndexOf(_Buffers, buf);
            if (idx != -1)
            {
                _BufferBytes[idx] = bytes;
            }

            int state;
            AL.GetSource(_Source, ALGetSourcei.SourceState, out state);
            if ((ALSourceState)state != ALSourceState.Playing)
            {
                AL.SourcePlay(_Source);
                game_engine.Con_DPrintf("Sound resumed from {0}, free {1} of {2} buffers\n",
                    ((ALSourceState)state).ToString("F"), _FreeBuffers.Count, _Buffers.Length);
            }
        }
    }
    public int GetPosition()
    {
        int state, offset = 0;
        AL.GetSource(_Source, ALGetSourcei.SourceState, out state);
        if ((ALSourceState)state != ALSourceState.Playing)
        {
            for (int i = 0; i < _BufferBytes.Length; i++)
            {
                _SamplesSent += _BufferBytes[i] >> ((game_engine.shm.samplebits / 8) - 1);
                _BufferBytes[i] = 0;
            }
            _SamplesSent &= (game_engine.shm.samples - 1);
        }
        else
        {
            AL.GetSource(_Source, ALGetSourcei.SampleOffset, out offset);
        }
        return (_SamplesSent + offset) & (game_engine.shm.samples - 1);
    }
}

public class NullSoundController : ISoundController
{
    public bool IsInitialized
    {
        get { return false; }
    }
    public void Init()
    {
        game_engine.shm.channels = 2;
        game_engine.shm.samplebits = 16;
        game_engine.shm.speed = 11025;
    }
    public void Shutdown()
    {
    }
    public void ClearBuffer()
    {
    }
    public byte[] LockBuffer()
    {
        return game_engine.shm.buffer;
    }
    public void UnlockBuffer(int bytes)
    {
    }
    public int GetPosition()
    {
        return 0;
    }
}

public class quakeparms_t
{
    public string basedir;
    public string cachedir;		// for development over ISDN lines
    public string[] argv;

    public quakeparms_t()
    {
        this.basedir = String.Empty;
        this.cachedir = String.Empty;
    }
}

public class NetTcpIp : INetLanDriver
{

    static NetTcpIp _Singletone = new NetTcpIp();

    bool _IsInitialized;
    IPAddress myAddr;
    Socket net_controlsocket;
    Socket net_broadcastsocket;
    EndPoint broadcastaddr;
    Socket net_acceptsocket;

    private NetTcpIp()
    {
    }
    public static NetTcpIp Instance
    {
        get { return _Singletone; }
    }
    public string Name
    {
        get { return "TCP/IP"; }
    }
    public bool IsInitialized
    {
        get { return _IsInitialized; }
    }
    public Socket ControlSocket
    {
        get { return net_controlsocket; }
    }
    public bool UDP_Init()
    {
        _IsInitialized = false;

        if (game_engine.HasParam("-noudp"))
            return false;

        // determine my name
        string hostName;
        try
        {
            hostName = Dns.GetHostName();
        }
        catch (SocketException se)
        {
            game_engine.Con_DPrintf("Cannot get host name: {0}\n", se.Message);
            return false;
        }

        // if the quake hostname isn't set, set it to the machine name
        if (game_engine.hostname.@string == "UNNAMED")
        {
            IPAddress addr;
            if (!IPAddress.TryParse(hostName, out addr))
            {
                int i = hostName.IndexOf('.');
                if (i != -1)
                {
                    hostName = hostName.Substring(0, i);
                }
            }
            Cvar.Cvar_Set("hostname", hostName);
        }

        int i2 = game_engine.COM_CheckParm("-ip");
        if (i2 > 0)
        {
            if (i2 < game_engine.com_argv.Length - 1)
            {
                string ipaddr = game_engine.Argv(i2 + 1);
                if (!IPAddress.TryParse(ipaddr, out myAddr))
                    game_engine.Sys_Error("{0} is not a valid IP address!", ipaddr);
                game_engine.my_tcpip_address = ipaddr;
            }
            else
            {
                game_engine.Sys_Error("Net.Init: you must specify an IP address after -ip");
            }
        }
        else
        {
            myAddr = IPAddress.Any;
            game_engine.my_tcpip_address = "INADDR_ANY";
        }

        net_controlsocket = OpenSocket(0);
        if (net_controlsocket == null)
        {
            game_engine.Con_Printf("TCP/IP: Unable to open control socket\n");
            return false;
        }

        broadcastaddr = new IPEndPoint(IPAddress.Broadcast, game_engine.net_hostport);

        _IsInitialized = true;
        game_engine.Con_Printf("TCP/IP Initialized\n");
        return true;
    }
    public void Shutdown()
    {
        UDP_Listen(false);
        CloseSocket(net_controlsocket);
    }
    public void UDP_Listen(bool state)
    {
        // enable listening
        if (state)
        {
            if (net_acceptsocket == null)
            {
                net_acceptsocket = OpenSocket(game_engine.net_hostport);
                if (net_acceptsocket == null)
                    game_engine.Sys_Error("UDP_Listen: Unable to open accept socket\n");
            }
        }
        else
        {
            // disable listening
            if (net_acceptsocket != null)
            {
                CloseSocket(net_acceptsocket);
                net_acceptsocket = null;
            }
        }
    }
    public Socket OpenSocket(int port)
    {
        Socket result = null;
        try
        {
            result = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            result.Blocking = false;
            EndPoint ep = new IPEndPoint(myAddr, port);
            result.Bind(ep);
        }
        catch (Exception ex)
        {
            if (result != null)
            {
                result.Close();
                result = null;
            }
            game_engine.Con_Printf("Unable to create socket: " + ex.Message);
        }

        return result;
    }
    public int CloseSocket(Socket socket)
    {
        if (socket == net_broadcastsocket)
            net_broadcastsocket = null;

        socket.Close();
        return 0;
    }
    public int Connect(Socket socket, EndPoint addr)
    {
        return 0;
    }
    public string GetNameFromAddr(EndPoint addr)
    {
        try
        {
            IPHostEntry entry = Dns.GetHostEntry(((IPEndPoint)addr).Address);
            return entry.HostName;
        }
        catch (SocketException)
        {
        }
        return String.Empty;
    }
    public EndPoint GetAddrFromName(string name)
    {
        try
        {
            IPAddress addr;
            int i = name.IndexOf(':');
            string saddr;
            int port = game_engine.net_hostport;
            if (i != -1)
            {
                saddr = name.Substring(0, i);
                int p;
                if (int.TryParse(name.Substring(i + 1), out p))
                    port = p;
            }
            else
                saddr = name;

            if (IPAddress.TryParse(saddr, out addr))
            {
                return new IPEndPoint(addr, port);
            }
            IPHostEntry entry = Dns.GetHostEntry(name);
            foreach (IPAddress addr2 in entry.AddressList)
            {
                return new IPEndPoint(addr2, port);
            }
        }
        catch (SocketException)
        {
        }
        return null;
    }
    public int AddrCompare(EndPoint addr1, EndPoint addr2)
    {
        if (addr1.AddressFamily != addr2.AddressFamily)
            return -1;

        IPEndPoint ep1 = addr1 as IPEndPoint;
        IPEndPoint ep2 = addr2 as IPEndPoint;

        if (ep1 == null || ep2 == null)
            return -1;

        if (!ep1.Address.Equals(ep2.Address))
            return -1;

        if (ep1.Port != ep2.Port)
            return 1;

        return 0;
    }
    public int GetSocketPort(EndPoint addr)
    {
        return ((IPEndPoint)addr).Port;
    }
    public int SetSocketPort(EndPoint addr, int port)
    {
        ((IPEndPoint)addr).Port = port;
        return 0;
    }
    public Socket CheckNewConnections()
    {
        if (net_acceptsocket == null)
            return null;

        if (net_acceptsocket.Available > 0)
            return net_acceptsocket;

        return null;
    }
    public int Read(Socket socket, byte[] buf, int len, ref EndPoint ep)
    {
        int ret = 0;
        try
        {
            ret = socket.ReceiveFrom(buf, len, SocketFlags.None, ref ep);
        }
        catch (SocketException se)
        {
            if (se.ErrorCode == q_shared.WSAEWOULDBLOCK || se.ErrorCode == q_shared.WSAECONNREFUSED)
                ret = 0;
            else
                ret = -1;
        }
        return ret;
    }
    public int Write(Socket socket, byte[] buf, int len, EndPoint ep)
    {
        int ret = 0;
        try
        {
            ret = socket.SendTo(buf, len, SocketFlags.None, ep);
        }
        catch (SocketException se)
        {
            if (se.ErrorCode == q_shared.WSAEWOULDBLOCK)
                ret = 0;
            else
                ret = -1;
        }
        return ret;
    }
    public int Broadcast(Socket socket, byte[] buf, int len)
    {
        if (socket != net_broadcastsocket)
        {
            if (net_broadcastsocket != null)
                game_engine.Sys_Error("Attempted to use multiple broadcasts sockets\n");
            try
            {
                socket.EnableBroadcast = true;
            }
            catch (SocketException se)
            {
                game_engine.Con_Printf("Unable to make socket broadcast capable: {0}\n", se.Message);
                return -1;
            }
        }

        return Write(socket, buf, len, broadcastaddr);
    }
}

public class NetLoop : INetDriver
{
    bool _IsInitialized;
    bool localconnectpending;
    qsocket_t loop_client;
    qsocket_t loop_server;

    public string Name
    {
        get { return "Loopback"; }
    }
    public bool IsInitialized
    {
        get { return _IsInitialized; }
    }
    public void Init()
    {
        if (game_engine.cls.state == cactive_t.ca_dedicated)
            return;// -1;

        _IsInitialized = true;
    }
    public void Datagram_Listen(bool state)
    {
        // nothig to do
    }
    public void Datagram_SearchForHosts(bool xmit)
    {
        if (!game_engine.sv.active)
            return;

        game_engine.hostCacheCount = 1;
        if (game_engine.hostname.@string == "UNNAMED")
            game_engine.hostcache[0].name = "local";
        else
            game_engine.hostcache[0].name = game_engine.hostname.@string;

        game_engine.hostcache[0].map = game_engine.sv.name;
        game_engine.hostcache[0].users = game_engine.net_activeconnections;
        game_engine.hostcache[0].maxusers = game_engine.svs.maxclients;
        game_engine.hostcache[0].driver = game_engine.net_driverlevel;
        game_engine.hostcache[0].cname = "local";
    }
    public qsocket_t Datagram_Connect(string host)
    {
        if (host != "local")
            return null;

        localconnectpending = true;

        if (loop_client == null)
        {
            loop_client = game_engine.NET_NewQSocket();
            if (loop_client == null)
            {
                game_engine.Con_Printf("Loop_Connect: no qsocket available\n");
                return null;
            }
            loop_client.address = "localhost";
        }
        loop_client.ClearBuffers();
        loop_client.canSend = true;

        if (loop_server == null)
        {
            loop_server = game_engine.NET_NewQSocket();
            if (loop_server == null)
            {
                game_engine.Con_Printf("Loop_Connect: no qsocket available\n");
                return null;
            }
            loop_server.address = "LOCAL";
        }
        loop_server.ClearBuffers();
        loop_server.canSend = true;

        loop_client.driverdata = loop_server;
        loop_server.driverdata = loop_client;

        return loop_client;
    }
    public qsocket_t Datagram_CheckNewConnections()
    {
        if (!localconnectpending)
            return null;

        localconnectpending = false;
        loop_server.ClearBuffers();
        loop_server.canSend = true;
        loop_client.ClearBuffers();
        loop_client.canSend = true;
        return loop_server;

    }
    private int IntAlign(int value)
    {
        return (value + (sizeof(int) - 1)) & (~(sizeof(int) - 1));
    }
    public int GetMessage(qsocket_t sock)
    {
        if (sock.receiveMessageLength == 0)
            return 0;

        int ret = sock.receiveMessage[0];
        int length = sock.receiveMessage[1] + (sock.receiveMessage[2] << 8);

        // alignment byte skipped here
        game_engine.Message.Clear();
        game_engine.Message.FillFrom(sock.receiveMessage, 4, length);

        length = IntAlign(length + 4);
        sock.receiveMessageLength -= length;

        if (sock.receiveMessageLength > 0)
            Array.Copy(sock.receiveMessage, length, sock.receiveMessage, 0, sock.receiveMessageLength);

        if (sock.driverdata != null && ret == 1)
            ((qsocket_t)sock.driverdata).canSend = true;

        return ret;
    }
    public int Datagram_SendMessage(qsocket_t sock, MsgWriter data)
    {
        if (sock.driverdata == null)
            return -1;

        qsocket_t sock2 = (qsocket_t)sock.driverdata;

        if ((sock2.receiveMessageLength + data.Length + 4) > q_shared.NET_MAXMESSAGE)
            game_engine.Sys_Error("Loop_SendMessage: overflow\n");

        // message type
        int offset = sock2.receiveMessageLength;
        sock2.receiveMessage[offset++] = 1;

        // length
        sock2.receiveMessage[offset++] = (byte)(data.Length & 0xff);
        sock2.receiveMessage[offset++] = (byte)(data.Length >> 8);

        // align
        offset++;

        // message
        Buffer.BlockCopy(data.Data, 0, sock2.receiveMessage, offset, data.Length);
        sock2.receiveMessageLength = IntAlign(sock2.receiveMessageLength + data.Length + 4);

        sock.canSend = false;
        return 1;
    }
    public int Datagram_SendUnreliableMessage(qsocket_t sock, MsgWriter data)
    {
        if (sock.driverdata == null)
            return -1;

        qsocket_t sock2 = (qsocket_t)sock.driverdata;

        if ((sock2.receiveMessageLength + data.Length + sizeof(byte) + sizeof(short)) > q_shared.NET_MAXMESSAGE)
            return 0;

        int offset = sock2.receiveMessageLength;

        // message type
        sock2.receiveMessage[offset++] = 2;

        // length
        sock2.receiveMessage[offset++] = (byte)(data.Length & 0xff);
        sock2.receiveMessage[offset++] = (byte)(data.Length >> 8);

        // align
        offset++;

        // message
        Buffer.BlockCopy(data.Data, 0, sock2.receiveMessage, offset, data.Length);
        sock2.receiveMessageLength = IntAlign(sock2.receiveMessageLength + data.Length + 4);

        return 1;
    }
    public bool Datagram_CanSendMessage(qsocket_t sock)
    {
        if (sock.driverdata == null)
            return false;
        return sock.canSend;
    }
    public bool Datagram_CanSendUnreliableMessage(qsocket_t sock)
    {
        return true;
    }
    public void Datagram_Close(qsocket_t sock)
    {
        if (sock.driverdata != null)
            ((qsocket_t)sock.driverdata).driverdata = null;

        sock.ClearBuffers();
        sock.canSend = true;
        if (sock == loop_client)
            loop_client = null;
        else
            loop_server = null;
    }
    public void Datagram_Shutdown()
    {
        _IsInitialized = false;
    }
}

public class NetDatagram : INetDriver
{
    static NetDatagram _Singletone = new NetDatagram();

    int _DriverLevel;
    bool _IsInitialized;
    byte[] _PacketBuffer;

    // statistic counters
    int packetsSent;
    int packetsReSent;
    int packetsReceived;
    int receivedDuplicateCount;
    int shortPacketCount;
    int droppedDatagrams;
    //

    private NetDatagram()
    {
        _PacketBuffer = new byte[q_shared.NET_DATAGRAMSIZE];
    }
    public static NetDatagram Instance
    {
        get { return _Singletone; }
    }
    public string Name
    {
        get { return "Datagram"; }
    }
    public bool IsInitialized
    {
        get { return _IsInitialized; }
    }
    public void Init()
    {
        _DriverLevel = Array.IndexOf(game_engine.net_drivers, this);
        game_engine.Cmd_AddCommand("net_stats", NET_Stats_f);

        if (game_engine.HasParam("-nolan"))
            return;

        foreach (INetLanDriver driver in game_engine.net_landrivers)
        {
            driver.UDP_Init();
        }

#if BAN_TEST
	    Cmd_AddCommand ("ban", NET_Ban_f);
#endif

        _IsInitialized = true;
    }
    public void Datagram_Listen(bool state)
    {
        foreach (INetLanDriver drv in game_engine.net_landrivers)
        {
            if (drv.IsInitialized)
                drv.UDP_Listen(state);
        }
    }
    public void Datagram_SearchForHosts(bool xmit)
    {
        for (game_engine.net_landriverlevel = 0; game_engine.net_landriverlevel < game_engine.net_landrivers.Length; game_engine.net_landriverlevel++)
        {
            if (game_engine.hostCacheCount == q_shared.HOSTCACHESIZE)
                break;
            if (game_engine.net_landrivers[game_engine.net_landriverlevel].IsInitialized)
                _Datagram_SearchForHosts(xmit);
        }
    }
    void _Datagram_SearchForHosts(bool xmit)
    {
        EndPoint myaddr = game_engine.net_landrivers[game_engine.net_landriverlevel].ControlSocket.LocalEndPoint;
        if (xmit)
        {
            game_engine.Message.Clear();
            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREQ_SERVER_INFO);
            game_engine.Message.MSG_WriteString("QUAKE");
            game_engine.Message.MSG_WriteByte(q_shared.NET_PROTOCOL_VERSION);
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL | (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Broadcast(game_engine.net_landrivers[game_engine.net_landriverlevel].ControlSocket, game_engine.Message.Data, game_engine.Message.Length);
            game_engine.Message.Clear();
        }

        EndPoint readaddr = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            game_engine.Message.FillFrom(game_engine.net_landrivers[game_engine.net_landriverlevel].ControlSocket, ref readaddr);
            if (game_engine.Message.IsEmpty)
                break;
            if (game_engine.Message.Length < sizeof(int))
                continue;

            // don't answer our own query
            if (game_engine.net_landrivers[game_engine.net_landriverlevel].AddrCompare(readaddr, myaddr) >= 0)
                continue;

            // is the cache full?
            if (game_engine.hostCacheCount == q_shared.HOSTCACHESIZE)
                continue;

            game_engine.Reader.MSG_BeginReading();
            int control = game_engine.BigLong(game_engine.Reader.MSG_ReadLong());// BigLong(*((int *)net_message.data));
            //MSG_ReadLong();
            if (control == -1)
                continue;
            if ((control & (~q_shared.NETFLAG_LENGTH_MASK)) != q_shared.NETFLAG_CTL)
                continue;
            if ((control & q_shared.NETFLAG_LENGTH_MASK) != game_engine.Message.Length)
                continue;

            if (game_engine.Reader.MSG_ReadByte() != q_shared.CCREP_SERVER_INFO)
                continue;

            readaddr = game_engine.net_landrivers[game_engine.net_landriverlevel].GetAddrFromName(game_engine.Reader.MSG_ReadString());
            int n;
            // search the cache for this server
            for (n = 0; n < game_engine.hostCacheCount; n++)
                if (game_engine.net_landrivers[game_engine.net_landriverlevel].AddrCompare(readaddr, game_engine.hostcache[n].addr) == 0)
                    break;

            // is it already there?
            if (n < game_engine.hostCacheCount)
                continue;

            // add it
            game_engine.hostCacheCount++;
            hostcache_t hc = game_engine.hostcache[n];
            hc.name = game_engine.Reader.MSG_ReadString();
            hc.map = game_engine.Reader.MSG_ReadString();
            hc.users = game_engine.Reader.MSG_ReadByte();
            hc.maxusers = game_engine.Reader.MSG_ReadByte();
            if (game_engine.Reader.MSG_ReadByte() != q_shared.NET_PROTOCOL_VERSION)
            {
                hc.cname = hc.name;
                hc.name = "*" + hc.name;
            }
            IPEndPoint ep = (IPEndPoint)readaddr;
            hc.addr = new IPEndPoint(ep.Address, ep.Port);
            hc.driver = game_engine.net_driverlevel;
            hc.ldriver = game_engine.net_landriverlevel;
            hc.cname = readaddr.ToString();

            // check for a name conflict
            for (int i = 0; i < game_engine.hostCacheCount; i++)
            {
                if (i == n)
                    continue;
                hostcache_t hc2 = game_engine.hostcache[i];
                if (hc.name == hc2.name)
                {
                    i = hc.name.Length;
                    if (i < 15 && hc.name[i - 1] > '8')
                    {
                        hc.name = hc.name.Substring(0, i) + '0';
                    }
                    else
                        hc.name = hc.name.Substring(0, i - 1) + (char)(hc.name[i - 1] + 1);
                    i = 0;// -1;
                }
            }
        }
    }
    public qsocket_t Datagram_Connect(string host)
    {
        qsocket_t ret = null;

        for (game_engine.net_landriverlevel = 0; game_engine.net_landriverlevel < game_engine.net_landrivers.Length; game_engine.net_landriverlevel++)
            if (game_engine.net_landrivers[game_engine.net_landriverlevel].IsInitialized)
            {
                ret = _Datagram_Connect(host);
                if (ret != null)
                    break;
            }
        return ret;
    }
    qsocket_t _Datagram_Connect(string host)
    {
        // see if we can resolve the host name
        EndPoint sendaddr = game_engine.net_landrivers[game_engine.net_landriverlevel].GetAddrFromName(host);
        if (sendaddr == null)
            return null;

        Socket newsock = game_engine.net_landrivers[game_engine.net_landriverlevel].OpenSocket(0);
        if (newsock == null)
            return null;

        qsocket_t sock = game_engine.NET_NewQSocket();
        if (sock == null)
            goto ErrorReturn2;
        sock.socket = newsock;
        sock.landriver = game_engine.net_landriverlevel;

        // connect to the host
        if (game_engine.net_landrivers[game_engine.net_landriverlevel].Connect(newsock, sendaddr) == -1)
            goto ErrorReturn;

        // send the connection request
        game_engine.Con_Printf("trying...\n");
        game_engine.SCR_UpdateScreen();
        double start_time = game_engine.net_time;
        int ret = 0;
        for (int reps = 0; reps < 3; reps++)
        {
            game_engine.Message.Clear();
            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREQ_CONNECT);
            game_engine.Message.MSG_WriteString("QUAKE");
            game_engine.Message.MSG_WriteByte(q_shared.NET_PROTOCOL_VERSION);
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            //*((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Write(newsock, game_engine.Message.Data, game_engine.Message.Length, sendaddr);
            game_engine.Message.Clear();
            EndPoint readaddr = new IPEndPoint(IPAddress.Any, 0);
            do
            {
                ret = game_engine.Message.FillFrom(newsock, ref readaddr);
                // if we got something, validate it
                if (ret > 0)
                {
                    // is it from the right place?
                    if (sock.LanDriver.AddrCompare(readaddr, sendaddr) != 0)
                    {
#if DEBUG
                        game_engine.Con_Printf("wrong reply address\n");
                        game_engine.Con_Printf("Expected: {0}\n", StrAddr(sendaddr));
                        game_engine.Con_Printf("Received: {0}\n", StrAddr(readaddr));
                        game_engine.SCR_UpdateScreen();
#endif
                        ret = 0;
                        continue;
                    }

                    if (ret < sizeof(int))
                    {
                        ret = 0;
                        continue;
                    }

                    game_engine.Reader.MSG_BeginReading();

                    int control = game_engine.BigLong(game_engine.Reader.MSG_ReadLong());// BigLong(*((int *)net_message.data));
                    //MSG_ReadLong();
                    if (control == -1)
                    {
                        ret = 0;
                        continue;
                    }
                    if ((control & (~q_shared.NETFLAG_LENGTH_MASK)) != q_shared.NETFLAG_CTL)
                    {
                        ret = 0;
                        continue;
                    }
                    if ((control & q_shared.NETFLAG_LENGTH_MASK) != ret)
                    {
                        ret = 0;
                        continue;
                    }
                }
            }
            while ((ret == 0) && (game_engine.SetNetTime() - start_time) < 2.5);
            if (ret > 0)
                break;
            game_engine.Con_Printf("still trying...\n");
            game_engine.SCR_UpdateScreen();
            start_time = game_engine.SetNetTime();
        }

        string reason = String.Empty;
        if (ret == 0)
        {
            reason = "No Response";
            game_engine.Con_Printf("{0}\n", reason);
            Menu.ReturnReason = reason;
            goto ErrorReturn;
        }

        if (ret == -1)
        {
            reason = "Network Error";
            game_engine.Con_Printf("{0}\n", reason);
            Menu.ReturnReason = reason;
            goto ErrorReturn;
        }

        ret = game_engine.Reader.MSG_ReadByte();
        if (ret == q_shared.CCREP_REJECT)
        {
            reason = game_engine.Reader.MSG_ReadString();
            game_engine.Con_Printf(reason);
            Menu.ReturnReason = reason;
            goto ErrorReturn;
        }

        if (ret == q_shared.CCREP_ACCEPT)
        {
            IPEndPoint ep = (IPEndPoint)sendaddr;
            sock.addr = new IPEndPoint(ep.Address, ep.Port);
            game_engine.net_landrivers[game_engine.net_landriverlevel].SetSocketPort(sock.addr, game_engine.Reader.MSG_ReadLong());
        }
        else
        {
            reason = "Bad Response";
            game_engine.Con_Printf("{0}\n", reason);
            Menu.ReturnReason = reason;
            goto ErrorReturn;
        }

        sock.address = game_engine.net_landrivers[game_engine.net_landriverlevel].GetNameFromAddr(sendaddr);

        game_engine.Con_Printf("Connection accepted\n");
        sock.lastMessageTime = game_engine.SetNetTime();

        // switch the connection to the specified address
        if (game_engine.net_landrivers[game_engine.net_landriverlevel].Connect(newsock, sock.addr) == -1)
        {
            reason = "Connect to Game failed";
            game_engine.Con_Printf("{0}\n", reason);
            Menu.ReturnReason = reason;
            goto ErrorReturn;
        }

        Menu.ReturnOnError = false;
        return sock;

        ErrorReturn:
        game_engine.NET_FreeQSocket(sock);
        ErrorReturn2:
        game_engine.net_landrivers[game_engine.net_landriverlevel].CloseSocket(newsock);
        if (Menu.ReturnOnError && Menu.ReturnMenu != null)
        {
            Menu.ReturnMenu.Show();
            Menu.ReturnOnError = false;
        }
        return null;
    }
    public qsocket_t Datagram_CheckNewConnections()
    {
        qsocket_t ret = null;

        for (game_engine.net_landriverlevel = 0; game_engine.net_landriverlevel < game_engine.net_landrivers.Length; game_engine.net_landriverlevel++)
            if (game_engine.net_landrivers[game_engine.net_landriverlevel].IsInitialized)
            {
                ret = _Datagram_CheckNewConnections();
                if (ret != null)
                    break;
            }
        return ret;
    }
    public qsocket_t _Datagram_CheckNewConnections()
    {
        Socket acceptsock = game_engine.net_landrivers[game_engine.net_landriverlevel].CheckNewConnections();
        if (acceptsock == null)
            return null;

        EndPoint clientaddr = new IPEndPoint(IPAddress.Any, 0);
        game_engine.Message.FillFrom(acceptsock, ref clientaddr);

        if (game_engine.Message.Length < sizeof(int))
            return null;

        game_engine.Reader.MSG_BeginReading();
        int control = game_engine.BigLong(game_engine.Reader.MSG_ReadLong());
        if (control == -1)
            return null;
        if ((control & (~q_shared.NETFLAG_LENGTH_MASK)) != q_shared.NETFLAG_CTL)
            return null;
        if ((control & q_shared.NETFLAG_LENGTH_MASK) != game_engine.Message.Length)
            return null;

        int command = game_engine.Reader.MSG_ReadByte();
        if (command == q_shared.CCREQ_SERVER_INFO)
        {
            string tmp = game_engine.Reader.MSG_ReadString();
            if (tmp != "QUAKE")
                return null;

            game_engine.Message.Clear();

            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREP_SERVER_INFO);
            EndPoint newaddr = acceptsock.LocalEndPoint; //dfunc.GetSocketAddr(acceptsock, &newaddr);
            game_engine.Message.MSG_WriteString(newaddr.ToString()); // dfunc.AddrToString(&newaddr));
            game_engine.Message.MSG_WriteString(game_engine.hostname.@string);
            game_engine.Message.MSG_WriteString(game_engine.sv.name);
            game_engine.Message.MSG_WriteByte(game_engine.net_activeconnections);
            game_engine.Message.MSG_WriteByte(game_engine.svs.maxclients);
            game_engine.Message.MSG_WriteByte(q_shared.NET_PROTOCOL_VERSION);
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
            game_engine.Message.Clear();
            return null;
        }

        if (command == q_shared.CCREQ_PLAYER_INFO)
        {
            int playerNumber = game_engine.Reader.MSG_ReadByte();
            int clientNumber, activeNumber = -1;
            client_t client = null;
            for (clientNumber = 0; clientNumber < game_engine.svs.maxclients; clientNumber++)
            {
                client = game_engine.svs.clients[clientNumber];
                if (client.active)
                {
                    activeNumber++;
                    if (activeNumber == playerNumber)
                        break;
                }
            }
            if (clientNumber == game_engine.svs.maxclients)
                return null;

            game_engine.Message.Clear();
            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREP_PLAYER_INFO);
            game_engine.Message.MSG_WriteByte(playerNumber);
            game_engine.Message.MSG_WriteString(client.name);
            game_engine.Message.MSG_WriteLong(client.colors);
            game_engine.Message.MSG_WriteLong((int)client.edict.v.frags);
            game_engine.Message.MSG_WriteLong((int)(game_engine.net_time - client.netconnection.connecttime));
            game_engine.Message.MSG_WriteString(client.netconnection.address);
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
            game_engine.Message.Clear();

            return null;
        }

        if (command == q_shared.CCREQ_RULE_INFO)
        {
            // find the search start location
            string prevCvarName = game_engine.Reader.MSG_ReadString();
            cvar_t var;
            if (!String.IsNullOrEmpty(prevCvarName))
            {
                var = Cvar.Cvar_FindVar(prevCvarName);
                if (var == null)
                    return null;
                var = var.next;
            }
            else
                var = Cvar.First;

            // search for the next server cvar
            while (var != null)
            {
                if (var.server)
                    break;
                var = var.next;
            }

            // send the response
            game_engine.Message.Clear();

            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREP_RULE_INFO);
            if (var != null)
            {
                game_engine.Message.MSG_WriteString(var.name);
                game_engine.Message.MSG_WriteString(var.@string);
            }
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
            game_engine.Message.Clear();

            return null;
        }

        if (command != q_shared.CCREQ_CONNECT)
            return null;

        if (game_engine.Reader.MSG_ReadString() != "QUAKE")
            return null;

        if (game_engine.Reader.MSG_ReadByte() != q_shared.NET_PROTOCOL_VERSION)
        {
            game_engine.Message.Clear();
            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREP_REJECT);
            game_engine.Message.MSG_WriteString("Incompatible version.\n");
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
            game_engine.Message.Clear();
            return null;
        }

#if BAN_TEST
        // check for a ban
        if (clientaddr.sa_family == AF_INET)
        {
            unsigned long testAddr;
            testAddr = ((struct sockaddr_in *)&clientaddr)->sin_addr.s_addr;
            if ((testAddr & banMask) == banAddr)
            {
                SZ_Clear(&net_message);
                // save space for the header, filled in later
                MSG_WriteLong(&net_message, 0);
                MSG_WriteByte(&net_message, CCREP_REJECT);
                MSG_WriteString(&net_message, "You have been banned.\n");
                *((int *)net_message.data) = BigLong(NETFLAG_CTL | (net_message.cursize & NETFLAG_LENGTH_MASK));
                dfunc.Write (acceptsock, net_message.data, net_message.cursize, &clientaddr);
                SZ_Clear(&net_message);
                return NULL;
            }
        }
#endif

        // see if this guy is already connected
        foreach (qsocket_t s in game_engine.net_activeSockets)
        {
            if (s.driver != game_engine.net_driverlevel)
                continue;

            int ret = game_engine.net_landrivers[game_engine.net_landriverlevel].AddrCompare(clientaddr, s.addr);
            if (ret >= 0)
            {
                // is this a duplicate connection reqeust?
                if (ret == 0 && game_engine.net_time - s.connecttime < 2.0)
                {
                    // yes, so send a duplicate reply
                    game_engine.Message.Clear();
                    // save space for the header, filled in later
                    game_engine.Message.MSG_WriteLong(0);
                    game_engine.Message.MSG_WriteByte(q_shared.CCREP_ACCEPT);
                    EndPoint newaddr = s.socket.LocalEndPoint; //dfunc.GetSocketAddr(s.socket, &newaddr);
                    game_engine.Message.MSG_WriteLong(game_engine.net_landrivers[game_engine.net_landriverlevel].GetSocketPort(newaddr));
                    game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                        (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
                    game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
                    game_engine.Message.Clear();
                    return null;
                }
                // it's somebody coming back in from a crash/disconnect
                // so close the old qsocket and let their retry get them back in
                game_engine.NET_Close(s);
                return null;
            }
        }

        // allocate a QSocket
        qsocket_t sock = game_engine.NET_NewQSocket();
        if (sock == null)
        {
            // no room; try to let him know
            game_engine.Message.Clear();
            // save space for the header, filled in later
            game_engine.Message.MSG_WriteLong(0);
            game_engine.Message.MSG_WriteByte(q_shared.CCREP_REJECT);
            game_engine.Message.MSG_WriteString("Server is full.\n");
            game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
                (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
            game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
            game_engine.Message.Clear();
            return null;
        }

        // allocate a network socket
        Socket newsock = game_engine.net_landrivers[game_engine.net_landriverlevel].OpenSocket(0);
        if (newsock == null)
        {
            game_engine.NET_FreeQSocket(sock);
            return null;
        }

        // connect to the client
        if (game_engine.net_landrivers[game_engine.net_landriverlevel].Connect(newsock, clientaddr) == -1)
        {
            game_engine.net_landrivers[game_engine.net_landriverlevel].CloseSocket(newsock);
            game_engine.NET_FreeQSocket(sock);
            return null;
        }

        // everything is allocated, just fill in the details	
        sock.socket = newsock;
        sock.landriver = game_engine.net_landriverlevel;
        sock.addr = clientaddr;
        sock.address = clientaddr.ToString();

        // send him back the info about the server connection he has been allocated
        game_engine.Message.Clear();
        // save space for the header, filled in later
        game_engine.Message.MSG_WriteLong(0);
        game_engine.Message.MSG_WriteByte(q_shared.CCREP_ACCEPT);
        EndPoint newaddr2 = newsock.LocalEndPoint;// dfunc.GetSocketAddr(newsock, &newaddr);
        game_engine.Message.MSG_WriteLong(game_engine.net_landrivers[game_engine.net_landriverlevel].GetSocketPort(newaddr2));
        game_engine.WriteInt(game_engine.Message.Data, 0, game_engine.BigLong(q_shared.NETFLAG_CTL |
            (game_engine.Message.Length & q_shared.NETFLAG_LENGTH_MASK)));
        game_engine.net_landrivers[game_engine.net_landriverlevel].Write(acceptsock, game_engine.Message.Data, game_engine.Message.Length, clientaddr);
        game_engine.Message.Clear();

        return sock;
    }
    public int GetMessage(qsocket_t sock)
    {
        if (!sock.canSend)
            if ((game_engine.net_time - sock.lastSendTime) > 1.0)
                ReSendMessage(sock);

        int ret = 0;
        EndPoint readaddr = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            int length = sock.Read(_PacketBuffer, q_shared.NET_DATAGRAMSIZE, ref readaddr);
            if (length == 0)
                break;

            if (length == -1)
            {
                game_engine.Con_Printf("Read error\n");
                return -1;
            }

            if (sock.LanDriver.AddrCompare(readaddr, sock.addr) != 0)
            {
#if DEBUG
                game_engine.Con_DPrintf("Forged packet received\n");
                game_engine.Con_DPrintf("Expected: {0}\n", StrAddr(sock.addr));
                game_engine.Con_DPrintf("Received: {0}\n", StrAddr(readaddr));
#endif
                continue;
            }

            if (length < q_shared.NET_HEADERSIZE)
            {
                shortPacketCount++;
                continue;
            }

            PacketHeader header = game_engine.BytesToStructure<PacketHeader>(_PacketBuffer, 0);

            length = game_engine.BigLong(header.length);
            int flags = length & (~q_shared.NETFLAG_LENGTH_MASK);
            length &= q_shared.NETFLAG_LENGTH_MASK;

            if ((flags & q_shared.NETFLAG_CTL) != 0)
                continue;

            uint sequence = (uint)game_engine.BigLong(header.sequence);
            packetsReceived++;

            if ((flags & q_shared.NETFLAG_UNRELIABLE) != 0)
            {
                if (sequence < sock.unreliableReceiveSequence)
                {
                    game_engine.Con_DPrintf("Got a stale datagram\n");
                    ret = 0;
                    break;
                }
                if (sequence != sock.unreliableReceiveSequence)
                {
                    int count = (int)(sequence - sock.unreliableReceiveSequence);
                    droppedDatagrams += count;
                    game_engine.Con_DPrintf("Dropped {0} datagram(s)\n", count);
                }
                sock.unreliableReceiveSequence = sequence + 1;

                length -= q_shared.NET_HEADERSIZE;

                game_engine.Message.FillFrom(_PacketBuffer, PacketHeader.SizeInBytes, length);

                ret = 2;
                break;
            }

            if ((flags & q_shared.NETFLAG_ACK) != 0)
            {
                if (sequence != (sock.sendSequence - 1))
                {
                    game_engine.Con_DPrintf("Stale ACK received\n");
                    continue;
                }
                if (sequence == sock.ackSequence)
                {
                    sock.ackSequence++;
                    if (sock.ackSequence != sock.sendSequence)
                        game_engine.Con_DPrintf("ack sequencing error\n");
                }
                else
                {
                    game_engine.Con_DPrintf("Duplicate ACK received\n");
                    continue;
                }
                sock.sendMessageLength -= q_shared.MAX_DATAGRAM;
                if (sock.sendMessageLength > 0)
                {
                    Buffer.BlockCopy(sock.sendMessage, q_shared.MAX_DATAGRAM, sock.sendMessage, 0, sock.sendMessageLength);
                    sock.sendNext = true;
                }
                else
                {
                    sock.sendMessageLength = 0;
                    sock.canSend = true;
                }
                continue;
            }

            if ((flags & q_shared.NETFLAG_DATA) != 0)
            {
                header.length = game_engine.BigLong(q_shared.NET_HEADERSIZE | q_shared.NETFLAG_ACK);
                header.sequence = game_engine.BigLong((int)sequence);

                game_engine.StructureToBytes(ref header, _PacketBuffer, 0);
                sock.Write(_PacketBuffer, q_shared.NET_HEADERSIZE, readaddr);

                if (sequence != sock.receiveSequence)
                {
                    receivedDuplicateCount++;
                    continue;
                }
                sock.receiveSequence++;

                length -= q_shared.NET_HEADERSIZE;

                if ((flags & q_shared.NETFLAG_EOM) != 0)
                {
                    game_engine.Message.Clear();
                    game_engine.Message.FillFrom(sock.receiveMessage, 0, sock.receiveMessageLength);
                    game_engine.Message.AppendFrom(_PacketBuffer, PacketHeader.SizeInBytes, length);
                    sock.receiveMessageLength = 0;

                    ret = 1;
                    break;
                }

                Buffer.BlockCopy(_PacketBuffer, PacketHeader.SizeInBytes, sock.receiveMessage, sock.receiveMessageLength, length);
                sock.receiveMessageLength += length;
                continue;
            }
        }

        if (sock.sendNext)
            SendMessageNext(sock);

        return ret;
    }
    int SendMessageNext(qsocket_t sock)
    {
        int dataLen;
        int eom;
        if (sock.sendMessageLength <= q_shared.MAX_DATAGRAM)
        {
            dataLen = sock.sendMessageLength;
            eom = q_shared.NETFLAG_EOM;
        }
        else
        {
            dataLen = q_shared.MAX_DATAGRAM;
            eom = 0;
        }
        int packetLen = q_shared.NET_HEADERSIZE + dataLen;

        PacketHeader header;
        header.length = game_engine.BigLong(packetLen | (q_shared.NETFLAG_DATA | eom));
        header.sequence = game_engine.BigLong((int)sock.sendSequence++);
        game_engine.StructureToBytes(ref header, _PacketBuffer, 0);
        Buffer.BlockCopy(sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen);

        sock.sendNext = false;

        if (sock.Write(_PacketBuffer, packetLen, sock.addr) == -1)
            return -1;

        sock.lastSendTime = game_engine.net_time;
        packetsSent++;
        return 1;
    }
    int ReSendMessage(qsocket_t sock)
    {
        int dataLen, eom;
        if (sock.sendMessageLength <= q_shared.MAX_DATAGRAM)
        {
            dataLen = sock.sendMessageLength;
            eom = q_shared.NETFLAG_EOM;
        }
        else
        {
            dataLen = q_shared.MAX_DATAGRAM;
            eom = 0;
        }
        int packetLen = q_shared.NET_HEADERSIZE + dataLen;

        PacketHeader header;
        header.length = game_engine.BigLong(packetLen | (q_shared.NETFLAG_DATA | eom));
        header.sequence = game_engine.BigLong((int)(sock.sendSequence - 1));
        game_engine.StructureToBytes(ref header, _PacketBuffer, 0);
        Buffer.BlockCopy(sock.sendMessage, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen);

        sock.sendNext = false;

        if (sock.Write(_PacketBuffer, packetLen, sock.addr) == -1)
            return -1;

        sock.lastSendTime = game_engine.net_time;
        packetsReSent++;
        return 1;
    }
    public int Datagram_SendMessage(qsocket_t sock, MsgWriter data)
    {
#if DEBUG
        if (data.IsEmpty)
            game_engine.Sys_Error("Datagram_SendMessage: zero length message\n");

        if (data.Length > q_shared.NET_MAXMESSAGE)
            game_engine.Sys_Error("Datagram_SendMessage: message too big {0}\n", data.Length);

        if (!sock.canSend)
            game_engine.Sys_Error("SendMessage: called with canSend == false\n");
#endif
        Buffer.BlockCopy(data.Data, 0, sock.sendMessage, 0, data.Length);
        sock.sendMessageLength = data.Length;

        int dataLen, eom;
        if (data.Length <= q_shared.MAX_DATAGRAM)
        {
            dataLen = data.Length;
            eom = q_shared.NETFLAG_EOM;
        }
        else
        {
            dataLen = q_shared.MAX_DATAGRAM;
            eom = 0;
        }
        int packetLen = q_shared.NET_HEADERSIZE + dataLen;

        PacketHeader header;
        header.length = game_engine.BigLong(packetLen | q_shared.NETFLAG_DATA | eom);
        header.sequence = game_engine.BigLong((int)sock.sendSequence++);
        game_engine.StructureToBytes(ref header, _PacketBuffer, 0);
        Buffer.BlockCopy(data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, dataLen);

        sock.canSend = false;

        if (sock.Write(_PacketBuffer, packetLen, sock.addr) == -1)
            return -1;

        sock.lastSendTime = game_engine.net_time;
        packetsSent++;
        return 1;
    }
    public int Datagram_SendUnreliableMessage(qsocket_t sock, MsgWriter data)
    {
        int packetLen;

#if DEBUG
        if (data.IsEmpty)
            game_engine.Sys_Error("Datagram_SendUnreliableMessage: zero length message\n");

        if (data.Length > q_shared.MAX_DATAGRAM)
            game_engine.Sys_Error("Datagram_SendUnreliableMessage: message too big {0}\n", data.Length);
#endif

        packetLen = q_shared.NET_HEADERSIZE + data.Length;

        PacketHeader header;
        header.length = game_engine.BigLong(packetLen | q_shared.NETFLAG_UNRELIABLE);
        header.sequence = game_engine.BigLong((int)sock.unreliableSendSequence++);
        game_engine.StructureToBytes(ref header, _PacketBuffer, 0);
        Buffer.BlockCopy(data.Data, 0, _PacketBuffer, PacketHeader.SizeInBytes, data.Length);

        if (sock.Write(_PacketBuffer, packetLen, sock.addr) == -1)
            return -1;

        packetsSent++;
        return 1;
    }
    public bool Datagram_CanSendMessage(qsocket_t sock)
    {
        if (sock.sendNext)
            SendMessageNext(sock);

        return sock.canSend;
    }
    public bool Datagram_CanSendUnreliableMessage(qsocket_t sock)
    {
        return true;
    }
    public void Datagram_Close(qsocket_t sock)
    {
        sock.LanDriver.CloseSocket(sock.socket);
    }
    public void Datagram_Shutdown()
    {
        //
        // shutdown the lan drivers
        //
        foreach (INetLanDriver driver in game_engine.net_landrivers)
        {
            if (driver.IsInitialized)
                driver.Shutdown();
        }

        _IsInitialized = false;
    }
    void NET_Stats_f()
    {
        if (game_engine.cmd_argc == 1)
        {
            game_engine.Con_Printf("unreliable messages sent   = %i\n", game_engine._UnreliableMessagesSent);
            game_engine.Con_Printf("unreliable messages recv   = %i\n", game_engine._UnreliableMessagesReceived);
            game_engine.Con_Printf("reliable messages sent     = %i\n", game_engine._MessagesSent);
            game_engine.Con_Printf("reliable messages received = %i\n", game_engine._MessagesReceived);
            game_engine.Con_Printf("packetsSent                = %i\n", packetsSent);
            game_engine.Con_Printf("packetsReSent              = %i\n", packetsReSent);
            game_engine.Con_Printf("packetsReceived            = %i\n", packetsReceived);
            game_engine.Con_Printf("receivedDuplicateCount     = %i\n", receivedDuplicateCount);
            game_engine.Con_Printf("shortPacketCount           = %i\n", shortPacketCount);
            game_engine.Con_Printf("droppedDatagrams           = %i\n", droppedDatagrams);
        }
        else if (game_engine.Cmd_Argv(1) == "*")
        {
            foreach (qsocket_t s in game_engine.net_activeSockets)
                PrintStats(s);

            foreach (qsocket_t s in game_engine.net_freeSockets)
                PrintStats(s);
        }
        else
        {
            qsocket_t sock = null;
            string cmdAddr = game_engine.Cmd_Argv(1);

            foreach (qsocket_t s in game_engine.net_activeSockets)
                if (game_engine.SameText(s.address, cmdAddr))
                {
                    sock = s;
                    break;
                }

            if (sock == null)
                foreach (qsocket_t s in game_engine.net_freeSockets)
                    if (game_engine.SameText(s.address, cmdAddr))
                    {
                        sock = s;
                        break;
                    }
            if (sock == null)
                return;
            PrintStats(sock);
        }
    }
    void PrintStats(qsocket_t s)
    {
        game_engine.Con_Printf("canSend = {0:4}   \n", s.canSend);
        game_engine.Con_Printf("sendSeq = {0:4}   ", s.sendSequence);
        game_engine.Con_Printf("recvSeq = {0:4}   \n", s.receiveSequence);
        game_engine.Con_Printf("\n");
    }
    static string StrAddr(EndPoint ep)
    {
        return ep.ToString();
    }
}

public class particle_t
{
    public Vector3 org;
    public float color;
    public particle_t next;
    public Vector3 vel;
    public float ramp;
    public float die;
    public ptype_t type;
}

public class viddef_t
{
    public byte[] colormap;
    public int fullbright;
    public int rowbytes;
    public int width;
    public int height;
    public float aspect;
    public int numpages;
    public bool recalc_refdef;
    public int conwidth;
    public int conheight;
    public int maxwarpwidth;
    public int maxwarpheight;
}

public class mode_t
{
    public int width;
    public int height;
    public int bpp;
    public float refreshRate;
    public bool fullScreen;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public class dqpicheader_t
{
    public int width, height;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public class lumpinfo_t
{
    public int filepos;
    public int disksize;
    public int size;
    public byte type;
    public byte compression;
    byte pad1, pad2;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] name;
}

#endregion
#region Structs
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct lump_t
{
    public int fileofs, filelen;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dmodel_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] mins; // [3];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] maxs; //[3];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] origin; // [3];
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.MAX_MAP_HULLS)]
    public int[] headnode; //[MAX_MAP_HULLS];
    public int visleafs;		// not including the solid leaf 0
    public int firstface, numfaces;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dmodel_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dheader_t
{
    public int version;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.HEADER_LUMPS)]
    public lump_t[] lumps; //[HEADER_LUMPS];

    public static int SizeInBytes = Marshal.SizeOf(typeof(dheader_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dmiptexlump_t
{
    public int nummiptex;
    public static int SizeInBytes = Marshal.SizeOf(typeof(dmiptexlump_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct miptex_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] name;
    public uint width, height;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.MIPLEVELS)]
    public uint[] offsets;

    public static int SizeInBytes = Marshal.SizeOf(typeof(miptex_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dvertex_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] point;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dvertex_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dplane_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public float[] normal; //[3];
    public float dist;
    public int type;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dplane_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dnode_t
{
    public int planenum;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public short[] children;//[2];	// negative numbers are -(leafs+1), not nodes
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] mins; //[3];		// for sphere culling
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] maxs; //[3];
    public ushort firstface;
    public ushort numfaces;	// counting both sides

    public static int SizeInBytes = Marshal.SizeOf(typeof(dnode_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dclipnode_t
{
    public int planenum;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public short[] children;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dclipnode_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct texinfo_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public float[] vecs;
    public int miptex;
    public int flags;

    public static int SizeInBytes = Marshal.SizeOf(typeof(texinfo_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dedge_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public ushort[] v;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dedge_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dface_t
{
    public short planenum;
    public short side;

    public int firstedge;
    public short numedges;
    public short texinfo;

    // lighting info
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.MAXLIGHTMAPS)]
    public byte[] styles;
    public int lightofs;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dface_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dleaf_t
{
    public int contents;
    public int visofs;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] mins;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public short[] maxs;

    public ushort firstmarksurface;
    public ushort nummarksurfaces;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.NUM_AMBIENTS)]
    public byte[] ambient_level;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dleaf_t));
}

public struct lightstyle_t
{
    //public int length;
    public string map;
}

public struct usercmd_t
{
    public Vector3 viewangles;

    // intended velocities
    public float forwardmove;
    public float sidemove;
    public float upmove;

    public void Clear()
    {
        this.viewangles = Vector3.Zero;
        this.forwardmove = 0;
        this.sidemove = 0;
        this.upmove = 0;
    }
}

public struct kbutton_t
{
    public int down0, down1;        // key nums holding it down
    public int state;			// low bit is down state

    public bool IsDown
    {
        get { return (this.state & 1) != 0; }
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct mdl_t
{
    public int ident;
    public int version;
    public v3f scale;
    public v3f scale_origin;
    public float boundingradius;
    public v3f eyeposition;
    public int numskins;
    public int skinwidth;
    public int skinheight;
    public int numverts;
    public int numtris;
    public int numframes;
    public synctype_t synctype;
    public int flags;
    public float size;

    public static readonly int SizeInBytes = Marshal.SizeOf(typeof(mdl_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct stvert_t
{
    public int onseam;
    public int s;
    public int t;

    public static int SizeInBytes = Marshal.SizeOf(typeof(stvert_t));
}

[StructLayout(LayoutKind.Sequential)]
public struct dtriangle_t
{
    public int facesfront;
    [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.I4, SizeConst = 3)]
    public int[] vertindex;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dtriangle_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct trivertx_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
    public byte[] v;
    public byte lightnormalindex;

    public static int SizeInBytes = Marshal.SizeOf(typeof(trivertx_t));

    public void Init()
    {
        if (this.v == null)
            this.v = new byte[3];
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasframe_t
{
    public trivertx_t bboxmin;	// lightnormal isn't used
    public trivertx_t bboxmax;	// lightnormal isn't used
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public byte[] name; // char[16]	// frame name from grabbing

    public static int SizeInBytes = Marshal.SizeOf(typeof(daliasframe_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasgroup_t
{
    public int numframes;
    public trivertx_t bboxmin;	// lightnormal isn't used
    public trivertx_t bboxmax;	// lightnormal isn't used

    public static int SizeInBytes = Marshal.SizeOf(typeof(daliasgroup_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasskingroup_t
{
    public int numskins;

    public static int SizeInBytes = Marshal.SizeOf(typeof(daliasskingroup_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasinterval_t
{
    public float interval;

    public static int SizeInBytes = Marshal.SizeOf(typeof(daliasinterval_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasskininterval_t
{
    public float interval;

    public static int SizeInBytes = Marshal.SizeOf(typeof(daliasskininterval_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasframetype_t
{
    public aliasframetype_t type;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct daliasskintype_t
{
    public aliasskintype_t type;

    public static int SizeInBytes = Marshal.SizeOf(typeof(daliasskintype_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dsprite_t
{
    public int ident;
    public int version;
    public int type;
    public float boundingradius;
    public int width;
    public int height;
    public int numframes;
    public float beamlength;
    public synctype_t synctype;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dsprite_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dspriteframe_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
    public int[] origin;
    public int width;
    public int height;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dspriteframe_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dspritegroup_t
{
    public int numframes;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dspritegroup_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dspriteinterval_t
{
    public float interval;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dspriteinterval_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dspriteframetype_t
{
    public spriteframetype_t type;
}

public struct mvertex_t
{
    public Vector3 position;
}

public struct maliasframedesc_t
{
    public int firstpose;
    public int numposes;
    public float interval;
    public trivertx_t bboxmin;
    public trivertx_t bboxmax;
    //public int frame;
    public string name; // char				name[16];

    public static int SizeInBytes = Marshal.SizeOf(typeof(maliasframedesc_t));

    public void Init()
    {
        this.bboxmin.Init();
        this.bboxmax.Init();
    }
}

public struct mspriteframedesc_t
{
    public spriteframetype_t type;
    public object frameptr; // mspriteframe_t or mspritegroup_t
}

public struct medge_t
{
    public ushort[] v; // [2];
    //public uint cachededgeoffset;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct v3f
{
    public float x, y, z;

    public bool IsEmpty
    {
        get { return (this.x == 0) && (this.y == 0) && (this.z == 0); }
    }
}

public struct vrect_t
{
    public int x, y, width, height;
}

public struct entity_state_t
{
    public v3f origin;
    public v3f angles;
    public int modelindex;
    public int frame;
    public int colormap;
    public int skin;
    public int effects;

    public static readonly entity_state_t Empty = new entity_state_t();
}

public struct floodfill_t
{
    public short x, y;
}

[StructLayout(LayoutKind.Explicit)]
public struct Union4b
{
    [FieldOffset(0)]
    public uint ui0;

    [FieldOffset(0)]
    public int i0;

    [FieldOffset(0)]
    public float f0;

    [FieldOffset(0)]
    public short s0;
    [FieldOffset(2)]
    public short s1;

    [FieldOffset(0)]
    public ushort us0;
    [FieldOffset(2)]
    public ushort us1;

    [FieldOffset(0)]
    public byte b0;
    [FieldOffset(1)]
    public byte b1;
    [FieldOffset(2)]
    public byte b2;
    [FieldOffset(3)]
    public byte b3;

    public static readonly Union4b Empty = new Union4b(0, 0, 0, 0);

    public Union4b(byte b0, byte b1, byte b2, byte b3)
    {
        // Shut up compiler
        this.ui0 = 0;
        this.i0 = 0;
        this.f0 = 0;
        this.s0 = 0;
        this.s1 = 0;
        this.us0 = 0;
        this.us1 = 0;
        this.b0 = b0;
        this.b1 = b1;
        this.b2 = b2;
        this.b3 = b3;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct dpackfile_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 56)]
    public byte[] name; // [56];
    public int filepos, filelen;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct dpackheader_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] id; // [4];
    [MarshalAs(UnmanagedType.I4, SizeConst = 4)]
    public int dirofs;
    [MarshalAs(UnmanagedType.I4, SizeConst = 4)]
    public int dirlen;
}

public struct keyname_t
{
    public string name;
    public int keynum;

    public keyname_t(string name, int keynum)
    {
        this.name = name;
        this.keynum = keynum;
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PacketHeader
{
    public int length;
    public int sequence;

    public static int SizeInBytes = Marshal.SizeOf(typeof(PacketHeader));
}

[StructLayout(LayoutKind.Explicit, Size = 12, Pack = 1)]
public unsafe struct eval_t
{
    [FieldOffset(0)]
    public int _string;
    [FieldOffset(0)]
    public float _float;
    [FieldOffset(0)]
    public fixed float vector[3];
    [FieldOffset(0)]
    public int function;
    [FieldOffset(0)]
    public int _int;
    [FieldOffset(0)]
    public int edict;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dedict_t
{
    public bool free;
    public int dummy1, dummy2;	 // former link_t area

    public int num_leafs;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = q_shared.MAX_ENT_LEAFS)]
    public short[] leafnums; // [MAX_ENT_LEAFS];

    public entity_state_t baseline;

    public float freetime;
    public entvars_t v;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dedict_t));
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct dstatement_t
{
    public ushort op;
    public short a, b, c;

    public static int SizeInBytes = Marshal.SizeOf(typeof(dstatement_t));

    public void SwapBytes()
    {
        this.op = (ushort)game_engine.LittleShort((short)this.op);
        this.a = game_engine.LittleShort(this.a);
        this.b = game_engine.LittleShort(this.b);
        this.c = game_engine.LittleShort(this.c);
    }
}

[StructLayout(LayoutKind.Explicit, Size = (4 * 28))]
public struct pad_int28
{
    //int pad[28];
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct entvars_t
{
    public float modelindex;
    public v3f absmin;
    public v3f absmax;
    public float ltime;
    public float movetype;
    public float solid;
    public v3f origin;
    public v3f oldorigin;
    public v3f velocity;
    public v3f angles;
    public v3f avelocity;
    public v3f punchangle;
    public string_t classname;
    public string_t model;
    public float frame;
    public float skin;
    public float effects;
    public v3f mins;
    public v3f maxs;
    public v3f size;
    public func_t touch;
    public func_t use;
    public func_t think;
    public func_t blocked;
    public float nextthink;
    public int groundentity;
    public float health;
    public float frags;
    public float weapon;
    public string_t weaponmodel;
    public float weaponframe;
    public float currentammo;
    public float ammo_shells;
    public float ammo_nails;
    public float ammo_rockets;
    public float ammo_cells;
    public float items;
    public float takedamage;
    public int chain;
    public float deadflag;
    public v3f view_ofs;
    public float button0;
    public float button1;
    public float button2;
    public float impulse;
    public float fixangle;
    public v3f v_angle;
    public float idealpitch;
    public string_t netname;
    public int enemy;
    public float flags;
    public float colormap;
    public float team;
    public float max_health;
    public float teleport_time;
    public float armortype;
    public float armorvalue;
    public float waterlevel;
    public float watertype;
    public float ideal_yaw;
    public float yaw_speed;
    public int aiment;
    public int goalentity;
    public float spawnflags;
    public string_t target;
    public string_t targetname;
    public float dmg_take;
    public float dmg_save;
    public int dmg_inflictor;
    public int owner;
    public v3f movedir;
    public string_t message;
    public float sounds;
    public string_t noise;
    public string_t noise1;
    public string_t noise2;
    public string_t noise3;

    public static int SizeInBytes = Marshal.SizeOf(typeof(entvars_t));
}

public struct gefv_cache
{
    public ddef_t pcache;
    public string field;
}

public struct prstack_t
{
    public int s;
    public dfunction_t f;
}

public struct plane_t
{
    public Vector3 normal;
    public float dist;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct portable_samplepair_t
{
    public int left;
    public int right;

    public override string ToString()
    {
        return String.Format("{{{0}, {1}}}", this.left, this.right);
    }
}

public struct glRect_t
{
    public byte l, t, w, h;
}

[StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
public struct wadinfo_t
{
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public byte[] identification;
    public int numlumps;
    public int infotableofs;
}
#endregion
#region Interfaces
public interface ICDAudioController
{
    bool IsInitialized { get; }
    bool IsEnabled { get; set; }
    bool IsPlaying { get; }
    bool IsPaused { get; }
    bool IsValidCD { get; }
    bool IsLooping { get; }
    byte[] Remap { get; }
    byte MaxTrack { get; }
    byte CurrentTrack { get; }
    float Volume { get; set; }
    void Init();
    void Play(byte track, bool looping);
    void Stop();
    void Pause();
    void Resume();
    void Shutdown();
    void Update();
    void CDAudio_GetAudioDiskInfo();
    void CloseDoor();
    void Edject();
}

public interface INetDriver
{
    string Name { get; }
    bool IsInitialized { get; }
    void Init();
    void Datagram_Listen(bool state);
    void Datagram_SearchForHosts(bool xmit);
    qsocket_t Datagram_Connect(string host);
    qsocket_t Datagram_CheckNewConnections();
    int GetMessage(qsocket_t sock);
    int Datagram_SendMessage(qsocket_t sock, MsgWriter data);
    int Datagram_SendUnreliableMessage(qsocket_t sock, MsgWriter data);
    bool Datagram_CanSendMessage(qsocket_t sock);
    bool Datagram_CanSendUnreliableMessage(qsocket_t sock);
    void Datagram_Close(qsocket_t sock);
    void Datagram_Shutdown();
}

public interface INetLanDriver
{
    string Name { get; }
    bool IsInitialized { get; }
    Socket ControlSocket { get; }
    bool UDP_Init();
    void Shutdown();
    void UDP_Listen(bool state);
    Socket OpenSocket(int port);
    int CloseSocket(Socket socket);
    int Connect(Socket socket, EndPoint addr);
    Socket CheckNewConnections();
    int Read(Socket socket, byte[] buf, int len, ref EndPoint ep);
    int Write(Socket socket, byte[] buf, int len, EndPoint ep);
    int Broadcast(Socket socket, byte[] buf, int len);
    string GetNameFromAddr(EndPoint addr);
    EndPoint GetAddrFromName(string name);
    int AddrCompare(EndPoint addr1, EndPoint addr2);
    int GetSocketPort(EndPoint addr);
    int SetSocketPort(EndPoint addr, int port);
}

public interface ISoundController
{
    bool IsInitialized { get; }
    void Init();
    void Shutdown();
    void ClearBuffer();
    byte[] LockBuffer();
    void UnlockBuffer(int count);
    int GetPosition();
    //void Submit();
}
#endregion
#region Enums
public enum cmd_source_t
{
    src_client,     // came in over a net connection as a clc_stringcmd
                    // host_client will be valid during this state.
    src_command		// from the command buffer
}
public enum cactive_t
{
    ca_dedicated, 		// a dedicated server with no ability to start a client
    ca_disconnected, 	// full screen console with no connection
    ca_connected		// valid netcon, talking to a server
}
public enum modtype_t
{
    mod_brush, mod_sprite, mod_alias
}
public enum synctype_t
{
    ST_SYNC = 0, ST_RAND
}
public enum aliasframetype_t
{
    ALIAS_SINGLE = 0, ALIAS_GROUP
}
public enum aliasskintype_t
{
    ALIAS_SKIN_SINGLE = 0, ALIAS_SKIN_GROUP
}
public enum spriteframetype_t
{
    SPR_SINGLE = 0, SPR_GROUP
}
public enum GameKind
{
    StandardQuake, Rogue, Hipnotic
}
public enum MTexTarget
{
    TEXTURE0_SGIS = 0x835E,
    TEXTURE1_SGIS = 0x835F
}
public enum etype_t
{
    ev_void, ev_string, ev_float, ev_vector, ev_entity, ev_field, ev_function, ev_pointer
}
public enum OP
{
    OP_DONE,
    OP_MUL_F,
    OP_MUL_V,
    OP_MUL_FV,
    OP_MUL_VF,
    OP_DIV_F,
    OP_ADD_F,
    OP_ADD_V,
    OP_SUB_F,
    OP_SUB_V,

    OP_EQ_F,
    OP_EQ_V,
    OP_EQ_S,
    OP_EQ_E,
    OP_EQ_FNC,

    OP_NE_F,
    OP_NE_V,
    OP_NE_S,
    OP_NE_E,
    OP_NE_FNC,

    OP_LE,
    OP_GE,
    OP_LT,
    OP_GT,

    OP_LOAD_F,
    OP_LOAD_V,
    OP_LOAD_S,
    OP_LOAD_ENT,
    OP_LOAD_FLD,
    OP_LOAD_FNC,

    OP_ADDRESS,

    OP_STORE_F,
    OP_STORE_V,
    OP_STORE_S,
    OP_STORE_ENT,
    OP_STORE_FLD,
    OP_STORE_FNC,

    OP_STOREP_F,
    OP_STOREP_V,
    OP_STOREP_S,
    OP_STOREP_ENT,
    OP_STOREP_FLD,
    OP_STOREP_FNC,

    OP_RETURN,
    OP_NOT_F,
    OP_NOT_V,
    OP_NOT_S,
    OP_NOT_ENT,
    OP_NOT_FNC,
    OP_IF,
    OP_IFNOT,
    OP_CALL0,
    OP_CALL1,
    OP_CALL2,
    OP_CALL3,
    OP_CALL4,
    OP_CALL5,
    OP_CALL6,
    OP_CALL7,
    OP_CALL8,
    OP_STATE,
    OP_GOTO,
    OP_AND,
    OP_OR,

    OP_BITAND,
    OP_BITOR
}
public enum keydest_t
{
    key_game, key_console, key_message, key_menu
}
public enum server_state_t
{
    Loading, Active
}
public enum ptype_t
{
    pt_static,
    pt_grav,
    pt_slowgrav,
    pt_fire,
    pt_explode,
    pt_explode2,
    pt_blob,
    pt_blob2
}
#endregion
#region Byte order converters

public interface IByteOrderConverter
{
    short BigShort(short l);
    short LittleShort(short l);
    int BigLong(int l);
    int LittleLong(int l);
    float BigFloat(float l);
    float LittleFloat(float l);
}

public  static class SwapHelper
{
    public static short ShortSwap(short l)
    {
        byte b1, b2;

        b1 = (byte)(l & 255);
        b2 = (byte)((l >> 8) & 255);

        return (short)((b1 << 8) + b2);
    }

    public static int LongSwap(int l)
    {
        byte b1, b2, b3, b4;

        b1 = (byte)(l & 255);
        b2 = (byte)((l >> 8) & 255);
        b3 = (byte)((l >> 16) & 255);
        b4 = (byte)((l >> 24) & 255);

        return ((int)b1 << 24) + ((int)b2 << 16) + ((int)b3 << 8) + b4;
    }

    public static float FloatSwap(float f)
    {
        byte[] bytes = BitConverter.GetBytes(f);
        byte[] bytes2 = new byte[4];

        bytes2[0] = bytes[3];
        bytes2[1] = bytes[2];
        bytes2[2] = bytes[1];
        bytes2[3] = bytes[0];

        return BitConverter.ToSingle(bytes2, 0);
    }

    public static void Swap4b(byte[] buff, int offset)
    {
        byte b1, b2, b3, b4;

        b1 = buff[offset + 0];
        b2 = buff[offset + 1];
        b3 = buff[offset + 2];
        b4 = buff[offset + 3];

        buff[offset + 0] = b4;
        buff[offset + 1] = b3;
        buff[offset + 2] = b2;
        buff[offset + 3] = b1;
    }
}

public  class LittleEndianConverter : IByteOrderConverter
{
    #region IByteOrderConverter Members

    short IByteOrderConverter.BigShort(short l)
    {
        return SwapHelper.ShortSwap(l);
    }

    short IByteOrderConverter.LittleShort(short l)
    {
        return l;
    }

    int IByteOrderConverter.BigLong(int l)
    {
        return SwapHelper.LongSwap(l);
    }

    int IByteOrderConverter.LittleLong(int l)
    {
        return l;
    }

    float IByteOrderConverter.BigFloat(float l)
    {
        return SwapHelper.FloatSwap(l);
    }

    float IByteOrderConverter.LittleFloat(float l)
    {
        return l;
    }

    #endregion
}

public  class BigEndianConverter : IByteOrderConverter
{
    #region IByteOrderConverter Members

    short IByteOrderConverter.BigShort(short l)
    {
        return l;
    }

    short IByteOrderConverter.LittleShort(short l)
    {
        return SwapHelper.ShortSwap(l);
    }

    int IByteOrderConverter.BigLong(int l)
    {
        return l;
    }

    int IByteOrderConverter.LittleLong(int l)
    {
        return SwapHelper.LongSwap(l);
    }

    float IByteOrderConverter.BigFloat(float l)
    {
        return l;
    }

    float IByteOrderConverter.LittleFloat(float l)
    {
        return SwapHelper.FloatSwap(l);
    }

    #endregion
}

#endregion