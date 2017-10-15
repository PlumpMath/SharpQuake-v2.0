using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

static class Menu
{
    const int SLIDER_RANGE = 10;

    public static bool EnterSound; //qboolean	m_entersound	// play after drawing a frame, so caching
							// won't disrupt the sound
    static bool _RecursiveDraw; // qboolean m_recursiveDraw
    static byte[] _IdentityTable = new byte[256]; // identityTable
    static byte[] _TranslationTable = new byte[256]; //translationTable
    public static bool ReturnOnError;
    public static string ReturnReason;
    public static MenuBase ReturnMenu;

    
    public static void M_Init()
    {
        game_engine.Cmd_AddCommand("togglemenu", M_ToggleMenu_f);
        game_engine.Cmd_AddCommand("menu_main", M_Menu_Main_f);
        game_engine.Cmd_AddCommand("menu_singleplayer", M_Menu_SinglePlayer_f);
        game_engine.Cmd_AddCommand("menu_load", M_Menu_Load_f);
        game_engine.Cmd_AddCommand("menu_save", M_Menu_Save_f);
        game_engine.Cmd_AddCommand("menu_multiplayer", M_Menu_MultiPlayer_f);
        game_engine.Cmd_AddCommand("menu_setup", M_Menu_Setup_f);
        game_engine.Cmd_AddCommand("menu_options", M_Menu_Options_f);
        game_engine.Cmd_AddCommand("menu_keys", M_Menu_Keys_f);
        game_engine.Cmd_AddCommand("menu_video", M_Menu_Video_f);
        game_engine.Cmd_AddCommand("help", M_Menu_Help_f);
        game_engine.Cmd_AddCommand("menu_quit", M_Menu_Quit_f);
    }
    public static void M_Keydown (int key)
    {
        if (MenuBase.CurrentMenu != null)
            MenuBase.CurrentMenu.KeyEvent(key);
    }
    public static void M_Draw()
    {
        if (MenuBase.CurrentMenu == null || game_engine.key_dest != keydest_t.key_menu)
            return;

        if (!_RecursiveDraw)
        {
            game_engine.scr_copyeverything = true;

            if (game_engine.scr_con_current > 0)
            {
                game_engine.Draw_ConsoleBackground(game_engine.vid.height);
                game_engine.S_ExtraUpdate();
            }
            else
                game_engine.Draw_FadeScreen();

            game_engine.scr_fullupdate = 0;
        }
        else
        {
            _RecursiveDraw = false;
        }

        if (MenuBase.CurrentMenu != null)
            MenuBase.CurrentMenu.Draw();

        if (EnterSound)
        {
            game_engine.S_LocalSound("misc/menu2.wav");
            EnterSound = false;
        }

        game_engine.S_ExtraUpdate();
    }
    public static void M_ToggleMenu_f()
    {
        EnterSound = true;

        if (game_engine.key_dest == keydest_t.key_menu)
        {
            if (MenuBase.CurrentMenu != MenuBase.MainMenu)
            {
                MenuBase.MainMenu.Show();
                return;
            }
            MenuBase.Hide();
            return;
        }
        if (game_engine.key_dest == keydest_t.key_console)
        {
            game_engine.Con_ToggleConsole_f();
        }
        else
        {
            MenuBase.MainMenu.Show();
        }
    }
    static void M_Menu_Main_f()
    {
        MenuBase.MainMenu.Show();
    }
    static void M_Menu_SinglePlayer_f()
    {
        MenuBase.SinglePlayerMenu.Show();
    }
    static void M_Menu_Load_f()
    {
        MenuBase.LoadMenu.Show();
    }
    static void M_Menu_Save_f()
    {
        MenuBase.SaveMenu.Show();
    }
    static void M_Menu_MultiPlayer_f()
    {
        MenuBase.MultiPlayerMenu.Show();
    }
    static void M_Menu_Setup_f()
    {
        MenuBase.SetupMenu.Show();
    }
    static void M_Menu_Options_f()
    {
        MenuBase.OptionsMenu.Show();
    }
    static void M_Menu_Keys_f()
    {
        MenuBase.KeysMenu.Show();
    }
    static void M_Menu_Video_f()
    {
        MenuBase.VideoMenu.Show();
    }
    static void M_Menu_Help_f()
    {
        MenuBase.HelpMenu.Show();
    }
    static void M_Menu_Quit_f()
    {
        MenuBase.QuitMenu.Show();
    }
    public static void DrawPic(int x, int y, glpic_t pic)
    {
        game_engine.Draw_Pic(x + ((game_engine.vid.width - 320) >> 1), y, pic);
    }
    public static void DrawTransPic(int x, int y, glpic_t pic)
    {
        game_engine.Draw_TransPic(x + ((game_engine.vid.width - 320) >> 1), y, pic);
    }
    public static void M_DrawTransPicTranslate(int x, int y, glpic_t pic)
    {
        game_engine.Draw_TransPicTranslate(x + ((game_engine.vid.width - 320) >> 1), y, pic, _TranslationTable);
    }
    public static void M_Print(int cx, int cy, string str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            M_DrawCharacter(cx, cy, str[i] + 128);
            cx += 8;
        }
    }
    public static void M_DrawCharacter(int cx, int line, int num)
    {
        game_engine.Draw_Character(cx + ((game_engine.vid.width - 320) >> 1), line, num);
    }
    public static void M_PrintWhite(int cx, int cy, string str)
    {
        for (int i = 0; i < str.Length; i++)
        {
            M_DrawCharacter(cx, cy, str[i]);
            cx += 8;
        }
    }
    public static void M_DrawTextBox(int x, int y, int width, int lines)
    {
        // draw left side
        int cx = x;
        int cy = y;
        glpic_t p = game_engine.Draw_CachePic("gfx/box_tl.lmp");
        DrawTransPic(cx, cy, p);
        p = game_engine.Draw_CachePic("gfx/box_ml.lmp");
        for (int n = 0; n < lines; n++)
        {
            cy += 8;
            DrawTransPic(cx, cy, p);
        }
        p = game_engine.Draw_CachePic("gfx/box_bl.lmp");
        DrawTransPic(cx, cy + 8, p);

        // draw middle
        cx += 8;
        while (width > 0)
        {
            cy = y;
            p = game_engine.Draw_CachePic("gfx/box_tm.lmp");
            DrawTransPic(cx, cy, p);
            p = game_engine.Draw_CachePic("gfx/box_mm.lmp");
            for (int n = 0; n < lines; n++)
            {
                cy += 8;
                if (n == 1)
                    p = game_engine.Draw_CachePic("gfx/box_mm2.lmp");
                DrawTransPic(cx, cy, p);
            }
            p = game_engine.Draw_CachePic("gfx/box_bm.lmp");
            DrawTransPic(cx, cy + 8, p);
            width -= 2;
            cx += 16;
        }

        // draw right side
        cy = y;
        p = game_engine.Draw_CachePic("gfx/box_tr.lmp");
        DrawTransPic(cx, cy, p);
        p = game_engine.Draw_CachePic("gfx/box_mr.lmp");
        for (int n = 0; n < lines; n++)
        {
            cy += 8;
            DrawTransPic(cx, cy, p);
        }
        p = game_engine.Draw_CachePic("gfx/box_br.lmp");
        DrawTransPic(cx, cy + 8, p);
    }
    public static void M_DrawSlider(int x, int y, float range)
    {
        if (range < 0)
            range = 0;
        if (range > 1)
            range = 1;
        M_DrawCharacter(x - 8, y, 128);
        int i;
        for (i = 0; i < SLIDER_RANGE; i++)
            M_DrawCharacter(x + i * 8, y, 129);
        M_DrawCharacter(x + i * 8, y, 130);
        M_DrawCharacter((int)(x + (SLIDER_RANGE - 1) * 8 * range), y, 131);
    }
    public static void M_DrawCheckbox(int x, int y, bool on)
    {
        if (on)
            M_Print(x, y, "on");
        else
            M_Print(x, y, "off");
    }
    public static void M_BuildTranslationTable(int top, int bottom)
    {
        for (int j = 0; j < 256; j++)
            _IdentityTable[j] = (byte)j;

        _IdentityTable.CopyTo(_TranslationTable, 0);

        if (top < 128)	// the artists made some backwards ranges.  sigh.
            Array.Copy(_IdentityTable, top, _TranslationTable, game_engine.TOP_RANGE, 16); // memcpy (dest + Render.TOP_RANGE, source + top, 16);
        else
            for (int j = 0; j < 16; j++)
                _TranslationTable[game_engine.TOP_RANGE + j] = _IdentityTable[top + 15 - j];

        if (bottom < 128)
            Array.Copy(_IdentityTable, bottom, _TranslationTable, game_engine.BOTTOM_RANGE, 16); // memcpy(dest + Render.BOTTOM_RANGE, source + bottom, 16);
        else
            for (int j = 0; j < 16; j++)
                _TranslationTable[game_engine.BOTTOM_RANGE + j] = _IdentityTable[bottom + 15 - j];
    }
}
abstract class MenuBase
{
    static MenuBase _CurrentMenu;
        
    // Top level menu items
    public static readonly MenuBase MainMenu = new MainMenu();
    public static readonly MenuBase SinglePlayerMenu = new SinglePlayerMenu();
    public static readonly MenuBase MultiPlayerMenu = new MultiPleerMenu();
    public static readonly MenuBase OptionsMenu = new OptionsMenu();
    public static readonly MenuBase HelpMenu = new HelpMenu();
    public static readonly MenuBase QuitMenu = new QuitMenu();
    public static readonly MenuBase LoadMenu = new LoadMenu();
    public static readonly MenuBase SaveMenu = new SaveMenu();
        
    // Submenus
    public static readonly MenuBase KeysMenu = new KeysMenu();
    public static readonly MenuBase LanConfigMenu = new LanConfigMenu();
    public static readonly MenuBase SetupMenu = new SetupMenu();
    public static readonly MenuBase GameOptionsMenu = new GameOptionsMenu();
    public static readonly MenuBase SearchMenu = new SearchMenu();
    public static readonly MenuBase ServerListMenu = new ServerListMenu();
    public static readonly MenuBase VideoMenu = new VideoMenu();

    public static MenuBase CurrentMenu
    {
        get { return _CurrentMenu; }
    }
        
    protected int _Cursor;

    public int Cursor
    {
        get { return _Cursor; }
    }

    public virtual void Show()
    {
        Menu.EnterSound = true;
        game_engine.key_dest = keydest_t.key_menu;
        _CurrentMenu = this;
    }

    public abstract void KeyEvent(int key);
    public abstract void Draw();

    public static void Hide()
    {
        game_engine.key_dest = keydest_t.key_game;
        _CurrentMenu = null;
    }
}
class MainMenu : MenuBase
{
    const int MAIN_ITEMS = 5;
    int _SaveDemoNum;

    public override void Show()
    {
        if (game_engine.key_dest != keydest_t.key_menu)
        {
            _SaveDemoNum = game_engine.cls.demonum;
            game_engine.cls.demonum = -1;
        }

        base.Show();
    }

    /// <summary>
    /// M_Main_Key
    /// </summary>
    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                //Key.Destination = keydest_t.key_game;
                MenuBase.Hide();
                game_engine.cls.demonum = _SaveDemoNum;
                if (game_engine.cls.demonum != -1 && !game_engine.cls.demoplayback && game_engine.cls.state != cactive_t.ca_connected)
                    game_engine.CL_NextDemo();
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                if (++_Cursor >= MAIN_ITEMS)
                    _Cursor = 0;
                break;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                if (--_Cursor < 0)
                    _Cursor = MAIN_ITEMS - 1;
                break;

            case q_shared.K_ENTER:
                Menu.EnterSound = true;

                switch (_Cursor)
                {
                    case 0:
                        MenuBase.SinglePlayerMenu.Show();
                        break;

                    case 1:
                        MenuBase.MultiPlayerMenu.Show();
                        break;

                    case 2:
                        MenuBase.OptionsMenu.Show();
                        break;

                    case 3:
                        MenuBase.HelpMenu.Show();
                        break;

                    case 4:
                        MenuBase.QuitMenu.Show();
                        break;
                }
                break;
        }

    }

    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/ttl_main.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);
        Menu.DrawTransPic(72, 32, game_engine.Draw_CachePic("gfx/mainmenu.lmp"));

        int f = (int)(game_engine.host_time * 10) % 6;

        Menu.DrawTransPic(54, 32 + _Cursor * 20, game_engine.Draw_CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));
    }
}
class SinglePlayerMenu : MenuBase
{
    const int SINGLEPLAYER_ITEMS = 3;

    /// <summary>
    /// M_SinglePlayer_Key
    /// </summary>
    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.MainMenu.Show();
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                if (++_Cursor >= SINGLEPLAYER_ITEMS)
                    _Cursor = 0;
                break;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                if (--_Cursor < 0)
                    _Cursor = SINGLEPLAYER_ITEMS - 1;
                break;

            case q_shared.K_ENTER:
                Menu.EnterSound = true;

                switch (_Cursor)
                {
                    case 0:
                        if (game_engine.sv.active)
                            if (!game_engine.SCR_ModalMessage("Are you sure you want to\nstart a new game?\n"))
                                break;
                        game_engine.key_dest = keydest_t.key_game;
                        if (game_engine.sv.active)
                            game_engine.Cbuf_AddText("disconnect\n");
                        game_engine.Cbuf_AddText("maxplayers 1\n");
                        game_engine.Cbuf_AddText("map start\n");
                        break;

                    case 1:
                        MenuBase.LoadMenu.Show();
                        break;

                    case 2:
                        MenuBase.SaveMenu.Show();
                        break;
                }
                break;
        }
    }

    /// <summary>
    /// M_SinglePlayer_Draw
    /// </summary>
    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/ttl_sgl.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);
        Menu.DrawTransPic(72, 32, game_engine.Draw_CachePic("gfx/sp_menu.lmp"));

        int f = (int)(game_engine.host_time * 10) % 6;

        Menu.DrawTransPic(54, 32 + _Cursor * 20, game_engine.Draw_CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));
    }
}
class LoadMenu : MenuBase
{
    public const int MAX_SAVEGAMES = 12;
    protected string[] _FileNames; //[MAX_SAVEGAMES]; // filenames
    protected bool[] _Loadable; //[MAX_SAVEGAMES]; // loadable

    public LoadMenu()
    {
        _FileNames = new string[MAX_SAVEGAMES];
        _Loadable = new bool[MAX_SAVEGAMES];
    }

    public override void Show()
    {
        base.Show();
        M_ScanSaves ();
    }
    protected void M_ScanSaves()
    {
        for (int i=0 ; i<MAX_SAVEGAMES ; i++)
	    {
            _FileNames[i] = "--- UNUSED SLOT ---";
            _Loadable[i] = false;
            string name =  String.Format("{0}/s{1}.sav", game_engine.com_gamedir, i);
            FileStream fs = game_engine.Sys_FileOpenRead(name);
            if (fs == null)
                continue;
                
            using(StreamReader reader = new StreamReader(fs, Encoding.ASCII))
            {
                string version = reader.ReadLine();
                if (version == null)
                    continue;
                string info = reader.ReadLine();
                if (info == null)
                    continue;
                info = info.TrimEnd('\0', '_').Replace('_', ' ');
                if (!String.IsNullOrEmpty(info))
                {
                    _FileNames[i] = info;
                    _Loadable[i] = true;
                }
            }
	    }
    }
        
    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.SinglePlayerMenu.Show();
                break;

            case q_shared.K_ENTER:
                game_engine.S_LocalSound("misc/menu2.wav");
                if (!_Loadable[_Cursor])
                    return;
                MenuBase.Hide();

                // Host_Loadgame_f can't bring up the loading plaque because too much
                // stack space has been used, so do it now
                game_engine.SCR_BeginLoadingPlaque();

                // issue the load command
                game_engine.Cbuf_AddText(String.Format("load s{0}\n", _Cursor));
                return;

            case q_shared.K_UPARROW:
            case q_shared.K_LEFTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = MAX_SAVEGAMES - 1;
                break;

            case q_shared.K_DOWNARROW:
            case q_shared.K_RIGHTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= MAX_SAVEGAMES)
                    _Cursor = 0;
                break;
        }
    }

    public override void Draw()
    {
        glpic_t p = game_engine.Draw_CachePic("gfx/p_load.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);

        for (int i = 0; i < MAX_SAVEGAMES; i++)
            Menu.M_Print(16, 32 + 8 * i, _FileNames[i]);

        // line cursor
        Menu.M_DrawCharacter(8, 32 + _Cursor * 8, 12 + ((int)(game_engine.realtime * 4) & 1));
    }
}
class SaveMenu : LoadMenu
{
    public override void Show()
    {
        if (!game_engine.sv.active)
            return;
        if (game_engine.cl.intermission != 0)
            return;
        if (game_engine.svs.maxclients != 1)
            return;

        base.Show();
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.SinglePlayerMenu.Show();
                break;

            case q_shared.K_ENTER:
                MenuBase.Hide();
                game_engine.Cbuf_AddText(String.Format("save s{0}\n", _Cursor));
                return;

            case q_shared.K_UPARROW:
            case q_shared.K_LEFTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = MAX_SAVEGAMES - 1;
                break;

            case q_shared.K_DOWNARROW:
            case q_shared.K_RIGHTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= MAX_SAVEGAMES)
                    _Cursor = 0;
                break;
        }
    }

    public override void Draw()
    {
        glpic_t p = game_engine.Draw_CachePic("gfx/p_save.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);

        for (int i = 0; i < MAX_SAVEGAMES; i++)
            Menu.M_Print(16, 32 + 8 * i, _FileNames[i]);

        // line cursor
        Menu.M_DrawCharacter(8, 32 + _Cursor * 8, 12 + ((int)(game_engine.realtime * 4) & 1));
    }
}
class QuitMenu : MenuBase
{
    MenuBase _PrevMenu; // m_quit_prevstate;

    public override void Show()
    {
        if (CurrentMenu == this)
            return;

        game_engine.key_dest = keydest_t.key_menu;
        _PrevMenu = CurrentMenu;

        base.Show();
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
            case 'n':
            case 'N':
                if (_PrevMenu != null)
                    _PrevMenu.Show();
                else
                    MenuBase.Hide();
                break;

            case 'Y':
            case 'y':
                game_engine.key_dest = keydest_t.key_console;
                game_engine.Host_Quit_f();
                break;

            default:
                break;
        }
    }

    public override void Draw()
    {
        Menu.M_DrawTextBox(0, 0, 38, 23);
        Menu.M_PrintWhite(16, 12, "  Quake version 1.09 by id Software\n\n");
        Menu.M_PrintWhite(16, 28, "Programming        Art \n");
        Menu.M_Print(16, 36, " John Carmack       Adrian Carmack\n");
        Menu.M_Print(16, 44, " Michael Abrash     Kevin Cloud\n");
        Menu.M_Print(16, 52, " John Cash          Paul Steed\n");
        Menu.M_Print(16, 60, " Dave 'Zoid' Kirsch\n");
        Menu.M_PrintWhite(16, 68, "Design             Biz\n");
        Menu.M_Print(16, 76, " John Romero        Jay Wilbur\n");
        Menu.M_Print(16, 84, " Sandy Petersen     Mike Wilson\n");
        Menu.M_Print(16, 92, " American McGee     Donna Jackson\n");
        Menu.M_Print(16, 100, " Tim Willits        Todd Hollenshead\n");
        Menu.M_PrintWhite(16, 108, "Support            Projects\n");
        Menu.M_Print(16, 116, " Barrett Alexander  Shawn Green\n");
        Menu.M_PrintWhite(16, 124, "Sound Effects\n");
        Menu.M_Print(16, 132, " Trent Reznor and Nine Inch Nails\n\n");
        Menu.M_PrintWhite(16, 140, "Quake is a trademark of Id Software,\n");
        Menu.M_PrintWhite(16, 148, "inc., (c)1996 Id Software, inc. All\n");
        Menu.M_PrintWhite(16, 156, "rights reserved. NIN logo is a\n");
        Menu.M_PrintWhite(16, 164, "registered trademark licensed to\n");
        Menu.M_PrintWhite(16, 172, "Nothing Interactive, Inc. All rights\n");
        Menu.M_PrintWhite(16, 180, "reserved. Press y to exit\n");
    }
}
class HelpMenu : MenuBase
{
    const int NUM_HELP_PAGES = 6;

    int _Page;

    public override void Show()
    {
        _Page = 0;
        base.Show();
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.MainMenu.Show();
                break;

            case q_shared.K_UPARROW:
            case q_shared.K_RIGHTARROW:
                Menu.EnterSound = true;
                if (++_Page >= NUM_HELP_PAGES)
                    _Page = 0;
                break;

            case q_shared.K_DOWNARROW:
            case q_shared.K_LEFTARROW:
                Menu.EnterSound = true;
                if (--_Page < 0)
                    _Page = NUM_HELP_PAGES - 1;
                break;
        }
    }

    public override void Draw()
    {
        Menu.DrawPic(0, 0, game_engine.Draw_CachePic(String.Format("gfx/help{0}.lmp", _Page)));
    }
}
class OptionsMenu : MenuBase
{
    const int OPTIONS_ITEMS = 13;

    float _BgmVolumeCoeff = 0.1f;

    public override void Show()
    {
        if (game_engine.IsWindows)
        {
            _BgmVolumeCoeff = 1.0f;
        }

        if (_Cursor > OPTIONS_ITEMS - 1)
            _Cursor = 0;

        if (_Cursor == OPTIONS_ITEMS - 1 && MenuBase.VideoMenu == null)
            _Cursor = 0;

        base.Show();
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.MainMenu.Show();
                break;

            case q_shared.K_ENTER:
                Menu.EnterSound = true;
                switch (_Cursor)
                {
                    case 0:
                        MenuBase.KeysMenu.Show();
                        break;

                    case 1:
                        MenuBase.Hide();
                        game_engine.Con_ToggleConsole_f();
                        break;

                    case 2:
                        game_engine.Cbuf_AddText("exec default.cfg\n");
                        break;

                    case 12:
                        MenuBase.VideoMenu.Show();
                        break;

                    default:
                        AdjustSliders(1);
                        break;
                }
                return;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = OPTIONS_ITEMS - 1;
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= OPTIONS_ITEMS)
                    _Cursor = 0;
                break;

            case q_shared.K_LEFTARROW:
                AdjustSliders(-1);
                break;

            case q_shared.K_RIGHTARROW:
                AdjustSliders(1);
                break;
        }

        if (_Cursor == 12 && VideoMenu == null)
        {
            if (key == q_shared.K_UPARROW)
                _Cursor = 11;
            else
                _Cursor = 0;
        }

#if _WIN32
        if ((options_cursor == 13) && (modestate != MS_WINDOWED))
        {
            if (k == K_UPARROW)
                options_cursor = 12;
            else
                options_cursor = 0;
        }
#endif
    }

    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/p_option.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);

        Menu.M_Print(16, 32, "    Customize controls");
        Menu.M_Print(16, 40, "         Go to console");
        Menu.M_Print(16, 48, "     Reset to defaults");

        Menu.M_Print(16, 56, "           Screen size");
        float r = (game_engine.scr_viewsize.value - 30) / (120 - 30);
        Menu.M_DrawSlider(220, 56, r);

        Menu.M_Print(16, 64, "            Brightness");
        r = (1.0f - game_engine.gamma.value) / 0.5f;
        Menu.M_DrawSlider(220, 64, r);

        Menu.M_Print(16, 72, "           Mouse Speed");
        r = (game_engine.sensitivity.value - 1) / 10;
        Menu.M_DrawSlider(220, 72, r);

        Menu.M_Print(16, 80, "       CD Music Volume");
        r = game_engine.bgmvolume.value;
        Menu.M_DrawSlider(220, 80, r);

        Menu.M_Print(16, 88, "          Sound Volume");
        r = game_engine.volume.value;
        Menu.M_DrawSlider(220, 88, r);

        Menu.M_Print(16, 96, "            Always Run");
        Menu.M_DrawCheckbox(220, 96, game_engine.cl_forwardspeed.value > 200);

        Menu.M_Print(16, 104, "          Invert Mouse");
        Menu.M_DrawCheckbox(220, 104, game_engine.m_pitch.value < 0);

        Menu.M_Print(16, 112, "            Lookspring");
        Menu.M_DrawCheckbox(220, 112, (game_engine.lookspring.value != 0));

        Menu.M_Print(16, 120, "            Lookstrafe");
        Menu.M_DrawCheckbox(220, 120, (game_engine.lookstrafe.value != 0));

        if (VideoMenu != null)
            Menu.M_Print(16, 128, "         Video Options");

#if _WIN32
if (modestate == MS_WINDOWED)
{
	Menu.Print (16, 136, "             Use Mouse");
	Menu.DrawCheckbox (220, 136, _windowed_mouse.value);
}
#endif

        // cursor
        Menu.M_DrawCharacter(200, 32 + _Cursor * 8, 12 + ((int)(game_engine.realtime * 4) & 1));
    }

    /// <summary>
    /// M_AdjustSliders
    /// </summary>
    void AdjustSliders(int dir)
    {
        game_engine.S_LocalSound("misc/menu3.wav");
        float value;

        switch (_Cursor)
        {
            case 3:	// screen size
                value = game_engine.scr_viewsize.value + dir * 10;
                if (value < 30)
                    value = 30;
                if (value > 120)
                    value = 120;
                Cvar.Cvar_SetValue("viewsize", value);
                break;

            case 4:	// gamma
                value = game_engine.gamma.value - dir * 0.05f;
                if (value < 0.5)
                    value = 0.5f;
                if (value > 1)
                    value = 1;
                Cvar.Cvar_SetValue("gamma", value);
                break;

            case 5:	// mouse speed
                value = game_engine.sensitivity.value + dir * 0.5f;
                if (value < 1)
                    value = 1;
                if (value > 11)
                    value = 11;
                Cvar.Cvar_SetValue("sensitivity", value);
                break;

            case 6:	// music volume
                value = game_engine.bgmvolume.value + dir * _BgmVolumeCoeff;
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                Cvar.Cvar_SetValue("bgmvolume", value);
                break;

            case 7:	// sfx volume
                value = game_engine.volume.value + dir * 0.1f;
                if (value < 0)
                    value = 0;
                if (value > 1)
                    value = 1;
                Cvar.Cvar_SetValue("volume", value);
                break;


            case 8:	// allways run
                if (game_engine.cl_forwardspeed.value > 200)
                {
                    Cvar.Cvar_SetValue("cl_forwardspeed", 200f);
                    Cvar.Cvar_SetValue("cl_backspeed", 200f);
                }
                else
                {
                    Cvar.Cvar_SetValue("cl_forwardspeed", 400f);
                    Cvar.Cvar_SetValue("cl_backspeed", 400f);
                }
                break;


            case 9:	// invert mouse
                Cvar.Cvar_SetValue("m_pitch", -game_engine.m_pitch.value);
                break;


            case 10:	// lookspring
                Cvar.Cvar_SetValue("lookspring", (game_engine.lookspring.value == 0) ? 1f : 0f);
                break;


            case 11:	// lookstrafe
                Cvar.Cvar_SetValue("lookstrafe", (game_engine.lookstrafe.value == 0) ? 1f : 0f);
                break;

#if _WIN32
	    case 13:	// _windowed_mouse
		    Cvar_SetValue ("_windowed_mouse", !_windowed_mouse.value);
		    break;
#endif
        }
    }
}
class KeysMenu : MenuBase
{
    static readonly string[][] _BindNames = new string[][]
    {
        new string[] {"+attack", 		"attack"},
        new string[] {"impulse 10", 	"change weapon"},
        new string[] {"+jump", 			"jump / swim up"},
        new string[] {"+forward", 		"walk forward"},
        new string[] {"+back", 			"backpedal"},
        new string[] {"+left", 			"turn left"},
        new string[] {"+right", 		"turn right"},
        new string[] {"+speed", 		"run"},
        new string[] {"+moveleft", 		"step left"},
        new string[] {"+moveright", 	"step right"},
        new string[] {"+strafe", 		"sidestep"},
        new string[] {"+lookup", 		"look up"},
        new string[] {"+lookdown", 		"look down"},
        new string[] {"centerview", 	"center view"},
        new string[] {"+mlook", 		"mouse look"},
        new string[] {"+klook", 		"keyboard look"},
        new string[] {"+moveup",		"swim up"},
        new string[] {"+movedown",		"swim down"}
    };

    //const inte	NUMCOMMANDS	(sizeof(bindnames)/sizeof(bindnames[0]))

    bool _BindGrab; // bind_grab

    public override void Show()
    {
        base.Show();
    }

    public override void KeyEvent(int key)
    {
        if (_BindGrab)
        {
            // defining a key
            game_engine.S_LocalSound("misc/menu1.wav");
            if (key == q_shared.K_ESCAPE)
            {
                _BindGrab = false;
            }
            else if (key != '`')
            {
                string cmd = String.Format("bind \"{0}\" \"{1}\"\n", game_engine.Key_KeynumToString(key), _BindNames[_Cursor][0]);
                game_engine.Cbuf_InsertText(cmd);
            }

            _BindGrab = false;
            return;
        }

        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.OptionsMenu.Show();
                break;

            case q_shared.K_LEFTARROW:
            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = _BindNames.Length - 1;
                break;

            case q_shared.K_DOWNARROW:
            case q_shared.K_RIGHTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= _BindNames.Length)
                    _Cursor = 0;
                break;

            case q_shared.K_ENTER:		// go into bind mode
                int[] keys = new int[2];
                FindKeysForCommand(_BindNames[_Cursor][0], keys);
                game_engine.S_LocalSound("misc/menu2.wav");
                if (keys[1] != -1)
                    UnbindCommand(_BindNames[_Cursor][0]);
                _BindGrab = true;
                break;

            case q_shared.K_BACKSPACE:		// delete bindings
            case q_shared.K_DEL:				// delete bindings
                game_engine.S_LocalSound("misc/menu2.wav");
                UnbindCommand(_BindNames[_Cursor][0]);
                break;
        }
    }

    public override void Draw()
    {
        glpic_t p = game_engine.Draw_CachePic("gfx/ttl_cstm.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);

        if (_BindGrab)
            Menu.M_Print(12, 32, "Press a key or button for this action");
        else
            Menu.M_Print(18, 32, "Enter to change, backspace to clear");

        // search for known bindings
        int[] keys = new int[2];
            
        for (int i = 0; i < _BindNames.Length; i++)
        {
            int y = 48 + 8 * i;

            Menu.M_Print(16, y, _BindNames[i][1]);

            FindKeysForCommand(_BindNames[i][0], keys);

            if (keys[0] == -1)
            {
                Menu.M_Print(140, y, "???");
            }
            else
            {
                string name = game_engine.Key_KeynumToString(keys[0]);
                Menu.M_Print(140, y, name);
                int x = name.Length * 8;
                if (keys[1] != -1)
                {
                    Menu.M_Print(140 + x + 8, y, "or");
                    Menu.M_Print(140 + x + 32, y, game_engine.Key_KeynumToString(keys[1]));
                }
            }
        }

        if (_BindGrab)
            Menu.M_DrawCharacter(130, 48 + _Cursor * 8, '=');
        else
            Menu.M_DrawCharacter(130, 48 + _Cursor * 8, 12 + ((int)(game_engine.realtime * 4) & 1));

    }

    /// <summary>
    /// M_FindKeysForCommand
    /// </summary>
    void FindKeysForCommand(string command, int[] twokeys)
    {
        twokeys[0] = twokeys[1] = -1;
        int len = command.Length;
        int count = 0;

        for (int j = 0; j < 256; j++)
        {
            string b = game_engine.keybindings[j];
            if (String.IsNullOrEmpty(b))
                continue;

            if (String.Compare(b, 0, command, 0, len) == 0)
            {
                twokeys[count] = j;
                count++;
                if (count == 2)
                    break;
            }
        }
    }

    /// <summary>
    /// M_UnbindCommand
    /// </summary>
    void UnbindCommand(string command)
    {
        int len = command.Length;

        for (int j = 0; j < 256; j++)
        {
            string b = game_engine.keybindings[j];
            if (String.IsNullOrEmpty(b))
                continue;

            if (String.Compare(b, 0, command, 0, len) == 0)
                game_engine.Key_SetBinding(j, String.Empty);
        }
    }
}
class MultiPleerMenu : MenuBase
{
    const int MULTIPLAYER_ITEMS = 3;

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.MainMenu.Show();
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                if (++_Cursor >= MULTIPLAYER_ITEMS)
                    _Cursor = 0;
                break;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                if (--_Cursor < 0)
                    _Cursor = MULTIPLAYER_ITEMS - 1;
                break;

            case q_shared.K_ENTER:
                Menu.EnterSound = true;
                switch (_Cursor)
                {
                    case 0:
                        if (NetTcpIp.Instance.IsInitialized)
                            MenuBase.LanConfigMenu.Show();
                        break;

                    case 1:
                        if (NetTcpIp.Instance.IsInitialized)
                            MenuBase.LanConfigMenu.Show();
                        break;

                    case 2:
                        MenuBase.SetupMenu.Show();
                        break;
                }
                break;
        }
    }

    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/p_multi.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);
        Menu.DrawTransPic(72, 32, game_engine.Draw_CachePic("gfx/mp_menu.lmp"));

        float f = (int)(game_engine.host_time * 10) % 6;

        Menu.DrawTransPic(54, 32 + _Cursor * 20, game_engine.Draw_CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));

        if (NetTcpIp.Instance.IsInitialized)
            return;
        Menu.M_PrintWhite((320 / 2) - ((27 * 8) / 2), 148, "No Communications Available");
    }
}
class LanConfigMenu : MenuBase
{
    const int NUM_LANCONFIG_CMDS = 3;

    static readonly int[] _CursorTable = new int[] { 72, 92, 124 };
        
    int _Port;
    string _PortName;
    string _JoinName;

    public bool JoiningGame
    {
        get { return MenuBase.MultiPlayerMenu.Cursor == 0; }
    }
    public bool StartingGame
    {
        get { return MenuBase.MultiPlayerMenu.Cursor == 1; }
    }
        
    public LanConfigMenu()
    {
        _Cursor = -1;
        _JoinName = String.Empty;
    }

    public override void Show()
    {
        base.Show();
            
        if (_Cursor == -1)
        {
            if (JoiningGame)
                _Cursor = 2;
            else
                _Cursor = 1;
        }
        if (StartingGame && _Cursor == 2)
            _Cursor = 1;
        _Port = game_engine.DEFAULTnet_hostport;
        _PortName = _Port.ToString();

        Menu.ReturnOnError = false;
        Menu.ReturnReason = String.Empty;
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.MultiPlayerMenu.Show();
                break;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = NUM_LANCONFIG_CMDS - 1;
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= NUM_LANCONFIG_CMDS)
                    _Cursor = 0;
                break;

            case q_shared.K_ENTER:
                if (_Cursor == 0)
                    break;

                Menu.EnterSound = true;
                game_engine.net_hostport = _Port;

                if (_Cursor == 1)
                {
                    if (StartingGame)
                    {
                        MenuBase.GameOptionsMenu.Show();
                    }
                    else
                    {
                        MenuBase.SearchMenu.Show();
                    }
                    break;
                }

                if (_Cursor == 2)
                {
                    Menu.ReturnMenu = this;
                    Menu.ReturnOnError = true;
                    MenuBase.Hide();
                    game_engine.Cbuf_AddText(String.Format("connect \"{0}\"\n", _JoinName));
                    break;
                }
                break;

            case q_shared.K_BACKSPACE:
                if (_Cursor == 0)
                {
                    if (!String.IsNullOrEmpty(_PortName))
                        _PortName = _PortName.Substring(0, _PortName.Length - 1);
                }

                if (_Cursor == 2)
                {
                    if (!String.IsNullOrEmpty(_JoinName))
                        _JoinName = _JoinName.Substring(0, _JoinName.Length - 1);
                }
                break;

            default:
                if (key < 32 || key > 127)
                    break;

                if (_Cursor == 2)
                {
                    if (_JoinName.Length < 21)
                        _JoinName += (char)key;
                }

                if (key < '0' || key > '9')
                    break;

                if (_Cursor == 0)
                {
                    if (_PortName.Length < 5)
                        _PortName += (char)key;
                }
                break;
        }

        if (StartingGame && _Cursor == 2)
            if (key == q_shared.K_UPARROW)
                _Cursor = 1;
            else
                _Cursor = 0;

        int k = game_engine.atoi(_PortName);
        if (k > 65535)
            k = _Port;
        else
            _Port = k;
        _PortName = _Port.ToString();
    }

    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/p_multi.lmp");
        int basex = (320 - p.width) / 2;
        Menu.DrawPic(basex, 4, p);

        string startJoin;
        if (StartingGame)
            startJoin = "New Game - TCP/IP";
        else
            startJoin = "Join Game - TCP/IP";

        Menu.M_Print(basex, 32, startJoin);
        basex += 8;

        Menu.M_Print(basex, 52, "Address:");
        Menu.M_Print(basex + 9 * 8, 52, game_engine.my_tcpip_address);

        Menu.M_Print(basex, _CursorTable[0], "Port");
        Menu.M_DrawTextBox(basex + 8 * 8, _CursorTable[0] - 8, 6, 1);
        Menu.M_Print(basex + 9 * 8, _CursorTable[0], _PortName);

        if (JoiningGame)
        {
            Menu.M_Print(basex, _CursorTable[1], "Search for local games...");
            Menu.M_Print(basex, 108, "Join game at:");
            Menu.M_DrawTextBox(basex + 8, _CursorTable[2] - 8, 22, 1);
            Menu.M_Print(basex + 16, _CursorTable[2], _JoinName);
        }
        else
        {
            Menu.M_DrawTextBox(basex, _CursorTable[1] - 8, 2, 1);
            Menu.M_Print(basex + 8, _CursorTable[1], "OK");
        }

        Menu.M_DrawCharacter(basex - 8, _CursorTable[_Cursor], 12 + ((int)(game_engine.realtime * 4) & 1));

        if (_Cursor == 0)
            Menu.M_DrawCharacter(basex + 9 * 8 + 8 * _PortName.Length,
                _CursorTable[0], 10 + ((int)(game_engine.realtime * 4) & 1));

        if (_Cursor == 2)
            Menu.M_DrawCharacter(basex + 16 + 8 * _JoinName.Length, _CursorTable[2],
                10 + ((int)(game_engine.realtime * 4) & 1));

        if (!String.IsNullOrEmpty(Menu.ReturnReason))
            Menu.M_PrintWhite(basex, 148, Menu.ReturnReason);
    }
}
class SetupMenu : MenuBase
{
    const int NUM_SETUP_CMDS = 5;

    readonly int[] _CursorTable = new int[]
    {
        40, 56, 80, 104, 140
    }; // setup_cursor_table
        
    string _HostName; // setup_hostname[16]
    string _MyName; // setup_myname[16]
    int _OldTop; // setup_oldtop
    int _OldBottom; // setup_oldbottom
    int _Top; // setup_top
    int _Bottom; // setup_bottom

    /// <summary>
    /// M_Menu_Setup_f
    /// </summary>
    public override void Show()
    {
        _MyName = game_engine.cl_name.@string;
        _HostName = game_engine.hostname.@string;
        _Top = _OldTop = ((int)game_engine.cl_color.value) >> 4;
        _Bottom = _OldBottom = ((int)game_engine.cl_color.value) & 15;

        base.Show();
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.MultiPlayerMenu.Show();
                break;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = NUM_SETUP_CMDS - 1;
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= NUM_SETUP_CMDS)
                    _Cursor = 0;
                break;

            case q_shared.K_LEFTARROW:
                if (_Cursor < 2)
                    return;
                game_engine.S_LocalSound("misc/menu3.wav");
                if (_Cursor == 2)
                    _Top = _Top - 1;
                if (_Cursor == 3)
                    _Bottom = _Bottom - 1;
                break;
                
            case q_shared.K_RIGHTARROW:
                if (_Cursor < 2)
                    return;
            forward:
                game_engine.S_LocalSound("misc/menu3.wav");
                if (_Cursor == 2)
                    _Top = _Top + 1;
                if (_Cursor == 3)
                    _Bottom = _Bottom + 1;
                break;

            case q_shared.K_ENTER:
                if (_Cursor == 0 || _Cursor == 1)
                    return;

                if (_Cursor == 2 || _Cursor == 3)
                    goto forward;

                // _Cursor == 4 (OK)
                if (_MyName != game_engine.cl_name.@string)
                    game_engine.Cbuf_AddText(String.Format("name \"{0}\"\n", _MyName));
                if (game_engine.hostname.@string != _HostName)
                    Cvar.Cvar_Set("hostname", _HostName);
                if (_Top != _OldTop || _Bottom != _OldBottom)
                    game_engine.Cbuf_AddText(String.Format("color {0} {1}\n", _Top, _Bottom));
                Menu.EnterSound = true;
                MenuBase.MultiPlayerMenu.Show();
                break;

            case q_shared.K_BACKSPACE:
                if (_Cursor == 0)
                {
                    if (!String.IsNullOrEmpty(_HostName))
                        _HostName = _HostName.Substring(0, _HostName.Length - 1);// setup_hostname[strlen(setup_hostname) - 1] = 0;
                }

                if (_Cursor == 1)
                {
                    if (!String.IsNullOrEmpty(_MyName))
                        _MyName = _MyName.Substring(0, _MyName.Length - 1);
                }
                break;

            default:
                if (key < 32 || key > 127)
                    break;
                if (_Cursor == 0)
                {
                    int l = _HostName.Length;
                    if (l < 15)
                    {
                        _HostName = _HostName + (char)key;
                    }
                }
                if (_Cursor == 1)
                {
                    int l = _MyName.Length;
                    if (l < 15)
                    {
                        _MyName = _MyName + (char)key;
                    }
                }
                break;
        }

        if (_Top > 13)
            _Top = 0;
        if (_Top < 0)
            _Top = 13;
        if (_Bottom > 13)
            _Bottom = 0;
        if (_Bottom < 0)
            _Bottom = 13;
    }

    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/p_multi.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);

        Menu.M_Print(64, 40, "Hostname");
        Menu.M_DrawTextBox(160, 32, 16, 1);
        Menu.M_Print(168, 40, _HostName);

        Menu.M_Print(64, 56, "Your name");
        Menu.M_DrawTextBox(160, 48, 16, 1);
        Menu.M_Print(168, 56, _MyName);

        Menu.M_Print(64, 80, "Shirt color");
        Menu.M_Print(64, 104, "Pants color");

        Menu.M_DrawTextBox(64, 140 - 8, 14, 1);
        Menu.M_Print(72, 140, "Accept Changes");

        p = game_engine.Draw_CachePic("gfx/bigbox.lmp");
        Menu.DrawTransPic(160, 64, p);
        p = game_engine.Draw_CachePic("gfx/menuplyr.lmp");
        Menu.M_BuildTranslationTable(_Top * 16, _Bottom * 16);
        Menu.M_DrawTransPicTranslate(172, 72, p);

        Menu.M_DrawCharacter(56, _CursorTable[_Cursor], 12 + ((int)(game_engine.realtime * 4) & 1));

        if (_Cursor == 0)
            Menu.M_DrawCharacter(168 + 8 * _HostName.Length, _CursorTable[_Cursor], 10 + ((int)(game_engine.realtime * 4) & 1));

        if (_Cursor == 1)
            Menu.M_DrawCharacter(168 + 8 * _MyName.Length, _CursorTable[_Cursor], 10 + ((int)(game_engine.realtime * 4) & 1));
    }
}
class GameOptionsMenu : MenuBase
{
    class level_t
    {
	    public string name;
	    public string description;

        public level_t(string name, string desc)
        {
            this.name = name;
            this.description = desc;
        }
    } //level_t;

    class episode_t
    {
	    public string description;
	    public int firstLevel;
	    public int levels;

        public episode_t(string desc, int firstLevel, int levels)
        {
            this.description = desc;
            this.firstLevel = firstLevel;
            this.levels = levels;
        }
    } //episode_t;

    static readonly level_t[] Levels = new level_t[]
    {
	    new level_t("start", "Entrance"),	// 0

	    new level_t("e1m1", "Slipgate Complex"),				// 1
	    new level_t("e1m2", "Castle of the Damned"),
	    new level_t("e1m3", "The Necropolis"),
	    new level_t("e1m4", "The Grisly Grotto"),
	    new level_t("e1m5", "Gloom Keep"),
	    new level_t("e1m6", "The Door To Chthon"),
	    new level_t("e1m7", "The House of Chthon"),
	    new level_t("e1m8", "Ziggurat Vertigo"),

	    new level_t("e2m1", "The Installation"),				// 9
	    new level_t("e2m2", "Ogre Citadel"),
	    new level_t("e2m3", "Crypt of Decay"),
	    new level_t("e2m4", "The Ebon Fortress"),
	    new level_t("e2m5", "The Wizard's Manse"),
	    new level_t("e2m6", "The Dismal Oubliette"),
	    new level_t("e2m7", "Underearth"),

	    new level_t("e3m1", "Termination Central"),			// 16
	    new level_t("e3m2", "The Vaults of Zin"),
	    new level_t("e3m3", "The Tomb of Terror"),
	    new level_t("e3m4", "Satan's Dark Delight"),
	    new level_t("e3m5", "Wind Tunnels"),
	    new level_t("e3m6", "Chambers of Torment"),
	    new level_t("e3m7", "The Haunted Halls"),

	    new level_t("e4m1", "The Sewage System"),				// 23
	    new level_t("e4m2", "The Tower of Despair"),
	    new level_t("e4m3", "The Elder God Shrine"),
	    new level_t("e4m4", "The Palace of Hate"),
	    new level_t("e4m5", "Hell's Atrium"),
	    new level_t("e4m6", "The Pain Maze"),
	    new level_t("e4m7", "Azure Agony"),
	    new level_t("e4m8", "The Nameless City"),

	    new level_t("end", "Shub-Niggurath's Pit"),			// 31

	    new level_t("dm1", "Place of Two Deaths"),				// 32
	    new level_t("dm2", "Claustrophobopolis"),
	    new level_t("dm3", "The Abandoned Base"),
	    new level_t("dm4", "The Bad Place"),
	    new level_t("dm5", "The Cistern"),
	    new level_t("dm6", "The Dark Zone")
    };

    //MED 01/06/97 added hipnotic levels
    static readonly level_t[] HipnoticLevels = new level_t[]
    {
        new level_t("start", "Command HQ"),  // 0

        new level_t("hip1m1", "The Pumping Station"),          // 1
        new level_t("hip1m2", "Storage Facility"),
        new level_t("hip1m3", "The Lost Mine"),
        new level_t("hip1m4", "Research Facility"),
        new level_t("hip1m5", "Military Complex"),

        new level_t("hip2m1", "Ancient Realms"),          // 6
        new level_t("hip2m2", "The Black Cathedral"),
        new level_t("hip2m3", "The Catacombs"),
        new level_t("hip2m4", "The Crypt"),
        new level_t("hip2m5", "Mortum's Keep"),
        new level_t("hip2m6", "The Gremlin's Domain"),

        new level_t("hip3m1", "Tur Torment"),       // 12
        new level_t("hip3m2", "Pandemonium"),
        new level_t("hip3m3", "Limbo"),
        new level_t("hip3m4", "The Gauntlet"),

        new level_t("hipend", "Armagon's Lair"),       // 16

        new level_t("hipdm1", "The Edge of Oblivion")           // 17
    };

    //PGM 01/07/97 added rogue levels
    //PGM 03/02/97 added dmatch level
    static readonly level_t[] RogueLevels = new level_t[]
    {
	    new level_t("start", "Split Decision"),
	    new level_t("r1m1",	"Deviant's Domain"),
	    new level_t("r1m2",	"Dread Portal"),
	    new level_t("r1m3",	"Judgement Call"),
	    new level_t("r1m4",	"Cave of Death"),
	    new level_t("r1m5",	"Towers of Wrath"),
	    new level_t("r1m6",	"Temple of Pain"),
	    new level_t("r1m7",	"Tomb of the Overlord"),
	    new level_t("r2m1",	"Tempus Fugit"),
	    new level_t("r2m2",	"Elemental Fury I"),
	    new level_t("r2m3",	"Elemental Fury II"),
	    new level_t("r2m4",	"Curse of Osiris"),
	    new level_t("r2m5",	"Wizard's Keep"),
	    new level_t("r2m6",	"Blood Sacrifice"),
	    new level_t("r2m7",	"Last Bastion"),
	    new level_t("r2m8",	"Source of Evil"),
	    new level_t("ctf1", "Division of Change")
    };

    static readonly episode_t[] Episodes = new episode_t[]
    {
	    new episode_t("Welcome to Quake", 0, 1),
	    new episode_t("Doomed Dimension", 1, 8),
	    new episode_t("Realm of Black Magic", 9, 7),
	    new episode_t("Netherworld", 16, 7),
	    new episode_t("The Elder World", 23, 8),
	    new episode_t("Final Level", 31, 1),
	    new episode_t("Deathmatch Arena", 32, 6)
    };

    //MED 01/06/97  added hipnotic episodes
    static readonly episode_t[] HipnoticEpisodes = new episode_t[]
    {
        new episode_t("Scourge of Armagon", 0, 1),
        new episode_t("Fortress of the Dead", 1, 5),
        new episode_t("Dominion of Darkness", 6, 6),
        new episode_t("The Rift", 12, 4),
        new episode_t("Final Level", 16, 1),
        new episode_t("Deathmatch Arena", 17, 1)
    };

    //PGM 01/07/97 added rogue episodes
    //PGM 03/02/97 added dmatch episode
    static readonly episode_t[] RogueEpisodes = new episode_t[]
    {
	    new episode_t("Introduction", 0, 1),
	    new episode_t("Hell's Fortress", 1, 7),
	    new episode_t("Corridors of Time", 8, 8),
	    new episode_t("Deathmatch Arena", 16, 1)
    };

    static readonly int[] _CursorTable = new int[]
    {
        40, 56, 64, 72, 80, 88, 96, 112, 120
    };

    const int NUM_GAMEOPTIONS = 9;

    int _StartEpisode;
    int _StartLevel;
    int _MaxPlayers;
    bool _ServerInfoMessage;
    double _ServerInfoMessageTime;


    public override void Show()
    {
        base.Show();

        if (_MaxPlayers == 0)
            _MaxPlayers = game_engine.svs.maxclients;
        if (_MaxPlayers < 2)
            _MaxPlayers = game_engine.svs.maxclientslimit;

    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.LanConfigMenu.Show();
                break;

            case q_shared.K_UPARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = NUM_GAMEOPTIONS - 1;
                break;

            case q_shared.K_DOWNARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= NUM_GAMEOPTIONS)
                    _Cursor = 0;
                break;

            case q_shared.K_LEFTARROW:
                if (_Cursor == 0)
                    break;
                game_engine.S_LocalSound("misc/menu3.wav");
                Change(-1);
                break;

            case q_shared.K_RIGHTARROW:
                if (_Cursor == 0)
                    break;
                game_engine.S_LocalSound("misc/menu3.wav");
                Change(1);
                break;

            case q_shared.K_ENTER:
                game_engine.S_LocalSound("misc/menu2.wav");
                if (_Cursor == 0)
                {
                    if (game_engine.sv.active)
                        game_engine.Cbuf_AddText("disconnect\n");
                    game_engine.Cbuf_AddText("listen 0\n");	// so host_netport will be re-examined
                    game_engine.Cbuf_AddText(String.Format("maxplayers {0}\n", _MaxPlayers));
                    game_engine.SCR_BeginLoadingPlaque();

                    if (game_engine._GameKind == GameKind.Hipnotic)
                        game_engine.Cbuf_AddText(String.Format("map {0}\n",
                            HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name));
                    else if (game_engine._GameKind == GameKind.Rogue)
                        game_engine.Cbuf_AddText(String.Format("map {0}\n",
                            RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name));
                    else
                        game_engine.Cbuf_AddText(String.Format("map {0}\n", Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name));

                    return;
                }

                Change(1);
                break;
        }
    }

    /// <summary>
    /// M_NetStart_Change
    /// </summary>
    void Change(int dir)
    {
        int count;

        switch (_Cursor)
        {
            case 1:
                _MaxPlayers += dir;
                if (_MaxPlayers > game_engine.svs.maxclientslimit)
                {
                    _MaxPlayers = game_engine.svs.maxclientslimit;
                    _ServerInfoMessage = true;
                    _ServerInfoMessageTime = game_engine.realtime;
                }
                if (_MaxPlayers < 2)
                    _MaxPlayers = 2;
                break;

            case 2:
                Cvar.Cvar_SetValue("coop", (game_engine.coop.value != 0) ? 0 : 1);
                break;

            case 3:
                if (game_engine._GameKind == GameKind.Rogue)
                    count = 6;
                else
                    count = 2;

                float tp = game_engine.teamplay.value + dir;
                if (tp > count)
                    tp = 0;
                else if (tp < 0)
                    tp = count;

                Cvar.Cvar_SetValue("teamplay", tp);
                break;

            case 4:
                float skill = game_engine.skill.value + dir;
                if (skill > 3)
                    skill = 0;
                if (skill < 0)
                    skill = 3;
                Cvar.Cvar_SetValue("skill", skill);
                break;

            case 5:
                float fraglimit = game_engine.fraglimit.value + dir * 10;
                if (fraglimit > 100)
                    fraglimit = 0;
                if (fraglimit < 0)
                    fraglimit = 100;
                Cvar.Cvar_SetValue("fraglimit", fraglimit);
                break;

            case 6:
                float timelimit = game_engine.timelimit.value + dir * 5;
                if (timelimit > 60)
                    timelimit = 0;
                if (timelimit < 0)
                    timelimit = 60;
                Cvar.Cvar_SetValue("timelimit", timelimit);
                break;

            case 7:
                _StartEpisode += dir;
                //MED 01/06/97 added hipnotic count
                if (game_engine._GameKind == GameKind.Hipnotic)
                    count = 6;
                //PGM 01/07/97 added rogue count
                //PGM 03/02/97 added 1 for dmatch episode
                else if (game_engine._GameKind == GameKind.Rogue)
                    count = 4;
                else if (game_engine.registered.value != 0)
                    count = 7;
                else
                    count = 2;

                if (_StartEpisode < 0)
                    _StartEpisode = count - 1;

                if (_StartEpisode >= count)
                    _StartEpisode = 0;

                _StartLevel = 0;
                break;

            case 8:
                _StartLevel += dir;
                //MED 01/06/97 added hipnotic episodes
                if (game_engine._GameKind == GameKind.Hipnotic)
                    count = HipnoticEpisodes[_StartEpisode].levels;
                //PGM 01/06/97 added hipnotic episodes
                else if (game_engine._GameKind == GameKind.Rogue)
                    count = RogueEpisodes[_StartEpisode].levels;
                else
                    count = Episodes[_StartEpisode].levels;

                if (_StartLevel < 0)
                    _StartLevel = count - 1;

                if (_StartLevel >= count)
                    _StartLevel = 0;
                break;
        }
    }

    public override void Draw()
    {
        Menu.DrawTransPic(16, 4, game_engine.Draw_CachePic("gfx/qplaque.lmp"));
        glpic_t p = game_engine.Draw_CachePic("gfx/p_multi.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);

        Menu.M_DrawTextBox(152, 32, 10, 1);
        Menu.M_Print(160, 40, "begin game");

        Menu.M_Print(0, 56, "      Max players");
        Menu.M_Print(160, 56, _MaxPlayers.ToString());

        Menu.M_Print(0, 64, "        Game Type");
        if (game_engine.coop.value != 0)
            Menu.M_Print(160, 64, "Cooperative");
        else
            Menu.M_Print(160, 64, "Deathmatch");

        Menu.M_Print(0, 72, "        Teamplay");
        if (game_engine._GameKind == GameKind.Rogue)
        {
            string msg;
            switch ((int)game_engine.teamplay.value)
            {
                case 1: msg = "No Friendly Fire"; break;
                case 2: msg = "Friendly Fire"; break;
                case 3: msg = "Tag"; break;
                case 4: msg = "Capture the Flag"; break;
                case 5: msg = "One Flag CTF"; break;
                case 6: msg = "Three Team CTF"; break;
                default:
                    msg = "Off";
                    break;
            }
            Menu.M_Print(160, 72, msg);
        }
        else
        {
            string msg;
            switch ((int)game_engine.teamplay.value)
            {
                case 1: msg = "No Friendly Fire"; break;
                case 2: msg = "Friendly Fire"; break;
                default:
                    msg = "Off";
                    break;
            }
            Menu.M_Print(160, 72, msg);
        }

        Menu.M_Print(0, 80, "            Skill");
        if (game_engine.skill.value == 0)
            Menu.M_Print(160, 80, "Easy difficulty");
        else if (game_engine.skill.value == 1)
            Menu.M_Print(160, 80, "Normal difficulty");
        else if (game_engine.skill.value == 2)
            Menu.M_Print(160, 80, "Hard difficulty");
        else
            Menu.M_Print(160, 80, "Nightmare difficulty");

        Menu.M_Print(0, 88, "       Frag Limit");
        if (game_engine.fraglimit.value == 0)
            Menu.M_Print(160, 88, "none");
        else
            Menu.M_Print(160, 88, String.Format("{0} frags", (int)game_engine.fraglimit.value));

        Menu.M_Print(0, 96, "       Time Limit");
        if (game_engine.timelimit.value == 0)
            Menu.M_Print(160, 96, "none");
        else
            Menu.M_Print(160, 96, String.Format("{0} minutes", (int)game_engine.timelimit.value));

        Menu.M_Print(0, 112, "         Episode");
        //MED 01/06/97 added hipnotic episodes
        if (game_engine._GameKind == GameKind.Hipnotic)
            Menu.M_Print(160, 112, HipnoticEpisodes[_StartEpisode].description);
        //PGM 01/07/97 added rogue episodes
        else if (game_engine._GameKind == GameKind.Rogue)
            Menu.M_Print(160, 112, RogueEpisodes[_StartEpisode].description);
        else
            Menu.M_Print(160, 112, Episodes[_StartEpisode].description);

        Menu.M_Print(0, 120, "           Level");
        //MED 01/06/97 added hipnotic episodes
        if (game_engine._GameKind == GameKind.Hipnotic)
        {
            Menu.M_Print(160, 120, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].description);
            Menu.M_Print(160, 128, HipnoticLevels[HipnoticEpisodes[_StartEpisode].firstLevel + _StartLevel].name);
        }
        //PGM 01/07/97 added rogue episodes
        else if (game_engine._GameKind == GameKind.Rogue)
        {
            Menu.M_Print(160, 120, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].description);
            Menu.M_Print(160, 128, RogueLevels[RogueEpisodes[_StartEpisode].firstLevel + _StartLevel].name);
        }
        else
        {
            Menu.M_Print(160, 120, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].description);
            Menu.M_Print(160, 128, Levels[Episodes[_StartEpisode].firstLevel + _StartLevel].name);
        }

        // line cursor
        Menu.M_DrawCharacter(144, _CursorTable[_Cursor], 12 + ((int)(game_engine.realtime * 4) & 1));

        if (_ServerInfoMessage)
        {
            if ((game_engine.realtime - _ServerInfoMessageTime) < 5.0)
            {
                int x = (320 - 26 * 8) / 2;
                Menu.M_DrawTextBox(x, 138, 24, 4);
                x += 8;
                Menu.M_Print(x, 146, "  More than 4 players   ");
                Menu.M_Print(x, 154, " requires using command ");
                Menu.M_Print(x, 162, "line parameters; please ");
                Menu.M_Print(x, 170, "   see techinfo.txt.    ");
            }
            else
            {
                _ServerInfoMessage = false;
            }
        }
    }
}
class SearchMenu : MenuBase
{
    bool _SearchComplete;
    double _SearchCompleteTime;

    public override void Show()
    {
        base.Show();
        game_engine.SlistSilent = true;
        game_engine.SlistLocal = false;
        _SearchComplete = false;
        game_engine.NET_Slist_f();
    }

    public override void KeyEvent(int key)
    {
        // nothing to do
    }

    public override void Draw()
    {
        glpic_t p = game_engine.Draw_CachePic("gfx/p_multi.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);
        int x = (320 / 2) - ((12 * 8) / 2) + 4;
        Menu.M_DrawTextBox(x - 8, 32, 12, 1);
        Menu.M_Print(x, 40, "Searching...");

        if (game_engine.slistInProgress)
        {
            game_engine.NET_Poll();
            return;
        }

        if (!_SearchComplete)
        {
            _SearchComplete = true;
            _SearchCompleteTime = game_engine.realtime;
        }

        if (game_engine.hostCacheCount > 0)
        {
            MenuBase.ServerListMenu.Show();
            return;
        }

        Menu.M_PrintWhite((320 / 2) - ((22 * 8) / 2), 64, "No Quake servers found");
        if ((game_engine.realtime - _SearchCompleteTime) < 3.0)
            return;

        MenuBase.LanConfigMenu.Show();
    }
}
class ServerListMenu : MenuBase
{
    bool _Sorted;

    public override void Show()
    {
        base.Show();
        _Cursor = 0;
        Menu.ReturnOnError = false;
        Menu.ReturnReason = String.Empty;
        _Sorted = false;
    }

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                MenuBase.LanConfigMenu.Show();
                break;

            case q_shared.K_SPACE:
                MenuBase.SearchMenu.Show();
                break;

            case q_shared.K_UPARROW:
            case q_shared.K_LEFTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = game_engine.hostCacheCount - 1;
                break;

            case q_shared.K_DOWNARROW:
            case q_shared.K_RIGHTARROW:
                game_engine.S_LocalSound("misc/menu1.wav");
                _Cursor++;
                if (_Cursor >= game_engine.hostCacheCount)
                    _Cursor = 0;
                break;

            case q_shared.K_ENTER:
                game_engine.S_LocalSound("misc/menu2.wav");
                Menu.ReturnMenu = this;
                Menu.ReturnOnError = true;
                _Sorted = false;
                MenuBase.Hide();
                game_engine.Cbuf_AddText(String.Format("connect \"{0}\"\n", game_engine.hostcache[_Cursor].cname));
                break;

            default:
                break;
        }
    }

    public override void Draw()
    {
        if (!_Sorted)
        {
            if (game_engine.hostCacheCount > 1)
            {
                Comparison<hostcache_t> cmp = delegate(hostcache_t a, hostcache_t b)
                {
                    return String.Compare(a.cname, b.cname);
                };

                Array.Sort(game_engine.hostcache, cmp);
            }
            _Sorted = true;
        }

        glpic_t p = game_engine.Draw_CachePic("gfx/p_multi.lmp");
        Menu.DrawPic((320 - p.width) / 2, 4, p);
        for (int n = 0; n < game_engine.hostCacheCount; n++)
        {
            hostcache_t hc = game_engine.hostcache[n];
            string tmp;
            if (hc.maxusers > 0)
                tmp = String.Format("{0,-15} {1,-15} {2:D2}/{3:D2}\n", hc.name, hc.map, hc.users, hc.maxusers);
            else
                tmp = String.Format("{0,-15} {1,-15}\n", hc.name, hc.map);
            Menu.M_Print(16, 32 + 8 * n, tmp);
        }
        Menu.M_DrawCharacter(0, 32 + _Cursor * 8, 12 + ((int)(game_engine.realtime * 4) & 1));

        if (!String.IsNullOrEmpty(Menu.ReturnReason))
            Menu.M_PrintWhite(16, 148, Menu.ReturnReason);
    }
}
class VideoMenu : MenuBase
{
    int current_mode = game_engine.vid_modenum;
    int items = 3;

    public override void KeyEvent(int key)
    {
        switch (key)
        {
            case q_shared.K_ESCAPE:
                game_engine.S_LocalSound("misc/menu1.wav");
                MenuBase.OptionsMenu.Show();
                break;
            case q_shared.K_LEFTARROW:
                if (_Cursor == 0)
                {
                    current_mode--;
                    if (current_mode < 0)
                        current_mode = game_engine.vid_modes.Length - 1;
                }
                if (_Cursor == 1)
                {
                    game_engine.windowed = !game_engine.windowed;
                }
                break;
            case q_shared.K_RIGHTARROW:
                if (_Cursor == 0)
                {
                    current_mode++;
                    if (current_mode >= game_engine.vid_modes.Length)
                        current_mode = 0;
                }
                if (_Cursor == 1)
                {
                    game_engine.windowed = !game_engine.windowed;
                }
                break;

            // selection
            case q_shared.K_UPARROW:
                _Cursor--;
                if (_Cursor < 0)
                    _Cursor = items - 1;
                break;
            case q_shared.K_DOWNARROW:
                _Cursor++;
                if (_Cursor >= items)
                    _Cursor = 0;
                break;

            case q_shared.K_ENTER:
                if (_Cursor == 2)
                {
                    game_engine.VID_SetMode(current_mode, game_engine.host_basepal);
                }
                break;


            default:
                break;
        }
    }
    public override void Draw()
    {
        glpic_t p = game_engine.Draw_CachePic("gfx/vidmodes.lmp");

        // Selected item
        int f = (int)(game_engine.host_time * 10) % 6;
        Menu.DrawTransPic(54, 32 + (_Cursor * 8),  game_engine.Draw_CachePic(String.Format("gfx/menudot{0}.lmp", f + 1)));


        // Display selected mode
        mode_t c = game_engine.vid_modes[current_mode];
        string format = string.Format("{0}x{1}x{2}", c.width, c.height, c.bpp);
        Menu.M_Print(75, 32, format);


        // Fullscreen
        Menu.M_Print(75, 44, game_engine.windowed ? "False" : "True");

        // Set Mode
        Menu.M_Print(75, 56, "[Apply]");
    }
}