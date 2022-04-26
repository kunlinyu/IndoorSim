using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class RLinesController : MonoBehaviour
{
    private RLineGroup rLines;
    public RLineGroup RLines
    {
        get => rLines;
        set {
            rLines = value;
            rLines.OnUpdate += updateRenderer;
        }
    }
    private List<GameObject> rendererObj = new List<GameObject>();

    public Material material;
    public float width = 0.02f;

    // Start is called before the first frame update
    void Start()
    {
        transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
        updateRenderer();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void updateRenderer()
    {
        Debug.Log("rLines updateRenderer");
        rendererObj.ForEach(obj => Destroy(obj));
        rendererObj.Clear();

        foreach (var rLine in rLines.rLines)
        {
            GameObject obj = new GameObject("rLine renderer");
            obj.transform.SetParent(transform);
            obj.transform.rotation = Quaternion.Euler(90.0f, 0.0f, 0.0f);
            rendererObj.Add(obj);
            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = rLine.geom.NumPoints;
            lr.SetPositions(rLine.geom.Coordinates.Select(coor => Utils.Coor2Vec(coor)).ToArray());
            lr.alignment = LineAlignment.TransformZ;
            lr.useWorldSpace = true;
            lr.loop = false;
            lr.startWidth = width * 2.0f;
            lr.endWidth = width / 2.0f;
            lr.numCapVertices = 5;
            lr.numCornerVertices = 0;
            lr.material = material;
            lr.sortingOrder = 2;
        }

    }
}
