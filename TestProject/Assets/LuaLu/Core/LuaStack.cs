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

	/// <summary>
	/// a custom converter for lua returned value
	/// </summary>
	public interface IScriptReturnedValueCollector {
		void collectReturnedValue(IntPtr L);
	}

	/// <summary>
	/// it manages one lua state machine, and provides helper method to ease lua api usage
	/// </summary>
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
				// register standard and thiry-party libs
				LuaLib.luaL_openlibs(L);
				LuaLib.luaopen_lfs(L);
				LuaLib.luaopen_cjson(L);
				LuaLib.luaopen_socket_core(L);

				// Register our version of the global "print" function
				luaL_Reg[] global_functions = {
					new luaL_Reg("print", new LuaFunction(LuaPrint)),
					new luaL_Reg(null, null)
				};
				LuaLib.luaL_register(L, "_G", global_functions);
			} else {
				Debug.Log("Fatal error: Failed to create lua state!");
			}
		}

		/// <summary>
		/// log callback for native plugin, it is set before lua state is created. By this callabck,
		/// native plugin can output log to unity console
		/// </summary>
		/// <param name="str">log string</param>
		static void LogCallback(string str) {
			Debug.Log("[" + LuaLib.getLibName() + "]: " + str);
		}

		/// <summary>
		/// custom lua print global function, it bridges lua print function to unity
		/// console log
		/// </summary>
		/// <returns>useless, just ignored</returns>
		/// <param name="L">lua state</param>
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
				collector.collectReturnedValue(L);
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