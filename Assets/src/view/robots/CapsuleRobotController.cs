using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleRobotController : MonoBehaviour, RobotHW
{
    private Action<ISensorData> listener;

    void Update()
    {
        listener?.Invoke(new Position() { x = transform.position.x, y = transform.position.z });

    }

    public void RegisterSensorDataListener(Action<ISensorData> listener)
    {
        this.listener = listener;
    }

    public void SetControlCommand(IControlCommand command)
    {

    }
}
