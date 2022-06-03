using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleAgentController : MonoBehaviour, IAgentHW
{
    public AgentDescriptor AgentDescriptor { get; set; }
    private Action<ISensorData> listener;

    void FixedUpdate()
    {
        listener?.Invoke(new Position() { x = transform.position.x, y = transform.position.z });
    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener += listener;
    }

    public void SetControlCommand(IControlCommand command)
    {
        SpeedVec speed = command as SpeedVec ?? throw new ArgumentException("accept only speed vector command");
        Vector3 position = transform.position;
        position.x += (float)speed.x * Time.deltaTime;
        position.z += (float)speed.y * Time.deltaTime;
        transform.position = position;
    }

    public void ResetToInitStatus()
    {
        transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
        transform.rotation = Quaternion.Euler(0.0f, AgentDescriptor.theta, 0.0f);
    }
}
