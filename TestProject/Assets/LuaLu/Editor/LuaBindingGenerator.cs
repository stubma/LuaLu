namespace LuaLu {
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using System.IO;
	using System;
	using System.Reflection;

	public class LuaBindingGenerator {
		// namespace to find classes
		private static List<string> INCLUDE_NAMESPACES;

		// class excluded
		private static List<string> EXCLUDE_CLASSES;

		// weird method need to be excluded, otherwise get error when build
		private static List<string> EXCLUDE_METHODS;

		// property excluded
		private static List<string> EXCLUDE_PROPERTIES;

		// support operator overload
		private static Dictionary<string, string> SUPPORTED_OPERATORS;
		private static Dictionary<string, string> OPERATOR_STRINGS;
		private static Dictionary<string, bool> OPERATOR_UNARY;

		// types to be generated
		static List<Type> s_types;

		// delegate types encountered
		static List<Type> s_delegates;

		static LuaBindingGenerator() {
			s_types = new List<Type>();
			s_delegates = new List<Type>();
			INCLUDE_NAMESPACES = new List<string> {
				"System",
				"UnityEngine",
				"UnityEngine.UI"
			};
			EXCLUDE_CLASSES = new List<string> {
				"string",
				"decimal",
				"void",
				"System.ArgIterator",
				"System.Array",
				"System.ComponentModel.TypeConverter",
				"System.UriTypeConverter",
				"System.Console",
				"System.GopherStyleUriParser",
				"System.LdapStyleUriParser",
				"System.RuntimeTypeHandle",
				"System.TypedReference",
				"UnityEngine.MeshSubsetCombineUtility",
				"UnityEngine.MeshSubsetCombineUtility.MeshInstance",
				"UnityEngine.MeshSubsetCombineUtility.SubMeshInstance",
				"UnityEngine.TerrainData"
			};
			EXCLUDE_METHODS = new List<string> {
				"OnRebuildRequested",
				"IsJoystickPreconfigured",
				"CreateComInstanceFrom",
				"CreateDomain"
			};
			EXCLUDE_PROPERTIES = new List<string> {
				"ApplicationTrust",
				"SystemDirectory"
			};
			SUPPORTED_OPERATORS = new Dictionary<string, string> {
				{ "Addition", "__add" },
				{ "Subtraction", "__sub" },
				{ "UnaryNegation", "__unm" },
				{ "Multiply", "__mul" },
				{ "Division", "__div" },
				{ "Equality", "__eq" },
				{ "LessThan", "__lt" },
				{ "LessThanOrEqual", "__le" },
				{ "Modulus", "__mod" }
			};
			OPERATOR_STRINGS = new Dictionary<string, string> {
				{ "__add", "+" },
				{ "__sub", "-" },
				{ "__unm", "-" },
				{ "__mul", "*" },
				{ "__div", "/" },
				{ "__eq", "==" },
				{ "__lt", "<" },
				{ "__le", "<=" },
				{ "__mod", "%" }
			};
			OPERATOR_UNARY = new Dictionary<string, bool> {
				{ "__add", false },
				{ "__sub", false },
				{ "__unm", true },
				{ "__mul", false },
				{ "__div", false },
				{ "__eq", false },
				{ "__lt", false },
				{ "__le", false },
				{ "__mod", false }
			};
		}

		public static void GenerateUnityLuaBinding() {
			// find types in wanted namespace
			List<Type> types = new List<Type>();
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach(Assembly asm in assemblies) {
				Type[] tArray = asm.GetExportedTypes();
				foreach(Type t in tArray) {
					if(INCLUDE_NAMESPACES.Contains(t.Namespace)) {
						types.Add(t);
					}
				}
			}

			// add lua component
			types.Add(typeof(LuaComponent));

			// sort them
			types = SortTypes(types);

			// filter types
			types.ForEach(t => {
				if(!t.IsGenericType && 
					!t.IsObsolete() &&
					!t.IsEnum && 
					!t.IsCustomDelegateType() && 
					!t.HasGenericBaseType() && 
					!t.IsPrimitive &&
					!t.Name.StartsWith("_") &&
					!EXCLUDE_CLASSES.Contains(t.GetNormalizedName())) {
					s_types.Add(t);
				}
			});

			// ensure folder exist
			if(!Directory.Exists(LuaConst.GENERATED_LUA_BINDING_PREFIX)) {
				Directory.CreateDirectory(LuaConst.GENERATED_LUA_BINDING_PREFIX);
			}

			// test code
//			s_types.Add(typeof(System.Object));
//			s_types.Add(typeof(Component));
//			s_types.Add(typeof(Type));
//			s_types.Add(typeof(GameObject));
//			s_types.Add(typeof(Transform));
//			s_types.Add(typeof(Time));
//			s_types.Add(typeof(Vector3));
//			s_types.Add(typeof(Rigidbody));
//			s_types.Add(typeof(Text));
//			s_types.Add(typeof(Input));
//			s_types.Add(typeof(BoxCollider));
//			s_types.Add(typeof(MulticastDelegate));
//			s_types.Add(typeof(Vector3));

			// start generate all classes
			GenerateTypesLuaBinding();

			// start generate delegate wrapper
			GenerateLuaDelegateWrapper();

			// clean
			s_types.Clear();
			s_delegates.Clear();

			// refresh
			AssetDatabase.Refresh();
		}

		private static void GenerateTypesLuaBinding() {
			// generate every type
			s_types.ForEach(t => { 
				GenerateOneTypeLuaBinding(t);
			});

			// generate register class
			GenerateRegisterAll();
		}

		private static List<Type> SortTypes(List<Type> types) {
			List<Type> sortedTypes = new List<Type>();
			foreach(Type t in types) {
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
				string tfnUnderscore = t.GetNormalizedUnderscoreName();
				string tClass = "lua_" + tfnUnderscore + "_binder";
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
			// get info
			string tn = t.Name;
			string tfn = t.GetNormalizedName();
			string tfnUnderscore = t.GetNormalizedUnderscoreName();
			string[] nsList = tfn.Split(new Char[] { '.' });
			Type bt = t.BaseType;
			string btfn = bt != null ? bt.FullName : "";
			Array.Resize(ref nsList, nsList.Length - 1);
			string clazz = "lua_" + tfnUnderscore + "_binder";
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

			// shared err struct
			buffer += "\t#if DEBUG\n";
			buffer += "\t\tstatic tolua_Error err = new tolua_Error();\n";
			buffer += "\t#endif\n\n";

			// get constructors
			ConstructorInfo[] ctors = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			if(ctors.Length > 0) {
				buffer += GenerateConstructor(t, ctors);
			}

			// methods
			MethodInfo[] methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			MethodInfo[] staticMethods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);

			// filter generic/obsolete methods, property setter/getter, operator, events
			List<MethodInfo> publicMethods = new List<MethodInfo>();
			Array.ForEach<MethodInfo>(methods, m => {
				if(!m.IsGenericMethod && 
					!m.IsObsolete() && 
					!EXCLUDE_METHODS.Contains(m.Name) && 
					!m.Name.StartsWith("get_") && 
					!m.Name.StartsWith("set_") &&
					!m.Name.StartsWith("add_") && 
					!m.Name.StartsWith("remove_")) {
					// special check for operator overload
					if(m.Name.StartsWith("op_")) {
						string op = m.Name.Substring(3);
						if(SUPPORTED_OPERATORS.ContainsKey(op)) {
							publicMethods.Add(m);;
						}
					} else {
						publicMethods.Add(m);
					}
				}
			});
			Array.ForEach<MethodInfo>(staticMethods, m => {
				if(!m.IsGenericMethod && 
					!m.IsObsolete() && 
					!EXCLUDE_METHODS.Contains(m.Name) && 
					!m.Name.StartsWith("get_") && 
					!m.Name.StartsWith("set_") &&
					!m.Name.StartsWith("add_") && 
					!m.Name.StartsWith("remove_")) {
					// special check for operator overload
					if(m.Name.StartsWith("op_")) {
						string op = m.Name.Substring(3);
						if(SUPPORTED_OPERATORS.ContainsKey(op)) {
							publicMethods.Add(m);;
						}
					} else {
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
			List<PropertyInfo> props = new List<PropertyInfo>();
			Array.ForEach<PropertyInfo>(t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance),
				p => {
					if(!p.IsObsolete() && !EXCLUDE_PROPERTIES.Contains(p.Name)) {
						props.Add(p);
					}
				});
			buffer += GenerateProperties(t, props);

			// static properties
			List<PropertyInfo> staticProps = new List<PropertyInfo>();
			Array.ForEach<PropertyInfo>(t.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static),
				p => {
					if(!p.IsObsolete() && !EXCLUDE_PROPERTIES.Contains(p.Name)) {
						staticProps.Add(p);
					}
				});
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
			buffer += string.Format("\t\t\tLuaLib.tolua_class(L, \"{0}\", \"{1}\", new LuaFunction(LuaStack.LuaGC));\n", tfn, btfn);
			buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", tn);
			if(ctors.Length > 0) {
				buffer += "\t\t\tLuaLib.tolua_function(L, \"new\", new LuaFunction(__Constructor__));\n";
			}
			foreach(string _ in publicMethodMap.Keys) {
				string mn = _;
				if(mn.StartsWith("op_")) {
					string op = mn.Substring(3);
					mn = SUPPORTED_OPERATORS[op];
				}
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

					// try to get object from first parameter
					if(!fi.IsStatic) {
						buffer += "\t\t\t// caller type check\n";
						buffer += "\t\t#if DEBUG\n";
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tn);
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", gn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tn);
						if(!t.IsValueType) {
							buffer += "\t\t#if DEBUG\n";
							buffer += "\t\t\tif(obj == null) {\n";
							buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", gn);
							buffer += "\t\t\t\treturn 0;\n";
							buffer += "\t\t\t}\n";
							buffer += "\t\t#endif\n\n";
						}
					}

					// call function
					buffer += "\t\t\t// get field\n";
					buffer += string.Format("\t\t\t{0} ret = {1}.{2};\n", ftn, fi.IsStatic ? tn : "obj", fin);

					// push returned value
					buffer += GenerateBoxReturnValue(ft, "\t\t\t");

					// method end
					buffer += "\t\t}\n\n";
				}

				// generate setter
				if(!fi.IsInitOnly) {
					// method start
					buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
					buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", sn);

					// try to get object from first parameter
					if(!fi.IsStatic) {
						buffer += "\t\t\t// caller type check\n";
						buffer += "\t\t#if DEBUG\n";
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tn);
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", sn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tn);
						if(!t.IsValueType) {
							buffer += "\t\t#if DEBUG\n";
							buffer += "\t\t\tif(obj == null) {\n";
							buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", sn);
							buffer += "\t\t\t\treturn 0;\n";
							buffer += "\t\t\t}\n";
							buffer += "\t\t#endif\n\n";
						}
					}

					// find conversion by property type name
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
						if(ftn == "System.Array") {
							buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_list(L, 2, out ret, \"{0}\");\n", sn);
						} else {
							buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_list<{0}>(L, 2, out ret, \"{1}\");\n", ftn, sn);
						}
					} else if(ft.IsDictionary()) {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_dictionary<{0}>(L, 2, out ret, \"{1}\");\n", ftn, sn);
					} else {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_type<{0}>(L, 2, out ret, \"{1}\");\n", ftn, sn);
					}

					// set field
					buffer += "\t\t\tif(ok) {\n";
					buffer += string.Format("\t\t\t\t{0}.{1} = ret;\n", fi.IsStatic ? tn : "obj", fin);

					// if value type, need replace old
					if(!fi.IsStatic && t.IsValueType) {
						buffer += "\t\t\t\tLuaStack.FromState(L).ReplaceObject(refId, obj);\n";
					}

					// close set field
					buffer += "\t\t\t}\n\n";

					// close setter
					buffer += "\t\t\treturn 0;\n";
					buffer += "\t\t}\n\n";
				}
			}

			// return
			return buffer;
		}

		private static string GenerateProperties(Type t, List<PropertyInfo> props) {
			string buffer = "";

			// generate every property
			foreach(PropertyInfo pi in props) {
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

					// try to get object from first parameter
					if(!getter.IsStatic) {
						// err object
						buffer += "\t\t\t// caller type check\n";
						buffer += "\t\t#if DEBUG\n";
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tn);
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tn);
						if(!t.IsValueType) {
							buffer += "\t\t#if DEBUG\n";
							buffer += "\t\t\tif(obj == null) {\n";
							buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
							buffer += "\t\t\t\treturn 0;\n";
							buffer += "\t\t\t}\n";
							buffer += "\t\t#endif\n\n";
						}
					}

					// call function
					buffer += "\t\t\t// get property\n";
					buffer += string.Format("\t\t\t{0} ret = {1}.{2};\n", ptn, getter.IsStatic ? tn : "obj", pn);

					// push returned value
					buffer += GenerateBoxReturnValue(getter, "\t\t\t");

					// method end
					buffer += "\t\t}\n\n";
				}

				// generate setter
				if(setter != null) {
					string fn = setter.Name;

					// method start
					buffer += "\t\t[MonoPInvokeCallback(typeof(LuaFunction))]\n";
					buffer += string.Format("\t\tpublic static int {0}(IntPtr L) {{\n", fn);

					// try to get object from first parameter
					if(!setter.IsStatic) {
						// err object
						buffer += "\t\t\t// caller type check\n";
						buffer += "\t\t#if DEBUG\n";
						buffer += string.Format("\t\t\tif(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tn);
						buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
						buffer += "\t\t\t\treturn 0;\n";
						buffer += "\t\t\t}\n";
						buffer += "\t\t#endif\n";
						buffer += "\t\t\tint refId = LuaLib.tolua_tousertype(L, 1);\n";
						buffer += string.Format("\t\t\t{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tn);
						if(!t.IsValueType) {
							buffer += "\t\t#if DEBUG\n";
							buffer += "\t\t\tif(obj == null) {\n";
							buffer += string.Format("\t\t\t\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
							buffer += "\t\t\t\treturn 0;\n";
							buffer += "\t\t\t}\n";
							buffer += "\t\t#endif\n\n";
						}
					}

					// find conversion by property type name
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
						if(ptn == "System.Array") {
							buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_list(L, 2, out ret, \"{0}\");\n", fn);
						} else {
							buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_list<{0}>(L, 2, out ret, \"{1}\");\n", ptn, fn);
						}
					} else if(pt.IsDictionary()) {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_dictionary<{0}>(L, 2, out ret, \"{1}\");\n", ptn, fn);
					} else {
						buffer += string.Format("\t\t\tok &= LuaValueBoxer.luaval_to_type<{0}>(L, 2, out ret, \"{1}\");\n", ptn, fn);
					}

					// set property
					buffer += "\t\t\tif(ok) {\n";
					buffer += string.Format("\t\t\t\t{0}.{1} = ret;\n", setter.IsStatic ? tn : "obj", pn);

					// if value type, need replace old obj
					if(!setter.IsStatic && t.IsValueType) {
						buffer += "\t\t\t\tLuaStack.FromState(L).ReplaceObject(refId, obj);\n";
					}

					// close set property
					buffer += "\t\t\t}\n\n";

					// close setter
					buffer += "\t\t\treturn 0;\n";
					buffer += "\t\t}\n\n";
				}
			}

			// return
			return buffer;
		}

		private static string GenerateConstructor(Type t, ConstructorInfo[] mList) {
			string buffer = "";
			string tfnUnderscore = t.GetNormalizedUnderscoreName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
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

			// get argument count
			buffer += "\t\t\t// get argument count\n";
			buffer += "\t\t\tint argc = LuaLib.lua_gettop(L);\n";

			// constructor body
			buffer += "\n";
			foreach(int c in cpMap.Keys) {
				// check argument count
				buffer += "\t\t\t// if argument count matched, call\n";
				buffer += string.Format("\t\t\tif(argc == {0}) {{\n", c);

				// check constructor count with same parameter count
				List<ConstructorInfo> cl = cpMap[c];
				if(cl.Count > 1) {
					// get lua types
					buffer += "\t\t\t\t// get lua parameter types\n";
					buffer += "\t\t\t\tint[] luaTypes;\n";
					buffer += "\t\t\t\tLuaValueBoxer.GetLuaParameterTypes(L, out luaTypes, true);\n";

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
						buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0}, true)) {{\n", i);

						// only one method, so it is simple, just pick the only one
						buffer += GenerateConstructorInvocation(t, cl[i], c, true);

						// close if
						buffer += "\t\t\t\t} else ";
					}

					// fuzzy match every method
					for(int i = 0; i < cl.Count; i++) {
						// accurate match
						buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0}, true, true)) {{\n", i);

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
			// decide method name, if operator, need a conversion
			string mn = mList[0].Name;
			if(mn.StartsWith("op_")) {
				string op = mn.Substring(3);
				mn = SUPPORTED_OPERATORS[op];
			}

			// other info
			string tfn = t.GetNormalizedName();
			string buffer = "";
			string tfnUnderscore = t.GetNormalizedUnderscoreName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + "." + mn;

			// group method by parameter count, mind optional parameter
			Dictionary<int, List<MethodInfo>> mpMap = new Dictionary<int, List<MethodInfo>>();
			mList.ForEach(m => {
				ParameterInfo[] pList = m.GetParameters();
				int maxArg = pList.Length;
				if(!m.IsStatic) {
					maxArg++;
				}
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
			buffer += "\t\t\t// get argument count\n";
			buffer += "\t\t\tint argc = LuaLib.lua_gettop(L);\n";
			buffer += "\n";
			foreach(int c in mpMap.Keys) {
				// check argument count
				buffer += "\t\t\t// if argument count matched, call\n";
				buffer += string.Format("\t\t\tif(argc == {0}) {{\n", c);

				// check method count with same parameter count
				List<MethodInfo> ml = mpMap[c];
				if(ml.Count > 1) {
					// get lua types
					buffer += "\t\t\t\t// get lua parameter types\n";
					buffer += "\t\t\t\tint[] luaTypes;\n";
					buffer += "\t\t\t\tint[] staticLuaTypes;\n";
					buffer += "\t\t\t\tLuaValueBoxer.GetLuaParameterTypes(L, out luaTypes, false);\n";
					buffer += "\t\t\t\tLuaValueBoxer.GetLuaParameterTypes(L, out staticLuaTypes, true);\n";

					// check first arg if it is user type
					buffer += string.Format("\t\t\t\tbool mayBeThis = LuaLib.tolua_checkusertype(L, 1, \"{0}\");\n", tfn);

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
						if(ml[i].IsStatic) {
							buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, staticLuaTypes, nativeTypes{0}, true)) {{\n", i);
						} else {
							buffer += string.Format("if(mayBeThis && LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0}, false)) {{\n", i);
						}

						// only one method, so it is simple, just pick the only one
						buffer += GenerateMethodInvocation(t, ml[i], c, "\t\t\t\t\t");

						// close if
						buffer += "\t\t\t\t} else ";
					}

					// fuzzy match every method
					for(int i = 0; i < ml.Count; i++) {
						// accurate match
						if(ml[i].IsStatic) {
							buffer += string.Format("if(LuaValueBoxer.CheckParameterType(L, staticLuaTypes, nativeTypes{0}, true, true)) {{\n", i);
						} else {
							buffer += string.Format("if(mayBeThis && LuaValueBoxer.CheckParameterType(L, luaTypes, nativeTypes{0}, false, true)) {{\n", i);
						}

						// only one method, so it is simple, just pick the only one
						buffer += GenerateMethodInvocation(t, ml[i], c, "\t\t\t\t\t");

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
					buffer += GenerateMethodInvocation(t, ml[0], c, "\t\t\t\t");
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
			string tfn = t.GetNormalizedName();
			string tfnUnderscore = t.GetNormalizedUnderscoreName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
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
					buffer += GenerateUnboxParameters(pList[i], i, tn + mn, indent, true);
				}

				// check conversion
				buffer += "\n";
				buffer += indent + "// if conversion is not ok, print error and return\n";
				buffer += indent + "if(!ok) {\n";
				buffer += indent.Substring(1) + "#if DEBUG\n";
				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"invalid arguments in function '{0}'\", ref err);\n", fn);
				buffer += indent.Substring(1) + "#endif\n";
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n\n";
			}

			// call function
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

			// return
			return buffer;
		}

		private static string GenerateMethodInvocation(Type t, MethodInfo callM, int paramCount, string indent) {
			// if operator, should conversion name
			string mn = callM.Name;
			bool isOperator = false;
			if(mn.StartsWith("op_")) {
				string op = mn.Substring(3);
				mn = SUPPORTED_OPERATORS[op];
				isOperator = true;
			}

			// other info
			string tn = t.Name;
			string tfn = t.GetNormalizedName();
			string tfnUnderscore = t.GetNormalizedUnderscoreName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + "." + mn;
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
					buffer += GenerateUnboxParameters(pList[i], i, tn + "." + mn, indent, callM.IsStatic);
				}

				// check conversion
				buffer += "\n";
				buffer += indent + "// if conversion is not ok, print error and return\n";
				buffer += indent + "if(!ok) {\n";
				buffer += indent.Substring(1) + "#if DEBUG\n";
				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"invalid arguments in function '{0}'\", ref err);\n", fn);
				buffer += indent.Substring(1) + "#endif\n";
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n\n";
			}

			// get info of method to be called
			Type rt = callM.ReturnType;
			string rtn = rt.GetNormalizedName();

			// perform object type checking based on method type, static or not
			if(!callM.IsStatic) {
				buffer += indent + "// caller type check\n";
				buffer += indent.Substring(1) + "#if DEBUG\n";
				buffer += string.Format(indent + "if(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tfn);
				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n";
				buffer += indent.Substring(1) + "#endif\n";
				buffer += indent + "int refId = LuaLib.tolua_tousertype(L, 1);\n";
				buffer += string.Format(indent + "{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tfn);
				if(!t.IsValueType) {
					buffer += indent.Substring(1) + "#if DEBUG\n";
					buffer += indent + "if(obj == null) {\n";
					buffer += string.Format(indent + "\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
					buffer += indent + "\treturn 0;\n";
					buffer += indent + "}\n";
					buffer += indent.Substring(1) + "#endif\n\n";
				}
			}

			// call function
			buffer += indent + "// call function\n";
			buffer += indent;
			if(rtn != "void") {
				buffer += string.Format("{0} ret = ", rtn);
			}
			if(isOperator) {
				// for operator
				bool unary = OPERATOR_UNARY[mn];
				string opStr = OPERATOR_STRINGS[mn];
				if(unary) {
					buffer += opStr + "arg0;\n";
				} else {
					buffer += "arg0 " + opStr + " arg1;\n";
				}
			} else {
				// for normal method
				if(callM.IsStatic) {
					buffer += string.Format("{0}.{1}(", tfn, mn);
				} else {
					buffer += string.Format("obj.{0}(", mn);
				}
				for(int i = 0; i < pList.Length; i++) {
					if(pList[i].IsOut) {
						buffer += "out ";
					} else if(pList[i].ParameterType.IsByRef) {
						buffer += "ref ";
					}
					buffer += string.Format("arg{0}", i);
					if(i < pList.Length - 1) {
						buffer += ", ";
					}
				}
				buffer += ");\n";
			}

			// push returned value
			buffer += GenerateBoxReturnValue(callM, indent);

			// return
			return buffer;
		}

		private static string GenerateBoxReturnValue(Type returnType, string indent) {
			string rtn = returnType.GetNormalizedName();
			string buffer = "";
			int retured = 1;

			// convert to lua value
			if(rtn == "void") {
				// do not generate for void return
				retured = 0;
			} else if(returnType.IsArray) {
				Type et = returnType.GetElementType();
				string etn = et.GetNormalizedName();
				buffer += string.Format(indent + "LuaValueBoxer.array_to_luaval<{0}>(L, ret);\n", etn);
			} else {
				buffer += string.Format(indent + "LuaValueBoxer.type_to_luaval<{0}>(L, ret);\n", rtn);
			}

			// returned value count
			buffer += indent + string.Format("return {0};\n", retured);

			// return
			return buffer;
		}

		private static string GenerateBoxReturnValue(MethodInfo m, string indent) {
			Type rt = m.ReturnType;
			return GenerateBoxReturnValue(rt, indent);
		}

		private static string GenerateUnboxParameters(ParameterInfo pi, int argIndex, string methodName, string indent, bool isStatic) {
			// if parameter is out, no need unbox it
			if(pi.IsOut) {
				// TODO need remember this parameter and use multiret
				return "";
			}

			// variables
			string buffer = "";
			Type pt = pi.ParameterType;
			string ptn = pt.GetNormalizedName();
			string ptnUnderscore = pt.GetNormalizedUnderscoreName();

			// find conversion by parameter type name
			if(pt.IsArray) {
				Type et = pt.GetElementType();
				string etn = et.GetNormalizedName();
				if(et.IsArray) {
					// TODO more than one dimension array? not supported yet
				} else {
					buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_array<{0}>(L, {1}, out arg{2}, \"{3}\");\n", etn, argIndex + (isStatic ? 1 : 2), argIndex, methodName);
				}
			} else if(pt.IsList()) {
				if(ptn == "System.Array") {
					buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_list(L, {0}, out arg{1}, \"{2}\");\n", argIndex + (isStatic ? 1 : 2), argIndex, methodName);
				} else {
					buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_list<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + (isStatic ? 1 : 2), argIndex, methodName);
				}
			} else if(pt.IsDictionary()) {
				buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_dictionary<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + (isStatic ? 1 : 2), argIndex, methodName);
			} else if(pt.IsCustomDelegateType()) {
				// add this delegate type
				if(!s_delegates.Contains(pt)) {
					s_delegates.Add(pt);
				}

				// create lua delegate wrapper for it
				buffer += indent + string.Format("LuaDelegateWrapper w{0} = new LuaDelegateWrapper(L, {1});\n", argIndex, argIndex + (isStatic ? 1 : 2));
				buffer += indent + string.Format("arg{0} = new {1}(w{0}.delegate_{2});\n", argIndex, ptn, ptnUnderscore);
			} else {
				buffer += string.Format(indent + "ok &= LuaValueBoxer.luaval_to_type<{0}>(L, {1}, out arg{2}, \"{3}\");\n", ptn, argIndex + (isStatic ? 1 : 2), argIndex, methodName);
			}

			// return
			return buffer;
		}

		private static void GenerateLuaDelegateWrapper() {
			// info
			string clazz = "LuaDelegateWrapper";
			string path = LuaConst.GENERATED_LUA_BINDING_PREFIX + clazz + ".cs";
			string buffer = "";
			string indent = "";

			// namespace begin
			buffer += indent + "namespace LuaLu {\n";
			indent += "\t";
			buffer += indent + "using System;\n";
			buffer += indent + "using System.IO;\n";
			buffer += indent + "using System.Collections;\n";
			buffer += indent + "using System.Collections.Generic;\n";
			buffer += indent + "using UnityEngine;\n";
			buffer += indent + "using LuaInterface;\n\n";

			// class begin
			buffer += indent + string.Format("public class {0} : IDisposable {{\n", clazz);

			// fields
			indent += "\t";
			buffer += indent + "// object if delegate method is on a user type object\n";
			buffer += indent + "private object targetObj;\n\n";
			buffer += indent + "// object name if has object\n";
			buffer += indent + "private string targetObjTypeName;\n\n";
			buffer += indent + "// table handler if delegate method is in a user table\n";
			buffer += indent + "private int targetTable;\n\n";
			buffer += indent + "// delegate lua function handler\n";
			buffer += indent + "private int funcHandler;\n\n";
			buffer += indent + "// lua state\n";
			buffer += indent + "private IntPtr L;\n\n";

			// constructor begin
			buffer += indent + string.Format("public {0}(IntPtr L, int lo) {{\n", clazz);

			// save lua state
			indent += "\t";
			buffer += indent + "// save lua state\n";
			buffer += indent + "this.L = L;\n\n";

			// get lua function target and handler
			buffer += indent + "// lua delegate info should packed in a table, with key 'target' and 'handler'\n";
			buffer += indent + "if(LuaLib.lua_istable(L, lo)) {\n";
			indent += "\t";
			buffer += indent + "// get target, it may be usertype or table\n";
			buffer += indent + "LuaLib.lua_pushstring(L, \"target\");\n";
			buffer += indent + "LuaLib.lua_gettable(L, lo);\n";
			buffer += indent + "if(!LuaLib.lua_isnil(L, -1) && LuaLib.tolua_checkusertype(L, -1, \"System.Object\")) {\n";
			indent += "\t";
			buffer += indent + "targetObjTypeName = LuaLib.tolua_typename(L, -1);\n";
			buffer += indent + "LuaValueBoxer.luaval_to_type<System.Object>(L, -1, out targetObj);\n";
			indent = indent.Substring(1);
			buffer += indent + "} else if(LuaLib.lua_istable(L, -1)) {\n";
			indent += "\t";
			buffer += indent + "targetTable = LuaLib.toluafix_ref_table(L, -1, 0);\n";
			indent = indent.Substring(1);
			buffer += indent + "}\n";
			buffer += indent + "LuaLib.lua_pop(L, 1);\n";
			buffer += "\n";
			buffer += indent + "// get function handler\n";
			buffer += indent + "LuaLib.lua_pushstring(L, \"handler\");\n";
			buffer += indent + "LuaLib.lua_gettable(L, lo);\n";
			buffer += indent + "funcHandler = LuaLib.lua_isnil(L, -1) ? 0 : LuaLib.toluafix_ref_function(L, -1, 0);\n";
			buffer += indent + "LuaLib.lua_pop(L, 1);\n";
			indent = indent.Substring(1);
			buffer += indent + "}\n";

			// constructor end
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// dispose start
			buffer += indent + "public void Dispose() {\n";

			// release table ref
			indent += "\t";
			buffer += indent + "if(targetTable > 0) {\n";
			indent += "\t";
			buffer += indent + "LuaLib.toluafix_remove_table_by_refid(L, targetTable);\n";
			buffer += indent + "targetTable = 0;\n";
			indent = indent.Substring(1);
			buffer += indent + "}\n";

			// release function ref
			buffer += indent + "if(funcHandler > 0) {\n";
			indent += "\t";
			buffer += indent + "LuaLib.toluafix_remove_function_by_refid(L, funcHandler);\n";
			buffer += indent + "funcHandler = 0;\n";
			indent = indent.Substring(1);
			buffer += indent + "}\n";

			// dispose end
			indent = indent.Substring(1);
			buffer += indent + "}\n";

			// generate delegate redirector
			foreach(Type t in s_delegates) {
				// info
				string tnUnderscore = t.GetNormalizedUnderscoreName();
				MethodInfo m = t.GetMethod("Invoke");
				Type rt = m.ReturnType;
				string rtn = rt.GetNormalizedName();
				string rtnUnderscore = rt.GetNormalizedUnderscoreName();
				ParameterInfo[] pList = m.GetParameters();

				// method start, without args
				buffer += "\n";
				buffer += indent + string.Format("public {0} delegate_{1}(", rtn, tnUnderscore);
				for(int i = 0; i < pList.Length; i++) {
					Type pt = pList[i].ParameterType;
					string ptn = pt.GetNormalizedName();
					buffer += string.Format("{0} arg{1}", ptn, i);
					if(i < pList.Length - 1) {
						buffer += ", ";
					}
				}

				// args end
				buffer += ") {\n";

				// value to be returned
				indent += "\t";
				if(rtn != "void") {
					buffer += indent + "// value to be returned\n";
					buffer += indent + string.Format("{0} ret = default({0});\n\n", rtn);
				}

				// call function, push function first
				buffer += indent + "// if func handler is set, deliver it to it\n";
				buffer += indent + "int argc = 0;\n";
				buffer += indent + "if(funcHandler > 0) {\n";
				indent += "\t";
				buffer += indent + "// push function\n";
				buffer += indent + "LuaLib.toluafix_get_function_by_refid(L, funcHandler);\n\n";

				// push target if not null, it can be null if static method
				buffer += indent + "// push target\n";
				buffer += indent + "LuaStack s = LuaStack.FromState(L);\n";
				buffer += indent + "if(targetObj != null) {\n";
				indent += "\t";
				buffer += indent + "s.PushObject(targetObj, targetObjTypeName);\n";
				buffer += indent + "argc++;\n";
				indent = indent.Substring(1);
				buffer += indent + "} else if(targetTable > 0) {\n";
				indent += "\t";
				buffer += indent + "LuaLib.toluafix_get_table_by_refid(L, targetTable);\n";
				buffer += indent + "argc++;\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";

				// push arguments
				if(pList.Length > 0) {
					buffer += "\n";
					buffer += indent + "// push arguments\n";
					buffer += indent + string.Format("argc += {0};\n", pList.Length);
					for(int i = 0; i < pList.Length; i++) {
						Type pt = pList[i].ParameterType;
						string ptn = pt.GetNormalizedName();
						if(pt.IsArray) {
							Type et = pt.GetElementType();
							string etn = et.GetNormalizedName();
							buffer += string.Format(indent + "LuaValueBoxer.array_to_luaval<{0}>(L, arg{1});\n", etn, i);
						} else {
							buffer += string.Format(indent + "LuaValueBoxer.type_to_luaval<{0}>(L, arg{1});\n", ptn, i);
						}
					}
				}

				// execute
				buffer += "\n";
				buffer += indent + "// execute function\n";
				buffer += indent + "s.ExecuteFunction(argc";
				if(rtn == "void") {
					buffer += ");\n";
				} else {
					// lamba start
					buffer += ", (state, nresult) => {\n";
					indent += "\t";

					// check return type
					if(rt.IsArray) {
						Type et = rt.GetElementType();
						string etn = et.GetNormalizedName();
						if(et.IsArray) {
							// TODO more than one dimension array? not supported yet
						} else {
							buffer += string.Format(indent + "LuaValueBoxer.luaval_to_array<{0}>(state, -1, out ret);\n", etn);
						}
					} else if(rt.IsList()) {
						if(rtn == "System.Array") {
							buffer += indent + "LuaValueBoxer.luaval_to_list(L, -1, out ret);\n";
						} else {
							buffer += string.Format(indent + "LuaValueBoxer.luaval_to_list<{0}>(state, -1, out ret);\n", rtn);
						}
					} else if(rt.IsDictionary()) {
						buffer += string.Format(indent + "LuaValueBoxer.luaval_to_dictionary<{0}>(state, -1, out ret);\n", rtn);
					} else if(rt.IsCustomDelegateType()) {
						// create lua delegate wrapper for it
						buffer += indent + "LuaDelegateWrapper w = new LuaDelegateWrapper(state, -1);\n";
						buffer += indent + string.Format("ret = new {0}(w.delegate_{1});\n", rtn, rtnUnderscore);
					} else {
						buffer += string.Format(indent + "LuaValueBoxer.luaval_to_type<{0}>(state, -1, out ret);\n", rtn);
					}

					// lamba end
					indent = indent.Substring(1);
					buffer += indent + "});\n";
				}

				// call function end
				indent = indent.Substring(1);
				buffer += indent + "}\n";

				// return
				if(rtn != "void") {
					buffer += indent + "return ret;\n";
				}

				// method end
				indent = indent.Substring(1);
				buffer += indent + "}\n";
			}

			// class end
			indent = indent.Substring(1);
			buffer += indent + "}\n";

			// namespace end
			buffer += "}\n";

			// write to file
			File.WriteAllText(path, buffer);
		}
	}
}