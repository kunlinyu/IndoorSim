#if UNITY_EDITOR

using System.IO;

using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

// Output the build size or a failure depending on BuildPlayer.

public class BuildPlayer : MonoBehaviour
{
    [MenuItem("Build/Build Linux")]
    public static void BuildLinux()
    {
        Build("../release", BuildTarget.StandaloneLinux64, true);
        Build("../release", BuildTarget.StandaloneLinux64, false);
    }

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        // Build("../release", BuildTarget.WebGL, true);
        Build("../release", BuildTarget.WebGL, false);
    }


    public static void Build(string prefix, BuildTarget target, bool development)
    {
        string[] lines = File.ReadAllLines(".git/refs/heads/master");
        if (lines.Length < 1)
            throw new System.Exception("can not read line from file");

        string shortSHA1 = lines[0].Substring(0, 7);

        string dirName = "IndoorSim-" + target.ToString() + (development ? "-dev" : "") + "-V" + Application.version + "." + shortSHA1;
        Debug.Log(dirName);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MappingScene.unity" };
        buildPlayerOptions.locationPathName = prefix + "/" + dirName + (target == BuildTarget.StandaloneLinux64 ? "/" + dirName : "");
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = development ? BuildOptions.Development : BuildOptions.None;

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes into path " + summary.outputPath);
        else
            Debug.Log("Build " + summary.result.ToString());


        if (target == BuildTarget.StandaloneLinux64) {
            CreateTarGZ(prefix + "/" + dirName + ".tar.gz", prefix + "/" + dirName);
            Directory.Delete(prefix + "/" + dirName, true);
        }
    }
    private static void CreateTarGZ(string tgzFilename, string sourceDirectory)
    {
        Stream outStream = File.Create(tgzFilename);
        Stream gzoStream = new GZipOutputStream(outStream);
        TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);

        // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
        // and must not end with a slash, otherwise cuts off first char of filename
        // This is scheduled for fix in next release
        tarArchive.RootPath = sourceDirectory.Replace('\\', '/');
        if (tarArchive.RootPath.EndsWith("/"))
            tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

        AddDirectoryFilesToTar(tarArchive, sourceDirectory, true);

        tarArchive.Close();
    }
    private static void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
    {
        // Optionally, write an entry for the directory itself.
        // Specify false for recursion here if we will add the directory's files individually.
        TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
        tarArchive.WriteEntry(tarEntry, false);

        // Write each file to the tar.
        string[] filenames = Directory.GetFiles(sourceDirectory);
        foreach (string filename in filenames)
        {
            tarEntry = TarEntry.CreateEntryFromFile(filename);
            tarArchive.WriteEntry(tarEntry, true);
        }

        if (recurse)
        {
            string[] directories = Directory.GetDirectories(sourceDirectory);
            foreach (string directory in directories)
                AddDirectoryFilesToTar(tarArchive, directory, recurse);
        }
    }
}

#endif