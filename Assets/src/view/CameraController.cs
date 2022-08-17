using UnityEngine;


[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    [SerializeField] public const float KeyboardMoveSpeed = 0.02f;
    [SerializeField] public const float dragSpeed = 0.001f;
    [SerializeField] public const float rotationSpeed = 0.05f;
    [SerializeField] public const float mouseScrollSpeed = 10.0f;
    [SerializeField] public const float minHeight = 1.0f;
    [SerializeField] public const float maxHeight = 100.0f;

    private static Plane ground = new Plane(Vector3.up, Vector3.zero);

    static public Vector3 CameraPosition;

    Vector3 anchorMouse;
    Quaternion anchorRot;
    Vector3 anchorPosition;

    void Start()
    {
        Application.targetFrameRate = 60;
    }

    void Update()
    {
        Vector3 move = Vector3.zero;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            move += Vector3.forward;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            move += Vector3.left;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            move += Vector3.back;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            move += Vector3.right;
        if (Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.PageUp))
            move += Vector3.up;
        if (Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.PageDown))
            move += Vector3.down;

        move += Vector3.up * Input.mouseScrollDelta.y * mouseScrollSpeed;

        float rot = 0.0f;
        if (Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.Home))
            rot += 20.0f;
        if (Input.GetKey(KeyCode.E) || Input.GetKey(KeyCode.End))
            rot -= 20.0f;

        Vector3 e = transform.rotation.eulerAngles + new Vector3(0.0f, rot) * rotationSpeed;
        if (e.x > 90.0f) e.x = 90.0f;
        if (e.x < 0.0f) e.x = 0.0f;
        transform.rotation = Quaternion.Euler(e);


        float move_space = transform.position.y * KeyboardMoveSpeed;
        if (move.magnitude > 0.0)
            MoveCameraXZ(transform.position, move_space * move);

        if (Input.GetMouseButtonDown(1))
        {
            anchorMouse = Input.mousePosition;
            anchorRot = transform.rotation;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 mouseDiff = anchorMouse - Input.mousePosition;
            Vector3 newEuler = anchorRot.eulerAngles + new Vector3(-mouseDiff.y, mouseDiff.x) * rotationSpeed;
            if (newEuler.x > 90.0f) newEuler.x = 90.0f;
            if (newEuler.x < 0.0f) newEuler.x = 0.0f;
            transform.rotation = Quaternion.Euler(newEuler);
        }

        if (Input.GetMouseButtonDown(2))
        {
            anchorMouse = Input.mousePosition;
            anchorPosition = transform.position;
        }
        if (Input.GetMouseButton(2))
        {
            Vector3 mouseDiff = anchorMouse - Input.mousePosition;
            Vector3 dragMove = new Vector3(mouseDiff.x, 0.0f, mouseDiff.y);
            MoveCameraXZ(anchorPosition, dragMove * dragSpeed * transform.position.y);
        }

        if (transform.position.y < minHeight)
        {
            var pos = transform.position;
            pos.y = minHeight;
            transform.position = pos;
        }
        if (transform.position.y > maxHeight)
        {
            var pos = transform.position;
            pos.y = maxHeight;
            transform.position = pos;
        }

        CameraPosition = Camera.main.transform.position;
    }

    void MoveCameraXZ(Vector3 anchorPosition, Vector3 moveVector)
    {
        Vector3 cameraEuler = transform.rotation.eulerAngles;
        float pitch = cameraEuler.x;
        cameraEuler.x = 0.0f;
        transform.rotation = Quaternion.Euler(cameraEuler);

        transform.position = anchorPosition;
        transform.Translate(moveVector);
        cameraEuler.x = pitch;
        transform.rotation = Quaternion.Euler(cameraEuler);
    }

    public static Vector3? screenPositionOnGround(Vector3 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        if (ground.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return null;
    }

    public static Vector3? mousePositionOnGround()
        => screenPositionOnGround(Input.mousePosition);
}