using System;
using UnityEngine;

#nullable enable

public class Twist2AgentModel : AgentController
{
    new void Start()
    {
        base.Start();
        command = new Twist2() { v_x = 0.0d, omega_z = 0.0d };
    }

    protected override ISensorData GetSensorData()
        => new Pose2()
        {
            x = transform.position.x,
            y = transform.position.z,
            theta = -transform.rotation.eulerAngles.y / 180.0f * Math.PI,
        };

    protected override void UpdateTransform(IActuatorCommand cmd, Transform transform)
    {
        Twist2 command = cmd as Twist2 ?? throw new ArgumentException("accept only twist");
        float v = (float)command.v_x;
        float omega = (float)command.omega_z;
        float dir = -transform.rotation.eulerAngles.y / 180.0f * Mathf.PI;

        float Arc = v * Time.deltaTime;
        float theta = omega * Time.deltaTime;
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

        var position = transform.position + secant;
        var rotation = Quaternion.Euler(0.0f, -lastDir / Mathf.PI * 180.0f, 0.0f);


        transform.position = position;
        transform.rotation = rotation;
    }

    void Update()
    {
        var twistLR = transform.Find("AgentVelLineRenderer").gameObject.GetComponent<LineRenderer>();
        var dirLR = transform.Find("AgentOmgLineRenderer").gameObject.GetComponent<LineRenderer>();
        UpdateTwistCommandLineRender(twistLR, dirLR);

        var motionLR = transform.Find("AgentMotionLineRenderer").gameObject.GetComponent<LineRenderer>();
        UpdateMotionLineRender(motionLR);
    }

    void UpdateTwistCommandLineRender(LineRenderer velLR, LineRenderer dirLR)
    {
        Twist2 twist2;
        lock (command)
        {
            Twist2 twist = command as Twist2 ?? throw new ArgumentException("accept only twist");
            twist2 = new Twist2() { v_x = twist.v_x, omega_z = twist.omega_z };
        }

        int segments = 10;
        float timeForecast = 1.0f;

        velLR.positionCount = 2;
        velLR.SetPosition(0, transform.position);
        velLR.SetPosition(1, transform.position + (transform.right * (float)twist2.v_x));

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

    void UpdateMotionLineRender(LineRenderer lr)
    {
        if (motionExecutor != null && motionExecutor.currentGoal != null)
        {
            if (motionExecutor.currentGoal.type == MotionType.Move)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, transform.position);
                var moveToCoor = motionExecutor.currentGoal as MoveToCoorMotion;
                if (moveToCoor == null) throw new Exception("can not cast to MoveToCoorMotion");
                Vector3 target = new Vector3((float)moveToCoor.x, 0.0f, (float)moveToCoor.y);
                lr.SetPosition(1, target);
            }
            else
            {
                throw new Exception("unknown motion type: " + motionExecutor.currentGoal.type);
            }
        }
        else
        {
            lr.positionCount = 0;
        }
    }
}
