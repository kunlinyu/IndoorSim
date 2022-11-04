#if UNITY_EDITOR

using System.IO;

using UnityEditor;
using UnityEngine;
using UnityEditor.Build.Reporting;

using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

using Markdig;

using UnityEngine.Assertions;

public class BuildPlayer : MonoBehaviour
{

    private static string releaseDirectoryPath = "release";

    [MenuItem("Build/schema hash")]
    public static void SchemaHash()
    {
        Debug.Log(IndoorSimData.JSchemaHash());
    }

    [MenuItem("Build/generate schema file")]
    private static void GenerateSchemaHash()
    {
        string dir = releaseDirectoryPath + "/schema/" + Application.version;
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);
        File.WriteAllText(dir + "/schema.json", IndoorSimData.JSchemaStableString());
        File.WriteAllText(dir + "/hash.txt", IndoorSimData.JSchemaHash());
    }

    [MenuItem("Build/Build Linux")]
    public static void BuildLinux()
    {
        Assert.IsTrue(IndoorSimData.schemaHashHistory.ContainsKey(Application.version));
        GenerateSchemaHash();
        Build(BuildTarget.StandaloneLinux64, true);
        Build(BuildTarget.StandaloneLinux64, false);
    }

    [MenuItem("Build/Build Windows")]
    public static void BuildWindows()
    {
        Assert.IsTrue(IndoorSimData.schemaHashHistory.ContainsKey(Application.version));
        GenerateSchemaHash();
        Build(BuildTarget.StandaloneWindows64, true);
        Build(BuildTarget.StandaloneWindows64, false);
    }

    [MenuItem("Build/Build WebGL")]
    public static void BuildWebGL()
    {
        Assert.IsTrue(IndoorSimData.schemaHashHistory.ContainsKey(Application.version));
        GenerateSchemaHash();
        Build(BuildTarget.WebGL, false);
    }

    [MenuItem("Build/Build WebGL dev")]
    public static void BuildWebGLDev()
    {
        Assert.IsTrue(IndoorSimData.schemaHashHistory.ContainsKey(Application.version));
        GenerateSchemaHash();
        Build(BuildTarget.WebGL, true);
    }

    // [MenuItem("Build/Generate release from markdown")]
    // use ci/generate_markdown.sh instead
    public static void GenerateReleaseFromMarkdown()
    {
        string header = @"<!DOCTYPE html>
<html>
<head>
  <meta http-equiv=""Content-Type"" content=""text/html; charset=utf-8"">
  <title>IndoorSim Release</title>
</head>
<body>
";
        string footer = "</body>\n</html>\n";

        string markdownPath = "RELEASE.md";
        string indexPath = releaseDirectoryPath + "/index.html";

        CheckReleaseDir();

        string markdown = File.ReadAllText(markdownPath);

        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        string html = Markdown.ToHtml(markdown, pipeline);
        string fullHtml = header + html + footer;

        File.WriteAllText(indexPath, fullHtml);
        Debug.Log($"build {indexPath} from {markdownPath}");
    }

    private static void CheckReleaseDir()
    {
        if (!Directory.Exists(releaseDirectoryPath))
            Directory.CreateDirectory(releaseDirectoryPath);
    }


    public static void Build(BuildTarget target, bool development)
    {
        CheckReleaseDir();
        string[] lines = File.ReadAllLines(".git/refs/heads/master");
        if (lines.Length < 1)
            throw new System.Exception("can not read line from file");

        string shortSHA1 = lines[0].Substring(0, 7);

        string dirName = "IndoorSim-" + target.ToString() + (development ? "-dev" : "") + "-V" + Application.version + "." + shortSHA1;
        string applicationExtension = (target == BuildTarget.StandaloneWindows || target == BuildTarget.StandaloneWindows64) ? ".exe" : "";
        string applicationName = "IndoorSim" + (development ? "-dev" : "") + "-V" + Application.version + applicationExtension;
        Debug.Log(dirName);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = new[] { "Assets/Scenes/MappingScene.unity" };
        buildPlayerOptions.locationPathName = releaseDirectoryPath + "/" + dirName + (target != BuildTarget.WebGL ? "/" + applicationName : "");
        buildPlayerOptions.target = target;
        buildPlayerOptions.options = development ? BuildOptions.Development : BuildOptions.None;
        buildPlayerOptions.extraScriptingDefines = new string[] { "HAVE_DATE_TIME_OFFSET" };  // for Newtonsoft.Json.Schema

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


        if (target != BuildTarget.WebGL)
        {
            CreateTarGZ(releaseDirectoryPath + "/" + dirName + ".tar.gz", releaseDirectoryPath + "/" + dirName, releaseDirectoryPath);
            Directory.Delete(releaseDirectoryPath + "/" + dirName, true);
        }
    }

    private static void CreateTarGZ(string tgzFilename, string sourceDirectory, string rootPath)
    {
        Stream outStream = File.Create(tgzFilename);
        GZipOutputStream gzoStream = new GZipOutputStream(outStream);
        gzoStream.SetLevel(3);
        TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream);
        tarArchive.RootPath = rootPath;

        TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
        tarArchive.WriteEntry(tarEntry, recurse: true);

        tarArchive.Close();
    }

}

#endif