using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class POIPanelController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    [SerializeField] private VisualTreeAsset m_QueuedPOITemplate;
    private string toolName;

    public void Init(UIEventDispatcher eventDispatcher, string toolName)
    {
        this.eventDispatcher = eventDispatcher;
        this.toolName = toolName;
    }

    void Start()
    {
        VisualElement POIPanel = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("POIPanel");
        POIPanel.style.left = Input.mousePosition.x;
        POIPanel.style.top = Screen.height - Input.mousePosition.y;

        List<POIType> allPOITypes = new List<POIType>(Resources.LoadAll<POIType>("POI/POITypes"));

        allPOITypes.ForEach(poit =>
        {
            Button button = new Button();
            button.text = (poit.needQueue ? "(Q)" : "") + poit.name;
            button.style.backgroundColor = poit.color;
            button.clicked += () =>
            {
                string json = poit.ToJson();
                eventDispatcher.Raise(this, new UIEvent() { name = toolName, message = json, type = UIEventType.ToolButton });
                eventDispatcher.Raise(this, new UIEvent() { name = "poi panel", message = "hide", type = UIEventType.PopUp });
            };
            POIPanel.Add(button);

        });
    }

    void Update()
    {

    }
}
