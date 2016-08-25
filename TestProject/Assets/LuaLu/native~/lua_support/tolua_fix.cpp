
#include "tolua_fix.h"
#include <stdlib.h>
#include <map>
#include <typeinfo>
#include "unity_log.h"

#ifdef __cplusplus
extern "C" {
#endif
    
static int s_function_ref_id = 0;
static int s_table_ref_id = 0;
static std::map<unsigned int, char*> hash_type_mapping;
    
TOLUA_API void toluafix_open(lua_State* L)
{
    lua_pushstring(L, TOLUA_REFID_FUNCTION_MAPPING);
    lua_newtable(L);
    lua_rawset(L, LUA_REGISTRYINDEX);
    
    lua_pushstring(L, TOLUA_REFID_TABLE_MAPPING);
    lua_newtable(L);
    lua_rawset(L, LUA_REGISTRYINDEX);
}
    
TOLUA_API int toluafix_ref_table(lua_State* L, int lo, int def) {
    // convert lo to positive if it is negative
    if(lo < 0)
        lo = lua_gettop(L) + lo + 1;
    
    // function at lo
    if (!lua_istable(L, lo))
        return 0;
    
    // increase id
    s_table_ref_id++;
    
    // push to ref table
    lua_pushstring(L, TOLUA_REFID_TABLE_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: fun ... refid_t */
    lua_pushinteger(L, s_table_ref_id);                         /* stack: fun ... refid_t refid */
    lua_pushvalue(L, lo);                                       /* stack: fun ... refid_t refid table */

    // set
    lua_rawset(L, -3);                                          /* refid_t[refid] = table, stack: fun ... refid_ptr */
    lua_pop(L, 1);                                              /* stack: fun ... */
    
    return s_table_ref_id;
}

TOLUA_API void toluafix_remove_table_by_refid(lua_State* L, int refid) {
    lua_pushstring(L, TOLUA_REFID_TABLE_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: ... refid_t */
    lua_pushinteger(L, refid);                                  /* stack: ... refid_t refid */
    lua_pushnil(L);                                             /* stack: ... refid_t refid nil */
    lua_rawset(L, -3);                                          /* refid_t[refid] = table, stack: ... refid_ptr */
    lua_pop(L, 1);                                              /* stack: ... */
}
    
TOLUA_API void toluafix_get_table_by_refid(lua_State* L, int refid) {
    lua_pushstring(L, TOLUA_REFID_TABLE_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: ... refid_t */
    lua_pushinteger(L, refid);                                  /* stack: ... refid_t refid */
    lua_rawget(L, -2);                                          /* stack: ... refid_t table */
    lua_remove(L, -2);                                          /* stack: ... table */
}
    
TOLUA_API int toluafix_ref_function(lua_State* L, int lo, int def)
{
    // convert lo to positive if it is negative
    if(lo < 0)
        lo = lua_gettop(L) + lo + 1;
    
    // function at lo
    if (!lua_isfunction(L, lo)) return 0;
    
    s_function_ref_id++;
    
    lua_pushstring(L, TOLUA_REFID_FUNCTION_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: fun ... refid_fun */
    lua_pushinteger(L, s_function_ref_id);                      /* stack: fun ... refid_fun refid */
    lua_pushvalue(L, lo);                                       /* stack: fun ... refid_fun refid fun */
    
    lua_rawset(L, -3);                  /* refid_fun[refid] = fun, stack: fun ... refid_ptr */
    lua_pop(L, 1);                                              /* stack: fun ... */
    
    return s_function_ref_id;
    
    // lua_pushvalue(L, lo);                                           /* stack: ... func */
    // return luaL_ref(L, LUA_REGISTRYINDEX);
}

TOLUA_API void toluafix_get_function_by_refid(lua_State* L, int refid)
{
    lua_pushstring(L, TOLUA_REFID_FUNCTION_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: ... refid_fun */
    lua_pushinteger(L, refid);                                  /* stack: ... refid_fun refid */
    lua_rawget(L, -2);                                          /* stack: ... refid_fun fun */
    lua_remove(L, -2);                                          /* stack: ... fun */
}

TOLUA_API void toluafix_remove_function_by_refid(lua_State* L, int refid)
{
    lua_pushstring(L, TOLUA_REFID_FUNCTION_MAPPING);
    lua_rawget(L, LUA_REGISTRYINDEX);                           /* stack: ... refid_fun */
    lua_pushinteger(L, refid);                                  /* stack: ... refid_fun refid */
    lua_pushnil(L);                                             /* stack: ... refid_fun refid nil */
    lua_rawset(L, -3);                  /* refid_fun[refid] = fun, stack: ... refid_ptr */
    lua_pop(L, 1);                                              /* stack: ... */

    // luaL_unref(L, LUA_REGISTRYINDEX, refid);
}

#ifdef __cplusplus
}
#endif