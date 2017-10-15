using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    static int[] _Ramp1 = new int[] { 0x6f, 0x6d, 0x6b, 0x69, 0x67, 0x65, 0x63, 0x61 };
    static int[] _Ramp2 = new int[] { 0x6f, 0x6e, 0x6d, 0x6c, 0x6b, 0x6a, 0x68, 0x66 };
    static int[] _Ramp3 = new int[] { 0x6d, 0x6b, 6, 5, 4, 3 };

    static byte[,] _DotTexture = new byte[8, 8]
    {
	    {0,1,1,0,0,0,0,0},
	    {1,1,1,1,0,0,0,0},
	    {1,1,1,1,0,0,0,0},
	    {0,1,1,0,0,0,0,0},
	    {0,0,0,0,0,0,0,0},
	    {0,0,0,0,0,0,0,0},
	    {0,0,0,0,0,0,0,0},
	    {0,0,0,0,0,0,0,0},
    };

    static int r_numparticles;
    static particle_t[] _Particles;
    static int particletexture;

    static particle_t active_particles;
    static particle_t free_particles;
    static int _TracerCount; // static tracercount from RocketTrail()
    static Vector3[] avelocities = new Vector3[q_shared.NUMVERTEXNORMALS];
    static float beamlength = 16;

    static void R_InitParticles()
    {
	    int i = COM_CheckParm("-particles");
	    if (i > 0 && i < com_argv.Length - 1)
	    {
            r_numparticles = int.Parse(Argv(i + 1));
		    if (r_numparticles < q_shared.ABSOLUTE_MIN_PARTICLES)
			    r_numparticles = q_shared.ABSOLUTE_MIN_PARTICLES;
	    }
	    else
		    r_numparticles = q_shared.MAX_PARTICLES;

        _Particles = new particle_t[r_numparticles];
        for (i = 0; i < r_numparticles; i++)
            _Particles[i] = new particle_t();
    }
    static void R_InitParticleTexture()
    {
        particletexture = GenerateTextureNumber();// texture_extension_number++;
        GL_Bind(particletexture);

        byte[,,] data = new byte[8, 8, 4];
        for (int x = 0; x < 8; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                data[y, x, 0] = 255;
                data[y, x, 1] = 255;
                data[y, x, 2] = 255;
                data[y, x, 3] = (byte)(_DotTexture[x, y] * 255);
            }
        }
        GL.TexImage2D(TextureTarget.Texture2D, 0, gl_alpha_format, 8, 8, 0, PixelFormat.Rgba, PixelType.UnsignedByte, data);
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
        SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);
    }
    static void R_ClearParticles()
    {
        free_particles = _Particles[0];
        active_particles = null;

        for (int i = 0; i < r_numparticles - 1; i++)
            _Particles[i].next = _Particles[i + 1];
        _Particles[r_numparticles - 1].next = null;
    }
    public static void R_RocketTrail(ref Vector3 start, ref Vector3 end, int type)
    {
        Vector3 vec = end - start;
        float len = Mathlib.Normalize(ref vec);
        int dec;
        if (type < 128)
            dec = 3;
        else
        {
            dec = 1;
            type -= 128;
        }

        while (len > 0)
        {
            len -= dec;

            particle_t p = AllocParticle();
            if (p == null)
                return;

            p.vel = Vector3.Zero;
            p.die = (float)cl.time + 2;

            switch (type)
            {
                case 0:	// rocket trail
                    p.ramp = (Random() & 3);
                    p.color = _Ramp3[(int)p.ramp];
                    p.type = ptype_t.pt_fire;
                    p.org = new Vector3(start.X + ((Random() % 6) - 3),
                        start.Y + ((Random() % 6) - 3), start.Z + ((Random() % 6) - 3));
                    break;

                case 1:	// smoke smoke
                    p.ramp = (Random() & 3) + 2;
                    p.color = _Ramp3[(int)p.ramp];
                    p.type = ptype_t.pt_fire;
                    p.org = new Vector3(start.X + ((Random() % 6) - 3),
                        start.Y + ((Random() % 6) - 3), start.Z + ((Random() % 6) - 3));
                    break;

                case 2:	// blood
                    p.type = ptype_t.pt_grav;
                    p.color = 67 + (Random() & 3);
                    p.org = new Vector3(start.X + ((Random() % 6) - 3),
                        start.Y + ((Random() % 6) - 3), start.Z + ((Random() % 6) - 3));
                    break;

                case 3:
                case 5:	// tracer
                    p.die = (float)cl.time + 0.5f;
                    p.type = ptype_t.pt_static;
                    if (type == 3)
                        p.color = 52 + ((_TracerCount & 4) << 1);
                    else
                        p.color = 230 + ((_TracerCount & 4) << 1);

                    _TracerCount++;

                    p.org = start;
                    if ((_TracerCount & 1) != 0)
                    {
                        p.vel.X = 30 * vec.Y; // Uze: why???
                        p.vel.Y = 30 * -vec.X;
                    }
                    else
                    {
                        p.vel.X = 30 * -vec.Y;
                        p.vel.Y = 30 * vec.X;
                    }
                    break;

                case 4:	// slight blood
                    p.type = ptype_t.pt_grav;
                    p.color = 67 + (Random() & 3);
                    p.org = new Vector3(start.X + ((Random() % 6) - 3),
                        start.Y + ((Random() % 6) - 3), start.Z + ((Random() % 6) - 3));
                    len -= 3;
                    break;

                case 6:	// voor trail
                    p.color = 9 * 16 + 8 + (Random() & 3);
                    p.type = ptype_t.pt_static;
                    p.die = (float)cl.time + 0.3f;
                    p.org = new Vector3(start.X + ((Random() % 15) - 8),
                        start.Y + ((Random() % 15) - 8), start.Z + ((Random() % 15) - 8));
                    break;
            }

            start += vec;
        }
    }
    private static void R_DrawParticles()
    {
        GL_Bind(particletexture);
        GL.Enable(EnableCap.Blend);
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Modulate);
        GL.Begin(BeginMode.Triangles);

        Vector3 up = vup * 1.5f;
        Vector3 right = vright * 1.5f;
        float frametime = (float)(cl.time - cl.oldtime);
        float time3 = frametime * 15;
        float time2 = frametime * 10;
        float time1 = frametime * 5;
        float grav = frametime * sv_gravity.value * 0.05f;
        float dvel = 4 * frametime;

        while (true)
        {
            particle_t kill = active_particles;
            if (kill != null && kill.die < cl.time)
            {
                active_particles = kill.next;
                kill.next = free_particles;
                free_particles = kill;
                continue;
            }
            break;
        }

        for (particle_t p = active_particles; p != null; p = p.next)
        {
            while (true)
            {
                particle_t kill = p.next;
                if (kill != null && kill.die < cl.time)
                {
                    p.next = kill.next;
                    kill.next = free_particles;
                    free_particles = kill;
                    continue;
                }
                break;
            }

            // hack a scale up to keep particles from disapearing
            float scale = Vector3.Dot((p.org - r_origin), vpn);
            if (scale < 20)
                scale = 1;
            else
                scale = 1 + scale * 0.004f;

            // Uze todo: check if this is correct
            uint color = d_8to24table[(byte)p.color];
            GL.Color4((byte)(color & 0xff), (byte)((color >> 8) & 0xff), (byte)((color >> 16) & 0xff), (byte)((color >> 24) & 0xff));
            GL.TexCoord2(0f, 0);
            GL.Vertex3(p.org);
            GL.TexCoord2(1f, 0);
            Vector3 v = p.org + up * scale;
            GL.Vertex3(v);
            GL.TexCoord2(0f, 1);
            v = p.org + right * scale;
            GL.Vertex3(v);

            p.org += p.vel * frametime;

            switch (p.type)
            {
                case ptype_t.pt_static:
                    break;

                case ptype_t.pt_fire:
                    p.ramp += time1;
                    if (p.ramp >= 6)
                        p.die = -1;
                    else
                        p.color = _Ramp3[(int)p.ramp];
                    p.vel.Z += grav;
                    break;

                case ptype_t.pt_explode:
                    p.ramp += time2;
                    if (p.ramp >= 8)
                        p.die = -1;
                    else
                        p.color = _Ramp1[(int)p.ramp];
                    p.vel += p.vel * dvel;
                    p.vel.Z -= grav;
                    break;

                case ptype_t.pt_explode2:
                    p.ramp += time3;
                    if (p.ramp >= 8)
                        p.die = -1;
                    else
                        p.color = _Ramp2[(int)p.ramp];
                    p.vel -= p.vel * frametime;
                    p.vel.Z -= grav;
                    break;

                case ptype_t.pt_blob:
                    p.vel += p.vel * dvel;
                    p.vel.Z -= grav;
                    break;

                case ptype_t.pt_blob2:
                    p.vel -= p.vel * dvel;
                    p.vel.Z -= grav;
                    break;

                case ptype_t.pt_grav:
                case ptype_t.pt_slowgrav:
                    p.vel.Z -= grav;
                    break;
            }
        }
        GL.End();
        GL.Disable(EnableCap.Blend);
        GL.TexEnv(TextureEnvTarget.TextureEnv, TextureEnvParameter.TextureEnvMode, (int)TextureEnvMode.Replace);
    }
    public static void R_ParticleExplosion(ref Vector3 org)
    {
        for (int i = 0; i < 1024; i++) // Uze: Why 1024 if MAX_PARTICLES = 2048?
        {
            particle_t p = AllocParticle();
            if (p == null)
                return;

            p.die = (float)cl.time + 5;
            p.color = _Ramp1[0];
            p.ramp = Random() & 3;
            if ((i & 1) != 0)
                p.type = ptype_t.pt_explode;
            else
                p.type = ptype_t.pt_explode2;
            p.org = org + new Vector3((Random() % 32) - 16, (Random() % 32) - 16, (Random() % 32) - 16);
            p.vel = new Vector3((Random() % 512) - 256, (Random() % 512) - 256, (Random() % 512) - 256);
        }
    }
    public static void R_RunParticleEffect(ref Vector3 org, ref Vector3 dir, int color, int count)
    {
        for (int i = 0; i < count; i++)
        {
            particle_t p = AllocParticle();
            if (p == null)
                return;

            if (count == 1024)
            {	// rocket explosion
                p.die = (float)cl.time + 5;
                p.color = _Ramp1[0];
                p.ramp = Random() & 3;
                if ((i & 1) != 0)
                    p.type = ptype_t.pt_explode;
                else
                    p.type = ptype_t.pt_explode2;
                p.org = org + new Vector3((Random() % 32) - 16, (Random() % 32) - 16, (Random() % 32) - 16);
                p.vel = new Vector3((Random() % 512) - 256, (Random() % 512) - 256, (Random() % 512) - 256);
            }
            else
            {
                p.die = (float)cl.time + 0.1f * (Random() % 5);
                p.color = (color & ~7) + (Random() & 7);
                p.type = ptype_t.pt_slowgrav;
                p.org = org + new Vector3((Random() & 15) - 8, (Random() & 15) - 8, (Random() & 15) - 8);
                p.vel = dir * 15.0f;
            }
        }
    }
    private static particle_t AllocParticle()
    {
        if (free_particles == null)
            return null;
            
        particle_t p = free_particles;
        free_particles = p.next;
        p.next = active_particles;
        active_particles = p;
            
        return p;
    }
    public static void R_ParseParticleEffect()
    {
        Vector3 org = Reader.ReadCoords();
        Vector3 dir = new Vector3(Reader.MSG_ReadChar() * ONE_OVER_16,
            Reader.MSG_ReadChar() * ONE_OVER_16,
            Reader.MSG_ReadChar() * ONE_OVER_16);
        int count = Reader.MSG_ReadByte();
        int color = Reader.MSG_ReadByte();

        if (count == 255)
            count = 1024;

        R_RunParticleEffect(ref org, ref dir, color, count);
    }
    public static void R_TeleportSplash(ref Vector3 org)
    {
        for (int i = -16; i < 16; i += 4)
            for (int j = -16; j < 16; j += 4)
                for (int k = -24; k < 32; k += 4)
                {
                    particle_t p = AllocParticle();
                    if (p == null)
                        return;

                    p.die = (float)(cl.time + 0.2 + (Random() & 7) * 0.02);
                    p.color = 7 + (Random() & 7);
                    p.type = ptype_t.pt_slowgrav;

                    Vector3 dir = new Vector3(j * 8, i * 8, k * 8);

                    p.org = org + new Vector3(i + (Random() & 3), j + (Random() & 3), k + (Random() & 3));

                    Mathlib.Normalize(ref dir);
                    float vel = 50 + (Random() & 63);
                    p.vel = dir * vel;
                }
    }
    public static void R_LavaSplash(ref Vector3 org)
    {
        Vector3 dir;

        for (int i = -16; i < 16; i++)
            for (int j = -16; j < 16; j++)
                for (int k = 0; k < 1; k++)
                {
                    particle_t p = AllocParticle();
                    if (p == null)
                        return;

                    p.die = (float)(cl.time + 2 + (Random() & 31) * 0.02);
                    p.color = 224 + (Random() & 7);
                    p.type = ptype_t.pt_slowgrav;

                    dir.X = j * 8 + (Random() & 7);
                    dir.Y = i * 8 + (Random() & 7);
                    dir.Z = 256;

                    p.org = org + dir;
                    p.org.Z += Random() & 63;

                    Mathlib.Normalize(ref dir);
                    float vel = 50 + (Random() & 63);
                    p.vel = dir * vel;
                }
    }
    public static void R_ParticleExplosion2(ref Vector3 org, int colorStart, int colorLength)
    {
        int colorMod = 0;

        for (int i = 0; i < 512; i++)
        {
            particle_t p = AllocParticle();
            if (p == null)
                return;

            p.die = (float)(cl.time + 0.3);
            p.color = colorStart + (colorMod % colorLength);
            colorMod++;

            p.type = ptype_t.pt_blob;
            p.org = org + new Vector3((Random() % 32) - 16, (Random() % 32) - 16, (Random() % 32) - 16);
            p.vel = new Vector3((Random() % 512) - 256, (Random() % 512) - 256, (Random() % 512) - 256);
        }
    }
    public static void R_BlobExplosion(ref Vector3 org)
    {
        for (int i = 0; i < 1024; i++)
        {
            particle_t p = AllocParticle();
            if (p == null)
                return;

            p.die = (float)(cl.time + 1 + (Random() & 8) * 0.05);

            if ((i & 1) != 0)
            {
                p.type = ptype_t.pt_blob;
                p.color = 66 + Random() % 6;
            }
            else
            {
                p.type = ptype_t.pt_blob2;
                p.color = 150 + Random() % 6;
            }
            p.org = org + new Vector3((Random() % 32) - 16, (Random() % 32) - 16, (Random() % 32) - 16);
            p.vel = new Vector3((Random() % 512) - 256, (Random() % 512) - 256, (Random() % 512) - 256);
        }
    }
    public static void R_EntityParticles(entity_t ent)
    {
        float dist = 64;

        if (avelocities[0].X == 0)
        {
            for (int i = 0; i < q_shared.NUMVERTEXNORMALS; i++)
            {
                avelocities[i].X = (Random() & 255) * 0.01f;
                avelocities[i].Y = (Random() & 255) * 0.01f;
                avelocities[i].Z = (Random() & 255) * 0.01f;
            }
        }

        for (int i = 0; i < q_shared.NUMVERTEXNORMALS; i++)
        {
            double angle = cl.time * avelocities[i].X;
            double sy = Math.Sin(angle);
            double cy = Math.Cos(angle);
            angle = cl.time * avelocities[i].Y;
            double sp = Math.Sin(angle);
            double cp = Math.Cos(angle);
            angle = cl.time * avelocities[i].Z;
            double sr = Math.Sin(angle);
            double cr = Math.Cos(angle);

            Vector3 forward = new Vector3((float)(cp * cy), (float)(cp * sy), (float)-sp);
            particle_t p = AllocParticle();
            if (p == null)
                return;

            p.die = (float)(cl.time + 0.01);
            p.color = 0x6f;
            p.type = ptype_t.pt_explode;

            p.org = ent.origin + anorms[i] * dist + forward * beamlength;
        }
    }
}