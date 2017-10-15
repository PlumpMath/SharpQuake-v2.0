using System;
using OpenTK;

static class Mathlib
{
    public static void AngleVectors(ref Vector3 angles, out Vector3 forward, out Vector3 right, out Vector3 up)
    {
        double angle, sr, sp, sy, cr, cp, cy;

        angle = angles.Y * (Math.PI * 2 / 360);
        sy = Math.Sin(angle);
        cy = Math.Cos(angle);
        angle = angles.X * (Math.PI * 2 / 360);
        sp = Math.Sin(angle);
        cp = Math.Cos(angle);
        angle = angles.Z * (Math.PI * 2 / 360);
        sr = Math.Sin(angle);
        cr = Math.Cos(angle);

        forward.X = (float)(cp * cy);
        forward.Y = (float)(cp * sy);
        forward.Z = (float)-sp;
        right.X = (float)(-1 * sr * sp * cy + -1 * cr * -sy);
        right.Y = (float)(-1 * sr * sp * sy + -1 * cr * cy);
        right.Z = (float)(-1 * sr * cp);
        up.X = (float)(cr * sp * cy + -sr * -sy);
        up.Y = (float)(cr * sp * sy + -sr * cy);
        up.Z = (float)(cr * cp);
    }
    public static float Normalize(ref Vector3 v)
    {
        float length = v.Length;
        if (length != 0)
        {
            float ool = 1 / length;
            v.X *= ool;
            v.Y *= ool;
            v.Z *= ool;
        }
        return length;
    }
    public static float Length(ref v3f v)
    {
        return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
    }
    public static float LengthXY(ref v3f v)
    {
        return (float)Math.Sqrt(v.x * v.x + v.y * v.y);
    }
    public static float Normalize(ref v3f v)
    {
        float length = (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        if (length != 0)
        {
            float ool = 1 / length;
            v.x *= ool;
            v.y *= ool;
            v.z *= ool;
        }
        return length;
    }
    public static void VectorMA(ref v3f a, float scale, ref v3f b, out v3f c)
    {
        c.x = a.x + b.x * scale;
        c.y = a.y + b.y * scale;
        c.z = a.z + b.z * scale;
    }
    public static void VectorScale(ref v3f a, float scale, out v3f b)
    {
        b.x = a.x * scale;
        b.y = a.y * scale;
        b.z = a.z * scale;
    }
    public static void VectorAdd(ref v3f a, ref v3f b, out v3f c)
    {
        c.x = a.x + b.x;
        c.y = a.y + b.y;
        c.z = a.z + b.z;
    }
    public static void VectorSubtract(ref v3f a, ref v3f b, out v3f c)
    {
        c.x = a.x - b.x;
        c.y = a.y - b.y;
        c.z = a.z - b.z;
    }
    public static void Clamp(ref v3f src, ref Vector3 min, ref Vector3 max, out v3f dest)
    {
        dest.x = Math.Max(Math.Min(src.x, max.X), min.X);
        dest.y = Math.Max(Math.Min(src.y, max.Y), min.Y);
        dest.z = Math.Max(Math.Min(src.z, max.Z), min.Z);
    }
    public static float DotProduct(ref v3f a, ref v3f b)
    {
        return a.x * b.x + a.y * b.y + a.z * b.z;
    }
    public static bool CheckNaN(ref v3f v, float defValue)
    {
        bool flag = false;
        if (float.IsNaN(v.x))
        {
            flag = true;
            v.x = defValue;
        }
        if (float.IsNaN(v.y))
        {
            flag = true;
            v.y = defValue;
        }
        if (float.IsNaN(v.z))
        {
            flag = true;
            v.z = defValue;
        }
        return flag;
    }
    public static float Comp(ref v3f a, int index)
    {
        if (index < 0 || index > 2)
            throw new ArgumentOutOfRangeException("index");
        return (index == 0 ? a.x : (index == 1 ? a.y : a.z));
    }
    public static float Comp(ref Vector3 a, int index)
    {
        if (index < 0 || index > 2)
            throw new ArgumentOutOfRangeException("index");
        return (index == 0 ? a.X : (index == 1 ? a.Y : a.Z));
    }
    public static float anglemod(double a)
    {
	    return (float)((360.0/65536) * ((int)(a*(65536/360.0)) & 65535));
    }
    public static float DotProduct(ref Vector3 a, ref Vector4 b)
    {
        return (a.X * b.X + a.Y * b.Y + a.Z * b.Z);
    }
    public static int BoxOnPlaneSide(ref v3f emins, ref v3f emaxs, mplane_t p)
    {
        float mindist, maxdist;
        switch (p.type)
        {
            case 0:
                mindist = emins.x;
                maxdist = emaxs.x;
                break;

            case 1:
                mindist = emins.y;
                maxdist = emaxs.y;
                break;

            case 2:
                mindist = emins.z;
                maxdist = emaxs.z;
                break;

            default:
                Vector3 mins, maxs;
                Copy(ref emins, out mins);
                Copy(ref emaxs, out maxs);
                return _BoxOnPlaneSide(ref mins, ref maxs, p);
        }
        return (p.dist <= mindist ? 1 : (p.dist >= maxdist ? 2 : 3));
    }
    public static int BoxOnPlaneSide(ref Vector3 emins, ref Vector3 emaxs, mplane_t p)
    {
        float mindist, maxdist;
        switch (p.type)
        {
            case 0:
                mindist = emins.X;
                maxdist = emaxs.X;
                break;

            case 1:
                mindist = emins.Y;
                maxdist = emaxs.Y;
                break;

            case 2:
                mindist = emins.Z;
                maxdist = emaxs.Z;
                break;

            default:
                return _BoxOnPlaneSide(ref emins, ref emaxs, p);
        }
        return (p.dist <= mindist ? 1 : (p.dist >= maxdist ? 2 : 3));
    }
    public static int _BoxOnPlaneSide(ref Vector3 emins, ref Vector3 emaxs, mplane_t p)
    {
        // general case
        float dist1, dist2;
            
        switch (p.signbits)
        {
            case 0:
                dist1 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                dist2 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                break;
                
            case 1:
                dist1 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                dist2 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                break;
                
            case 2:
                dist1 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                dist2 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                break;
                
            case 3:
                dist1 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                dist2 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                break;
                
            case 4:
                dist1 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                dist2 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                break;
                
            case 5:
                dist1 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emins.Z;
                dist2 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emaxs.Z;
                break;
                
            case 6:
                dist1 = p.normal.X * emaxs.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                dist2 = p.normal.X * emins.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                break;
                
            case 7:
                dist1 = p.normal.X * emins.X + p.normal.Y * emins.Y + p.normal.Z * emins.Z;
                dist2 = p.normal.X * emaxs.X + p.normal.Y * emaxs.Y + p.normal.Z * emaxs.Z;
                break;
                
            default:
                dist1 = dist2 = 0;		// shut up compiler
                game_engine.Sys_Error("BoxOnPlaneSide:  Bad signbits");
                break;
        }

        int sides = 0;
        if (dist1 >= p.dist)
            sides = 1;
        if (dist2 < p.dist)
            sides |= 2;

#if PARANOID
        if (sides == 0)
            Sys.Error("BoxOnPlaneSide: sides==0");
#endif

        return sides;
    }
    public static void SetComp(ref Vector3 dest, int index, float value)
    {
        if (index == 0)
            dest.X = value;
        else if (index == 1)
            dest.Y = value;
        else if (index == 2)
            dest.Z = value;
        else
            throw new ArgumentException("Index must be in range 0-2!");
    }
    public static void CorrectAngles180(ref Vector3 a)
    {
        if (a.X > 180)
            a.X -= 360;
        else if (a.X < -180)
            a.X += 360;
        if (a.Y > 180)
            a.Y -= 360;
        else if (a.Y < -180)
            a.Y += 360;
        if (a.Z > 180)
            a.Z -= 360;
        else if (a.Z < -180)
            a.Z += 360;
    }
    public static void RotatePointAroundVector(out Vector3 dst, ref Vector3 dir, ref Vector3 point, float degrees)
    {
        Matrix4 m = Matrix4.CreateFromAxisAngle(dir, (float)(degrees * Math.PI / 180.0));
        Vector3.Transform(ref point, ref m, out dst);
    }
    public static void Copy(ref v3f src, out Vector3 dest)
    {
        dest.X = src.x;
        dest.Y = src.y;
        dest.Z = src.z;
    }
    public static void Copy(ref Vector3 src, out v3f dest)
    {
        dest.x = src.X;
        dest.y = src.Y;
        dest.z = src.Z;
    }
}