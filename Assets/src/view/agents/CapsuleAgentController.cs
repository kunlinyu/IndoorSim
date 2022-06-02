using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleAgentController : MonoBehaviour, IAgentHW
{
    public AgentDescriptor AgentDescriptor { get; set; }
    private Action<ISensorData> listener;

    void Update()
    {
        listener?.Invoke(new Position() { x = transform.position.x, y = transform.position.z });
    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener += listener;
    }

    public void SetControlCommand(IControlCommand command)
    {


    }

    public void ResetToInitStatus()
    {
        transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
        transform.rotation = Quaternion.Euler(0.0f, AgentDescriptor.theta, 0.0f);
    }
}
