using System.Collections;
using System.Collections.Generic;
using NetTopologySuite.Geometries;
using UnityEngine;
#nullable enable

public class Shelves : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { set; get; }
    public MapView? mapView { get; set; }
    public int sortingLayerId { set; get; }
    public Material? draftMaterial { set; get; }
    public bool MouseOnUI { set; get; }

    Vector3 firstPoint;
    Vector3 secondPoint;
    float shelfWidth;
    float corridorWidth;
    int shelfCount;
    int corridorCount;
    Vector3 lastPoint;

    // status == 0, Idle
    // status == 1, firstPoint clicked, going to click secondPoint
    // status == 2, secondPoint clicked, going to click shelfWidth
    // status == 3, shelfWidth clicked, going to click corridorWidth
    // status == 4, corridorWidth clicked, going to click shelves count
    // status > 4, Illegal
    int status = 0;

    GameObject firstToSecondObj;
    GameObject shelfWidthLineObj;
    GameObject corridorWidthLineObj;
    List<GameObject> shelvesObj = new List<GameObject>();

    Vector3? mousePositionNullable = null;


    void Start()
    {
        firstToSecondObj = new GameObject("first to second");
        firstToSecondObj.transform.SetParent(transform);
        firstToSecondObj.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        firstToSecondObj.AddComponent<LineRenderer>();

        shelfWidthLineObj = new GameObject("shelf width line");
        shelfWidthLineObj.transform.SetParent(transform);
        shelfWidthLineObj.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        shelfWidthLineObj.AddComponent<LineRenderer>();

        corridorWidthLineObj = new GameObject("corridor width line");
        corridorWidthLineObj.transform.SetParent(transform);
        corridorWidthLineObj.transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        corridorWidthLineObj.AddComponent<LineRenderer>();
    }


    void Update()
    {
        mousePositionNullable = CameraController.mousePositionOnGround();
        UpdateViewModel();
        UpdateView();
    }

    void UpdateViewModel()
    {
        if (Input.GetMouseButtonUp(0) && !MouseOnUI)
        {
            if (mousePositionNullable == null) return;
            Vector3 mousePosition = mousePositionNullable.Value;
            Debug.Log(status);

            switch (status)
            {
                case 0:
                    firstPoint = mousePosition;
                    status++;
                    break;
                case 1:
                    secondPoint = mousePosition;
                    status++;
                    break;
                case 2:
                    {
                        Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                        Vector3 toNew = mousePosition - firstPoint;
                        shelfWidth = Vector3.Cross(segmentDir, toNew).y;
                        status++;
                        break;
                    }
                case 3:
                    {
                        Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                        Vector3 toNew = mousePosition - firstPoint;
                        float shelfAndCorridorWidth = Vector3.Cross(segmentDir, toNew).y;
                        if (Mathf.Abs(shelfAndCorridorWidth) > Mathf.Abs(shelfWidth))
                        {
                            corridorWidth = shelfAndCorridorWidth - shelfWidth;
                            status++;
                        }
                        break;
                    }
                case 4:
                    {
                        Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                        Vector3 toNew = mousePosition - firstPoint;
                        float blockWidth = Vector3.Cross(segmentDir, toNew).y;
                        float pairWidth = shelfWidth + corridorWidth;
                        int fullPairCount = Mathf.FloorToInt(blockWidth / pairWidth);

                        shelfCount = fullPairCount + 1;
                        corridorCount = fullPairCount;

                        float remain = blockWidth - fullPairCount * pairWidth;
                        if (remain > shelfWidth)
                            corridorCount++;

                        lastPoint = mousePosition;

                        // TODO: calculate the latest two points and make it to be the new first and second, and jump to 2
                        // TODO: click right button to cancel

                        status = 0;

                        break;
                    }
                default:
                    throw new System.Exception("Illegal shelves status: " + status);
            }
            Debug.Log(status);
        }
    }

    void UpdateView()
    {

        // first to second
        if (status > 0)
        {
            Vector3 currentPoint;
            LineRenderer lr = firstToSecondObj.GetComponent<LineRenderer>();

            if (status == 1 && mousePositionNullable == null)
                lr.positionCount = 0;
            else
            {
                if (status == 1 && mousePositionNullable != null)
                    currentPoint = mousePositionNullable.Value;
                else
                    currentPoint = secondPoint;

                lr.positionCount = 2;
                lr.SetPosition(0, firstPoint);
                lr.SetPosition(1, currentPoint);

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

        // shelf width
        if (status == 2 || status == 3)
        {
            float currentShelfWidth;
            LineRenderer lr = shelfWidthLineObj.GetComponent<LineRenderer>();

            if (status == 2 && mousePositionNullable == null)
            {
                lr.positionCount = 0;
            }
            else
            {
                Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                if (status == 2 && mousePositionNullable != null)
                {
                    Vector3 toNew = mousePositionNullable.Value - firstPoint;
                    currentShelfWidth = Vector3.Cross(segmentDir, toNew).y;
                }
                else
                {
                    currentShelfWidth = shelfWidth;
                }

                Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                Vector3 firstRight = firstPoint - right * currentShelfWidth;
                Vector3 secondRight = secondPoint - right * currentShelfWidth;

                lr.positionCount = 4;

                lr.SetPosition(0, firstPoint);
                lr.SetPosition(1, firstRight);
                lr.SetPosition(2, secondRight);
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
            shelfWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        // corridor width
        if (status == 3)
        {
            LineRenderer lr = corridorWidthLineObj.GetComponent<LineRenderer>();

            if (mousePositionNullable == null)
            {
                lr.positionCount = 0;
            }
            else
            {
                Vector3 segmentDir = (secondPoint - firstPoint).normalized;

                Vector3 toNew = mousePositionNullable.Value - firstPoint;
                float currentBlockWidth = Vector3.Cross(segmentDir, toNew).y;

                if (currentBlockWidth * shelfWidth < 0.0f || Mathf.Abs(currentBlockWidth) <= Mathf.Abs(shelfWidth))
                {
                    lr.positionCount = 0;
                }
                else
                {
                    Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                    Vector3 firstRight = firstPoint - right * shelfWidth;
                    Vector3 secondRight = secondPoint - right * shelfWidth;
                    Vector3 firstRightRight = firstRight - right * (currentBlockWidth - shelfWidth);
                    Vector3 secondRightRight = secondRight - right * (currentBlockWidth - shelfWidth);

                    lr.positionCount = 4;

                    lr.SetPosition(0, firstRight);
                    lr.SetPosition(1, firstRightRight);
                    lr.SetPosition(2, secondRightRight);
                    lr.SetPosition(3, secondRight);

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
        }
        else
        {
            corridorWidthLineObj.GetComponent<LineRenderer>().positionCount = 0;
        }

        if (status == 4)
        {
            if (mousePositionNullable == null)
            {
                foreach (var obj in shelvesObj)
                    Destroy(obj);
                shelvesObj.Clear();
            }
            else
            {
                Vector3 segmentDir = (secondPoint - firstPoint).normalized;
                Vector3 toNew = mousePositionNullable.Value - firstPoint;
                Vector3 right = Quaternion.AngleAxis(-90.0f, Vector3.up) * segmentDir;
                float blockWidth = Vector3.Cross(segmentDir, toNew).magnitude;
                float pairWidth = Mathf.Abs(shelfWidth + corridorWidth);
                int fullPairCount = Mathf.FloorToInt(blockWidth / pairWidth);

                shelfCount = fullPairCount + 1;
                corridorCount = fullPairCount;

                float remain = blockWidth - fullPairCount * pairWidth;
                if (remain > Mathf.Abs(shelfWidth))
                    corridorCount += 1;

                while (shelvesObj.Count < Mathf.Abs(shelfCount + corridorCount))
                {
                    string objName = "shelf";
                    if (shelvesObj.Count % 2 == 1)
                        objName = "corridor";
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
                while (shelvesObj.Count > Mathf.Abs(shelfCount + corridorCount))
                {
                    Destroy(shelvesObj[shelvesObj.Count - 1]);
                    shelvesObj.RemoveAt(shelvesObj.Count - 1);
                }

                Vector3 secondToNew = mousePositionNullable.Value - secondPoint;
                Vector3 secondToNewDir = secondToNew.normalized;
                secondToNewDir = secondToNewDir / Mathf.Abs(Vector3.Dot(right, secondToNewDir));
                for (int i = 0; i < shelvesObj.Count; i++)
                {
                    int previousCorridors = i / 2;
                    int previousShelves = i - previousCorridors;
                    float previousWidth = previousCorridors * Mathf.Abs(corridorWidth) + previousShelves * Mathf.Abs(shelfWidth);
                    float currentWidth = (i % 2 == 0) ? Mathf.Abs(shelfWidth) : Mathf.Abs(corridorWidth);

                    Vector3[] points = new Vector3[4];

                    points[0] = firstPoint + secondToNewDir * previousWidth;
                    points[1] = firstPoint + secondToNewDir * (previousWidth + currentWidth);
                    points[2] = secondPoint + secondToNewDir * (previousWidth + currentWidth);
                    points[3] = secondPoint + secondToNewDir * previousWidth;

                    shelvesObj[i].GetComponent<LineRenderer>().positionCount = 4;
                    shelvesObj[i].GetComponent<LineRenderer>().SetPositions(points);
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
