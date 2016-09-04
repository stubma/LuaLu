namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using UnityEditor;
	using UnityEditor.Callbacks;
	using System;
	using System.IO;

	[NoLuaBinding]
	public class LuaFileHandler {
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

		public static string NewComponentLuaFile(string filename) {
			string path = EditorUtility.SaveFilePanelInProject ("New Lua Script", filename, "lua", "Create new lua script file in project, lua file MUST be saved under Assets/Resources folder");
			if (path.Length != 0) {
				// save a template lua file
				string clazz = Path.GetFileNameWithoutExtension(path);
				string buffer = "";
				buffer += "import(\"UnityEngine\")\n";
				buffer += "\n";
				buffer += string.Format("{0} = class(\"{0}\", function() return LuaLu.LuaComponent.new() end)\n", clazz);
				buffer += "\n";
				buffer += string.Format("function {0}:ctor()\n", clazz);
				buffer += "end\n";
				buffer += "\n";
				buffer += string.Format("function {0}:Start()\n", clazz);
				buffer += "end\n";
				buffer += "\n";
				buffer += string.Format("function {0}:Update()\n", clazz);
				buffer += "end\n";
				File.WriteAllText(path, buffer);

				// highlight it
				AssetDatabase.Refresh();
				Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
				EditorGUIUtility.PingObject(Selection.activeObject);
			}
			return path;
		}

		public static string NewStandaloneLuaFile() {
			string path = EditorUtility.SaveFilePanelInProject("New Lua Script", "NewLuaScript.lua", "lua", "Create new lua script file in project, lua file MUST be saved under Assets/Resources folder");
			if(path.Length != 0) {
				// save a template lua file
				string clazz = Path.GetFileNameWithoutExtension(path);
				string buffer = "";
				buffer += "import(\"UnityEngine\")\n";
				buffer += "\n";
				buffer += string.Format("{0} = class(\"{0}\")\n", clazz);
				buffer += "\n";
				buffer += string.Format("function {0}:ctor()\n", clazz);
				buffer += "end\n";
				File.WriteAllText(path, buffer);

				// highlight it
				AssetDatabase.Refresh();
				Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
				EditorGUIUtility.PingObject(Selection.activeObject);
			}
			return path;
		}
	}
}