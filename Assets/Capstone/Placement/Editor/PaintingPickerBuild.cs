using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Capstone.Placement
{
    public static class PaintingPickerBuild
    {
        [PostProcessBuild(50)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS)
                return;

            string plistPath = Path.Combine(path, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromFile(plistPath);
            plist.root.SetString(
                "NSPhotoLibraryUsageDescription",
                "A clear picture of the painting is placed on the wall at the size you enter.");
            plist.WriteToFile(plistPath);

            string projectPath = PBXProject.GetPBXProjectPath(path);
            PBXProject project = new PBXProject();
            project.ReadFromFile(projectPath);
            project.AddFrameworkToProject(project.GetUnityFrameworkTargetGuid(), "PhotosUI.framework", false);
            project.WriteToFile(projectPath);
        }
    }
}
