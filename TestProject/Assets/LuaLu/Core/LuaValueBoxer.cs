namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using System.Runtime.InteropServices;
	using LuaInterface;

	[NoLuaBinding]
	public class LuaValueBoxer {
		// shared error struct
		static tolua_Error tolua_err;

		// implicit numeric conversion table
		static Dictionary<string, Dictionary<string, string>> implicitConversionTable;

		// static better conversion check
		static Dictionary<string, Dictionary<string, string>> betterConversionTable;

		static LuaValueBoxer() {
			tolua_err = new tolua_Error();
			implicitConversionTable = new Dictionary<string, Dictionary<string, string>> {
				{ 
					"System.SByte", 
					new Dictionary<string, string> {
						{ "System.Int16", "System.Int16" },
						{ "System.Int32", "System.Int32" },
						{ "System.Int64", "System.Int64" },
						{ "System.Float", "System.Float"},
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{ 
					"System.Byte",
					new Dictionary<string, string> {
						{ "System.Int16", "System.Int16" },
						{ "System.UInt16", "System.UInt16" },
						{ "System.Int32", "System.Int32" },
						{ "System.UInt32", "System.UInt32" },
						{ "System.Int64", "System.Int64" },
						{ "System.UInt64", "System.UInt64" },
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{ 
					"System.Int16",
					new Dictionary<string, string> {
						{ "System.Int32", "System.Int32" },
						{ "System.Int64", "System.Int64" },
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{ 
					"System.UInt16",
					new Dictionary<string, string> {
						{ "System.Int32", "System.Int32" },
						{ "System.UInt32", "System.UInt32" },
						{ "System.Int64", "System.Int64" },
						{ "System.UInt64", "System.UInt64" },
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Int32",
					new Dictionary<string, string> {
						{ "System.Int64", "System.Int64" },
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.UInt32",
					new Dictionary<string, string> {
						{ "System.Int64", "System.Int64" },
						{ "System.UInt64", "System.UInt64" },
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Int64",
					new Dictionary<string, string> {
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Char",
					new Dictionary<string, string> {
						{ "System.UInt16", "System.UInt16" },
						{ "System.Int32", "System.Int32" },
						{ "System.UInt32", "System.UInt32" },
						{ "System.Int64", "System.Int64" },
						{ "System.UInt64", "System.UInt64" },
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Float",
					new Dictionary<string, string> {
						{ "System.Double", "System.Double" }
					}
				},
				{
					"System.UInt64",
					new Dictionary<string, string> {
						{ "System.Float" , "System.Float" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				}
			};
			betterConversionTable = new Dictionary<string, Dictionary<string, string>> {
				{ 
					"sbyte", 
					new Dictionary<string, string> {
						{ "byte", "byte" },
						{ "ushort", "ushort" }, 
						{ "uint", "uint" },
						{ "ulong", "ulong" }
					}
				},
				{ 
					"short", 
					new Dictionary<string, string> {
						{ "ushort", "ushort" },
						{ "uint", "uint" },
						{ "ulong", "ulong" }
					}
				},
				{ 
					"int", 
					new Dictionary<string, string> {
						{ "uint", "uint" },
						{ "ulong", "ulong" }
					}
				},
				{ 
					"long", 
					new Dictionary<string, string> {
						{ "ulong", "ulong" }
					}
				}
			};
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

		public static bool luaval_to_type<T>(IntPtr L, int lo, out T ret, string funcName = "") {
			// validate
			if(IntPtr.Zero == L || LuaLib.lua_gettop(L) < lo) {
				ret = default(T);
				return false;
			}

			// get type and name
			Type t = typeof(T);
			string tn = t.GetNormalizedName();

			// convert
			bool ok = true;
			if(tn == "byte" ||
			   tn == "sbyte" ||
			   tn == "short" ||
			   tn == "ushort" ||
			   tn == "int" ||
			   tn == "uint" ||
			   tn == "decimal" ||
			   tn == "long" ||
			   tn == "ulong" ||
				tn == "char") {
				// top should be a number
				if(!LuaLib.tolua_isnumber(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to integer then convert
				if(ok) {
					ret = (T)Convert.ChangeType(LuaLib.tolua_tointeger(L, lo, 0), t);
				} else {
					ret = default(T);
				}
			} else if(tn == "bool") {
				// top should be a boolean
				if(!LuaLib.tolua_isboolean(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to boolean then convert
				if(ok) {
					ret = (T)Convert.ChangeType(LuaLib.tolua_toboolean(L, lo, 0), t);
				} else {
					ret = default(T);
				}
			} else if(tn == "string") {
				// top should be a string
				if(!LuaLib.tolua_isstring(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to number then convert
				if(ok) {
					ret = (T)Convert.ChangeType(LuaLib.tolua_tostring(L, lo, ""), t);
				} else {
					ret = default(T);
				}
			} else if(tn == "float" || tn == "double") {
				// top should be a number
				if(!LuaLib.tolua_isnumber(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to string then convert
				if(ok) {
					ret = (T)Convert.ChangeType(LuaLib.tolua_tonumber(L, lo, 0), t);
				} else {
					ret = default(T);
				}
			} else if(t.IsEnum) {
				// top should be a number
				if(!LuaLib.tolua_isnumber(L, lo, ref tolua_err)) {
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
			} else {
				// top should be a user type
				if(!luaval_is_usertype(L, lo, tn)) {
					ok = false;
				}

				// to obj
				if(ok) {
					int refId = LuaLib.tolua_tousertype(L, lo);
					ret = (T)LuaStack.FromState(L).FindObject(refId);
				} else {
					ret = default(T);
				}

				// check
				if(ret == null) {
					Debug.Log(string.Format("luaval_to_object failed when convert to object type: {0} for func: {1}", tn, funcName));
				}
			}

			return ok;
		}

		public static bool luaval_to_list(IntPtr L, int lo, out Array ret, string funcName = "") {
			// new
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
			if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				ret = Array.CreateInstance(typeof(object), len);
				IList list = ret as IList;
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);

					// get value and push to list
					int t = LuaLib.lua_type(L, -1);
					if(t == (int)LuaTypes.LUA_TBOOLEAN) {
						list.Add(LuaLib.tolua_toboolean(L, -1, 0));
					} else if(t == (int)LuaTypes.LUA_TNUMBER) {
						list.Add(LuaLib.tolua_tonumber(L, -1, 0));
					} else if(t == (int)LuaTypes.LUA_TSTRING) {
						list.Add(LuaLib.tolua_tostring(L, -1, ""));
					} else if(t == (int)LuaTypes.LUA_TUSERDATA) {
						string tn = LuaLib.tolua_typename(L, -1);
						if(luaval_is_usertype(L, -1, tn)) {
							// to obj
							int refId = LuaLib.tolua_tousertype(L, -1);
							object obj = LuaStack.FromState(L).FindObject(refId);
							if(obj != null) {
								list.Add(obj);
							}
						}
					}

					// pop value
					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_list<T>(IntPtr L, int lo, out T ret, string funcName = "") where T : IList, new() {
			// new
			ret = new T();

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
			if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				int len = LuaLib.lua_objlen(L, lo);
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);

					// get value and push to list
					int t = LuaLib.lua_type(L, -1);
					if(t == (int)LuaTypes.LUA_TBOOLEAN) {
						ret.Add(LuaLib.tolua_toboolean(L, -1, 0));
					} else if(t == (int)LuaTypes.LUA_TNUMBER) {
						ret.Add(LuaLib.tolua_tonumber(L, -1, 0));
					} else if(t == (int)LuaTypes.LUA_TSTRING) {
						ret.Add(LuaLib.tolua_tostring(L, -1, ""));
					} else if(t == (int)LuaTypes.LUA_TUSERDATA) {
						string tn = LuaLib.tolua_typename(L, -1);
						if(luaval_is_usertype(L, -1, tn)) {
							// to obj
							int refId = LuaLib.tolua_tousertype(L, -1);
							object obj = LuaStack.FromState(L).FindObject(refId);
							if(obj != null) {
								ret.Add(obj);
							}
						}
					}

					// pop value
					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_dictionary<T>(IntPtr L, int lo, out T ret, string funcName = "") where T: IDictionary, new() {
			// new
			ret = new T();

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
			if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				LuaLib.lua_pushnil(L);
				while(LuaLib.lua_next(L, lo) != 0) {
					// if key is not string, ignore
					if(!LuaLib.lua_isstring(L, -2)) {
						LuaLib.lua_pop(L, 1);
						continue;
					}

					// get key
					string strKey = LuaLib.lua_tostring(L, -2);

					// get value and push to list
					int vt = LuaLib.lua_type(L, -1);
					if(vt == (int)LuaTypes.LUA_TBOOLEAN) {
						ret.Add(strKey, LuaLib.tolua_toboolean(L, -1, 0));
					} else if(vt == (int)LuaTypes.LUA_TNUMBER) {
						ret.Add(strKey, LuaLib.tolua_tonumber(L, -1, 0));
					} else if(vt == (int)LuaTypes.LUA_TSTRING) {
						ret.Add(strKey, LuaLib.tolua_tostring(L, -1, ""));
					} else if(vt == (int)LuaTypes.LUA_TUSERDATA) {
						string tn = LuaLib.tolua_typename(L, -1);
						if(luaval_is_usertype(L, -1, tn)) {
							// to obj
							int refId = LuaLib.tolua_tousertype(L, -1);
							object obj = LuaStack.FromState(L).FindObject(refId);
							if(obj != null) {
								ret.Add(strKey, obj);
							}
						}
					}

					// pop value
					LuaLib.lua_pop(L, 1);
				}
			}

			return ok;
		}

		public static bool luaval_to_array<T>(IntPtr L, int lo, int toLo, out T[] ret, string funcName = "") {
			// by default nullify it
			ret = null;

			// convert negative index to positive
			int top = LuaLib.lua_gettop(L);
			if(lo < 0) {
				lo = top + lo + 1;
			}
			if(toLo < 0) {
				toLo = top + toLo + 1;
			}

			// if toLo <= lo, then we think lo should be a table
			// otherwise, the array is flatten from lo to toLo 
			bool flat = toLo > lo;

			// validate, however, if no such index, we return true, not false
			if(IntPtr.Zero == L) {
				return false;
			}
			if(top < lo) {
				return true;
			}

			// if top is not a table, force it flat
			if(!flat && !LuaLib.tolua_istable(L, lo, ref tolua_err)) {
				flat = true;
			}

			// fill elements
			bool ok = true;
			if(ok) {
				// element type
				Type t = typeof(T);
				string tn = t.GetNormalizedName();

				// iterate all elements
				int len = flat ? (toLo - lo + 1) : LuaLib.lua_objlen(L, lo);
				ret = new T[len];
				for(int i = 0; i < len; i++) {
					// if not flat, get value from table
					if(!flat) {
						LuaLib.lua_pushnumber(L, i + 1);
						LuaLib.lua_gettable(L, lo);
					}

					// convert value
					T element;
					ok = luaval_to_type<T>(L, flat ? (lo + i) : -1, out element, funcName);
					if(ok) {
						ret[i] = element;
					}

					// if failed, assert fail
					if(!ok) {
						Debug.Assert(false, string.Format("luaval_to_array: some element are not type: {0}", tn));
					}

					// pop value if not flat
					if(!flat) {
						LuaLib.lua_pop(L, 1);
					}
				}
			}

			return ok;
		}

		public static void object_to_luaval(IntPtr L, string type, object t) {
			if(t != null) {
				LuaStack.SharedInstance().PushObject(t, type);
			} else {
				LuaLib.lua_pushnil(L);
			}
		}

		public static void array_to_luaval<T>(IntPtr L, T[] inValue) {
			// new table for array value
			LuaLib.lua_newtable(L);

			// validate
			if(IntPtr.Zero == L || null == inValue)
				return;

			// push every element
			for(int i = 0; i < inValue.Length; i++) {
				LuaLib.lua_pushnumber(L, i + 1);
				type_to_luaval<T>(L, inValue[i]);
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void list_to_luaval(IntPtr L, IEnumerable inValue, bool flat = false) {
			// new table for array value, if not flat
			if(!flat) {
				LuaLib.lua_newtable(L);
			}

			// validate
			if(IntPtr.Zero == L || null == inValue)
				return;

			// push every element
			int indexTable = 1;
			IEnumerator e = inValue.GetEnumerator();
			while(e.MoveNext()) {
				// get type
				Type t = e.Current.GetType();
				string tn = t.GetNormalizedName();

				// if not flat, assign a table index
				if(!flat) {
					LuaLib.lua_pushnumber(L, indexTable);
				}

				// convert
				if(tn == "byte") {
					LuaLib.lua_pushinteger(L, Convert.ToByte(e.Current));
				} else if(tn == "sbyte") {
					LuaLib.lua_pushinteger(L, Convert.ToSByte(e.Current));
				} else if(tn == "char") {
					LuaLib.lua_pushinteger(L, Convert.ToChar(e.Current));
				} else if(tn == "short") {
					LuaLib.lua_pushinteger(L, Convert.ToInt16(e.Current));
				} else if(tn == "ushort") {
					LuaLib.lua_pushinteger(L, Convert.ToUInt16(e.Current));
				} else if(tn == "bool") {
					LuaLib.lua_pushboolean(L, Convert.ToBoolean(e.Current));
				} else if(tn == "int" || tn == "decimal") {
					LuaLib.lua_pushinteger(L, Convert.ToInt32(e.Current));
				} else if(tn == "uint") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt32(e.Current));
				} else if(tn == "long") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToInt64(e.Current));
				} else if(tn == "ulong") {
					LuaLib.lua_pushnumber(L, indexTable);
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt64(e.Current));
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				} else if(tn == "float" || tn == "double") {
					LuaLib.lua_pushnumber(L, Convert.ToDouble(e.Current));
				} else if(tn == "string") {
					LuaLib.lua_pushstring(L, (string)e.Current);
				} else if(tn == "void") {
					// impossible
					Debug.Assert(false, "void type in in list, that should not be possible");
				} else if(t.IsEnum) {
					LuaLib.lua_pushinteger(L, (int)e.Current);
				} else if(t.IsArray) {
					Array a = (Array)e.Current;
					list_to_luaval(L, a as IEnumerable);
				} else if(t.IsList() && t.IsEnumerable()) {
					list_to_luaval(L, e.Current as IEnumerable);
				} else if(t.IsDictionary()) {
					dictionary_to_luaval(L, (IDictionary)e.Current);
				} else {
					object_to_luaval(L, tn, e.Current);
				}

				// if not flat, add to table
				if(!flat) {
					LuaLib.lua_rawset(L, -3);
					++indexTable;
				}
			}
		}

		public static void dictionary_to_luaval(IntPtr L, IDictionary dict) {
			// new table for dictionary value
			LuaLib.lua_newtable(L);

			// validate
			if(IntPtr.Zero == L || null == dict)
				return;

			// push
			IDictionaryEnumerator e = dict.GetEnumerator();
			while(e.MoveNext()) {
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
				} else if(vtn == "sbyte") {
					LuaLib.lua_pushinteger(L, Convert.ToSByte(e.Value));
				} else if(vtn == "char") {
					LuaLib.lua_pushinteger(L, Convert.ToChar(e.Value));
				} else if(vtn == "short") {
					LuaLib.lua_pushinteger(L, Convert.ToInt16(e.Value));
				} else if(vtn == "ushort") {
					LuaLib.lua_pushinteger(L, Convert.ToUInt16(e.Value));
				} else if(vtn == "bool") {
					LuaLib.lua_pushboolean(L, Convert.ToBoolean(e.Value));
				} else if(vtn == "int" || vtn == "decimal") {
					LuaLib.lua_pushinteger(L, Convert.ToInt32(e.Value));
				} else if(vtn == "uint") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt32(e.Value));
				} else if(vtn == "long") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToInt64(e.Value));
				} else if(vtn == "ulong") {
					LuaLib.lua_pushinteger(L, (int)Convert.ToUInt64(e.Value));
				} else if(vtn == "float" || vtn == "double") {
					LuaLib.lua_pushnumber(L, Convert.ToDouble(e.Value));
				} else if(vtn == "string") {
					LuaLib.lua_pushstring(L, (string)e.Value);
				} else if(vtn == "void") {
					// impossible
					Debug.Assert(false, "void type in in list, that should not be possible");
				} else if(vt.IsEnum) {
					LuaLib.lua_pushinteger(L, (int)e.Value);
				} else if(vt.IsArray) {
					Array a = (Array)e.Value;
					list_to_luaval(L, a as IEnumerable);
				} else if(vt.IsList() && vt.IsEnumerable()) {
					list_to_luaval(L, e.Value as IEnumerable);
				} else if(vt.IsDictionary()) {
					dictionary_to_luaval(L, e.Value as IDictionary);
				} else {
					object_to_luaval(L, vtn, e.Value);
				}

				// set to dict
				LuaLib.lua_rawset(L, -3);
			}
		}

		public static void type_to_luaval<T>(IntPtr L, T v) {
			Type t = v.GetType();
			string tn = t.GetNormalizedName();

			if(tn == "byte") {
				LuaLib.lua_pushinteger(L, Convert.ToByte(v));
			} else if(tn == "sbyte") {
				LuaLib.lua_pushinteger(L, Convert.ToSByte(v));
			} else if(tn == "char") {
				LuaLib.lua_pushinteger(L, Convert.ToChar(v));
			} else if(tn == "short") {
				LuaLib.lua_pushinteger(L, Convert.ToInt16(v));
			} else if(tn == "ushort") {
				LuaLib.lua_pushinteger(L, Convert.ToUInt16(v));
			} else if(tn == "bool") {
				LuaLib.lua_pushboolean(L, Convert.ToBoolean(v));
			} else if(tn == "int" || tn == "decimal") {
				LuaLib.lua_pushinteger(L, Convert.ToInt32(v));
			} else if(tn == "uint") {
				LuaLib.lua_pushinteger(L, (int)Convert.ToUInt32(v));
			} else if(tn == "long") {
				LuaLib.lua_pushinteger(L, (int)Convert.ToInt64(v));
			} else if(tn == "ulong") {
				LuaLib.lua_pushinteger(L, (int)Convert.ToUInt64(v));
			} else if(tn == "float" || tn == "double") {
				LuaLib.lua_pushnumber(L, Convert.ToDouble(v));
			} else if(tn == "string") {
				LuaLib.lua_pushstring(L, Convert.ToString(v));
			} else if(tn == "void") {
				// do nothing for void
			} else if(t.IsEnum) {
				LuaLib.lua_pushinteger(L, Convert.ToInt32(v));
			} else if(t.IsArray) {
				Array a = v as Array;
				list_to_luaval(L, a as IEnumerable);
			} else if(t.IsList() && t.IsEnumerable()) {
				list_to_luaval(L, v as IEnumerable);
			} else if(t.IsDictionary()) {
				dictionary_to_luaval(L, v as IDictionary);
			} else {
				object_to_luaval(L, tn, v);
			}
		}

		public static int CompareOverload(IntPtr L, List<string> sigs1, List<string> sigs2) {
			// check last is params or not
			int sc1 = sigs1.Count;
			int sc2 = sigs2.Count;
			bool lastIsParams1 = sigs1.Count > 0 ? sigs1[sc1 - 1].StartsWith("params ") : false;
			bool lastIsParams2 = sigs2.Count > 0 ? sigs2[sc2 - 1].StartsWith("params ") : false;
			int argc = LuaLib.lua_gettop(L);

			// compare every parameter
			for(int i = 0; i < argc; i++) {
				// get native type info
				string sig1 = sigs1[Math.Min(i, sc1 - 1)];
				string sig2 = sigs2[Math.Min(i, sc2 - 1)];
				bool isLast1 = i >= sc1 - 1;
				bool isLast2 = i >= sc2 - 1;
				bool isParams1 = isLast1 && lastIsParams1;
				bool isParams2 = isLast2 && lastIsParams2;
				string tn1 = isParams1 ? sig1.Substring(7) : sig1;
				string tn2 = isParams2 ? sig2.Substring(7) : sig2;

				// if equals, quick continue
				if(tn1 == tn2) {
					continue;
				}

				// get type
				Type nt1 = ExtensionType.GetType(tn1);
				Type nt2 = ExtensionType.GetType(tn2);

				// check 
				LuaTypes luaType = (LuaTypes)LuaLib.lua_type(L, i + 1);
				switch(luaType) {
				case LuaTypes.LUA_TBOOLEAN:
					{
						if(tn1 == "System.Boolean") {
							return 1;
						} else if(tn2 == "System.Boolean") {
							return -1;
						}
						break;
					}
				case LuaTypes.LUA_TNUMBER:
					{
						// check int, double, float
						if(tn1 == "System.Int32") {
							return 1;
						} else if(tn2 == "System.Int32") {
							return -1;
						} else if(tn1 == "System.Double") {
							return 1;
						} else if(tn2 == "System.Double") {
							return -1;
						} else if(tn1 == "System.Float") {
							return 1;
						} else if(tn2 == "System.Float") {
							return -1;
						}

						// check implicit conversion between tn1 and tn2
						bool can1to2 = implicitConversionTable.ContainsKey(tn1) ? implicitConversionTable[tn1].ContainsKey(tn2) : false;
						bool can2to1 = implicitConversionTable.ContainsKey(tn2) ? implicitConversionTable[tn2].ContainsKey(tn1) : false;
						if(can1to2 && !can2to1) {
							return 1;
						} else if(!can1to2 && can2to1) {
							return -1;
						}

						// check fixed better conversion
						if(betterConversionTable.ContainsKey(tn1) && betterConversionTable[tn1].ContainsKey(tn2)) {
							return 1;
						} else if(betterConversionTable.ContainsKey(tn2) && betterConversionTable[tn2].ContainsKey(tn1)) {
							return -1;
						}
						break;
					}
				case LuaTypes.LUA_TSTRING:
					{
						if(tn1 == "System.String") {
							return 1;
						} else if(tn2 == "System.String") {
							return -1;
						}
						break;
					}
				case LuaTypes.LUA_TTABLE:
					{
						if(nt2.IsAssignableFrom(nt1)) {
							return 1;
						} else if(nt1.IsAssignableFrom(nt2)) {
							return -1;
						} else {
							// if the table is a delegate wrapper, check if there is a delegate type
							LuaLib.lua_pushstring(L, "handler");
							LuaLib.lua_gettable(L, i + 1);
							if(!LuaLib.lua_isnil(L, -1)) {
								LuaLib.lua_pop(L, 1);
								if(nt1.IsCustomDelegateType()) {
									return 1;
								} else if(nt2.IsCustomDelegateType()) {
									return -1;
								}
							}
						}
						break;
					}
				case LuaTypes.LUA_TUSERDATA:
					{
						if(isParams1 == isParams2) {
							if(isParams1) {
								Type et1 = nt1.GetElementType();
								Type et2 = nt2.GetElementType();
								if(et2.IsAssignableFrom(et1)) {
									return 1;
								} else if(et1.IsAssignableFrom(et2)) {
									return -1;
								}
							} else {
								if(nt2.IsAssignableFrom(nt1)) {
									return 1;
								} else if(nt1.IsAssignableFrom(nt2)) {
									return -1;
								}
							}
						} else {
							if(isParams1) {
								Type et1 = nt1.GetElementType();
								if(et1 == nt2) {
									return -1;
								} else if(nt2.IsAssignableFrom(et1)) {
									return 1;
								} else if(et1.IsAssignableFrom(nt2)) {
									return -1;
								}
							} else {
								Type et2 = nt2.GetElementType();
								if(nt1 == et2) {
									return 1;
								} else if(et2.IsAssignableFrom(nt1)) {
									return 1;
								} else if(nt1.IsAssignableFrom(et2)) {
									return -1;
								}
							}
						}
						break;
					}
				case LuaTypes.LUA_TNIL:
					{
						// check which one is more specific
						if(nt2.IsAssignableFrom(nt1)) {
							return 1;
						} else if(nt1.IsAssignableFrom(nt2)) {
							return -1;
						}
						break;
					}
				}
			}

			return 0;
		}

		public static bool CanLuaNativeMatch(IntPtr L, int lo, string t) {
			// get native type info
			bool isParams = t.StartsWith("params ");
			string nativeType = isParams ? t.Substring(7) : t;
			Type nt = ExtensionType.GetType(nativeType);
			Type et = isParams ? nt.GetElementType() : null;
			string etn = isParams ? et.GetNormalizedName() : null;

			// check based on lua type
			LuaTypes luaType = (LuaTypes)LuaLib.lua_type(L, lo);
			switch(luaType) {
			case LuaTypes.LUA_TBOOLEAN:
				{
					if(isParams) {
						if(etn == "System.Boolean") {
							return true;
						}
					} else if(nativeType == "System.Boolean") {
						return true;
					}
					break;
				}
			case LuaTypes.LUA_TNUMBER:
				{
					if(isParams) {
						if(etn == "System.Byte" ||
							etn == "System.SByte" ||
							etn == "System.Int16" ||
							etn == "System.UInt16" ||
							etn == "System.Int32" ||
							etn == "System.UInt32" ||
							etn == "System.Decimal" ||
							etn == "System.Int64" ||
							etn == "System.UInt64" ||
							etn == "System.Float" ||
							etn == "System.Double" ||
							etn == "System.Char") {
							return true;
						}
					} else if(nativeType == "System.Byte" ||
						nativeType == "System.SByte" ||
						nativeType == "System.Int16" ||
						nativeType == "System.UInt16" ||
						nativeType == "System.Int32" ||
						nativeType == "System.UInt32" ||
						nativeType == "System.Decimal" ||
						nativeType == "System.Int64" ||
						nativeType == "System.UInt64" ||
						nativeType == "System.Float" ||
						nativeType == "System.Double" ||
						nativeType == "System.Char") {
						return true;
					}
					break;
				}
			case LuaTypes.LUA_TSTRING:
				{
					if(isParams) {
						if(etn == "System.String") {
							return true;
						}
					} else if(nativeType == "System.String") {
						return true;
					}
					break;
				}
			case LuaTypes.LUA_TTABLE:
				{
					if(isParams) {
						if(et.IsList() || et.IsDictionary() || et.IsCustomDelegateType()) {
							return true;
						}
					} else if(nt.IsList() || nt.IsDictionary() || nt.IsCustomDelegateType()) {
						return true;
					}
					break;
				}
			case LuaTypes.LUA_TUSERDATA:
				{
					string typeName = LuaLib.tolua_typename(L, lo);
					Type lt = ExtensionType.GetType(typeName);
					if(isParams) {
						if(etn == typeName || et.IsAssignableFrom(lt)) {
							return true;
						}
					} else if(nativeType == typeName || nt.IsAssignableFrom(lt)) {
						return true;
					}
					break;
				}
			case LuaTypes.LUA_TNIL:
				{
					if(!nt.IsValueType) {
						return true;
					}
					break;
				}
			}

			// fallback
			return false;
		}
	}
}