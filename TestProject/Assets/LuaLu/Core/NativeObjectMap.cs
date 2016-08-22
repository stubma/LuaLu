namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;

	public class NativeObjectMap {
		private static Dictionary<int, WeakReference> s_hashObjMap;

		static NativeObjectMap() {
			s_hashObjMap = new Dictionary<int, WeakReference>();
		}

		public static object FindObject(int hash) {
			if(s_hashObjMap.ContainsKey(hash)) {
				WeakReference r = s_hashObjMap[hash];
				if(r.IsAlive) {
					return r.Target;
				} else {
					return null;
				}
			} else {
				return null;
			}
		}

		public static bool isRegistered(object obj) {
			return s_hashObjMap.ContainsKey(obj.GetHashCode());
		}

		public static void RegisterObject(object obj) {
			int hash = obj.GetHashCode();
			if(!s_hashObjMap.ContainsKey(hash)) {
				s_hashObjMap[hash] = new WeakReference(obj);
			}

			// TODO think about collision
		}
	}
}