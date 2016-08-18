namespace LuaLu {
	using UnityEditor;
	using UnityEngine;
	using System.Collections;
	using System.IO;
	using System;

	public class LuaMenu : ScriptableObject {
		[MenuItem("Lua/Generate Unity Lua Binding", false, 1)]  
		static void MenuGenerateUnityLuaBinding() {
		}

		[MenuItem("Lua/Build All Lua AssetBundles - OSX")]
		static void BuildAllLuaAssetBundlesOSX() {
			LuaAssetBundleExporter.BuildAllLuaAssetBundles(BuildTarget.StandaloneOSXUniversal);
		}

		[MenuItem("Lua/Build All Lua AssetBundles - iOS")]
		static void BuildAllLuaAssetBundlesIOS() {
			LuaAssetBundleExporter.BuildAllLuaAssetBundles(BuildTarget.iOS);
		}

		[MenuItem("Lua/Build All Lua AssetBundles - Android")]
		static void BuildAllLuaAssetBundlesAndroid() {
			LuaAssetBundleExporter.BuildAllLuaAssetBundles(BuildTarget.Android);
		}
	}
}