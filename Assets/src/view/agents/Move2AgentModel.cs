using System;
using UnityEngine;

#nullable enable

public class Move2AgentModel : AgentController
{
    new void Start()
    {
        base.Start();
        command = new SpeedVec() { x = 0.0d, y = 0.0d };
    }
    protected override ISensorData GetSensorData()
        => new Position() { x = transform.position.x, y = transform.position.z };

    protected override void UpdateTransform(IActuatorCommand cmd, Transform transform)
    {
        SpeedVec command = cmd as SpeedVec ?? throw new ArgumentException("accept only speed vector");
        Vector3 position = transform.position;
        position.x += (float)command.x * Time.deltaTime;
        position.z += (float)command.y * Time.deltaTime;
        transform.position = position;
    }

    new void Update()
    {
        var speedLR = transform.Find("AgentSpeedLineRenderer").gameObject.GetComponent<LineRenderer>();
        UpdateSpeedCommandLineRender(speedLR);

        var motionLR = transform.Find("AgentMotionLineRenderer").gameObject.GetComponent<LineRenderer>();
        UpdateMotionLineRender(motionLR);
    }

    void UpdateSpeedCommandLineRender(LineRenderer lr)
    {
        lr.positionCount = 2;
        lr.SetPosition(0, transform.position);
        Vector3 speedTarget;
        lock (command!)
        {
            SpeedVec speed = command as SpeedVec ?? throw new ArgumentException("accept only speed vector");
            speedTarget = transform.position + new Vector3((float)speed.x, 0.0f, (float)speed.y);
        }
        lr.SetPosition(1, speedTarget);
    }

    void UpdateMotionLineRender(LineRenderer lr)
    {
        if (motionExecutor != null && motionExecutor.currentGoal != null)
        {
            if (motionExecutor.currentGoal.type == MotionType.Move)
            {
                lr.positionCount = 2;
                lr.SetPosition(0, transform.position);
                var moveToCoor = motionExecutor.currentGoal as MoveToCoorMotion ?? throw new Exception("can not cast to MoveToCoorMotion");
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
