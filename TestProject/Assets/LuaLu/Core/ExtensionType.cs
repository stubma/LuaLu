using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using LuaLu;

[NoLuaBinding]
public static class ExtensionType {
	public static bool IsDictionary(this Type t) {
		// check self
		if(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
			return true;
		} else if(t == typeof(IDictionary)) {
			return true;
		}

		// check interface
		foreach(Type it in t.GetInterfaces()) {
			if(it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IDictionary<,>)) {
				return true;
			} else if(it == typeof(IDictionary)) {
				return true;
			}
		}
		return false;
	}

	public static bool IsEnumerable(this Type t) {
		return typeof(IEnumerable).IsAssignableFrom(t);
	}

	public static bool IsDelegate(this Type t) {
		return typeof(Delegate).IsAssignableFrom(t);
	}

	public static bool IsCustomDelegateType(this Type t) {
		return t.IsDelegate() && t != typeof(Delegate) && t != typeof(MulticastDelegate);
	}

	public static Type GetType(string name) {
		// resolve simple form
		if(LuaConst.SIMPLE_NAME_MAPPING.ContainsKey(name)) {
			name = LuaConst.SIMPLE_NAME_MAPPING[name];
		}

		// append assembly name
		if(name.StartsWith("UnityEngine.") || name.StartsWith("UnityEditor")) {
			string ns = name.Substring(0, name.LastIndexOf("."));
			return Type.GetType(name + ", " + ns);
		} else {
			return Type.GetType(name);
		}
	}

	public static bool IsList(this Type t) {
		// check self
		if(t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IList<>)) {
			return true;
		} else if(t == typeof(IList)) {
			return true;
		}

		// check interface
		foreach(Type it in t.GetInterfaces()) {
			if(it.IsGenericType && it.GetGenericTypeDefinition() == typeof(IList<>)) {
				return true;
			} else if(it == typeof(IList)) {
				return true;
			}
		}
		return false;
	}

	public static bool HasGenericBaseType(this Type t) {
		if(t.IsGenericType) {
			return true;
		} else if(t.BaseType == null) {
			return false;
		} else {
			return t.BaseType.HasGenericBaseType();
		}
	}

	public static string GetNormalizedCodeName(this Type t) {
		if(t.IsArray) {
			Type et = t.GetElementType();
			string str = et.GetNormalizedCodeName();
			str += "[";
			int rank = t.GetArrayRank();
			for(int i = 1; i < rank; i++) {
				str += ",";
			}
			str += "]";
			return str;
		} else if(t.IsGenericType) {
			// get pure type
			Type[] gArgs = t.GetGenericArguments();
			string typeName = t.FullName;
			string pureTypeName = typeName.Substring(0, typeName.IndexOf('`'));

			// append generic parameter
			pureTypeName += "<";
			bool first = true;
			Array.ForEach<Type>(gArgs, arg => {
				if(!first) {
					pureTypeName += ",";
				}
				first = false;
				pureTypeName += arg.GetNormalizedCodeName();
			});
			pureTypeName += ">";
			return pureTypeName;
		} else {
			return t.FullName == null ? t.Name.NormalizeCodeName() : t.FullName.NormalizeCodeName();            
		}
	}

	public static string GetNormalizedIdentityName(this Type t) {
		return t.GetNormalizedCodeName().Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace("[]", "_array").Replace(",", "_");
	}

	public static string GetNormalizedTypeName(this Type t) {
		if(t.IsGenericType) {
			// get pure type name
			Type[] gArgs = t.GetGenericArguments();
			string typeName = t.FullName;
			string pureTypeName = typeName.Substring(0, typeName.IndexOf('`'));
			pureTypeName += "`" + gArgs.Length;

			// append generic names
			pureTypeName += "[";
			bool first = true;
			Array.ForEach<Type>(gArgs, arg => {
				if(!first) {
					pureTypeName += ",";
				}
				first = false;
				pureTypeName += "[" + arg.GetNormalizedTypeName() + "]";
			});
			pureTypeName += "]";
			return pureTypeName;
		} else if(t.IsArray) {
			Type et = t.GetElementType();
			string str = et.GetNormalizedTypeName();
			str += "[";
			int rank = t.GetArrayRank();
			for(int i = 1; i < rank; i++) {
				str += ",";
			}
			str += "]";
			return str;
		} else {
			return t.FullName == null ? t.Name.NormalizeTypeName() : t.FullName.NormalizeTypeName();
		}
	}
}
