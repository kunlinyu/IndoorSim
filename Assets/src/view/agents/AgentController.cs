using System;
using UnityEngine;

public abstract class AgentController : MonoBehaviour, IActuatorSensor, Selectable
{
    public AgentDescriptor agentDescriptor;

    public AgentDescriptor AgentDescriptor
    {
        get => agentDescriptor;
        set
        {
            agentDescriptor = value;
            if (agentDescriptor != null)
                agentDescriptor.OnUpdate += UpdateTransform;
        }
    }
    public AbstractMotionExecutor motionExecutor { get; set; }
    private bool reset = false;
    private Action<ISensorData> listener = (sensorData) => { };

    protected IActuatorCommand command = null;

    public AgentTypeMetaUnity meta = null;

    private bool _highLight = false;
    public bool highLight
    {
        get => _highLight;
        set
        {
            _highLight = value;
        }
    }
    private bool _selected = false;
    public bool selected
    {
        get => _selected;
        set
        {
            _selected = value;
            transform.Find("AgentShadow").gameObject.SetActive(_selected);
        }
    }
    public SelectableType type { get => SelectableType.Agent; }

    public string Tip() => agentDescriptor == null ? "" : agentDescriptor.type;

    public float Distance(Vector3 vec)
    {
        if (agentDescriptor == null) return float.MaxValue;
        float distanceToCenter = (vec - new Vector3(agentDescriptor.x, 0.0f, agentDescriptor.y)).magnitude;
        distanceToCenter -= meta!.collisionRadius;
        if (distanceToCenter < 0.0f) distanceToCenter = 0.0f;
        return distanceToCenter;
    }

    public void ResetToInitStatus() => reset = true;

    public void RegisterSensorDataListener(Action<ISensorData> listener) => this.listener += listener;

    public void Execute(IActuatorCommand cmd)
    {
        lock (command!) command = cmd;
    }

    void FixedUpdate()
    {
        listener?.Invoke(GetSensorData());

        lock (command!) UpdateTransform(command, transform);

        if (reset)
        {
            reset = false;
            UpdateTransform();
        }
    }

    void UpdateTransform()
    {
        if (agentDescriptor == null) return;
        transform.position = new Vector3(agentDescriptor.x, 0.0f, agentDescriptor.y);
        transform.rotation = Quaternion.Euler(0.0f, (float)agentDescriptor.theta / Mathf.PI * 180.0f * -1.0f, 0.0f);
    }

    protected abstract ISensorData GetSensorData();

    protected abstract void UpdateTransform(IActuatorCommand command, Transform transform);


    protected void OnEnable()
    {
        GetComponentInChildren<AgentShadowController>().meta = meta;
    }

    protected void Start()
    {
    }

    protected void Update()
    {
    }

    void OnDestroy()
    {
        if (agentDescriptor != null)
            if (agentDescriptor.OnUpdate != null)
                agentDescriptor.OnUpdate -= this.UpdateTransform;
    }
}
