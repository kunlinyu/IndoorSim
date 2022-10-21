using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class SplitPanelController : MonoBehaviour
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
        VisualElement SplitPanel = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("SplitPanel");
        SplitPanel.style.left = Input.mousePosition.x;
        SplitPanel.style.top = Screen.height - Input.mousePosition.y;

        var buttons = SplitPanel.Query<Button>().Build();
        string delimiter = " x ";
        foreach (var button in buttons)
        {
            button.clicked += () =>
            {
                string text = button.text;
                int index = text.IndexOf(delimiter);
                int rows, coloums;
                if (text == "n x n")
                {
                    rows = 0;
                    coloums = 0;
                }
                else
                {
                    rows = int.Parse(text.Substring(0, index));
                    coloums = int.Parse(text.Substring(index + delimiter.Length));
                }
                string json = $"{{\"rows\":\"{rows}\",\"coloums\":\"{coloums}\"}}";

                eventDispatcher.Raise(this, new UIEvent() { name = "split panel", message = "hide", type = UIEventType.PopUp });
                eventDispatcher.Raise(this, new UIEvent() { name = "split", message = json, type = UIEventType.ToolButton });
            };
        }
    }

    void Update()
    {

    }
}
