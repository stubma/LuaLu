using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public static class TypeExtension {
	public static bool IsDictionary(this Type t) {
		return t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
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
			return NormalizeTypeName(t.FullName);            
		}
	}

	public static string GetGenericName(this Type t) {
		if(t.GetGenericArguments().Length == 0) {
			return NormalizeTypeName(t.FullName);
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
