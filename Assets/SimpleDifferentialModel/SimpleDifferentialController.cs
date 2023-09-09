using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class SimpleDifferentialController : MonoBehaviour
{
    [SerializeField]
    float throttle_scaling = 250f;
    [SerializeField]
    float yawrate_scaling = 1400f;

    RosMessageTypes.Geometry.TwistMsg prev_twist_msg;
    Rigidbody rb;
    float last_msg_time;

    void Start()
    {
        ROSConnection.GetOrCreateInstance().Subscribe<RosMessageTypes.Geometry.TwistMsg>("cmd_vel", CallbackCmdVel);
        rb = GetComponent<Rigidbody>();

        prev_twist_msg = new RosMessageTypes.Geometry.TwistMsg();
        last_msg_time = 0f;
    }

    void Update()
    {
        rb.AddRelativeForce(Vector3.back * (float)prev_twist_msg.linear.x * throttle_scaling * Time.deltaTime, ForceMode.Acceleration);
        rb.AddRelativeTorque(Vector3.down * (float)prev_twist_msg.angular.z * yawrate_scaling * Time.deltaTime, ForceMode.Acceleration);

        // 0.5s timeout on cmd_vel messages
        if (Time.time - last_msg_time > 0.5) {
            prev_twist_msg = new RosMessageTypes.Geometry.TwistMsg();
        }
    }

    void CallbackCmdVel(RosMessageTypes.Geometry.TwistMsg twist_msg)
    {
        prev_twist_msg = twist_msg;
        last_msg_time = Time.time;
    }
}
