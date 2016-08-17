namespace LuaLu {
	using UnityEngine;
	using System.Collections;
	using UnityEditor;
	using System.IO;

	public class LuaPostprocessor : AssetPostprocessor {
		public const string CORE_LUA_PREFIX = "Assets/LuaLu/Resources/";
		public const string USER_LUA_PREFIX = "Assets/Resources/";
		public const string GENERATED_LUA_PREFIX = "Assets/Generated/Resources/";

		private static bool GenerateLuaTextAsset(string asset) {
			if(asset.EndsWith(".lua")) {
				bool coreLua = asset.StartsWith(CORE_LUA_PREFIX);
				bool userLua = asset.StartsWith(USER_LUA_PREFIX);
				if(coreLua || userLua) {
					// get copy path
					string originalFolder = Path.GetDirectoryName(asset);
					string folder = GENERATED_LUA_PREFIX + originalFolder.Substring(coreLua ? CORE_LUA_PREFIX.Length : USER_LUA_PREFIX.Length);
					string filename = Path.GetFileName(asset) + ".bytes";
					string finalPath = Path.Combine(folder, filename);

					// ensure folder is here
					if(!Directory.Exists(folder)) {
						Directory.CreateDirectory(folder);
					}

					// read original file
					StreamReader reader = new StreamReader(asset);
					string fileData = reader.ReadToEnd();
					reader.Close();

					// write to copy
					FileStream fs = new FileStream(finalPath, FileMode.OpenOrCreate, FileAccess.Write);
					StreamWriter writer = new StreamWriter(fs);
					writer.Write(fileData);
					writer.Close();
					fs.Close();

					// return
					return true;
				}
			}

			return false;
		}

		private static bool DeleteLuaTextAsset(string asset) {
			if(asset.EndsWith(".lua")) {
				bool coreLua = asset.StartsWith(CORE_LUA_PREFIX);
				bool userLua = asset.StartsWith(USER_LUA_PREFIX);
				if(coreLua || userLua) {
					// get copy path
					string originalFolder = Path.GetDirectoryName(asset);
					string folder = GENERATED_LUA_PREFIX + originalFolder.Substring(coreLua ? CORE_LUA_PREFIX.Length : USER_LUA_PREFIX.Length);
					string filename = Path.GetFileName(asset) + ".bytes";
					string finalPath = Path.Combine(folder, filename);

					// delete it
					AssetDatabase.DeleteAsset(finalPath);

					return true;
				}
			}

			return false;
		}

		private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromPath) {
			bool changed = false;
			foreach(string asset in importedAssets) {
				changed = GenerateLuaTextAsset(asset);
			}
			foreach(string asset in deletedAssets) {
				changed = DeleteLuaTextAsset(asset);
			}
			for(int i = 0; i < movedAssets.Length; i++) {
				changed = DeleteLuaTextAsset(movedFromPath[i]);
				changed = GenerateLuaTextAsset(movedAssets[i]);
			}

			// refresh
			if(changed) {
				AssetDatabase.Refresh(ImportAssetOptions.Default);
			}
		}
	}
}