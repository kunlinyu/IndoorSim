using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class POIPanelController : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    [SerializeField] private VisualTreeAsset m_QueuedPOITemplate;

    public void Init(UIEventDispatcher eventDispatcher)
    {
        this.eventDispatcher = eventDispatcher;
    }

    void Start()
    {
        VisualElement POIPanel = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("POIPanel");
        POIPanel.style.left = Input.mousePosition.x;
        POIPanel.style.top = Screen.height - Input.mousePosition.y;

        List<POIType> allPOITypes = new List<POIType>(Resources.LoadAll<POIType>("POI/POITypes"));

        allPOITypes.ForEach(poit =>
        {
            if (poit.needQueue)
            {
                TemplateContainer buttonContainer = m_QueuedPOITemplate.Instantiate();
                Button button = buttonContainer.Q<Button>("POI");
                button.text = poit.name;

                Button Qbutton = buttonContainer.Q<Button>("QPOI");
                Qbutton.text = "Q";
                POIPanel.Add(buttonContainer);
            }
            else
            {
                Button button = new Button();
                button.text = poit.name;
                POIPanel.Add(button);
            }
        });
    }

    void Update()
    {

    }
}
