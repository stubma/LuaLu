namespace LuaLu {
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using System.IO;
	using System;
	using System.Reflection;
	using System.Text.RegularExpressions;

	[NoLuaBinding]
	public class LuaBindingGenerator {
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

			// add lua component
			s_types.Add(typeof(LuaComponent));

			// XXX: test code
			s_types.Add(typeof(System.Object));
			s_types.Add(typeof(Component));
			s_types.Add(typeof(Type));
			s_types.Add(typeof(GameObject));
			s_types.Add(typeof(Transform));
			s_types.Add(typeof(Time));
			s_types.Add(typeof(Vector3));
			s_types.Add(typeof(Rigidbody));
			s_types.Add(typeof(Text));
			s_types.Add(typeof(Input));
			s_types.Add(typeof(BoxCollider));
			s_types.Add(typeof(MulticastDelegate));
			s_types.Add(typeof(Vector3));
			s_types.Add(typeof(CameraType));
		}

		public static void ExposeSystemAndUnityTypes() {
			// find types in wanted namespace
//			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
//			foreach(Assembly asm in assemblies) {
//				Type[] tArray = asm.GetExportedTypes();
//				foreach(Type t in tArray) {
//					if(INCLUDE_NAMESPACES.Contains(t.Namespace)) {
//						s_types.Add(t);
//					}
//				}
//			}

			// generate
			StartGenerate();
		}

		public static void ExposeCustomTypes(List<Type> wanted) {
			// add wanted type and start to generate all
			s_types.AddRange(wanted as IEnumerable<Type>);
			StartGenerate();
		}

		public static void HideCustomTypes(List<Type> list) {
			list.ForEach(t => {
				s_types.Remove(t);
			});
			StartGenerate();
		}

		private static void CleanAllGenerated() {
			DirectoryInfo di = new DirectoryInfo(LuaConst.GENERATED_LUA_BINDING_PREFIX);
			foreach(FileInfo fi in di.GetFiles()) {
				fi.Delete();
			}
		}

		private static void StartGenerate() {
			// ensure folder exist
			if(!Directory.Exists(LuaConst.GENERATED_LUA_BINDING_PREFIX)) {
				Directory.CreateDirectory(LuaConst.GENERATED_LUA_BINDING_PREFIX);
			}

			// clean for a new generating
			CleanAllGenerated();

			// add wanted type and sort again
			List<Type> sortedTypes = SortTypes(s_types);

			// filter types
			List<Type> finalTypes = new List<Type>();
			sortedTypes.ForEach(t => {
				if(!t.IsGenericType && 
					!t.IsObsolete() &&
					!t.HasGenericBaseType() && 
					!t.IsPrimitive &&
					!t.Name.StartsWith("_") &&
					!EXCLUDE_CLASSES.Contains(t.GetNormalizedCodeName())) {
					finalTypes.Add(t);
				}
			});

			// start generate all classes
			GenerateTypesLuaBinding(finalTypes);

			// start generate delegate wrapper
			GenerateLuaDelegateWrapper();

			// generate register class
			GenerateRegisterAll(finalTypes);

			// refresh
			AssetDatabase.Refresh();
		}

		private static void GenerateTypesLuaBinding(List<Type> types) {
			// generate every type
			types.ForEach(t => { 
				if(t.IsEnum) {
					GenerateEnumLuaBinding(t);
				} else {
					GenerateClassLuaBinding(t);
				}
			});
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

		private static void GenerateRegisterAll(List<Type> types) {
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
			buffer += "\tusing LuaLu;\n\n";
			buffer += "\t[NoLuaBinding]\n";
			buffer += string.Format("\tpublic class {0} {{\n", clazz);

			// register method
			buffer += "\t\tpublic static int RegisterAll(IntPtr L) {\n";
			buffer += "\t\t\tLuaLib.tolua_open(L);\n";
			buffer += "\t\t\tLuaLib.tolua_module(L, null, 0);\n";
			buffer += "\t\t\tLuaLib.tolua_beginmodule(L, null);\n";
			foreach(Type t in types) {
				string tfnUnderscore = t.GetNormalizedIdentityName();
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

		private static void GenerateEnumLuaBinding(Type t) {
			// get info
			string tfn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string[] nsList = tfn.Split(new Char[] { '.' });
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

			// register method
			buffer += "\t\tpublic static int __Register__(IntPtr L) {\n";
			buffer += string.Format("\t\t\tLuaLib.tolua_usertype(L, \"{0}\");\n", tfn);
			foreach(string ns in nsList) {
				buffer += string.Format("\t\t\tLuaLib.tolua_module(L, \"{0}\", 0);\n", ns);
				buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", ns);
			}

			// class type and name, 1 means c sharp class
			buffer += "\t\t\tLuaLib.tolua_constant(L, \"__ctype\", 1);\n";
			buffer += string.Format("\t\t\tLuaLib.tolua_constant_string(L, \"__cname\", \"{0}\");\n", tfn);

			// get all enums
			Array names = Enum.GetNames(t);
			Array values = Enum.GetValues(t);
			for(int i = 0; i < names.Length; i++) {
				buffer += string.Format("\t\t\tLuaLib.tolua_constant(L, \"{0}\", {1});\n", names.GetValue(i), Convert.ChangeType(values.GetValue(i), typeof(int)));
			}

			// register method ends
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

		private static Dictionary<string, List<MethodBase>> GetBindableMethods(Type t, out Dictionary<string, List<MethodInfo>> publicGenericMethodMap) {
			// out
			publicGenericMethodMap = new Dictionary<string, List<MethodInfo>>();

			// methods
			MethodInfo[] mArr = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			MethodInfo[] smArr = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);

			// filter generic/obsolete methods, property setter/getter, operator, events
			List<MethodBase> publicMethods = new List<MethodBase>();
			Array.ForEach<MethodInfo>(mArr, m => {
				if(!m.IsObsolete() && 
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
			Array.ForEach<MethodInfo>(smArr, m => {
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
			Dictionary<string, List<MethodBase>> publicMethodMap = new Dictionary<string, List<MethodBase>>();
			foreach(MethodBase m in publicMethods) {
				if(m.IsGenericMethod) {
					// build method name for generic method
					int gargc = m.GetGenericArguments().Length;
					string mn = m.Name;
					for(int i = 0; i < gargc; i++) {
						mn += "T";
					}

					// group it by name
					List<MethodInfo> mList = null;
					if(publicGenericMethodMap.ContainsKey(mn)) {
						mList = publicGenericMethodMap[mn];
					} else {
						mList = new List<MethodInfo>();
						publicGenericMethodMap[mn] = mList;
					}
					mList.Add(m as MethodInfo);
				} else {
					List<MethodBase> mList = null;
					if(publicMethodMap.ContainsKey(m.Name)) {
						mList = publicMethodMap[m.Name];
					} else {
						mList = new List<MethodBase>();
						publicMethodMap[m.Name] = mList;
					}
					mList.Add(m);
				}
			}

			// return
			return publicMethodMap;
		}

		private static List<MethodBase> GetBindableConstructors(Type t) {
			ConstructorInfo[] cArr = t.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
			List<MethodBase> ctors = new List<MethodBase>();
			Array.ForEach<ConstructorInfo>(cArr, c => {
				if(!c.IsGenericMethod && !c.IsObsolete()) {
					ctors.Add(c);
				}
			});
			return ctors;
		}

		public static string GenerateOverloadMethodSignatures(Type t, Dictionary<string, List<MethodBase>> publicMethodMap, Dictionary<string, List<MethodInfo>> publicGenericMethodMap, List<MethodBase> ctors) {
			string buffer = "";

			// map start
			buffer += "\t\t// overload method parameter signatures\n";
			buffer += "\t\tstatic Dictionary<string, List<List<string>>> SIG = new Dictionary<string, List<List<string>>> {\n";

			// for constructor
			if(ctors.Count > 1) {
				// get method info
				string mn = "__Constructor__";

				// entry start
				buffer += "\t\t\t{\n";
				buffer += string.Format("\t\t\t\t\"{0}\",\n", mn);

				// value list start
				buffer += "\t\t\t\tnew List<List<string>> {\n";

				// method arguments
				for(int i = 0; i < ctors.Count; i++) {
					// method info
					ParameterInfo[] pList = ctors[i].GetParameters();

					// method arguments start
					buffer += "\t\t\t\t\tnew List<string> { ";

					// method arguments
					for(int j = 0; j < pList.Length; j++) {
						// get parameter info
						ParameterInfo pi = pList[j];
						Type pt = pi.ParameterType;
						string ptn = pt.GetNormalizedTypeName();

						// append parameter name
						buffer += string.Format("\"{0}{1}\"", pi.IsParams() ? "params " : "", ptn);
						if(j < pList.Length - 1) {
							buffer += ", ";
						}
					}

					// method arguments end
					buffer += " }";
					if(i < ctors.Count - 1) {
						buffer += ",";
					}
					buffer += "\n";
				}

				// value list end
				buffer += "\t\t\t\t}\n";

				// entry end
				buffer += "\t\t\t}, \n";
			}

			// for public methods
			foreach(List<MethodBase> mList in publicMethodMap.Values) {
				if(mList.Count > 1) {
					// get method info
					string mn = mList[0].Name;
					if(mn.StartsWith("op_")) {
						string op = mn.Substring(3);
						mn = SUPPORTED_OPERATORS[op];
					}

					// entry start
					buffer += "\t\t\t{\n";
					buffer += string.Format("\t\t\t\t\"{0}\",\n", mn);

					// value list start
					buffer += "\t\t\t\tnew List<List<string>> {\n";

					// method arguments
					for(int i = 0; i < mList.Count; i++) {
						// method info
						bool isStatic = mList[i].IsStatic;
						ParameterInfo[] pList = mList[i].GetParameters();

						// method arguments start
						buffer += "\t\t\t\t\tnew List<string> { ";

						// if not static, append type name
						if(!isStatic) {
							buffer += string.Format("\"{0}\"", t.GetNormalizedTypeName());
							if(pList.Length > 0) {
								buffer += ", ";
							}
						}

						// method arguments
						for(int j = 0; j < pList.Length; j++) {
							// get parameter info
							ParameterInfo pi = pList[j];
							Type pt = pi.ParameterType;
							string ptn = pt.GetNormalizedTypeName();

							// append parameter name
							buffer += string.Format("\"{0}{1}\"", pi.IsParams() ? "params " : "", ptn);
							if(j < pList.Length - 1) {
								buffer += ", ";
							}
						}

						// method arguments end
						buffer += " }";
						if(i < mList.Count - 1) {
							buffer += ",";
						}
						buffer += "\n";
					}

					// value list end
					buffer += "\t\t\t\t}\n";

					// entry end
					buffer += "\t\t\t}, \n";
				}
			}

			// for public generic methods
			foreach(string mn in publicGenericMethodMap.Keys) {
				List<MethodInfo> mList = publicGenericMethodMap[mn];

				// entry start
				buffer += "\t\t\t{\n";
				buffer += string.Format("\t\t\t\t\"{0}\",\n", mn);

				// value list start
				buffer += "\t\t\t\tnew List<List<string>> {\n";

				// method arguments
				for(int i = 0; i < mList.Count; i++) {
					// method info
					bool isStatic = mList[i].IsStatic;
					ParameterInfo[] pList = mList[i].GetParameters();

					// method arguments start
					buffer += "\t\t\t\t\tnew List<string> { ";

					// if not static, append type name
					if(!isStatic) {
						buffer += string.Format("\"{0}\"", t.GetNormalizedTypeName());
						if(pList.Length > 0) {
							buffer += ", ";
						}
					}

					// method arguments
					for(int j = 0; j < pList.Length; j++) {
						// get parameter info
						ParameterInfo pi = pList[j];
						Type pt = pi.ParameterType;
						string ptn = pt.GetNormalizedTypeName();

						// append parameter name
						buffer += string.Format("\"{0}{1}\"", pi.IsParams() ? "params " : "", pt.IsGenericParameter ? "T" : ptn);
						if(j < pList.Length - 1) {
							buffer += ", ";
						}
					}

					// method arguments end
					buffer += " }";
					if(i < mList.Count - 1) {
						buffer += ",";
					}
					buffer += "\n";
				}

				// value list end
				buffer += "\t\t\t\t}\n";

				// entry end
				buffer += "\t\t\t}, \n";
			}

			// map end
			buffer += "\t\t};\n";
			buffer += "\t\t#pragma warning restore 0414\n\n";

			// return
			return buffer;
		}

		private static void GenerateClassLuaBinding(Type t) {
			// get info
			string tn = t.Name;
			string tfn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
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
			buffer += "\tusing LuaLu;\n\n";
			buffer += "\t[NoLuaBinding]\n";
			buffer += string.Format("\tpublic class {0} {{\n", clazz);

			// shared err struct
			buffer += "\t\t#pragma warning disable 0414\n";
			buffer += "\t#if DEBUG\n";
			buffer += "\t\tstatic tolua_Error err = new tolua_Error();\n";
			buffer += "\t#endif\n\n";

			// overload methods resolution help list
			buffer += "\t\t// help list for overload resolution\n";
			buffer += "\t\tstatic List<int> ORList = new List<int>();\n\n";

			// get a map about methods which can be bound to lua
			Dictionary<string, List<MethodInfo>> publicGenericMethodMap;
			Dictionary<string, List<MethodBase>> publicMethodMap = GetBindableMethods(t, out publicGenericMethodMap);

			// get constructors which can be bound to lua
			List<MethodBase> ctors = GetBindableConstructors(t);

			// generate parameter map for overload methods
			buffer += GenerateOverloadMethodSignatures(t, publicMethodMap, publicGenericMethodMap, ctors);

			// we don't generate constructor for delegate type
			if(!t.IsCustomDelegateType()) {
				if(ctors.Count > 0) {
					buffer += GeneratePublicMethod(t, ctors);
				}
			}

			// generate for public methods
			foreach(List<MethodBase> mList in publicMethodMap.Values) {
				buffer += GeneratePublicMethod(t, mList);
			}

			// generate for public generic methods
			foreach(List<MethodInfo> mList in publicGenericMethodMap.Values) {
				buffer += GeneratePublicGenericMethod(t, mList);
			}

			// for custom delegate, we will append custom meta __add and __sub method to mimic operator overload
			if(t.IsCustomDelegateType()) {
				if(!s_delegates.Contains(t)) {
					s_delegates.Add(t);
				}
				buffer += GenerateCustomDelegateAddMethod(t);
				buffer += GenerateCustomDelegateRemoveMethod(t);
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

			// get events
			EventInfo[] events = t.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance);
			buffer += GenerateEvents(t, events);

			// get static events
			EventInfo[] staticEvents = t.GetEvents(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
			buffer += GenerateEvents(t, staticEvents);

			// register method
			buffer += "\t\tpublic static int __Register__(IntPtr L) {\n";
			buffer += string.Format("\t\t\tLuaLib.tolua_usertype(L, \"{0}\");\n", tfn);
			foreach(string ns in nsList) {
				buffer += string.Format("\t\t\tLuaLib.tolua_module(L, \"{0}\", 0);\n", ns);
				buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", ns);
			}
			buffer += string.Format("\t\t\tLuaLib.tolua_class(L, \"{0}\", \"{1}\", new LuaFunction(LuaStack.LuaGC));\n", tfn, btfn);
			buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", tn);

			// class type and name, 1 means c sharp class
			buffer += "\t\t\tLuaLib.tolua_constant(L, \"__ctype\", 1);\n";
			buffer += string.Format("\t\t\tLuaLib.tolua_constant_string(L, \"__cname\", \"{0}\");\n", tfn);

			// register constructor, except custom delegate type
			if(!t.IsCustomDelegateType()) {
				if(ctors.Count > 0) {
					buffer += "\t\t\tLuaLib.tolua_function(L, \"new\", new LuaFunction(__Constructor__));\n";
				}
			}

			// register public instance/static methods
			foreach(string _ in publicMethodMap.Keys) {
				string mn = _;
				if(mn.StartsWith("op_")) {
					string op = mn.Substring(3);
					mn = SUPPORTED_OPERATORS[op];
				}
				buffer += string.Format("\t\t\tLuaLib.tolua_function(L, \"{0}\", new LuaFunction({0}));\n", mn);
			}

			// __add and __sub for custom delegate type
			if(t.IsCustomDelegateType()) {
				buffer += "\t\t\tLuaLib.tolua_function(L, \"__add\", new LuaFunction(__add));\n";
				buffer += "\t\t\tLuaLib.tolua_function(L, \"__sub\", new LuaFunction(__sub));\n";
			}

			// register instance events
			foreach(EventInfo ei in events) {
				string en = ei.Name;
				buffer += string.Format("\t\t\tLuaLib.tolua_function(L, \"add{0}\", new LuaFunction(add_{0}));\n", en);
				buffer += string.Format("\t\t\tLuaLib.tolua_function(L, \"remove{0}\", new LuaFunction(remove_{0}));\n", en);
			}

			// register static events 
			foreach(EventInfo ei in staticEvents) {
				string en = ei.Name;
				buffer += string.Format("\t\t\tLuaLib.tolua_function(L, \"add{0}\", new LuaFunction(add_{0}));\n", en);
				buffer += string.Format("\t\t\tLuaLib.tolua_function(L, \"remove{0}\", new LuaFunction(remove_{0}));\n", en);
			}

			// register instance properties
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

			// register static properties
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

			// register instance fields
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

			// register static fields
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

			// register method ends
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

		private static string GenerateEvents(Type t, EventInfo[] events) {
			string buffer = "";
			string tfn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";

			// generate every event
			// for event, we generate a add method and a remove method for it
			foreach(EventInfo ei in events) {
				// info
				bool isStatic = ei.GetAddMethod().IsStatic;
				string en = ei.Name;
				Type et = ei.EventHandlerType;
				string etn = et.GetNormalizedCodeName();
				string etnUnderscore = et.GetNormalizedIdentityName();
				string fn = clazz + ".add_" + en;
				string indent = "\t\t";

				// add method start
				buffer += indent + "[MonoPInvokeCallback(typeof(LuaFunction))]\n";
				buffer += indent + string.Format("public static int add_{0}(IntPtr L) {{\n", en);

				// get argument count
				indent += "\t";
				buffer += indent + "// get argument count\n";
				buffer += indent + "int argc = LuaLib.lua_gettop(L);\n";
				buffer += "\n";

				// argument count should be 2, if not static, should be 1 argument
				buffer += indent + "// check arguments\n";
				buffer += indent + string.Format("if(argc == {0}) {{\n", isStatic ? 1 : 2);

				// object should be first argument
				indent += "\t";
				if(!isStatic) {
					buffer += indent + "// first should be this\n";
					buffer += indent.Substring(1) + "#if DEBUG\n";
					buffer += string.Format(indent + "if(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tfn);
					buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
					buffer += indent + "\treturn 0;\n";
					buffer += indent + "}\n";
					buffer += indent.Substring(1) + "#endif\n";
					buffer += indent + "int refId = LuaLib.tolua_tousertype(L, 1);\n";
					buffer += string.Format(indent + "{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tfn);
					buffer += indent.Substring(1) + "#if DEBUG\n";
					buffer += indent + "if(obj == null) {\n";
					buffer += string.Format(indent + "\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
					buffer += indent + "\treturn 0;\n";
					buffer += indent + "}\n";
					buffer += indent.Substring(1) + "#endif\n\n";
				}

				// create lua delegate wrapper
				buffer += indent + "// create lua delegate wrapper\n";
				buffer += indent + "LuaDelegateWrapper w = new LuaDelegateWrapper(L, -1);\n";
				buffer += indent + string.Format("{0} arg0 = new {0}(w.delegate_{1});\n\n", etn, etnUnderscore);

				// push this event as delegate type
				buffer += indent + "// combine them\n";
				if(isStatic) {
					buffer += indent + string.Format("{0}.{1} += arg0;\n", tfn, en);
				} else {
					buffer += indent + string.Format("obj.{0} += arg0;\n", en);
				}
				indent = indent.Substring(1);
				buffer += indent + "}\n";

				// no return value
				buffer += indent + "return 0;\n";

				// add method end
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";

				// remove method start
				fn = clazz + ".remove_" + en;
				buffer += indent + "[MonoPInvokeCallback(typeof(LuaFunction))]\n";
				buffer += indent + string.Format("public static int remove_{0}(IntPtr L) {{\n", en);

				// get argument count
				indent += "\t";
				buffer += indent + "// get argument count\n";
				buffer += indent + "int argc = LuaLib.lua_gettop(L);\n";
				buffer += "\n";

				// argument count should be 2, if not static, should be 1 argument
				buffer += indent + "// check arguments\n";
				buffer += indent + string.Format("if(argc == {0}) {{\n", isStatic ? 1 : 2);

				// object should be first argument
				indent += "\t";
				if(!isStatic) {
					buffer += indent + "// first should be this\n";
					buffer += indent.Substring(1) + "#if DEBUG\n";
					buffer += string.Format(indent + "if(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tfn);
					buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
					buffer += indent + "\treturn 0;\n";
					buffer += indent + "}\n";
					buffer += indent.Substring(1) + "#endif\n";
					buffer += indent + "int refId = LuaLib.tolua_tousertype(L, 1);\n";
					buffer += string.Format(indent + "{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tfn);
					buffer += indent.Substring(1) + "#if DEBUG\n";
					buffer += indent + "if(obj == null) {\n";
					buffer += string.Format(indent + "\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
					buffer += indent + "\treturn 0;\n";
					buffer += indent + "}\n";
					buffer += indent.Substring(1) + "#endif\n\n";
				}

				// get target
				buffer += indent + "// lua delegate info should packed in a table, with key 'target' and 'handler'\n";
				buffer += indent + "object targetObj = null;\n";
				buffer += indent + "int targetTable = 0;\n";
				buffer += indent + "int funcHandler = 0;\n";
				buffer += indent + string.Format("if(LuaLib.lua_istable(L, {0})) {{\n", isStatic ? 1 : 2);
				indent += "\t";
				buffer += indent + "// get target, it may be usertype or table\n";
				buffer += indent + "LuaLib.lua_pushstring(L, \"target\");\n";
				buffer += indent + string.Format("LuaLib.lua_gettable(L, {0});\n", isStatic ? 1 : 2);
				buffer += indent + "if(!LuaLib.lua_isnil(L, -1) && LuaLib.tolua_checkusertype(L, -1, \"System.Object\")) {\n";
				indent += "\t";
				buffer += indent + "targetObj = LuaValueBoxer.luaval_to_type(L, -1, \"System.Object\");\n";
				indent = indent.Substring(1);
				buffer += indent + "} else if(LuaLib.lua_istable(L, -1)) {\n";
				indent += "\t";
				buffer += indent + "targetTable = LuaLib.toluafix_ref_table(L, -1, 0);\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				buffer += indent + "LuaLib.lua_pop(L, 1);\n\n";

				// get handler
				buffer += indent + "// get function handler\n";
				buffer += indent + "LuaLib.lua_pushstring(L, \"handler\");\n";
				buffer += indent + string.Format("LuaLib.lua_gettable(L, {0});\n", isStatic ? 1 : 2);
				buffer += indent + "funcHandler = LuaLib.lua_isnil(L, -1) ? 0 : LuaLib.toluafix_ref_function(L, -1, 0);\n";
				buffer += indent + "LuaLib.lua_pop(L, 1);\n\n";

				// get event invocation list by reflection
				buffer += indent + "// get event invocation list by reflection\n";
				if(isStatic) {
					buffer += indent + string.Format("Type t = typeof({0});\n", tfn);
				} else {
					buffer += indent + "Type t = obj.GetType();\n";
				}
				buffer += indent + string.Format("System.Reflection.FieldInfo fi = t.GetField(\"{0}\", System.Reflection.BindingFlags.{1} | System.Reflection.BindingFlags.NonPublic);\n", en, isStatic ? "Static" : "Instance");
				buffer += indent + string.Format("object f = fi.GetValue({0});\n", isStatic ? "null" : "obj");
				buffer += indent + "MulticastDelegate e = (MulticastDelegate)f;\n";
				buffer += indent + "Delegate[] list = e.GetInvocationList();\n\n";

				// find lua delegate wrapper
				buffer += indent + "// find lua delegate wrapper and remove it\n";
				buffer += indent + "foreach(Delegate d in list) {\n";
				indent += "\t";
				buffer += indent + "if(d.Target is LuaDelegateWrapper) {\n";
				indent += "\t";
				buffer += indent + "LuaDelegateWrapper ldw = (LuaDelegateWrapper)d.Target;\n";
				buffer += indent + "if(ldw.Equals(targetObj, targetTable, funcHandler)) {\n";
				indent += "\t";
				if(isStatic) {
					buffer += indent + string.Format("{0}.{1} -= ({2})d;\n", tfn, en, etn);
				} else {
					buffer += indent + string.Format("obj.{0} -= ({1})d;\n", en, etn);
				}
				buffer += indent + "break;\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";

				// close argc if
				indent = indent.Substring(1);
				buffer += indent + "}\n";

				// no return value
				buffer += indent + "return 0;\n";

				// remove method end
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";
			}

			// return 
			return buffer;
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
				string ftcn = ft.GetNormalizedCodeName();
				string fttn = ft.GetNormalizedTypeName();
				string fin = fi.Name;
				string gn = "get_" + fin;
				string sn = "set_" + fin;
				string tn = t.GetNormalizedCodeName();

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
					buffer += string.Format("\t\t\t{0} ret = {1}.{2};\n", ftcn, fi.IsStatic ? tn : "obj", fin);

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
					buffer += string.Format("\t\t\t{0} ret;\n", ftcn);
					buffer += string.Format("\t\t\tret = ({0})LuaValueBoxer.luaval_to_type(L, 2, \"{1}\", \"{2}\");\n", ftcn, fttn, sn);
					if(!ft.IsValueType) {
						buffer += "\t\t\tok &= ret != null;\n";
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
				string ptcn = pt.GetNormalizedCodeName();
				string pttn = pt.GetNormalizedTypeName();
				string pn = pi.Name;
				string tn = t.GetNormalizedCodeName();

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
					buffer += string.Format("\t\t\t{0} ret = {1}.{2};\n", ptcn, getter.IsStatic ? tn : "obj", pn);

					// push returned value
					buffer += GenerateBoxReturnValue(getter.ReturnType, "\t\t\t");

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
					buffer += string.Format("\t\t\t{0} ret;\n", ptcn);
					buffer += string.Format("\t\t\tret = ({0})LuaValueBoxer.luaval_to_type(L, 2, \"{1}\", \"{2}\");\n", ptcn, pttn, fn);
					if(!pt.IsValueType) {
						buffer += "\t\t\tok &= ret != null;\n";
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

		private static string GeneratePublicGenericMethod(Type t, List<MethodInfo> mList) {
			// method name
			string mn = mList[0].Name;
			int gargc = mList[0].GetGenericArguments().Length;
			for(int i = 0; i < gargc; i++) {
				mn += "T";
			}

			// other info
			string buffer = "";
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + "." + mn;
			string indent = "\t\t";

			// generate invocation wrappers
			for(int i = 0; i < mList.Count; i++) {
				buffer += GenerateGenericMethodInvocation(t, mList[i], i);
			}

			return buffer;
		}

		private static string GenerateGenericMethodInvocation(Type t, MethodInfo m, int mIndex) {
			// if operator, should conversion name
			string mn = m.Name;
			int gargc = m.GetGenericArguments().Length;
			for(int i = 0; i < gargc; i++) {
				mn += "T";
			}

			// other info
			string indent = "\t\t";
			string tn = t.Name;
			string tfn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + "." + mn;
			string buffer = "";
			ParameterInfo[] pList = m.GetParameters();
			int paramCount = pList.Length;
			bool isStatic = m.IsStatic;

			// invocation start
			buffer += indent + string.Format("private static int call_{0}_{1}(IntPtr L) {{\n", mIndex, mn);

			// get argument count
			indent += "\t";
			buffer += indent + "// get argument count\n";
			buffer += indent + "int argc = LuaLib.lua_gettop(L);\n\n";

			// perform object type checking based on method type, static or not
			if(!isStatic) {
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

			// get generatic type strings
			buffer += indent + "// get generic type names\n";
			for(int i = 0; i < gargc; i++) {
				buffer += indent + string.Format("string t{0} = null;\n", i);
			}
			for(int i = 0; i < gargc; i++) {
				buffer += indent + string.Format("if(LuaLib.lua_isstring(L, {0})) {{\n", (isStatic ? 0 : 1) + i + 1);
				buffer += indent + string.Format("\tt{0} = LuaLib.lua_tostring(L, {1});\n", i, (isStatic ? 0 : 1) + i + 1);
				buffer += indent + "} else {\n";
				buffer += indent + string.Format("\tLuaLib.tolua_error(L, string.Format(\"generic type name is not string type in function '{0}'\", refId), ref err);\n", fn);
				buffer += indent + "\treturn 0;\n";
				buffer += indent + "}\n";
			}

			// argument handling
//			if(paramCount > 0) {
//				// argument declaration
//				buffer += "\n";
//				indent += "\t";
//				buffer += indent + "// arguments declaration\n";
//				for(int i = 0; i < pList.Length; i++) {
//					Type pt = pList[i].ParameterType;
//					if(pt.IsGenericParameter) {
//						
//					} else {
//						string ptn = pt.GetNormalizedCodeName();
//						buffer += string.Format(indent + "{0} arg{1} = default({0});\n", ptn, i);
//					}
//				}
//
//				// argument conversion
//				buffer += "\n";
//				buffer += indent + "// convert lua value to desired arguments\n";
//				buffer += indent + "bool ok = true;\n";
//				for(int i = 0; i < pList.Length; i++) {
//					buffer += GenerateUnboxParameters(pList[i], i, tn + "." + mn, indent, isStatic);
//				}
//
//				// check conversion
//				buffer += "\n";
//				buffer += indent + "// if conversion is not ok, print error and return\n";
//				buffer += indent + "if(!ok) {\n";
//				buffer += indent.Substring(1) + "#if DEBUG\n";
//				buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"invalid arguments in function '{0}'\", ref err);\n", fn);
//				buffer += indent.Substring(1) + "#endif\n";
//				buffer += indent + "\treturn 0;\n";
//				buffer += indent + "}\n\n";
//				indent = indent.Substring(1);
//			}

			// invocation end
			buffer += indent + "return 0;\n";
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return
			return buffer;
		}

		private static string GeneratePublicMethod(Type t, List<MethodBase> mList) {
			// decide method name, if operator, need a conversion
			bool isCtor = mList[0].IsConstructor;
			string mn = mList[0].Name;
			if(mn.StartsWith("op_")) {
				string op = mn.Substring(3);
				mn = SUPPORTED_OPERATORS[op];
			}

			// other info
			string buffer = "";
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + "." + mn;
			string indent = "\t\t";

			// generate invocation wrappers
			for(int i = 0; i < mList.Count; i++) {
				buffer += GenerateMethodInvocation(t, mList[i], i);
			}

			// method start
			buffer += indent + "[MonoPInvokeCallback(typeof(LuaFunction))]\n";
			buffer += indent + string.Format("public static int {0}(IntPtr L) {{\n", isCtor ? "__Constructor__" : mn);

			// get argument count
			indent += "\t";
			buffer += indent + "// get argument count\n";
			buffer += indent + "int argc = LuaLib.lua_gettop(L);\n\n";

			// if no overload method, just call
			// if has, need do overload resolution
			if(mList.Count > 1) {
				// prepare, clear resolution list
				buffer += indent + "// clear resolution list\n";
				buffer += indent + "ORList.Clear();\n\n";

				// first exclude methods which argument count not matched
				buffer += indent + "// first exclude methods whose argument count not matched\n";
				buffer += indent + string.Format("List<List<string>> sigList = SIG[\"{0}\"];\n", isCtor ? "__Constructor__" : mn);
				buffer += indent + "for(int i = 0; i < sigList.Count; i++) {\n";
				indent += "\t";
				buffer += indent + "List<string> sigs = sigList[i];\n";
				buffer += indent + "bool lastIsParams = sigs.Count > 0 ? sigs[sigs.Count - 1].StartsWith(\"params \") : false;\n";
				buffer += indent + "int minArg = sigs.Count - (lastIsParams ? 1 : 0);\n";
				buffer += indent + "if((lastIsParams && argc >= minArg) || (!lastIsParams && argc == minArg)) {\n";
				indent += "\t";
				buffer += indent + "ORList.Add(i);\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";

				// if still more than one candidate, remove method whose parameter type can not be implicit converted
				buffer += indent + "// if still more than candidates, remove methods whose\n";
				buffer += indent + "// parameter type can not be implicit converted from lua type\n";
				buffer += indent + "if(ORList.Count > 1) {\n";
				indent += "\t";
				buffer += indent + "for(int i = 0; i < argc && ORList.Count > 1; i++) {\n";
				indent += "\t";
				buffer += indent + "for(int j = ORList.Count - 1; j >= 0; j--) {\n";
				indent += "\t";
				buffer += indent + "List<string> sigs = sigList[ORList[j]];\n";
				buffer += indent + "string nativeType = sigs[Math.Min(i, sigs.Count - 1)];\n";
				buffer += indent + "if(!LuaValueBoxer.CanLuaNativeMatch(L, i + 1, nativeType)) {\n";
				indent += "\t";
				buffer += indent + "ORList.RemoveAt(j);\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";

				// if still more than candidates, compare conversion and keep best one
				buffer += indent + "// if still more than candidates, compare conversion and keep best one\n";
				buffer += indent + "if(ORList.Count > 1) {\n";
				indent += "\t";
				buffer += indent + "for(int i = ORList.Count - 1; i >= 1; i--) {\n";
				indent += "\t";
				buffer += indent + "List<string> sigs1 = sigList[ORList[i]];\n";
				buffer += indent + "List<string> sigs2 = sigList[ORList[i - 1]];\n";
				buffer += indent + "int result = LuaValueBoxer.CompareOverload(L, sigs1, sigs2);\n";
				buffer += indent + "if(result > 0) {\n";
				indent += "\t";
				buffer += indent + "ORList.RemoveAt(i - 1);\n";
				indent = indent.Substring(1);
				buffer += indent + "} else if(result < 0) {\n";
				indent += "\t";
				buffer += indent + "while(i <= ORList.Count - 1) {\n";
				indent += "\t";
				buffer += indent + "ORList.RemoveAt(i);\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";

				// if still more than one candidates, just call first one
				// or if no candidates, fallback to error
				buffer += indent + "// if has candidates, just call first one\n";
				buffer += indent + "if(ORList.Count > 0) {\n";
				indent += "\t";
				buffer += indent + "int mIndex = ORList[0];\n";
				buffer += indent + "switch(mIndex) {\n";
				indent += "\t";
				for(int i = 0; i < mList.Count; i++) {
					buffer += indent + string.Format("case {0}:\n", i);
					buffer += indent + string.Format("\treturn call_{0}_{1}(L);\n", i, isCtor ? "Constructor" : mn);
				}
				indent = indent.Substring(1);
				buffer += indent + "}\n";
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";
			} else {
				// method info
				bool isStatic = isCtor ? true : mList[0].IsStatic;
				ParameterInfo[] pList = mList[0].GetParameters();
				bool lastIsParams = pList.Length > 0 ? pList[pList.Length - 1].IsParams() : false;
				int minArg = pList.Length + (isStatic ? 0 : 1) - (lastIsParams ? 1 : 0);

				// check argument count
				buffer += indent + "// if argument count matched, call\n";
				buffer += string.Format("\t\t\tif(argc {0} {1}) {{\n", lastIsParams ? ">=" : "==", minArg);

				// call
				indent += "\t";
				buffer += indent + string.Format("return call_0_{0}(L);\n", isCtor ? "Constructor" : mn);

				// check argument count - end
				indent = indent.Substring(1);
				buffer += indent + "}\n\n";
			}

			// fallback if argc doesn't match
			buffer += indent + "// if to here, means argument count is not correct\n";
			buffer += indent + string.Format("LuaLib.luaL_error(L, \"{0} has wrong number of arguments: \" + argc);\n", fn);
			buffer += indent + "return 0;\n";
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return
			return buffer;
		}

		private static string GenerateMethodInvocation(Type t, MethodBase m, int mIndex) {
			// if operator, should conversion name
			string mn = m.Name;
			bool isOperator = false;
			if(mn.StartsWith("op_")) {
				string op = mn.Substring(3);
				mn = SUPPORTED_OPERATORS[op];
				isOperator = true;
			}

			// other info
			string indent = "\t\t";
			string tn = t.Name;
			string tfn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + "." + mn;
			string buffer = "";
			ParameterInfo[] pList = m.GetParameters();
			int paramCount = pList.Length;
			bool isStatic = m.IsConstructor ? true : m.IsStatic;

			// invocation start
			buffer += indent + string.Format("private static int call_{0}_{1}(IntPtr L) {{\n", mIndex, m.IsConstructor ? "Constructor" : mn);

			// argument handling
			if(paramCount > 0) {
				// argument declaration
				indent += "\t";
				buffer += indent + "// arguments declaration\n";
				for(int i = 0; i < pList.Length; i++) {
					Type pt = pList[i].ParameterType;
					string ptn = pt.GetNormalizedCodeName();
					buffer += string.Format(indent + "{0} arg{1} = default({0});\n", ptn, i);
				}

				// argument conversion
				buffer += "\n";
				buffer += indent + "// convert lua value to desired arguments\n";
				buffer += indent + "bool ok = true;\n";
				for(int i = 0; i < pList.Length; i++) {
					buffer += GenerateUnboxParameters(pList[i], i, tn + "." + mn, indent, isStatic);
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
				indent = indent.Substring(1);
			}

			// get info of method to be called
			Type rt = m.IsConstructor ? t : ((MethodInfo)m).ReturnType;
			string rtn = rt.GetNormalizedCodeName();

			// perform object type checking based on method type, static or not
			if(!isStatic) {
				indent += "\t";
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
				indent = indent.Substring(1);
			}

			// call function
			indent += "\t";
			buffer += indent + "// call\n";
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
				if(m.IsConstructor) {
					buffer += string.Format("new {0}(", tfn);
				} else if(isStatic) {
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
			buffer += GenerateBoxReturnValue(rt, indent);

			// invocation end
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return
			return buffer;
		}

		private static string GenerateBoxReturnValue(Type returnType, string indent) {
			string buffer = "";
			int retured = 1;

			// convert to lua value
			if(returnType == typeof(void)) {
				// do not generate for void return
				retured = 0;
			} else {
				buffer += indent + "LuaValueBoxer.type_to_luaval(L, ret);\n";
			}

			// returned value count
			buffer += indent + string.Format("return {0};\n", retured);

			// return
			return buffer;
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
			string ptcn = pt.GetNormalizedCodeName();
			string pttn = pt.GetNormalizedTypeName();
			string ptnUnderscore = pt.GetNormalizedIdentityName();

			// find conversion by parameter type name
			if(pt.IsCustomDelegateType()) {
				// add this delegate type
				if(!s_delegates.Contains(pt)) {
					s_delegates.Add(pt);
				}

				// create lua delegate wrapper for it
				buffer += indent + string.Format("LuaDelegateWrapper w{0} = new LuaDelegateWrapper(L, {1});\n", argIndex, argIndex + (isStatic ? 1 : 2));
				buffer += indent + string.Format("arg{0} = new {1}(w{0}.delegate_{2});\n", argIndex, ptcn, ptnUnderscore);
			} else {
				buffer += indent + string.Format("arg{0} = ({1})LuaValueBoxer.luaval_to_type(L, {2}, \"{3}\", \"{4}\");\n", argIndex, ptcn, argIndex + (isStatic ? 1 : 2), pttn, methodName);
				if(!pt.IsValueType) {
					buffer += indent + string.Format("ok &= arg{0} != null;\n", argIndex);
				}
			}

			// return
			return buffer;
		}

		private static void GenerateLuaDelegateWrapper() {
			// info
			string clazz = "LuaDelegateWrapper";
			string path = LuaConst.GENERATED_LUA_BINDING_PREFIX + clazz + ".cs";
			string buffer = "";
			string indent = "\t\t";

			// namespace, members, constructor
			buffer += 
@"namespace LuaLu {
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEngine;
	using LuaInterface;
	using LuaLu;

	[NoLuaBinding]
	public class LuaDelegateWrapper : IDisposable {
		// object if delegate method is on a user type object
		private object targetObj;

		// object name if has object
		private string targetObjTypeName;

		// table handler if delegate method is in a user table
		private int targetTable;

		// delegate lua function handler
		private int funcHandler;

		// lua state
		private IntPtr L;

		public LuaDelegateWrapper(IntPtr L, int lo) {
			// save lua state
			this.L = L;

			// ensure lo is positive\
			if(lo < 0) {
				lo = LuaLib.lua_gettop(L) + lo + 1;
			}

			// lua delegate info should packed in a table, with key 'target' and 'handler'
			if(LuaLib.lua_istable(L, lo)) {
				// get target, it may be usertype or table
				LuaLib.lua_pushstring(L, ""target"");
				LuaLib.lua_gettable(L, lo);
				if(!LuaLib.lua_isnil(L, -1) && LuaLib.tolua_checkusertype(L, -1, ""System.Object"")) {
					targetObjTypeName = LuaLib.tolua_typename(L, -1);
					targetObj = LuaValueBoxer.luaval_to_type(L, -1, ""System.Object"");
				} else if(LuaLib.lua_istable(L, -1)) {
					targetTable = LuaLib.toluafix_ref_table(L, -1, 0);
					LuaLib.lua_pop(L, 1);
				}

				// get function handler
				LuaLib.lua_pushstring(L, ""handler"");
				LuaLib.lua_gettable(L, lo);
				funcHandler = LuaLib.lua_isnil(L, -1) ? 0 : LuaLib.toluafix_ref_function(L, -1, 0);
				LuaLib.lua_pop(L, 1);
			}
		}

		public bool Equals(object obj, int t, int f) {
			// check obj
			if(obj != targetObj) {
				return false;
			}

			// check table
			if(t != 0 && targetTable != 0) {
				LuaLib.toluafix_get_table_by_refid(L, t);
				LuaLib.toluafix_get_table_by_refid(L, targetTable);
				if(!LuaLib.lua_rawequal(L, -1, -2)) {
					LuaLib.lua_pop(L, 2);
					return false;
				}
				LuaLib.lua_pop(L, 2);
			} else if(t != 0 || targetTable != 0) {
				return false;
			}

			// check func
			if(f != 0 && funcHandler != 0) {
				LuaLib.toluafix_get_function_by_refid(L, f);
				LuaLib.toluafix_get_function_by_refid(L, funcHandler);
				if(!LuaLib.lua_rawequal(L, -1, -2)) {
					LuaLib.lua_pop(L, 2);
					return false;
				}
				LuaLib.lua_pop(L, 2);
			} else if(f != 0 || funcHandler != 0) {
				return false;
			}

			return true;
		}";

			// dispose
			buffer += "\n\n";
			buffer += 
@"		public void Dispose() {
			if(targetTable > 0) {
				LuaLib.toluafix_remove_table_by_refid(L, targetTable);
				targetTable = 0;
			}
			if(funcHandler > 0) {
				LuaLib.toluafix_remove_function_by_refid(L, funcHandler);
				funcHandler = 0;
			}
			GC.SuppressFinalize(this);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
";

			// generate delegate redirector
			foreach(Type t in s_delegates) {
				// info
				string tnUnderscore = t.GetNormalizedIdentityName();
				MethodInfo m = t.GetMethod("Invoke");
				Type rt = m.ReturnType;
				string rtcn = rt.GetNormalizedCodeName();
				string rttn = rt.GetNormalizedTypeName();
				string rtnUnderscore = rt.GetNormalizedIdentityName();
				ParameterInfo[] pList = m.GetParameters();

				// method start, without args
				buffer += "\n";
				buffer += indent + string.Format("public {0} delegate_{1}(", rtcn, tnUnderscore);
				for(int i = 0; i < pList.Length; i++) {
					Type pt = pList[i].ParameterType;
					string ptn = pt.GetNormalizedCodeName();
					buffer += string.Format("{0} arg{1}", ptn, i);
					if(i < pList.Length - 1) {
						buffer += ", ";
					}
				}

				// args end
				buffer += ") {\n";

				// value to be returned
				indent += "\t";
				if(rtcn != "void") {
					buffer += indent + "// value to be returned\n";
					buffer += indent + string.Format("{0} ret = default({0});\n\n", rtcn);
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
						buffer += indent + string.Format("LuaValueBoxer.type_to_luaval(L, arg{0});\n", i);
					}
				}

				// execute
				buffer += "\n";
				buffer += indent + "// execute function\n";
				buffer += indent + "s.ExecuteFunction(argc";
				if(rtcn == "void") {
					buffer += ");\n";
				} else {
					// lamba start
					buffer += ", (state, nresult) => {\n";
					indent += "\t";

					// check return type
					if(rt.IsCustomDelegateType()) {
						// create lua delegate wrapper for it
						buffer += indent + "LuaDelegateWrapper w = new LuaDelegateWrapper(state, -1);\n";
						buffer += indent + string.Format("ret = new {0}(w.delegate_{1});\n", rtcn, rtnUnderscore);
					} else {
						buffer += indent + string.Format("ret = ({0})LuaValueBoxer.luaval_to_type(L, -1, \"{1}\");\n", rtcn, rttn);
					}

					// lamba end
					indent = indent.Substring(1);
					buffer += indent + "});\n";
				}

				// call function end
				indent = indent.Substring(1);
				buffer += indent + "}\n";

				// return
				if(rtcn != "void") {
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

		private static string GenerateCustomDelegateAddMethod(Type t) {
			string buffer = "";
			string indent = "\t\t";
			string tfcn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + ".__add";

			// method start
			buffer += indent + "[MonoPInvokeCallback(typeof(LuaFunction))]\n";
			buffer += indent + "public static int __add(IntPtr L) {\n";

			// get argument count
			indent += "\t";
			buffer += indent + "// get argument count\n";
			buffer += indent + "int argc = LuaLib.lua_gettop(L);\n";
			buffer += "\n";

			// object should be first argument
			buffer += indent + "// first should be this\n";
			buffer += indent.Substring(1) + "#if DEBUG\n";
			buffer += string.Format(indent + "if(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tfcn);
			buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
			buffer += indent + "\treturn 0;\n";
			buffer += indent + "}\n";
			buffer += indent.Substring(1) + "#endif\n";
			buffer += indent + "int refId = LuaLib.tolua_tousertype(L, 1);\n";
			buffer += string.Format(indent + "{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tfcn);
			buffer += indent.Substring(1) + "#if DEBUG\n";
			buffer += indent + "if(obj == null) {\n";
			buffer += string.Format(indent + "\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
			buffer += indent + "\treturn 0;\n";
			buffer += indent + "}\n";
			buffer += indent.Substring(1) + "#endif\n\n";

			// argument count should be 2
			buffer += indent + "// it should have 2 arguments\n";
			buffer += indent + "if(argc == 2) {\n";

			// second should be a table, and we should create a lua delegate wrapper for it
			indent += "\t";
			buffer += indent + "// create lua delegate wrapper\n";
			buffer += indent + "LuaDelegateWrapper w = new LuaDelegateWrapper(L, -1);\n";
			buffer += indent + string.Format("{0} arg0 = new {0}(w.delegate_{1});\n", tfcn, tfnUnderscore);

			// combine them
			buffer += "\n";
			buffer += indent + "// combine them\n";
			buffer += indent + "obj += arg0;\n";

			// close if
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return this
			buffer += indent + "LuaValueBoxer.type_to_luaval(L, obj);\n";
			buffer += indent + "return 1;\n";

			// method end
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return
			return buffer;
		}

		private static string GenerateCustomDelegateRemoveMethod(Type t) {
			string buffer = "";
			string indent = "\t\t";
			string tfcn = t.GetNormalizedCodeName();
			string tfnUnderscore = t.GetNormalizedIdentityName();
			string clazz = "lua_" + tfnUnderscore + "_binder";
			string fn = clazz + ".__sub";

			// method start
			buffer += indent + "[MonoPInvokeCallback(typeof(LuaFunction))]\n";
			buffer += indent + "public static int __sub(IntPtr L) {\n";

			// get argument count
			indent += "\t";
			buffer += indent + "// get argument count\n";
			buffer += indent + "int argc = LuaLib.lua_gettop(L);\n";
			buffer += "\n";

			// object should be first argument
			buffer += indent + "// first should be this\n";
			buffer += indent.Substring(1) + "#if DEBUG\n";
			buffer += string.Format(indent + "if(!LuaLib.tolua_isusertype(L, 1, \"{0}\", ref err)) {{\n", tfcn);
			buffer += string.Format(indent + "\tLuaLib.tolua_error(L, \"#ferror in function '{0}'\", ref err);\n", fn);
			buffer += indent + "\treturn 0;\n";
			buffer += indent + "}\n";
			buffer += indent.Substring(1) + "#endif\n";
			buffer += indent + "int refId = LuaLib.tolua_tousertype(L, 1);\n";
			buffer += string.Format(indent + "{0} obj = ({0})LuaStack.FromState(L).FindObject(refId);\n", tfcn);
			buffer += indent.Substring(1) + "#if DEBUG\n";
			buffer += indent + "if(obj == null) {\n";
			buffer += string.Format(indent + "\tLuaLib.tolua_error(L, string.Format(\"invalid obj({{0}}) in function '{0}'\", refId), ref err);\n", fn);
			buffer += indent + "\treturn 0;\n";
			buffer += indent + "}\n";
			buffer += indent.Substring(1) + "#endif\n\n";

			// argument count should be 2
			buffer += indent + "// it should have 2 arguments\n";
			buffer += indent + "if(argc == 2) {\n";

			// second should be a table, and we should create a lua delegate wrapper for it
			indent += "\t";
			buffer += indent + "// create lua delegate wrapper\n";
			buffer += indent + "LuaDelegateWrapper w = new LuaDelegateWrapper(L, -1);\n";
			buffer += indent + string.Format("{0} arg0 = new {0}(w.delegate_{1});\n", tfcn, tfnUnderscore);

			// remove
			buffer += "\n";
			buffer += indent + "// remove\n";
			buffer += indent + "obj -= arg0;\n";

			// close if
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return this
			buffer += indent + "LuaValueBoxer.type_to_luaval(L, obj);\n";
			buffer += indent + "return 1;\n";

			// method end
			indent = indent.Substring(1);
			buffer += indent + "}\n\n";

			// return
			return buffer;
		}

		/// <summary>
		/// From selected assets, find any c sharp class which can be exported to lua side
		/// </summary>
		/// <returns>Types which can be generated for lua</returns>
		public static List<Type> GetSelectedBindableClassTypes() {
			List<Type> types = new List<Type>();
			foreach(UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets)) {
				string path = AssetDatabase.GetAssetPath(obj);
				if(!string.IsNullOrEmpty(path) && File.Exists(path)) {
					if(obj is MonoScript) {
						string ext = Path.GetExtension(path);
						if(ext == ".cs") {
							// load file
							string code = File.ReadAllText(path);

							// find classes
							List<string> classes = new List<string>();
							string pclazz = @"^[\s]*((internal|public|private|protected|sealed|abstract|static)?[\s]+){0,2}class[\s]+([\w\d_]+)[\s]*[\:]?[\s]*[\w\d_\.]*[\s]*\{";
							MatchCollection mc = Regex.Matches(code, pclazz, RegexOptions.Multiline);
							IEnumerator e = mc.GetEnumerator();
							while(e.MoveNext()) {
								Match m = (Match)e.Current;
								string clazz = m.Groups[m.Groups.Count - 1].ToString();
								classes.Add(clazz);
							}

							// find namespaces
							if(classes.Count > 0) {
								List<string> nsList = new List<string>();
								string pns = @"^[\s]*namespace[\s]*([\w\d_]+)[\s]*\{";
								mc = Regex.Matches(code, pns, RegexOptions.Multiline);
								e = mc.GetEnumerator();
								while(e.MoveNext()) {
									Match m = (Match)e.Current;
									string ns = m.Groups[m.Groups.Count - 1].ToString();
									nsList.Add(ns);
								}

								// full namespace
								string fullNS = string.Join(".", nsList.ToArray());

								// check every class, if found a class which has no NoLuaBindingAttribute, then ok
								foreach(string c in classes) {
									Type t = ExtensionType.GetType(fullNS + "." + c);
									if(t != null) {
										if(t.GetCustomAttributes(typeof(NoLuaBindingAttribute), false).Length == 0) {
											types.Add(t);
										}
									}
								}
							}
						}
					}
				}
			}

			return types;
		}
	}
}