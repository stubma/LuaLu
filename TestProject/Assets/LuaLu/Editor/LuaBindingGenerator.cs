namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using System.IO;
	using System;
	using System.Reflection;

	public class LuaBindingGenerator {
		private static List<string> INCLUDE_NAMESPACES;
		private static List<string> FUNDMENTAL_TYPES;

		static LuaBindingGenerator() {
			INCLUDE_NAMESPACES = new List<string> {
				"System"
			};
			FUNDMENTAL_TYPES = new List<string> {
				"byte",
				"sbyte",
				"char",
				"short",
				"ushort",
				"bool",
				"int",
				"uint",
				"long",
				"ulong",
				"float",
				"double",
				"decimal",
				"string"
			};
		}

		// types to be generated
		static List<Type> s_types;

		public static void GenerateUnityLuaBinding() {
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach(Assembly asm in assemblies) {
				Type[] types = asm.GetExportedTypes();
				foreach(Type t in types) {
					if(INCLUDE_NAMESPACES.Contains(t.Namespace)) {
						if(t.IsGenericType) {
							// TODO
						} else {
						}
					}
				}
			}

			// ensure folder exist
			if(!Directory.Exists(LuaConst.GENERATED_LUA_BINDING_PREFIX)) {
				Directory.CreateDirectory(LuaConst.GENERATED_LUA_BINDING_PREFIX);
			}

			// test code
			s_types = new List<Type>();
			s_types.Add(typeof(Component));
			s_types.Add(typeof(Type));
			s_types.Add(typeof(LuaComponent));
			GenerateTypesLuaBinding();

			// clean
			s_types = null;
		}

		private static void GenerateTypesLuaBinding() {
			// sort types
			s_types = SortTypes();

			// generate every type
			s_types.ForEach(t => { 
				Debug.Log("Start to generate type: " + t.FullName);
				GenerateOneTypeLuaBinding(t);
				Debug.Log(t.FullName + " Done");
			});

			// generate register class
			GenerateRegisterAll();

			// refresh
			AssetDatabase.Refresh();
		}

		private static List<Type> SortTypes() {
			List<Type> sortedTypes = new List<Type>();
			foreach(Type t in s_types) {
				List<Type> pList = SortParentTypes(t);
				pList.ForEach(p => {
					if(!sortedTypes.Contains(p)) {
						sortedTypes.Add(p);
					}
				});
			}
			return sortedTypes;
		}

		private static List<Type> SortParentTypes(Type t) {
			List<Type> sortedParents = new List<Type>();
			if(t.BaseType != null) {
				List<Type> pList = SortParentTypes(t.BaseType);
				pList.ForEach(p => sortedParents.Add(p));
			}
			sortedParents.Add(t);
			return sortedParents;
		}

		private static void GenerateRegisterAll() {
			string clazz = "lua_register_unity";
			string path = LuaConst.GENERATED_LUA_BINDING_PREFIX + clazz + ".cs";
			string buffer = "";

			// namepace and class start
			buffer += "namespace LuaLu {\n";
			buffer += "\tusing System;\n";
			buffer += "\tusing System.IO;\n";
			buffer += "\tusing System.Collections;\n";
			buffer += "\tusing System.Collections.Generic;\n";
			buffer += "\tusing UnityEngine;\n";
			buffer += "\tusing LuaInterface;\n";
			buffer += string.Format("\n\tpublic class {0} {{\n", clazz);

			// register method
			buffer += "\t\tpublic static int RegisterAll(IntPtr L) {\n";
			buffer += "\t\t\tLuaLib.tolua_open(L);\n";
			buffer += "\t\t\tLuaLib.tolua_module(L, null, 0);\n";
			buffer += "\t\t\tLuaLib.tolua_beginmodule(L, null);\n";
			foreach(Type t in s_types) {
				string tfn = t.FullName;
				string tClass = "lua_unity_" + tfn.Replace(".", "_") + "_auto";
				buffer += string.Format("\t\t\t{0}.__Register__(L);\n", tClass);
			}
			buffer += "\t\t\tLuaLib.tolua_endmodule(L);\n";
			buffer += "\t\t\treturn 1;\n";
			buffer += "\t\t}\n";

			// close class and namespace
			buffer += "\t}\n}";

			// write to file
			File.WriteAllText(path, buffer);
		}

		private static void GenerateOneTypeLuaBinding(Type t) {
			string tn = t.Name;
			string tfn = t.FullName;
			string[] nsList = tfn.Split(new Char[] { '.' });
			Type bt = t.BaseType;
			string btfn = bt != null ? bt.FullName : "";
			Array.Resize(ref nsList, nsList.Length - 1);
			string tfnUnderscore = tfn.Replace(".", "_");
			string clazz = "lua_unity_" + tfnUnderscore + "_auto";
			string path = LuaConst.GENERATED_LUA_BINDING_PREFIX + clazz + ".cs";
			string buffer = "";

			// namepace and class start
			buffer += "namespace LuaLu {\n";
			buffer += "\tusing System;\n";
			buffer += "\tusing System.IO;\n";
			buffer += "\tusing System.Collections;\n";
			buffer += "\tusing System.Collections.Generic;\n";
			buffer += "\tusing UnityEngine;\n";
			buffer += "\tusing LuaInterface;\n";
			buffer += string.Format("\n\tpublic class {0} {{\n", clazz);

			// get constructors
			ConstructorInfo[] ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			if(ctors.Length > 0) {
				buffer += GenerateConstructor(t, ctors);
			}

			// methods
			MethodInfo[] methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);

			// filter generic methods and property setter/getter
			List<MethodInfo> publicMethods = new List<MethodInfo>();
			Array.ForEach<MethodInfo>(methods, m => {
				if(!m.IsGenericMethod) {
					if(!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_")) {
						publicMethods.Add(m);
					}
				}
			});

			// group it by name
			Dictionary<string, List<MethodInfo>> publicMethodMap = new Dictionary<string, List<MethodInfo>>();
			publicMethods.ForEach(m => {
				List<MethodInfo> mList = null;
				if(publicMethodMap.ContainsKey(m.Name)) {
					mList = publicMethodMap[m.Name];
				} else {
					mList = new List<MethodInfo>();
					publicMethodMap[m.Name] = mList;
				}
				mList.Add(m);
			});

			// generate for public methods
			foreach(List<MethodInfo> mList in publicMethodMap.Values) {
				buffer += GenerateInstanceMethod(t, mList);
			}

			// register method
			buffer += "\t\tpublic static int __Register__(IntPtr L) {\n";
			buffer += string.Format("\t\t\tLuaLib.tolua_usertype(L, \"{0}\");\n", tfn);
			foreach(string ns in nsList) {
				buffer += string.Format("\t\t\tLuaLib.tolua_module(L, \"{0}\", 0);\n", ns);
				buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", ns);
			}
			buffer += string.Format("\t\t\tLuaLib.tolua_class(L, \"{0}\", \"{1}\", null);\n", tfn, btfn);
			buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", tn);
			if(ctors.Length > 0) {
				buffer += "\t\t\tLuaLib.tolua_function(L, \"new\", new LuaFunction(__Constructor__));\n";
			}
			foreach(string mn in publicMethodMap.Keys) {
				buffer += string.Format("\t\t\tLuaLib.tolua_function(L, \"{0}\", new LuaFunction({0}));\n", mn);
			}
			buffer += "\t\t\tLuaLib.tolua_endmodule(L);\n";
			for(int i = 0; i < nsList.Length; i++) {
				buffer += "\t\t\tLuaLib.tolua_endmodule(L);\n";
			}
			buffer += "\t\t\treturn 1;\n";
			buffer += "\t\t}\n";

			// close class and namespace
			buffer += "\t}\n}";

			// write to file
			File.WriteAllText(path, buffer);
		}

		private static string GenerateConstructor(Type t, ConstructorInfo[] mList) {
			ConstructorInfo m = mList[0];
			string tfn = t.FullName;
			string buffer = "";

			// constructor
			buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
			buffer += "\t\tpublic static int __Constructor__(IntPtr L) {\n";
			buffer += "\t\t\treturn 0;\n";
			buffer += "\t\t}\n\n";

			// return
			return buffer;
		}

		private static string GenerateInstanceMethod(Type t, List<MethodInfo> mList) {
			string mn = mList[0].Name;
			string tn = t.Name;
			string tfn = t.FullName;
			string buffer = "";
			string tfnUnderscore = tfn.Replace(".", "_");
			string clazz = "lua_unity_" + tfnUnderscore + "_auto";
			string fn = clazz + "." + mn;

			// group method by parameter count, mind optional parameter
			Dictionary<int, List<MethodInfo>> mpMap = new Dictionary<int, List<MethodInfo>>();
			mList.ForEach(m => {
				ParameterInfo[] pList = m.GetParameters();
				int maxArg = pList.Length;
				int minArg = maxArg;
				foreach(ParameterInfo pi in pList) {
					if(pi.IsOptional) {
						minArg--;
					}
				}
				for(int i = minArg; i <= maxArg; i++) {
					List<MethodInfo> ml = null;
					if(mpMap.ContainsKey(i)) {
						ml = mpMap[i];
					} else {
						ml = new List<MethodInfo>();
						mpMap[i] = ml;
					}
					ml.Add(m);
				}
			});

			// method
			buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
			buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", mn);
			buffer += "\t\t\t// variables\n";
			buffer += "\t\t\tint argc = 0;\n";
			buffer += string.Format("\t\t\t{0} obj = null;\n", tfn);
			buffer += "\t\t\tbool ok = true;\n";
			buffer += "\t\t#if DEBUG\n";
			buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
			buffer += "\t\t#endif\n";
			buffer += "\n";
			buffer += "\t\t#if DEBUG\n";
			buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", 0, ref err)) {{\n", tfn);
			buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
			buffer += "\t\t\t}\n";
			buffer += "\t\t#endif\n";
			buffer += "\t\t\tIntPtr handler = LuaLib.tolua_tousertype(L, 1, IntPtr.Zero);\n";
			buffer += "\t\t\tint hash = handler.ToInt32();\n";
			buffer += string.Format("\t\t\tobj = ({0})NativeObjectMap.FindObject(hash);\n", tfn);
			buffer += "\t\t#if DEBUG\n";
			buffer += "\t\t\tif(obj == null) {\n";
			buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"invalid 'cobj' in function '{0}'\", ref err);\n", fn);
			buffer += "\t\t\t}\n";
			buffer += "\t\t#endif\n";
			buffer += "\n";
			buffer += "\t\t\t// get argument count\n";
			buffer += "\t\t\targc = LuaLib.lua_gettop(L) - 1;\n";
			buffer += "\n";
			foreach(int c in mpMap.Keys) {
				// check argument count
				buffer += "\t\t\t// if argument count matched, call\n";
				buffer += string.Format("\t\t\tif(argc == {0}) {{\n", c);

				// check method count with same parameter count
				MethodInfo callM = null;
				List<MethodInfo> ml = mpMap[c];
				if(ml.Count > 1) {
					// TODO check with method to be called by parameter type
					callM = ml[0];
				} else {
					// only one method, so it is simple, just pick the only one
					callM = ml[0];
				}

				// parameters
				ParameterInfo[] pList = callM.GetParameters();

				// argument handling
				if(c > 0) {
					// argument declaration
					buffer += "\t\t\t\t// arguments declaration\n";
					for(int i = 0; i < pList.Length; i++) {
						Type pt = pList[i].ParameterType;
						string ptn = GetTypeName(pt);
						buffer += string.Format("\t\t\t\t{0} arg{1} = default({0});\n", ptn, i);
					}

					// argument conversion
					buffer += "\n";
					buffer += "\t\t\t\t// convert lua value to desired arguments\n";
					for(int i = 0; i < pList.Length; i++) {
						buffer += GenerateUnboxParameters(pList[i], i, tn + "." + mn);
					}

					// check conversion
					buffer += "\n";
					buffer += "\t\t\t\t// if conversion is not ok, print error and return\n";
					buffer += "\t\t\t\tif(!ok) {\n";
					buffer += string.Format("\t\t\t\t\tLuaLib.tolua_error(L, \"invalid arguments in function '{0}'\", ref err);\n", fn);
					buffer += "\t\t\t\t\treturn 0;\n";
					buffer += "\t\t\t\t}\n";
				}

				// call function
				Type rt = callM.ReturnType;
				string rtn = GetTypeName(rt);
				buffer += "\n";
				buffer += "\t\t\t\t// call function\n";
				buffer += "\t\t\t\t";
				if(rtn != "System.Void") {
					buffer += string.Format("{0} ret = ", rtn);
				}
				buffer += string.Format("obj.{0}(", mn);
				for(int i = 0; i < pList.Length; i++) {
					if(pList[i].IsOut) {
						buffer += "out ";
					}
					buffer += string.Format("arg{0}", i);
					if(i < pList.Length - 1) {
						buffer += ", ";
					}
				}
				buffer += ");\n";

				// push returned value
				buffer += GenerateBoxReturnValue(callM);

				// close if
				buffer += "\t\t\t}\n\n";
			}
			buffer += "\t\t\treturn 0;\n";
			buffer += "\t\t}\n\n";

			// return
			return buffer;
		}

		private static string GenerateBoxReturnValue(MethodInfo m) {
			Type rt = m.ReturnType;
			string rtn = GetTypeName(rt);
			string buffer = "";

			// convert to lua value
			if(FUNDMENTAL_TYPES.Contains(rtn)) {
				buffer += string.Format("\t\t\t\tLuaValueBoxer.{0}_to_luaval(L, ret);\n", rtn);
			} else if(rtn == "System.Void") {
				// do nothing for void
			} else if(rt.IsEnum) {
				buffer += "\t\t\t\tLuaValueBoxer.int_to_luaval(L, (int)ret);\n";
			} else if(rt.IsArray) {
				Type et = rt.GetElementType();
				string etn = GetTypeName(et);
				if(FUNDMENTAL_TYPES.Contains(etn)) {
					buffer += string.Format("\t\t\t\tLuaValueBoxer.{0}_array_to_luaval(L, ret);\n", etn);
				} else if(et.IsEnum) {
					buffer += string.Format("\t\t\t\tLuaValueBoxer.enum_array_to_luaval<{0}>(L, ret);\n", etn);
				} else if(et.IsArray) {
					// TODO more than one dimension array? not supported yet
				} else {
					buffer += string.Format("\t\t\t\tLuaValueBoxer.object_array_to_luaval<{0}>(L, \"{0}\", ret);\n", etn);
				}
			} else {
				buffer += string.Format("\t\t\t\tLuaValueBoxer.object_to_luaval<{0}>(L, \"{0}\", ret);\n", rtn);
			}

			// return
			return buffer;
		}

		private static string GenerateUnboxParameters(ParameterInfo pi, int argIndex, string methodName) {
			// if parameter is out, no need unbox it
			if(pi.IsOut) {
				// TODO need remember this parameter and use multiret
				return "";
			}

			// variables
			string buffer = "";
			Type pt = pi.ParameterType;
			string ptn = GetTypeName(pt);

			// find conversion by parameter type name
			if(FUNDMENTAL_TYPES.Contains(ptn)) {
				buffer += string.Format("\t\t\t\tok &= LuaValueBoxer.luaval_to_{0}(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + 2, argIndex, methodName);
			} else if(pt.IsEnum) {
				buffer += string.Format("\t\t\t\tok &= LuaValueBoxer.luaval_to_enum<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + 2, argIndex, methodName);
			} else if(pt.IsArray) {
				Type et = pt.GetElementType();
				string etn = GetTypeName(et);
				if(FUNDMENTAL_TYPES.Contains(etn)) {
					buffer += string.Format("\t\t\t\tok &= LuaValueBoxer.luaval_to_{0}_array(L, {1}, out arg{2}, \"{3}\");\n", etn, argIndex + 2, argIndex, methodName);
				} else if(et.IsEnum) {
					buffer += string.Format("\t\t\t\tok &= LuaValueBoxer.luaval_to_enum_array<{0}>(L, {1}, out arg{2}, \"{3}\");\n", etn, argIndex + 2, argIndex, methodName);
				} else if(et.IsArray) {
					// TODO more than one dimension array? not supported yet
				} else {
					buffer += string.Format("\t\t\t\tok &= LuaValueBoxer.luaval_to_object_array<{0}>(L, {1}, \"{0}\", out arg{2}, \"{3}\");\n", etn, argIndex + 2, argIndex, methodName);
				}
			} else {
				buffer += string.Format("\t\t\t\tok &= LuaValueBoxer.luaval_to_object<{0}>(L, {1}, \"{0}\", out arg{2}, \"{3}\");\n", ptn, argIndex + 2, argIndex, methodName);
			}

			// return
			return buffer;
		}

		private static string[] GetGenericName(Type[] types) {
			string[] results = new string[types.Length];
			for(int i = 0; i < types.Length; i++) {
				if(types[i].IsGenericType) {
					results[i] = GetGenericName(types[i]);
				} else {
					results[i] = GetTypeName(types[i]);
				}
			}
			return results;
		}

		private static string GetGenericName(Type t) {
			if(t.GetGenericArguments().Length == 0) {
				return t.FullName;
			}
			Type[] gArgs = t.GetGenericArguments();
			string typeName = t.FullName;
			string pureTypeName = typeName.Substring(0, typeName.IndexOf('`'));
			return pureTypeName + "<" + string.Join(",", GetGenericName(gArgs)) + ">";
		}

		private static string GetTypeName(Type t) {
			if(t.IsArray) {
				t = t.GetElementType();
				string str = GetTypeName(t);
				str += "[]";
				return str;
			} else if(t.IsGenericType) {
				return GetGenericName(t);
			} else {
				return NormalizeTypeName(t.FullName);            
			}
		}

		private static string NormalizeTypeName(string str) {
			if(str.Length > 1 && str[str.Length - 1] == '&') {
				str = str.Remove(str.Length - 1);            
			}

			if(str == "System.Single" || str == "Single") {            
				return "float";
			} else if(str == "System.String" || str == "String") {
				return "string";
			} else if(str == "System.Int32" || str == "Int32") {
				return "int";
			} else if(str == "System.Int64" || str == "Int64") {
				return "long";
			} else if(str == "System.SByte" || str == "SByte") {
				return "sbyte";
			} else if(str == "System.Byte" || str == "Byte") {
				return "byte";
			} else if(str == "System.Int16" || str == "Int16") {
				return "short";
			} else if(str == "System.UInt16" || str == "UInt16") {
				return "ushort";
			} else if(str == "System.Char" || str == "Char") {
				return "char";
			} else if(str == "System.UInt32" || str == "UInt32") {
				return "uint";
			} else if(str == "System.UInt64" || str == "UInt64") {
				return "ulong";
			} else if(str == "System.Decimal" || str == "Decimal") {
				return "decimal";
			} else if(str == "System.Double" || str == "Double") {
				return "double";
			} else if(str == "System.Boolean" || str == "Boolean") {
				return "bool";
			}

			if(str.Contains("+")) {
				return str.Replace('+', '.');
			}

			return str;
		}
	}
}