namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using LuaInterface;

	public class NativeObjectManager {
		private static Dictionary<int, object> s_hashObjMap;

		static NativeObjectManager() {
			s_hashObjMap = new Dictionary<int, object>();
		}

		public static object FindObject(int hash) {
			Debug.Log("map count: " + s_hashObjMap.Count);
			if(s_hashObjMap.ContainsKey(hash)) {
				return s_hashObjMap[hash];
			} else {
				return null;
			}
		}

		public static bool isRegistered(object obj) {
			return s_hashObjMap.ContainsKey(obj.GetHashCode());
		}

		public static void RemoveObject(int hash) {
			s_hashObjMap.Remove(hash);
		}

		public static void RegisterObject(object obj) {
			int hash = obj.GetHashCode();
			if(s_hashObjMap.ContainsKey(hash)) {
				RemoveObject(hash);
			}
			s_hashObjMap[hash] = obj;
		}
	}
}