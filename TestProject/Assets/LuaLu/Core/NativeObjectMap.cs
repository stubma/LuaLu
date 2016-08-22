namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using LuaInterface;

	public class NativeObjectMap {
		private static Dictionary<int, WeakReference> s_hashObjMap;

		static NativeObjectMap() {
			s_hashObjMap = new Dictionary<int, WeakReference>();
		}

		public static void ClearObject(int hash) {
			if(s_hashObjMap.ContainsKey(hash)) {
				LuaLib.toluafix_remove_object_by_refid(LuaStack.SharedInstance().GetLuaState(), hash);
				s_hashObjMap.Remove(hash);
			}
		}

		public static object FindObject(int hash) {
			if(s_hashObjMap.ContainsKey(hash)) {
				WeakReference r = s_hashObjMap[hash];
				if(r.IsAlive) {
					return r.Target;
				} else {
					// clear this object
					ClearObject(hash);

					// return null
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
			} else {
				// check if the refresh is same as obj, if not same, need a clear
				WeakReference r = s_hashObjMap[hash];
				if(r.IsAlive) {
					if(!r.Target.Equals(obj)) {
						Debug.Log("A collision occurs when register object: " + obj + ", old object is cleared");
						ClearObject(hash);
						s_hashObjMap[hash] = new WeakReference(obj);
					}
				} else {
					ClearObject(hash);
					s_hashObjMap[hash] = new WeakReference(obj);
				}
			}
		}
	}
}