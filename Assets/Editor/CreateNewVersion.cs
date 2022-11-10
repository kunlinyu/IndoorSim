using LibGit2Sharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class CreateNewVersion : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;


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
        root.Q<TextField>("lastSchemaHash").value = VersionSchemaHistoryLoader.Load()[Application.version];

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
            if (root.Q<TextField>("commit_message").text.StartsWith("Change version to"))
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
        Tag latestTag = repo.Tags.OrderByDescending(tag => tag.Annotation.Tagger.When).First();
        Debug.Log(latestTag.FriendlyName);

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

        // tag target sha1
        root.Q<TextField>("targetSHA1").value = repo.Head.Tip.ToString();
        Debug.Log(repo.Head.ToString());

        // make tag
        root.Q<Button>("makeTag").clicked += () =>
        {
            using var repo = new Repository(".");
            Signature tagger = new("Kunlin Yu", "yukunlin@syriusrobotics.com", DateTime.Now);
            repo.ApplyTag(root.Q<TextField>("tagName").text, tagger, root.Q<TextField>("tagMessage").text);
        };

        root.Q<Button>("cancel_commit").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_build").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_tag").clicked += () => { GetWindow<CreateNewVersion>().Close(); };

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
