namespace LuaLu {
	using System.Collections.Generic;

	[NoLuaBinding]
	public class LuaConst {
		// simple name to class name
		public static Dictionary<string, string> SIMPLE_NAME_MAPPING = new Dictionary<string, string> {
			{ "void", "System.Void" },
			{ "float", "System.Single" },
			{ "string", "System.String" },
			{ "int", "System.Int32" },
			{ "long", "System.Int64" },
			{ "sbyte", "System.SByte" },
			{ "byte", "System.Byte" },
			{ "short", "System.Int16" },
			{ "ushort", "System.UInt16" },
			{ "char", "System.Char" },
			{ "uint", "System.UInt32" },
			{ "ulong", "System.UInt64" },
			{ "decimal", "System.Decimal" },
			{ "double", "System.Double" },
			{ "bool", "System.Boolean" }
		};

		// path prefix
		public const string CORE_LUA_PREFIX = "Assets/LuaLu/Resources/";
		public const string USER_LUA_PREFIX = "Assets/Resources/";
		public const string GENERATED_LUA_PREFIX = "Assets/Generated/Resources/";
		public const string ASSET_BUNDLE_OUTPUT_FOLDER = "Assets/AssetBundles";
		public const string GENERATED_LUA_BINDING_PREFIX = "Assets/Scripts/LuaBinding/";

		// files
		public const string LUA_ASSET_BUNDLE_LIST_FILE = "lua_asset_bundles";
	}
}