using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Nav;

public class WheelEncoder : MonoBehaviour
{
    public string topic_name;
    public float publish_rate_hz;

    float prev_publish_time;
    Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ROSConnection.GetOrCreateInstance().RegisterPublisher<RosMessageTypes.Nav.OdometryMsg>(topic_name);
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
        RosMessageTypes.Nav.OdometryMsg encoderMsg = new RosMessageTypes.Nav.OdometryMsg();

        encoderMsg.pose.pose.position.x = 0;
        encoderMsg.pose.pose.position.y = 0;
        encoderMsg.pose.pose.position.z = 0;
        encoderMsg.pose.pose.orientation.x = 0;
        encoderMsg.pose.pose.orientation.y = 0;
        encoderMsg.pose.pose.orientation.z = 0;
        encoderMsg.pose.pose.orientation.w = 0;

        encoderMsg.twist.twist.linear.x = forward_velocity;
        encoderMsg.twist.twist.linear.y = 0;
        encoderMsg.twist.twist.linear.z = 0;
        encoderMsg.twist.twist.angular.x = 0;
        encoderMsg.twist.twist.angular.y = 0;
        encoderMsg.twist.twist.angular.z = angular_velocity;
        
        ROSConnection.GetOrCreateInstance().Publish(topic_name, encoderMsg);
    }
}
