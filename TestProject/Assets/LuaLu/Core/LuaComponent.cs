namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using LuaInterface;

	/// <summary>
	/// a lua component which indirect c# calling to lua side. Every component
	/// should bind a lua script file and the lua file must be saved in Assets/Resources 
	/// folder
	/// </summary>
	[AddComponentMenu("Lua/Lua Script")]
	public class LuaComponent : MonoBehaviour {
		// default file index
		private static int m_fileIndex = 1;

		// default file name
		public string m_luaFile = DefaultFileName();

		// is lua file path valid?
		private bool m_valid = false;

		// file name is set or not
		public bool m_fileBound = false;

		// generate default file name
		static string DefaultFileName() {
			string fn = "Untitled" + m_fileIndex + ".lua";
			m_fileIndex++;
			return fn;
		}

		void OnValidate() {
			// check lua file path, it must be saved in Assets/Resources
			if(!m_luaFile.StartsWith("Assets/Resources/")) {
				Debug.Log("Currently LuaLu requires you save lua file in Assets/Resources folder");
				m_valid = false;
			} else {
				m_valid = true;
			}
		}

		void Awake() {
			// init global lua state
			LuaStack.InitGlobalState();
		}

		void Start() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua
			LuaStack L = LuaStack.SharedInstance();
			string finalPath = m_luaFile.Substring("Assets/Resources/".Length);
			TextAsset t = (TextAsset)Resources.Load(finalPath, typeof(TextAsset));
			if(t != null) {
				L.ExecuteString(t.text);
			} else {
				Debug.Log("t is null");
			}
		}

		void Update() {
			// if not valid, return
			if(!m_valid) {
				return;
			}
		}
	}
}