/* tolua: funcitons to convert to C types
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

#include <string.h>
#include <stdlib.h>

TOLUA_API lua_Number tolua_tonumber (lua_State* L, int narg, lua_Number def)
{
    return lua_gettop(L)<abs(narg) ? def : lua_tonumber(L,narg);
}

TOLUA_API lua_Integer tolua_tointeger (lua_State* L, int narg, lua_Integer def)
{
    return lua_gettop(L)<abs(narg) ? def : lua_tointeger(L,narg);
}

TOLUA_API const char* tolua_tostring (lua_State* L, int narg, const char* def)
{
    return lua_gettop(L)<abs(narg) ? def : lua_tostring(L,narg);
}

extern int push_table_instance(lua_State* L, int lo);

TOLUA_API int tolua_tousertype (lua_State* L, int narg)
{
    if (lua_gettop(L)<abs(narg))
        return 0;
    else
    {
        void* u;
        if (!lua_isuserdata(L, narg)) {
            if (!push_table_instance(L, narg)) return 0;
        }
        u = lua_touserdata(L,narg);
        return *(int*)u;
    }
}

TOLUA_API int tolua_tovalue (lua_State* L, int narg, int def)
{
    return lua_gettop(L)<abs(narg) ? def : narg;
}

TOLUA_API int tolua_toboolean (lua_State* L, int narg, int def)
{
    return lua_gettop(L)<abs(narg) ?  def : lua_toboolean(L,narg);
}

TOLUA_API lua_Number tolua_tofieldnumber (lua_State* L, int lo, int index, lua_Number def)
{
    double v;
    lua_pushnumber(L,index);
    lua_gettable(L,lo);
    v = lua_isnil(L,-1) ? def : lua_tonumber(L,-1);
    lua_pop(L,1);
    return v;
}

TOLUA_API const char* tolua_tofieldstring
(lua_State* L, int lo, int index, const char* def)
{
    const char* v;
    lua_pushnumber(L,index);
    lua_gettable(L,lo);
    v = lua_isnil(L,-1) ? def : lua_tostring(L,-1);
    lua_pop(L,1);
    return v;
}

TOLUA_API void* tolua_tofielduserdata (lua_State* L, int lo, int index, void* def)
{
    void* v;
    lua_pushnumber(L,index);
    lua_gettable(L,lo);
    v = lua_isnil(L,-1) ? def : lua_touserdata(L,-1);
    lua_pop(L,1);
    return v;
}

TOLUA_API void* tolua_tofieldusertype (lua_State* L, int lo, int index, void* def)
{
    void* v;
    lua_pushnumber(L,index);
    lua_gettable(L,lo);
    v = lua_isnil(L,-1) ? def : (*(void **)(lua_touserdata(L, -1))); /* lua_unboxpointer(L,-1); */
    lua_pop(L,1);
    return v;
}

TOLUA_API int tolua_tofieldvalue (lua_State* L, int lo, int index, int def)
{
    int v;
    lua_pushnumber(L,index);
    lua_gettable(L,lo);
    v = lua_isnil(L,-1) ? def : lo;
    lua_pop(L,1);
    return v;
}

TOLUA_API int tolua_getfieldboolean (lua_State* L, int lo, int index, int def)
{
    int v;
    lua_pushnumber(L,index);
    lua_gettable(L,lo);
    v = lua_isnil(L,-1) ? 0 : lua_toboolean(L,-1);
    lua_pop(L,1);
    return v;
}

TOLUA_API void tolua_stack_dump(lua_State* L, const char* label)
{
    int i;
    int top = lua_gettop(L);
    printf("Total [%d] in lua stack: %s\n", top, label != 0 ? label : "");
    for (i = -1; i >= -top; i--)
    {
        int t = lua_type(L, i);
        switch (t)
        {
            case LUA_TSTRING:
                printf("  [%02d] string %s\n", i, lua_tostring(L, i));
                break;
            case LUA_TBOOLEAN:
                printf("  [%02d] boolean %s\n", i, lua_toboolean(L, i) ? "true" : "false");
                break;
            case LUA_TNUMBER:
                printf("  [%02d] number %g\n", i, lua_tonumber(L, i));
                break;
            default:
                printf("  [%02d] %s\n", i, lua_typename(L, t));
        }
    }
    printf("\n");
}