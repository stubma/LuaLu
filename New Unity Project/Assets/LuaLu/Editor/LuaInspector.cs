using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(LuaComponent))]
public class LuaInspector : Editor {
	SerializedProperty damageProp;

	void OnEnable () {
		// Setup the SerializedProperties.
		damageProp = serializedObject.FindProperty ("damage");
	}

	public override void OnInspectorGUI() {
		// Update the serializedProperty - always do this in the beginning of OnInspectorGUI.
		serializedObject.Update ();

		// Show the custom GUI controls.
		EditorGUILayout.IntSlider (damageProp, 0, 100, new GUIContent ("Damage"));

		// Only show the damage progress bar if all the objects have the same damage value:
		ProgressBar (damageProp.intValue / 100.0f, "Damage");
			
		// Apply changes to the serializedProperty - always do this in the end of OnInspectorGUI.
		serializedObject.ApplyModifiedProperties ();
	}

	// Custom GUILayout progress bar.
	void ProgressBar (float value, string label) {
		// Get a rect for the progress bar using the same margins as a textfield:
		Rect rect = GUILayoutUtility.GetRect (18, 18, "TextField");
		EditorGUI.ProgressBar (rect, value, label);
		EditorGUILayout.Space ();
	}
}
