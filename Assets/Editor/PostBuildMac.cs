using UnityEditor;
using UnityEditor.Callbacks;
using System.Diagnostics;

public class PostBuildMac
{
    [PostProcessBuild]
    public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
    {
        if (target == BuildTarget.StandaloneOSX)
        {
            string macAppPath = pathToBuiltProject + "/Contents/MacOS/OperatingRoomBuild_Mac"; // Change "YourGame" to match your executable name
            Process.Start("chmod", "+x " + macAppPath);
        }
    }
}
