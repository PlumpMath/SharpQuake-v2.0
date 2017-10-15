using System;
using System.Collections.Generic;
using System.Text;

public static partial class game_engine
{
    public static CacheEntry _Head;
    public static int _Capacity;
    public static int _BytesAllocated;

    
    public static void Cache_Init(int capacity)
    {
        _Capacity = capacity;
        _BytesAllocated = 0;
        _Head = new CacheEntry(true);

        Cmd_AddCommand("flush", Cache_Flush);
    }
    public static object Cache_Check(cache_user_t c)
    {
        CacheEntry cs = (CacheEntry)c;

	    if (cs == null || cs.data == null)
		    return null;

        // move to head of LRU
        cs.Cache_UnlinkLRU();
	    cs.LRUInstertAfter(_Head);
	
	    return cs.data;
    }
    public static cache_user_t Cache_Alloc(int size, string name)
    {
	    if (size <= 0)
		    Sys_Error("Cache_Alloc: size {0}", size);

	    size = (size + 15) & ~15;

        CacheEntry entry = null;

        // find memory for it	
	    while (true)
	    {
		    entry = Cache_TryAlloc(size);
		    if (entry != null)
			    break;
	
	        // free the least recently used cahedat
		    if (_Head.LruPrev == _Head)// cache_head.lru_prev == &cache_head)
			    Sys_Error("Cache_Alloc: out of memory");
													    // not enough memory at all
		    Cache_Free(_Head.LruPrev);
	    }

        Cache_Check(entry);
	    return entry;
    }
    static void Cache_Flush()
    {
	    while (_Head.Next != _Head)
		    Cache_Free(_Head.Next); // reclaim the space
    }
    static void Cache_Free(cache_user_t c)
    {
        if (c.data == null)
            Sys_Error("Cache_Free: not allocated");

	    CacheEntry entry = (CacheEntry)c;
        entry.Remove();
    }
    static CacheEntry Cache_TryAlloc(int size)
    {
        if (_BytesAllocated + size > _Capacity)
            return null;

        CacheEntry result = new CacheEntry(size);
        _Head.InsertBefore(result);
        result.LRUInstertAfter(_Head);
        return result;
    }
    public static void Cache_Report()
    {
        Con_DPrintf("{0,4:F1} megabyte data cache, used {1,4:F1} megabyte\n",
            _Capacity / (float)(1024 * 1024), _BytesAllocated / (float)(1024 * 1024));
    }
}