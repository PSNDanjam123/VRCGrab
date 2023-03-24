
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class Demo : UdonSharpBehaviour
{
    public GameObject m_Object1;
    public GameObject m_Object2;
    public PIDController m_pidController1;
    public PIDController m_pidController2;
    public GameObject m_Visual;

    public bool Execute = false;


    void OnStart()
    {
    }

    void Update()
    {
        DrawXYZ(m_Object1);
        DrawXYZ(m_Object2);
        m_Object1.GetComponent<Rigidbody>().maxAngularVelocity = 20f;
    }

    void FixedUpdate()
    {
        var correction = GetCorrection(m_pidController1, m_Object1.transform.rotation, m_Object2.transform.rotation, Vector3.forward, Color.red);
        correction += GetCorrection(m_pidController2, m_Object1.transform.rotation, m_Object2.transform.rotation, Vector3.up, Color.green);
        VisualizeCorrection(correction);
        if (Execute)
        {
            PerformCorrection(correction);
        }
    }

    void VisualizeCorrection(Vector3 correction)
    {
        var rb = m_Object1.GetComponent<Rigidbody>();
        Quaternion q = m_Object1.transform.rotation * rb.inertiaTensorRotation;
        var T = q * Vector3.Scale(rb.inertiaTensor, (Quaternion.Inverse(q) * correction));
        DrawLineFrom(m_Visual.transform.position, T, Color.magenta, T.magnitude * 0.01f);
    }

    void PerformCorrection(Vector3 correction)
    {
        var rb = m_Object1.GetComponent<Rigidbody>();

        Quaternion q = m_Object1.transform.rotation * rb.inertiaTensorRotation;
        var T = q * Vector3.Scale(rb.inertiaTensor, (Quaternion.Inverse(q) * correction));
        rb.AddTorque(T, ForceMode.Impulse);
    }

    Vector3 GetCorrection(PIDController pidController, Quaternion rot1, Quaternion rot2, Vector3 direction, Color color)
    {
        var pos = m_Visual.transform.position;
        var currentForward = rot1 * direction;
        var expectedForward = rot2 * direction;
        DrawLineFrom(pos, currentForward, color);
        DrawLineFrom(pos, expectedForward, color);

        var torqueVector = Vector3.Cross(currentForward.normalized, expectedForward.normalized);
        float theta = Mathf.Asin(torqueVector.magnitude);
        var pidOutput = pidController.CorrectionFloat(0.0f, theta, Time.fixedDeltaTime);
        Vector3 result = torqueVector.normalized * pidOutput;

        result *= pidOutput;

        var T = rot1 * (Quaternion.Inverse(rot1) * result);

        return T;
    }

    void DrawXYZ(GameObject gameObject, float len = 0.2f)
    {
        var trans = gameObject.transform;
        var pos = trans.position;
        DrawLineFrom(pos, trans.forward, Color.red);
        DrawLineFrom(pos, trans.up, Color.green);
        DrawLineFrom(pos, trans.right, Color.blue);
    }

    void DrawLineFrom(Vector3 pos, Vector3 direction, Color color, float len = 0.2f)
    {
        Debug.DrawLine(pos, pos + direction.normalized * len, color);
    }

}
