using System;
using System.Collections.Generic;
using UnityEngine;

#nullable enable

public abstract class AgentController : MonoBehaviour, IActuatorSensor, Selectable
{
    public AgentDescriptor agentDescriptor;

    public AgentDescriptor AgentDescriptor
    {
        get => agentDescriptor;
        set
        {
            agentDescriptor = value;
            agentDescriptor.OnUpdate += UpdateTransform;
        }
    }
    public AbstractMotionExecutor? motionExecutor { get; set; }
    private bool reset = false;
    private Action<ISensorData> listener;

    protected IActuatorCommand? command = null;

    public AgentTypeMetaUnity? meta = null;

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

    public string Tip() => AgentDescriptor.type;

    public float Distance(Vector3 vec)
    {
        float distanceToCenter = (vec - new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y)).magnitude;
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
        transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
        transform.rotation = Quaternion.Euler(0.0f, (float)AgentDescriptor.theta / Mathf.PI * 180.0f * -1.0f, 0.0f);
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
            agentDescriptor.OnUpdate -= UpdateTransform;
    }
}
