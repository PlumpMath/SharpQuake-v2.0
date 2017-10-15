using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL;

public static partial class game_engine
{
    public static int[][] _ScrapAllocated;
    public static byte[][] _ScrapTexels;
    public static bool scrap_dirty;
    public static int scrap_texnum;
    public static int scrap_uploads;

    public static readonly glmode_t[] _Modes = new glmode_t[]
    {
	    new glmode_t("GL_NEAREST", TextureMinFilter.Nearest, TextureMagFilter.Nearest),
	    new glmode_t("GL_LINEAR", TextureMinFilter.Linear, TextureMagFilter.Linear),
	    new glmode_t("GL_NEAREST_MIPMAP_NEAREST", TextureMinFilter.NearestMipmapNearest, TextureMagFilter.Nearest),
	    new glmode_t("GL_LINEAR_MIPMAP_NEAREST", TextureMinFilter.LinearMipmapNearest, TextureMagFilter.Linear),
	    new glmode_t("GL_NEAREST_MIPMAP_LINEAR", TextureMinFilter.NearestMipmapLinear, TextureMagFilter.Nearest),
	    new glmode_t("GL_LINEAR_MIPMAP_LINEAR", TextureMinFilter.LinearMipmapLinear, TextureMagFilter.Linear)
    };

    public static readonly gltexture_t[] gltextures = new gltexture_t[q_shared.MAX_GLTEXTURES];
    public static int numgltextures;
    public static int texels;
    public static int pic_texels;
    public static int pic_count;


    public static cvar_t gl_nobind;
    public static cvar_t gl_max_size;
    public static cvar_t gl_picmip;

    public static glpic_t draw_disc;
    public static glpic_t draw_backtile;
    public static glpic_t conback;

    public static int char_texture;
    public static int translate_texture;
    public static int texture_extension_number = 1;
    public static int currenttexture = -1;
    public static MTexTarget oldtarget = MTexTarget.TEXTURE0_SGIS;
    public static int[] cnttextures = new int[2] { -1, -1 };
    public static TextureMinFilter gl_filter_min = TextureMinFilter.LinearMipmapNearest;
    public static TextureMagFilter gl_filter_max = TextureMagFilter.Linear;

    public static PixelFormat gl_lightmap_format = PixelFormat.Rgba;
    public static PixelInternalFormat gl_solid_format = PixelInternalFormat.Three;
    public static PixelInternalFormat gl_alpha_format = PixelInternalFormat.Four;

    public static readonly cachepic_t[] menu_cachepics = new cachepic_t[q_shared.MAX_CACHED_PICS];
    public static int menu_numcachepics;
    public static readonly byte[] menuplyr_pixels = new byte[4096];


 
    public static void Draw_Init()
    {
        _ScrapAllocated = new int[q_shared.MAX_SCRAPS][];
        for (int i = 0; i < _ScrapAllocated.GetLength(0); i++)
            _ScrapAllocated[i] = new int[q_shared.BLOCK_WIDTH];
        
        _ScrapTexels = new byte[q_shared.MAX_SCRAPS][];
        for (int i = 0; i < _ScrapTexels.GetLength(0); i++)
            _ScrapTexels[i] = new byte[q_shared.BLOCK_WIDTH * q_shared.BLOCK_HEIGHT * 4];

        for (int i = 0; i < menu_cachepics.Length; i++)
            menu_cachepics[i] = new cachepic_t();
        
        gl_nobind = new cvar_t("gl_nobind", "0");
        gl_max_size = new cvar_t("gl_max_size", "1024");
        gl_picmip = new cvar_t("gl_picmip", "0");

	    // 3dfx can only handle 256 wide textures
        string renderer = GL.GetString(StringName.Renderer);
        if (renderer.Contains("3dfx") || renderer.Contains("Glide"))
            Cvar.Cvar_Set("gl_max_size", "256");

	    Cmd_AddCommand("gl_texturemode", Draw_TextureMode_f);

	    // load the console background and the charset
	    // by hand, because we need to write the version
	    // string into the background before turning
	    // it into a texture
	    int offset = game_engine.W_GetLumpName("conchars");
        byte[] draw_chars = game_engine.wad_base; // draw_chars
        for (int i = 0; i < 256 * 64; i++)
        {
		    if (draw_chars[offset + i] == 0)
			    draw_chars[offset + i] = 255;	// proper transparent color
        }

	    // now turn them into textures
        char_texture = GL_LoadTexture("charset", 128, 128, new ByteArraySegment(draw_chars, offset), false, true);

        byte[] buf = COM_LoadFile("gfx/conback.lmp");
        if (buf == null)
		    Sys_Error("Couldn't load gfx/conback.lmp");

	    dqpicheader_t cbHeader = BytesToStructure<dqpicheader_t>(buf, 0);
	    game_engine.SwapPic(cbHeader);

	    // hack the version number directly into the pic
        string ver = String.Format("(c# {0,7:F2}) {1,7:F2}", (float)q_shared.CSQUAKE_VERSION, (float)q_shared.VERSION);
        int offset2 = Marshal.SizeOf(typeof(dqpicheader_t)) + 320 * 186 + 320 - 11 - 8 * ver.Length;
	    int y = ver.Length;
        for (int x = 0; x < y; x++)
            CharToConback(ver[x], new ByteArraySegment(buf, offset2 + (x << 3)), new ByteArraySegment(draw_chars, offset));

        conback = new glpic_t();
        conback.width = cbHeader.width;
        conback.height = cbHeader.height;
        int ncdataIndex = Marshal.SizeOf(typeof(dqpicheader_t)); // cb->data;

        SetTextureFilters(TextureMinFilter.Nearest, TextureMagFilter.Nearest);

        conback.texnum = GL_LoadTexture("conback", conback.width, conback.height, new ByteArraySegment(buf, ncdataIndex), false, false);
	    conback.width = vid.width;
        conback.height = vid.height;

	    // save a texture slot for translated picture
	    translate_texture = texture_extension_number++;

	    // save slots for scraps
	    scrap_texnum = texture_extension_number;
	    texture_extension_number += q_shared.MAX_SCRAPS;

	    //
	    // get the other pics we need
	    //
	    draw_disc = Draw_PicFromWad ("disc");
	    draw_backtile = Draw_PicFromWad ("backtile");
    }
    public static void SetTextureFilters(TextureMinFilter min, TextureMagFilter mag)
    {
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)min);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)mag);
    }    
    public static int GenerateTextureNumber()
    {
        return texture_extension_number++;
    }
    public static int GenerateTextureNumberRange(int count)
    {
        int result = texture_extension_number;
        texture_extension_number += count;
        return result;
    }    
    public static void Draw_Pic(int x, int y, glpic_t pic)
    {
        if (scrap_dirty)
            UploadScrap();

        GL.Color4(1f, 1f, 1f, 1f);
        GL_Bind(pic.texnum);
        GL.Begin(BeginMode.Quads);
        GL.TexCoord2(pic.sl, pic.tl);
        GL.Vertex2(x, y);
        GL.TexCoord2(pic.sh, pic.tl);
        GL.Vertex2(x + pic.width, y);
        GL.TexCoord2(pic.sh, pic.th);
        GL.Vertex2(x + pic.width, y + pic.height);
        GL.TexCoord2(pic.sl, pic.th);
        GL.Vertex2(x, y + pic.height);
        GL.End();
    }    
    public static void Draw_BeginDisc()
    {
        if (draw_disc != null)
        {
            GL.DrawBuffer(DrawBufferMode.Front);
            Draw_Pic(vid.width - 24, 0, draw_disc);
            GL.DrawBuffer(DrawBufferMode.Back);
        }
    }
    public static void Draw_EndDisc()
    {
        // nothing to do?
    }
    public static void Draw_TileClear(int x, int y, int w, int h)
    {
        GL.Color3(1.0f, 1.0f, 1.0f);
        GL_Bind(draw_backtile.texnum); //GL_Bind (*(int *)draw_backtile->data);
        GL.Begin(BeginMode.Quads);
        GL.TexCoord2(x / 64.0f, y / 64.0f);
        GL.Vertex2(x, y);
        GL.TexCoord2((x + w) / 64.0f, y / 64.0f);
        GL.Vertex2(x + w, y);
        GL.TexCoord2((x + w) / 64.0f, (y + h) / 64.0f);
        GL.Vertex2(x + w, y + h);
        GL.TexCoord2(x / 64.0f, (y + h) / 64.0f);
        GL.Vertex2(x, y + h);
        GL.End();
    }
    public static glpic_t Draw_PicFromWad(string name)
    {
        int offset = game_engine.W_GetLumpName(name);
        IntPtr ptr = new IntPtr(game_engine._DataPtr.ToInt64() + offset);
        dqpicheader_t header = (dqpicheader_t)Marshal.PtrToStructure(ptr, typeof(dqpicheader_t));
        glpic_t gl = new glpic_t(); // (glpic_t)Marshal.PtrToStructure(ptr, typeof(glpic_t));
        gl.width = header.width;
        gl.height = header.height;
        offset += Marshal.SizeOf(typeof(dqpicheader_t));

        // load little ones into the scrap
        if (gl.width < 64 && gl.height < 64)
        {
            int x, y;
            int texnum = Scrap_AllocBlock(gl.width, gl.height, out x, out y);
            scrap_dirty = true;
            int k = 0;
            for (int i = 0; i < gl.height; i++)
                for (int j = 0; j < gl.width; j++, k++)
                    _ScrapTexels[texnum][(y + i) * q_shared.BLOCK_WIDTH + x + j] = game_engine.wad_base[offset + k];// p->data[k];
            texnum += scrap_texnum;
            gl.texnum = texnum;
            gl.sl = (float)((x + 0.01) / (float)q_shared.BLOCK_WIDTH);
            gl.sh = (float)((x + gl.width - 0.01) / (float)q_shared.BLOCK_WIDTH);
            gl.tl = (float)((y + 0.01) / (float)q_shared.BLOCK_WIDTH);
            gl.th = (float)((y + gl.height - 0.01) / (float)q_shared.BLOCK_WIDTH);

            pic_count++;
            pic_texels += gl.width * gl.height;
        }
        else
        {
            gl.texnum = GL_LoadPicTexture(gl, new ByteArraySegment(game_engine.wad_base, offset));
        }
        return gl;
    }
    public static void GL_Bind(int texnum)
    {
	    //if (_glNoBind.Value != 0)
		//    texnum = _CharTexture;
	    if (currenttexture == texnum)
		    return;
	    currenttexture = texnum;
        GL.BindTexture(TextureTarget.Texture2D, texnum);
    }
    public static void Draw_FadeScreen()
    {
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.Texture2D);
        GL.Color4(0, 0, 0, 0.8f);
        GL.Begin(BeginMode.Quads);

        GL.Vertex2(0f, 0f);
        GL.Vertex2(vid.width, 0f);
        GL.Vertex2((float)vid.width, (float)vid.height);
        GL.Vertex2(0f, vid.height);

        GL.End();
        GL.Color4(1f, 1f, 1f, 1f);
        GL.Enable(EnableCap.Texture2D);
        GL.Disable(EnableCap.Blend);

	    Sbar_Changed();
    }
    public static void Draw_TextureMode_f()
    {
        int i;
        if (cmd_argc == 1)
        {
            for (i = 0; i < 6; i++)
                if (gl_filter_min == _Modes[i].minimize)
                {
                    Con_Printf("{0}\n", _Modes[i].name);
                    return;
                }
            Con_Printf("current filter is unknown???\n");
            return;
        }

        for (i = 0; i < _Modes.Length; i++)
        {
            if (SameText(_Modes[i].name, Cmd_Argv(1)))
                break;
        }
        if (i == _Modes.Length)
        {
            Con_Printf("bad filter name!\n");
            return;
        }

        gl_filter_min = _Modes[i].minimize;
        gl_filter_max = _Modes[i].maximize;

        // change all the existing mipmap texture objects
        for (i = 0; i < numgltextures; i++)
        {
            gltexture_t glt = gltextures[i];
            if (glt.mipmap)
            {
                GL_Bind(glt.texnum);
                SetTextureFilters(gl_filter_min, gl_filter_max);
            }
        }
    }
    public static int GL_LoadTexture(string identifier, int width, int height, ByteArraySegment data, bool mipmap, bool alpha)
    {
	    // see if the texture is allready present
	    if (!String.IsNullOrEmpty(identifier))
	    {
            for (int i = 0; i < numgltextures; i++)
            {
                gltexture_t glt = gltextures[i];
                if (glt.identifier == identifier)
                {
                    if (width != glt.width || height != glt.height)
                        Sys_Error("GL_LoadTexture: cache mismatch!");
                    return glt.texnum;
                }
            }
	    }
        if (numgltextures == gltextures.Length)
            Sys_Error("GL_LoadTexture: no more texture slots available!");
	        
        gltexture_t tex = new gltexture_t();
        gltextures[numgltextures] = tex;
        numgltextures++;

        tex.identifier = identifier;
        tex.texnum = texture_extension_number;
        tex.width = width;
        tex.height = height;
        tex.mipmap = mipmap;

	    GL_Bind(tex.texnum);

        GL_Upload8(data, width, height, mipmap, alpha);

	    texture_extension_number++;

        return tex.texnum;
    }
    public static int GL_LoadPicTexture(glpic_t pic, ByteArraySegment data)
    {
	    return GL_LoadTexture(String.Empty, pic.width, pic.height, data, false, true);
    }
    public static void CharToConback(int num, ByteArraySegment dest, ByteArraySegment drawChars)
    {
        int row = num >> 4;
        int col = num & 15;
        int destOffset = dest.StartIndex;
        int srcOffset = drawChars.StartIndex + (row << 10) + (col << 3);
	    //source = draw_chars + (row<<10) + (col<<3);
        int drawline = 8;

	    while (drawline-- > 0)
	    {
            for (int x = 0; x < 8; x++)
                if (drawChars.Data[srcOffset + x] != 255)
                    dest.Data[destOffset + x] = (byte)(0x60 + drawChars.Data[srcOffset + x]); // source[x];
            srcOffset += 128; // source += 128;
            destOffset += 320; // dest += 320;
	    }
    }
    public static void GL_Upload8(ByteArraySegment data, int width, int height, bool mipmap, bool alpha)
    {
        int s = width * height;
        uint[] trans = new uint[s];
        uint[] table = d_8to24table;
        byte[] data1 = data.Data;
        int offset = data.StartIndex;
            
        // if there are no transparent pixels, make it a 3 component
	    // texture even if it was specified as otherwise
	    if (alpha)
	    {
		    bool noalpha = true;
            for (int i = 0; i < s; i++, offset++)
            {
                byte p = data1[offset];
                if (p == 255)
                    noalpha = false;
                trans[i] = table[p];
            }

		    if (alpha && noalpha)
			    alpha = false;
	    }
	    else
	    {
		    if ((s & 3) != 0)
			    Sys_Error ("GL_Upload8: s&3");
                
            for (int i = 0; i < s; i += 4, offset += 4)
            {
                trans[i] = table[data1[offset]];
                trans[i + 1] = table[data1[offset + 1]];
                trans[i + 2] = table[data1[offset + 2]];
                trans[i + 3] = table[data1[offset + 3]];
            }
	    }

	    GL_Upload32(trans, width, height, mipmap, alpha);
    }
    public static void GL_Upload32(uint[] data, int width, int height, bool mipmap, bool alpha)
    {
        int scaled_width, scaled_height;

        for (scaled_width = 1; scaled_width < width; scaled_width <<= 1)
            ;
        for (scaled_height = 1; scaled_height < height; scaled_height <<= 1)
            ;

	    scaled_width >>= (int)gl_picmip.value;
	    scaled_height >>= (int)gl_picmip.value;

	    if (scaled_width > gl_max_size.value)
            scaled_width = (int)gl_max_size.value;
        if (scaled_height > gl_max_size.value)
            scaled_height = (int)gl_max_size.value;

	    PixelInternalFormat samples = alpha ? gl_alpha_format : gl_solid_format;
        uint[] scaled;
            
        texels += scaled_width * scaled_height;

	    if (scaled_width == width && scaled_height == height)
	    {
		    if (!mipmap)
		    {
                GCHandle h2 = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    GL.TexImage2D(TextureTarget.Texture2D, 0, samples, scaled_width, scaled_height, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, h2.AddrOfPinnedObject());
                }
                finally
                {
                    h2.Free();
                }
			    goto Done;
		    }
            scaled = new uint[scaled_width * scaled_height]; // uint[1024 * 512];
            data.CopyTo(scaled, 0);
	    }
	    else
		    GL_ResampleTexture (data, width, height, out scaled, scaled_width, scaled_height);

        GCHandle h = GCHandle.Alloc(scaled, GCHandleType.Pinned);
        try
        {
            IntPtr ptr = h.AddrOfPinnedObject();
            GL.TexImage2D(TextureTarget.Texture2D, 0, samples, scaled_width, scaled_height, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
            ErrorCode err = GL.GetError(); // debug
            if (mipmap)
            {
                int miplevel = 0;
                while (scaled_width > 1 || scaled_height > 1)
                {
                    GL_MipMap(scaled, scaled_width, scaled_height);
                    scaled_width >>= 1;
                    scaled_height >>= 1;
                    if (scaled_width < 1)
                        scaled_width = 1;
                    if (scaled_height < 1)
                        scaled_height = 1;
                    miplevel++;
                    GL.TexImage2D(TextureTarget.Texture2D, miplevel, samples, scaled_width, scaled_height, 0,
                        PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
                }
            }
        }
        finally
        {
            h.Free();
        }
        
        Done: ;

	    if (mipmap)
            SetTextureFilters(gl_filter_min, gl_filter_max);
	    else
            SetTextureFilters((TextureMinFilter)gl_filter_max, gl_filter_max);
    }
    public static void GL_ResampleTexture(uint[] src, int srcWidth, int srcHeight, out uint[] dest,  int destWidth, int destHeight)
    {
        dest = new uint[destWidth * destHeight];
        int fracstep = srcWidth * 0x10000 / destWidth;
        int destOffset = 0;
        for (int i = 0; i < destHeight; i++)
        {
            int srcOffset = srcWidth * (i * srcHeight / destHeight);
            int frac = fracstep >> 1;
            for (int j = 0; j < destWidth; j += 4)
            {
                dest[destOffset + j] = src[srcOffset + (frac >> 16)];
                frac += fracstep;
                dest[destOffset + j + 1] = src[srcOffset + (frac >> 16)];
                frac += fracstep;
                dest[destOffset + j + 2] = src[srcOffset + (frac >> 16)];
                frac += fracstep;
                dest[destOffset + j + 3] = src[srcOffset + (frac >> 16)];
                frac += fracstep;
            }
            destOffset += destWidth;
        }
    }
    public static void GL_MipMap(uint[] src, int width, int height)
    {
        Union4b p1 = Union4b.Empty, p2 = Union4b.Empty, p3 = Union4b.Empty, p4 = Union4b.Empty;

	    width >>= 1;
	    height >>= 1;
	        
        uint[] dest = src;
        int srcOffset = 0;
        int destOffset = 0;
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                p1.ui0 = src[srcOffset];
                int offset = srcOffset + 1;
                p2.ui0 = offset < src.Length ? src[offset] : p1.ui0;
                offset = srcOffset + (width << 1);
                p3.ui0 = offset < src.Length ? src[offset] : p1.ui0;
                offset = srcOffset + (width << 1) + 1;
                p4.ui0 = offset < src.Length ? src[offset] : p1.ui0;

                p1.b0 = (byte)((p1.b0 + p2.b0 + p3.b0 + p4.b0) >> 2);
                p1.b1 = (byte)((p1.b1 + p2.b1 + p3.b1 + p4.b1) >> 2);
                p1.b2 = (byte)((p1.b2 + p2.b2 + p3.b2 + p4.b2) >> 2);
                p1.b3 = (byte)((p1.b3 + p2.b3 + p3.b3 + p4.b3) >> 2);

                dest[destOffset] = p1.ui0;
                destOffset++;
                srcOffset += 2;
            }
            srcOffset += width << 1;
        }
    }
    public static void Draw_Character(int x, int y, int num)
    {
        if (num == 32)
            return;		// space

        num &= 255;

        if (y <= -8)
            return;			// totally off screen

        int row = num >> 4;
        int col = num & 15;

        float frow = row * 0.0625f;
        float fcol = col * 0.0625f;
        float size = 0.0625f;

        GL_Bind(char_texture);

        GL.Begin(BeginMode.Quads);
        GL.TexCoord2(fcol, frow);
        GL.Vertex2(x, y);
        GL.TexCoord2(fcol + size, frow);
        GL.Vertex2(x + 8, y);
        GL.TexCoord2(fcol + size, frow + size);
        GL.Vertex2(x + 8, y + 8);
        GL.TexCoord2(fcol, frow + size);
        GL.Vertex2(x, y + 8);
        GL.End();
    }
    public static void Draw_String(int x, int y, string str)
    {
        for (int i = 0; i < str.Length; i++, x += 8)
            Draw_Character(x, y, str[i]);
    }
    public static int Scrap_AllocBlock(int w, int h, out int x, out int y)
    {
        x = -1;
        y = -1;
        for (int texnum = 0; texnum < q_shared.MAX_SCRAPS; texnum++)
        {
            int best = q_shared.BLOCK_HEIGHT;

            for (int i = 0; i < q_shared.BLOCK_WIDTH - w; i++)
            {
                int best2 = 0, j;

                for (j = 0; j < w; j++)
                {
                    if (_ScrapAllocated[texnum][i + j] >= best)
                        break;
                    if (_ScrapAllocated[texnum][i + j] > best2)
                        best2 = _ScrapAllocated[texnum][i + j];
                }
                if (j == w)
                {
                    // this is a valid spot
                    x = i;
                    y = best = best2;
                }
            }

            if (best + h > q_shared.BLOCK_HEIGHT)
                continue;

            for (int i = 0; i < w; i++)
                _ScrapAllocated[texnum][x + i] = best + h;

            return texnum;
        }

        Sys_Error("Scrap_AllocBlock: full");
        return -1;
    }
    public static glpic_t Draw_CachePic(string path)
    {
        for (int i = 0; i < menu_numcachepics; i++)
        {
            cachepic_t p = menu_cachepics[i];
            if (p.name == path)// !strcmp(path, pic->name))
                return p.pic;
        }

	    if (menu_numcachepics == q_shared.MAX_CACHED_PICS)
		    Sys_Error("menu_numcachepics == MAX_CACHED_PICS");
            
        cachepic_t pic = menu_cachepics[menu_numcachepics];
	    menu_numcachepics++;
        pic.name = path;

        //
        // load the pic from disk
        //
        byte[] data = COM_LoadFile(path);
	    if (data == null)
		    Sys_Error ("Draw_CachePic: failed to load {0}", path);
        dqpicheader_t header = BytesToStructure<dqpicheader_t>(data, 0);
        game_engine.SwapPic(header);

        int headerSize = Marshal.SizeOf(typeof(dqpicheader_t));

	    // HACK HACK HACK --- we need to keep the bytes for
	    // the translatable player picture just for the menu
	    // configuration dialog
        if (path == "gfx/menuplyr.lmp")
        {
            Buffer.BlockCopy(data, headerSize, menuplyr_pixels, 0, header.width * header.height);
            //memcpy (menuplyr_pixels, dat->data, dat->width*dat->height);
        }

        glpic_t gl = new glpic_t();
	    gl.width = header.width;
	    gl.height = header.height;

	    //gl = (glpic_t *)pic->pic.data;
	    gl.texnum = GL_LoadPicTexture(gl, new ByteArraySegment(data, headerSize));
	    gl.sl = 0;
	    gl.sh = 1;
	    gl.tl = 0;
	    gl.th = 1;
        pic.pic = gl;

	    return gl;
    }
    public static void Draw_Fill(int x, int y, int w, int h, int c)
    {
        GL.Disable(EnableCap.Texture2D);

        byte[] pal = host_basepal;

        GL.Color3(pal[c * 3] / 255.0f, pal[c * 3 + 1] / 255.0f, pal[c * 3 + 2] / 255.0f);
        GL.Begin(BeginMode.Quads);
        GL.Vertex2(x, y);
        GL.Vertex2(x + w, y);
        GL.Vertex2(x + w, y + h);
        GL.Vertex2(x, y + h);
        GL.End();
        GL.Color3(1f, 1f, 1f);
        GL.Enable(EnableCap.Texture2D);
    }
    public static void Draw_TransPic(int x, int y, glpic_t pic)
    {
        if (x < 0 || (uint)(x + pic.width) > vid.width ||
            y < 0 || (uint)(y + pic.height) > vid.height)
        {
            Sys_Error("Draw_TransPic: bad coordinates");
        }

        Draw_Pic(x, y, pic);
    }
    public static void Draw_TransPicTranslate(int x, int y, glpic_t pic, byte[] translation)
    {
        GL_Bind(translate_texture);

        int c = pic.width * pic.height;
        int destOffset = 0;
        uint[] trans = new uint[64 * 64];

        for (int v = 0; v < 64; v++, destOffset += 64)
        {
            int srcOffset = ((v * pic.height) >> 6) * pic.width;
            for (int u = 0; u < 64; u++)
            {
                uint p = menuplyr_pixels[srcOffset + ((u * pic.width) >> 6)];
                if (p == 255)
                    trans[destOffset + u] = p;
                else
                    trans[destOffset + u] = d_8to24table[translation[p]];
            }
        }

        GCHandle handle = GCHandle.Alloc(trans, GCHandleType.Pinned);
        try
        {
            GL.TexImage2D(TextureTarget.Texture2D, 0, gl_alpha_format, 64, 64, 0,
                PixelFormat.Rgba, PixelType.UnsignedByte, handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }

        SetTextureFilters(TextureMinFilter.Linear, TextureMagFilter.Linear);

        GL.Color3(1f, 1, 1);
        GL.Begin(BeginMode.Quads);
        GL.TexCoord2(0f, 0);
        GL.Vertex2((float)x, y);
        GL.TexCoord2(1f, 0);
        GL.Vertex2((float)x + pic.width, y);
        GL.TexCoord2(1f, 1);
        GL.Vertex2((float)x + pic.width, y + pic.height);
        GL.TexCoord2(0f, 1);
        GL.Vertex2((float)x, y + pic.height);
        GL.End();
    }
    public static void Draw_ConsoleBackground(int lines)
    {
        int y = (vid.height * 3) >> 2;

        if (lines > y)
            Draw_Pic(0, lines - vid.height, conback);
        else
            Draw_AlphaPic(0, lines - vid.height, conback, (float)(1.2 * lines) / y);
    }
    public static void Draw_AlphaPic (int x, int y, glpic_t pic, float alpha)
    {
	    if (scrap_dirty)
            UploadScrap();
	        
        GL.Disable(EnableCap.AlphaTest);
        GL.Enable(EnableCap.Blend);
        GL.Color4(1f, 1f, 1f, alpha);
        GL_Bind(pic.texnum);
        GL.Begin(BeginMode.Quads);
        GL.TexCoord2(pic.sl, pic.tl);
        GL.Vertex2(x, y);
        GL.TexCoord2(pic.sh, pic.tl);
        GL.Vertex2(x + pic.width, y);
        GL.TexCoord2(pic.sh, pic.th);
        GL.Vertex2(x + pic.width, y + pic.height);
        GL.TexCoord2(pic.sl, pic.th);
        GL.Vertex2(x, y + pic.height);
        GL.End();
        GL.Color4(1f, 1f, 1f, 1f);
        GL.Enable(EnableCap.AlphaTest);
        GL.Disable(EnableCap.Blend);
    }
    public static void GL_SelectTexture(MTexTarget target)
    {
        if (!gl_mtexable)
            return;
            
        switch (target)
        {
            case MTexTarget.TEXTURE0_SGIS:
                GL.Arb.ActiveTexture(TextureUnit.Texture0);
                break;

            case MTexTarget.TEXTURE1_SGIS:
                GL.Arb.ActiveTexture(TextureUnit.Texture1);
                break;
                
            default:
                Sys_Error("GL_SelectTexture: Unknown target\n");
                break;
        }
            
        if (target == oldtarget)
            return;

        cnttextures[oldtarget - MTexTarget.TEXTURE0_SGIS] = currenttexture;
        currenttexture = cnttextures[target - MTexTarget.TEXTURE0_SGIS];
        oldtarget = target;
    }
    public static void UploadScrap()
    {
        scrap_uploads++;
        for (int i = 0; i < q_shared.MAX_SCRAPS; i++)
        {
            GL_Bind(scrap_texnum + i);
            GL_Upload8(new ByteArraySegment(_ScrapTexels[i]), q_shared.BLOCK_WIDTH, q_shared.BLOCK_HEIGHT, false, true);
        }
        scrap_dirty = false;
    }
}