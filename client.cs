using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using OpenTK;

public static partial class game_engine
{
    public static client_static_t cls = new client_static_t();
    public static client_state_t cl = new client_state_t();

    public static efrag_t[] cl_efrags = new efrag_t[q_shared.MAX_EFRAGS];
    public static entity_t[] cl_entities = new entity_t[q_shared.MAX_EDICTS];
    public static entity_t[] cl_static_entities = new entity_t[q_shared.MAX_STATIC_ENTITIES];
    public static lightstyle_t[] cl_lightstyle = new lightstyle_t[q_shared.MAX_LIGHTSTYLES];
    public static dlight_t[] cl_dlights = new dlight_t[q_shared.MAX_DLIGHTS];

    public static cvar_t cl_name;
    public static cvar_t cl_color;
    public static cvar_t cl_shownet;
    public static cvar_t cl_nolerp;
    public static cvar_t lookspring;
    public static cvar_t lookstrafe;
    public static cvar_t sensitivity;
    public static cvar_t m_pitch;
    public static cvar_t m_yaw;
    public static cvar_t m_forward;
    public static cvar_t m_side;

    public static int cl_numvisedicts;
    public static entity_t[] cl_visedicts = new entity_t[q_shared.MAX_VISEDICTS];
}