using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using LuaLu;

[NoLuaBinding]
public static class ExtensionParameterInfo {
	public static bool IsParams(this ParameterInfo p) {
		return p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0;
	}
}
