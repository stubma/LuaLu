using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public static class ExtensionString {
	private static Dictionary<string, string> TYPE_MAPPING = new Dictionary<string, string> {
		{ "System.Void", "void" },
		{ "System.Single", "float" },
		{ "System.String", "string" },
		{ "System.Int32", "int" },
		{ "System.Int64", "long" },
		{ "System.SByte", "sbyte" },
		{ "System.Byte", "byte" },
		{ "System.Int16", "short" },
		{ "System.UInt16", "ushort" },
		{ "System.Char", "char" },
		{ "System.UInt32", "uint" },
		{ "System.UInt64", "ulong" },
		{ "System.Decimal", "decimal" },
		{ "System.Double", "double" },
		{ "System.Boolean", "bool" },
		{ "System.MonoType", "System.Type" }
	};

	public static string NormalizeTypeName(this string s) {
		string str = s; 
		if(str.Length > 1 && str[str.Length - 1] == '&') {
			str = str.Remove(str.Length - 1); 
		}

		if(TYPE_MAPPING.ContainsKey(str)) {
			return TYPE_MAPPING[str];
		}

		if(str.Contains("+")) {
			return str.Replace('+', '.');
		}

		return str;
	}
}
