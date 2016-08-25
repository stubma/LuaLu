namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using LuaInterface;
	using System.IO;
	using System;

	/// <summary>
	/// a lua component which indirect c# calling to lua side. Every component
	/// should bind a lua script file and the lua file must be saved in Assets/Resources 
	/// folder
	/// </summary>
	[AddComponentMenu("Lua/Lua Script")]
	public class LuaComponent : MonoBehaviour {
		// default file index
		private static int s_fileIndex;

		// default file name
		public string m_luaFile;

		// is lua file path valid?
		private bool m_valid = false;

		// is lua file loaded?
		private bool m_loaded = false;

		// file name is set or not
		public bool m_fileBound = false;

		// generate default file name
		static string DefaultFileName() {
			string fn = "Untitled" + s_fileIndex + ".lua";
			s_fileIndex++;
			return fn;
		}

		static LuaComponent() {
			s_fileIndex = 1;
		}

		public LuaComponent() {
			#if UNITY_EDITOR
			m_luaFile = DefaultFileName();
			#endif
		}

		void OnValidate() {
			// check lua file path, it must be saved in Assets/Resources
			if(!m_luaFile.StartsWith(LuaConst.USER_LUA_PREFIX)) {
				Debug.Log("Currently LuaLu requires you save lua file in Assets/Resources folder");
				m_valid = false;
			} else {
				m_valid = true;
			}

		}

		void Awake() {
			// init global lua state
			LuaStack.InitGlobalState();

			// validate
			#if !UNITY_EDITOR
			OnValidate();
			#endif

			// load script
			if(m_valid && !m_loaded) {
				string resPath = m_luaFile.Substring(LuaConst.USER_LUA_PREFIX.Length);
				string resDir = Path.GetDirectoryName(resPath);
				string resName = Path.GetFileNameWithoutExtension(resPath);
				string finalPath = Path.Combine(resDir, resName);
				LuaStack L = LuaStack.SharedInstance();
				L.ExecuteString("require(\"" + finalPath + "\")");
				m_loaded = true;

				// bind this instance to lua class
				// the class name must be same as lua file name
				string clazz = Path.GetFileNameWithoutExtension(m_luaFile);
				L.BindInstanceToLuaClass(this, clazz);
			}
		}

		void Start() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side Start
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "Start");
		}

		void Update() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side Update
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "Update");
		}

		void FixedUpdate () {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side Update
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "FixedUpdate");
		}

		void LateUpdate() {
			// stack late update
			LuaStack L = LuaStack.SharedInstance();
			L.LateUpdate();

			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side LateUpdate
			L.ExecuteObjectFunction(this, "LateUpdate");
		}

		void OnTriggerEnter(Collider other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side Update
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerEnter", new object[] { other });
		}
	}
}