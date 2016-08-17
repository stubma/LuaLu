namespace LuaLu {
	using UnityEngine;
	using UnityEditor;
	using UnityEditor.Callbacks;
	using UnityEditor.iOS.Xcode;
	using System.Collections;
	using System.IO;

	public class XcodeProjectMod {
		[PostProcessBuild]
		public static void OnPostprocessBuild(BuildTarget buildTarget, string path) {
			if(buildTarget == BuildTarget.iOS) {
				// get project and target
				string projPath = PBXProject.GetPBXProjectPath(path);
				PBXProject proj = new PBXProject();
				proj.ReadFromString(File.ReadAllText(projPath));
				string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());

				// disable bitcode
				proj.SetBuildProperty(target, "ENABLE_BITCODE", "NO");

				// write project
				File.WriteAllText(projPath, proj.WriteToString());
			}
		}
	}
}