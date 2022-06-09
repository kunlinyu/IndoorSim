using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move2AgentModel : MonoBehaviour, IActuatorSensor
{
    public AgentDescriptor AgentDescriptor { get; set; }
    private Action<ISensorData> listener;

    private SpeedVec speed = new SpeedVec() { x = 0.0f, y = 0.0f };

    private bool reset = false;


    void FixedUpdate()
    {
        listener?.Invoke(new Position() { x = transform.position.x, y = transform.position.z });

        lock (speed)
        {
            Vector3 position = transform.position;
            position.x += (float)speed.x * Time.deltaTime;
            position.z += (float)speed.y * Time.deltaTime;
            transform.position = position;
        }

        if (reset)
        {
            reset = false;
            transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
            transform.rotation = Quaternion.Euler(0.0f, AgentDescriptor.theta, 0.0f);
        }

    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener += listener;
    }

    public void Execute(IActuatorCommand command)
    {
        lock (speed)
            speed = command as SpeedVec ?? throw new ArgumentException("accept only speed vector command");
    }

    public void ResetToInitStatus()
    {
        reset = true;
    }
}
