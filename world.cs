using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{

    public static areanode_t[] sv_areanodes = new areanode_t[q_shared.AREA_NODES];
    public static int sv_numareanodes;
    public static hull_t box_hull = new hull_t();
    public static dclipnode_t[] _BoxClipNodes = new dclipnode_t[6];
    public static mplane_t[] _BoxPlanes = new mplane_t[6];


    public static void SV_ClearWorld()
    {
        SV_InitBoxHull();

        foreach (areanode_t node in sv_areanodes)
            node.Clear();
        sv_numareanodes = 0;

        SV_CreateAreaNode(0, ref sv.worldmodel.mins, ref sv.worldmodel.maxs);
    }
    public static areanode_t SV_CreateAreaNode(int depth, ref Vector3 mins, ref Vector3 maxs)
    {
        areanode_t anode = sv_areanodes[sv_numareanodes];
        sv_numareanodes++;

        anode.trigger_edicts.Clear();
        anode.solid_edicts.Clear();

        if (depth == q_shared.AREA_DEPTH)
        {
            anode.axis = -1;
            anode.children[0] = anode.children[1] = null;
            return anode;
        }

        Vector3 size = maxs - mins;
        Vector3 mins1 = mins;
        Vector3 mins2 = mins;
        Vector3 maxs1 = maxs;
        Vector3 maxs2 = maxs;

        if (size.X > size.Y)
        {
            anode.axis = 0;
            anode.dist = 0.5f * (maxs.X + mins.X);
            maxs1.X = mins2.X = anode.dist;
        }
        else
        {
            anode.axis = 1;
            anode.dist = 0.5f * (maxs.Y + mins.Y);
            maxs1.Y = mins2.Y = anode.dist;
        }

        anode.children[0] = SV_CreateAreaNode(depth + 1, ref mins2, ref maxs2);
        anode.children[1] = SV_CreateAreaNode(depth + 1, ref mins1, ref maxs1);

        return anode;
    }
    public static void SV_UnlinkEdict(edict_t ent)
    {
        if (ent.area.Prev == null)
            return;

        ent.area.Remove();

    }
    public static void SV_LinkEdict(edict_t ent, bool touch_triggers)
    {
        if (ent.area.Prev != null)
            SV_UnlinkEdict(ent);	// unlink from old position

        if (ent == sv.edicts[0])
            return;		// don't add the world

        if (ent.free)
            return;

        // set the abs box
        Mathlib.VectorAdd(ref ent.v.origin, ref ent.v.mins, out ent.v.absmin);
        Mathlib.VectorAdd(ref ent.v.origin, ref ent.v.maxs, out ent.v.absmax);

        //
        // to make items easier to pick up and allow them to be grabbed off
        // of shelves, the abs sizes are expanded
        //
        if (((int)ent.v.flags & q_shared.FL_ITEM) != 0)
        {
            ent.v.absmin.x -= 15;
            ent.v.absmin.y -= 15;
            ent.v.absmax.x += 15;
            ent.v.absmax.y += 15;
        }
        else
        {	// because movement is clipped an epsilon away from an actual edge,
            // we must fully check even when bounding boxes don't quite touch
            ent.v.absmin.x -= 1;
            ent.v.absmin.y -= 1;
            ent.v.absmin.z -= 1;
            ent.v.absmax.x += 1;
            ent.v.absmax.y += 1;
            ent.v.absmax.z += 1;
        }

        // link to PVS leafs
        ent.num_leafs = 0;
        if (ent.v.modelindex != 0)
            SV_FindTouchedLeafs(ent, sv.worldmodel.nodes[0]);

        if (ent.v.solid == q_shared.SOLID_NOT)
            return;

        // find the first node that the ent's box crosses
        areanode_t node = sv_areanodes[0];
        while (true)
        {
            if (node.axis == -1)
                break;
            if (Mathlib.Comp(ref ent.v.absmin, node.axis) > node.dist)
                node = node.children[0];
            else if (Mathlib.Comp(ref ent.v.absmax, node.axis) < node.dist)
                node = node.children[1];
            else
                break;		// crosses the node
        }

        // link it in	

        if (ent.v.solid == q_shared.SOLID_TRIGGER)
            ent.area.InsertBefore(node.trigger_edicts);
        else
            ent.area.InsertBefore(node.solid_edicts);

        // if touch_triggers, touch all entities at this node and decend for more
        if (touch_triggers)
            SV_TouchLinks(ent, sv_areanodes[0]);
    }
    public static int SV_PointContents(ref Vector3 p)
    {
        int cont = SV_HullPointContents(sv.worldmodel.hulls[0], 0, ref p);
        if (cont <= q_shared.CONTENTS_CURRENT_0 && cont >= q_shared.CONTENTS_CURRENT_DOWN)
            cont = q_shared.CONTENTS_WATER;
        return cont;
    }
    public static edict_t SV_TestEntityPosition(edict_t ent)
    {
        trace_t trace = Move(ref ent.v.origin, ref ent.v.mins, ref ent.v.maxs, ref ent.v.origin, 0, ent);

        if (trace.startsolid)
            return sv.edicts[0];

        return null;
    }
    public static trace_t SV_Move(ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, int type, edict_t passedict)
    {
        moveclip_t clip = new moveclip_t();

        // clip to world
        clip.trace = SV_ClipMoveToEntity(sv.edicts[0], ref start, ref mins, ref maxs, ref end);

        clip.start = start;
        clip.end = end;
        clip.mins = mins;
        clip.maxs = maxs;
        clip.type = type;
        clip.passedict = passedict;

        if (type == q_shared.MOVE_MISSILE)
        {
            clip.mins2 = Vector3.One * -15;
            clip.maxs2 = Vector3.One * 15;
        }
        else
        {
            clip.mins2 = mins;
            clip.maxs2 = maxs;
        }

        // create the bounding box of the entire move
        SV_MoveBounds(ref start, ref clip.mins2, ref clip.maxs2, ref end, out clip.boxmins, out clip.boxmaxs);

        // clip to entities
        SV_ClipToLinks(sv_areanodes[0], clip);

        return clip.trace;
    }
    public static void SV_InitBoxHull()
    {
        box_hull.clipnodes = _BoxClipNodes;
        box_hull.planes = _BoxPlanes;
        box_hull.firstclipnode = 0;
        box_hull.lastclipnode = 5;

        for (int i = 0; i < 6; i++)
        {
            _BoxClipNodes[i].planenum = i;

            int side = i & 1;

            _BoxClipNodes[i].children[side] = q_shared.CONTENTS_EMPTY;
            if (i != 5)
                _BoxClipNodes[i].children[side ^ 1] = (short)(i + 1);
            else
                _BoxClipNodes[i].children[side ^ 1] = q_shared.CONTENTS_SOLID;

            _BoxPlanes[i].type = (byte)(i >> 1);
            switch (i >> 1)
            {
                case 0:
                    _BoxPlanes[i].normal.X = 1;
                    break;

                case 1:
                    _BoxPlanes[i].normal.Y = 1;
                    break;

                case 2:
                    _BoxPlanes[i].normal.Z = 1;
                    break;
            }
            //_BoxPlanes[i].normal[i>>1] = 1;
        }
    }
    public static hull_t SV_HullForBox(ref Vector3 mins, ref Vector3 maxs)
    {
        _BoxPlanes[0].dist = maxs.X;
        _BoxPlanes[1].dist = mins.X;
        _BoxPlanes[2].dist = maxs.Y;
        _BoxPlanes[3].dist = mins.Y;
        _BoxPlanes[4].dist = maxs.Z;
        _BoxPlanes[5].dist = mins.Z;

        return box_hull;
    }
    public static hull_t SV_HullForEntity(edict_t ent, ref Vector3 mins, ref Vector3 maxs, out Vector3 offset)
    {
        hull_t hull = null;

        // decide which clipping hull to use, based on the size
        if (ent.v.solid == q_shared.SOLID_BSP)
        {	// explicit hulls in the BSP model
            if (ent.v.movetype != q_shared.MOVETYPE_PUSH)
                Sys_Error("SOLID_BSP without MOVETYPE_PUSH");

            model_t model = sv.models[(int)ent.v.modelindex];

            if (model == null || model.type != modtype_t.mod_brush)
                Sys_Error("MOVETYPE_PUSH with a non bsp model");

            Vector3 size = maxs - mins;
            if (size.X < 3)
                hull = model.hulls[0];
            else if (size.X <= 32)
                hull = model.hulls[1];
            else
                hull = model.hulls[2];

            // calculate an offset value to center the origin
            offset = hull.clip_mins - mins;
            offset += ToVector(ref ent.v.origin);
        }
        else
        {
            // create a temp hull from bounding box sizes
            Vector3 hullmins = ToVector(ref ent.v.mins) - maxs;
            Vector3 hullmaxs = ToVector(ref ent.v.maxs) - mins;
            hull = SV_HullForBox(ref hullmins, ref hullmaxs);

            offset = ToVector(ref ent.v.origin);
        }

        return hull;
    }
    public static bool SV_RecursiveHullCheck(hull_t hull, int num, float p1f, float p2f, ref Vector3 p1, ref Vector3 p2, trace_t trace)
    {
        // check for empty
        if (num < 0)
        {
            if (num != q_shared.CONTENTS_SOLID)
            {
                trace.allsolid = false;
                if (num == q_shared.CONTENTS_EMPTY)
                    trace.inopen = true;
                else
                    trace.inwater = true;
            }
            else
                trace.startsolid = true;
            return true;		// empty
        }

        if (num < hull.firstclipnode || num > hull.lastclipnode)
            Sys_Error("SV_RecursiveHullCheck: bad node number");

        //
        // find the point distances
        //
        short[] node_children = hull.clipnodes[num].children;
        mplane_t plane = hull.planes[hull.clipnodes[num].planenum];
        float t1, t2;

        if (plane.type < 3)
        {
            t1 = Mathlib.Comp(ref p1, plane.type) - plane.dist;
            t2 = Mathlib.Comp(ref p2, plane.type) - plane.dist;
        }
        else
        {
            t1 = Vector3.Dot(plane.normal, p1) - plane.dist;
            t2 = Vector3.Dot(plane.normal, p2) - plane.dist;
        }

        if (t1 >= 0 && t2 >= 0)
            return SV_RecursiveHullCheck(hull, node_children[0], p1f, p2f, ref p1, ref p2, trace);
        if (t1 < 0 && t2 < 0)
            return SV_RecursiveHullCheck(hull, node_children[1], p1f, p2f, ref p1, ref p2, trace);

        // put the crosspoint DIST_EPSILON pixels on the near side
        float frac;
        if (t1 < 0)
            frac = (t1 + q_shared.DIST_EPSILON) / (t1 - t2);
        else
            frac = (t1 - q_shared.DIST_EPSILON) / (t1 - t2);
        if (frac < 0)
            frac = 0;
        if (frac > 1)
            frac = 1;

        float midf = p1f + (p2f - p1f) * frac;
        Vector3 mid = p1 + (p2 - p1) * frac;

        int side = (t1 < 0) ? 1 : 0;

        // move up to the node
        if (!SV_RecursiveHullCheck(hull, node_children[side], p1f, midf, ref p1, ref mid, trace))
            return false;

        if (SV_HullPointContents(hull, node_children[side ^ 1], ref mid) != q_shared.CONTENTS_SOLID)
            // go past the node
            return SV_RecursiveHullCheck(hull, node_children[side ^ 1], midf, p2f, ref mid, ref p2, trace);

        if (trace.allsolid)
            return false;		// never got out of the solid area

        //==================
        // the other side of the node is solid, this is the impact point
        //==================
        if (side == 0)
        {
            trace.plane.normal = plane.normal;
            trace.plane.dist = plane.dist;
        }
        else
        {
            trace.plane.normal = -plane.normal;
            trace.plane.dist = -plane.dist;
        }

        while (SV_HullPointContents(hull, hull.firstclipnode, ref mid) == q_shared.CONTENTS_SOLID)
        {
            // shouldn't really happen, but does occasionally
            frac -= 0.1f;
            if (frac < 0)
            {
                trace.fraction = midf;
                trace.endpos = mid;
                Con_DPrintf("backup past 0\n");
                return false;
            }
            midf = p1f + (p2f - p1f) * frac;
            mid = p1 + (p2 - p1) * frac;
        }

        trace.fraction = midf;
        trace.endpos = mid;

        return false;
    }
    public static void SV_FindTouchedLeafs(edict_t ent, mnodebase_t node)
    {
        if (node.contents == q_shared.CONTENTS_SOLID)
            return;

        // add an efrag if the node is a leaf

        if (node.contents < 0)
        {
            if (ent.num_leafs == q_shared.MAX_ENT_LEAFS)
                return;

            mleaf_t leaf = (mleaf_t)node;
            int leafnum = Array.IndexOf(sv.worldmodel.leafs, leaf) - 1;

            ent.leafnums[ent.num_leafs] = (short)leafnum;
            ent.num_leafs++;
            return;
        }

        // NODE_MIXED
        mnode_t n = (mnode_t)node;
        mplane_t splitplane = n.plane;
        int sides = Mathlib.BoxOnPlaneSide(ref ent.v.absmin, ref ent.v.absmax, splitplane);

        // recurse down the contacted sides
        if ((sides & 1) != 0)
            SV_FindTouchedLeafs(ent, n.children[0]);

        if ((sides & 2) != 0)
            SV_FindTouchedLeafs(ent, n.children[1]);
    }
    public static void SV_TouchLinks(edict_t ent, areanode_t node)
    {
        // touch linked edicts
        link_t next;
        for (link_t l = node.trigger_edicts.Next; l != node.trigger_edicts; l = next)
        {
            next = l.Next;
            edict_t touch = (edict_t)l.Owner;// EDICT_FROM_AREA(l);
            if (touch == ent)
                continue;
            if (touch.v.touch == 0 || touch.v.solid != q_shared.SOLID_TRIGGER)
                continue;
            if (ent.v.absmin.x > touch.v.absmax.x || ent.v.absmin.y > touch.v.absmax.y ||
                ent.v.absmin.z > touch.v.absmax.z || ent.v.absmax.x < touch.v.absmin.x ||
                ent.v.absmax.y < touch.v.absmin.y || ent.v.absmax.z < touch.v.absmin.z)
                continue;

            int old_self = pr_global_struct.self;
            int old_other = pr_global_struct.other;

            pr_global_struct.self = EDICT_TO_PROG(touch);
            pr_global_struct.other = EDICT_TO_PROG(ent);
            pr_global_struct.time = (float)sv.time;
            PR_ExecuteProgram(touch.v.touch);

            pr_global_struct.self = old_self;
            pr_global_struct.other = old_other;
        }

        // recurse down both sides
        if (node.axis == -1)
            return;

        if (Mathlib.Comp(ref ent.v.absmax, node.axis) > node.dist)
            SV_TouchLinks(ent, node.children[0]);
        if (Mathlib.Comp(ref ent.v.absmin, node.axis) < node.dist)
            SV_TouchLinks(ent, node.children[1]);
    }
    public static trace_t SV_ClipMoveToEntity(edict_t ent, ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end)
    {
        trace_t trace = new trace_t();
        // fill in a default trace
        trace.fraction = 1;
        trace.allsolid = true;
        trace.endpos = end;

        // get the clipping hull
        Vector3 offset;
        hull_t hull = SV_HullForEntity(ent, ref mins, ref maxs, out offset);

        Vector3 start_l = start - offset;
        Vector3 end_l = end - offset;

        // trace a line through the apropriate clipping hull
        SV_RecursiveHullCheck(hull, hull.firstclipnode, 0, 1, ref start_l, ref end_l, trace);

        // fix trace up by the offset
        if (trace.fraction != 1)
            trace.endpos += offset;

        // did we clip the move?
        if (trace.fraction < 1 || trace.startsolid)
            trace.ent = ent;

        return trace;
    }
    public static void SV_MoveBounds(ref Vector3 start, ref Vector3 mins, ref Vector3 maxs, ref Vector3 end, out Vector3 boxmins, out Vector3 boxmaxs)
    {
        boxmins = Vector3.ComponentMin(start, end) + mins - Vector3.One;
        boxmaxs = Vector3.ComponentMax(start, end) + maxs + Vector3.One;
    }
    public static void SV_ClipToLinks(areanode_t node, moveclip_t clip)
    {
        link_t next;
        trace_t trace;

        // touch linked edicts
        for (link_t l = node.solid_edicts.Next; l != node.solid_edicts; l = next)
        {
            next = l.Next;
            edict_t touch = (edict_t)l.Owner;// EDICT_FROM_AREA(l);
            if (touch.v.solid == q_shared.SOLID_NOT)
                continue;
            if (touch == clip.passedict)
                continue;
            if (touch.v.solid == q_shared.SOLID_TRIGGER)
                Sys_Error("Trigger in clipping list");

            if (clip.type == q_shared.MOVE_NOMONSTERS && touch.v.solid != q_shared.SOLID_BSP)
                continue;

            if (clip.boxmins.X > touch.v.absmax.x || clip.boxmins.Y > touch.v.absmax.y ||
                clip.boxmins.Z > touch.v.absmax.z || clip.boxmaxs.X < touch.v.absmin.x ||
                clip.boxmaxs.Y < touch.v.absmin.y || clip.boxmaxs.Z < touch.v.absmin.z)
                continue;

            if (clip.passedict != null && clip.passedict.v.size.x != 0 && touch.v.size.x == 0)
                continue;	// points never interact

            // might intersect, so do an exact clip
            if (clip.trace.allsolid)
                return;
            if (clip.passedict != null)
            {
                if (PROG_TO_EDICT(touch.v.owner) == clip.passedict)
                    continue;	// don't clip against own missiles
                if (PROG_TO_EDICT(clip.passedict.v.owner) == touch)
                    continue;	// don't clip against owner
            }

            if (((int)touch.v.flags & q_shared.FL_MONSTER) != 0)
                trace = SV_ClipMoveToEntity(touch, ref clip.start, ref clip.mins2, ref clip.maxs2, ref clip.end);
            else
                trace = SV_ClipMoveToEntity(touch, ref clip.start, ref clip.mins, ref clip.maxs, ref clip.end);

            if (trace.allsolid || trace.startsolid || trace.fraction < clip.trace.fraction)
            {
                trace.ent = touch;
                if (clip.trace.startsolid)
                {
                    clip.trace = trace;
                    clip.trace.startsolid = true;
                }
                else
                    clip.trace = trace;
            }
            else if (trace.startsolid)
                clip.trace.startsolid = true;
        }

        // recurse down both sides
        if (node.axis == -1)
            return;

        if (Mathlib.Comp(ref clip.boxmaxs, node.axis) > node.dist)
            SV_ClipToLinks(node.children[0], clip);
        if (Mathlib.Comp(ref clip.boxmins, node.axis) < node.dist)
            SV_ClipToLinks(node.children[1], clip);
    }
    public static int SV_HullPointContents(hull_t hull, int num, ref Vector3 p)
    {
        while (num >= 0)
        {
            if (num < hull.firstclipnode || num > hull.lastclipnode)
                Sys_Error("SV_HullPointContents: bad node number");

            short[] node_children = hull.clipnodes[num].children;
            mplane_t plane = hull.planes[hull.clipnodes[num].planenum];
            float d;
            if (plane.type < 3)
                d = Mathlib.Comp(ref p, plane.type) - plane.dist;
            else
                d = Vector3.Dot(plane.normal, p) - plane.dist;
            if (d < 0)
                num = node_children[1];
            else
                num = node_children[0];
        }

        return num;
    }
}