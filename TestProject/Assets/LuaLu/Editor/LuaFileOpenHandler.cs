namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using UnityEditor;
	using UnityEditor.Callbacks;
	using System;
	using System.IO;

	public class LuaFileOpenHandler {
		[OnOpenAsset]
		public static bool OnOpenLuaFile(int instanceID, int line) {
			// check if open lua file, if it is, check lua editor setting
			string path = AssetDatabase.GetAssetPath(instanceID);
			string ext = Path.GetExtension(path);
			if(ext.ToLower() == ".lua") {
				if(LuaLuPrefs.GetLuaEditor() == LuaEditor.SYSTEM_DEFAULT) {
					return false;
				} else if(LuaLuPrefs.GetLuaEditor() == LuaEditor.ZERO_BRANE_STUDIO) {
					string zbsFolder = LuaLuPrefs.GetZeroBraneStudioFolder();
					System.Diagnostics.Process.Start(zbsFolder + "/zbstudio.sh", Directory.GetParent(Application.dataPath) + "/" + path);
					return true;
				} else if(LuaLuPrefs.GetLuaEditor() == LuaEditor.CUSTOM) {
					string exec = LuaLuPrefs.GetCustomEditorExecutable();
					string args = LuaLuPrefs.GetCustomEditorArguments();
					args = args.Replace("${file}", Directory.GetParent(Application.dataPath) + "/" + path);
					System.Diagnostics.Process.Start(exec, args);
					return true;
				} else {
					return false;
				}
			} else {
				return false;
			}
		}
	}
}