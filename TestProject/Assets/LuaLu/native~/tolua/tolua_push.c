/* tolua: functions to push C values.
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

#include "tolua++.h"
#include "lauxlib.h"
#include "unity_log.h"
#include <stdlib.h>

TOLUA_API void tolua_pushvalue (lua_State* L, int lo)
{
    lua_pushvalue(L,lo);
}

TOLUA_API void tolua_pushboolean (lua_State* L, int value)
{
    lua_pushboolean(L,value);
}

TOLUA_API void tolua_pushnumber (lua_State* L, lua_Number value)
{
    lua_pushnumber(L,value);
}

TOLUA_API void tolua_pushstring (lua_State* L, const char* value)
{
    if (value == NULL)
        lua_pushnil(L);
    else
        lua_pushstring(L,value);
}

TOLUA_API void tolua_pushuserdata (lua_State* L, void* value)
{
    if (value == NULL)
        lua_pushnil(L);
    else
        lua_pushlightuserdata(L,value);
}

TOLUA_API void tolua_replaceref(lua_State* L, int oldRefId, int newRefId) {
    // if old is zero, return
    if(oldRefId == 0) {
        return;
    }
    
    // get type
    const char* type = NULL;
    lua_pushstring(L, TOLUA_REFID_TYPE_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: refid_type */
    lua_pushinteger(L, oldRefId);                                  /* stack: refid_type refid */
    lua_rawget(L, -2); // refid_type type
    if(!lua_isnil(L, -1)) {
        type = lua_tostring(L, -1);
        lua_pushinteger(L, oldRefId);   // refid_type type refid
        lua_pushnil(L); // refid_type type refid nil
        lua_rawset(L, -4); // delete old ref id from type mapping, stack: refid_type type
        
        // set new refid to type
        lua_pushinteger(L, newRefId); // refid_type type new_refid
        lua_insert(L, -2); // refid_type new_refid type
        lua_rawset(L, -3); // refid_type
    } else {
        lua_pop(L, 1); // refid_type
    }
    lua_pop(L, 1); // empty

    // get metatable
    luaL_getmetatable(L, type);                                 /* stack: mt */
    if (lua_isnil(L, -1)) { /* NOT FOUND metatable */
        lua_pop(L, 1);
        return;
    }
    
    // get ubox, or get global ubox if not found in metatable
    lua_pushstring(L,"tolua_ubox");
    lua_rawget(L,-2);                                           /* stack: mt ubox */
    if (lua_isnil(L, -1)) {
        lua_pop(L, 1);
        lua_pushstring(L, "tolua_ubox");
        lua_rawget(L, LUA_REGISTRYINDEX);
    }
    
    // refid => ud
    lua_pushinteger(L, oldRefId);                             /* stack: mt ubox key<refid> */
    lua_rawget(L,-2);                                           /* stack: mt ubox ud */
    if (!lua_isnil(L,-1)) {
        tolua_unregister_gc(L, -1);
        *(int*)lua_touserdata(L, -1) = newRefId; // set new ref id
        tolua_register_gc(L, -1);
        
        // remove old
        lua_pushinteger(L, oldRefId); // mt ubox ud old_refid
        lua_pushnil(L); // mt ubox ud old_refid nil
        lua_rawset(L, -4); // mt ubox ud
        
        // add new
        lua_pushinteger(L, newRefId); // mt ubox ud new_refid
        lua_pushvalue(L, -2); // mt ubox ud new_refid ud
        lua_rawset(L, -4); // mt ubox ud
        
        // add/remove root
        tolua_add_value_to_root(L, newRefId); // mt ubox
        tolua_remove_value_from_root(L, oldRefId);
        lua_pop(L, 2); // empty
    } else {
        lua_pop(L, 3); // empty
    }
}

TOLUA_API void tolua_pushusertype (lua_State* L, int refid, const char* type, int addToRoot)
{
    if (refid == 0)
        lua_pushnil(L);
    else
    {
        luaL_getmetatable(L, type);                                 /* stack: mt */
        if (lua_isnil(L, -1)) { /* NOT FOUND metatable */
            lua_pop(L, 1);
            return;
        }
        
        // refid => type
        lua_pushstring(L, TOLUA_REFID_TYPE_MAPPING);
        lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: refid_type */
        lua_pushinteger(L, refid);                                  /* stack: refid_type refid */
        lua_pushstring(L, type);                     /* stack: refid_type refid type */
        lua_rawset(L, -3);                /* refid_type[refid] = type, stack: refid_type */
        lua_pop(L, 1);
        
        // get ubox, or get global ubox if not found in metatable
        lua_pushstring(L,"tolua_ubox");
        lua_rawget(L,-2);                                           /* stack: mt ubox */
        if (lua_isnil(L, -1)) {
            lua_pop(L, 1);
            lua_pushstring(L, "tolua_ubox");
            lua_rawget(L, LUA_REGISTRYINDEX);
        }
        
        // refid => ud
        lua_pushinteger(L,refid);                             /* stack: mt ubox key<refid> */
        lua_rawget(L,-2);                                           /* stack: mt ubox ubox[refid] */
        
        // if ud is not here, create new
        // if ud is here, check if need cast to specific type
        if (lua_isnil(L,-1))
        {
            lua_pop(L,1);                                           /* stack: mt ubox */
            lua_pushinteger(L,refid);
            *(int*)lua_newuserdata(L,sizeof(int)) = refid;     /* stack: mt ubox value newud */
            lua_pushvalue(L,-1);                                    /* stack: mt ubox value newud newud */
            lua_insert(L,-4);                                       /* stack: mt newud ubox value newud */
            lua_rawset(L,-3);                  /* ubox[value] = newud, stack: mt newud ubox */
            lua_pop(L,1);                                           /* stack: mt newud */
            /*luaL_getmetatable(L,type);*/
            lua_pushvalue(L, -2);                                   /* stack: mt newud mt */
            lua_setmetatable(L,-2);                      /* update mt, stack: mt newud */
            
#ifdef LUA_VERSION_NUM
            lua_pushvalue(L, TOLUA_NOPEER);             /* stack: mt newud peer */
            lua_setfenv(L, -2);                         /* stack: mt newud */
#endif
        }
        else
        {
            /* check the need of updating the metatable to a more specialized class */
            lua_insert(L,-2);                                       /* stack: mt ubox[u] ubox */
            lua_pop(L,1);                                           /* stack: mt ubox[u] */
            lua_pushstring(L,"tolua_super");
            lua_rawget(L,LUA_REGISTRYINDEX);                        /* stack: mt ubox[u] super */
            lua_getmetatable(L,-2);                                 /* stack: mt ubox[u] super mt */
            lua_rawget(L,-2);                                       /* stack: mt ubox[u] super super[mt] */
            if (lua_istable(L,-1))
            {
                lua_pushstring(L,type);                             /* stack: mt ubox[u] super super[mt] type */
                lua_rawget(L,-2);                                   /* stack: mt ubox[u] super super[mt] flag */
                if (lua_toboolean(L,-1) == 1)                       /* if true */
                {
                    lua_pop(L,3);                                   /* mt ubox[u]*/
                    lua_remove(L, -2);
                    return;
                }
            }
            /* type represents a more specilized type */
            /*luaL_getmetatable(L,type);             // stack: mt ubox[u] super super[mt] flag mt */
            lua_pushvalue(L, -5);                    /* stack: mt ubox[u] super super[mt] flag mt */
            lua_setmetatable(L,-5);                /* stack: mt ubox[u] super super[mt] flag */
            lua_pop(L,3);                          /* stack: mt ubox[u] */
        }
        lua_remove(L, -2);    /* stack: ubox[u]*/
        
        if (0 != addToRoot)
        {
            lua_pushvalue(L, -1);
            tolua_add_value_to_root(L, refid);
        }
        
        // register gc for this instance
        tolua_register_gc(L,lua_gettop(L));
    }
}

TOLUA_API void tolua_add_value_to_root(lua_State* L, int refid)
{
    
    lua_pushstring(L, TOLUA_VALUE_ROOT);
    lua_rawget(L, LUA_REGISTRYINDEX);                               /* stack: ud root */
    lua_insert(L, -2);                                              /* stack: root ud */
    lua_pushinteger(L, refid);                                  /* stack: root ud refid */
    lua_insert(L, -2);                                              /* stack: root refid ud */
    lua_rawset(L, -3);                                              /* root[ptr] = value, stack: root */
    lua_pop(L, 1);                                                  /* stack: - */
}


TOLUA_API void tolua_remove_value_from_root (lua_State* L, int refid)
{
    lua_pushstring(L, TOLUA_VALUE_ROOT);
    lua_rawget(L, LUA_REGISTRYINDEX);                               /* stack: root */
    lua_pushinteger(L, refid);                                  /* stack: root refid */
    
    lua_pushnil(L);                                                 /* stack: root ptr nil */
    lua_rawset(L, -3);                                              /* root[ptr] = nil, stack: root */
    lua_pop(L, 1);
}