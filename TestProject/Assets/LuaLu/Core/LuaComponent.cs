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

				// run lua side method
				#if UNITY_EDITOR
				if(Application.isPlaying) {
					L.ExecuteObjectFunction(this, "Awake");
				}
				#else
				L.ExecuteObjectFunction(this, "Awake");
				#endif
			}
		}

		void Start() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "Start");
		}

		void Update() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "Update");
		}

		void FixedUpdate () {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
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

			// run lua side method
			L.ExecuteObjectFunction(this, "LateUpdate");
		}

		void OnAnimatorIK(int layerIndex) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnAnimatorIK", new object[] { layerIndex });
		}

		void OnAnimatorMove() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnAnimatorMove");
		}

		void OnApplicationFocus(bool focusStatus) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnApplicationFocus", new object[] { focusStatus });
		}

		void OnApplicationPause(bool pauseStatus) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnApplicationPause", new object[] { pauseStatus });
		}

		void OnApplicationQuit() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnApplicationQuit");
		}

		void OnAudioFilterRead(float[] data, int channels) {
			// TODO: OnAudioFilterRead runs in other thread, so it will corrupt object map.
			// Furthermore, lua is not real multithread so we can't run it in shared lua state
//			// if not valid, return
//			if(!m_valid) {
//				return;
//			}
//
//			// run lua side method
//			LuaStack L = LuaStack.SharedInstance();
//			L.ExecuteObjectFunction(this, "OnAudioFilterRead", new object[] { data, channels });
		}

		void OnBecameInvisible() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnBecameInvisible");
		}

		void OnBecameVisible() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnBecameVisible");
		}

		void OnCollisionEnter(Collision collision) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnCollisionEnter", new object[] { collision });
		}

		void OnCollisionEnter2D(Collision2D coll) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnCollisionEnter2D", new object[] { coll });
		}

		void OnCollisionExit(Collision collisionInfo) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnCollisionExit", new object[] { collisionInfo });
		}

		void OnCollisionExit2D(Collision2D coll) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnCollisionExit2D", new object[] { coll });
		}

		void OnCollisionStay(Collision collisionInfo) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnCollisionStay", new object[] { collisionInfo });
		}

		void OnCollisionStay2D(Collision2D coll) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnCollisionStay2D", new object[] { coll });
		}

		void OnConnectedToServer() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnConnectedToServer");
		}

		void OnControllerColliderHit(ControllerColliderHit hit) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnControllerColliderHit", new object[] { hit });
		}

		void OnDestroy() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnDestroy");

			// release it because LuaComponent is a keep alive object
			L.ReleaseObject(this);
		}

		void OnDisable() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnDestroy");
		}

		void OnDisconnectedFromServer(NetworkDisconnection info) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnDisconnectedFromServer", new object[] { info });
		}

		void OnEnable() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnEnable");
		}

		void OnFailedToConnect(NetworkConnectionError error) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnFailedToConnect", new object[] { error });
		}

		void OnFailedToConnectToMasterServer(NetworkConnectionError info) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnFailedToConnectToMasterServer", new object[] { info });
		}

		void OnGUI() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnGUI");
		}

		void OnJointBreak(float breakForce) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnJointBreak", new object[] { breakForce });
		}

		void OnJointBreak2D(Joint2D brokenJoint) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnJointBreak2D", new object[] { brokenJoint });
		}

		void OnMasterServerEvent(MasterServerEvent msEvent) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMasterServerEvent", new object[] { msEvent });
		}

		void OnMouseDown() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseDown");
		}

		void OnMouseDrag() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseDrag");
		}

		void OnMouseEnter() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseEnter");
		}

		void OnMouseExit() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseExit");
		}

		void OnMouseOver() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseOver");
		}

		void OnMouseUp() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseUp");
		}

		void OnMouseUpAsButton() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnMouseUpAsButton");
		}

		void OnNetworkInstantiate(NetworkMessageInfo info) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnNetworkInstantiate", new object[] { info });
		}

		void OnParticleCollision(GameObject other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnParticleCollision", new object[] { other });
		}

		void OnParticleTrigger() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnParticleTrigger");
		}

		void OnPlayerConnected(NetworkPlayer player) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnPlayerConnected", new object[] { player });
		}

		void OnPlayerDisconnected(NetworkPlayer player) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnPlayerDisconnected", new object[] { player });
		}

		void OnPostRender() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnPostRender");
		}

		void OnPreCull() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnPreCull");
		}

		void OnPreRender() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnPreRender");
		}

		void OnRenderImage(RenderTexture src, RenderTexture dest) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnRenderImage", new object[] { src, dest });
		}

		void OnRenderObject() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnRenderObject");
		}

		void OnSerializeNetworkView(BitStream stream, NetworkMessageInfo info) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnSerializeNetworkView", new object[] { stream, info });
		}

		void OnServerInitialized() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnServerInitialized");
		}

		void OnTransformChildrenChanged() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTransformChildrenChanged");
		}

		void OnTransformParentChanged() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTransformParentChanged");
		}

		void OnTriggerEnter(Collider other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerEnter", new object[] { other });
		}

		void OnTriggerEnter2D(Collider2D other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerEnter2D", new object[] { other });
		}

		void OnTriggerExit(Collider other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerExit", new object[] { other });
		}

		void OnTriggerExit2D(Collider2D other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerExit2D", new object[] { other });
		}

		void OnTriggerStay(Collider other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerStay", new object[] { other });
		}

		void OnTriggerStay2D(Collider2D other) {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnTriggerStay2D", new object[] { other });
		}

		void OnWillRenderObject() {
			// if not valid, return
			if(!m_valid) {
				return;
			}

			// run lua side method
			LuaStack L = LuaStack.SharedInstance();
			L.ExecuteObjectFunction(this, "OnWillRenderObject");
		}
	}
}