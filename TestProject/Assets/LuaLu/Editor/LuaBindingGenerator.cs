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
				buffer += string.Format("\t\t\t{0}.Register(L);\n", tClass);
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
			string btn = bt != null ? bt.Name : "";
			string btfn = bt != null ? bt.FullName : "";
			Array.Resize(ref nsList, nsList.Length - 1);
			string clazz = "lua_unity_" + tfn.Replace(".", "_") + "_auto";
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
			buffer += "\t\tpublic static int Register(IntPtr L) {\n";
			buffer += string.Format("\t\t\tLuaLib.tolua_usertype(L, \"{0}\");\n", tfn);
			foreach(string ns in nsList) {
				buffer += string.Format("\t\t\tLuaLib.tolua_module(L, \"{0}\", 0);\n", ns);
				buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", ns);
			}
			buffer += string.Format("\t\t\tLuaLib.tolua_class(L, \"{0}\", \"{1}\", null);\n", tfn, btfn);
			buffer += string.Format("\t\t\tLuaLib.tolua_beginmodule(L, \"{0}\");\n", tn);
			buffer += "\t\t\tLuaLib.tolua_endmodule(L);\n";
			foreach(string ns in nsList) {
				buffer += "\t\t\tLuaLib.tolua_endmodule(L);\n";
			}
			buffer += "\t\t\treturn 1;\n";
			buffer += "\t\t}\n";

			// close class and namespace
			buffer += "\t}\n}";

			// write to file
			File.WriteAllText(path, buffer);
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
				return t.Name;
			}
			Type[] gArgs = t.GetGenericArguments();
			string typeName = t.Name;
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
				return NormalizeTypeName(t.ToString());            
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
			} else if(str == "System.Object") {                        
				return "object";
			}

			if(str.Contains("+")) {
				return str.Replace('+', '.');
			}

			return str;
		}
	}
}