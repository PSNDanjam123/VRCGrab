
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PIDController : UdonSharpBehaviour
{
    public float P = 1.0f;
    public float I = 0.0f;
    public float D = 0.1f;

    [HideInInspector]
    public Vector3 integralV3;
    [HideInInspector]
    public Vector3 lastErrorV3;

    public void SetPID(float newP, float newI, float newD)
    {
        P = newP;
        I = newI;
        D = newD;
    }

    public Vector3 CorrectionV3(Vector3 expected, Vector3 current, float timeFrame)
    {
        var error = (current - expected) * -1;
        integralV3 += error * timeFrame;
        var deriv = (error - lastErrorV3) / timeFrame;
        lastErrorV3 = error;
        return error * P + integralV3 * I + deriv * D;
    }

}
