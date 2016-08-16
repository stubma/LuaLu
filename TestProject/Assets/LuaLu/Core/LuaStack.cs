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

	public interface IScriptReturnedValueCollector {
		void collectReturnedValue();
	}

	public class LuaStack : IDisposable {
		private IntPtr L;
		private int m_callFromLua;

		// for log support
		#if !UNITY_IPHONE
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		#endif
		public delegate void LogDelegate(string str);

		public LuaStack() {
			// set log function
			LogDelegate logd = new LogDelegate(LogCallback);
			IntPtr logPtr = Marshal.GetFunctionPointerForDelegate(logd);
			LuaLib.set_unity_log_func(logPtr);

			// create lua state
			L = LuaLib.luaL_newstate();
			if(L != IntPtr.Zero) {
				LuaLib.luaL_openlibs(L);
			} else {
				Debug.Log("Fatal error: Failed to create lua state!");
			}
		}

		static void LogCallback(string str) {
			Debug.Log("[" + LuaLib.getLibName() + "]: " + str);
		}

		public void Close() {
			Dispose();
		}

		public void Dispose() {
			if(L != IntPtr.Zero) {
				LuaLib.lua_close(L);
				L = IntPtr.Zero;
			}
			GC.SuppressFinalize(this);
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}

		public int executeString(string codes) {
			LuaLib.luaL_loadstring(L, codes);
			return executeFunction(0, null);
		}

		public int executeFunction(int numArgs, IScriptReturnedValueCollector collector) {
			int functionIndex = -(numArgs + 1);
			if(!LuaLib.lua_isfunction(L, functionIndex)) {
				Debug.Log(string.Format("value at stack [{0}] is not function", functionIndex));
				LuaLib.lua_pop(L, numArgs + 1); // remove function and arguments
				return 0;
			}

			int traceback = 0;
			LuaLib.lua_getglobal(L, "__G__TRACKBACK__");                         /* L: ... func arg1 arg2 ... G */
			if(!LuaLib.lua_isfunction(L, -1)) {
				LuaLib.lua_pop(L, 1);                                            /* L: ... func arg1 arg2 ... */
			} else {
				LuaLib.lua_insert(L, functionIndex - 1);                         /* L: ... G func arg1 arg2 ... */
				traceback = functionIndex - 1;
			}

			int error = 0;
			++m_callFromLua;
			error = LuaLib.lua_pcall(L, numArgs, 1, traceback);                  /* L: ... [G] ret */
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
				collector.collectReturnedValue();
			} else if(LuaLib.lua_isnumber(L, -1)) {
				ret = (int)LuaLib.lua_tointeger(L, -1);
			} else if(LuaLib.lua_isboolean(L, -1)) {
				ret = LuaLib.lua_toboolean(L, -1);
			}

			// remove return value from stack
			LuaLib.lua_pop(L, 1);                                                /* L: ... [G] */

			if(traceback != 0) {
				LuaLib.lua_pop(L, 1); // remove __G__TRACKBACK__ from stack      /* L: ... */
			}

			return ret;
		}
	}
}