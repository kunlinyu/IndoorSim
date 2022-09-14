using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class IndoorMapView : MonoBehaviour
{
    public UIEventDispatcher eventDispatcher;
    private UIEventSubscriber eventSubscriber;

    public IndoorFeatures indoorFeatures;
    Dictionary<ThematicLayer, GameObject> layer2Obj = new Dictionary<ThematicLayer, GameObject>();

    public LayerView activeLayerView = null;

    void Start()
    {
        eventSubscriber = new UIEventSubscriber(eventDispatcher);

        GameObject obj = Instantiate(Resources.Load<GameObject>("Layer"), transform);
        activeLayerView = obj.GetComponent<LayerView>();
        activeLayerView.layer = indoorFeatures.activeLayer;
        activeLayerView.eventDispatcher = eventDispatcher;
        activeLayerView.RegisterOnMethod();


        obj.name = indoorFeatures.activeLayer.level;
        layer2Obj[indoorFeatures.activeLayer] = obj;

        indoorFeatures.OnLayerCreated += (layer) =>
        {
            GameObject obj = Instantiate(Resources.Load<GameObject>("Layer"), transform);
            activeLayerView = obj.GetComponent<LayerView>();
            activeLayerView.layer = layer;
            activeLayerView.eventDispatcher = eventDispatcher;
            activeLayerView.RegisterOnMethod();
            obj.name = layer.level;
            layer2Obj[layer] = obj;
        };
        indoorFeatures.OnLayerRemoved += (layer) =>
        {
            if (layer2Obj[layer] == activeLayerView)
                activeLayerView = null;

            Destroy(layer2Obj[layer]);
            layer2Obj.Remove(layer);

            if (layer2Obj.Count > 0)
                activeLayerView = layer2Obj.First().Value.GetComponent<LayerView>();
        };
    }

    void Update()
    {
        eventSubscriber.ConsumeAll(EventListener);
    }

    void EventListener(object sender, UIEvent e)
    {
    }



}
