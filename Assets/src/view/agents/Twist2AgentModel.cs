using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Twist2AgentModel : MonoBehaviour, IActuatorSensor
{
    public AgentDescriptor AgentDescriptor { get; set; }
    private Action<ISensorData> listener;

    private Twist2 twist = new Twist2() { v_x = 0.0f, omega_z = 0.0f };
    private bool reset = false;

    void FixedUpdate()
    {
        listener?.Invoke(new Pose2()
        {
            x = transform.position.x,
            y = transform.position.z,
            theta = -transform.rotation.eulerAngles.y / 180.0f * Math.PI,
        });

        lock (twist)
        {
            TwistToTransform(transform, twist, Time.deltaTime, out Vector3 position, out Quaternion rotation);
            transform.position = position;
            transform.rotation = rotation;
        }

        if (reset)
        {
            reset = false;
            transform.position = new Vector3(AgentDescriptor.x, 0.0f, AgentDescriptor.y);
            transform.rotation = Quaternion.Euler(0.0f, -AgentDescriptor.theta / Mathf.PI * 180.0f, 0.0f);
        }
    }

    void Update()
    {
        Twist2 twist2;
        lock (twist)
            twist2 = new Twist2() { v_x = twist.v_x, omega_z = twist.omega_z };

        int segments = 10;
        float timeForecast = 1.0f;

        var twistLRObj = transform.Find("RobotVelLineRenderer").gameObject;
        var twistLR = twistLRObj.GetComponent<LineRenderer>();
        twistLR.positionCount = 2;
        twistLR.SetPosition(0, transform.position);
        twistLR.SetPosition(1, transform.position + (transform.right * (float)twist2.v_x));

        var dirLRObj = transform.Find("RobotOmgLineRenderer").gameObject;
        var dirLR = dirLRObj.GetComponent<LineRenderer>();
        dirLR.positionCount = segments + 1;
        float omega = (float)(-twist2.omega_z / Math.PI * 180.0);
        for (int i = 0; i < segments; i++)
        {
            float omega_i = omega * timeForecast / segments * i;
            Vector3 agentFront = transform.right * (timeForecast);
            Vector3 rotatedDir = Quaternion.Euler(0.0f, omega_i, 0.0f) * agentFront;
            dirLR.SetPosition(i, transform.position + rotatedDir);
        }
        dirLR.SetPosition(segments, transform.position);
    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener += listener;
    }

    public void Execute(IActuatorCommand command)
    {
        lock (twist)
            twist = command as Twist2 ?? throw new ArgumentException("accept only twist two wheel command");
    }

    public void ResetToInitStatus()
    {
        reset = true;
    }

    private static void TwistToTransform(Transform transform, Twist2 twist, float t, out Vector3 position, out Quaternion rotation)
    {
        float v = (float)twist.v_x;
        float omega = (float)twist.omega_z;
        float dir = -transform.rotation.eulerAngles.y / 180.0f * Mathf.PI;

        float Arc = v * t;
        float theta = omega * t;
        float secantDir = dir + theta / 2.0f;
        float lastDir = dir + theta;

        float secantLength;
        if (Math.Abs(theta) > 1e-3)
        {
            float R = Math.Abs(v / omega);
            secantLength = Mathf.Sign(v) * Mathf.Sqrt(2.0f * (1 - Mathf.Cos(theta))) * R;  // The Law of Cosines (c^2 == a^2 + b^2 - 2ab*Cos(theta))
        }
        else
        {
            secantLength = Arc;
        }
        Vector3 secant = new Vector3(Mathf.Cos(secantDir), 0.0f, Mathf.Sin(secantDir)) * secantLength;

        position = transform.position + secant;
        rotation = Quaternion.Euler(0.0f, -lastDir / Mathf.PI * 180.0f, 0.0f);
    }
}
