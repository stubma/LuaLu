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
#include "unity_log.h"
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

TOLUA_API void tolua_stack_dump(lua_State* L, const char* label)
{
    int i;
    int top = lua_gettop(L);
    char buf[512];
    sprintf(buf, "Total [%d] in lua stack: %s", top, label != 0 ? label : "");
    unity_log(buf);
    for (i = -1; i >= -top; i--)
    {
        int t = lua_type(L, i);
        switch (t)
        {
            case LUA_TSTRING:
                sprintf(buf, "  [%02d] string %s", i, lua_tostring(L, i));
                unity_log(buf);
                break;
            case LUA_TBOOLEAN:
                sprintf(buf, "  [%02d] boolean %s", i, lua_toboolean(L, i) ? "true" : "false");
                unity_log(buf);
                break;
            case LUA_TNUMBER:
                sprintf(buf, "  [%02d] number %g", i, lua_tonumber(L, i));
                unity_log(buf);
                break;
            default:
                sprintf(buf, "  [%02d] %s", i, lua_typename(L, t));
                unity_log(buf);
                break;
        }
    }
}