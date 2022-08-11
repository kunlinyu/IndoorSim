using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AgentShadowController : MonoBehaviour
{
    public Material freeMat;
    public Material collidedMat;
    public bool collided = false;
    public AgentTypeMetaUnity meta;
    public Vector3 center = new Vector3(0.0f, 0.0f, 0.0f);

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
    }

    void Update()
    {
        var lr = GetComponent<LineRenderer>();
        lr.material = collided ? collidedMat : freeMat;
        lr.positionCount = 30;
        lr.SetPositions(CirclePosition(center, meta.collisionRadius, 30));
    }
}
