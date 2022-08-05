using System;
using System.IO;
using System.Text;
using System.Collections;
using System.IO.Compression;
using System.Runtime.InteropServices;

using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Networking;

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
        // TODO: move everthing here to separate object
        root = GetComponent<UIDocument>().rootVisualElement;

        eventDispatcher.eventListener += this.EventListener;

        // toolBar
        VisualElement toolBar = root.Q<VisualElement>("ToolBar");
        var toolBarController = GetComponent<ToolBarController>();
        toolBarController.LoadButtons(toolBar,
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "trigger", type = UIEventType.ToolButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "enter", type = UIEventType.ToolButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = "tool bar", message = "cancel", type = UIEventType.ToolButton }); });
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

        // view panel
        var viewPanelController = GetComponent<ViewPanelController>();
        var viewPanel = root.Q<VisualElement>("ViewPanel");
        viewPanelController.Init(viewPanel,
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "enable", type = UIEventType.ViewButton }); },
            (tbd) => { eventDispatcher.Raise(this, new UIEvent() { name = tbd.m_ToolName, message = "disable", type = UIEventType.ViewButton }); });
        viewPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "view panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        viewPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(toolBar, new UIEvent() { name = "view panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }

    // TODO: we should use a standalone module to handle all file import and export
    private void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.ToolButton && e.name == "load")
            LoadFromFile();
        else if (e.type == UIEventType.Resources && e.name == "save")
            SaveToFile(e.message);
        else if (e.type == UIEventType.ToolButton && e.name == "gridmap")
            LoadGridMap();
        else if (e.type == UIEventType.Resources && e.name == "gridmap")
            DestroyGridMapImportPanel();
    }

    private void DestroyGridMapImportPanel()
    {
        root.Focus();  // prevent warning if we focus on visualElement on gridMapImportPanelObj
        Destroy(gridMapImportPanelObj);
        gridMapImportPanelObj = null;
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
#if UNITY_WEBGL && !UNITY_EDITOR
        var bytes = Encoding.UTF8.GetBytes(content);
        DownloadFile(gameObject.name, "OnFileDownload", "unnamed_map.indoor.json", bytes, bytes.Length);
#else
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "Assets/src/Tests/", "unnamed_map.indoor.json", "indoor.json");
        Debug.Log("save file to: " + path);
        File.WriteAllText(path, content);
#endif
    }

    private void LoadFromFile()
    {

#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnMapUpload", ".json", true);
#else
        string[] path = StandaloneFileBrowser.OpenFilePanel("Load File", "Assets/src/Tests/", "json", false);
        if (path.Length > 0 && path[0].Length > 0)
        {
            if (File.Exists(path[0]))
            {
                string fileContent = File.ReadAllText(path[0]);
                eventDispatcher.Raise(this, new UIEvent() { name = "load", message = fileContent, type = UIEventType.Resources });
            }
            else
            {
                Debug.LogWarning(path[0] + " don't exist");
            }
        }
        else
        {
            Debug.Log("no map file selected");
        }
#endif
    }
    public void OnMapUpload(string url_filePath)
    {
        string[] array = url_filePath.Split(",");
        string url = array[0];
        string filePath = array[1];
        StartCoroutine(OutputRoutine(url, filePath, (request) =>
           eventDispatcher.Raise(this, new UIEvent() { name = "load", message = request.text, type = UIEventType.Resources }
        )));
    }
    private void LoadGridMap()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        UploadFile(gameObject.name, "OnGridMapUpload", ".png, .pgm", true);
#else
        string[] path = StandaloneFileBrowser.OpenFilePanel("Load File", "Assets/src/Tests/", new[] { new ExtensionFilter("", "pgm", "png") }, false);
        if (path.Length > 0 && path[0].Length > 0)
        {
            if (File.Exists(path[0]))
                PopGridMapImportPanel(path[0], File.ReadAllBytes(path[0]));
            else
                Debug.LogWarning(path[0] + " don't exist");
        }
        else
        {
            Debug.Log("no grid map selected");
        }
#endif
    }
    public void OnGridMapUpload(string url_filePath)
    {
        string[] array = url_filePath.Split(",");
        string url = array[0];
        string filePath = array[1];
        StartCoroutine(OutputRoutine(url, filePath, (request) =>
            {
                PopGridMapImportPanel(filePath, request.bytes);
            }));
    }

    private void PopGridMapImportPanel(string filePath, byte[] imageBytes)
    {
        byte[] zipBytes = Compress(imageBytes);
        string zippedBase64Image = Convert.ToBase64String(zipBytes);

        GridMapImageFormat format = GridMapImageFormat.PGM;
        if (filePath.EndsWith("pgm"))
            format = GridMapImageFormat.PGM;
        else if (filePath.EndsWith("png"))
            format = GridMapImageFormat.PNG;
        else
            Debug.LogError("unrecognize file format: " + filePath);

        Texture2D tex = new Texture2D(1, 1);
        if (format == GridMapImageFormat.PNG)
            tex.LoadImage(imageBytes);
        else if (format == GridMapImageFormat.PGM)
            tex.LoadPGMImage(imageBytes);
        else
        {
            Debug.LogError("Unrecognize file format: " + format);
            return;
        }

        gridMapImportPanelObj = Instantiate(Resources.Load<GameObject>("UIObj/GridMapImporter"), Vector3.zero, Quaternion.identity);
        gridMapImportPanelObj.GetComponent<GridMapImporter>()
            .Init(filePath, tex.width, tex.height, format, zippedBase64Image, this.PublishGridMapLoadInfo, this.DestroyGridMapImportPanel);
    }

    public void OnFileDownload()
    {
        Debug.Log("File Successfully Downloaded");
    }
    private IEnumerator OutputRoutine(string url, string filePath, Action<WWW> postAction)
    {
        // var request = new UnityWebRequest(url);
        // request.uploadHandler = new UploadHandlerFile();
        // yield return request.SendWebRequest();
        // postAction?.Invoke(request);

        var loader = new WWW(url);
        yield return loader;
        postAction?.Invoke(loader);
    }

    void Update()
    {

    }

    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
}
