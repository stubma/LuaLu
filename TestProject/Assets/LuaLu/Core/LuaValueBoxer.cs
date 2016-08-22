namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using System.Runtime.InteropServices;
	using LuaInterface;

	public class LuaValueBoxer {
		// shared error struct
		static tolua_Error tolua_err;

		static LuaValueBoxer() {
			tolua_err = new tolua_Error();
		}

		static bool luaval_is_usertype(IntPtr L, int lo, string type) {
			if(LuaLib.lua_gettop(L) < Math.Abs(lo))
				return true;

			if(LuaLib.lua_isnil(L, lo) || LuaLib.lua_isusertype(L, lo, type))
				return true;

			return false;
		}

		static void luaval_to_native_err(IntPtr L, string msg, ref tolua_Error err, string funcName = "") {
			if(L == IntPtr.Zero || msg == null || msg.Length == 0)
				return;

			if(msg[0] == '#') {
				string expected = Marshal.PtrToStringAnsi(err.type);
				string provided = LuaLib.tolua_typename(L, err.index);
				if(msg[1] == 'f') {
					int narg = err.index;
					if(err.array != 0)
						Debug.Log(string.Format("{0}\n     {1} argument #{2} is array of '{3}'; array of '{4}' expected.\n", msg.Substring(2), funcName, narg, provided, expected));
					else
						Debug.Log(string.Format("{0}\n     {1} argument #{2} is '{3}'; '{4}' expected.\n", msg.Substring(2), funcName, narg, provided, expected));
				} else if(msg[1] == 'v') {
					if(err.array != 0)
						Debug.Log(string.Format("{0}\n     {1} value is array of '{2}'; array of '{4}' expected.\n", funcName, msg.Substring(2), provided, expected));
					else
						Debug.Log(string.Format("{0}\n     {1} value is '{2}'; '{4}' expected.\n", msg.Substring(2), funcName, provided, expected));
				}
			}
		}

		public static bool luaval_to_byte(IntPtr L, int lo, out byte outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (byte)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_sbyte(IntPtr L, int lo, out sbyte outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (sbyte)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_char(IntPtr L, int lo, out char outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = '\0';
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (char)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = '\0';
			}

			return ok;
		}

		public static bool luaval_to_short(IntPtr L, int lo, out short outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (short)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_ushort(IntPtr L, int lo, out ushort outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (ushort)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_bool(IntPtr L, int lo, out bool outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = false;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isboolean(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = LuaLib.tolua_toboolean(L, lo, 0);
			} else {
				outValue = false;
			}

			return ok;
		}

		public static bool luaval_to_int(IntPtr L, int lo, out int outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_uint(IntPtr L, int lo, out uint outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (uint)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_long(IntPtr L, int lo, out long outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (long)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_ulong(IntPtr L, int lo, out ulong outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (ulong)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_float(IntPtr L, int lo, out float outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (float)LuaLib.tolua_tonumber(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_double(IntPtr L, int lo, out double outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = LuaLib.tolua_tonumber(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_decimal(IntPtr L, int lo, out decimal outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = 0;
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = (decimal)LuaLib.tolua_tointeger(L, lo, 0);
			} else {
				outValue = 0;
			}

			return ok;
		}

		public static bool luaval_to_string(IntPtr L, int lo, out string outValue, string funcName = "") {
			if(IntPtr.Zero == L) {
				outValue = "";
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isstring(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			if(ok) {
				outValue = LuaLib.tolua_tostring(L, lo, "");
			} else {
				outValue = "";
			}

			return ok;
		}

		public static bool luaval_to_object<T>(IntPtr L, int lo, string type, out T ret, string funcName = "") {
			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				ret = default(T);
				return false;
			}
			if(!luaval_is_usertype(L, lo, type)) {
				ret = default(T);
				return false;
			}

			// to obj
			int refId = LuaLib.tolua_tousertype(L, lo);
			ret = (T)NativeObjectMap.FindObject(refId);

			// check
			if(ret == null) {
				Debug.Log(string.Format("luaval_to_object failed when convert to object type: {0} for func: {1}", type, funcName));
			}

			return true;
		}

		public static bool luaval_to_enum<T>(IntPtr L, int lo, out T ret, string funcName = "") {
			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				ret = default(T);
				return false;
			}

			bool ok = true;
			if(!LuaLib.tolua_isnumber(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// to enum
			if(ok) {
				ret = (T)Enum.ToObject(typeof(T), LuaLib.tolua_tointeger(L, lo, 0));
			} else {
				ret = default(T);
			}

			return true;
		}

		public static bool luaval_to_byte_array(IntPtr L, int lo, out byte[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new byte[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (byte)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_sbyte_array(IntPtr L, int lo, out sbyte[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new sbyte[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (sbyte)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_char_array(IntPtr L, int lo, out char[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new char[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (char)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_short_array(IntPtr L, int lo, out short[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new short[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (short)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_ushort_array(IntPtr L, int lo, out ushort[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new ushort[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (ushort)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_bool_array(IntPtr L, int lo, out bool[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new bool[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isboolean(L, -1)) {
						ret[i] = LuaLib.tolua_toboolean(L, -1, 0);
					} else {
						Debug.Assert(false, "boolean type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_int_array(IntPtr L, int lo, out int[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new int[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (int)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_uint_array(IntPtr L, int lo, out uint[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new uint[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (uint)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_long_array(IntPtr L, int lo, out long[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new long[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (long)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_ulong_array(IntPtr L, int lo, out ulong[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new ulong[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (ulong)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_decimal_array(IntPtr L, int lo, out decimal[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new decimal[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (decimal)LuaLib.tolua_tointeger(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_float_array(IntPtr L, int lo, out float[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new float[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (float)LuaLib.tolua_tonumber(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_double_array(IntPtr L, int lo, out double[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new double[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = LuaLib.tolua_tonumber(L, -1, 0);
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_string_array(IntPtr L, int lo, out string[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new string[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isstring(L, -1)) {
						ret[i] = LuaLib.tolua_tostring(L, -1, "");
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_enum_array<T>(IntPtr L, int lo, out T[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new T[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(LuaLib.lua_isnumber(L, -1)) {
						ret[i] = (T)Enum.ToObject(typeof(T), LuaLib.tolua_tointeger(L, -1, 0));
					} else {
						Debug.Assert(false, "int type is needed");
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_object_array<T>(IntPtr L, int lo, string type, out T[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				return false;
			}

			// convert negative index to positive
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// top should be a table
			bool ok = true;
			if(!LuaLib.tolua_istable(L, lo, 0, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = new T[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);
					if(luaval_is_usertype(L, -1, type)) {
						int refId = LuaLib.tolua_tousertype(L, -1);
						ret[i] = (T)NativeObjectMap.FindObject(refId);
					} else {
						Debug.Assert(false, string.Format("{0} type is needed", type));
					}

					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static void byte_to_luaval(IntPtr L, byte v) {
			LuaLib.lua_pushinteger(L, v);
		}

		public static void sbyte_to_luaval(IntPtr L, sbyte v) {
			LuaLib.lua_pushinteger(L, v);
		}

		public static void char_to_luaval(IntPtr L, char v) {
			LuaLib.lua_pushinteger(L, v);
		}

		public static void short_to_luaval(IntPtr L, short v) {
			LuaLib.lua_pushinteger(L, v);
		}

		public static void ushort_to_luaval(IntPtr L, ushort v) {
			LuaLib.lua_pushinteger(L, v);
		}

		public static void int_to_luaval(IntPtr L, int v) {
			LuaLib.lua_pushinteger(L, v);
		}

		public static void uint_to_luaval(IntPtr L, uint v) {
			LuaLib.lua_pushinteger(L, (int)v);
		}

		public static void long_to_luaval(IntPtr L, long v) {
			LuaLib.lua_pushinteger(L, (int)v);
		}

		public static void ulong_to_luaval(IntPtr L, ulong v) {
			LuaLib.lua_pushinteger(L, (int)v);
		}

		public static void bool_to_luaval(IntPtr L, bool v) {
			LuaLib.tolua_pushboolean(L, v);
		}

		public static void decimal_to_luaval(IntPtr L, decimal v) {
			LuaLib.lua_pushinteger(L, (int)v);
		}

		public static void float_to_luaval(IntPtr L, float v) {
			LuaLib.tolua_pushnumber(L, v);
		}

		public static void double_to_luaval(IntPtr L, double v) {
			LuaLib.tolua_pushnumber(L, v);
		}

		public static void string_to_luaval(IntPtr L, string v) {
			LuaLib.tolua_pushstring(L, v);
		}

		public static void object_to_luaval(IntPtr L, string type, object t) {
			if(t == null) {
				LuaStack.SharedInstance().PushObject(t, type);
			} else {
				LuaLib.lua_pushnil(L);
			}
		}

		public static void byte_array_to_luaval(IntPtr L, byte[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void sbyte_array_to_luaval(IntPtr L, sbyte[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void char_array_to_luaval(IntPtr L, char[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void short_array_to_luaval(IntPtr L, short[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void ushort_array_to_luaval(IntPtr L, ushort[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void int_array_to_luaval(IntPtr L, int[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void uint_array_to_luaval(IntPtr L, uint[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, (int)inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void long_array_to_luaval(IntPtr L, long[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, (int)inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void ulong_array_to_luaval(IntPtr L, ulong[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushinteger(L, (int)inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void bool_array_to_luaval(IntPtr L, bool[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.tolua_pushboolean(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void float_array_to_luaval(IntPtr L, float[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushnumber(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void double_array_to_luaval(IntPtr L, double[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.lua_pushnumber(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void string_array_to_luaval(IntPtr L, string[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				LuaLib.tolua_pushstring(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void enum_array_to_luaval<T>(IntPtr L, T[] inValue) {
			int[] iv = new int[inValue.Length];
			for(int i = 0; i < inValue.Length; i++) {
				string s = inValue[i].ToString();
				iv[i] = (int)Enum.Parse(typeof(T), s);
			}
			int_array_to_luaval(L, iv);
		}

		public static void object_array_to_luaval<T>(IntPtr L, string type, T[] inValue) {
			// validate
			if(IntPtr.Zero == L)
				return;

			// new table for array
			LuaLib.lua_newtable(L);

			// push
			int c = inValue.Length;
			for(int i = 0; i < c; i++) {
				LuaLib.lua_pushinteger(L, i + 1);
				object_to_luaval(L, type, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void array_to_luaval<T>(IntPtr L, T[] inValue) {
			Array a = Array.CreateInstance(typeof(T), inValue.Length);
			for(int i = 0; i < inValue.Length; i++) {
				a.SetValue(inValue[i], i);
			}
			array_to_luaval(L, a);
		}

		public static void array_to_luaval(IntPtr L, Array inValue) {
			// new table for array value
			LuaLib.lua_newtable(L);

			// validate
			if (IntPtr.Zero == L || null == inValue)
				return;

			// push every element
			int indexTable = 1;
			IEnumerator e = inValue.GetEnumerator();
			while(e.Current != null) {
				// get type
				Type t = e.Current.GetType();
				string tn = t.GetNormalizedName();

				// convert
				if(tn == "byte") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, Convert.ToByte(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "sbyte") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, Convert.ToSByte(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "char") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, Convert.ToChar(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "short") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, Convert.ToInt16(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "ushort") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, Convert.ToUInt16(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "bool") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushboolean(L, Convert.ToBoolean(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "int" || tn == "decimal") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, Convert.ToInt32(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "uint") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt32(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "long") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, (int)Convert.ToInt64(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "ulong") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt64(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "float" || tn == "double") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushnumber(L, Convert.ToDouble(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "string") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushstring(L, (string)e.Current);
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(t.IsEnum) {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, (int)e.Current);
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(t.IsArray) {
					LuaLib.lua_pushnumber(L, indexTable);
					array_to_luaval(L, (Array)e.Current);
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(t.IsDictionary()) {
					LuaLib.lua_pushnumber(L, indexTable);
					dictionary_to_luaval(L, (IDictionary)e.Current);
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else {
					LuaLib.lua_pushnumber(L, indexTable);
					object_to_luaval(L, tn, e.Current);
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				}

				// next
				e.MoveNext();
			}
		}

		public static void dictionary_to_luaval(IntPtr L, IDictionary dict) {
			// new table for dictionary value
			LuaLib.lua_newtable(L);

			// validate
			if (IntPtr.Zero == L || null == dict)
				return;

			// push
			IDictionaryEnumerator e = dict.GetEnumerator();
			while(e.Current != null) {
				// get key type
				Type kt = e.Key.GetType();
				string ktn = kt.GetNormalizedName();
				string strKey = null;
				int intKey = 0;

				// convert key
				if(ktn == "byte") {
					intKey = Convert.ToByte(e.Key);
				} else if(ktn == "sbyte") {
					intKey = Convert.ToSByte(e.Key);
				} else if(ktn == "char") {
					strKey = Convert.ToChar(e.Key).ToString();
				} else if(ktn == "short") {
					intKey = Convert.ToInt16(e.Key);
				} else if(ktn == "ushort") {
					intKey = Convert.ToUInt16(e.Key);
				} else if(ktn == "bool") {
					intKey = Convert.ToBoolean(e.Key) ? 1 : 0;
				} else if(ktn == "int" || ktn == "decimal") {
					intKey = Convert.ToInt32(e.Key);
				} else if(ktn == "uint") {
					intKey = (int)Convert.ToUInt32(e.Key);
				} else if(ktn == "long") {
					intKey = (int)Convert.ToInt64(e.Key);
				} else if(ktn == "ulong") {
					intKey = (int)Convert.ToUInt64(e.Key);
				} else if(ktn == "string") {
					strKey = (string)e.Key;
				} else {
					// not supported key type
					e.MoveNext();
					continue;
				}

				// push key
				if(strKey != null) {
					LuaLib.lua_pushstring(L, strKey);
				} else {
					LuaLib.lua_pushnumber(L, intKey);
				}

				// push value
				Type vt = e.Value.GetType();
				string vtn = vt.GetNormalizedName();
				if(vtn == "byte") {
					LuaLib.lua_pushinteger(L, Convert.ToByte(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "sbyte") {
					LuaLib.lua_pushinteger(L, Convert.ToSByte(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "char") {
					LuaLib.lua_pushinteger(L, Convert.ToChar(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "short") {
					LuaLib.lua_pushinteger(L, Convert.ToInt16(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "ushort") {
					LuaLib.lua_pushinteger(L, Convert.ToUInt16(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "bool") {
					LuaLib.lua_pushboolean(L, Convert.ToBoolean(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "int" || vtn == "decimal") {
					LuaLib.lua_pushinteger(L, Convert.ToInt32(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "uint") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt32(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "long") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToInt64(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "ulong") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt64(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "float" || vtn == "double") {
					LuaLib.lua_pushnumber(L, Convert.ToDouble(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "string") {
					LuaLib.lua_pushstring(L, (string)e.Value);
					LuaLib.lua_rawset(L, -3);
				} else if(vt.IsEnum) {
					LuaLib.lua_pushinteger(L, (int)e.Value);
					LuaLib.lua_rawset(L, -3);
				} else if(vt.IsArray) {
					array_to_luaval(L, (Array)e.Value);
					LuaLib.lua_rawset(L, -3);
				} else if(vt.IsDictionary()) {
					dictionary_to_luaval(L, (IDictionary)e.Value);
					LuaLib.lua_rawset(L, -3);
				} else {
					object_to_luaval(L, vtn, e.Value);
					LuaLib.lua_rawset(L, -3);
				}

				// next
				e.MoveNext();
			}
		}

		public static bool CheckParameterType(IntPtr L, params string[] ptList) {
			int argc = LuaLib.lua_gettop(L) - 1;
			if(ptList != null && ptList.Length >= argc) {
				for(int i = 0; i < argc; i++) {
					
				}
			}

			return false;
		}
	}
}