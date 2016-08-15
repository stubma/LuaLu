namespace LuaInterface {
	using System;
	using System.Runtime.InteropServices;
	using System.Reflection;
	using System.Collections;
	using System.Text;
	using System.Security;

	[AttributeUsage(AttributeTargets.Method)]
	public sealed class MonoPInvokeCallbackAttribute : Attribute {
		public MonoPInvokeCallbackAttribute(Type t) {
		}
	}

	public enum LuaTypes {
		LUA_TNONE = -1,
		LUA_TNIL = 0,
		LUA_TBOOLEAN = 1,
		LUA_TLIGHTUSERDATA = 2,
		LUA_TNUMBER = 3,
		LUA_TSTRING = 4,
		LUA_TTABLE = 5,
		LUA_TFUNCTION = 6,
		LUA_TUSERDATA = 7,
		LUA_TTHREAD = 8
	}

	public enum LuaGCOptions {
		LUA_GCSTOP = 0,
		LUA_GCRESTART = 1,
		LUA_GCCOLLECT = 2,
		LUA_GCCOUNT = 3,
		LUA_GCCOUNTB = 4,
		LUA_GCSTEP = 5,
		LUA_GCSETPAUSE = 6,
		LUA_GCSETSTEPMUL = 7,
	}

	public enum LuaIndex {
		LUA_REGISTRYINDEX = -10000,
		LUA_ENVIRONINDEX = -10001,
		LUA_GLOBALSINDEX = -10002
	}

	// lua side function
	#if !UNITY_IPHONE
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	#endif
	public delegate int LuaCSFunction(IntPtr L);

	// lua native lib wrapper
	#if !UNITY_IPHONE
	[SuppressUnmanagedCodeSecurity]
	#endif
	public class LuaLib {
		// lua lib name
		#if UNITY_IPHONE
		const string LUALIB = "__Internal";
		#else
		const string LUALIB = "lualu";
		#endif

		// get lib name
		public static string getLibName() {
			return LUALIB;
		}

		/////////////////////////////////////////////
		// lua.h
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_close(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_call(IntPtr L, int nArgs, int nResults);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_pcall(IntPtr L, int nArgs, int nResults, int errfunc);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_isnumber(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_isstring(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_iscfunction(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern bool lua_isuserdata(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_type(IntPtr L, int index);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string lua_typename(IntPtr L, int tp);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gettop(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_settop(IntPtr L, int newTop);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushvalue(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_remove(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_insert(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_replace(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_checkstack(IntPtr L, int sz);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfield(IntPtr L, int stackPos, string meta);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern double lua_tonumber(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_tointeger(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_toboolean(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string lua_tolstring(IntPtr L, int idx, out int len);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_objlen(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tocfunction(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_touserdata(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tothread(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_topointer(IntPtr L, int idx);

		/////////////////////////////////////////////
		// lualib.h
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_openlibs(IntPtr L);

		/////////////////////////////////////////////
		// luaxlib.h
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_newstate();

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadstring(IntPtr L, string chunk);

		/////////////////////////////////////////////
		// log support
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void set_unity_log_func(IntPtr fp);

		/////////////////////////////////////////////
		// macros from lua.h
		/////////////////////////////////////////////

		public static void lua_pop(IntPtr L, int amount) {
			lua_settop(L, -(amount) - 1);
		}

		public static bool lua_isfunction(IntPtr L, int stackPos) {
			return lua_type(L, stackPos) == (int)LuaTypes.LUA_TFUNCTION;
		}

		public static bool lua_istable(IntPtr L, int n) {
			return lua_type(L, (n)) == (int)LuaTypes.LUA_TTABLE;
		}

		public static bool lua_islightuserdata(IntPtr L, int n) {
			return lua_type(L, (n)) == (int)LuaTypes.LUA_TLIGHTUSERDATA;
		}

		public static bool lua_isnil(IntPtr L, int n) {
			return lua_type(L, (n)) == (int)LuaTypes.LUA_TNIL;
		}

		public static bool lua_isboolean(IntPtr L, int n) {
			return lua_type(L, (n)) == (int)LuaTypes.LUA_TBOOLEAN;
		}

		public static bool lua_isthread(IntPtr L, int n) {
			return lua_type(L, (n)) == (int)LuaTypes.LUA_TTHREAD;
		}

		public static bool lua_isnone(IntPtr L, int n) {
			return lua_type(L, (n)) == (int)LuaTypes.LUA_TNONE;
		}

		public static bool lua_isnoneornil(IntPtr L, int n) {
			return lua_type(L, (n)) <= 0;
		}

		public static void luaL_getmetatable(IntPtr L, string meta) {
			lua_getfield(L, (int)LuaIndex.LUA_REGISTRYINDEX, meta);
		}

		public static void lua_getglobal(IntPtr L, string n) {
			lua_getfield(L, (int)LuaIndex.LUA_GLOBALSINDEX, n);
		}

		public static string lua_tostring(IntPtr L, int idx) {
			int len;
			return lua_tolstring(L, idx, out len);
		}

		/////////////////////////////////////////////
		// macros from luaxlib.h
		/////////////////////////////////////////////

		public static IntPtr lua_open() {
			return luaL_newstate();
		}

		public static int luaL_dostring(IntPtr L, string chunk) {
			int result = luaL_loadstring(L, chunk);
			if(result != 0)
				return result;
			return lua_pcall(L, 0, -1, 0);
		}
	}
}