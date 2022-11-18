#if UNITY_EDITOR

using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using LibGit2Sharp;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildPlayer : MonoBehaviour
{

    public static readonly string releaseDirectoryPath = "release";

    [MenuItem("Build/schema hash")]
    public static void SchemaHash()
    {
        Debug.Log(IndoorSimData.JSchemaHash());
    }

    [MenuItem("Build/generate schema file")]
    public static void GenerateSchemaHash()
    {
        string dir = VersionPath() + "/schema";
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(dir + "/schema.json", IndoorSimData.JSchemaStableString());
        File.WriteAllText(dir + "/hash.txt", IndoorSimData.JSchemaHash());
    }

    [MenuItem("Build/Build Linux")]
    public static void BuildLinux()
    {
        Build(BuildTarget.StandaloneLinux64, Snapshot(), true);
        Build(BuildTarget.StandaloneLinux64, Snapshot(), false);
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        Build(BuildTarget.StandaloneWindows64, Snapshot(), true);
        Build(BuildTarget.StandaloneWindows64, Snapshot(), false);
    }

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        Build(BuildTarget.WebGL, Snapshot(), false);
    }

    [MenuItem("Build/Build WebGL dev")]
    public static void BuildWebGLDev()
    {
        Build(BuildTarget.WebGL, Snapshot(), true);
    }

    static public bool Snapshot()
    {
        using var repo = new Repository(".");
        Commit lastCommit = repo.Commits.Take(1).First();
        string firstLine = lastCommit.Message.Split("\n")[0];
        return firstLine != "Change version to " + Application.version;
    }

    static public string VersionPath()
    {
        return releaseDirectoryPath + "/V" + Application.version + (Snapshot() ? ".SNAPSHOT" : "");
    }

    private static void CheckReleaseDir()
    {
        if (!Directory.Exists(VersionPath()))
            Directory.CreateDirectory(VersionPath());
    }


    public static void Build(BuildTarget target, bool snapshot, bool development)
    {
        CheckReleaseDir();

        string versionPath = VersionPath();

        string[] lines = File.ReadAllLines(".git/refs/heads/master");
        if (lines.Length < 1)
            throw new System.Exception("can not read line from file");

        string hash = lines[0][..7];
        if (snapshot)
            hash = "SNAPSHOT";

        string dirName = "IndoorSim-" + target.ToString() + (development ? "-dev" : "") + "-V" + Application.version + "." + hash;
        string applicationName = "IndoorSim" + (development ? "-dev" : "") + "-V" + Application.version + ".exe";
        Debug.Log(dirName);

        BuildPlayerOptions buildPlayerOptions = new()
        {
            scenes = new[] { "Assets/Scenes/MappingScene.unity" },
            locationPathName = versionPath + "/" + dirName + (target != BuildTarget.WebGL ? "/" + applicationName : ""),
            target = target,
            options = development ? BuildOptions.Development : BuildOptions.None,
            extraScriptingDefines = new string[] { "HAVE_DATE_TIME_OFFSET" }  // for Newtonsoft.Json.Schema
        };

        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log("Build succeeded: " + summary.totalSize + " bytes into path " + summary.outputPath);
        }
        else
        {
            Debug.Log("Build result: " + summary.result.ToString());
            return;
        }


        if (target == BuildTarget.StandaloneLinux64)
        {
            CreateTarGZ(versionPath + "/" + dirName + ".tar.gz", versionPath + "/" + dirName, versionPath);
            Directory.Delete(versionPath + "/" + dirName, true);
        }
        else if (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64)
        {
            System.IO.Compression.ZipFile.CreateFromDirectory(versionPath + "/" + dirName, versionPath + "/" + dirName + ".zip");
            Directory.Delete(versionPath + "/" + dirName, true);
        }
    }

    private static void CreateTarGZ(string tgzFilename, string sourceDirectory, string rootPath)
    {
        Stream outStream = File.Create(tgzFilename);
        GZipOutputStream gzoStream = new(outStream);
        gzoStream.SetLevel(3);
        TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);
        tarArchive.RootPath = rootPath;

        TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
        tarArchive.WriteEntry(tarEntry, recurse: true);

        tarArchive.Close();
    }

}

#endif