using LibGit2Sharp;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class VersionSummary
{
    public string version;
    public string message;
    public DateTimeOffset dateTime;
    public string commitId;
}


public class CreateNewVersion : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    public static void GenVersionIndices()
    {
        using var repo = new Repository(".");
        var tags = repo.Tags.OrderByDescending(tag => tag.Annotation.Tagger.When).ToList();
        var versions = tags.Select(tag => new VersionSummary()
        {
            version = tag.FriendlyName,
            message = tag.Annotation.Message.Split("\n")[0],
            dateTime = tag.Annotation.Tagger.When,
            commitId = tag.Target.Id.ToString(),
        }).ToList();
        File.WriteAllText(BuildPlayer.releaseDirectoryPath +  "/version_indices.json", JsonConvert.SerializeObject(versions, Formatting.Indented));
    }


    [MenuItem("Build/Create New Version")]
    public static void ShowExample()
    {
        GetWindow<CreateNewVersion>().titleContent = new GUIContent("Create New Version");
    }

    public void CreateGUI()
    {

        VisualElement root = rootVisualElement;

        VisualElement labelFromUXML = m_VisualTreeAsset.Instantiate();
        root.Add(labelFromUXML);

        // current schema hash
        root.Q<TextField>("lastSchemaHash").value = VersionSchemaHistoryLoader.ReLoad()[Application.version];

        // current version
        root.Q<TextField>("current_version").value = Application.version;

        // new schema hash
        root.Q<TextField>("newSchemaHash").value = IndoorSimData.JSchemaHash();
        if (IndoorSimData.JSchemaHash() != VersionSchemaHistoryLoader.Load()[Application.version])
            root.Q<TextField>("newSchemaHash").label += "(changed)";

        // new version
        root.Q<TextField>("new_version").value = Application.version;
        root.Q<TextField>("new_version").RegisterValueChangedCallback((evt) =>
        {
            if (root.Q<TextField>("commit_message").text.StartsWith("Change version to "))
                root.Q<TextField>("commit_message").value = "Change version to " + evt.newValue;
        });

        // commit
        root.Q<Button>("commit").clicked += () =>
        {
            // Load ProjectSettings file
            string projectSettingsFilePath = ".\\ProjectSettings\\ProjectSettings.asset";
            var lines = File.ReadAllLines(projectSettingsFilePath);

            // Write new version number to ProjectSettings file
            var newVersion = root.Q<TextField>("new_version").value;
            var oldLine = "bundleVersion: " + Application.version;
            var newLine = "bundleVersion: " + newVersion;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].IndexOf(oldLine) > -1)
                {
                    lines[i] = lines[i].Replace(oldLine, newLine);
                    break;
                }
            }
            File.WriteAllLines(projectSettingsFilePath, lines);
            Debug.Log($"Write \"{newLine}\" to {projectSettingsFilePath}");

            // update new version and schema hash to schema hash history file
            UpdateSchemaHashHistory(newVersion);

            // Stage
            using var repo = new Repository(".");
            Commands.Stage(repo, "*");

            // Commit to the repository
            Signature author = new("Kunlin Yu", "yukunlin@syriusrobotics.com", DateTime.Now);
            Signature committer = author;
            Commit commit = repo.Commit(root.Q<TextField>("commit_message").text, author, committer);
            Debug.Log($"committed({commit.Sha[..7]}): " + root.Q<TextField>("commit_message").text);
        };

        // build
        root.Q<Button>("build").clicked += () =>
        {
            GetWindow<CreateNewVersion>().Close();

            if (root.Q<Toggle>("webgl").value)
            {
                BuildPlayer.Build(BuildTarget.WebGL, BuildPlayer.Snapshot(), true);
                BuildPlayer.Build(BuildTarget.WebGL, BuildPlayer.Snapshot(), false);
            }
            if (root.Q<Toggle>("windows").value)
            {
                BuildPlayer.Build(BuildTarget.StandaloneWindows64, BuildPlayer.Snapshot(), true);
                BuildPlayer.Build(BuildTarget.StandaloneWindows64, BuildPlayer.Snapshot(), false);
            }
            if (root.Q<Toggle>("linux").value)
            {
                BuildPlayer.Build(BuildTarget.StandaloneLinux64, BuildPlayer.Snapshot(), true);
                BuildPlayer.Build(BuildTarget.StandaloneLinux64, BuildPlayer.Snapshot(), false);
            }
        };

        using var repo = new Repository(".");

        var tags = repo.Tags.OrderByDescending(tag => tag.Annotation.Tagger.When).ToList();
        Tag latestTag = tags.First();
        Debug.Log("Latest: " + latestTag.FriendlyName);

        // log
        var filter = new CommitFilter()
        {
            IncludeReachableFrom = repo.Head,
            ExcludeReachableFrom = ((Commit)latestTag.Target).Parents.First().Sha,
        };
        var commits = repo.Commits.QueryBy(filter).ToList();

        var format = "ddd dd MMM";
        TextField textField = root.Q<TextField>("log");
        StringBuilder sb = new();
        foreach (Commit c in commits)
        {
            sb.Append(c.Id.Sha[..7] + "\t");
            sb.Append($"({c.Author.When.ToString(format, CultureInfo.InvariantCulture)})\t");
            sb.Append(c.Message.Split("\n")[0]);
            if (c.Sha == latestTag.Target.Sha)
                sb.Append($"\t<-- {latestTag.FriendlyName} {latestTag.Annotation.Message}");
            sb.Append("\n");
        }

        textField.value = sb.ToString();

        // tag name
        root.Q<TextField>("tagName").value = latestTag.FriendlyName;
        Commit latestCommit = repo.Commits.Take(1).First();
        string firstLine = latestCommit.Message.Split("\n")[0];
        if (firstLine.StartsWith("Change version to "))
            root.Q<TextField>("tagName").value = "V" + firstLine["Change version to ".Length..];

        // tag target sha1
        root.Q<TextField>("targetSHA1").value = repo.Head.Tip.ToString();

        // make tag
        root.Q<Button>("makeTag").clicked += () =>
        {
            using var repo = new Repository(".");
            Signature tagger = new("Kunlin Yu", "yukunlin@syriusrobotics.com", DateTime.Now);
            repo.ApplyTag(root.Q<TextField>("tagName").text, tagger, root.Q<TextField>("tagMessage").text);
        };

        string versionPath = BuildPlayer.VersionPath();

        // Load ChangeLog
        root.Q<TextField>("changeLog").value = LoadOrEmpty(versionPath + "/ChangeLog");

        // Load KnowIssues
        root.Q<TextField>("knowIssues").value = LoadOrEmpty(versionPath + "/KnowIssues");

        // Load Artifacts
        root.Q<TextField>("artifacts").value = LoadOrEmpty(versionPath + "/Artifacts");

        // Load DateTime
        root.Q<TextField>("dateTime").value = LoadOrEmpty(versionPath + "/DateTime");

        // Generate
        root.Q<Button>("generate").clicked += () =>
        {
            string versionPath = BuildPlayer.VersionPath();
            Debug.Log(versionPath);

            // ChangeLog
            File.WriteAllText(versionPath + "/ChangeLog", root.Q<TextField>("changeLog").text);

            // KnowIssues
            File.WriteAllText(versionPath + "/KnowIssues", root.Q<TextField>("knowIssues").text);

            // DateTime
            string dateTime = DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ssK");
            File.WriteAllText(versionPath + "/DateTime", dateTime);
            root.Q<TextField>("dateTime").value = dateTime;

            // Artifacts
            List<string> files = new(Directory.GetFileSystemEntries(versionPath));
            List<string> artifacts = files.Select(file => Path.GetFileName(file)).Where(file => file.StartsWith("IndoorSim-")).ToList();
            artifacts.Sort(string.CompareOrdinal);
            artifacts.Sort((str1, str2) => Platform(str1).Length - Platform(str2).Length);
            File.WriteAllText(versionPath + "/Artifacts", String.Join("\n", artifacts));
            root.Q<TextField>("artifacts").value = String.Join("\n", artifacts);

            // Version Indices
            GenVersionIndices();

            // Schema Hash
            BuildPlayer.GenerateSchemaHash();
        };

        root.Q<Button>("cancel_commit").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_build").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_tag").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_gen").clicked += () => { GetWindow<CreateNewVersion>().Close(); };

    }

    static private string LoadOrEmpty(string filePath)
    {
        if (File.Exists(filePath))
            return File.ReadAllText(filePath);
        else
            return "";
    }

    static private string Platform(string version)
    {
        int begin = version.IndexOf('-');
        int length = version.Substring(begin + 1).IndexOf('-');
        return version.Substring(begin + 1, length);
    }

    static readonly string schemaHashHistoryFile = "Assets\\Resources\\schemaHashHistory.txt";
    public static void UpdateSchemaHashHistory(string newVersion)
    {
        List<string> lines = new(File.ReadAllLines(schemaHashHistoryFile));
        if (lines.Count < 3) throw new Exception("file too shor");
        if (lines.Any(line => line.IndexOf(newVersion) != -1)) throw new Exception("the file contains the current version: " + newVersion);
        string newLine = newVersion + " " + IndoorSimData.JSchemaHash();
        lines.Add(newLine);
        Debug.Log("Add a new line: " + newLine);
        File.WriteAllLines(schemaHashHistoryFile, lines);
    }
}
