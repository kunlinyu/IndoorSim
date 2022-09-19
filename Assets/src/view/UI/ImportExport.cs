using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.Runtime.InteropServices;
using Newtonsoft.Json.Linq;

using UnityEngine;
using UnityEngine.UIElements;
using SFB;

public class ImportExport : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;
    public UIDocument rootUIDocument;

    // pop up panels
    GameObject gridMapImportPanelObj;
    GameObject exportPanelObj;
    VisualElement root;
    List<string> layers = null;

    void Start()
    {
        eventSubscriber = new UIEventSubscriber(eventDispatcher);
    }
    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
    }
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
        else if (e.type == UIEventType.ToolButton && e.name == "export")
            PopExportPanel();
        else if (e.type == UIEventType.Resources && e.name == "export")
        {
            Export(e.message, e.data);
            DestroyExportPanel();
        }
        else if (e.type == UIEventType.Hierarchy && e.name == "indoordata")
        {
            var jsonData = JObject.Parse(e.message);
            layers = jsonData["layers"].Children().Select(layer => layer["level"].Value<string>()).ToList();
        }
    }

    void PopExportPanel()
    {
        if (layers != null && layers.Count > 0)
        {
            exportPanelObj = Instantiate(Resources.Load<GameObject>("UIObj/ExportPanel"), Vector3.zero, Quaternion.identity);
            exportPanelObj.GetComponent<ExportPanelController>().Init(layers, this.PublishExportInfo, this.DestroyExportPanel);
        }
        else
        {
            Debug.LogWarning("no layer can be export");
        }
    }

    void Export(string message, string data)
    {
        var jsonData = JObject.Parse(message);
        string layer = jsonData["layer"].Value<string>();
        string file = jsonData["file"].Value<string>();
        int dotIndex = file.LastIndexOf(".");
        string suffix = file.Substring(dotIndex + 1);
        bool include = jsonData["include"].Value<bool>();

        string helperString = include ? "" : "NOT";

        Debug.Log($"export layer: \"{layer}\" to file \"{file}\" and \"{helperString} INCLUDE\" a copy of whole file");

#if UNITY_WEBGL && !UNITY_EDITOR
        var bytes = Encoding.UTF8.GetBytes(data);
        DownloadFile(gameObject.name, "OnFileDownload", file, bytes, bytes.Length);
#else
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "Assets/src/Tests/", file, suffix);
        if (path.Length != 0)
        {
            Debug.Log("save file to: " + path);
            File.WriteAllText(path, data);
        }
        else
        {
            Debug.Log("save cancelled");
        }
#endif
    }

    void DestroyExportPanel()
    {
        rootUIDocument.rootVisualElement.Focus();  // prevent warning if we focus on visualElement on gridMapImportPanelObj
        Destroy(exportPanelObj);
        exportPanelObj = null;
    }


    private void DestroyGridMapImportPanel()
    {
        rootUIDocument.rootVisualElement.Focus();  // prevent warning if we focus on visualElement on gridMapImportPanelObj
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

    private void PublishExportInfo(string exportInfo)
    {
        eventDispatcher.Raise(this, new UIEvent() { name = "exportInfo", message = exportInfo, type = UIEventType.Resources });
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
        string path = StandaloneFileBrowser.SaveFilePanel("Save File", "Assets/src/Tests/", "unnamed_map", "indoor.json");
        if (path.Length != 0)
        {
            Debug.Log("save file to: " + path);
            File.WriteAllText(path, content);
        }
        else
        {
            Debug.Log("save cancelled");
        }
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
        Debug.Log("File downloaded... perhabs. If you didn't click \"cancel\"");
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

    [DllImport("__Internal")]
    private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);
}