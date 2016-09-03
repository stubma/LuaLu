namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System;

	[AttributeUsage(AttributeTargets.Class)]
	[NoLuaBinding]
	public sealed class NoLuaBindingAttribute : Attribute {
	}
}