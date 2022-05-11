using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

public class AssetsPanelController : MonoBehaviour
{
    [SerializeField] public VisualTreeAsset assetButtonTemplate;
    private VisualElement assetsPanel;
    private Action<int> OnApply;
    private Action<int> OnRemove;


    public void Init(VisualElement assetsPanel, Action<int> OnApply, Action<int> OnRemove)
    {
        this.OnApply = OnApply;
        this.OnRemove = OnRemove;
        this.assetsPanel = assetsPanel;
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
            button.clicked += () => { OnApply(localIndex); };
            assetsPanel.Add(assetButtonContainer);

            i ++;
        }

    }

}
