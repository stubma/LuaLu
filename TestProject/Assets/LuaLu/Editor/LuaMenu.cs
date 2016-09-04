namespace LuaLu {
	using UnityEditor;
	using UnityEngine;
	using UnityEngine.UI;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System;
	using System.Reflection;

	[NoLuaBinding]
	public class LuaMenu {
		[MenuItem("Lua/Expose Unity Classes", false, 1)]  
		static void MenuExposeUnityTypes() {
			LuaBindingGenerator.ExposeSystemAndUnityTypes();
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

		[MenuItem("Assets/Expose to Lua", false, 80)]
		static void MenuExposeCustomTypes() {
			List<Type> types = LuaBindingGenerator.GetSelectedBindableClassTypes();
			LuaBindingGenerator.ExposeCustomTypes(types);
		}

		[MenuItem("Assets/Expose to Lua", true)]
		static bool MenuExposeCustomTypesValidator() {
			return LuaBindingGenerator.GetSelectedBindableClassTypes().Count > 0;
		}

		[MenuItem("Assets/Hide from Lua", false, 80)]
		static void MenuHideCustomTypes() {
			List<Type> types = LuaBindingGenerator.GetSelectedBindableClassTypes();
			LuaBindingGenerator.HideCustomTypes(types);
		}

		[MenuItem("Assets/Hide from Lua", true, 80)]
		static bool MenuHideCustomTypesValidator() {
			return LuaBindingGenerator.GetSelectedBindableClassTypes().Count > 0;
		}

		[MenuItem("Assets/Create/Lua Script", false, 82)]
		static void MenuCreateLuaScript() {
			LuaFileHandler.NewStandaloneLuaFile();
		}
	}
}