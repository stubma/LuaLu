namespace LuaInterface {
	using System;
	using System.IO;
	using System.Collections;
	using System.Collections.Generic;
	using System.Collections.Specialized;
	using System.Reflection;
	using System.Security;
	using System.Runtime.InteropServices;
	using System.Threading;
	using System.Text;
	using UnityEngine;
	using LuaLu;

	/// <summary>
	/// it manages one lua state machine, and provides helper method to ease lua api usage
	/// </summary>
	[NoLuaBinding]
	public class LuaStack : IDisposable {
		// lua state
		private IntPtr L;

		// flag indicating calling in lua
		private int m_callFromLua;

		// hash -> object, hold native object in case it is recycled
		private Dictionary<object, int> m_objRefIdMap;
		private Dictionary<int, object> m_refIdObjMap;
		private Dictionary<int, int> m_objRefCountMap;
		private Dictionary<int, string> m_objNameMap;
		private Dictionary<int, bool> m_keepAliveObjs;

		// hash of object which should be released next update
		private Dictionary<int, int> m_autoReleasePool;

		// unique increasing id for object
		private static int s_nextRefId;

		// lua asset bundle list
		private static List<string> s_luaAssetBundleNames;

		// lua files in asset bundle
		private static Dictionary<string, List<string>> s_bundledLuaFiles;

		// for log support
		public delegate void LogDelegate(string str);

		// shared lua stack instance
		private volatile static LuaStack s_sharedInstance;

		// lock
		private static object s_lockRoot = new System.Object();

		// global init flag
		private static volatile bool s_globalInited = false;

		// stack state map, to support multi lua state
		private static Dictionary<IntPtr, LuaStack> s_stackMap;

		static LuaStack() {
			// init static
			s_luaAssetBundleNames = new List<string>();
			s_bundledLuaFiles = new Dictionary<string, List<string>>();
			s_stackMap = new Dictionary<IntPtr, LuaStack>();
			s_nextRefId = 1;

			// load lua asset bundle names and load lua file list in bundles
			TextAsset t = Resources.Load<TextAsset>(LuaConst.LUA_ASSET_BUNDLE_LIST_FILE);
			if(t != null) {
				StringReader r = new StringReader(t.text);
				string line = r.ReadLine();
				while(line != null) {
					if(line != "") {
						s_luaAssetBundleNames.Add(line);

						// load file list
						List<string> files = new List<string>();
						AssetBundle ab = LoadAssetBundle(line);
						if(ab != null) {
							string[] fileArray = ab.GetAllAssetNames();
							foreach(string f in fileArray) {
								string resPath = f.Substring(LuaConst.GENERATED_LUA_PREFIX.Length);
								string resDir = Path.GetDirectoryName(resPath);
								string resName = Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(resPath));
								string requirePath = Path.Combine(resDir, resName);
								files.Add(requirePath);
							}
							s_bundledLuaFiles[line] = files;
							ab.Unload(false);
						}
					}
					line = r.ReadLine();
				}
				r.Close();
			}
		}

		/// <summary>
		/// shared lua stack, a.k.a. global lua state
		/// </summary>
		/// <value>The shared instance</value>
		public static LuaStack SharedInstance() {
			if(s_sharedInstance == null) {
				lock(s_lockRoot) {
					if(s_sharedInstance == null) {
						s_sharedInstance = new LuaStack();
					}
				}
			}
			return s_sharedInstance;
		}

		/// <summary>
		/// initialize global lua state, and load core lua scripts
		/// </summary>
		public static void InitGlobalState() {
			if(!s_globalInited) {
				lock(s_lockRoot) {
					SharedInstance();
					s_globalInited = true;
				}
			}
		}

		public static LuaStack FromState(IntPtr L) {
			return s_stackMap[L];
		}

		public LuaStack() {
			// init member
			m_refIdObjMap = new Dictionary<int, object>();
			m_objRefCountMap = new Dictionary<int, int>();
			m_objNameMap = new Dictionary<int, string>();
			m_objRefIdMap = new Dictionary<object, int>();
			m_autoReleasePool = new Dictionary<int, int>();
			m_keepAliveObjs = new Dictionary<int, bool>();
			m_callFromLua = 0;

			// set log function
			LogDelegate logd = new LogDelegate(LogCallback);
			IntPtr logPtr = Marshal.GetFunctionPointerForDelegate(logd);
			LuaLib.set_unity_log_func(logPtr);

			// create lua state
			L = LuaLib.luaL_newstate();
			if(L != IntPtr.Zero) {
				// register standard and thiry-party libs
				LuaLib.luaL_openlibs(L);
				LuaLib.luaopen_lfs(L);
				LuaLib.luaopen_cjson(L);
				LuaLib.luaopen_socket_core(L);
				LuaLib.toluafix_open(L);

				// register unity classes if lua bindings are generated
				// use reflect in case bindings are not generate
				Type t = Type.GetType("LuaLu.lua_register_unity", false);
				if(t != null) {
					MethodInfo mi = t.GetMethod("RegisterAll", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(IntPtr) }, null);
					if(mi != null) {
						mi.Invoke(null, new object[] { L });
					}
				}

				// Register our version of the global "print" function
				luaL_Reg[] global_functions = {
					new luaL_Reg("print", new LuaFunction(LuaPrint)),
					new luaL_Reg(null, null)
				};
				LuaLib.luaL_register(L, "_G", global_functions);

				// map it
				s_stackMap[L] = this;
			} else {
				Debug.Log("Fatal error: Failed to create lua state!");
			}

			// add lua loader
			AddLuaLoader(new LuaFunction(LuaLoader));

			// load core lib
			ExecuteString("require(\"core/__init__\")");
		}

		private static AssetBundle LoadAssetBundle(string abName) {
			string abPath = Application.persistentDataPath + "/../assetbundles/" + abName;
			if(File.Exists(abPath)) { 
				return AssetBundle.LoadFromFile(abPath);
			} else {
				return null;
			}
		}

		/// <summary>
		/// log callback for native plugin, it is set before lua state is created. By this callabck,
		/// native plugin can output log to unity console
		/// </summary>
		/// <param name="str">log string</param>
		[MonoPInvokeCallback(typeof(LogDelegate))]
		static void LogCallback(string str) {
			Debug.Log("[" + LuaLib.GetLibName() + "]: " + str);
		}

		/// <summary>
		/// custom lua print global function, it bridges lua print function to unity
		/// console log
		/// </summary>
		/// <returns>useless, just ignored</returns>
		/// <param name="L">lua state</param>
		[MonoPInvokeCallback(typeof(LuaFunction))]
		static int LuaPrint(IntPtr L) {
			int nargs = LuaLib.lua_gettop(L);

			string t = "";
			for(int i = 1; i <= nargs; i++) {
				if(LuaLib.lua_istable(L, i)) {
					t += "table";
				} else if(LuaLib.lua_isnone(L, i)) {
					t += "none";
				} else if(LuaLib.lua_isnil(L, i)) {
					t += "nil";
				} else if(LuaLib.lua_isboolean(L, i)) {
					if(LuaLib.lua_toboolean(L, i) != 0) {
						t += "true";
					} else {
						t += "false";
					}
				} else if(LuaLib.lua_isfunction(L, i)) {
					t += "function";
				} else if(LuaLib.lua_islightuserdata(L, i)) {
					t += "lightuserdata";
				} else if(LuaLib.lua_isthread(L, i)) {
					t += "thread";
				} else {
					if(LuaLib.lua_isstring(L, i)) {
						t += LuaLib.lua_tostring(L, i);
					} else {
						t += LuaLib.lua_typename(L, LuaLib.lua_type(L, i));
					}
				}
				if(i != nargs) {
					t += "\t";
				}
			}
			Debug.Log(string.Format("[LUA-print] {0}", t));

			return 0;
		}

		/// <summary>
		/// garbage collection for lua side, it clears native object
		/// </summary>
		/// <returns>The G.</returns>
		/// <param name="L">L.</param>
		[MonoPInvokeCallback(typeof(LuaFunction))]
		public static int LuaGC(IntPtr L) {
			int refId = LuaLib.tolua_tousertype(L, -1);
			LuaStack.FromState(L).RemoveObject(refId);
			return 0;
		}

		/// <summary>
		/// Find lua asset from file path relative to Resources, it try asset bundle first, then
		/// fallback to app Resources if not found
		/// </summary>
		/// <returns>The lua text asset object</returns>
		/// <param name="requirePath">path in lua require directive</param>
		static TextAsset FindLuaAsset(string requirePath) {
			// remove extension
			if(requirePath.EndsWith(".lua")) {
				requirePath = requirePath.Substring(0, requirePath.Length - 4);
			}

			// text asset of lua
			TextAsset luaAsset = null;

			// first check asset bundles, asset bundle use lowercase 
			// so we have to convert path to lowercase
			string lowerRequirePath = requirePath.ToLower();
			foreach(string bn in s_luaAssetBundleNames) {
				if(s_bundledLuaFiles.ContainsKey(bn)) {
					List<string> files = s_bundledLuaFiles[bn];
					if(files.Contains(lowerRequirePath)) {
						// try to get asset from asset bundle
						AssetBundle ab = LoadAssetBundle(bn);
						if(ab != null) {
							string abPath = LuaConst.GENERATED_LUA_PREFIX.ToLower() + lowerRequirePath + ".lua.bytes";
							luaAsset = ab.LoadAsset<TextAsset>(abPath);
							ab.Unload(false);
						}

						// end search if matched
						break;
					}
				}
			}

			// try to get asset from resource
			string path = requirePath + ".lua";
			if(luaAsset == null) {
				luaAsset = Resources.Load<TextAsset>(path);
			}

			// return
			return luaAsset;
		}

		/// <summary>
		/// custom lua loader for unity
		/// </summary>
		/// <returns>useless, just ignored</returns>
		/// <param name="L">lua state</param>
		[MonoPInvokeCallback(typeof(LuaFunction))]
		static int LuaLoader(IntPtr L) {
			// original filepath, remove extension
			string requirePath = LuaLib.luaL_checkstring(L, 1);

			// try to find lua asset
			TextAsset luaAsset = FindLuaAsset(requirePath);

			// load lua
			if(luaAsset != null) {
				if(LuaLib.luaL_loadbuffer(L, luaAsset.bytes, luaAsset.bytes.Length, requirePath) != 0) {
					Debug.Log(string.Format("error loading module {0} from file{1} :\n\t{2}",
						LuaLib.lua_tostring(L, 1), requirePath, LuaLib.lua_tostring(L, -1)));
				}
			} else {
				Debug.Log(string.Format("Can't get lua file data from {0}", requirePath));
			}

			return 1;
		}

		public void Close() {
			Dispose();
		}

		public void Dispose() {
			if(L != IntPtr.Zero) {
				s_stackMap.Remove(L);
				LuaLib.lua_close(L);
				L = IntPtr.Zero;
			}
			GC.SuppressFinalize(this);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		/// <summary>
		/// Method used to get a pointer to the lua_State that the script module is attached to.
		/// </summary>
		/// <returns>A pointer to the lua_State that the script module is attached to.</returns>
		public IntPtr GetLuaState() {
			return L;
		}

		public object FindObject(int hash) {
			if(m_refIdObjMap.ContainsKey(hash)) {
				return m_refIdObjMap[hash];
			} else {
				return null;
			}
		}

		void RemoveObject(int refId) {
			if(m_objRefCountMap[refId] > 0) {
				// sometimes, lua side perform a gc to collect userdata, then
				// native side want to refer this object again, to recover from
				// such situation, we push this object again to re-establish data
				// struct in lua side, and abort removing
				string typeName = m_objNameMap[refId];
				LuaLib.tolua_pushusertype(L, refId, typeName, true);
				LuaLib.lua_pop(L, 1);
			} else {
				object obj = m_refIdObjMap[refId];
				m_objRefIdMap.Remove(obj);
				m_refIdObjMap.Remove(refId);
				m_objRefCountMap.Remove(refId);
				m_objNameMap.Remove(refId);
			}
		}

		int RegisterObject(object obj, string typeName) {
			// if obj is not in ref id map, then allocate a ref id to object
			// we can't use hashcode, because hashcode may duplicated
			int refId = 0;
			if(!m_objRefIdMap.ContainsKey(obj)) {
				refId = s_nextRefId++;
				m_objRefIdMap[obj] = refId;
			} else {
				refId = m_objRefIdMap[obj];
			}

			// now map it by ref id
			if(m_refIdObjMap.ContainsKey(refId)) {
				// if this object is kept alive, set it ref count to 1 always
				if(m_keepAliveObjs.ContainsKey(refId)) {
					m_objRefCountMap[refId] = 1;
				} else {
					int rc = m_objRefCountMap[refId];
					m_objRefCountMap[refId] = rc + 1;
				}
			} else {
				m_refIdObjMap[refId] = obj;
				m_objRefCountMap[refId] = 1;
				m_objNameMap[refId] = typeName;
			}

			// return ref id of this object
			return refId;
		}

		/// <summary>
		/// Push an object onto lua stack
		/// </summary>
		/// <param name="obj">Object to be push</param>
		/// <param name="typeName">object type name</param>
		/// <param name="keepAlive">If set to <c>true</c>, the object will always keep retain count 1 so that
		/// lua side info will not be recycled. Until you call <c>ReleaseObject</c> it will not be removed from
		/// lua side.</param>
		public void PushObject(object obj, string typeName, bool keepAlive = false) {
			// register object
			int refId = RegisterObject(obj, typeName);

			// if object is pushed before with keepAlive flag set, then it should be always kept alive
			if(m_keepAliveObjs.ContainsKey(refId)) {
				keepAlive = true;
			}

			// actually we alwasy add this type to root, but if keepAlive is false, it will be removed
			// from root next update. This mechanism is way like cocos2dx autorelease and its purpose is
			// preventing it to be collected in current event loop
			LuaLib.tolua_pushusertype(L, refId, typeName, true);
			if(!keepAlive) {
				AutoRelease(refId);
			} else {
				m_keepAliveObjs[refId] = true;
			}
		}

		/// <summary>
		/// Release a object which is kept alive. If an object is not kept alive, this
		/// method does nothing
		/// </summary>
		/// <param name="obj">Object to be cancelled from keep alive state</param>
		public void ReleaseObject(object obj) {
			if(m_objRefIdMap.ContainsKey(obj)) {
				int refId = m_objRefIdMap[obj];
				if(m_keepAliveObjs.ContainsKey(refId)) {
					m_keepAliveObjs.Remove(refId);
					AutoRelease(refId);
				}
			}
		}

		public void ReplaceObject(int refId, object newObj) {
			// replace map
			if(m_refIdObjMap.ContainsKey(refId)) {
				object oldObj = m_refIdObjMap[refId];
				m_objRefIdMap.Remove(oldObj);
				m_objRefIdMap[newObj] = refId;
				m_refIdObjMap[refId] = newObj;
			}
		}

		/// <summary>
		/// record a object need to be auto released next update
		/// </summary>
		/// <param name="hash">object hash</param>
		void AutoRelease(int hash) {
			if(m_autoReleasePool.ContainsKey(hash)) {
				int ar = m_autoReleasePool[hash];
				m_autoReleasePool[hash] = ar + 1;
			} else {
				m_autoReleasePool[hash] = 1;
			}
		}

		/// <summary>
		/// you should have a place to call this method
		/// </summary>
		public void LateUpdate() {
			DrainAutoReleasePool();
		}

		/// <summary>
		/// auto release some objects from value root
		/// </summary>
		private void DrainAutoReleasePool() {
			if(m_autoReleasePool.Count > 0) {
				foreach(int hash in m_autoReleasePool.Keys) {
					int rc = m_objRefCountMap[hash];
					rc -= m_autoReleasePool[hash];
					m_objRefCountMap[hash] = rc;
					if(rc <= 0) {
						LuaLib.tolua_remove_value_from_root(L, hash);
					}
				}
				m_autoReleasePool.Clear();
			}
		}

		/// <summary>
		/// Add a path to find lua files in
		/// </summary>
		/// <param name="path">to be added to the Lua path.</param>
		public void AddSearchPath(string path) {
			LuaLib.lua_getglobal(L, "package"); /* L: package */
			LuaLib.lua_getfield(L, -1, "path"); /* get package.path, L: package path */
			string cur_path = LuaLib.lua_tostring(L, -1);
			LuaLib.lua_pushstring(L, string.Format("{0};{1}/?.lua", cur_path, path));  /* L: package path newpath */
			LuaLib.lua_setfield(L, -3, "path"); /* package.path = newpath, L: package path */
			LuaLib.lua_pop(L, 2); /* L: - */
		}

		/// <summary>
		/// add custom lua file loader
		/// </summary>
		/// <param name="func">lua loader function</param>
		public void AddLuaLoader(LuaFunction func) {
			if(func == null) {
				return;
			}

			// stack content after the invoking of the function
			// get loader table
			LuaLib.lua_getglobal(L, "package");                                  /* L: package */
			LuaLib.lua_getfield(L, -1, "loaders");                               /* L: package, loaders */

			// insert loader into index 2
			LuaLib.lua_pushcfunction(L, Marshal.GetFunctionPointerForDelegate(func));                                   /* L: package, loaders, func */
			for(int i = LuaLib.lua_objlen(L, -2) + 1; i > 2; --i) {
				LuaLib.lua_rawgeti(L, -2, i - 1);                                /* L: package, loaders, func, function */
				// we call lua_rawgeti, so the loader table now is at -3
				LuaLib.lua_rawseti(L, -3, i);                                    /* L: package, loaders, func */
			}
			LuaLib.lua_rawseti(L, -2, 2);                                        /* L: package, loaders */

			// set loaders into package
			LuaLib.lua_setfield(L, -2, "loaders");                               /* L: package */

			LuaLib.lua_pop(L, 1);
		}

		/// <summary>
		/// check if two script function handlers point to same function
		/// </summary>
		/// <returns><c>true</c>, if script functions are same, <c>false</c> otherwise.</returns>
		/// <param name="handler1">function lua handler</param>
		/// <param name="handler2">function lua handler</param>
		public bool IsScriptFunctionSame(int handler1, int handler2) {
			LuaLib.toluafix_get_function_by_refid(L, handler1);
			LuaLib.toluafix_get_function_by_refid(L, handler2);
			bool ret = LuaLib.lua_rawequal(L, -1, -2);
			LuaLib.lua_pop(L, 2);
			return ret;
		}

		/// <summary>
		/// Remove script side user data
		/// </summary>
		/// <param name="nRefId">reference id</param>
		public void RemoveScriptUserData(int nRefId) {
			LuaLib.toluafix_remove_table_by_refid(L, nRefId);
		}

		/// <summary>
		/// Remove Lua function reference
		/// </summary>
		/// <param name="nHandler">lua function handler</param>
		public void RemoveScriptHandler(int nHandler) {
			LuaLib.toluafix_remove_function_by_refid(L, nHandler);
		}

		/// <summary>
		/// Execute script code contained in the given string
		/// </summary>
		/// <returns>0 if the string is excuted correctly. other value if the string is executed wrongly</returns>
		/// <param name="codes">holding the valid script code that should be executed.</param>
		public int ExecuteString(string codes) {
			LuaLib.luaL_loadstring(L, codes);
			return ExecuteFunction(0);
		}

		/// <summary>
		/// execute a lua script file
		/// </summary>
		/// <returns>zero means execution is ok, non-zero means error</returns>
		/// <param name="path">The script file path, it should be placed in Assets/Resources</param>
		public int ExecuteScriptFile(string path) {
			// remove unecessary prefix
			if(path.StartsWith(LuaConst.USER_LUA_PREFIX)) {
				path = path.Substring(LuaConst.USER_LUA_PREFIX.Length);
			}

			// find lua text asset
			TextAsset luaAsset = FindLuaAsset(path);

			// if text asset is found
			if(luaAsset != null) {
				// do string
				++m_callFromLua;
				int nRet = LuaLib.luaL_dostring(L, luaAsset.text);
				--m_callFromLua;

				// check return
				if(nRet != 0) {
					Debug.Log(string.Format("[LUA ERROR] {0}", LuaLib.lua_tostring(L, -1)));
					LuaLib.lua_pop(L, 1);
					return nRet;
				}
			}

			// default return
			return 0;
		}

		/// <summary>
		/// invoke a lua function
		/// </summary>
		/// <returns>value if the function returns a number, or zero if the function returns other type value</returns>
		/// <param name="numArgs">number of parameters</param>
		/// <param name="collector">if the function returns non-number type, can set a collector to convert returned value</param>
		public int ExecuteFunction(int numArgs, Action<IntPtr, int> collector = null) {
			int functionIndex = -(numArgs + 1);
			if(!LuaLib.lua_isfunction(L, functionIndex)) {
				Debug.Log(string.Format("value at stack [{0}] is not function", functionIndex));
				LuaLib.lua_pop(L, numArgs + 1); // remove function and arguments
				return 0;
			}

			// add trackback
			int traceback = 0;
			LuaLib.lua_getglobal(L, "__G__TRACKBACK__");                         /* L: ... func arg1 arg2 ... G */
			if(!LuaLib.lua_isfunction(L, -1)) {
				LuaLib.lua_pop(L, 1);                                            /* L: ... func arg1 arg2 ... */
			} else {
				LuaLib.lua_insert(L, functionIndex - 1);                         /* L: ... G func arg1 arg2 ... */
				traceback = functionIndex - 1;
			}

			int error = 0;
			int oldTop = LuaLib.lua_gettop(L);
			++m_callFromLua;
			error = LuaLib.lua_pcall(L, numArgs, LuaLib.LUA_MULTRET, traceback);                  /* L: ... [G] ret */
			int nresult = LuaLib.lua_gettop(L) - oldTop + numArgs + 1;
			--m_callFromLua;
			if(error != 0) {
				if(traceback == 0) {
					Debug.Log(string.Format("[LUA ERROR] {0}", LuaLib.lua_tostring(L, -1)));        /* L: ... error */
					LuaLib.lua_pop(L, 1); // remove error message from stack
				} else {                                                            /* L: ... G error */
					LuaLib.lua_pop(L, 2); // remove __G__TRACKBACK__ and error message from stack
				}
				return 0;
			}

			// get return value
			int ret = 0;
			if(collector != null) {
				collector(L, nresult);
			} else if(LuaLib.lua_isnumber(L, -1)) {
				ret = (int)LuaLib.lua_tointeger(L, -1);
			} else if(LuaLib.lua_isboolean(L, -1)) {
				ret = LuaLib.lua_toboolean(L, -1);
			}

			// remove return value from stack
			LuaLib.lua_pop(L, nresult); /* L: ... [G] */

			if(traceback != 0) {
				LuaLib.lua_pop(L, 1); // remove __G__TRACKBACK__ from stack      /* L: ... */
			}

			return ret;
		}

		public int ExecuteObjectFunction(object obj, string funcName, Array args = null, bool isStatic = false, Action<IntPtr, int> collector = null) {
			// get func
			PushObject(obj, obj.GetType().GetNormalizedCodeName());
			PushString(funcName);
			LuaLib.lua_gettable(L, -2);

			// check func
			if(LuaLib.lua_isnil(L, -1) || !LuaLib.lua_isfunction(L, -1)) {
				LuaLib.lua_pop(L, 2);
				return 0;
			}

			// push obj if not static method
			int argc = 0;
			if(!isStatic) {
				LuaLib.lua_pushvalue(L, -2);
				argc++;
			}

			// push args
			if(args != null) {
				PushArray(args, true);
				argc += args.Length;
			}

			// exec
			int ret = ExecuteFunction(argc, collector);

			// pop obj
			LuaLib.lua_pop(L, 1);

			// return
			return ret;
		}

		/// <summary>
		/// Execute a scripted global function. The function should not take any parameters and should return an integer.
		/// </summary>
		/// <returns>The integer value returned from the script function.</returns>
		/// <param name="functionName">String object holding the name of the function, in the global script environment, that is to be executed.</param>
		public int ExecuteGlobalFunction(string functionName) {
			LuaLib.lua_getglobal(L, functionName);       /* query function by name, stack: function */
			if(!LuaLib.lua_isfunction(L, -1)) {
				Debug.Log(string.Format("[LUA ERROR] name '{0}' does not represent a Lua function", functionName));
				LuaLib.lua_pop(L, 1);
				return 0;
			}
			return ExecuteFunction(0);
		}

		public void Clean() {
			LuaLib.lua_settop(L, 0);
		}

		public void PushInt(int intValue) {
			LuaLib.lua_pushinteger(L, intValue);
		}

		public void PushFloat(float floatValue) {
			LuaLib.lua_pushnumber(L, floatValue);
		}

		public void PushBoolean(bool boolValue) {
			LuaLib.lua_pushboolean(L, boolValue);
		}

		public void PushString(string stringValue) {
			LuaLib.lua_pushstring(L, stringValue);
		}

		public void PushString(string stringValue, int length) {
			LuaLib.lua_pushlstring(L, Encoding.UTF8.GetBytes(stringValue), length);
		}

		public void PushNil() {
			LuaLib.lua_pushnil(L);
		}
			
		public void PushArray(Array array, bool flat = false) {
			LuaValueBoxer.type_to_luaval(L, array, flat);
		}

		public void PushDictionary(Dictionary<string, object> dict) {
			LuaValueBoxer.type_to_luaval(L, dict);
		}

		public bool PushFunctionByHandler(int nHandler) {
			LuaLib.toluafix_get_function_by_refid(L, nHandler);
			if(!LuaLib.lua_isfunction(L, -1)) {
				Debug.Log(string.Format("[LUA ERROR] function refid '{0}' does not reference a Lua function", nHandler));
				LuaLib.lua_pop(L, 1);
				return false;
			}
			return true;
		}

		public void Pop(int count) {
			LuaLib.lua_pop(L, count);
		}

		public int ExecuteFunctionByHandler(int nHandler, int numArgs, Action<IntPtr, int> collector = null) {
			int ret = 0;
			if(PushFunctionByHandler(nHandler)) {                                /* L: ... arg1 arg2 ... func */
				if(numArgs > 0) {
					LuaLib.lua_insert(L, -(numArgs + 1));                        /* L: ... func arg1 arg2 ... */
				}
				ret = ExecuteFunction(numArgs, collector);
			}
			LuaLib.lua_settop(L, 0);
			return ret;
		}

		public bool HandleAssert(string msg) {
			if(m_callFromLua == 0)
				return false;
			LuaLib.lua_pushstring(L, string.Format("ASSERT FAILED ON LUA EXECUTE: {0}", msg != null ? msg : "unknown"));
			LuaLib.lua_error(L);
			return true;
		}

		public int ReallocateScriptHandler(int nHandler) {
			int nNewHandle = -1;
			if(PushFunctionByHandler(nHandler)) {
				nNewHandle = LuaLib.toluafix_ref_function(L, LuaLib.lua_gettop(L), 0);
			}
			return nNewHandle;
		}

		public void BindInstanceToLuaClass(object obj, string luaType) {
			// push obj to a tmp global
			PushObject(obj, obj.GetType().FullName, true);
			LuaLib.lua_setglobal(L, "__tmp_obj__");

			// build script and run
			string buffer = "local t = _G[\"__tmp_obj__\"]\n"; // get global to local
			buffer += "_G[\"__tmp_obj__\"] = nil\n"; // remove global
			buffer += string.Format("for k,v in pairs({0}) do t[k] = v end\n", luaType); // copy fields from class to native object
			buffer += string.Format("t.class = {0}\n", luaType); // assign class to instance
			buffer += "callCtor(t, t.super)\n"; // call super ctor
			buffer += "t:ctor()\n"; // call self ctor
			ExecuteString(buffer);
		}
	}
}