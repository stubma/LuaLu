namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using UnityEditor;

	// supported editor
	public enum LuaEditor {
		SYSTEM_DEFAULT = 0,
		ZERO_BRANE_STUDIO = 1,
		CUSTOM = 2
	}

	[NoLuaBinding]
	public class LuaLuPrefs {
		// pref key
		private const string PREF_LUA_EDITOR = "com.luma.lualu.editor";
		private const string PREF_LUA_EDITOR_EXECUTABLE = "com.luma.lualu.editor.executable";
		private const string PREF_LUA_EDITOR_ARGUMENTS = "com.luma.lualu.editor.args";
		private const string PREF_ZERO_BRANE_STUDIO_FOLDER = "com.luma.lualu.zbs.folder";

		// editor choosed
		private static LuaEditor s_activeEditor;

		// custom editor executable
		private static string s_customEditorExecutable = "";

		// custom editor arguments, ${file} is lua file path
		private static string s_customEditorArguments = "";

		// zero brane studio folder
		private static string s_zbsFolder = "";

		public static LuaEditor GetLuaEditor() {
			return s_activeEditor;
		}

		public static string GetZeroBraneStudioFolder() {
			return s_zbsFolder;
		}

		public static string GetCustomEditorExecutable() {
			return s_customEditorExecutable;
		}

		public static string GetCustomEditorArguments() {
			return s_customEditorArguments;
		}

		static LuaLuPrefs() {
			s_activeEditor = (LuaEditor)EditorPrefs.GetInt(PREF_LUA_EDITOR, (int)LuaEditor.SYSTEM_DEFAULT);
			s_customEditorExecutable = EditorPrefs.GetString(PREF_LUA_EDITOR_EXECUTABLE, "");
			s_customEditorArguments = EditorPrefs.GetString(PREF_LUA_EDITOR_ARGUMENTS, "");
			s_zbsFolder = EditorPrefs.GetString(PREF_ZERO_BRANE_STUDIO_FOLDER, "");
		}

		[PreferenceItem("LuaLu")]
		public static void OnPreferenceGUI() {
			// load ui
			{
				// style
				GUIStyle optStyle = new GUIStyle(EditorStyles.radioButton);
				GUIStyle headerStyle = EditorStyles.boldLabel;
				GUIStyle readonlyTextFieldStyle = EditorStyles.textField;

				// section label
				EditorGUILayout.LabelField("Lua Editor", headerStyle);

				// option 1 - system default
				if(EditorGUILayout.Toggle("System Default", s_activeEditor == LuaEditor.SYSTEM_DEFAULT, optStyle)) {
					s_activeEditor = LuaEditor.SYSTEM_DEFAULT;
				}

				// option 2 - zero brane studio
				if(EditorGUILayout.Toggle("ZeroBraneStudio", s_activeEditor == LuaEditor.ZERO_BRANE_STUDIO, optStyle)) {
					s_activeEditor = LuaEditor.ZERO_BRANE_STUDIO;
				}

				// path for zero brane studio
				EditorGUI.BeginDisabledGroup(s_activeEditor != LuaEditor.ZERO_BRANE_STUDIO);
				EditorGUILayout.BeginHorizontal(GUILayout.Height(26));
				EditorGUILayout.Space();
				GUILayout.Label("Directory:");
				EditorGUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.SelectableLabel(s_zbsFolder, readonlyTextFieldStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndVertical();
				if(GUILayout.Button("Browse")) {
					string path = EditorUtility.OpenFolderPanel("Select ZeroBraneStudio Folder", "", "");
					if(path.Length != 0) {
						s_zbsFolder = path;
						OnPreferenceGUI();
					}
				}
				EditorGUILayout.Space();
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				// option 3 - custom
				if(EditorGUILayout.Toggle("Custom", s_activeEditor == LuaEditor.CUSTOM, optStyle)) {
					s_activeEditor = LuaEditor.CUSTOM;
				}

				// command line for custom editor
				EditorGUI.BeginDisabledGroup(s_activeEditor != LuaEditor.CUSTOM);
				EditorGUILayout.BeginHorizontal(GUILayout.Height(26));
				EditorGUILayout.Space();
				GUILayout.Label("Executable:");
				EditorGUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
				EditorGUILayout.SelectableLabel(s_customEditorExecutable, readonlyTextFieldStyle, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndVertical();
				if(GUILayout.Button("Browse")) {
					string path = EditorUtility.OpenFilePanel("Select Custom Editor Executable", "", "");
					if(path.Length != 0) {
						s_customEditorExecutable = path;
						OnPreferenceGUI();
					}
				}
				EditorGUILayout.Space();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal(GUILayout.Height(26));
				EditorGUILayout.Space();
				GUILayout.Label("Arguments:");
				EditorGUILayout.BeginVertical();
				GUILayout.FlexibleSpace();
				s_customEditorArguments = EditorGUILayout.TextField(s_customEditorArguments, GUILayout.ExpandWidth(true));
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndVertical();
				EditorGUILayout.Space();
				EditorGUILayout.EndHorizontal();
				EditorGUILayout.BeginHorizontal();
				GUILayout.FlexibleSpace();
				GUILayout.Label("Hint: use ${file} to be the placeholder of lua file");
				GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();
			}

			// save change
			if(GUI.changed) {
				EditorPrefs.SetInt(PREF_LUA_EDITOR, (int)s_activeEditor);
				EditorPrefs.SetString(PREF_LUA_EDITOR_EXECUTABLE, s_customEditorExecutable);
				EditorPrefs.SetString(PREF_LUA_EDITOR_ARGUMENTS, s_customEditorArguments);
				EditorPrefs.SetString(PREF_ZERO_BRANE_STUDIO_FOLDER, s_zbsFolder);
			}
		}
	}
}