#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

// Output the build size or a failure depending on BuildPlayer.

public class BuildPlayer : MonoBehaviour
{
    [MenuItem("Build/Build Linux")]
    public static void BuildLinux() => Build("../release", BuildTarget.StandaloneLinux64, true);

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL() => Build("../release", BuildTarget.WebGL, true);


    public static void Build(string prefix, BuildTarget target, bool development)
    {
        string[] lines = File.ReadAllLines(".git/refs/heads/master");
        if (lines.Length < 1)
            throw new System.Exception("can not read line from file");

        string shortSHA1 = lines[0].Substring(0, 7);

        string dirName = "IndoorSim-" + target.ToString() + (development ? "-dev" : "") + "-V" + Application.version + "." + shortSHA1;
        Debug.Log(dirName);
        if (target == BuildTarget.StandaloneLinux64)
            dirName = dirName + "/" + dirName;

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MappingScene.unity" };
        buildPlayerOptions.locationPathName = prefix + "/" + dirName;
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = development ? BuildOptions.Development : BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes into path " + summary.outputPath);
        else
            Debug.Log("Build " + summary.result.ToString());
    }


}

#endif