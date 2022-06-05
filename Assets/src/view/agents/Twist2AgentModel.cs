using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Twist2AgentModel : MonoBehaviour, IAgentHW
{
    public AgentDescriptor AgentDescriptor { get; set; }
    private Action<ISensorData> listener;

    void FixedUpdate()
    {
        listener?.Invoke(new Pose2() {
            x = transform.position.x,
            y = transform.position.z,
            theta = transform.rotation.eulerAngles.y,
        });
    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener += listener;
    }

    public void SetControlCommand(IControlCommand command)
    {
        Twist2 twist = command as Twist2 ?? throw new ArgumentException("accept only twist two wheel command");

        float v = (float)twist.v_x;
        float omega = (float)twist.omega_z;
        float dir = transform.rotation.eulerAngles.y;
        float t = Time.deltaTime;

        float Arc = v * t;
        float theta = omega * t;
        float secantDir = dir + theta / 2.0f;
        float lastDir = dir + theta;

        float secantLength;
        if (Math.Abs(theta) > 1e-3)
        {
            float R = v / omega;
            secantLength = Mathf.Sqrt(2.0f * (1 - Mathf.Cos(theta))) * R;  // The Law of Cosines (c^2 == a^2 + b^2 - 2ab*Cos(theta))
        }
        else
        {
            secantLength = Arc;
        }
        Vector3 secant= new Vector3(Mathf.Cos(secantDir), 0.0f, Mathf.Sin(secantDir)) * secantLength;

        Vector3 position = transform.position;
        position = position + secant;
        transform.position = position;

        Quaternion rotation = transform.rotation;
        transform.rotation = Quaternion.Euler(0.0f, lastDir, 0.0f);
    }

    public void ResetToInitStatus()
    {
        transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
        transform.rotation = Quaternion.Euler(0.0f, AgentDescriptor.theta, 0.0f);
    }
}
