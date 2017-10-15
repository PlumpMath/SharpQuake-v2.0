using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    static int num_temp_entities;
    static entity_t[] cl_temp_entities = new entity_t[q_shared.MAX_TEMP_ENTITIES];
    static beam_t[] cl_beams = new beam_t[q_shared.MAX_BEAMS];

    static sfx_t cl_sfx_wizhit;
    static sfx_t cl_sfx_knighthit;
    static sfx_t cl_sfx_tink1;
    static sfx_t cl_sfx_ric1;
    static sfx_t cl_sfx_ric2;
    static sfx_t cl_sfx_ric3;
    static sfx_t cl_sfx_r_exp3;

    public static void CL_InitTEnts()
    {
        cl_sfx_wizhit = S_PrecacheSound("wizard/hit.wav");
        cl_sfx_knighthit = S_PrecacheSound("hknight/hit.wav");
        cl_sfx_tink1 = S_PrecacheSound("weapons/tink1.wav");
        cl_sfx_ric1 = S_PrecacheSound("weapons/ric1.wav");
        cl_sfx_ric2 = S_PrecacheSound("weapons/ric2.wav");
        cl_sfx_ric3 = S_PrecacheSound("weapons/ric3.wav");
        cl_sfx_r_exp3 = S_PrecacheSound("weapons/r_exp3.wav");

        for (int i = 0; i < cl_temp_entities.Length; i++)
            cl_temp_entities[i] = new entity_t();

        for (int i = 0; i < cl_beams.Length; i++)
            cl_beams[i] = new beam_t();
    }
    public static void CL_UpdateTEnts()
    {
        num_temp_entities = 0;

        // update lightning
        for (int i = 0; i < q_shared.MAX_BEAMS; i++)
        {
            beam_t b = cl_beams[i];
            if (b.model == null || b.endtime < cl.time)
                continue;

            // if coming from the player, update the start position
            if (b.entity == cl.viewentity)
            {
                b.start = cl_entities[cl.viewentity].origin;
            }

            // calculate pitch and yaw
            Vector3 dist = b.end - b.start;
            float yaw, pitch, forward;

            if (dist.Y == 0 && dist.X == 0)
            {
                yaw = 0;
                if (dist.Z > 0)
                    pitch = 90;
                else
                    pitch = 270;
            }
            else
            {
                yaw = (int)(Math.Atan2(dist.Y, dist.X) * 180 / Math.PI);
                if (yaw < 0)
                    yaw += 360;

                forward = (float)Math.Sqrt(dist.X * dist.X + dist.Y * dist.Y);
                pitch = (int)(Math.Atan2(dist.Z, forward) * 180 / Math.PI);
                if (pitch < 0)
                    pitch += 360;
            }

            // add new entities for the lightning
            Vector3 org = b.start;
            float d = Mathlib.Normalize(ref dist);
            while (d > 0)
            {
                entity_t ent = CL_NewTempEntity();
                if (ent == null)
                    return;

                ent.origin = org;
                ent.model = b.model;
                ent.angles.X = pitch;
                ent.angles.Y = yaw;
                ent.angles.Z = Random() % 360;

                org += dist * 30;
                // Uze: is this code bug (i is outer loop variable!!!) or what??????????????
                //for (i=0 ; i<3 ; i++)
                //    org[i] += dist[i]*30;
                d -= 30;
            }
        }
    }
    public static entity_t CL_NewTempEntity()
    {
        if (cl_numvisedicts == q_shared.MAX_VISEDICTS)
            return null;
        if (num_temp_entities == q_shared.MAX_TEMP_ENTITIES)
            return null;

        entity_t ent = cl_temp_entities[num_temp_entities];
        num_temp_entities++;
        cl_visedicts[cl_numvisedicts] = ent;
        cl_numvisedicts++;

        ent.colormap = vid.colormap;

        return ent;
    }
    public static void CL_ParseTEnt()
    {
        Vector3 pos;
        dlight_t dl;
        int type = Reader.MSG_ReadByte();
        switch (type)
        {
            case q_shared.TE_WIZSPIKE:			// spike hitting wall
                pos = Reader.ReadCoords();
                R_RunParticleEffect(ref pos, ref q_shared.ZeroVector, 20, 30);
                S_StartSound(-1, 0, cl_sfx_wizhit, ref pos, 1, 1);
                break;

            case q_shared.TE_KNIGHTSPIKE:			// spike hitting wall
                pos = Reader.ReadCoords();
                R_RunParticleEffect(ref pos, ref q_shared.ZeroVector, 226, 20);
                S_StartSound(-1, 0, cl_sfx_knighthit, ref pos, 1, 1);
                break;

            case q_shared.TE_SPIKE:			// spike hitting wall
                pos = Reader.ReadCoords();
#if GLTEST
                Test_Spawn (pos);
#else
                R_RunParticleEffect(ref pos, ref q_shared.ZeroVector, 0, 10);
#endif
                if ((Random() % 5) != 0)
                    S_StartSound(-1, 0, cl_sfx_tink1, ref pos, 1, 1);
                else
                {
                    int rnd = Random() & 3;
                    if (rnd == 1)
                        S_StartSound(-1, 0, cl_sfx_ric1, ref pos, 1, 1);
                    else if (rnd == 2)
                        S_StartSound(-1, 0, cl_sfx_ric2, ref pos, 1, 1);
                    else
                        S_StartSound(-1, 0, cl_sfx_ric3, ref pos, 1, 1);
                }
                break;

            case q_shared.TE_SUPERSPIKE:			// super spike hitting wall
                pos = Reader.ReadCoords();
                R_RunParticleEffect(ref pos, ref q_shared.ZeroVector, 0, 20);

                if ((Random() % 5) != 0)
                    S_StartSound(-1, 0, cl_sfx_tink1, ref pos, 1, 1);
                else
                {
                    int rnd = Random() & 3;
                    if (rnd == 1)
                        S_StartSound(-1, 0, cl_sfx_ric1, ref pos, 1, 1);
                    else if (rnd == 2)
                        S_StartSound(-1, 0, cl_sfx_ric2, ref pos, 1, 1);
                    else
                        S_StartSound(-1, 0, cl_sfx_ric3, ref pos, 1, 1);
                }
                break;

            case q_shared.TE_GUNSHOT:			// bullet hitting wall
                pos = Reader.ReadCoords();
                R_RunParticleEffect(ref pos, ref q_shared.ZeroVector, 0, 20);
                break;

            case q_shared.TE_EXPLOSION:			// rocket explosion
                pos = Reader.ReadCoords();
                R_ParticleExplosion(ref pos);
                dl = CL_AllocDlight(0);
                dl.origin = pos;
                dl.radius = 350;
                dl.die = (float)cl.time + 0.5f;
                dl.decay = 300;
                S_StartSound(-1, 0, cl_sfx_r_exp3, ref pos, 1, 1);
                break;

            case q_shared.TE_TAREXPLOSION:			// tarbaby explosion
                pos = Reader.ReadCoords();
                R_BlobExplosion(ref pos);
                S_StartSound(-1, 0, cl_sfx_r_exp3, ref pos, 1, 1);
                break;

            case q_shared.TE_LIGHTNING1:				// lightning bolts
                CL_ParseBeam(Mod_ForName("progs/bolt.mdl", true));
                break;

            case q_shared.TE_LIGHTNING2:				// lightning bolts
                CL_ParseBeam(Mod_ForName("progs/bolt2.mdl", true));
                break;

            case q_shared.TE_LIGHTNING3:				// lightning bolts
                CL_ParseBeam(Mod_ForName("progs/bolt3.mdl", true));
                break;

            // PGM 01/21/97 
            case q_shared.TE_BEAM:				// grappling hook beam
                CL_ParseBeam(Mod_ForName("progs/beam.mdl", true));
                break;
            // PGM 01/21/97

            case q_shared.TE_LAVASPLASH:
                pos = Reader.ReadCoords();
                R_LavaSplash(ref pos);
                break;

            case q_shared.TE_TELEPORT:
                pos = Reader.ReadCoords();
                R_TeleportSplash(ref pos);
                break;

            case q_shared.TE_EXPLOSION2:				// color mapped explosion
                pos = Reader.ReadCoords();
                int colorStart = Reader.MSG_ReadByte();
                int colorLength = Reader.MSG_ReadByte();
                R_ParticleExplosion2(ref pos, colorStart, colorLength);
                dl = CL_AllocDlight(0);
                dl.origin = pos;
                dl.radius = 350;
                dl.die = (float)cl.time + 0.5f;
                dl.decay = 300;
                S_StartSound(-1, 0, cl_sfx_r_exp3, ref pos, 1, 1);
                break;

            default:
                Sys_Error("CL_ParseTEnt: bad type");
                break;
        }
    }
    public static void CL_ParseBeam(model_t m)
    {
        int ent = Reader.MSG_ReadShort();

        Vector3 start = Reader.ReadCoords();
        Vector3 end = Reader.ReadCoords();

        // override any beam with the same entity
        for (int i = 0; i < q_shared.MAX_BEAMS; i++)
        {
            beam_t b = cl_beams[i];
            if (b.entity == ent)
            {
                b.entity = ent;
                b.model = m;
                b.endtime = (float)(cl.time + 0.2);
                b.start = start;
                b.end = end;
                return;
            }
        }

        // find a free beam
        for (int i = 0; i < q_shared.MAX_BEAMS; i++)
        {
            beam_t b = cl_beams[i];
            if (b.model == null || b.endtime < cl.time)
            {
                b.entity = ent;
                b.model = m;
                b.endtime = (float)(cl.time + 0.2);
                b.start = start;
                b.end = end;
                return;
            }
        }
        Con_Printf("beam list overflow!\n");
    }
}