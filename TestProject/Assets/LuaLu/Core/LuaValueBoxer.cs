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
						{ "System.Single", "System.Single"},
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
						{ "System.Single" , "System.Single" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{ 
					"System.Int16",
					new Dictionary<string, string> {
						{ "System.Int32", "System.Int32" },
						{ "System.Int64", "System.Int64" },
						{ "System.Single" , "System.Single" },
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
						{ "System.Single" , "System.Single" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Int32",
					new Dictionary<string, string> {
						{ "System.Int64", "System.Int64" },
						{ "System.Single" , "System.Single" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.UInt32",
					new Dictionary<string, string> {
						{ "System.Int64", "System.Int64" },
						{ "System.UInt64", "System.UInt64" },
						{ "System.Single" , "System.Single" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Int64",
					new Dictionary<string, string> {
						{ "System.Single" , "System.Single" },
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
						{ "System.Single" , "System.Single" },
						{ "System.Double", "System.Double" },
						{ "System.Decimal", "System.Decimal" }
					}
				},
				{
					"System.Single",
					new Dictionary<string, string> {
						{ "System.Double", "System.Double" }
					}
				},
				{
					"System.UInt64",
					new Dictionary<string, string> {
						{ "System.Single" , "System.Single" },
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

		public static object luaval_to_type(IntPtr L, int lo, string tn, string funcName = "") {
			// get top
			int top = LuaLib.lua_gettop(L);

			// ensure lo is positive
			if(lo < 0) {
				lo = top + lo + 1;
			}

			// basic checking
			if(IntPtr.Zero == L || top < lo) {
				return null;
			}

			// get type
			Type t = ExtensionType.GetType(tn);

			// convert
			bool ok = true;
			if(tn == "System.Byte" ||
			   tn == "System.SByte" ||
			   tn == "System.Int16" ||
			   tn == "System.UInt16" ||
			   tn == "System.Int32" ||
			   tn == "System.UInt32" ||
			   tn == "System.Decimal" ||
			   tn == "System.Int64" ||
			   tn == "System.UInt64" ||
			   tn == "System.Char") {
				// top should be a number
				if(!LuaLib.tolua_isnumber(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;

					// to integer then cast to desired type
					if(ok) {
						return Convert.ChangeType(LuaLib.tolua_tointeger(L, lo, 0), t);
					}
				}
			} else if(tn == "System.Boolean") {
				// top should be a boolean
				if(!LuaLib.tolua_isboolean(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to boolean
				if(ok) {
					return LuaLib.tolua_toboolean(L, lo, 0);
				}
			} else if(tn == "System.String") {
				// top should be a string
				if(!LuaLib.tolua_isstring(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to string
				if(ok) {
					return LuaLib.tolua_tostring(L, lo, "");
				}
			} else if(tn == "System.Single" || tn == "System.Double") {
				// top should be a number
				if(!LuaLib.tolua_isnumber(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to number
				if(ok) {
					return Convert.ChangeType(LuaLib.tolua_tonumber(L, lo, 0), t);
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
					return Enum.ToObject(t, LuaLib.tolua_tointeger(L, lo, 0));
				}
			} else if(t.IsArray) {
				// if top is a table, then convert this table to array
				// if top is not table, then convert all elements from lo to top to array
				// that is because an array may be a variant parameter
				bool flat = false;
				if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
					flat = true;
				}

				// create array
				Type et = t.GetElementType();
				string etn = et.GetNormalizedTypeName();
				int len = flat ? (top - lo + 1) : LuaLib.lua_objlen(L, lo);
				Array a = Array.CreateInstance(et, len);

				// add element to array
				for(int i = 0; i < len; i++) {
					// if not flat, get value from table
					if(!flat) {
						LuaLib.lua_pushnumber(L, i + 1);
						LuaLib.lua_gettable(L, lo);
					}

					// convert value
					object element = luaval_to_type(L, flat ? (lo + i) : -1, etn, funcName);
					if(element != null) {
						a.SetValue(element, i);
					}

					// pop value if not flat
					if(!flat) {
						LuaLib.lua_pop(L, 1);
					}
				}

				// return
				return a;
			} else if(tn == "System.Array") {
				// top should be a table
				if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// fill elements
				if(ok) {
					int len = LuaLib.lua_objlen(L, lo);
					Array a = Array.CreateInstance(typeof(object), len);
					for(int i = 0; i < len; i++) {
						LuaLib.lua_pushnumber(L, i + 1);
						LuaLib.lua_gettable(L, lo);

						// get value and push to array
						int lt = LuaLib.lua_type(L, -1);
						if(lt == (int)LuaTypes.LUA_TBOOLEAN) {
							a.SetValue(LuaLib.tolua_toboolean(L, -1, 0), i);
						} else if(lt == (int)LuaTypes.LUA_TNUMBER) {
							a.SetValue(LuaLib.tolua_tonumber(L, -1, 0), i);
						} else if(lt == (int)LuaTypes.LUA_TSTRING) {
							a.SetValue(LuaLib.tolua_tostring(L, -1, ""), i);
						} else if(lt == (int)LuaTypes.LUA_TUSERDATA) {
							string ltn = LuaLib.tolua_typename(L, -1);
							if(luaval_is_usertype(L, -1, ltn)) {
								// to obj
								int refId = LuaLib.tolua_tousertype(L, -1);
								object obj = LuaStack.FromState(L).FindObject(refId);
								if(obj != null) {
									a.SetValue(obj, i);
								}
							}
						}

						// pop value
						LuaLib.lua_pop(L, 1);
					}

					// return
					return a;
				}
			} else if(t.IsList()) {
				// top should be a table
				if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to list
				if(ok) {
					// get element type
					Type et = typeof(object);
					string etn = "System.Object";
					if(t.IsGenericType) {
						et = t.GetGenericArguments()[0];
						etn = et.GetNormalizedTypeName();
					}

					// create list instance, if original type is an abstract type, create 
					// a list for it
					IList list = null;
					if(t.IsAbstract) {
						Type listType = typeof(List<>).MakeGenericType(et);
						list = Activator.CreateInstance(listType) as IList;
					} else {
						list = Activator.CreateInstance(t) as IList;
					}

					// add element
					int len = LuaLib.lua_objlen(L, lo);
					for(int i = 0; i < len; i++) {
						// get value from table
						LuaLib.lua_pushnumber(L, i + 1);
						LuaLib.lua_gettable(L, lo);

						// convert value
						object element = luaval_to_type(L, -1, etn, funcName);
						if(element != null) {
							list.Add(element);
						}

						// pop index
						LuaLib.lua_pop(L, 1);
					}

					// return
					return list;
				}
			} else if(t.IsDictionary()) {
				// top should be a table
				if(!LuaLib.tolua_istable(L, lo, ref tolua_err)) {
					#if DEBUG
					luaval_to_native_err(L, "#ferror:", ref tolua_err, funcName);
					#endif
					ok = false;
				}

				// to dictionary
				if(ok) {
					// get key/value type
					Type kt = typeof(object);
					Type vt = typeof(object);
					string ktn = "System.Object";
					string vtn = "System.Object";
					if(t.IsGenericType) {
						Type[] gt = t.GetGenericArguments();
						kt = gt[0];
						vt = gt[1];
						ktn = kt.GetNormalizedTypeName();
						vtn = vt.GetNormalizedTypeName();
					}

					// create dictionary instance
					IDictionary dict = null;
					if(t.IsAbstract) {
						Type dictType = typeof(Dictionary<,>).MakeGenericType(kt, vt);
						dict = Activator.CreateInstance(dictType) as IDictionary;
					} else {
						dict = Activator.CreateInstance(t) as IDictionary;
					}

					// add pairs
					LuaLib.lua_pushnil(L);
					while(LuaLib.lua_next(L, lo) != 0) {
						// get key
						object k = LuaValueBoxer.luaval_to_type(L, -2, ktn, funcName);

						// get value
						object v = LuaValueBoxer.luaval_to_type(L, -1, vtn, funcName);

						// add
						dict.Add(k, v);

						// pop value
						LuaLib.lua_pop(L, 1);
					}

					// return
					return dict;
				}
			} else {
				// top should be a user type
				if(!luaval_is_usertype(L, lo, tn)) {
					ok = false;
				}

				// to obj
				if(ok) {
					int refId = LuaLib.tolua_tousertype(L, lo);
					object obj = LuaStack.FromState(L).FindObject(refId);
					return obj;
				}
			}

			// fallback, return null
			return null;
		}

		public static void type_to_luaval(IntPtr L, object v, bool flat = false) {
			// check nil
			if(v == null) {
				LuaLib.lua_pushnil(L);
				return;
			}

			// get type
			Type t = v.GetType();
			string tn = t.GetNormalizedTypeName();

			if(tn == "System.Byte") {
				LuaLib.lua_pushinteger(L, Convert.ToByte(v));
			} else if(tn == "System.SByte") {
				LuaLib.lua_pushinteger(L, Convert.ToSByte(v));
			} else if(tn == "System.Char") {
				LuaLib.lua_pushinteger(L, Convert.ToChar(v));
			} else if(tn == "System.Int16") {
				LuaLib.lua_pushinteger(L, Convert.ToInt16(v));
			} else if(tn == "System.UInt16") {
				LuaLib.lua_pushinteger(L, Convert.ToUInt16(v));
			} else if(tn == "System.Boolean") {
				LuaLib.lua_pushboolean(L, Convert.ToBoolean(v));
			} else if(tn == "System.Int32" || tn == "System.Decimal" || tn == "System.Int64") {
				LuaLib.lua_pushinteger(L, Convert.ToInt32(v));
			} else if(tn == "System.UInt32" || tn == "System.UInt64") {
				LuaLib.lua_pushinteger(L, (int)Convert.ToUInt32(v));
			} else if(tn == "System.Single" || tn == "System.Double") {
				LuaLib.lua_pushnumber(L, Convert.ToDouble(v));
			} else if(tn == "System.String") {
				LuaLib.lua_pushstring(L, Convert.ToString(v));
			} else if(tn == "System.Void") {
				// do nothing for void
			} else if(t.IsEnum) {
				LuaLib.lua_pushinteger(L, Convert.ToInt32(v));
			} else if(t.IsArray) {
				// new table if not flat
				if(!flat) {
					LuaLib.lua_newtable(L);
				}

				// cast to array and push every element
				int indexTable = 1;
				Array a = v as Array;
				IEnumerator e = a.GetEnumerator();
				while(e.MoveNext()) {
					// if not flat, assign a table index
					if(!flat) {
						LuaLib.lua_pushnumber(L, indexTable);
					}

					// push to stack
					type_to_luaval(L, e.Current);

					// if not flat, add to table
					if(!flat) {
						LuaLib.lua_rawset(L, -3);
						++indexTable;
					}
				}
			} else if(t.IsList()) {
				// new table if not flat
				if(!flat) {
					LuaLib.lua_newtable(L);
				}

				// cast to list and push every element
				int indexTable = 1;
				IList list = v as IList;
				for(int i = 0; i < list.Count; i++) {
					// if not flat, assign a table index
					if(!flat) {
						LuaLib.lua_pushnumber(L, indexTable);
					}

					// push to stack
					type_to_luaval(L, list[i]);

					// if not flat, add to table
					if(!flat) {
						LuaLib.lua_rawset(L, -3);
						++indexTable;
					}
				}
			} else if(t.IsDictionary()) {
				// get key/value type
				string ktn = "System.Object";
				if(t.IsGenericType) {
					ktn = t.GetGenericArguments()[0].GetNormalizedTypeName();
				}

				// new table for dictionary value
				LuaLib.lua_newtable(L);

				// if key type is not int or string, do nothing
				if(ktn != "System.Byte" &&
					ktn != "System.SByte" &&
					ktn != "System.Int16" &&
					ktn != "System.UInt16" &&
					ktn != "System.Int32" &&
					ktn != "System.UInt32" &&
					ktn != "System.Decimal" &&
					ktn != "System.Int64" &&
					ktn != "System.UInt64" &&
					ktn != "System.Char" && 
					ktn != "System.String") {
					// just leave an empty table
					return;
				}

				// push
				IDictionary dict = v as IDictionary;
				IDictionaryEnumerator e = dict.GetEnumerator();
				while(e.MoveNext()) {
					// push key and value
					type_to_luaval(L, e.Key);
					type_to_luaval(L, e.Value);

					// set to dict
					LuaLib.lua_rawset(L, -3);
				}
			} else {
				LuaStack.SharedInstance().PushObject(v, t.GetNormalizedCodeName());
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
						} else if(tn1 == "System.Single") {
							return 1;
						} else if(tn2 == "System.Single") {
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
			string etn = isParams ? et.GetNormalizedCodeName() : null;

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
							etn == "System.Single" ||
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
						nativeType == "System.Single" ||
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