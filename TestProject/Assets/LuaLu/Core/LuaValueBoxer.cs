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
			   tn == "ulong") {
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
			} else if(tn == "char" || tn == "string") {
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

		public static bool luaval_to_array<T>(IntPtr L, int lo, out T[] ret, string funcName = "") {
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
			if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
				#if DEBUG
				luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
				#endif
				ok = false;
			}

			// fill elements
			if(ok) {
				// element type
				Type t = typeof(T);
				string tn = t.GetNormalizedName();

				// iterate all elements
				int len = LuaLib.lua_objlen(L, lo);
				ret = new T[len];
				for(int i = 0; i < len; i++) {
					LuaLib.lua_pushnumber(L, i + 1);
					LuaLib.lua_gettable(L, lo);

					// convert value
					T element;
					ok = luaval_to_type<T>(L, -1, out element, funcName);
					if(ok) {
						ret[i] = element;
					}

					// if failed, assert fail
					if(!ok) {
						Debug.Assert(false, string.Format("luaval_to_array: some element are not type: {0}", tn));
					}

					// pop value
					LuaLib.lua_pop(L, 1);
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
					LuaLib.lua_pushstring(L, Convert.ToChar(e.Current).ToString());
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
				} else if(t.IsEnum) {
					LuaLib.lua_pushinteger(L, (int)e.Current);
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
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "sbyte") {
					LuaLib.lua_pushinteger(L, Convert.ToSByte(e.Value));
					LuaLib.lua_rawset(L, -3);
				} else if(vtn == "char") {
					LuaLib.lua_pushstring(L, Convert.ToChar(e.Value).ToString());
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
				} else if(vt.IsList() && vt.IsEnumerable()) {
					list_to_luaval(L, e.Value as IEnumerable);
					LuaLib.lua_rawset(L, -3);
				} else if(vt.IsDictionary()) {
					dictionary_to_luaval(L, e.Value as IDictionary);
					LuaLib.lua_rawset(L, -3);
				} else {
					object_to_luaval(L, vtn, e.Value);
					LuaLib.lua_rawset(L, -3);
				}
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
				LuaLib.lua_pushstring(L, Convert.ToChar(v).ToString());
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
			} else if(t.IsVoid()) {
				// do nothing for void
			} else if(t.IsEnum) {
				LuaLib.lua_pushinteger(L, Convert.ToInt32(v));
			} else if(t.IsList() && t.IsEnumerable()) {
				list_to_luaval(L, v as IEnumerable);
			} else if(t.IsDictionary()) {
				dictionary_to_luaval(L, v as IDictionary);
			} else {
				object_to_luaval(L, tn, v);
			}
		}

		public static void GetLuaParameterTypes(IntPtr L, out int[] types, bool isStatic) {
			int argc = LuaLib.lua_gettop(L);
			if(!isStatic) {
				argc--;
			}
			types = new int[argc];
			for(int i = 0; i < argc; i++) {
				types[i] = LuaLib.lua_type(L, i + 1 + (isStatic ? 0 : 1));
			}
		}

		public static bool CheckParameterType(IntPtr L, int[] luaTypes, string[] typeFullNames, bool isStatic, bool fuzzy = false) {
			// first time we perform accurate match
			int argc = luaTypes.Length;
			bool matched = true;
			if(typeFullNames != null && typeFullNames.Length >= argc) {
				for(int i = 0; i < argc; i++) {
					int luaType = luaTypes[i];
					string tfn = typeFullNames[i];
					if(luaType == (int)LuaTypes.LUA_TBOOLEAN) {
						if(fuzzy) {
							if(tfn != "System.Boolean" &&
							   tfn != "System.Byte" &&
							   tfn != "System.SByte" &&
							   tfn != "System.Int16" &&
							   tfn != "System.Int32" &&
							   tfn != "System.Int64" &&
							   tfn != "System.UInt16" &&
							   tfn != "System.UInt32" &&
							   tfn != "System.UInt64") {
								matched = false;
								break;
							}
						} else if(tfn != "System.Boolean") {
							matched = false;
							break;
						}
					} else if(luaType == (int)LuaTypes.LUA_TNUMBER) {
						if(fuzzy) {
							if(tfn != "System.Boolean" &&
							   tfn != "System.Byte" &&
							   tfn != "System.SByte" &&
							   tfn != "System.Int16" &&
							   tfn != "System.Int32" &&
							   tfn != "System.Int64" &&
							   tfn != "System.UInt16" &&
							   tfn != "System.UInt32" &&
							   tfn != "System.UInt64" &&
							   tfn != "System.Decimal" &&
							   tfn != "System.Double" &&
							   tfn != "System.Single") {
								matched = false;
								break;
							}
						} else if(tfn != "System.Int16" &&
						          tfn != "System.Int32" &&
						          tfn != "System.Int64" &&
						          tfn != "System.UInt16" &&
						          tfn != "System.UInt32" &&
						          tfn != "System.UInt64" &&
						          tfn != "System.Decimal" &&
						          tfn != "System.Double" &&
						          tfn != "System.Single") {
							matched = false;
							break;
						}
					} else if(luaType == (int)LuaTypes.LUA_TSTRING) {
						if(fuzzy) {
							if(tfn != "System.String" && tfn != "System.Char") {
								matched = false;
								break;
							} else if(tfn == "System.Char") {
								string arg = LuaLib.lua_tostring(L, i + (isStatic ? 1 : 2));
								if(arg.Length != 1) {
									matched = false;
									break;
								}
							}
						} else if(tfn != "System.String") {
							matched = false;
							break;
						}
					} else if(luaType == (int)LuaTypes.LUA_TTABLE) {
						Type t = ExtensionType.GetType(tfn);
						if(!t.IsList() && !t.IsDictionary()) {
							matched = false;
							break;
						}
					} else if(luaType == (int)LuaTypes.LUA_TUSERDATA) {
						string typeName = LuaLib.tolua_typename(L, i + (isStatic ? 1 : 2));
						if(fuzzy) {
							Type nt = ExtensionType.GetType(tfn);
							Type lt = ExtensionType.GetType(typeName);
							if(typeName != tfn && !nt.IsAssignableFrom(lt)) {
								matched = false;
								break;
							}
						} else if(typeName != tfn) {
							matched = false;
							break;
						}
					} else if(luaType == (int)LuaTypes.LUA_TNIL) {
						Type t = ExtensionType.GetType(tfn);
						if(fuzzy) {
							if(t.IsPrimitive &&
							   tfn != "System.Boolean" &&
							   tfn != "System.Byte" &&
							   tfn != "System.SByte" &&
							   tfn != "System.Int16" &&
							   tfn != "System.Int32" &&
							   tfn != "System.Int64" &&
							   tfn != "System.UInt16" &&
							   tfn != "System.UInt32" &&
							   tfn != "System.UInt64" &&
							   tfn != "System.Decimal" &&
							   tfn != "System.Double" &&
							   tfn != "System.Single") {
								matched = false;
								break;
							}
						} else if(t.IsPrimitive) {
							matched = false;
							break;
						}
					}
				}
			}

			return matched;
		}
	}
}