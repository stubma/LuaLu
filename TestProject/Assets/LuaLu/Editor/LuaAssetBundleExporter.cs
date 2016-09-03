namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using UnityEditor;
	using System.IO;

	[NoLuaBinding]
	public class LuaAssetBundleExporter {
		public static void BuildAllLuaAssetBundles(BuildTarget targetPlatform) {
			// collections
			List<string> assetBundleNames = new List<string>();
			Dictionary<string, List<string>> bundleAssetMap = new Dictionary<string, List<string>>();

			// iterate all assets, find all lua files
			string[] guids = AssetDatabase.FindAssets("");
			foreach(string guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if(path.EndsWith(".lua")) {
					AssetImporter ai = AssetImporter.GetAtPath(path);
					string bn = ai.assetBundleName + (ai.assetBundleVariant == "" ? "" : ".") + ai.assetBundleVariant;
					if(!assetBundleNames.Contains(bn)) {
						assetBundleNames.Add(bn);
					}
					List<string> assets = null;
					if(bundleAssetMap.ContainsKey(bn)) {
						assets = bundleAssetMap[bn];
					} else {
						assets = new List<string>();
						bundleAssetMap[bn] = assets;
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
					bool coreLua = asset.StartsWith(LuaConst.CORE_LUA_PREFIX);
					string originalFolder = Path.GetDirectoryName(asset) + "/";
					string folder = LuaConst.GENERATED_LUA_PREFIX + originalFolder.Substring(coreLua ? LuaConst.CORE_LUA_PREFIX.Length : LuaConst.USER_LUA_PREFIX.Length);
					string filename = Path.GetFileName(asset) + ".bytes";
					string peerPath = Path.Combine(folder, filename);

					// assign same bundle name to it
					AssetImporter oriAI = AssetImporter.GetAtPath(asset);
					AssetImporter ai = AssetImporter.GetAtPath(peerPath);
					ai.assetBundleName = oriAI.assetBundleName;
					if(oriAI.assetBundleName != "") {
						ai.assetBundleVariant = oriAI.assetBundleVariant;
					}

					// save peer path
					assetPeers.Add(peerPath);
				}

				// replace assets with peers
				bundleAssetMap[bn] = assetPeers;
			}

			// remove empty bundle name
			assetBundleNames.Remove(".");
			bundleAssetMap.Remove(".");

			// ensure generated folder exists
			if(!Directory.Exists(LuaConst.GENERATED_LUA_PREFIX)) {
				Directory.CreateDirectory(LuaConst.GENERATED_LUA_PREFIX);
			}

			// write a csv to save bundle names
			string abnCSVFile = LuaConst.GENERATED_LUA_PREFIX + LuaConst.LUA_ASSET_BUNDLE_LIST_FILE + ".txt";
			string csvData = string.Join("\n", assetBundleNames.ToArray());
			File.WriteAllText(abnCSVFile, csvData);

			// ensure folder exists
			if(!Directory.Exists(LuaConst.ASSET_BUNDLE_OUTPUT_FOLDER)) {
				Directory.CreateDirectory(LuaConst.ASSET_BUNDLE_OUTPUT_FOLDER);
			}

			// build bundles
			AssetBundleBuild[] buildMap = new AssetBundleBuild[assetBundleNames.Count];
			for(int i = 0; i < assetBundleNames.Count; i++) {
				string bn = assetBundleNames[i];
				List<string> assets = bundleAssetMap[bn];
				AssetImporter ai = AssetImporter.GetAtPath(assets[0]);
				buildMap[i].assetBundleName = ai.assetBundleName;
				buildMap[i].assetBundleVariant = ai.assetBundleVariant;
				buildMap[i].assetNames = assets.ToArray();
			}
			BuildPipeline.BuildAssetBundles(LuaConst.ASSET_BUNDLE_OUTPUT_FOLDER, buildMap, BuildAssetBundleOptions.None, targetPlatform);

			// refresh
			AssetDatabase.Refresh();
		}
	}
}