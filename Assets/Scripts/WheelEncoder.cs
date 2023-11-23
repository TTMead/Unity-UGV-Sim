using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class WheelEncoder : MonoBehaviour
{
    public string topic_name;
    public float publish_rate_hz;

    float prev_publish_time;
    Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ROSConnection.GetOrCreateInstance().RegisterPublisher<RosMessageTypes.Geometry.TwistMsg>(topic_name);
    }

    // Update is called once per frame
    void Update()
    {
        if ((Time.time - prev_publish_time) > (1/publish_rate_hz))
        {
            prev_publish_time = Time.time;
            PublishEncoderMsg(rb.velocity.z, -rb.angularVelocity.y);
        }
    }

    void PublishEncoderMsg(float forward_velocity, float angular_velocity)
    {
        RosMessageTypes.Geometry.TwistMsg encoderMsg = new RosMessageTypes.Geometry.TwistMsg();
        encoderMsg.linear.x = forward_velocity;
        encoderMsg.linear.y = 0;
        encoderMsg.linear.z = 0;
        encoderMsg.angular.x = 0;
        encoderMsg.angular.y = 0;
        encoderMsg.angular.z = angular_velocity;
        ROSConnection.GetOrCreateInstance().Publish(topic_name, encoderMsg);
    }
}
