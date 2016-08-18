namespace LuaLu {
	using UnityEditor;
	using UnityEngine;
	using System.Collections;
	using System.Collections.Generic;
	using System.IO;
	using System;

	public class LuaMenu : ScriptableObject {
		private const string ASSET_BUNDLE_OUTPUT_FOLDER = "Assets/AssetBundles";

		[MenuItem("Lua/Re-Generate Unity Lua Binding", false, 1)]  
		static void MenuGenerateBinding() {  
			Transform[] transforms = Selection.GetTransforms(SelectionMode.TopLevel | SelectionMode.OnlyUserModifiable);  

			foreach(Transform transform in transforms) {  
				GameObject newChild = new GameObject("_Child");  
				newChild.transform.parent = transform;  
			}  
		}

		[MenuItem("Lua/Build All Lua AssetBundles - OSX")]
		static void BuildAllLuaAssetBundlesOSX() {
			BuildAllLuaAssetBundles(BuildTarget.StandaloneOSXUniversal);
		}

		[MenuItem("Lua/Build All Lua AssetBundles - iOS")]
		static void BuildAllLuaAssetBundlesIOS() {
			BuildAllLuaAssetBundles(BuildTarget.iOS);
		}

		[MenuItem("Lua/Build All Lua AssetBundles - Android")]
		static void BuildAllLuaAssetBundlesAndroid() {
			BuildAllLuaAssetBundles(BuildTarget.Android);
		}

		static void BuildAllLuaAssetBundles(BuildTarget targetPlatform) {
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
					bool coreLua = asset.StartsWith(LuaPostprocessor.CORE_LUA_PREFIX);
					string originalFolder = Path.GetDirectoryName(asset);
					string folder = LuaPostprocessor.GENERATED_LUA_PREFIX + originalFolder.Substring(coreLua ? LuaPostprocessor.CORE_LUA_PREFIX.Length : LuaPostprocessor.USER_LUA_PREFIX.Length);
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
			if(!Directory.Exists(LuaPostprocessor.GENERATED_LUA_PREFIX)) {
				Directory.CreateDirectory(LuaPostprocessor.GENERATED_LUA_PREFIX);
			}

			// write a csv to save bundle names
			string abnCSVFile = LuaPostprocessor.GENERATED_LUA_PREFIX + "lua_asset_bundles.txt";
			string csvData = string.Join("\n", assetBundleNames.ToArray());
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
				AssetImporter ai = AssetImporter.GetAtPath(assets[0]);
				buildMap[i].assetBundleName = ai.assetBundleName;
				buildMap[i].assetBundleVariant = ai.assetBundleVariant;
				buildMap[i].assetNames = assets.ToArray();
			}
			BuildPipeline.BuildAssetBundles(ASSET_BUNDLE_OUTPUT_FOLDER, buildMap, BuildAssetBundleOptions.None, targetPlatform);

			// refresh
			AssetDatabase.Refresh();
		}
	}
}