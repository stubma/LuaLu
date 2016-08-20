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

	/// <summary>
	/// lua value type constant
	/// </summary>
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

	/// <summary>
	/// lua garbage collection option
	/// </summary>
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

	/// <summary>
	/// lua thread status
	/// </summary>
	public enum LuaThreadStatus {
		LUA_YIELD = 1,
		LUA_ERRRUN = 2,
		LUA_ERRSYNTAX = 3,
		LUA_ERRMEM = 4,
		LUA_ERRERR = 5
	}

	/// <summary>
	/// special lua index
	/// </summary>
	public enum LuaIndex {
		LUA_REGISTRYINDEX = -10000,
		LUA_ENVIRONINDEX = -10001,
		LUA_GLOBALSINDEX = -10002
	}

	// lua side function
	#if !UNITY_IPHONE
	[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
	#endif
	public delegate int LuaFunction(IntPtr L);

	/// <summary>
	/// struct for lua side registeration
	/// </summary>
	[StructLayout(LayoutKind.Sequential)]
	public struct luaL_Reg {
		[MarshalAs(UnmanagedType.LPStr)]
		public string name;

		// in il2cpp enabled platform, here is a marshalling bug for reference type,
		// so we have to use IntPtr directly
		// issue tracker: https://issuetracker.unity3d.com/issues/il2cpp-marshaldirectiveexception-marshaling-of-delegates-as-fields-of-a-struct-is-not-working
		public IntPtr func;

		public luaL_Reg(string n, LuaFunction f) {
			name = n;
			func = f == null ? IntPtr.Zero : Marshal.GetFunctionPointerForDelegate(f);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct tolua_Error {
		public int index;
		public int array;
		public IntPtr type;
	}

	/// <summary>
	/// lua native lib wrapper
	/// </summary>
	#if !UNITY_IPHONE
	[SuppressUnmanagedCodeSecurity]
	#endif
	public class LuaLib {
		// option for multiple returns in `lua_pcall' and `lua_call'
		public static int LUA_MULTRET = -1;

		// tolua no peer
		public const int tolua_NOPEER = (int)LuaIndex.LUA_REGISTRYINDEX;

		// lua lib name
		#if UNITY_IPHONE
		const string LUALIB = "__Internal";
		#else
		const string LUALIB = "lualu";
		#endif

		// get lib name
		public static string GetLibName() {
			return LUALIB;
		}

		/// <summary>
		/// get a utf-8 string
		/// </summary>
		/// <returns>utf-8 string</returns>
		/// <param name="source">source bytes</param>
		/// <param name="strlen">byte length of this string</param>
		static string AnsiToUnicode(IntPtr source, int strlen) {
			byte[] buffer = new byte[strlen];
			Marshal.Copy(source, buffer, 0, strlen);            
			return Encoding.UTF8.GetString(buffer);
		}

		/////////////////////////////////////////////
		// lua.h
		/////////////////////////////////////////////

		// pending api
//		LUA_API IntPtr (lua_newstate) (lua_Alloc f, void *ud);
//		LUA_API const char *(lua_pushvfstring) (IntPtr L, const char *fmt, va_list argp);
//		LUA_API const char *(lua_pushfstring) (IntPtr L, const char *fmt, ...);
//		LUA_API int   (lua_load) (lua_State *L, lua_Reader reader, void *dt, const char *chunkname);
//		LUA_API int (lua_dump) (lua_State *L, lua_Writer writer, void *data);
//		LUA_API lua_Alloc (lua_getallocf) (lua_State *L, void **ud);
//		LUA_API void lua_setallocf (lua_State *L, lua_Alloc f, void *ud);
//		LUA_API int lua_getstack (lua_State *L, int level, lua_Debug *ar);
//		LUA_API int lua_getinfo (lua_State *L, const char *what, lua_Debug *ar);
//		LUA_API const char *lua_getlocal (lua_State *L, const lua_Debug *ar, int n);
//		LUA_API const char *lua_setlocal (lua_State *L, const lua_Debug *ar, int n);
//		LUA_API int lua_sethook (lua_State *L, lua_Hook func, int mask, int count);
//		LUA_API lua_Hook lua_gethook (lua_State *L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_close(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_newthread(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_atpanic(IntPtr L, LuaFunction panicf);

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
		public static extern int lua_equal(IntPtr L, int idx1, int idx2);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_rawequal(IntPtr L, int idx1, int idx2);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_lessthan(IntPtr L, int idx1, int idx2);

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
		public static extern void lua_xmove(IntPtr from, IntPtr to, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern double lua_tonumber(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_tointeger(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_toboolean(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_tolstring(IntPtr L, int idx, out int len);

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

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushnil(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushnumber(IntPtr L, double n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushinteger(IntPtr L, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushlstring(IntPtr L, byte[] s, int l);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushstring(IntPtr L, string s);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushcclosure(IntPtr L, IntPtr fn, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushboolean(IntPtr L, int b);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_pushlightuserdata(IntPtr L, IntPtr p);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_pushthread(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_gettable(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfield(IntPtr L, int stackPos, string meta);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawget(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawgeti(IntPtr L, int idx, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_createtable(IntPtr L, int narr, int nrec);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr lua_newuserdata(IntPtr L, int sz);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_getmetatable(IntPtr L, int objindex);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_getfenv(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_settable(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_setfield(IntPtr L, int idx, string k);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawset(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_rawseti(IntPtr L, int idx, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_setmetatable(IntPtr L, int objindex);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_setfenv(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_call(IntPtr L, int nArgs, int nResults);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_pcall(IntPtr L, int nArgs, int nResults, int errfunc);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_cpcall(IntPtr L, LuaFunction func, IntPtr ud);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_yield(IntPtr L, int nresults);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_resume(IntPtr L, int narg);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_status(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gc(IntPtr L, int what, int data);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_error(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_next(IntPtr L, int idx);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void lua_concat(IntPtr L, int n);

		public static void lua_pop(IntPtr L, int amount) {
			lua_settop(L, -(amount) - 1);
		}

		public static void lua_newtable(IntPtr L) {
			lua_createtable(L, 0, 0);
		}

		public static void lua_register(IntPtr L, string n, IntPtr f) {
			lua_pushcfunction(L, f);
			lua_setglobal(L, n);
		}

		public static void lua_pushcfunction(IntPtr L, IntPtr f) {
			lua_pushcclosure(L, f, 0);
		}

		public static int lua_strlen(IntPtr L, int i) {
			return lua_objlen(L, i);
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

		public static void lua_pushliteral(IntPtr L, byte[] s) {
			lua_pushlstring(L, s, s.Length);
		}

		public static void lua_setglobal(IntPtr L, string s) {
			lua_setfield(L, (int)LuaIndex.LUA_GLOBALSINDEX, s);
		}

		public static void lua_getglobal(IntPtr L, string n) {
			lua_getfield(L, (int)LuaIndex.LUA_GLOBALSINDEX, n);
		}

		public static string lua_tostring(IntPtr L, int idx) {
			int len;
			IntPtr str = lua_tolstring(L, idx, out len);
			if(str != IntPtr.Zero) {
				string ss = Marshal.PtrToStringAnsi(str, len);

				// when lua return non-ansi string, conversion fails
				if(ss == null) {
					return AnsiToUnicode(str, len);
				}

				return ss;
			} else {
				return null;
			}
		}

		public static IntPtr lua_open() {
			return luaL_newstate();
		}

		public static void lua_getregistry(IntPtr L) {
			lua_pushvalue(L, (int)LuaIndex.LUA_REGISTRYINDEX);
		}

		public static int lua_getgccount(IntPtr L) {
			return lua_gc(L, (int)LuaGCOptions.LUA_GCCOUNT, 0);
		}

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string lua_getupvalue(IntPtr L, int funcindex, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string lua_setupvalue(IntPtr L, int funcindex, int n);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gethookmask(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_gethookcount(IntPtr L);

		/////////////////////////////////////////////
		// lualib.h
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_openlibs(IntPtr L);

		/////////////////////////////////////////////
		// luaxlib.h
		/////////////////////////////////////////////

		// pending api
//		LUALIB_API void (luaL_buffinit) (lua_State *L, luaL_Buffer *B);
//		LUALIB_API char *(luaL_prepbuffer) (luaL_Buffer *B);
//		LUALIB_API void (luaL_addlstring) (luaL_Buffer *B, const char *s, size_t l);
//		LUALIB_API void (luaL_addstring) (luaL_Buffer *B, const char *s);
//		LUALIB_API void (luaL_addvalue) (luaL_Buffer *B);
//		LUALIB_API void (luaL_pushresult) (luaL_Buffer *B);
//		LUALIB_API void (luaI_openlib) (lua_State *L, const char *libname, const luaL_Reg *l, int nup);
//		LUALIB_API int (luaL_checkoption) (lua_State *L, int narg, const char *def, const char *const lst[]);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_register(IntPtr L, string libname, luaL_Reg[] l);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_getmetafield(IntPtr L, int obj, string e);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_callmeta(IntPtr L, int obj, string e);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_typerror(IntPtr L, int narg, string tname);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_argerror(IntPtr L, int numarg, string extramsg);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_checklstring(IntPtr L, int numArg, out int l);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_optlstring(IntPtr L, int numArg, byte[] def, out int l);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern double luaL_checknumber(IntPtr L, int numArg);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern double luaL_optnumber(IntPtr L, int nArg, double def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_checkinteger(IntPtr L, int numArg);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_optinteger(IntPtr L, int nArg, int def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_checkstack(IntPtr L, int sz, string msg);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_checktype(IntPtr L, int narg, int t);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_checkany(IntPtr L, int narg);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_newmetatable(IntPtr L, string tname);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_checkudata(IntPtr L, int ud, string tname);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_where(IntPtr L, int lvl);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_error(IntPtr L, string fmt);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_ref(IntPtr L, int t);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaL_unref(IntPtr L, int t, int _ref);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadfile(IntPtr L, string filename);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadbuffer(IntPtr L, byte[] buff, int sz, string name);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int luaL_loadstring(IntPtr L, string chunk);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr luaL_newstate();

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string luaL_gsub(IntPtr L, string s, string p, string r);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string luaL_findtable(IntPtr L, int idx, string fname, int szhint);
		
		public static string luaL_checkstring(IntPtr L, int n) {
			int len;
			IntPtr str = luaL_checklstring(L, n, out len);
			if(str != IntPtr.Zero) {
				string ss = Marshal.PtrToStringAnsi(str, len);

				// when lua return non-ansi string, conversion fails
				if(ss == null) {
					return AnsiToUnicode(str, len);
				}

				return ss;
			} else {
				return null;
			}
		}

		public static string luaL_optstring(IntPtr L, int n, string d) {
			int len;
			IntPtr str = luaL_optlstring(L, n, Encoding.UTF8.GetBytes(d), out len);
			if(str != IntPtr.Zero) {
				string ss = Marshal.PtrToStringAnsi(str, len);

				// when lua return non-ansi string, conversion fails
				if(ss == null) {
					return AnsiToUnicode(str, len);
				}

				return ss;
			} else {
				return null;
			}
		}

		public static int luaL_checkint(IntPtr L, int n) {
			return luaL_checkinteger(L, n);
		}

		public static int luaL_optint(IntPtr L, int n, int d) {
			return luaL_optinteger(L, n, d);
		}

		public static long luaL_checklong(IntPtr L, int n) {
			return (long)luaL_checkinteger(L, n);
		}

		public static long luaL_optlong(IntPtr L, int n, int d) {
			return (long)luaL_optinteger(L, n, d);
		}

		public static string luaL_typename(IntPtr L, int i) {
			return lua_typename(L, lua_type(L, i));
		}

		public static int luaL_dofile(IntPtr L, string fn) {
			int result = luaL_loadfile(L, fn);
			if(result != 0) {
				return result;
			}
			return lua_pcall(L, 0, LUA_MULTRET, 0);
		}

		public static int luaL_dostring(IntPtr L, string chunk) {
			int result = luaL_loadstring(L, chunk);
			if(result != 0) {
				return result;
			}
			return lua_pcall(L, 0, -1, 0);
		}

		public static void luaL_getmetatable(IntPtr L, string meta) {
			lua_getfield(L, (int)LuaIndex.LUA_REGISTRYINDEX, meta);
		}

		public static int lua_ref(IntPtr L, int lockRef) {
			if(lockRef != 0) {
				return luaL_ref(L, (int)LuaIndex.LUA_REGISTRYINDEX);
			} else {
				lua_pushstring(L, "unlocked references are obsolete");
				lua_error(L);
				return 0;
			}
		}

		public static void lua_unref(IntPtr L, int _ref) {
			luaL_unref(L, (int)LuaIndex.LUA_REGISTRYINDEX, _ref);
		}

		public static void lua_getref(IntPtr L, int _ref) {
			lua_rawgeti(L, (int)LuaIndex.LUA_REGISTRYINDEX, _ref);
		}

		/////////////////////////////////////////////
		// other module
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaopen_lfs(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaopen_cjson(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void luaopen_socket_core(IntPtr L);

		/////////////////////////////////////////////
		// log support
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void set_unity_log_func(IntPtr fp);

		/////////////////////////////////////////////
		// tolua
		/////////////////////////////////////////////

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_stack_dump(IntPtr L, string label);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int lua_isusertype(IntPtr L, int lo, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string tolua_typename(IntPtr L, int lo);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_error(IntPtr L, string msg, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isnoobj(IntPtr L, int lo, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isfunction(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isvalue(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isvaluenil(IntPtr L, int lo, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isboolean(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isnumber(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isstring(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_istable(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isusertable(IntPtr L, int lo, string type, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isuserdata(IntPtr L, int lo, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isusertype(IntPtr L, int lo, string type, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isvaluearray(IntPtr L, int lo, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isbooleanarray(IntPtr L, int lo, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isnumberarray(IntPtr L, int lo, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isstringarray(IntPtr L, int lo, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_istablearray(IntPtr L, int lo, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isuserdataarray(IntPtr L, int lo, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_isusertypearray(IntPtr L, int lo, string type, int dim, int def, ref tolua_Error err);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_open(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr tolua_copy(IntPtr L, IntPtr value, uint size);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_register_gc(IntPtr L, int lo);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_default_collect(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_usertype(IntPtr L, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_beginmodule(IntPtr L, string name);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_endmodule(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_module(IntPtr L, string name, int hasvar);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_class(IntPtr L, string name, string _base, LuaFunction col);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_function(IntPtr L, string name, LuaFunction func);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_constant(IntPtr L, string name, double value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_variable(IntPtr L, string name, LuaFunction get, LuaFunction set);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_array(IntPtr L, string name, LuaFunction get, LuaFunction set);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_addbase(IntPtr L, string name, string _base);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushvalue(IntPtr L, int lo);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushboolean(IntPtr L, int value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushnumber(IntPtr L, double value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushstring(IntPtr L, string value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushuserdata(IntPtr L, IntPtr value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushusertype(IntPtr L, IntPtr value, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushusertype_and_takeownership(IntPtr L, IntPtr value, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfieldvalue(IntPtr L, int lo, int index, int v);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfieldboolean(IntPtr L, int lo, int index, int v);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfieldnumber(IntPtr L, int lo, int index, double v);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfieldstring(IntPtr L, int lo, int index, string v);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfielduserdata(IntPtr L, int lo, int index, IntPtr v);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfieldusertype(IntPtr L, int lo, int index, IntPtr v, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushfieldusertype_and_takeownership(IntPtr L, int lo, int index, IntPtr v, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_pushusertype_and_addtoroot(IntPtr L, IntPtr value, string type);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_add_value_to_root(IntPtr L, IntPtr value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_remove_value_from_root(IntPtr L, IntPtr value);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern double tolua_tonumber(IntPtr L, int narg, double def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string tolua_tostring(IntPtr L, int narg, string def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr tolua_touserdata(IntPtr L, int narg, IntPtr def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr tolua_tousertype(IntPtr L, int narg, IntPtr def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_tovalue(IntPtr L, int narg, int def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_toboolean(IntPtr L, int narg, int def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern double tolua_tofieldnumber(IntPtr L, int lo, int index, double def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string tolua_tofieldstring(IntPtr L, int lo, int index, string def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr tolua_tofielduserdata(IntPtr L, int lo, int index, IntPtr def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern IntPtr tolua_tofieldusertype(IntPtr L, int lo, int index, IntPtr def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_tofieldvalue(IntPtr L, int lo, int index, int def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_getfieldboolean(IntPtr L, int lo, int index, int def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern void tolua_dobuffer(IntPtr L, byte[] B, uint size, string name);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int class_gc_event(IntPtr L);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string tolua_tocppstring(IntPtr L, int narg, string def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern string tolua_tofieldcppstring(IntPtr L, int lo, int index, string def);

		[DllImport(LUALIB, CallingConvention = CallingConvention.Cdecl)]
		public static extern int tolua_fast_isa(IntPtr L, int mt_indexa, int mt_indexb, int super_index);
	}
}