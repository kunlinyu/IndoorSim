using System.Collections.Generic;
using UnityEngine;
#nullable enable

public class ShelvesEditor : MonoBehaviour, ITool
{
    public IndoorSimData? IndoorSimData { set; get; }
    public IndoorMapView? mapView { get; set; }
    public SimulationView? simView { set; get; }
    public bool MouseOnUI { set; get; }

    Vector3 firstPoint;
    Vector3 secondPoint;
    Vector3 lastPoint;

    float shelfWidth;
    float corridorWidth;
    int shelfCount;
    int corridorCount;

    // status == 0, Idle
    // status == 1, firstPoint clicked, going to click secondPoint
    // status == 2, secondPoint clicked, going to click shelfWidth
    // status == 3, shelfWidth clicked, going to click corridorWidth
    // status == 4, corridorWidth clicked, going to click shelves count
    // status > 4, Illegal
    int status = 0;

#pragma warning disable CS8618
    GameObject firstToSecondObj;
    GameObject shelfWidthLineObj;
    GameObject corridorWidthLineObj;
#pragma warning restore CS8618

    List<GameObject> shelvesObj = new List<GameObject>();

    Vector3? mousePositionNullable = null;
    Vector3? mouseSnapPosition = null;

    List<List<Vector3>> spaceVectors = new List<List<Vector3>>();

    bool firstIsShelf = true;


    void Start()
    {
        firstToSecondObj = Instantiate(Resources.Load<GameObject>("ToolObj/ShelvesLineRenderer"), this.transform);
        firstToSecondObj.name = "first to second";

        shelfWidthLineObj = Instantiate(Resources.Load<GameObject>("ToolObj/ShelvesLineRenderer"), this.transform);
        shelfWidthLineObj.name = "shelf width line";

        corridorWidthLineObj = Instantiate(Resources.Load<GameObject>("ToolObj/ShelvesLineRenderer"), this.transform);
        corridorWidthLineObj.name = "corridor width line";
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
            mouseSnapPosition = U.Coor2Vec(MousePickController.PointedVertex.Vertex.Coordinate);
        if (mouseSnapPosition == null) return;

        Vector3 segmentDir = (secondPoint - firstPoint).normalized;
        Vector3 toNew = mouseSnapPosition.Value - firstPoint;
        switch (status)
        {
            case 0:
                firstPoint = mouseSnapPosition.Value;
                break;
            case 1:
                secondPoint = mouseSnapPosition.Value;
                break;
            case 2:
                shelfWidth = Vector3.Cross(segmentDir, toNew).y;
                break;
            case 3:
                float shelfAndCorridorWidth = Vector3.Cross(segmentDir, toNew).y;
                if (Mathf.Abs(shelfAndCorridorWidth) > Mathf.Abs(shelfWidth) && shelfAndCorridorWidth * shelfWidth > 0.0f)
                    corridorWidth = shelfAndCorridorWidth - shelfWidth;
                else
                    corridorWidth = 0.0f;
                break;
            case 4:
                float blockWidth = Vector3.Cross(segmentDir, toNew).y;
                float pairWidth = shelfWidth + corridorWidth;
                int fullPairCount = Mathf.FloorToInt(Mathf.Abs(blockWidth / pairWidth));

                shelfCount = fullPairCount;
                corridorCount = fullPairCount;

                float remain = Mathf.Abs(blockWidth) - Mathf.Abs(fullPairCount * pairWidth);
                if (Mathf.Abs(remain) > 1e-3)
                    shelfCount++;
                if (Mathf.Abs(remain) > Mathf.Abs(shelfWidth))
                    corridorCount++;

                lastPoint = mouseSnapPosition.Value;

                while (spaceVectors.Count < shelfCount + corridorCount)
                    spaceVectors.Add(new List<Vector3>(4));
                while (spaceVectors.Count > shelfCount + corridorCount)
                    spaceVectors.RemoveAt(spaceVectors.Count - 1);

                Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                Vector3 secondToNew = lastPoint - secondPoint;
                Vector3 secondToNewDir = secondToNew.normalized;
                secondToNewDir = secondToNewDir / Mathf.Abs(Vector3.Dot(right, secondToNewDir));
                for (int i = 0; i < spaceVectors.Count; i++)
                {
                    int previousCorridors = i / 2;
                    int previousShelves = i - previousCorridors;

                    float previousWidth = previousCorridors * Mathf.Abs(corridorWidth) + previousShelves * Mathf.Abs(shelfWidth);
                    float currentWidth = i % 2 == 0 ? Mathf.Abs(shelfWidth) : Mathf.Abs(corridorWidth);

                    spaceVectors[i].Clear();
                    spaceVectors[i].Add(firstPoint + secondToNewDir * previousWidth);
                    spaceVectors[i].Add(firstPoint + secondToNewDir * (previousWidth + currentWidth));
                    spaceVectors[i].Add(secondPoint + secondToNewDir * (previousWidth + currentWidth));
                    spaceVectors[i].Add(secondPoint + secondToNewDir * previousWidth);
                }

                break;
            default:
                throw new System.Exception("Illegal shelves status: " + status);
        }


        if (Input.GetMouseButtonUp(0) && !MouseOnUI)
        {
            if (mousePositionNullable == null) return;
            mouseSnapPosition = mousePositionNullable.Value;
            if (MousePickController.PointedVertex != null)
                mouseSnapPosition = U.Coor2Vec(MousePickController.PointedVertex.Vertex.Coordinate);
            Debug.Log(status);

            switch (status)
            {
                case 0: status++; break;
                case 1: status++; break;
                case 2: status++; break;
                case 3: if (corridorWidth != 0.0f) status++; break;
                case 4:
                    IndoorSimData!.activeTiling.resultValidate = false;
                    IndoorSimData?.SessionStart();
                    IndoorSimData?.AddBoundaryAutoSnap(U.Vec2Coor(firstPoint), U.Vec2Coor(secondPoint));
                    CellBoundary? lastBoundary = null;
                    for (int i = 0; i < spaceVectors.Count; i++)
                    {
                        CellBoundary? b1 = IndoorSimData?.AddBoundaryAutoSnap(U.Vec2Coor(spaceVectors[i][0]), U.Vec2Coor(spaceVectors[i][1]));
                        if (b1 == null) break;
                        CellBoundary? b2 = IndoorSimData?.AddBoundaryAutoSnap(U.Vec2Coor(spaceVectors[i][1]), U.Vec2Coor(spaceVectors[i][2]));
                        if (b2 == null) break;
                        CellBoundary? b3 = IndoorSimData?.AddBoundaryAutoSnap(U.Vec2Coor(spaceVectors[i][2]), U.Vec2Coor(spaceVectors[i][3]));
                        if (b3 == null) break;

                        Navigable navigable = isShelf(firstIsShelf, i) ? Navigable.PhysicallyNonNavigable : Navigable.Navigable;
                        CellSpace? newSpace = b3.leftSpace != null ? b3.leftSpace : b3.rightSpace;
                        IndoorSimData?.UpdateSpaceNavigable(newSpace!, navigable);

                        lastBoundary = b2;
                    }
                    IndoorSimData?.SessionCommit();
                    IndoorSimData!.activeTiling.resultValidate = true;

                    firstPoint = U.Coor2Vec(lastBoundary!.P0.Coordinate);
                    secondPoint = U.Coor2Vec(lastBoundary!.P1.Coordinate);

                    if (spaceVectors.Count % 2 == 1)
                        firstIsShelf = !firstIsShelf;

                    status = 2;

                    break;
                default:
                    throw new System.Exception("Illegal shelves status: " + status);
            }
            Debug.Log(status);
        }

        if ((Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)) && !MouseOnUI)
        {
            if (status > 0) status--;
            if (status == 0) firstIsShelf = true;
        }
    }

    void ApplyToModel(Vector3 firstPoint, Vector3 secondPoint, Vector3 lastPoint,
                      float shelfWidth, float corridorWidth,
                      int shelfCount, int corridorCount)
    {

    }

    void UpdateView()
    {
        // first to second
        if (status > 0)
        {

            if (status == 1 && mouseSnapPosition == null)
            {
                firstToSecondObj.GetComponent<LineRenderer>().positionCount = 0;
            }
            else
            {
                var lr = firstToSecondObj.GetComponent<LineRenderer>();
                lr.positionCount = 2;
                lr.SetPosition(0, firstPoint);
                lr.SetPosition(1, secondPoint);
            }
        }
        else
        {
            firstToSecondObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        // shelf width
        if (status == 2 || status == 3)
        {

            if (status == 2 && mouseSnapPosition == null)
            {
                shelfWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
            }
            else
            {
                Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                Vector3 firstRight = firstPoint - right * shelfWidth;
                Vector3 secondRight = secondPoint - right * shelfWidth;

                var lr = shelfWidthLineObj.GetComponent<LineRenderer>();
                lr.positionCount = 4;
                lr.SetPosition(0, firstPoint);
                lr.SetPosition(1, firstRight);
                lr.SetPosition(2, secondRight);
                lr.SetPosition(3, secondPoint);
            }
        }
        else
        {
            shelfWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        // corridor width
        if (status == 3)
        {

            if (mouseSnapPosition == null)
            {
                corridorWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
            }
            else
            {
                Vector3 segmentDir = (secondPoint - firstPoint).normalized;

                if (corridorWidth == 0.0f)
                {
                    corridorWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
                }
                else
                {
                    Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                    Vector3 firstRight = firstPoint - right * shelfWidth;
                    Vector3 secondRight = secondPoint - right * shelfWidth;
                    Vector3 firstRightRight = firstRight - right * corridorWidth;
                    Vector3 secondRightRight = secondRight - right * corridorWidth;

                    var lr = corridorWidthLineObj.GetComponent<LineRenderer>();
                    lr.positionCount = 4;
                    lr.SetPosition(0, firstRight);
                    lr.SetPosition(1, firstRightRight);
                    lr.SetPosition(2, secondRightRight);
                    lr.SetPosition(3, secondRight);
                }
            }
        }
        else
        {
            corridorWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        if (status == 4)
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
                    GameObject obj = Instantiate(Resources.Load<GameObject>("ToolObj/ShelvesLineRenderer"), this.transform);
                    obj.name = objName;
                    shelvesObj.Add(obj);
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
