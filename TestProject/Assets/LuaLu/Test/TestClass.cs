namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;

	public class TestClass {
		public bool TestPrimitiveTypes(byte arg0, sbyte arg1, char arg2, bool arg3, short arg4, ushort arg5,
		                               int arg6, uint arg7, decimal arg8, long arg9, ulong arg10, float arg11, double arg12) {
			return arg0 == 100 &&
			arg1 == 100 &&
			arg2 == 100 &&
			arg3 == true &&
			arg4 == 100 &&
			arg5 == 100 &&
			arg6 == 100 &&
			arg7 == 100 &&
			arg8 == 100 &&
			arg9 == 100 &&
			arg10 == 100 &&
			arg11 == 100 &&
			arg12 == 100;
		}

		public bool TestValueTruncate(byte arg0, sbyte arg1, char arg2, short arg3, ushort arg4) {
			return arg0 == 0x78 &&
			arg1 == 0x78 &&
			arg2 == 0x5678 &&
			arg3 == 0x5678 &&
			arg4 == 0x5678;
		}

		public bool TestListInt(List<int> arg0) {
			return arg0.Count == 3 &&
			arg0[0] == 100 &&
			arg0[1] == 200 &&
			arg0[2] == 300;
		}

		public bool TestListValueTruncate(List<byte> arg0, List<char> arg1) {
			return arg0.Count == 2 &&
			arg0[0] == 0x78 &&
			arg0[1] == 0x78 &&
			arg1.Count == 2 &&
			arg1[0] == 0x5678 &&
			arg1[1] == 0x5678;
		}

		public bool TestDictionaryIntInt(Dictionary<int, int> arg0) {
			return arg0.Count == 3 &&
			arg0[100] == 101 &&
			arg0[200] == 202 &&
			arg0[300] == 303;
		}
	}
}
