using System;
using System.Collections.Generic;
using System.Text;

public static partial class game_engine
{
    public static int sb_updates;
    public static bool sb_showscores;
    public static glpic_t[,] sb_nums = new glpic_t[2, 11];
    public static glpic_t sb_colon;
    public static glpic_t sb_slash;
    public static glpic_t sb_ibar;
    public static glpic_t sb_sbar;
    public static glpic_t sb_scorebar;
    public static glpic_t[,] sb_weapons = new glpic_t[7, 8];
    public static glpic_t[] sb_ammo = new glpic_t[4];
    public static glpic_t[] sb_sigil = new glpic_t[4];
    public static glpic_t[] sb_armor = new glpic_t[3];
    public static glpic_t[] sb_items = new glpic_t[32];
    public static glpic_t[,] sb_faces = new glpic_t[7, 2];
    public static glpic_t sb_face_invis;
    public static glpic_t sb_face_quad;
    public static glpic_t sb_face_invuln;
    public static glpic_t sb_face_invis_invuln;
    public static glpic_t[] rsb_invbar = new glpic_t[2];
    public static glpic_t[] rsb_weapons = new glpic_t[5];
    public static glpic_t[] rsb_items = new glpic_t[2];
    public static glpic_t[] rsb_ammo = new glpic_t[3];
    public static glpic_t rsb_teambord;
    public static glpic_t[,] hsb_weapons = new glpic_t[7, 5];
    public static int[] hipweapons = new int[]
    {
        q_shared.HIT_LASER_CANNON_BIT, q_shared.HIT_MJOLNIR_BIT, 4, q_shared.HIT_PROXIMITY_GUN_BIT
    };
    public static glpic_t[] hsb_items = new glpic_t[2];
    public static int[] fragsort = new int[q_shared.MAX_SCOREBOARD];
    public static string[] scoreboardtext = new string[q_shared.MAX_SCOREBOARD];
    public static int[] scoreboardtop = new int[q_shared.MAX_SCOREBOARD];
    public static int[] scoreboardbottom = new int[q_shared.MAX_SCOREBOARD];
    public static int[] scoreboardcount = new int[q_shared.MAX_SCOREBOARD];
    public static int scoreboardlines;
    public static int sb_lines;


    public static void Sbar_Init()
    {
        for (int i = 0; i < 10; i++)
        {
            string str = i.ToString();
            sb_nums[0, i] = Draw_PicFromWad("num_" + str);
            sb_nums[1, i] = Draw_PicFromWad("anum_" + str);
        }

        sb_nums[0, 10] = Draw_PicFromWad("num_minus");
        sb_nums[1, 10] = Draw_PicFromWad("anum_minus");

        sb_colon = Draw_PicFromWad("num_colon");
        sb_slash = Draw_PicFromWad("num_slash");

        sb_weapons[0, 0] = Draw_PicFromWad("inv_shotgun");
        sb_weapons[0, 1] = Draw_PicFromWad("inv_sshotgun");
        sb_weapons[0, 2] = Draw_PicFromWad("inv_nailgun");
        sb_weapons[0, 3] = Draw_PicFromWad("inv_snailgun");
        sb_weapons[0, 4] = Draw_PicFromWad("inv_rlaunch");
        sb_weapons[0, 5] = Draw_PicFromWad("inv_srlaunch");
        sb_weapons[0, 6] = Draw_PicFromWad("inv_lightng");

        sb_weapons[1, 0] = Draw_PicFromWad("inv2_shotgun");
        sb_weapons[1, 1] = Draw_PicFromWad("inv2_sshotgun");
        sb_weapons[1, 2] = Draw_PicFromWad("inv2_nailgun");
        sb_weapons[1, 3] = Draw_PicFromWad("inv2_snailgun");
        sb_weapons[1, 4] = Draw_PicFromWad("inv2_rlaunch");
        sb_weapons[1, 5] = Draw_PicFromWad("inv2_srlaunch");
        sb_weapons[1, 6] = Draw_PicFromWad("inv2_lightng");

        for (int i = 0; i < 5; i++)
        {
            string s = "inva" + (i + 1).ToString();
            sb_weapons[2 + i, 0] = Draw_PicFromWad(s + "_shotgun");
            sb_weapons[2 + i, 1] = Draw_PicFromWad(s + "_sshotgun");
            sb_weapons[2 + i, 2] = Draw_PicFromWad(s + "_nailgun");
            sb_weapons[2 + i, 3] = Draw_PicFromWad(s + "_snailgun");
            sb_weapons[2 + i, 4] = Draw_PicFromWad(s + "_rlaunch");
            sb_weapons[2 + i, 5] = Draw_PicFromWad(s + "_srlaunch");
            sb_weapons[2 + i, 6] = Draw_PicFromWad(s + "_lightng");
        }

        sb_ammo[0] = Draw_PicFromWad("sb_shells");
        sb_ammo[1] = Draw_PicFromWad("sb_nails");
        sb_ammo[2] = Draw_PicFromWad("sb_rocket");
        sb_ammo[3] = Draw_PicFromWad("sb_cells");

        sb_armor[0] = Draw_PicFromWad("sb_armor1");
        sb_armor[1] = Draw_PicFromWad("sb_armor2");
        sb_armor[2] = Draw_PicFromWad("sb_armor3");

        sb_items[0] = Draw_PicFromWad("sb_key1");
        sb_items[1] = Draw_PicFromWad("sb_key2");
        sb_items[2] = Draw_PicFromWad("sb_invis");
        sb_items[3] = Draw_PicFromWad("sb_invuln");
        sb_items[4] = Draw_PicFromWad("sb_suit");
        sb_items[5] = Draw_PicFromWad("sb_quad");

        sb_sigil[0] = Draw_PicFromWad("sb_sigil1");
        sb_sigil[1] = Draw_PicFromWad("sb_sigil2");
        sb_sigil[2] = Draw_PicFromWad("sb_sigil3");
        sb_sigil[3] = Draw_PicFromWad("sb_sigil4");

        sb_faces[4, 0] = Draw_PicFromWad("face1");
        sb_faces[4, 1] = Draw_PicFromWad("face_p1");
        sb_faces[3, 0] = Draw_PicFromWad("face2");
        sb_faces[3, 1] = Draw_PicFromWad("face_p2");
        sb_faces[2, 0] = Draw_PicFromWad("face3");
        sb_faces[2, 1] = Draw_PicFromWad("face_p3");
        sb_faces[1, 0] = Draw_PicFromWad("face4");
        sb_faces[1, 1] = Draw_PicFromWad("face_p4");
        sb_faces[0, 0] = Draw_PicFromWad("face5");
        sb_faces[0, 1] = Draw_PicFromWad("face_p5");

        sb_face_invis = Draw_PicFromWad("face_invis");
        sb_face_invuln = Draw_PicFromWad("face_invul2");
        sb_face_invis_invuln = Draw_PicFromWad("face_inv2");
        sb_face_quad = Draw_PicFromWad("face_quad");

        Cmd_AddCommand("+showscores", Sbar_ShowScores);
        Cmd_AddCommand("-showscores", Sbar_DontShowScores);

        sb_sbar = Draw_PicFromWad("sbar");
        sb_ibar = Draw_PicFromWad("ibar");
        sb_scorebar = Draw_PicFromWad("scorebar");

        //MED 01/04/97 added new hipnotic weapons
        if (_GameKind == GameKind.Hipnotic)
        {
            hsb_weapons[0, 0] = Draw_PicFromWad("inv_laser");
            hsb_weapons[0, 1] = Draw_PicFromWad("inv_mjolnir");
            hsb_weapons[0, 2] = Draw_PicFromWad("inv_gren_prox");
            hsb_weapons[0, 3] = Draw_PicFromWad("inv_prox_gren");
            hsb_weapons[0, 4] = Draw_PicFromWad("inv_prox");

            hsb_weapons[1, 0] = Draw_PicFromWad("inv2_laser");
            hsb_weapons[1, 1] = Draw_PicFromWad("inv2_mjolnir");
            hsb_weapons[1, 2] = Draw_PicFromWad("inv2_gren_prox");
            hsb_weapons[1, 3] = Draw_PicFromWad("inv2_prox_gren");
            hsb_weapons[1, 4] = Draw_PicFromWad("inv2_prox");

            for (int i = 0; i < 5; i++)
            {
                string s = "inva" + (i + 1).ToString();
                hsb_weapons[2 + i, 0] = Draw_PicFromWad(s + "_laser");
                hsb_weapons[2 + i, 1] = Draw_PicFromWad(s + "_mjolnir");
                hsb_weapons[2 + i, 2] = Draw_PicFromWad(s + "_gren_prox");
                hsb_weapons[2 + i, 3] = Draw_PicFromWad(s + "_prox_gren");
                hsb_weapons[2 + i, 4] = Draw_PicFromWad(s + "_prox");
            }

            hsb_items[0] = Draw_PicFromWad("sb_wsuit");
            hsb_items[1] = Draw_PicFromWad("sb_eshld");
        }

        if (_GameKind == GameKind.Rogue)
        {
            rsb_invbar[0] = Draw_PicFromWad("r_invbar1");
            rsb_invbar[1] = Draw_PicFromWad("r_invbar2");

            rsb_weapons[0] = Draw_PicFromWad("r_lava");
            rsb_weapons[1] = Draw_PicFromWad("r_superlava");
            rsb_weapons[2] = Draw_PicFromWad("r_gren");
            rsb_weapons[3] = Draw_PicFromWad("r_multirock");
            rsb_weapons[4] = Draw_PicFromWad("r_plasma");

            rsb_items[0] = Draw_PicFromWad("r_shield1");
            rsb_items[1] = Draw_PicFromWad("r_agrav1");

            // PGM 01/19/97 - team color border
            rsb_teambord = Draw_PicFromWad("r_teambord");
            // PGM 01/19/97 - team color border

            rsb_ammo[0] = Draw_PicFromWad("r_ammolava");
            rsb_ammo[1] = Draw_PicFromWad("r_ammomulti");
            rsb_ammo[2] = Draw_PicFromWad("r_ammoplasma");
        }
    }
    public static void Sbar_Changed()
    {
        sb_updates = 0;	// update next frame
    }
    public static void Sbar_Draw()
    {
        if (scr_con_current == vid.height)
            return;		// console is full screen

        if (sb_updates >= vid.numpages)
            return;

        scr_copyeverything = true;

        sb_updates++;

        if (sb_lines > 0 && vid.width > 320)
            Draw_TileClear(0, vid.height - sb_lines, vid.width, sb_lines);

        if (sb_lines > 24)
        {
            Sbar_DrawInventory();
            if (cl.maxclients != 1)
                Sbar_DrawFrags();
        }
        
        if (sb_showscores || cl.stats[q_shared.STAT_HEALTH] <= 0)
        {
            Sbar_DrawPic(0, 0, sb_scorebar);
            Sbar_DrawScoreboard();
            sb_updates = 0;
        }
        else if (sb_lines > 0)
        {
            Sbar_DrawPic(0, 0, sb_sbar);

            // keys (hipnotic only)
            //MED 01/04/97 moved keys here so they would not be overwritten
            if (_GameKind == GameKind.Hipnotic)
            {
                if (cl.HasItems(q_shared.IT_KEY1))
                    Sbar_DrawPic(209, 3, sb_items[0]);
                if (cl.HasItems(q_shared.IT_KEY2))
                    Sbar_DrawPic(209, 12, sb_items[1]);
            }
            // armor
            if (cl.HasItems(q_shared.IT_INVULNERABILITY))
            {
                Sbar_DrawNum(24, 0, 666, 3, 1);
                Sbar_DrawPic(0, 0, draw_disc);
            }
            else
            {
                if (_GameKind == GameKind.Rogue)
                {
                    Sbar_DrawNum(24, 0, cl.stats[q_shared.STAT_ARMOR], 3, cl.stats[q_shared.STAT_ARMOR] <= 25 ? 1 : 0); // uze: corrected color param
                    if (cl.HasItems(q_shared.RIT_ARMOR3))
                        Sbar_DrawPic(0, 0, sb_armor[2]);
                    else if (cl.HasItems(q_shared.RIT_ARMOR2))
                        Sbar_DrawPic(0, 0, sb_armor[1]);
                    else if (cl.HasItems(q_shared.RIT_ARMOR1))
                        Sbar_DrawPic(0, 0, sb_armor[0]);
                }
                else
                {
                    Sbar_DrawNum(24, 0, cl.stats[q_shared.STAT_ARMOR], 3, cl.stats[q_shared.STAT_ARMOR] <= 25 ? 1 : 0);
                    if (cl.HasItems(q_shared.IT_ARMOR3))
                        Sbar_DrawPic(0, 0, sb_armor[2]);
                    else if (cl.HasItems(q_shared.IT_ARMOR2))
                        Sbar_DrawPic(0, 0, sb_armor[1]);
                    else if (cl.HasItems(q_shared.IT_ARMOR1))
                        Sbar_DrawPic(0, 0, sb_armor[0]);
                }
            }

            // face
            Sbar_DrawFace();

            // health
            Sbar_DrawNum(136, 0, cl.stats[q_shared.STAT_HEALTH], 3, cl.stats[q_shared.STAT_HEALTH] <= 25 ? 1 : 0);

            // ammo icon
            if (_GameKind == GameKind.Rogue)
            {
                if (cl.HasItems(q_shared.RIT_SHELLS))
                    Sbar_DrawPic(224, 0, sb_ammo[0]);
                else if (cl.HasItems(q_shared.RIT_NAILS))
                    Sbar_DrawPic(224, 0, sb_ammo[1]);
                else if (cl.HasItems(q_shared.RIT_ROCKETS))
                    Sbar_DrawPic(224, 0, sb_ammo[2]);
                else if (cl.HasItems(q_shared.RIT_CELLS))
                    Sbar_DrawPic(224, 0, sb_ammo[3]);
                else if (cl.HasItems(q_shared.RIT_LAVA_NAILS))
                    Sbar_DrawPic(224, 0, rsb_ammo[0]);
                else if (cl.HasItems(q_shared.RIT_PLASMA_AMMO))
                    Sbar_DrawPic(224, 0, rsb_ammo[1]);
                else if (cl.HasItems(q_shared.RIT_MULTI_ROCKETS))
                    Sbar_DrawPic(224, 0, rsb_ammo[2]);
            }
            else
            {
                if (cl.HasItems(q_shared.IT_SHELLS))
                    Sbar_DrawPic(224, 0, sb_ammo[0]);
                else if (cl.HasItems(q_shared.IT_NAILS))
                    Sbar_DrawPic(224, 0, sb_ammo[1]);
                else if (cl.HasItems(q_shared.IT_ROCKETS))
                    Sbar_DrawPic(224, 0, sb_ammo[2]);
                else if (cl.HasItems(q_shared.IT_CELLS))
                    Sbar_DrawPic(224, 0, sb_ammo[3]);
            }

            Sbar_DrawNum(248, 0, cl.stats[q_shared.STAT_AMMO], 3, cl.stats[q_shared.STAT_AMMO] <= 10 ? 1 : 0);
        }

        if (vid.width > 320)
        {
            if (cl.gametype == q_shared.GAME_DEATHMATCH)
                Sbar_DeathmatchOverlay();
        }
    }
    public static void Sbar_IntermissionOverlay()
    {
        scr_copyeverything = true;
        scr_fullupdate = 0;

        if (cl.gametype == q_shared.GAME_DEATHMATCH)
        {
            Sbar_MiniDeathmatchOverlay();
            return;
        }

        glpic_t pic = Draw_CachePic("gfx/complete.lmp");
        Draw_Pic(64, 24, pic);

        pic = Draw_CachePic("gfx/inter.lmp");
        Draw_TransPic(0, 56, pic);

        // time
        int dig = cl.completed_time / 60;
        Sbar_IntermissionNumber(160, 64, dig, 3, 0);
        int num = cl.completed_time - dig * 60;
        Draw_TransPic(234, 64, sb_colon);
        Draw_TransPic(246, 64, sb_nums[0, num / 10]);
        Draw_TransPic(266, 64, sb_nums[0, num % 10]);

        Sbar_IntermissionNumber(160, 104, cl.stats[q_shared.STAT_SECRETS], 3, 0);
        Draw_TransPic(232, 104, sb_slash);
        Sbar_IntermissionNumber(240, 104, cl.stats[q_shared.STAT_TOTALSECRETS], 3, 0);

        Sbar_IntermissionNumber(160, 144, cl.stats[q_shared.STAT_MONSTERS], 3, 0);
        Draw_TransPic(232, 144, sb_slash);
        Sbar_IntermissionNumber(240, 144, cl.stats[q_shared.STAT_TOTALMONSTERS], 3, 0);
    }
    public static void Sbar_IntermissionNumber(int x, int y, int num, int digits, int color)
    {
        string str = num.ToString();
        if (str.Length > digits)
        {
            str = str.Remove(0, str.Length - digits);
        }

        if (str.Length < digits)
            x += (digits - str.Length) * 24;

        for (int i = 0; i < str.Length; i++)
        {
            int frame = (str[i] == '-' ? q_shared.STAT_MINUS : str[i] - '0');
            Draw_TransPic(x, y, sb_nums[color, frame]);
            x += 24;
        }
    }
    public static void Sbar_FinaleOverlay()
    {
        scr_copyeverything = true;

        glpic_t pic = Draw_CachePic("gfx/finale.lmp");
        Draw_TransPic((vid.width - pic.width) / 2, 16, pic);
    }
    public static void Sbar_DrawInventory()
    {
        int flashon;
        
        if (_GameKind == GameKind.Rogue)
        {
            if (cl.stats[q_shared.STAT_ACTIVEWEAPON] >= q_shared.RIT_LAVA_NAILGUN)
                Sbar_DrawPic(0, -24, rsb_invbar[0]);
            else
                Sbar_DrawPic(0, -24, rsb_invbar[1]);
        }
        else
            Sbar_DrawPic(0, -24, sb_ibar);

        // weapons
        for (int i = 0; i < 7; i++)
        {
            if (cl.HasItems(q_shared.IT_SHOTGUN << i))
            {
                float time = cl.item_gettime[i];
                flashon = (int)((cl.time - time) * 10);
                if (flashon >= 10)
                {
                    if (cl.stats[q_shared.STAT_ACTIVEWEAPON] == (q_shared.IT_SHOTGUN << i))
                        flashon = 1;
                    else
                        flashon = 0;
                }
                else
                    flashon = (flashon % 5) + 2;

                Sbar_DrawPic(i * 24, -16, sb_weapons[flashon, i]);

                if (flashon > 1)
                    sb_updates = 0; // force update to remove flash
            }
        }

        // MED 01/04/97
        // hipnotic weapons
        if (_GameKind == GameKind.Hipnotic)
        {

            int grenadeflashing = 0;
            for (int i = 0; i < 4; i++)
            {
                if (cl.HasItems(1 << hipweapons[i]))
                {
                    float time = cl.item_gettime[hipweapons[i]];
                    flashon = (int)((cl.time - time) * 10);
                    if (flashon >= 10)
                    {
                        if (cl.stats[q_shared.STAT_ACTIVEWEAPON] == (1 << hipweapons[i]))
                            flashon = 1;
                        else
                            flashon = 0;
                    }
                    else
                        flashon = (flashon % 5) + 2;

                    // check grenade launcher
                    if (i == 2)
                    {
                        if (cl.HasItems(q_shared.HIT_PROXIMITY_GUN))
                        {
                            if (flashon > 0)
                            {
                                grenadeflashing = 1;
                                Sbar_DrawPic(96, -16, hsb_weapons[flashon, 2]);
                            }
                        }
                    }
                    else if (i == 3)
                    {
                        if (cl.HasItems(q_shared.IT_SHOTGUN << 4))
                        {
                            if (flashon > 0 && grenadeflashing == 0)
                            {
                                Sbar_DrawPic(96, -16, hsb_weapons[flashon, 3]);
                            }
                            else if (grenadeflashing == 0)
                            {
                                Sbar_DrawPic(96, -16, hsb_weapons[0, 3]);
                            }
                        }
                        else
                            Sbar_DrawPic(96, -16, hsb_weapons[flashon, 4]);
                    }
                    else
                        Sbar_DrawPic(176 + (i * 24), -16, hsb_weapons[flashon, i]);
                    if (flashon > 1)
                        sb_updates = 0; // force update to remove flash
                }
            }
        }

        if (_GameKind == GameKind.Rogue)
        {
            // check for powered up weapon.
            if (cl.stats[q_shared.STAT_ACTIVEWEAPON] >= q_shared.RIT_LAVA_NAILGUN)
                for (int i = 0; i < 5; i++)
                    if (cl.stats[q_shared.STAT_ACTIVEWEAPON] == (q_shared.RIT_LAVA_NAILGUN << i))
                        Sbar_DrawPic((i + 2) * 24, -16, rsb_weapons[i]);
        }

        // ammo counts
        for (int i = 0; i < 4; i++)
        {
            string num = cl.stats[q_shared.STAT_SHELLS + i].ToString().PadLeft(3);
            //sprintf(num, "%3i", cl.stats[QStats.STAT_SHELLS + i]);
            if (num[0] != ' ')
                Sbar_DrawCharacter((6 * i + 1) * 8 - 2, -24, 18 + num[0] - '0');
            if (num[1] != ' ')
                Sbar_DrawCharacter((6 * i + 2) * 8 - 2, -24, 18 + num[1] - '0');
            if (num[2] != ' ')
                Sbar_DrawCharacter((6 * i + 3) * 8 - 2, -24, 18 + num[2] - '0');
        }

        flashon = 0;
        // items
        for (int i = 0; i < 6; i++)
        {
            if (cl.HasItems(1 << (17 + i)))
            {
                float time = cl.item_gettime[17 + i];
                if (time > 0 && time > cl.time - 2 && flashon > 0)
                {  // flash frame
                    sb_updates = 0;
                }
                else
                {
                    //MED 01/04/97 changed keys
                    if (_GameKind != GameKind.Hipnotic || (i > 1))
                    {
                        Sbar_DrawPic(192 + i * 16, -16, sb_items[i]);
                    }
                }
                if (time > 0 && time > cl.time - 2)
                    sb_updates = 0;
            }
        }

        //MED 01/04/97 added hipnotic items
        // hipnotic items
        if (_GameKind == GameKind.Hipnotic)
        {
            for (int i = 0; i < 2; i++)
            {
                if (cl.HasItems(1 << (24 + i)))
                {
                    float time = cl.item_gettime[24 + i];
                    if (time > 0 && time > cl.time - 2 && flashon > 0)
                    {  // flash frame
                        sb_updates = 0;
                    }
                    else
                    {
                        Sbar_DrawPic(288 + i * 16, -16, hsb_items[i]);
                    }
                    if (time > 0 && time > cl.time - 2)
                        sb_updates = 0;
                }
            }
        }

        if (_GameKind == GameKind.Rogue)
        {
            // new rogue items
            for (int i = 0; i < 2; i++)
            {
                if (cl.HasItems(1 << (29 + i)))
                {
                    float time = cl.item_gettime[29 + i];

                    if (time > 0 && time > cl.time - 2 && flashon > 0)
                    {	// flash frame
                        sb_updates = 0;
                    }
                    else
                    {
                        Sbar_DrawPic(288 + i * 16, -16, rsb_items[i]);
                    }

                    if (time > 0 && time > cl.time - 2)
                        sb_updates = 0;
                }
            }
        }
        else
        {
            // sigils
            for (int i = 0; i < 4; i++)
            {
                if (cl.HasItems(1 << (28 + i)))
                {
                    float time = cl.item_gettime[28 + i];
                    if (time > 0 && time > cl.time - 2 && flashon > 0)
                    {	// flash frame
                        sb_updates = 0;
                    }
                    else
                        Sbar_DrawPic(320 - 32 + i * 8, -16, sb_sigil[i]);
                    if (time > 0 && time > cl.time - 2)
                        sb_updates = 0;
                }
            }
        }
    }
    public static void Sbar_DrawFrags()
    {
        Sbar_SortFrags();

        // draw the text
        int l = scoreboardlines <= 4 ? scoreboardlines : 4;
        int xofs, x = 23;

        if (cl.gametype == q_shared.GAME_DEATHMATCH)
            xofs = 0;
        else
            xofs = (vid.width - 320) >> 1;

        int y = vid.height - q_shared.SBAR_HEIGHT - 23;

        for (int i = 0; i < l; i++)
        {
            int k = fragsort[i];
            scoreboard_t s = cl.scores[k];
            if (String.IsNullOrEmpty(s.name))
                continue;

            // draw background
            int top = s.colors & 0xf0;
            int bottom = (s.colors & 15) << 4;
            top = Sbar_ColorForMap(top);
            bottom = Sbar_ColorForMap(bottom);

            Draw_Fill(xofs + x * 8 + 10, y, 28, 4, top);
            Draw_Fill(xofs + x * 8 + 10, y + 4, 28, 3, bottom);

            // draw number
            int f = s.frags;
            string num = f.ToString().PadLeft(3);
            //sprintf(num, "%3i", f);

            Sbar_DrawCharacter((x + 1) * 8, -24, num[0]);
            Sbar_DrawCharacter((x + 2) * 8, -24, num[1]);
            Sbar_DrawCharacter((x + 3) * 8, -24, num[2]);

            if (k == cl.viewentity - 1)
            {
                Sbar_DrawCharacter(x * 8 + 2, -24, 16);
                Sbar_DrawCharacter((x + 4) * 8 - 4, -24, 17);
            }
            x += 4;
        }
    }
    public static void Sbar_DrawPic(int x, int y, glpic_t pic)
    {
        if (cl.gametype == q_shared.GAME_DEATHMATCH)
            Draw_Pic(x, y + (vid.height - q_shared.SBAR_HEIGHT), pic);
        else
            Draw_Pic(x + ((vid.width - 320) >> 1), y + (vid.height - q_shared.SBAR_HEIGHT), pic);
    }
    public static void Sbar_DrawScoreboard()
    {
        Sbar_SoloScoreboard();
        if (cl.gametype == q_shared.GAME_DEATHMATCH)
            Sbar_MiniDeathmatchOverlay();
    }
    public static void Sbar_DrawNum(int x, int y, int num, int digits, int color)
    {
        string str = num.ToString();// int l = Sbar_itoa(num, str);

        if (str.Length > digits)
            str = str.Remove(str.Length - digits);
        if (str.Length < digits)
            x += (digits - str.Length) * 24;

        for (int i = 0, frame; i < str.Length; i++)
        {
            if (str[i] == '-')
                frame = q_shared.STAT_MINUS;
            else
                frame = str[i] - '0';

            Sbar_DrawTransPic(x, y, sb_nums[color, frame]);
            x += 24;
        }
    }
    public static void Sbar_DrawFace()
    {
        // PGM 01/19/97 - team color drawing
        // PGM 03/02/97 - fixed so color swatch only appears in CTF modes
        if (_GameKind == GameKind.Rogue &&
            (cl.maxclients != 1) &&
            (teamplay.value > 3) &&
            (teamplay.value < 7))
        {
            scoreboard_t s = cl.scores[cl.viewentity - 1];

            // draw background
            int top = s.colors & 0xf0;
            int bottom = (s.colors & 15) << 4;
            top = Sbar_ColorForMap(top);
            bottom = Sbar_ColorForMap(bottom);

            int xofs;
            if (cl.gametype == q_shared.GAME_DEATHMATCH)
                xofs = 113;
            else
                xofs = ((vid.width - 320) >> 1) + 113;

            Sbar_DrawPic(112, 0, rsb_teambord);
            Draw_Fill(xofs, vid.height - q_shared.SBAR_HEIGHT + 3, 22, 9, top);
            Draw_Fill(xofs, vid.height - q_shared.SBAR_HEIGHT + 12, 22, 9, bottom);

            // draw number
            string num = s.frags.ToString().PadLeft(3);
            if (top == 8)
            {
                if (num[0] != ' ')
                    Sbar_DrawCharacter(109, 3, 18 + num[0] - '0');
                if (num[1] != ' ')
                    Sbar_DrawCharacter(116, 3, 18 + num[1] - '0');
                if (num[2] != ' ')
                    Sbar_DrawCharacter(123, 3, 18 + num[2] - '0');
            }
            else
            {
                Sbar_DrawCharacter(109, 3, num[0]);
                Sbar_DrawCharacter(116, 3, num[1]);
                Sbar_DrawCharacter(123, 3, num[2]);
            }

            return;
        }
        // PGM 01/19/97 - team color drawing

        int f, anim;

        if (cl.HasItems(q_shared.IT_INVISIBILITY | q_shared.IT_INVULNERABILITY))
        {
            Sbar_DrawPic(112, 0, sb_face_invis_invuln);
            return;
        }
        if (cl.HasItems(q_shared.IT_QUAD))
        {
            Sbar_DrawPic(112, 0, sb_face_quad);
            return;
        }
        if (cl.HasItems(q_shared.IT_INVISIBILITY))
        {
            Sbar_DrawPic(112, 0, sb_face_invis);
            return;
        }
        if (cl.HasItems(q_shared.IT_INVULNERABILITY))
        {
            Sbar_DrawPic(112, 0, sb_face_invuln);
            return;
        }

        if (cl.stats[q_shared.STAT_HEALTH] >= 100)
            f = 4;
        else
            f = cl.stats[q_shared.STAT_HEALTH] / 20;

        if (cl.time <= cl.faceanimtime)
        {
            anim = 1;
            sb_updates = 0; // make sure the anim gets drawn over
        }
        else
            anim = 0;

        Sbar_DrawPic(112, 0, sb_faces[f, anim]);
    }
    public static void Sbar_DeathmatchOverlay()
    {
        if (vid.width < 512 || sb_lines == 0)
            return;

        scr_copyeverything = true;
        scr_fullupdate = 0;

        // scores
        Sbar_SortFrags();

        // draw the text
        int l = scoreboardlines;
        int y = vid.height - sb_lines;
        int numlines = sb_lines / 8;
        if (numlines < 3)
            return;

        //find us
        int i;
        for (i = 0; i < scoreboardlines; i++)
            if (fragsort[i] == cl.viewentity - 1)
                break;

        if (i == scoreboardlines) // we're not there
            i = 0;
        else // figure out start
            i = i - numlines / 2;

        if (i > scoreboardlines - numlines)
            i = scoreboardlines - numlines;
        if (i < 0)
            i = 0;

        int x = 324;
        for (; i < scoreboardlines && y < vid.height - 8; i++)
        {
            int k = fragsort[i];
            scoreboard_t s = cl.scores[k];
            if (String.IsNullOrEmpty(s.name))
                continue;

            // draw background
            int top = s.colors & 0xf0;
            int bottom = (s.colors & 15) << 4;
            top = Sbar_ColorForMap(top);
            bottom = Sbar_ColorForMap(bottom);

            Draw_Fill(x, y + 1, 40, 3, top);
            Draw_Fill(x, y + 4, 40, 4, bottom);

            // draw number
            string num = s.frags.ToString().PadLeft(3);
            Draw_Character(x + 8, y, num[0]);
            Draw_Character(x + 16, y, num[1]);
            Draw_Character(x + 24, y, num[2]);

            if (k == cl.viewentity - 1)
            {
                Draw_Character(x, y, 16);
                Draw_Character(x + 32, y, 17);
            }

            // draw name
            Draw_String(x + 48, y, s.name);

            y += 8;
        }
    }
    public static void Sbar_SortFrags()
    {
        // sort by frags
        scoreboardlines = 0;
        for (int i = 0; i < cl.maxclients; i++)
        {
            if (!String.IsNullOrEmpty(cl.scores[i].name))
            {
                fragsort[scoreboardlines] = i;
                scoreboardlines++;
            }
        }

        for (int i = 0; i < scoreboardlines; i++)
        {
            for (int j = 0; j < scoreboardlines - 1 - i; j++)
                if (cl.scores[fragsort[j]].frags < cl.scores[fragsort[j + 1]].frags)
                {
                    int k = fragsort[j];
                    fragsort[j] = fragsort[j + 1];
                    fragsort[j + 1] = k;
                }
        }
    }
    public static void Sbar_DrawCharacter(int x, int y, int num)
    {
        if (cl.gametype == q_shared.GAME_DEATHMATCH)
            Draw_Character(x + 4, y + vid.height - q_shared.SBAR_HEIGHT, num);
        else
            Draw_Character(x + ((vid.width - 320) >> 1) + 4, y + vid.height - q_shared.SBAR_HEIGHT, num);
    }
    public static int Sbar_ColorForMap(int m)
    {
        return m < 128 ? m + 8 : m + 8;
    }
    public static void Sbar_SoloScoreboard()
    {
        StringBuilder sb = new StringBuilder(80);

        sb.AppendFormat("Monsters:{0,3:d} /{1,3:d}", cl.stats[q_shared.STAT_MONSTERS], cl.stats[q_shared.STAT_TOTALMONSTERS]);
        Sbar_DrawString(8, 4, sb.ToString());

        sb.Length = 0;
        sb.AppendFormat("Secrets :{0,3:d} /{1,3:d}", cl.stats[q_shared.STAT_SECRETS], cl.stats[q_shared.STAT_TOTALSECRETS]);
        Sbar_DrawString(8, 12, sb.ToString());

        // time
        int minutes = (int)(cl.time / 60.0);
        int seconds = (int)(cl.time - 60 * minutes);
        int tens = seconds / 10;
        int units = seconds - 10 * tens;
        sb.Length = 0;
        sb.AppendFormat("Time :{0,3}:{1}{2}", minutes, tens, units);
        Sbar_DrawString(184, 4, sb.ToString());

        // draw level name
        int l = cl.levelname.Length;
        Sbar_DrawString(232 - l * 4, 12, cl.levelname);
    }
    public static void Sbar_MiniDeathmatchOverlay()
    {
        scr_copyeverything = true;
        scr_fullupdate = 0;

        glpic_t pic = Draw_CachePic("gfx/ranking.lmp");
        Menu.DrawPic((320 - pic.width) / 2, 8, pic);

        // scores
        Sbar_SortFrags();

        // draw the text
        int l = scoreboardlines;

        int x = 80 + ((vid.width - 320) >> 1);
        int y = 40;
        for (int i = 0; i < l; i++)
        {
            int k = fragsort[i];
            scoreboard_t s = cl.scores[k];
            if (String.IsNullOrEmpty(s.name))
                continue;

            // draw background
            int top = s.colors & 0xf0;
            int bottom = (s.colors & 15) << 4;
            top = Sbar_ColorForMap(top);
            bottom = Sbar_ColorForMap(bottom);

            Draw_Fill(x, y, 40, 4, top);
            Draw_Fill(x, y + 4, 40, 4, bottom);

            // draw number
            string num = s.frags.ToString().PadLeft(3);

            Draw_Character(x + 8, y, num[0]);
            Draw_Character(x + 16, y, num[1]);
            Draw_Character(x + 24, y, num[2]);

            if (k == cl.viewentity - 1)
                Draw_Character(x - 8, y, 12);

            // draw name
            Draw_String(x + 64, y, s.name);

            y += 10;
        }
    }
    public static void Sbar_DrawTransPic(int x, int y, glpic_t pic)
    {
        if (cl.gametype == q_shared.GAME_DEATHMATCH)
            Draw_TransPic(x, y + (vid.height - q_shared.SBAR_HEIGHT), pic);
        else
            Draw_TransPic(x + ((vid.width - 320) >> 1), y + (vid.height - q_shared.SBAR_HEIGHT), pic);
    }
    public static void Sbar_DrawString(int x, int y, string str)
    {
        if (cl.gametype == q_shared.GAME_DEATHMATCH)
            Draw_String(x, y + vid.height - q_shared.SBAR_HEIGHT, str);
        else
            Draw_String(x + ((vid.width - 320) >> 1), y + vid.height - q_shared.SBAR_HEIGHT, str);
    }
    public static void Sbar_ShowScores()
    {
        if (sb_showscores)
            return;
        sb_showscores = true;
        sb_updates = 0;
    }
    public static void Sbar_DontShowScores()
    {
        sb_showscores = false;
        sb_updates = 0;
    }
}