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
		[MenuItem("Lua/Generate Unity Lua Binding", false, 1)]  
		static void MenuGenerateUnityLuaBinding() {
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
			string path = EditorUtility.SaveFilePanelInProject("New Lua Script", "NewLuaScript.lua", "lua", "Create new lua script file in project, lua file MUST be saved under Assets/Resources folder");
			if(path.Length != 0) {
				// save a template lua file
				string filename = Path.GetFileNameWithoutExtension(path);
				string buffer = "";
				buffer += "import(\"UnityEngine\")\n";
				buffer += "\n";
				buffer += string.Format("{0} = class(\"{0}\")\n", filename);
				buffer += "\n";
				buffer += string.Format("function {0}:ctor()\n", filename);
				buffer += "end\n";
				File.WriteAllText(path, buffer);

				// highlight it
				AssetDatabase.Refresh();
				Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
				EditorGUIUtility.PingObject(Selection.activeObject);
			}
		}
	}
}