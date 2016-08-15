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
				// save a empty lua file
				File.WriteAllBytes (path, new byte[0]);

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
		EditorGUILayout.DelayedTextField (m_luaFileProp);

		// if file not exist, show error and re-bound button
		if (!File.Exists (m_luaFileProp.stringValue)) {
			// create rebound ui
			EditorGUILayout.BeginHorizontal ();
			GUIStyle style = new GUIStyle (EditorStyles.label);
			style.normal.textColor = Color.red;
			EditorGUILayout.LabelField ("The lua file can not be resolved.", style);
			if (GUILayout.Button ("Rebound")) {
				string path = EditorUtility.OpenFilePanel ("Locate Lua Script", "", "lua");
				if (path.Length != 0) {
					if (path.StartsWith (Application.dataPath)) {
						path = "Assets" + path.Substring (Application.dataPath.Length);
					}
					m_luaFileProp.stringValue = path;
				}
			}
			EditorGUILayout.EndHorizontal();
		}
			
		// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
		serializedObject.ApplyModifiedProperties ();
	}
}
