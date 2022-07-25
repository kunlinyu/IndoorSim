using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using System.IO.Compression;

using SFB;

[RequireComponent(typeof(ToolBarController))]
[RequireComponent(typeof(CursorTip))]
[RequireComponent(typeof(LogWindow))]
[RequireComponent(typeof(AssetsPanelController))]
[RequireComponent(typeof(HierarchyPanelController))]
[RequireComponent(typeof(IdPanelController))]
public class All : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    VisualElement root;


    // pop up panels
    GameObject idPanelObj;  // TODO: do not put it in All.uxml, but load it as prefab
    GameObject gridMapImportPanelObj;

    void Start()
    {
        root = GetComponent<UIDocument>().rootVisualElement;

        eventDispatcher.eventListener += this.EventListener;

        // toolBar
        VisualElement toolBar = root.Q<VisualElement>("ToolBar");
        var toolBarController = GetComponent<ToolBarController>();
        toolBarController.LoadButtons(toolBar,
            (button, tbd) => { eventDispatcher.Raise(button, new UIEvent() { name = tbd.m_ToolName, type = UIEventType.ToolButton }); },
            () => { eventDispatcher.Raise(this, new UIEvent() { name = "tool bar", message = "cancel", type = UIEventType.ToolButton }); });
        toolBar.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        toolBar.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "tool bar", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });

        // cursor tip
        CursorTip cursorTip = GetComponent<CursorTip>();
        cursorTip.Init(root.Q<Label>("Tip"));
        eventDispatcher.eventListener += cursorTip.EventListener;

        // version label
        Label versionLabel = root.Q<Label>("VersionLabel");
        versionLabel.text = " IndoorSim version: V" + Application.version;

        // log window
        GetComponent<LogWindow>().Init(root.Q<ListView>("LogList"));

        // assets panel
        var assetsPanelController = GetComponent<AssetsPanelController>();
        var assetsPanel = root.Q<VisualElement>("AssetsPanel");
        assetsPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "assets panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        assetsPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "assets panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
        assetsPanelController.Init(
            assetsPanel,
            (index) => { eventDispatcher.Raise(assetsPanelController, new UIEvent() { name = "apply asset", message = index.ToString(), type = UIEventType.ToolButton }); },
            (index) => { eventDispatcher.Raise(assetsPanelController, new UIEvent() { name = "remove asset", message = index.ToString(), type = UIEventType.ToolButton }); }
        );
        eventDispatcher.eventListener += assetsPanelController.EventListener;

        // hierarchy panel
        var hierarchyPanelController = GetComponent<HierarchyPanelController>();
        var hierarchyPanel = root.Q<ScrollView>("FoldoutContainer");

        hierarchyPanelController.Init(hierarchyPanel);

        hierarchyPanelController.OnAddSimulation += simName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "add simulation", message = simName, type = UIEventType.Hierarchy });

        hierarchyPanelController.OnSelectSimulation += simName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select simulation", message = simName, type = UIEventType.Hierarchy });

        hierarchyPanelController.OnSelectIndoorMap += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select indoor map", type = UIEventType.Hierarchy });

        hierarchyPanelController.OnSelectGridMap += gridName =>
            eventDispatcher.Raise(this, new UIEvent() { name = "select grid map", message = gridName, type = UIEventType.Hierarchy });

        eventDispatcher.eventListener += hierarchyPanelController.EventListener;

        // simulation panel
        root.Q<Button>("play_pause").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "play pause", message = "", type = UIEventType.Simulation });
        root.Q<Button>("fast").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "fast", message = "", type = UIEventType.Simulation });
        root.Q<Button>("slow").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "slow", message = "", type = UIEventType.Simulation });
        root.Q<Button>("stop").clicked += () =>
            eventDispatcher.Raise(this, new UIEvent() { name = "stop", message = "", type = UIEventType.Simulation });

        // id panel
        var idPanelCtr = GetComponent<IdPanelController>();
        var idPanel = root.Q<VisualElement>("IdPanel");
        idPanelCtr.Init(idPanel, (containerId, childrenId) =>
            {
                eventDispatcher.Raise(this, new UIEvent()
                {
                    name = "container id",
                    message = $"{{\"containerId\":\"{containerId}\",\"childrenId\":\"{childrenId}\"}}",
                    type = UIEventType.IndoorSimData
                });
            });
        eventDispatcher.eventListener += idPanelCtr.EventListener;
        idPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "id panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        idPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "id panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });

    }

    private void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ToolButton && e.name == "load")
        {
            string[] path = StandaloneFileBrowser.OpenFilePanel("Load File", "Assets/src/Tests/", "json", false);
            string fileContent = File.ReadAllText(path[0]);
            eventDispatcher.Raise(this, new UIEvent() { name = "load", message = fileContent, type = UIEventType.Resources });
        }
        else if (e.type == UIEventType.Resources && e.name == "save")
        {
            SaveToFile(e.message);
        }
        else if (e.type == UIEventType.ToolButton && e.name == "gridmap")
        {
            string[] path = StandaloneFileBrowser.OpenFilePanel("Load File", "Assets/src/Tests/", new[] { new ExtensionFilter("", "pgm", "png") }, false);
            byte[] imageBytes = File.ReadAllBytes(path[0]);
            byte[] zipBytes = Compress(imageBytes);
            string zippedBase64Image = Convert.ToBase64String(zipBytes);

            PGMImage pgm = new PGMImage();
            pgm.Load(imageBytes);

            GameObject gridMapImporterPrefab = Resources.Load<GameObject>("UIObj/GridMapImporter");
            gridMapImportPanelObj = Instantiate(gridMapImporterPrefab, Vector3.zero, Quaternion.identity);

            GridMapImageFormat fileFormat = GridMapImageFormat.PGM;
            if (path[0].EndsWith("pgm"))
                fileFormat = GridMapImageFormat.PGM;
            else if (path[0].EndsWith("png"))
                fileFormat = GridMapImageFormat.PNG;
            else
                Debug.LogError("unrecognize file format: " + path[0]);
            gridMapImportPanelObj.GetComponent<GridMapImporter>().Init(path[0], pgm.width(), pgm.height(), fileFormat, zippedBase64Image, this.PublishGridMapLoadInfo);
        }
        else if (e.type == UIEventType.Resources && e.name == "gridmap")
        {
            Destroy(gridMapImportPanelObj);
            gridMapImportPanelObj = null;
        }

    }

    public static byte[] Compress(byte[] bytes)
    {
        using (var memoryStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(memoryStream, System.IO.Compression.CompressionLevel.Optimal))
                gzipStream.Write(bytes, 0, bytes.Length);
            return memoryStream.ToArray();
        }
    }

    private void PublishGridMapLoadInfo(string serializedGridMapInfo)
    {
        eventDispatcher.Raise(this, new UIEvent() { name = "gridmap", message = serializedGridMapInfo, type = UIEventType.Resources });
    }

    private void SaveToFile(string content)
    {
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "Assets/src/Tests/", "unnamed_map.indoor.json", "indoor.json");
        Debug.Log("save file to: " + path);
        File.WriteAllText(path, content);
    }

    void Update()
    {
        // Button? focusButton = root.focusController.focusedElement as Button;
        // if (focusButton != null)
        //     Debug.Log(focusButton.tooltip);
    }
}
