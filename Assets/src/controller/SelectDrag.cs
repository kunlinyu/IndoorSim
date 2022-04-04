using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetTopologySuite.Geometries;
#nullable enable

enum Status
{
    Idle,
    Selecting,
    Selected,
    Dragging,
}

[RequireComponent(typeof(LineRenderer))]
public class SelectDrag : MonoBehaviour, ITool
{
    public IndoorSim? IndoorSim { get; set; }
    public int sortingLayerId { get; set; }
    public Material? draftMaterial { get; set; }
    public bool MouseOnUI { get; set; }

    private Status status = Status.Idle;

    private bool adhoc = false;

    private List<VertexController> selectedVertices = new List<VertexController>();
    private List<BoundaryController> selectedBoundaries = new List<BoundaryController>();
    private List<SpaceController> selectedSpaces = new List<SpaceController>();

    private Vector3? mouseDownPosition = null;

    void Start()
    {
        transform.rotation = Quaternion.Euler(new Vector3(90.0f, 0.0f, 0.0f));
        GetComponent<LineRenderer>().positionCount = 0;
    }

    void Update()
    {
        Selectable? pointedEntity = MousePickController.PointedEntity;

        switch (status)
        {
            case Status.Idle:
                if (Input.GetMouseButtonDown(0) && !MouseOnUI)
                {
                    if (false && (pointedEntity != null && pointedEntity.type == SelectableType.Vertex))
                    {
                        adhoc = true;
                        selectedVertices.Add((VertexController)pointedEntity);
                        status = Status.Dragging;
                        Debug.Log(status);
                    }
                    else
                    {
                        status = Status.Selecting;
                        mouseDownPosition = CameraController.mousePositionOnGround();
                        Debug.Log(status);
                    }
                }
                else if (Input.GetMouseButtonUp(0))
                    throw new System.Exception("Oops");
                break;

            case Status.Selecting:
                if (Input.GetMouseButtonDown(0))
                    throw new System.Exception("Oops");
                else if (Input.GetMouseButtonUp(0))
                {
                    // TODO: detect and select

                    if (selectedVertices.Count > 0)
                    {
                        status = Status.Selected;
                        Debug.Log(status);
                    }

                    // TODO: notify selected entities
                }
                else
                {
                    // render bounding box
                    List<Vector3> square = SquareFromCursor();
                    if (square.Count > 0)
                    {
                        LineRenderer lr = GetComponent<LineRenderer>();
                        lr.positionCount = 4;
                        lr.SetPositions(square.ToArray());
                        lr.useWorldSpace = true;
                        lr.loop = true;
                        lr.startWidth = 0.1f;
                        lr.endWidth = 0.1f;
                        lr.material = draftMaterial;
                    }
                    else
                    {
                        GetComponent<LineRenderer>().positionCount = 0;
                    }
                }
                break;

            case Status.Selected:
                if (Input.GetMouseButtonUp(0))
                    throw new System.Exception("Oops");
                else if (Input.GetMouseButtonDown(0))
                {
                    if (false)  // on selected entities
                    {
                        // pointedEntity

                    }
                    else
                    {
                        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
                        {
                            // TODO: support hold control to select more

                        }
                        else
                        {
                            selectedVertices.Clear();
                            selectedBoundaries.Clear();
                            selectedSpaces.Clear();
                            // TODO: notify selected entities

                            status = Status.Selecting;
                            Debug.Log(status);
                        }
                    }


                }
                else if (Input.GetMouseButtonUp(1))
                {
                    selectedVertices.Clear();
                    selectedBoundaries.Clear();
                    selectedSpaces.Clear();
                    // TODO: notify selected entities

                    status = Status.Idle;
                    Debug.Log(status);
                }
                break;

            case Status.Dragging:
                break;
        }

        if (Input.GetMouseButtonDown(1))
        {
            status = Status.Idle;
            selectedVertices.Clear();
        }

    }

    List<Vector3> SquareFromCursor()
    {
        List<Vector3> result = new List<Vector3>();
        Vector3? currentMousePosition = CameraController.mousePositionOnGround();
        if (mouseDownPosition == null || currentMousePosition == null) return result;

        Vector3 P0 = (Vector3)mouseDownPosition;
        Vector3 P2 = (Vector3)currentMousePosition;

        Vector3? midBottom0 = CameraController.screenPositionOnGround(new Vector3(Screen.width / 2, 0, 0));
        Vector3? midBottom1 = CameraController.screenPositionOnGround(new Vector3(Screen.width / 2, 2, 0));
        Vector3? up_ = midBottom1 - midBottom0;

        if (up_ != null)
        {
            Vector3 L = P0 - P2;
            Vector3 up = (Vector3)(up_.Value.normalized);
            float sin = Vector3.Dot(L.normalized, up);

            Vector3 P1 = (Vector3)P0 - L.magnitude * sin * up;
            Vector3 P3 = (Vector3)P2 + L.magnitude * sin * up;

            result.Add(P0);
            result.Add(P1);
            result.Add(P2);
            result.Add(P3);
        }

        return result;
    }
}
