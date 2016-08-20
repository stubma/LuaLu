namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using System.Runtime.InteropServices;

	public class LuaValueBoxer {
		public static IntPtr Obj2Ptr(object obj) {
			GCHandle h = GCHandle.Alloc(obj);
			return (IntPtr)h;
		}

		public static object Ptr2Obj(IntPtr ptr) {
			GCHandle h = (GCHandle)ptr;
			return h.Target;
		}

		public static T Ptr2Obj<T>(IntPtr ptr) {
			GCHandle h = (GCHandle)ptr;
			T t = (T)h.Target;
			return t;
		}

		public static void array_to_luaval(IntPtr L, Array inValue) {
		}

		public static void dictionary_to_luaval(IntPtr L, Dictionary<string, object> dict) {
		}
	}
}