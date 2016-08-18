namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using System.IO;
	using System;
	using System.Reflection;

	public class LuaBindingGenerator {
		private static string[] INCLUDE_ASSEMBLIES;

		static LuaBindingGenerator() {
			INCLUDE_ASSEMBLIES = new string[] {
				"UnityScript.Lang"
			}
		}

		public static void GenerateUnityLuaBinding() {
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach(Assembly asm in assemblies) {
				Debug.Log("asm name: " + asm.FullName);
				if(Array.IndexOf(INCLUDE_ASSEMBLIES, asm.FullName) != -1) {
					Type[] types = asm.GetExportedTypes();
				}
			}
		}


	}
}