using System;
using System.Collections.Generic;
using System.Text;
using OpenTK;

public static partial class game_engine
{
    public static entity_t r_addent;
    public static mnode_t r_pefragtopnode;
    public static Vector3 r_emins;
    public static Vector3 r_emaxs;
    public static object _LastObj;

    public static void R_AddEfrags(entity_t ent)
    {
        if (ent.model == null)
            return;

        r_addent = ent;
        _LastObj = ent; //  lastlink = &ent->efrag;
        r_pefragtopnode = null;

        model_t entmodel = ent.model;
        r_emins = ent.origin + entmodel.mins;
        r_emaxs = ent.origin + entmodel.maxs;

        R_SplitEntityOnNode(cl.worldmodel.nodes[0]);
        ent.topnode = r_pefragtopnode;
    }
    public static void R_SplitEntityOnNode(mnodebase_t node)
    {
        if (node.contents == q_shared.CONTENTS_SOLID)
            return;

        // add an efrag if the node is a leaf
        if (node.contents < 0)
        {
            if (r_pefragtopnode == null)
                r_pefragtopnode = node as mnode_t;

            mleaf_t leaf = (mleaf_t)(object)node;

            // grab an efrag off the free list
            efrag_t ef = cl.free_efrags;
            if (ef == null)
            {
                Con_Printf("Too many efrags!\n");
                return;	// no free fragments...
            }
            cl.free_efrags = cl.free_efrags.entnext;

            ef.entity = r_addent;

            // add the entity link
            // *lastlink = ef;
            if (_LastObj is entity_t)
            {
                ((entity_t)_LastObj).efrag = ef;
            }
            else
            {
                ((efrag_t)_LastObj).entnext = ef;
            }
            _LastObj = ef; // lastlink = &ef->entnext;
            ef.entnext = null;

            // set the leaf links
            ef.leaf = leaf;
            ef.leafnext = leaf.efrags;
            leaf.efrags = ef;

            return;
        }

        // NODE_MIXED
        mnode_t n = node as mnode_t;
        if (n == null)
            return;
            
        mplane_t splitplane = n.plane;
        int sides = Mathlib.BoxOnPlaneSide(ref r_emins, ref r_emaxs, splitplane);

        if (sides == 3)
        {
            // split on this plane
            // if this is the first splitter of this bmodel, remember it
            if (r_pefragtopnode == null)
                r_pefragtopnode = n;
        }

        // recurse down the contacted sides
        if ((sides & 1) != 0)
            R_SplitEntityOnNode(n.children[0]);

        if ((sides & 2) != 0)
            R_SplitEntityOnNode(n.children[1]);
    }
    public static void R_StoreEfrags(efrag_t ef)
    {
        while (ef != null)
        {
            entity_t pent = ef.entity;
            model_t clmodel = pent.model;

            switch (clmodel.type)
            {
                case modtype_t.mod_alias:
                case modtype_t.mod_brush:
                case modtype_t.mod_sprite:
                    if ((pent.visframe != r_framecount) && (cl_numvisedicts < q_shared.MAX_VISEDICTS))
                    {
                        cl_visedicts[cl_numvisedicts++] = pent;

                        // mark that we've recorded this entity for this frame
                        pent.visframe = r_framecount;
                    }

                    ef = ef.leafnext;
                    break;

                default:
                    Sys_Error("R_StoreEfrags: Bad entity type {0}\n", clmodel.type);
                    break;
            }
        }
    }
}