using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class ExtensionType {
	public static bool IsDictionary(this Type t) {
		return typeof(IDictionary).IsAssignableFrom(t);
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
		if(name.StartsWith("UnityEngine.") || name.StartsWith("UnityEditor")) {
			string ns = name.Substring(0, name.LastIndexOf("."));
			return Type.GetType(name + ", " + ns);
		} else {
			return Type.GetType(name);
		}
	}

	public static bool IsList(this Type t) {
		return typeof(IList).IsAssignableFrom(t);
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

	public static string GetNormalizedName(this Type t) {
		if(t.IsArray) {
			Type et = t.GetElementType();
			string str = et.GetNormalizedName();
			str += "[";
			int rank = t.GetArrayRank();
			for(int i = 1; i < rank; i++) {
				str += ",";
			}
			str += "]";
			return str;
		} else if(t.IsGenericType) {
			return t.GetGenericName();
		} else {
			return t.FullName == null ? t.Name.NormalizeTypeName() : t.FullName.NormalizeTypeName();            
		}
	}

	public static string GetNormalizedUnderscoreName(this Type t) {
		return t.GetNormalizedName().Replace(".", "_").Replace("<", "_").Replace(">", "_").Replace("[]", "_array").Replace(",", "_");
	}

	public static string GetGenericName(this Type t) {
		if(t.GetGenericArguments().Length == 0) {
			return t.FullName.NormalizeTypeName();
		}
		Type[] gArgs = t.GetGenericArguments();
		string typeName = t.FullName;
		string pureTypeName = typeName.Substring(0, typeName.IndexOf('`'));
		return pureTypeName + "<" + string.Join(",", GetGenericName(gArgs)) + ">";
	}

	private static string[] GetGenericName(Type[] types) {
		string[] results = new string[types.Length];
		for(int i = 0; i < types.Length; i++) {
			if(types[i].IsGenericType) {
				results[i] = types[i].GetGenericName();
			} else {
				results[i] = types[i].GetNormalizedName();
			}
		}
		return results;
	}
}
