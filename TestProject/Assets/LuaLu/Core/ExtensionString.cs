using UnityEngine;
using System.Collections;

public static class ExtensionString {
	public static string NormalizeTypeName(this string s) {
		string str = s; 
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
