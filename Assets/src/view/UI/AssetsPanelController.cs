using System;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AssetsPanelController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    public UIDocument rootUIDocument;
    [SerializeField] public VisualTreeAsset assetButtonTemplate;
    private VisualElement assetsPanel;

    void Start()
    {
        eventDispatcher.eventListener += this.EventListener;
        this.assetsPanel = rootUIDocument.rootVisualElement.Q<VisualElement>("AssetsPanel");
        this.assetsPanel.RegisterCallback<MouseEnterEvent>(e =>
            { eventDispatcher.Raise(assetsPanel, new UIEvent() { name = "assets panel", message = "enter", type = UIEventType.EnterLeaveUIPanel }); });
        this.assetsPanel.RegisterCallback<MouseLeaveEvent>(e =>
            { eventDispatcher.Raise(assetsPanel, new UIEvent() { name = "assets panel", message = "leave", type = UIEventType.EnterLeaveUIPanel }); });
    }

    public void EventListener(object sender, UIEvent e)
    {
        if (e.type == UIEventType.Asset && e.name == "list")
            UpdateAssetsList(e.message);
    }

    private void UpdateAssetsList(string json)
    {
        assetsPanel.Clear();

        var jsonData = JArray.Parse(json);
        int i = 0;
        foreach (var assetJson in jsonData.Children())
        {
            TemplateContainer assetButtonContainer = assetButtonTemplate.Instantiate();
            Button button = assetButtonContainer.Q<Button>("AssetButton");
            button.text = "";
            button.tooltip = "name: " + assetJson["name"].Value<string>();
            button.tooltip += "\ndatetime: " + assetJson["dateTime"].Value<string>();
            button.tooltip += "\nverticesCount: " + assetJson["verticesCount"].Value<string>();
            button.tooltip += "\nboundariesCount: " + assetJson["boundariesCount"].Value<string>();
            button.tooltip += "\nspacesCount: " + assetJson["spacesCount"].Value<string>();
            button.AddManipulator(new ToolTipManipulator(assetsPanel));

            string base64 = assetJson["thumbnailBase64"].Value<string>();
            byte[] image = Convert.FromBase64String(base64);
            Texture2D tex = new Texture2D(128, 128);
            tex.LoadImage(image);
            button.style.backgroundImage = tex;

            int localIndex = i;
            button.clicked += () =>
            {
                eventDispatcher.Raise(this, new UIEvent() { name = "apply asset", message = localIndex.ToString(), type = UIEventType.ToolButton });
            };
            assetsPanel.Add(assetButtonContainer);

            i++;
        }

    }

}
