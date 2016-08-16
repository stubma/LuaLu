namespace LuaLu {
	using UnityEditor;
	using System.Collections;
	using UnityEngine;

	public class LuaMenu : ScriptableObject {
		[MenuItem("Lua/Re-Generate Unity Lua Binding")]  
		static void MenuGenerateBinding() {  
			Transform[] transforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable);  

			foreach(Transform transform in transforms) {  
				GameObject newChild = new GameObject("_Child");  
				newChild.transform.parent = transform;  
			}  
		}
	}
}