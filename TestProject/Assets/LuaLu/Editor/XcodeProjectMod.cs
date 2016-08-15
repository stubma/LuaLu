using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.Collections;
using System.IO;

public class XcodeProjectMod : MonoBehaviour {
	[PostProcessBuild]
	public static void OnPostprocessBuild(BuildTarget buildTarget, string path) {
		if(buildTarget == BuildTarget.StandaloneOSXIntel64) {
			string projPath = PBXProject.GetPBXProjectPath(path);
			PBXProject proj = new PBXProject();
			proj.ReadFromString(File.ReadAllText(projPath));
			string target = proj.TargetGuidByName(PBXProject.GetUnityTargetName());
			proj.AddBuildProperty(target, "OTHER_LDFLAGS", "-pagezero_size 10000 -image_base 100000000");
			File.WriteAllText(projPath, proj.WriteToString());
		}
	}
}
