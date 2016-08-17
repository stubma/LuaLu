namespace LuaLu {
	using UnityEditor;
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;

	public class ExportLuaAssetBundles {
		private const string ASSET_BUNDLE_OUTPUT_FOLDER = "Assets/AssetBundles";

		[MenuItem("Lua/Build AssetBundles")]
		static void BuildAllLuaAssetBundles() {
			// collections
			List<string> assetBundleNames = new List<string>();
			Dictionary<string, List<string>> bundleAssetMap = new Dictionary<string, List<string>>();

			// iterate all assets, find all lua files
			string[] guids = AssetDatabase.FindAssets("");
			foreach(string guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if(path.EndsWith(".lua")) {
					string assetBundleName = AssetImporter.GetAtPath(path).assetBundleName;
					if(!assetBundleNames.Contains(assetBundleName)) {
						assetBundleNames.Add(assetBundleName);
					}
					List<string> assets = null;
					if(bundleAssetMap.ContainsKey(assetBundleName)) {
						assets = bundleAssetMap[assetBundleName];
					} else {
						assets = new List<string>();
						bundleAssetMap[assetBundleName] = assets;
					}
					assets.Add(path);
				}
			}

			// change lua text asset peer asset bundle name
			foreach(string bn in assetBundleNames) {
				// get assets
				List<string> assets = bundleAssetMap[bn];
				List<string> assetPeers = new List<string>();

				// iterate assets in this bundle, assign same bundle name to peer
				foreach(string asset in assets) {
					// get copy path
					bool coreLua = asset.StartsWith(LuaPostprocessor.CORE_LUA_PREFIX);
					string originalFolder = Path.GetDirectoryName(asset);
					string folder = LuaPostprocessor.GENERATED_LUA_PREFIX + originalFolder.Substring(coreLua ? LuaPostprocessor.CORE_LUA_PREFIX.Length : LuaPostprocessor.USER_LUA_PREFIX.Length);
					string filename = Path.GetFileName(asset) + ".bytes";
					string peerPath = Path.Combine(folder, filename);

					// assign same bundle name to it
					AssetImporter.GetAtPath(peerPath).assetBundleName = bn;

					// save peer path
					assetPeers.Add(peerPath);
				}

				// replace assets with peers
				bundleAssetMap[bn] = assetPeers;
			}

			// remove empty bundle name
			assetBundleNames.Remove("");
			bundleAssetMap.Remove("");

			// ensure generated folder exists
			if(!Directory.Exists(LuaPostprocessor.GENERATED_LUA_PREFIX)) {
				Directory.CreateDirectory(LuaPostprocessor.GENERATED_LUA_PREFIX);
			}

			// write a csv to save bundle names
			string abnCSVFile = LuaPostprocessor.GENERATED_LUA_PREFIX + "lua_asset_bundles.csv";
			string csvData = string.Join(",", assetBundleNames.ToArray());
			File.WriteAllText(abnCSVFile, csvData);

			// ensure folder exists
			if(!Directory.Exists(ASSET_BUNDLE_OUTPUT_FOLDER)) {
				Directory.CreateDirectory(ASSET_BUNDLE_OUTPUT_FOLDER);
			}

			// build bundles
			AssetBundleBuild[] buildMap = new AssetBundleBuild[assetBundleNames.Count];
			for(int i = 0; i < assetBundleNames.Count; i++) {
				string bn = assetBundleNames[i];
				List<string> assets = bundleAssetMap[bn];
				buildMap[i].assetBundleName = bn;
				buildMap[i].assetNames = assets.ToArray();
			}
			BuildPipeline.BuildAssetBundles(ASSET_BUNDLE_OUTPUT_FOLDER, buildMap, BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXUniversal);

			// refresh
			AssetDatabase.Refresh();
		}
	}
}