/* tolua: event functions
** Support code for Lua bindings.
** Written by Waldemar Celes
** TeCGraf/PUC-Rio
** Apr 2003
** $Id: $
*/

/* This code is free software; you can redistribute it and/or modify it.
** The software provided hereunder is on an "as is" basis, and
** the author has no obligation to provide maintenance, support, updates,
** enhancements, or modifications.
*/

#include <stdio.h>
#include "unity_log.h"
#include "tolua++.h"

/* Store int peer
    * It stores, creating the corresponding table if needed,
    * the pair key/value in the corresponding peer table
*/
static void store_in_peer (lua_State* L, int lo)
{
#ifdef LUA_VERSION_NUM
    lua_getfenv(L, lo);
    if (lua_rawequal(L, -1, TOLUA_NOPEER)) {
        lua_pop(L, 1);
        lua_newtable(L);
        lua_pushvalue(L, -1);
        lua_setfenv(L, lo);    /* stack: k,v,table  */
    };
    lua_insert(L, -3);
    lua_settable(L, -3); /* on lua 5.1, we trade the "tolua_peers" lookup for a settable call */
    lua_pop(L, 1);
#else
    /* stack: key value (to be stored) */
    lua_pushstring(L,"tolua_peers");
    lua_rawget(L,LUA_REGISTRYINDEX);        /* stack: k v ubox */
    lua_pushvalue(L,lo);
    lua_rawget(L,-2);                       /* stack: k v ubox ubox[u] */
    if (!lua_istable(L,-1))
    {
        lua_pop(L,1);                          /* stack: k v ubox */
        lua_newtable(L);                       /* stack: k v ubox table */
        lua_pushvalue(L,1);
        lua_pushvalue(L,-2);                   /* stack: k v ubox table u table */
        lua_rawset(L,-4);                      /* stack: k v ubox ubox[u]=table */
    }
    lua_insert(L,-4);                       /* put table before k */
    lua_pop(L,1);                           /* pop ubox */
    lua_rawset(L,-3);                       /* store at table */
    lua_pop(L,1);                           /* pop ubox[u] */
#endif
}

/* Module index function
*/
static int module_index_event (lua_State* L)
{
    lua_pushstring(L,".get"); // t k .get
    lua_rawget(L,-3); // t k tget
    if (lua_istable(L,-1))
    {
        lua_pushvalue(L,2);  // t k tget k
        lua_rawget(L, -2); // t k tget vget
        lua_remove(L, -2); // t k vget
        if (lua_iscfunction(L,-1))
        {
            lua_call(L,0,1);  // t k v
            return 1;
        } else if (lua_istable(L,-1)) {
            return 1;
        }
    }

    /* call old index meta event */
    if (lua_getmetatable(L,1)) // t k mt
    {
        lua_pushstring(L,"__index"); // t k mt __index_key
        lua_rawget(L,-2); // t k mt __index
        if (lua_isfunction(L,-1))
        {
            lua_pushvalue(L, -2); // t k mt __index mt
            lua_pushvalue(L,2); // t k mt __index mt k
            lua_remove(L, -4); // t k __index mt k
            lua_call(L,2,1); // t k v
            return 1;
        }
        else if (lua_istable(L,-1))
        {
            lua_pushvalue(L,2); // t k mt __index k
            lua_gettable(L,-2); // t k mt v
            lua_remove(L, -2); // t k v
            return 1;
        }
    }
    lua_pushnil(L);
    return 1;
}

/* Module newindex function
*/
static int module_newindex_event (lua_State* L)
{
    lua_pushstring(L,".set");
    lua_rawget(L,-4);
    if (lua_istable(L,-1))
    {
        lua_pushvalue(L,2);  /* key */
        lua_rawget(L,-2);
        if (lua_iscfunction(L,-1))
        {
            lua_pushvalue(L,1); /* only to be compatible with non-static vars */
            lua_pushvalue(L,3); /* value */
            lua_call(L,2,0);
            return 0;
        }
    }
    /* call old newindex meta event */
    if (lua_getmetatable(L,1) && lua_getmetatable(L,-1))
    {
        lua_pushstring(L,"__newindex");
        lua_rawget(L,-2);
        if (lua_isfunction(L,-1))
        {
            lua_pushvalue(L,1);
            lua_pushvalue(L,2);
            lua_pushvalue(L,3);
            lua_call(L,3,0);
        }
    }
    lua_settop(L,3);
    lua_rawset(L,-3);
    return 0;
}

/* Class index function
    * If the object is a userdata (ie, an object), it searches the field in
    * the alternative table stored in the corresponding "ubox" table.
*/
static int class_index_event (lua_State* L)
{
    int t = lua_type(L,1);
    if (t == LUA_TUSERDATA)
    {
        /* Access alternative table */
#ifdef LUA_VERSION_NUM /* new macro on version 5.1 */
        lua_getfenv(L,1);
        if (!lua_rawequal(L, -1, TOLUA_NOPEER)) {
            lua_pushvalue(L, 2); /* key */
            lua_gettable(L, -2); /* on lua 5.1, we trade the "tolua_peers" lookup for a gettable call */
            if (!lua_isnil(L, -1))
                return 1;
        };
#else
        lua_pushstring(L,"tolua_peers");
        lua_rawget(L,LUA_REGISTRYINDEX);        /* stack: obj key ubox */
        lua_pushvalue(L,1);
        lua_rawget(L,-2);                       /* stack: obj key ubox ubox[u] */
        if (lua_istable(L,-1))
        {
            lua_pushvalue(L,2);  /* key */
            lua_rawget(L,-2);                      /* stack: obj key ubox ubox[u] value */
            if (!lua_isnil(L,-1))
                return 1;
        }
#endif
        lua_settop(L,2);                        /* stack: obj key */

        /* Try metatables */
        lua_pushvalue(L,1);                     /* stack: obj key obj */
        while (lua_getmetatable(L,-1))
        {   /* stack: obj key obj mt */
            lua_remove(L,-2);                      /* stack: obj key mt */
         
            // try to get it from metatable
            lua_pushvalue(L,2);                    /* stack: obj key mt key */
            lua_rawget(L,-2);                      /* stack: obj key mt value */
            if (!lua_isnil(L,-1)) {
                lua_remove(L, -2); // obj key value
                return 1;
            } else {
                lua_pop(L,1); // obj key mt
            }
            
            /* try C/C++ variable */
            lua_pushstring(L,".get");
            lua_rawget(L,-2);                      /* stack: obj key mt tget */
            if (lua_istable(L,-1))
            {
                lua_pushvalue(L,2); // obj key mt tget key
                lua_rawget(L,-2);                      /* stack: obj key mt tget vget */
                if (lua_iscfunction(L,-1))
                {
                    lua_pushvalue(L,1); // obj key mt tget vget obj
                    lua_pushvalue(L,2); // obj key mt tget vget obj key
                    lua_call(L,2,1); // obj key mt tget value
                    lua_insert(L, -3); // obj key value mt tget
                    lua_pop(L, 2); // obj key value
                    return 1;
                }
                else if (lua_istable(L,-1))
                {
                    /* deal with array: create table to be returned and cache it in peer table */
                    void* u = *((void**)lua_touserdata(L,1));
                    lua_newtable(L);                /* stack: obj key mt vget table */
                    lua_pushstring(L,".self");      // obj key mt vget table .self
                    lua_pushlightuserdata(L,u);     // obj key mt vget table .self ud
                    lua_rawset(L,-3);               /* obj key mt vget table */
                    lua_insert(L,-2);               /* obj key mt table vget */
                    lua_setmetatable(L,-2);         /* set stored vget as metatable, obj key mt table */
                    lua_pushvalue(L,-1);            /* stack: obj key met table table */
                    lua_pushvalue(L,2);             /* stack: obj key mt table table key */
                    lua_insert(L,-2);               /*  stack: obj key mt table key table */
                    store_in_peer(L,1);               /* stack: obj key mt table */
                    return 1;
                }
            }
            lua_settop(L,3);
        }
        lua_settop(L,2);
        lua_pushnil(L);
        return 1;
    } else if(t == LUA_TTABLE) {
        module_index_event(L);
        return 1;
    }
    lua_pushnil(L);
    return 1;
}

/* Newindex function
    * It first searches for a C/C++ varaible to be set.
    * Then, it either stores it in the alternative ubox table (in the case it is
    * an object) or in the own table (that represents the class or module).
*/
static int class_newindex_event (lua_State* L)
{
    int t = lua_type(L,1);
    if (t == LUA_TUSERDATA)
    {
        /* Try accessing a C/C++ variable to be set */
        lua_getmetatable(L,1);
        while (lua_istable(L,-1))                /* stack: t k v mt */
        {
            lua_pushstring(L,".set");
            lua_rawget(L,-2);                      /* stack: t k v mt tset */
            if (lua_istable(L,-1))
            {
                lua_pushvalue(L,2);
                lua_rawget(L,-2);                     /* stack: t k v mt tset func */
                if (lua_iscfunction(L,-1))
                {
                    lua_pushvalue(L,1); // t k v mt tset func t
                    lua_pushvalue(L,3); // t k v mt tset func t v
                    lua_call(L,2,0); // t k v mt tset
                    lua_pop(L, 2); // t k v
                    return 0;
                }
                lua_pop(L,1);                          /* stack: t k v mt tset */
            }
            lua_pop(L,1);                           /* stack: t k v mt */
            if (!lua_getmetatable(L,-1))            /* stack: t k v mt mt */
                lua_pushnil(L);
            lua_remove(L,-2);                       /* stack: t k v mt */
        }
        lua_settop(L,3);                          /* stack: t k v */

        /* then, store as a new field */
        store_in_peer(L,1);
    }
    else if (t== LUA_TTABLE)
    {
        module_newindex_event(L);
    }
    return 0;
}

/*
static int class_gc_event (lua_State* L)
{
    void* u = *((void**)lua_touserdata(L,1));
    fprintf(stderr, "collecting: looking at %p\n", u);
    lua_pushstring(L,"tolua_gc");
    lua_rawget(L,LUA_REGISTRYINDEX);
    lua_pushlightuserdata(L,u);
    lua_rawget(L,-2);
    if (lua_isfunction(L,-1))
    {
        lua_pushvalue(L,1);
        lua_call(L,1,0);
         lua_pushlightuserdata(L,u);
        lua_pushnil(L);
        lua_rawset(L,-3);
    }
    lua_pop(L,2);
    return 0;
}
*/

TOLUA_API int class_gc_event (lua_State* L)
{
    // get ref id
    int refid = *(int*)lua_touserdata(L,1);
    
    // get metatable of type and super type
    lua_pushvalue(L, lua_upvalueindex(1)); // ud tolua_gc
    lua_pushinteger(L, refid); // ud tolua_gc refid
    lua_rawget(L,-2);            // ud tolua_gc mt
    lua_getmetatable(L,1);       // ud tolua_gc mt mt

    int top = lua_gettop(L);
    if (tolua_fast_isa(L,top,top-1, lua_upvalueindex(2))) /* make sure we collect correct type */
    {
        // remove refid type mapping
        lua_pushstring(L, TOLUA_REFID_TYPE_MAPPING);
        lua_rawget(L, LUA_REGISTRYINDEX);                               /* stack: refid_type */
        lua_pushinteger(L, refid);                                      /* stack: refid_type refid */
        lua_rawget(L, -2);                                              /* stack: refid_type type */
        if (lua_isnil(L, -1)) {
            lua_pop(L, 2);
            char buf[256];
            sprintf(buf, "[LUA ERROR] remove Object with NULL type, refid: %d", refid);
            unity_log(buf);
        } else {
            const char* type = lua_tostring(L, -1);
            lua_pop(L, 1);                                                  /* stack: refid_type */
            lua_pushinteger(L, refid);     // refid_type refid
            lua_pushnil(L); /* stack: refid_type refid nil */
            lua_rawset(L, -3); // refid_type
            lua_pop(L, 1);
            
            // get ubox
            luaL_getmetatable(L, type);                                     /* stack: mt */
            lua_pushstring(L, "tolua_ubox");                                /* stack: mt key */
            lua_rawget(L, -2);                                              /* stack: mt ubox */
            if (lua_isnil(L, -1)) {
                // use global ubox
                lua_pop(L, 1);                                              /* stack: mt */
                lua_pushstring(L, "tolua_ubox");                            /* stack: mt key */
                lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: mt ubox */
            }
            
            // remove ud from ubox
            lua_pushinteger(L, refid);                                  /* stack: mt ubox refid */
            lua_pushnil(L); // mt ubox refid nil
            lua_rawset(L, -3); // stack: mt ubox
            lua_pop(L, 2);
        }
        
        /*fprintf(stderr, "Found type!\n");*/
        /* get gc function */
        lua_pushliteral(L,".collector"); // ud tolua_gc ptr mt mt .collector
        lua_rawget(L,-2);           // ud tolua_gc ptr mt mt collector
        if (lua_isfunction(L,-1)) {
            /*fprintf(stderr, "Found .collector!\n");*/
        } else {
            lua_pop(L,1); // ud tolua_gc ptr mt
            /*fprintf(stderr, "Using default cleanup\n");*/
            lua_pushcfunction(L,tolua_default_collect); // ud tolua_gc ptr mt collector(default)
        }

        lua_pushvalue(L,1);         // ud tolua_gc refid mt collector ud
        lua_call(L,1,0); // collector executed, ud tolua_gc refid mt

        lua_pushinteger(L, refid); // ud tolua_gc refid mt refid
        lua_pushnil(L);             // ud tolua_gc refid mt refid nil
        lua_rawset(L,-5);           // ud tolua_gc(ptr->nil) refid mt
    }
    lua_pop(L,3); // ud
    return 0;
}


/* Register module events
    * It expects the metatable on the top of the stack
*/
TOLUA_API void tolua_moduleevents (lua_State* L)
{
    lua_pushstring(L,"__index");
    lua_pushcfunction(L,module_index_event);
    lua_rawset(L,-3);
    lua_pushstring(L,"__newindex");
    lua_pushcfunction(L,module_newindex_event);
    lua_rawset(L,-3);
}

/* Check if the object on the top has a module metatable
*/
TOLUA_API int tolua_ismodulemetatable (lua_State* L)
{
    int r = 0;
    if (lua_getmetatable(L,-1))
    {
        lua_pushstring(L,"__index");
        lua_rawget(L,-2);
        r = (lua_tocfunction(L,-1) == module_index_event);
        lua_pop(L,2);
    }
    return r;
}

/* Register class events
    * It expects the metatable on the top of the stack
*/
TOLUA_API void tolua_classevents (lua_State* L)
{
    lua_pushstring(L,"__index");
    lua_pushcfunction(L,class_index_event);
    lua_rawset(L,-3);
    lua_pushstring(L,"__newindex");
    lua_pushcfunction(L,class_newindex_event);
    lua_rawset(L,-3);

    lua_pushstring(L,"__gc");
    lua_pushstring(L, "tolua_gc_event");
    lua_rawget(L, LUA_REGISTRYINDEX);
    /*lua_pushcfunction(L,class_gc_event);*/
    lua_rawset(L,-3);
}

