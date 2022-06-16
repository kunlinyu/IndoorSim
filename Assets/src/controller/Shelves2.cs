using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class Shelves2 : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public MapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    Vector3 firstPoint;
    Vector3 secondPoint;
    Vector3 lastPoint;


#pragma warning disable CS8618
    GameObject firstToSecondObj;
    GameObject rectangleObj;
#pragma warning restore CS8618
    List<GameObject> shelvesObj = new List<GameObject>();

    Vector3? mousePositionNullable = null;
    Vector3? mouseSnapPosition = null;

    int splitCount = 2;
    int shelfCount;
    int corridorCount;
    float shelfRatio;
    float shelfWidth;
    float corridorWidth;

    List<List<Vector3>> spaceVectors = new List<List<Vector3>>();

    bool firstIsShelf = true;


    // status == 0, Idle
    // status == 1, firstPoint clicked, going to click secondPoint
    // status == 2, secondPoint clicked, going to click lastPoint
    // status == 3, lastPoint clicked, going to scroll wheel, and click to apply
    // status > 3, Illegal
    int status = 0;

    // Start is called before the first frame update
    void Start()
    {
        firstToSecondObj = new GameObject("first to second");
        firstToSecondObj.transform.SetParent(transform);
        firstToSecondObj.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        firstToSecondObj.AddComponent<LineRenderer>();

        rectangleObj = new GameObject("rectangle");
        rectangleObj.transform.SetParent(transform);
        rectangleObj.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        rectangleObj.AddComponent<LineRenderer>();
    }

    void Update()
    {
        UpdateViewModel();
        UpdateView();
    }

    static bool isShelf(bool firstIsShelf, int i)
    => (i % 2 == 0) ^ !firstIsShelf;

    void UpdateViewModel()
    {
        mousePositionNullable = CameraController.mousePositionOnGround();
        mouseSnapPosition = mousePositionNullable;
        if (MousePickController.PointedVertex != null)
            mouseSnapPosition = Utils.Coor2Vec(MousePickController.PointedVertex.Vertex.Coordinate);
        if (mouseSnapPosition == null) return;

        switch (status)
        {
            case 0:
                firstPoint = mouseSnapPosition.Value;
                break;
            case 1:
                secondPoint = mouseSnapPosition.Value;
                break;
            case 2:
                lastPoint = mouseSnapPosition.Value;
                break;
            case 3:
                float current2First = (firstPoint - mouseSnapPosition.Value).magnitude;
                float current2Second = (secondPoint - mouseSnapPosition.Value).magnitude;
                float current2Last = (lastPoint - mouseSnapPosition.Value).magnitude;
                shelfRatio = current2Second / (current2Second + current2Last);
                if (shelfRatio < 0.1f) shelfRatio = 0.1f;
                if (shelfRatio > 0.9f) shelfRatio = 0.9f;

                if (Input.GetMouseButtonDown(2)) splitCount++;
                // if (Input.GetMouseButtonDown(1)) splitCount--;
                if (splitCount <= 1) splitCount = 1;
                corridorCount = splitCount / 2;
                shelfCount = splitCount - corridorCount;

                while (spaceVectors.Count < shelfCount + corridorCount)
                    spaceVectors.Add(new List<Vector3>(4));
                while (spaceVectors.Count > shelfCount + corridorCount)
                    spaceVectors.RemoveAt(spaceVectors.Count - 1);

                float totalWidth = (lastPoint - secondPoint).magnitude;

                if (corridorCount == shelfCount)
                {
                    shelfWidth = totalWidth / corridorCount * shelfRatio;
                    corridorWidth = totalWidth / corridorCount * (1 - shelfRatio);
                }
                else
                {
                    shelfWidth = totalWidth / (corridorCount + shelfRatio) * shelfRatio;
                    corridorWidth = totalWidth / (corridorCount + shelfRatio) * (1 - shelfRatio);
                }

                Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                Vector3 secondToLastDir = (lastPoint - secondPoint).normalized;
                for (int i = 0; i < spaceVectors.Count; i++)
                {
                    int previousCorridors = i / 2;
                    int previousShelves = i - previousCorridors;

                    float previousWidth = previousCorridors * Mathf.Abs(corridorWidth) + previousShelves * Mathf.Abs(shelfWidth);
                    float currentWidth = i % 2 == 0 ? Mathf.Abs(shelfWidth) : Mathf.Abs(corridorWidth);

                    spaceVectors[i].Clear();
                    spaceVectors[i].Add(firstPoint + secondToLastDir * previousWidth);
                    spaceVectors[i].Add(firstPoint + secondToLastDir * (previousWidth + currentWidth));
                    spaceVectors[i].Add(secondPoint + secondToLastDir * (previousWidth + currentWidth));
                    spaceVectors[i].Add(secondPoint + secondToLastDir * previousWidth);
                }


                break;
        }
        if (Input.GetMouseButtonUp(0) && !MouseOnUI)
        {
            switch (status)
            {
                case 0: status++; break;
                case 1: status++; break;
                case 2: status++; break;
                case 3:
                    IndoorSimData?.SessionStart();
                    IndoorSimData?.AddBoundaryAutoSnap(Utils.Vec2Coor(firstPoint), Utils.Vec2Coor(secondPoint));
                    CellBoundary? lastBoundary = null;
                    for (int i = 0; i < spaceVectors.Count; i++)
                    {
                        var b1 = IndoorSimData?.AddBoundaryAutoSnap(Utils.Vec2Coor(spaceVectors[i][0]), Utils.Vec2Coor(spaceVectors[i][1]));
                        if (b1 == null) break;
                        var b2 = IndoorSimData?.AddBoundaryAutoSnap(Utils.Vec2Coor(spaceVectors[i][1]), Utils.Vec2Coor(spaceVectors[i][2]));
                        if (b2 == null) break;
                        var b3 = IndoorSimData?.AddBoundaryAutoSnap(Utils.Vec2Coor(spaceVectors[i][2]), Utils.Vec2Coor(spaceVectors[i][3]));
                        if (b3 == null) break;

                        Navigable navigable = isShelf(firstIsShelf, i) ? Navigable.PhysicallyNonNavigable : Navigable.Navigable;
                        CellSpace newSpace = shelfWidth > 0.0f ? b3.leftSpace! : b3.rightSpace!;
                        IndoorSimData?.UpdateSpaceNavigable(newSpace!, navigable);

                        lastBoundary = b2;
                    }
                    IndoorSimData?.SessionCommit();
                    splitCount = 2;
                    status = 0;
                    break;
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            if (status == 3)
                if (splitCount > 1)
                    splitCount --;
                else
                    status --;
            else if (status > 0) status--;
            if (status == 0) firstIsShelf = true;
        }
    }

    void UpdateView()
    {
        // first to second
        if (status > 0)
        {
            LineRenderer lr = firstToSecondObj.GetComponent<LineRenderer>();

            if (status == 1 && mouseSnapPosition == null)
                lr.positionCount = 0;
            else
            {
                lr.positionCount = 2;
                lr.SetPosition(0, firstPoint);
                lr.SetPosition(1, secondPoint);

                lr.alignment = LineAlignment.TransformZ;
                lr.useWorldSpace = true;

                lr.loop = false;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.numCapVertices = 3;

                lr.sortingLayerID = sortingLayerId;
                lr.sortingOrder = 10;

                lr.material = draftMaterial;
            }
        }
        else
        {
            firstToSecondObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        if (status > 1)
        {
            LineRenderer lr = rectangleObj.GetComponent<LineRenderer>();

            if (status == 2 && mouseSnapPosition == null)
                lr.positionCount = 0;
            else
            {
                lr.positionCount = 4;
                lr.SetPosition(0, firstPoint);
                lr.SetPosition(1, firstPoint - secondPoint + lastPoint);
                lr.SetPosition(2, lastPoint);
                lr.SetPosition(3, secondPoint);

                lr.alignment = LineAlignment.TransformZ;
                lr.useWorldSpace = true;

                lr.loop = false;
                lr.startWidth = 0.05f;
                lr.endWidth = 0.05f;
                lr.numCapVertices = 3;

                lr.sortingLayerID = sortingLayerId;
                lr.sortingOrder = 10;

                lr.material = draftMaterial;
            }
        }
        else
        {
            rectangleObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        if (status == 3)
        {
            if (mouseSnapPosition == null)
            {
                foreach (var obj in shelvesObj)
                    Destroy(obj);
                shelvesObj.Clear();
            }
            else
            {
                while (shelvesObj.Count < spaceVectors.Count)
                {
                    string objName = "corridor";
                    if (isShelf(firstIsShelf, shelvesObj.Count))
                        objName = "shelf";
                    objName += " " + shelvesObj.Count.ToString();
                    GameObject obj = new GameObject(objName);
                    obj.transform.SetParent(transform);
                    obj.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
                    shelvesObj.Add(obj);
                    LineRenderer lr = obj.AddComponent<LineRenderer>();
                    lr.alignment = LineAlignment.TransformZ;
                    lr.useWorldSpace = true;
                    lr.loop = false;
                    lr.startWidth = 0.05f;
                    lr.endWidth = 0.05f;
                    lr.numCapVertices = 3;
                    lr.sortingLayerID = sortingLayerId;
                    lr.sortingOrder = 10;
                    lr.material = draftMaterial;
                }
                while (shelvesObj.Count > spaceVectors.Count)
                {
                    Destroy(shelvesObj[shelvesObj.Count - 1]);
                    shelvesObj.RemoveAt(shelvesObj.Count - 1);
                }

                for (int i = 0; i < shelvesObj.Count; i++)
                {
                    shelvesObj[i].GetComponent<LineRenderer>().positionCount = 4;
                    shelvesObj[i].GetComponent<LineRenderer>().SetPositions(spaceVectors[i].ToArray());
                }
            }
        }
        else
        {
            foreach (var obj in shelvesObj)
                Destroy(obj);
            shelvesObj.Clear();
        }

    }
}
