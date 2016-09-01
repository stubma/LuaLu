namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using UnityEditor;
	using System.IO;

	[CustomEditor(typeof(LuaComponent))]
	public class LuaInspector : Editor {
		// properities of LuaComponent
		SerializedProperty m_luaFileProp;
		SerializedProperty m_fileBoundProp;

		void OnEnable () {
			// Setup the SerializedProperties.
			m_luaFileProp = serializedObject.FindProperty ("m_luaFile");
			m_fileBoundProp = serializedObject.FindProperty ("m_fileBound");

			// if the lua file is not bound yet, open save file panel to create a new lua file
			if (!m_fileBoundProp.boolValue) {
				string path = EditorUtility.SaveFilePanelInProject ("New Lua Script", m_luaFileProp.stringValue, "lua", "Create new lua script file in project");
				if (path.Length != 0) {
					// save a template lua file
					string filename = Path.GetFileNameWithoutExtension(path);
					string buffer = "";
					buffer += "import(\"UnityEngine\")\n";
					buffer += "\n";
					buffer += string.Format("{0} = class(\"{0}\", function() return LuaLu.LuaComponent.new() end)\n", filename);
					buffer += "\n";
					buffer += string.Format("function {0}:ctor()\n", filename);
					buffer += "end\n";
					buffer += "\n";
					buffer += string.Format("function {0}:Start()\n", filename);
					buffer += "end\n";
					buffer += "\n";
					buffer += string.Format("function {0}:Update()\n", filename);
					buffer += "end\n";
					File.WriteAllText(path, buffer);

					// write file path back
					m_luaFileProp.stringValue = path;
					m_fileBoundProp.boolValue = true;
					serializedObject.ApplyModifiedProperties ();

					// refresh folder
					AssetDatabase.ImportAsset(path);

					// select file
					Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(path);
				} else {
					DestroyImmediate (target);
				}
			}
		}

		public override void OnInspectorGUI() {
			// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
			serializedObject.Update ();

			// text field for lua file path
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Lua File:");
			EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(m_luaFileProp.stringValue), EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight), GUILayout.ExpandWidth(true));
			Rect filenameRect = GUILayoutUtility.GetLastRect();
			EditorGUILayout.EndHorizontal();

			// if file not exist, show error and re-bound button
			// if file exists, check file name click, single click to select, double click to open
			if(!File.Exists(m_luaFileProp.stringValue)) {
				// create rebound ui
				EditorGUILayout.BeginHorizontal();
				GUIStyle style = new GUIStyle(EditorStyles.label);
				style.normal.textColor = Color.red;
				EditorGUILayout.LabelField("The lua file can not be located.", style);
				if(GUILayout.Button("Browse")) {
					string path = EditorUtility.OpenFilePanel("Locate Lua Script", "", "lua");
					if(path.Length != 0) {
						string appPath = Directory.GetParent(Application.dataPath).FullName;
						if(path.StartsWith(appPath)) {
							path = path.Substring(appPath.Length);
							if(path.StartsWith("/")) {
								path = path.Substring(1);
							}
						}
						m_luaFileProp.stringValue = path;
					}
				}
				EditorGUILayout.EndHorizontal();
			} else {
				// check file name click, single click to select, double click to open
				Event e = Event.current;
				if(e.type == EventType.MouseDown && e.button == 0 && filenameRect.Contains(e.mousePosition)) {
					if(e.clickCount == 1) {
						UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(m_luaFileProp.stringValue);
						EditorGUIUtility.PingObject(asset);
						e.Use();
					} else if(e.clickCount == 2) {
						UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(m_luaFileProp.stringValue);
						AssetDatabase.OpenAsset(asset);
						e.Use();
					}
				}

				// check lua folder, must in Assets/Resources
				if(!m_luaFileProp.stringValue.StartsWith("Assets/Resources/")) {
					GUIStyle style = new GUIStyle(EditorStyles.label);
					style.normal.textColor = Color.red;
					style.wordWrap = true;
					EditorGUILayout.LabelField("Currently you must place lua script in Assets/Resources folder, otherwise it won't run", style);
				}
			}
				
			// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
			serializedObject.ApplyModifiedProperties ();
		}
	}
}