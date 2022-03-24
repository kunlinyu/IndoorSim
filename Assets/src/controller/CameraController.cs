using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] public const float KeyboardMoveSpeed = 1.0f;
    [SerializeField] public const float dragSpeed = 0.001f;
    [SerializeField] public const float rotationSpeed = 0.05f;
    [SerializeField] public const float mouseScrollSpeed = 1.0f;
    [SerializeField] public const float minHeight = 1.0f;

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

        if (Input.GetKey(KeyCode.W))
            move += Vector3.forward;
        if (Input.GetKey(KeyCode.A))
            move += Vector3.left;
        if (Input.GetKey(KeyCode.S))
            move += Vector3.back;
        if (Input.GetKey(KeyCode.D))
            move += Vector3.right;
        if (Input.GetKey(KeyCode.X))
            move += Vector3.up;
        if (Input.GetKey(KeyCode.Z))
            move += Vector3.down;

        if (Input.mouseScrollDelta.y != 0)
        {
            Debug.Log(Input.mouseScrollDelta.y);
            move += Vector3.up * Input.mouseScrollDelta.y * mouseScrollSpeed;
        }

        if (move.magnitude > 0.0)
            MoveCameraXZ(transform.position, KeyboardMoveSpeed * move);

        if (Input.GetMouseButtonDown(1))
        {
            anchorMouse = Input.mousePosition;
            anchorRot = transform.rotation;
        }

        if (Input.GetMouseButton(1))
        {
            Vector3 mouseDiff = anchorMouse - Input.mousePosition;
            Vector3 newEuler = anchorRot.eulerAngles + new Vector3(-mouseDiff.y, mouseDiff.x) * rotationSpeed;
            if (newEuler.x > 90.0f)
                newEuler.x = 90.0f;
            if (newEuler.x < 0.0f)
                newEuler.x = 0.0f;
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
}