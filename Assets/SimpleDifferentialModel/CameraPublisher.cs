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

    private RenderTexture renderTexture;

    private const int isBigEndian = 0;
    private const int step = 4;

    private float last_send_time;

    private Camera cameraComp;

    void Start()
    {
        cameraComp = GetComponent<Camera>();

        // Render texture 
        renderTexture = cameraComp.targetTexture;
        // renderTexture = new RenderTexture(cameraComp.pixelWidth, cameraComp.pixelHeight, 24, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB);
        // renderTexture.Create();
        
        ROSConnection.GetOrCreateInstance().RegisterPublisher<RosMessageTypes.Sensor.ImageMsg>("/front_camera");

        last_send_time = 0f;

    }

    void Update()
    {
        if (Time.time - last_send_time > (1/fps_target)) {
            SendImage();
            last_send_time = Time.time;
        }
    }


    private void SendImage()
    {
        // cameraComp.targetTexture = renderTexture;
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        cameraComp.Render();

        Texture2D mainCameraTexture = new Texture2D(renderTexture.width, renderTexture.height);
        mainCameraTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        mainCameraTexture.Apply();
        RenderTexture.active = currentRT;

        // Convert screenshot to image message
        RosMessageTypes.Std.HeaderMsg header = new RosMessageTypes.Std.HeaderMsg();
        header.stamp.sec = (int)Time.time;
        header.stamp.nanosec = (uint)(Time.time*1e9);
        header.frame_id = "camera_link";
        ImageMsg imageMsg = mainCameraTexture.ToImageMsg(header);

        // publish
        ROSConnection.GetOrCreateInstance().Publish("/front_camera", imageMsg);
    }

    
    private void PublishImage(byte[] imageData)
    {
        uint imageHeight = (uint)renderTexture.height;
        uint imageWidth = (uint)renderTexture.width;

        RosMessageTypes.Std.HeaderMsg header = new RosMessageTypes.Std.HeaderMsg();
        header.stamp.sec = (int)Time.time;
        header.stamp.nanosec = (uint)(Time.time*1e9);
        header.frame_id = "camera_link";

        RosMessageTypes.Sensor.ImageMsg rosImage = new RosMessageTypes.Sensor.ImageMsg(header, imageHeight, imageWidth, "rgba8", isBigEndian, step, imageData);
        ROSConnection.GetOrCreateInstance().Publish("/front_camera", rosImage);
    }

    /// <summary>
    ///     Capture the main camera's render texture and convert to bytes.
    /// </summary>
    /// <returns>imageBytes</returns>
    private byte[] CaptureScreenshot()
    {
        Camera.main.targetTexture = renderTexture;
        RenderTexture currentRT = RenderTexture.active;
        RenderTexture.active = renderTexture;
        Camera.main.Render();
        Texture2D mainCameraTexture = new Texture2D(renderTexture.width, renderTexture.height);
        mainCameraTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        mainCameraTexture.Apply();
        RenderTexture.active = currentRT;
        // Get the raw byte info from the screenshot
        byte[] imageBytes = mainCameraTexture.GetRawTextureData();
        Camera.main.targetTexture = null;
        return imageBytes;
    }

}
