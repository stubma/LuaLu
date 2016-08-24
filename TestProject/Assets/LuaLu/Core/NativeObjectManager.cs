namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System;
	using LuaInterface;

	public class NativeObjectManager {
		private static Dictionary<int, object> s_objMap;

		static NativeObjectManager() {
			s_objMap = new Dictionary<int, object>();
		}

		public static object FindObject(int hash) {
			Debug.Log("map count: " + s_objMap.Count);
			if(s_objMap.ContainsKey(hash)) {
				return s_objMap[hash];
			} else {
				return null;
			}
		}

		public static bool isRegistered(object obj) {
			return s_objMap.ContainsKey(obj.GetHashCode());
		}

		public static void RemoveObject(int hash) {
			LuaLib.toluafix_remove_object_by_refid(LuaStack.SharedInstance().GetLuaState(), hash);
			s_objMap.Remove(hash);
		}

		public static void RegisterObject(object obj) {
			int hash = obj.GetHashCode();
			if(s_objMap.ContainsKey(hash)) {
				RemoveObject(hash);
			}
			s_objMap[hash] = obj;
		}
	}
}