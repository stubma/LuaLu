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

		static LuaBindingGenerator() {
			INCLUDE_NAMESPACES = new List<string> {
				"System"
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
			s_types.Add(typeof(GameObject));
			s_types.Add(typeof(Transform));
			s_types.Add(typeof(Time));
			s_types.Add(typeof(Vector3));

			// filter types
			for(int i = s_types.Count - 1; i >= 0; i--) {
				if(s_types[i].IsObsolete()) {
					s_types.RemoveAt(i);
				}
			}

			// start generate
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
			MethodInfo[] staticMethods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);

			// filter generic methods, property setter/getter, operator
			List<MethodInfo> publicMethods = new List<MethodInfo>();
			Array.ForEach<MethodInfo>(methods, m => {
				if(!m.IsGenericMethod && !m.IsObsolete()) {
					if(!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("op_")) {
						publicMethods.Add(m);
					}
				}
			});
			Array.ForEach<MethodInfo>(staticMethods, m => {
				if(!m.IsGenericMethod && !m.IsObsolete()) {
					if(!m.Name.StartsWith("get_") && !m.Name.StartsWith("set_") && !m.Name.StartsWith("op_")) {
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
				buffer += GeneratePublicMethod(t, mList);
			}

			// get properties
			PropertyInfo[] props = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			buffer += GenerateProperties(t, props);

			// static properties
			PropertyInfo[] staticProps = t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
			buffer += GenerateProperties(t, staticProps);

			// get fields
			FieldInfo[] fields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			buffer += GenerateFields(t, fields);

			// get static fields
			FieldInfo[] staticFields = t.GetFields(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
			buffer += GenerateFields(t, staticFields);

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
			foreach(PropertyInfo pi in props) {
				// skip some
				if(pi.IsObsolete()) {
					continue;
				}

				// filter special name
				if(pi.Name == "Item") {
					continue;
				}

				// get info
				MethodInfo getter = pi.GetGetMethod();
				MethodInfo setter = pi.GetSetMethod();
				string pn = pi.Name;

				// register property
				buffer += string.Format("\t\t\tLuaLib.tolua_variable(L, \"{0}\", {1}, {2});\n", pn, 
					getter == null ? "null" : string.Format("new LuaFunction({0})", getter.Name), 
					setter == null ? "null" : string.Format("new LuaFunction({0})", setter.Name));
			}
			foreach(PropertyInfo pi in staticProps) {
				// skip some
				if(pi.IsObsolete()) {
					continue;
				}

				// filter special name
				if(pi.Name == "Item") {
					continue;
				}

				// get info
				MethodInfo getter = pi.GetGetMethod();
				MethodInfo setter = pi.GetSetMethod();
				string pn = pi.Name;

				// register property
				buffer += string.Format("\t\t\tLuaLib.tolua_variable(L, \"{0}\", {1}, {2});\n", pn, 
					getter == null ? "null" : string.Format("new LuaFunction({0})", getter.Name), 
					setter == null ? "null" : string.Format("new LuaFunction({0})", setter.Name));
			}
			foreach(FieldInfo fi in fields) {
				// skip some
				if(fi.IsObsolete() || fi.IsLiteral) {
					continue;
				}

				// get info
				string fin = fi.Name;

				// register field
				buffer += string.Format("\t\t\tLuaLib.tolua_variable(L, \"{0}\", new LuaFunction(get_{0}), {1});\n", fin,
					fi.IsInitOnly ? "null" : string.Format("new LuaFunction(set_{0})", fin));
			}
			foreach(FieldInfo fi in staticFields) {
				// skip some
				if(fi.IsObsolete() || fi.IsLiteral) {
					continue;
				}

				// get info
				string fin = fi.Name;

				// register field
				buffer += string.Format("\t\t\tLuaLib.tolua_variable(L, \"{0}\", new LuaFunction(get_{0}), {1});\n", fin,
					fi.IsInitOnly ? "null" : string.Format("new LuaFunction(set_{0})", fin));
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

		private static string GenerateFields(Type t, FieldInfo[] fields) {
			string buffer = "";

			// generate every fields
			foreach(FieldInfo fi in fields) {
				// skip
				if(fi.IsObsolete() || fi.IsLiteral) {
					continue;
				}

				// get info
				Type ft = fi.FieldType;
				string ftn = ft.GetNormalizedName();
				string fin = fi.Name;
				string gn = "get_" + fin;
				string sn = "set_" + fin;
				string tn = t.GetNormalizedName();

				// generate getter
				{
					// method start
					buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
					buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", gn);

					// err object
					buffer += "\t\t#if DEBUG\n";
					buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
					buffer += "\t\t#endif\n";

					// try to get object from first parameter
					buffer += "\n";
					buffer += "\t\t\t// caller type check\n";
					buffer += "\t\t#if DEBUG\n";
					if(fi.IsStatic) {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertable(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					} else {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					}
					buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", gn);
					buffer += "\t\t\t\treturn 0;\n";
					buffer += "\t\t\t}\n";
					buffer += "\t\t#endif\n";
					if(!fi.IsStatic) {
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})NativeObjectMap.FindObject(refId);\n", tn);
						buffer += "\t\t#if DEBUG\n";
						buffer += "\t\t\tif(obj == null) {\n";
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"invalid 'cobj' in function '{0}'\", ref err);\n", gn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
					}

					// call function
					buffer += "\n";
					buffer += "\t\t\t// get field\n";
					buffer += string.Format("\t\t\t{0} ret = {1}.{2};\n", ftn, fi.IsStatic ? tn : "obj", fin);

					// push returned value
					buffer += GenerateBoxReturnValue(ft, "\t\t\t");

					// method end
					buffer += "\t\t\treturn 1;\n";
					buffer += "\t\t}\n\n";
				}

				// generate setter
				if(!fi.IsInitOnly) {
					// method start
					buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
					buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", sn);

					// err object
					buffer += "\t\t#if DEBUG\n";
					buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
					buffer += "\t\t#endif\n";

					// try to get object from first parameter
					buffer += "\n";
					buffer += "\t\t\t// caller type check\n";
					buffer += "\t\t#if DEBUG\n";
					if(fi.IsStatic) {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertable(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					} else {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					}
					buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", sn);
					buffer += "\t\t\t\treturn 0;\n";
					buffer += "\t\t\t}\n";
					buffer += "\t\t#endif\n";
					if(!fi.IsStatic) {
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})NativeObjectMap.FindObject(refId);\n", tn);
						buffer += "\t\t#if DEBUG\n";
						buffer += "\t\t\tif(obj == null) {\n";
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"invalid 'cobj' in function '{0}'\", ref err);\n", sn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
					}

					// find conversion by property type name
					buffer += "\n";
					buffer += "\t\t\t// set field\n";
					buffer += "\t\t\tbool ok = true;\n";
					buffer += string.Format("\t\t\t{0} ret;\n", ftn);
					if(ft.IsArray) {
						Type et = ft.GetElementType();
						string etn = et.GetNormalizedName();
						if(et.IsArray) {
							// TODO more than one dimension array? not supported yet
						} else {
							buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_array<{0}>(L, 2, out ret, \"{1}\");\n", etn, sn);
						}
					} else if(ft.IsList()) {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_list<{0}>(L, 2, out ret, \"{1}\");\n", ftn, sn);
					} else if(ft.IsDictionary()) {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_dictionary<{0}>(L, 2, out ret, \"{1}\");\n", ftn, sn);
					} else {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_type<{0}>(L, 2, out ret, \"{1}\");\n", ftn, sn);
					}
					buffer += "\t\t\tif(ok) {\n";
					buffer += string.Format("\t\t\t\t{0}.{1} = ret;\n", fi.IsStatic ? tn : "obj", fin);
					buffer += "\t\t\t}\n\n";

					// method end
					buffer += "\t\t\treturn 0;\n";
					buffer += "\t\t}\n\n";
				}
			}

			// return
			return buffer;
		}

		private static string GenerateProperties(Type t, PropertyInfo[] props) {
			string buffer = "";

			// generate every property
			foreach(PropertyInfo pi in props) {
				// skip some
				if(pi.IsObsolete()) {
					continue;
				}

				// get info
				MethodInfo getter = pi.GetGetMethod();
				MethodInfo setter = pi.GetSetMethod();
				Type pt = pi.PropertyType;
				string ptn = pt.GetNormalizedName();
				string pn = pi.Name;
				string tn = t.GetNormalizedName();

				// filter special name
				if(pi.Name == "Item") {
					continue;
				}

				// generate getter
				if(getter != null) {
					string fn = getter.Name;

					// method start
					buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
					buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", fn);

					// err object
					buffer += "\t\t#if DEBUG\n";
					buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
					buffer += "\t\t#endif\n";

					// try to get object from first parameter
					buffer += "\n";
					buffer += "\t\t\t// caller type check\n";
					buffer += "\t\t#if DEBUG\n";
					if(getter.IsStatic) {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertable(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					} else {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					}
					buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
					buffer += "\t\t\t\treturn 0;\n";
					buffer += "\t\t\t}\n";
					buffer += "\t\t#endif\n";
					if(!getter.IsStatic) {
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})NativeObjectMap.FindObject(refId);\n", tn);
						buffer += "\t\t#if DEBUG\n";
						buffer += "\t\t\tif(obj == null) {\n";
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"invalid 'cobj' in function '{0}'\", ref err);\n", fn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
					}

					// call function
					buffer += "\n";
					buffer += "\t\t\t// get property\n";
					buffer += string.Format("\t\t\t{0} ret = {1}.{2};\n", ptn, getter.IsStatic ? tn : "obj", pn);

					// push returned value
					buffer += GenerateBoxReturnValue(getter, "\t\t\t");

					// method end
					buffer += "\t\t\treturn 1;\n";
					buffer += "\t\t}\n\n";
				}

				// generate setter
				if(setter != null) {
					string fn = setter.Name;

					// method start
					buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
					buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", fn);

					// err object
					buffer += "\t\t#if DEBUG\n";
					buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
					buffer += "\t\t#endif\n";

					// try to get object from first parameter
					buffer += "\n";
					buffer += "\t\t\t// caller type check\n";
					buffer += "\t\t#if DEBUG\n";
					if(setter.IsStatic) {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertable(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					} else {
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", 0, ref err)) {{\n", tn);
					}
					buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
					buffer += "\t\t\t\treturn 0;\n";
					buffer += "\t\t\t}\n";
					buffer += "\t\t#endif\n";
					if(!setter.IsStatic) {
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})NativeObjectMap.FindObject(refId);\n", tn);
						buffer += "\t\t#if DEBUG\n";
						buffer += "\t\t\tif(obj == null) {\n";
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"invalid 'cobj' in function '{0}'\", ref err);\n", fn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
					}

					// find conversion by property type name
					buffer += "\n";
					buffer += "\t\t\t// set property\n";
					buffer += "\t\t\tbool ok = true;\n";
					buffer += string.Format("\t\t\t{0} ret;\n", ptn);
					if(pt.IsArray) {
						Type et = pt.GetElementType();
						string etn = et.GetNormalizedName();
						if(et.IsArray) {
							// TODO more than one dimension array? not supported yet
						} else {
							buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_array<{0}>(L, 2, out ret, \"{1}\");\n", etn, fn);
						}
					} else if(pt.IsList()) {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_list<{0}>(L, 2, out ret, \"{1}\");\n", ptn, fn);
					} else if(pt.IsDictionary()) {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_dictionary<{0}>(L, 2, out ret, \"{1}\");\n", ptn, fn);
					} else {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_type<{0}>(L, 2, out ret, \"{1}\");\n", ptn, fn);
					}
					buffer += "\t\t\tif(ok) {\n";
					buffer += string.Format("\t\t\t\t{0}.{1} = ret;\n", setter.IsStatic ? tn : "obj", pn);
					buffer += "\t\t\t}\n\n";

					// method end
					buffer += "\t\t\treturn 0;\n";
					buffer += "\t\t}\n\n";
				}
			}

			// return
			return buffer;
		}

		private static string GenerateConstructor(Type t, ConstructorInfo[] mList) {
			ConstructorInfo m = mList[0];
			string tfn = t.FullName;
			string buffer = "";
			string tfnUnderscore = tfn.Replace(".", "_");
			string clazz = "lua_unity_" + tfnUnderscore + "_auto";
			string fn = clazz + ".__Constructor__";

			// constructor start
			buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
			buffer += "\t\tpublic static int __Constructor__(IntPtr L) {\n";

			// group constructor by parameter count, mind optional parameter
			Dictionary<int, List<ConstructorInfo>> cpMap = new Dictionary<int, List<ConstructorInfo>>();
			Array.ForEach<ConstructorInfo>(mList, c => {
				ParameterInfo[] pList = c.GetParameters();
				int maxArg = pList.Length;
				int minArg = maxArg;
				foreach(ParameterInfo pi in pList) {
					if(pi.IsOptional) {
						minArg--;
					}
				}
				for(int i = minArg; i <= maxArg; i++) {
					List<ConstructorInfo> cl = null;
					if(cpMap.ContainsKey(i)) {
						cl = cpMap[i];
					} else {
						cl = new List<ConstructorInfo>();
						cpMap[i] = cl;
					}
					cl.Add(c);
				}
			});

			// variables
			buffer += "\t\t\t// variables\n";
			buffer += "\t\t\tint argc = 0;\n";
			buffer += "\t\t#if DEBUG\n";
			buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
			buffer += "\t\t#endif\n";

			// get argument count
			buffer += "\n";
			buffer += "\t\t\t// get argument count\n";
			buffer += "\t\t\targc = LuaLib.lua_gettop(L) - 1;\n";

			// constructor body
			buffer += "\n";
			foreach(int c in cpMap.Keys) {
				// check argument count
				buffer += "\t\t\t// if argument count matched, call\n";
				buffer += string.Format("\t\t\tif(argc == {0}) {{\n", c);

				// check constructor count with same parameter count
				ConstructorInfo callM = null;
				List<ConstructorInfo> cl = cpMap[c];
				if(cl.Count > 1) {
					// get lua types
					buffer += "\t\t\t\t// get lua parameter types\n";
					buffer += "\t\t\t\tint[] luaTypes;\n";
					buffer += "\t\t\t\tLuaValueBoxer.GetLuaParameterTypes(L, out luaTypes);\n";

					// native types
					buffer += "\n";
					buffer += "\t\t\t\t// native types\n";
					for(int i = 0; i < cl.Count; i++) {
						// get parameter name which can be queried by GetType
						buffer += string.Format("\t\t\t\tstring[] nativeTypes{0} = new string[] {{\n", i);
						ParameterInfo[] pList = cl[i].GetParameters();
						for(int j = 0; j < pList.Length; j++) {
							ParameterInfo pi = pList[j];
							Type pt = pi.ParameterType;
							string ptn = pt.FullName;
							if(pt.IsGenericType) {
								int gArgc = pt.GetGenericArguments().Length;
								if(gArgc > 0) {
									ptn = ptn.Substring(0, ptn.IndexOf('`')) + "`" + gArgc;
								}
							}

							// put
							buffer += string.Format("\t\t\t\t\t\"{0}\"", ptn);
							if(j < pList.Length - 1) {
								buffer += ",";
							}
							buffer += "\n";
						}
						buffer += "\t\t\t\t};\n";
					}

					// accurate match every method
					buffer += "\n";
					for(int i = 0; i < cl.Count; i++) {
						// accurate match
						if(i == 0) {
							buffer += "\t\t\t\t";
						}
						buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0})) {{\n", i);

						// only one method, so it is simple, just pick the only one
						buffer += GenerateConstructorInvocation(t, cl[i], c, true);

						// close if
						buffer += "\t\t\t\t} else ";
					}

					// fuzzy match every method
					for(int i = 0; i < cl.Count; i++) {
						// accurate match
						buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0}, true)) {{\n", i);

						// only one method, so it is simple, just pick the only one
						buffer += GenerateConstructorInvocation(t, cl[i], c, true);

						// close if
						buffer += "\t\t\t\t}";
						if(i < cl.Count - 1) {
							buffer += " else ";
						} else {
							buffer += "\n";
						}
					}
				} else {
					// only one method, so it is simple, just pick the only one
					buffer += GenerateConstructorInvocation(t, cl[0], c, false);
				}

				// close if
				buffer += "\t\t\t}\n\n";
			}

			// fallback if argc doesn't match
			buffer += "\t\t\t// if to here, means argument count is not correct\n";
			buffer += string.Format("\t\t\tLuaLib.luaL_error(L, \"{0} has wrong number of arguments: \" + argc);\n", fn);

			// constructor end
			buffer += "\t\t\treturn 0;\n";
			buffer += "\t\t}\n\n";

			// return
			return buffer;
		}

		private static string GeneratePublicMethod(Type t, List<MethodInfo> mList) {
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
			buffer += "\t\t#if DEBUG\n";
			buffer += "\t\t\ttolua_Error err = new tolua_Error();\n";
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
					// get lua types
					buffer += "\t\t\t\t// get lua parameter types\n";
					buffer += "\t\t\t\tint[] luaTypes;\n";
					buffer += "\t\t\t\tLuaValueBoxer.GetLuaParameterTypes(L, out luaTypes);\n";

					// native types
					buffer += "\n";
					buffer += "\t\t\t\t// native types\n";
					for(int i = 0; i < ml.Count; i++) {
						// get parameter name which can be queried by GetType
						buffer += string.Format("\t\t\t\tstring[] nativeTypes{0} = new string[] {{\n", i);
						ParameterInfo[] pList = ml[i].GetParameters();
						for(int j = 0; j < pList.Length; j++) {
							ParameterInfo pi = pList[j];
							Type pt = pi.ParameterType;
							string ptn = pt.FullName;
							if(pt.IsGenericType) {
								int gArgc = pt.GetGenericArguments().Length;
								if(gArgc > 0) {
									ptn = ptn.Substring(0, ptn.IndexOf('`')) + "`" + gArgc;
								}
							}

							// put
							buffer += string.Format("\t\t\t\t\t\"{0}\"", ptn);
							if(j < pList.Length - 1) {
								buffer += ",";
							}
							buffer += "\n";
						}
						buffer += "\t\t\t\t};\n";
					}

					// accurate match every method
					buffer += "\n";
					for(int i = 0; i < ml.Count; i++) {
						// accurate match
						if(i == 0) {
							buffer += "\t\t\t\t";
						}
						buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0})) {{\n", i);

						// only one method, so it is simple, just pick the only one
						buffer += GenerateMethodInvocation(t, ml[i], c, true);

						// close if
						buffer += "\t\t\t\t} else ";
					}

					// fuzzy match every method
					for(int i = 0; i < ml.Count; i++) {
						// accurate match
						buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0}, true)) {{\n", i);

						// only one method, so it is simple, just pick the only one
						buffer += GenerateMethodInvocation(t, ml[i], c, true);

						// close if
						buffer += "\t\t\t\t}";
						if(i < ml.Count - 1) {
							buffer += " else ";
						} else {
							buffer += "\n";
						}
					}
				} else {
					// only one method, so it is simple, just pick the only one
					buffer += GenerateMethodInvocation(t, ml[0], c, false);
				}

				// close if
				buffer += "\t\t\t}\n\n";
			}

			// fallback if argc doesn't match
			buffer += "\t\t\t// if to here, means argument count is not correct\n";
			buffer += string.Format("\t\t\tLuaLib.luaL_error(L, \"{0} has wrong number of arguments: \" + argc);\n", fn);
			buffer += "\t\t\treturn 0;\n";
			buffer += "\t\t}\n\n";

			// return
			return buffer;
		}

		private static string GenerateConstructorInvocation(Type t, ConstructorInfo callM, int paramCount, bool paramTypeCheck) {
			string mn = callM.Name;
			string tn = t.Name;
			string tfn = t.FullName;
			string tfnUnderscore = tfn.Replace(".", "_");
			string clazz = "lua_unity_" + tfnUnderscore + "_auto";
			string fn = clazz + "." + mn;
			string indent = paramTypeCheck ? "\t\t\t\t\t" : "\t\t\t\t";
			string buffer = "";

			// parameters
			ParameterInfo[] pList = callM.GetParameters();

			// argument handling
			if(paramCount > 0) {
				// argument declaration
				buffer += indent + "// arguments declaration\n";
				for(int i = 0; i < pList.Length; i++) {
					Type pt = pList[i].ParameterType;
					string ptn = pt.GetNormalizedName();
					buffer += string.Format(indent + "{0} arg{1} = default({0});\n", ptn, i);
				}

				// argument conversion
				buffer += "\n";
				buffer += indent + "// convert lua value to desired arguments\n";
				buffer += indent + "bool ok = true;\n";
				for(int i = 0; i < pList.Length; i++) {
					buffer += GenerateUnboxParameters(pList[i], i, tn + mn, indent);
				}

				// check conversion
				buffer += "\n";
				buffer += indent + "// if conversion is not ok, print error and return\n";
				buffer += indent + "if(!ok) {\n";
				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"invalid arguments in function '{0}'\", ref err);\n", fn);
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n";
			}

			// perform object type checking based on method type, static or not
			buffer += "\n";
			buffer += indent + "// caller type check\n";
			buffer += indent.Substring(1) + "#if DEBUG\n";
			buffer += string.Format(indent + "if(!LuaLib.tolua_isusertable(L, 1, \"{0}\", 0, ref err)) {{\n", tfn);
			buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
			buffer += indent + "\treturn 0;\n";
			buffer += indent + "}\n";
			buffer += indent.Substring(1) + "#endif\n";

			// call function
			buffer += "\n";
			buffer += indent + "// call constructor\n";
			buffer += indent;
			buffer += string.Format("{0} ret = new {0}(", tfn);
			for(int i = 0; i < pList.Length; i++) {
				buffer += string.Format("arg{0}", i);
				if(i < pList.Length - 1) {
					buffer += ", ";
				}
			}
			buffer += ");\n";

			// push returned value
			buffer += GenerateBoxReturnValue(t, indent);
			buffer += indent + "return 1;\n";

			// return
			return buffer;
		}

		private static string GenerateMethodInvocation(Type t, MethodInfo callM, int paramCount, bool paramTypeCheck) {
			string mn = callM.Name;
			string tn = t.Name;
			string tfn = t.FullName;
			string tfnUnderscore = tfn.Replace(".", "_");
			string clazz = "lua_unity_" + tfnUnderscore + "_auto";
			string fn = clazz + "." + mn;
			string indent = paramTypeCheck ? "\t\t\t\t\t" : "\t\t\t\t";
			string buffer = "";

			// parameters
			ParameterInfo[] pList = callM.GetParameters();

			// argument handling
			if(paramCount > 0) {
				// argument declaration
				buffer += indent + "// arguments declaration\n";
				for(int i = 0; i < pList.Length; i++) {
					Type pt = pList[i].ParameterType;
					string ptn = pt.GetNormalizedName();
					buffer += string.Format(indent + "{0} arg{1} = default({0});\n", ptn, i);
				}

				// argument conversion
				buffer += "\n";
				buffer += indent + "// convert lua value to desired arguments\n";
				buffer += indent + "bool ok = true;\n";
				for(int i = 0; i < pList.Length; i++) {
					buffer += GenerateUnboxParameters(pList[i], i, tn + "." + mn, indent);
				}

				// check conversion
				buffer += "\n";
				buffer += indent + "// if conversion is not ok, print error and return\n";
				buffer += indent + "if(!ok) {\n";
				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"invalid arguments in function '{0}'\", ref err);\n", fn);
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n";
			}

			// get info of method to be called
			Type rt = callM.ReturnType;
			string rtn = rt.GetNormalizedName();

			// perform object type checking based on method type, static or not
			buffer += "\n";
			buffer += indent + "// caller type check\n";
			buffer += indent.Substring(1) + "#if DEBUG\n";
			if(callM.IsStatic) {
				buffer += string.Format(indent + "if(!LuaLib.tolua_isusertable(L, 1, \"{0}\", 0, ref err)) {{\n", tfn);
			} else {
				buffer += string.Format(indent + "if(!LuaLib.tolua_isusertype(L, 1, \"{0}\", 0, ref err)) {{\n", tfn);
			}
			buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
			buffer += indent + "\treturn 0;\n";
			buffer += indent + "}\n";
			buffer += indent.Substring(1) + "#endif\n";
			if(!callM.IsStatic) {
				buffer += indent + "int refId = LuaLib.tolua_tousertype(L, 1);\n";
				buffer += string.Format(indent + "{0} obj = ({0})NativeObjectMap.FindObject(refId);\n", tfn);
				buffer += indent.Substring(1) + "#if DEBUG\n";
				buffer += indent + "if(obj == null) {\n";
				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"invalid 'cobj' in function '{0}'\", ref err);\n", fn);
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n";
				buffer += indent.Substring(1) + "#endif\n";
			}

			// call function
			buffer += "\n";
			buffer += indent + "// call function\n";
			buffer += indent;
			if(rtn != "System.Void") {
				buffer += string.Format("{0} ret = ", rtn);
			}
			if(callM.IsStatic) {
				buffer += string.Format("{0}.{1}(", tfn, mn);
			} else {
				buffer += string.Format("obj.{0}(", mn);
			}
			for(int i = 0; i < pList.Length; i++) {
				if(pList[i].IsOut) {
					buffer += "out ";
				}
				if(pList[i].ParameterType.IsByRef) {
					buffer += "ref ";
				}
				buffer += string.Format("arg{0}", i);
				if(i < pList.Length - 1) {
					buffer += ", ";
				}
			}
			buffer += ");\n";

			// push returned value
			buffer += GenerateBoxReturnValue(callM, indent);
			buffer += indent + "return 1;\n";

			// return
			return buffer;
		}

		private static string GenerateBoxReturnValue(Type returnType, string indent) {
			string rtn = returnType.GetNormalizedName();
			string buffer = "";

			// convert to lua value
			if(returnType.IsVoid()) {
				// do not generate for void return
			} else if(returnType.IsArray) {
				Type et = returnType.GetElementType();
				string etn = et.GetNormalizedName();
				buffer += string.Format(indent + "LuaValueBoxer.array_to_luaval<{0}>(L, ret);\n", etn);
			} else {
				buffer += string.Format(indent + "LuaValueBoxer.type_to_luaval<{0}>(L, ret);\n", rtn);
			}

			// return
			return buffer;
		}

		private static string GenerateBoxReturnValue(MethodInfo m, string indent) {
			Type rt = m.ReturnType;
			return GenerateBoxReturnValue(rt, indent);
		}

		private static string GenerateUnboxParameters(ParameterInfo pi, int argIndex, string methodName, string indent) {
			// if parameter is out, no need unbox it
			if(pi.IsOut) {
				// TODO need remember this parameter and use multiret
				return "";
			}

			// variables
			string buffer = "";
			Type pt = pi.ParameterType;
			string ptn = pt.GetNormalizedName();

			// find conversion by parameter type name
			if(pt.IsArray) {
				Type et = pt.GetElementType();
				string etn = et.GetNormalizedName();
				if(et.IsArray) {
					// TODO more than one dimension array? not supported yet
				} else {
					buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_array<{0}>(L, {1}, out arg{2}, \"{3}\");\n", etn, argIndex + 2, argIndex, methodName);
				}
			} else if(pt.IsList()) {
				buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_list<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + 2, argIndex, methodName);
			} else if(pt.IsDictionary()) {
				buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_dictionary<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + 2, argIndex, methodName);
			} else {
				buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_type<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + 2, argIndex, methodName);
			}

			// return
			return buffer;
		}
	}
}