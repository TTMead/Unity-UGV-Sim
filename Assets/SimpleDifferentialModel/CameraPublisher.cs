using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

public class CameraPublisher : MonoBehaviour
{
    public float fps_target = 10f;

    private RenderTexture beboopPovRenderTexture;
    private float last_send_time;
    private Camera cameraComp;

    void Start()
    {
        // Extract the render texture for this camera
        cameraComp = GetComponent<Camera>();   
        beboopPovRenderTexture = cameraComp.targetTexture;
        
        // Initialise image publisher
        ROSConnection.GetOrCreateInstance().RegisterPublisher<RosMessageTypes.Sensor.ImageMsg>("/front_camera");

        last_send_time = 0f;
    }

    void Update()
    {
        // Queue screenshots to be published at a desired fps target rate
        if (Time.time - last_send_time > (1/fps_target)) {
            StartCoroutine(QueueScreenshot());
            last_send_time = Time.time;
        }
    }

    /// <summary>
    /// Waits for the end of a frame before taking a screenshot and publishing the image over ROS
    /// </summary>
    private IEnumerator QueueScreenshot()
    {
        yield return new WaitForEndOfFrame();

        // Set the current render texture to our Beboop-pov camera
        RenderTexture.active = beboopPovRenderTexture;

        // Create Texture2D
        Texture2D screenshotTexture = new Texture2D(beboopPovRenderTexture.width, beboopPovRenderTexture.height);

        // Copy the pixels of the current renderer onto the texture 
        screenshotTexture.ReadPixels(new Rect(0, 0, beboopPovRenderTexture.width, beboopPovRenderTexture.height), 0, 0);
        screenshotTexture.Apply();

        // Release the current render texture
        RenderTexture.active = null;

        // Create ROS header
        RosMessageTypes.Std.HeaderMsg header = new RosMessageTypes.Std.HeaderMsg();
        header.stamp.sec = (int)Time.time;
        header.stamp.nanosec = (uint)(Time.time*1e9);
        header.frame_id = "camera_link";

        // Convert screenshot to image message with header
        ImageMsg imageMsg = screenshotTexture.ToImageMsg(header);

        ROSConnection.GetOrCreateInstance().Publish("/front_camera", imageMsg);
    }

}