namespace LuaLu {
	using UnityEditor;

	public class ExportLuaAssetBundles {
		[MenuItem("Lua/Build AssetBundles")]
		static void BuildAllAssetBundles() {
			BuildPipeline.BuildAssetBundles("Assets/StreamingAssets", BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXUniversal);
		}
	}
}