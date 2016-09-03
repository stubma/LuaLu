using UnityEngine;
using System.Collections;
using System.Reflection;
using System;
using LuaLu;

[NoLuaBinding]
public static class MemberInfoExtension {
	public static bool IsObsolete(this MemberInfo t) {
		return t.IsDefined(typeof(ObsoleteAttribute), false);
	}
}
