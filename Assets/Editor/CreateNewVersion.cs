using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using LibGit2Sharp;




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
        root.Q<TextField>("new_version").RegisterValueChangedCallback((evt) =>
        {
            if (root.Q<TextField>("commit_message").text.StartsWith("Change version to"))
                root.Q<TextField>("commit_message").value = "Change version to " + evt.newValue;
        });

        // commit
        root.Q<Button>("commit").clicked += () =>
        {
            using var repo = new Repository(".");
            Commands.Stage(repo, "*");
        };


        root.Q<Button>("cancel_commit").clicked += () => { GetWindow<CreateNewVersion>().Close(); };
        root.Q<Button>("cancel_build").clicked += () => { GetWindow<CreateNewVersion>().Close(); };

    }
}
