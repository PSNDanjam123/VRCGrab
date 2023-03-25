
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Demo : UdonSharpBehaviour
{
    [SerializeField] Vector3 Test1;
    [SerializeField] Vector3 Test2;

    private float time = 0;

    void FixedUpdate()
    {
        // inputs
        Quaternion obj1 = Quaternion.Euler(Test1);
        Quaternion obj2 = Quaternion.Euler(Test2);

        // rotate the correction to be the same direction as object two
        var correction = Quaternion.RotateTowards(obj1, obj2, Time.fixedDeltaTime);

        correction = obj1 * correction;


        drawQuaternion(new Vector3(-1.2f, 0, 0), obj1);
        drawQuaternion(new Vector3(1.2f, 0, 0), obj2);
        drawQuaternion(new Vector3(2.2f, 0, 0), correction);

        // animate the response
        time += Time.fixedDeltaTime;
        var speed = (time * 0.2f) % 1;
        Quaternion anim = Quaternion.Lerp(obj1, correction, speed);
        drawQuaternion(new Vector3(2.2f, 0, 0), anim);
    }

    private void drawQuaternion(Vector3 pos, Quaternion q)
    {
        drawLine(pos, q * Vector3.forward, Color.red);
        drawLine(pos, q * Vector3.up, Color.green);
        drawLine(pos, q * Vector3.right, Color.blue);
    }

    private void drawTorque(Vector3 pos, Vector3 a, Vector3 b)
    {
        var t = Vector3.Cross(a.normalized, b.normalized).normalized;
        Debug.DrawLine(pos, t, Color.magenta);
    }

    private void drawLine(Vector3 pos, Vector3 direction, Color color)
    {
        pos = transform.position + pos;
        Debug.DrawLine(pos, pos + direction, color);
    }
}
