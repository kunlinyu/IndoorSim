using LibGit2Sharp;
using System;
using System.IO;
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

        // current version
        root.Q<TextField>("current_version").value = Application.version;

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
            var oldLine = "bundleVersion: " + Application.version;
            var newLine = "bundleVersion: " + root.Q<TextField>("new_version").value;
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

            // Stage
            using var repo = new Repository(".");
            Commands.Stage(repo, "*");

            // Commit to the repository
            Signature author = new("Kunlin Yu", "yukunlin@syriusrobotics.com", DateTime.Now);
            Signature committer = author;
            Commit commit = repo.Commit(root.Q<TextField>("commit_message").text, author, committer);
            Debug.Log($"committed({commit.Sha[..7]}): " + root.Q<TextField>("commit_message").text);
        };
        // commit
        root.Q<Button>("build").clicked += () =>
        {
            GetWindow<CreateNewVersion>().Close();
            if (root.Q<Toggle>("webgl").value)
                BuildPlayer.BuildWebGL();
            if (root.Q<Toggle>("windows").value)
                BuildPlayer.BuildWindows();
            if (root.Q<Toggle>("linux").value)
                BuildPlayer.BuildLinux();
        };

        root.Q<Button>("cancel_commit").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_build").clicked += () => { GetWindow<CreateNewVersion>().Close(); };

    }
}
