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

	public static bool IsVoid(this Type t) {
		return t == typeof(void);
	}

	public static bool IsList(this Type t) {
		return typeof(IList).IsAssignableFrom(t);
	}

	public static string GetNormalizedName(this Type t) {
		if(t.IsArray) {
			t = t.GetElementType();
			string str = GetNormalizedName(t);
			str += "[]";
			return str;
		} else if(t.IsGenericType) {
			return t.GetGenericName();
		} else {
			return t.FullName.NormalizeTypeName();            
		}
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
