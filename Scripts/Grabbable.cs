
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Grabbable : UdonSharpBehaviour
{
    public float maxAngularVelocity = 1000;
    public GameObject[] m_handles;
    public Rigidbody m_rigidBody;
    void Start()
    {
        m_rigidBody = GetComponent<Rigidbody>();
        m_rigidBody.maxAngularVelocity = maxAngularVelocity;
    }

    public bool IsHandle(GameObject gameObject)
    {
        foreach (var handle in m_handles)
        {
            if (gameObject == handle)
            {
                return true;
            }
        }
        return false;
    }

}
