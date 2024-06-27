using RosMessageTypes.Sensor;
using RosMessageTypes.Std;
using RosMessageTypes.BuiltinInterfaces;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;

public class ImuFromTransform : MonoBehaviour
{
    [SerializeField] private string frameId = "imu";
    [SerializeField] private string topicName = "mavros/imu/data";
    
    [Header("Noise Parameters")]
    [SerializeField] [Tooltip("dps/sqrt(Hz)")] private float gyroscopeNoiseDensity = 0.015f;
    [SerializeField] [Tooltip("Hz")] private float gyroscopeSampleRate = 1125.0f;
    [SerializeField] [Tooltip("ug/sqrt(Hz)")] private float accelerometerNoiseDensity = 230.0f;

    [Header("Published Variances")]
    [SerializeField] private float linearAccelerationVariance = 0.0003f;
    [SerializeField] private float angularVelocityVariance = 0.0003f;
    [SerializeField] private float orientationVariance = 1.0f;
    
    private Quaternion rotationLast = Quaternion.identity;
    private Vector3 positionLast = Vector3.zero;
    private Vector3 velocityLast = Vector3.zero;

    private Vector3 angularVelocity = Vector3.zero;
    private Vector3 linearVelocity = Vector3.zero;
    private Vector3 linearAcceleration = Vector3.zero;

    void Start() 
    {
        ROSConnection.GetOrCreateInstance().RegisterPublisher<ImuMsg>(topicName);
    }

    void FixedUpdate()
    {
        updateTransforms();

        sendImuMsg(
            transform.rotation,
            addAngularVelocityNoise(angularVelocity),
            addAccelerationNoise(linearAcceleration)
        );
    }

    private void updateTransforms()
    {
        // Calculate delta rotation
        Quaternion rotation_delta = Quaternion.Inverse(rotationLast) * transform.rotation;
        rotationLast = transform.rotation;

        // Convert delta rotation into euler form
        Vector3 angVelDeg = wrapTo180(rotation_delta.eulerAngles);

        // Divide by time and convert to degrees
        angularVelocity = (angVelDeg * Mathf.Deg2Rad) / Time.fixedDeltaTime;

        // Derive position to velocity
        linearVelocity = (transform.position - positionLast) / Time.fixedDeltaTime;
        positionLast = transform.position;

        // Derive velocity to acceleration
        linearAcceleration = (linearVelocity - velocityLast) / Time.fixedDeltaTime;
        velocityLast = linearVelocity;

        // Add gravity to linear acceleration
        Vector3 gravityVector = new Vector3(0f, 9.81f, 0f);
        linearAcceleration = transform.InverseTransformDirection(linearAcceleration + gravityVector);

        // Check for any NaNs
        angularVelocity = checkForNans(angularVelocity, "Angular Velocity");
        linearVelocity = checkForNans(linearVelocity, "Linear Velocity");
        linearAcceleration = checkForNans(linearAcceleration, "Linear Acceleration");
    }
    
    private static Vector3 wrapTo180(Vector3 angleVector)
    {
        Vector3 wrappedVector;
        wrappedVector.x = wrapTo180(angleVector.x);
        wrappedVector.y = wrapTo180(angleVector.y);
        wrappedVector.z = wrapTo180(angleVector.z);
        return wrappedVector;
    }

    private static float wrapTo180(float angle)
    {
        return (angle > 180) ? (angle - 360) : angle;
    }

    private static Vector3 checkForNans(Vector3 vec, string vec_label)
    {
        if (float.IsNaN(vec.x) || float.IsNaN(vec.y) || float.IsNaN(vec.z))
        {
            Debug.LogError(vec_label + " contains NaNs");
            return Vector3.zero;
        }
        return vec;
    }
    
    private Vector3 addAngularVelocityNoise(Vector3 angularVelocity)
    {
        // See [Documentation/imu_simulation.md] for elaboration of noise calculations
        const float angular_velocity_mean = 0.0f;
        float angular_velocity_std_dev = gyroscopeNoiseDensity * System.MathF.Sqrt(gyroscopeSampleRate);
        angularVelocity += noiseVector(angular_velocity_mean, angular_velocity_std_dev);
        return angularVelocity;
    }

    private Vector3 addAccelerationNoise(Vector3 acceleration)
    {
        // See [Documentation/imu_simulation.md] for elaboration of noise calculations
        const float linear_acceleration_mean = 0.0f;
        float linear_acceleration_std_dev = (accelerometerNoiseDensity * 0.001f) * System.MathF.Sqrt(Time.fixedDeltaTime); 
        acceleration += noiseVector(linear_acceleration_mean, linear_acceleration_std_dev);
        return acceleration;
    }

    private void sendImuMsg(Quaternion orientation, Vector3 angularVelocity, Vector3 acceleration)
    {
        ImuMsg imuMessage = new ImuMsg();
        imuMessage.header = new HeaderMsg();
        imuMessage.header.frame_id = frameId;
        imuMessage.header.stamp = get_current_time();

        // Absolute orientation is in the ENU frame as per REP 103
        imuMessage.orientation = orientation.To<ENU>();

        // Flip angular velocity due to conversion from left hand (Unity) to right hand (ROS) conventions
        imuMessage.angular_velocity = -angularVelocity.To<FLU>();
        imuMessage.linear_acceleration = acceleration.To<FLU>();

        imuMessage.orientation_covariance = getCovarianceMatrix(orientationVariance);
        imuMessage.angular_velocity_covariance = getCovarianceMatrix(angularVelocityVariance);
        imuMessage.linear_acceleration_covariance = getCovarianceMatrix(linearAccelerationVariance);
    
        ROSConnection.GetOrCreateInstance().Publish(topicName, imuMessage);
    }

    /// <returns>A diagonal covariance matrix with the given scalar value</returns>
    private static double[] getCovarianceMatrix(double epsilon)
    {
        double[] covariance = 
        {
            epsilon, 0, 0,
            0, epsilon, 0,
            0, 0, epsilon
        };
        return covariance;
    }

    private static Vector3 noiseVector(float mean = 0.0f, float std_dev = 1.0f)
    {
        return new Vector3(
            nextGaussian(mean, std_dev),
            nextGaussian(mean, std_dev),
            nextGaussian(mean, std_dev)
        );
    }

    public static float nextGaussian(float mean = 0.0f, float std_dev = 1.0f)
    {
        float u1 = Random.Range(0.0f, 1.0f); // Uniform(0,1] random doubles
        float u2 = Random.Range(0.0f, 1.0f);
        float std_normal = System.MathF.Sqrt(-2.0f * System.MathF.Log(u1)) * System.MathF.Sin(2.0f * System.MathF.PI * u2); // Random normal(0,1)
        return mean + std_dev * std_normal;
    }

    public static TimeMsg get_current_time()
    {
        const float second_to_nano_second = 1e9f;

        TimeMsg current_time_msg = new TimeMsg();
        float current_time = Time.time;
        current_time_msg.sec = Mathf.FloorToInt(current_time);
        current_time_msg.nanosec = (uint) (second_to_nano_second * (current_time - Mathf.FloorToInt(current_time)));

        return current_time_msg;
    }

}
