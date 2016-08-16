using UnityEngine;
using System.Collections;
using LuaInterface;

[AddComponentMenu("Lua/Lua Script")]
public class LuaComponent : MonoBehaviour {
	// default file index
	private static int m_fileIndex = 1;

	// default file name
	public string m_luaFile = DefaultFileName();

	// file name is set or not
	public bool m_fileBound = false;

	// generate default file name
	static string DefaultFileName() {
		string fn = "Untitled" + m_fileIndex + ".lua";
		m_fileIndex++;
		return fn;
	}

	void Start() {
		LuaStack L = new LuaStack();
		L.ExecuteString("print(\"hello, it works!!!\")");
	}
}
