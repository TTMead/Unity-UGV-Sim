using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using Unity.Robotics.Core;

public class WheelEncoder : MonoBehaviour
{
    public string topic_name;
    public float publish_rate_hz;

    float prev_publish_time;
    Rigidbody rb;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ROSConnection.GetOrCreateInstance().RegisterPublisher<RosMessageTypes.Geometry.TwistWithCovarianceStampedMsg>(topic_name);
    }

    // Update is called once per frame
    void Update()
    {
        if ((Time.time - prev_publish_time) > (1/publish_rate_hz))
        {
            prev_publish_time = Time.time;
            PublishEncoderMsg(transform.InverseTransformDirection(rb.velocity).z, -rb.angularVelocity.y);
        }
    }

    void PublishEncoderMsg(float forward_velocity, float angular_velocity)
    {
        RosMessageTypes.Geometry.TwistWithCovarianceStampedMsg encoderMsg = new RosMessageTypes.Geometry.TwistWithCovarianceStampedMsg();

        var publishTime = Clock.time;
        encoderMsg.header.stamp.sec = (int)publishTime;
        encoderMsg.header.stamp.nanosec = (uint)((publishTime - Math.Floor(publishTime)) * Clock.k_NanoSecondsInSeconds);
        encoderMsg.header.frame_id = "base_link";

        encoderMsg.twist.twist.linear.x = forward_velocity;
        encoderMsg.twist.twist.linear.y = 0;
        encoderMsg.twist.twist.linear.z = 0;
        encoderMsg.twist.twist.angular.x = 0;
        encoderMsg.twist.twist.angular.y = 0;
        encoderMsg.twist.twist.angular.z = angular_velocity;

        const double epsilon = 1e-6;
        encoderMsg.twist.covariance[0] = epsilon;
        encoderMsg.twist.covariance[7] = epsilon;
        encoderMsg.twist.covariance[14] = epsilon;
        encoderMsg.twist.covariance[21] = epsilon;
        encoderMsg.twist.covariance[28] = epsilon;
        encoderMsg.twist.covariance[35] = epsilon;
        
        ROSConnection.GetOrCreateInstance().Publish(topic_name, encoderMsg);
    }
}
