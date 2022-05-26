using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AgentShadowController : MonoBehaviour
{
    public float radius;
    public Material freeMat;
    public Material collidedMat;
    public bool collided = false;
    public Vector3 center;

    private static Vector3[] CirclePosition(Vector3 center, float radius, int step)
    {
        Vector3[] result = new Vector3[step];
        for (int i = 0; i < step; i++)
        {
            float theta = 2 * Mathf.PI / step * i;
            float x = center.x + radius * Mathf.Cos(theta);
            float z = center.z + radius * Mathf.Sin(theta);
            result[i] = new Vector3(x, 0.0f, z);
        }

        return result;
    }

    void Start()
    {
        var lr = GetComponent<LineRenderer>();
        lr.positionCount = 30;
        lr.SetPositions(CirclePosition(center, radius, 30));
        lr.material = freeMat;
    }

    void Update()
    {
        if (collided)
            GetComponent<LineRenderer>().material = collidedMat;
        else
            GetComponent<LineRenderer>().material = freeMat;

        GetComponent<LineRenderer>().SetPositions(CirclePosition(center, radius, 30));
    }
}
