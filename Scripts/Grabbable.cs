
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class Grabbable : UdonSharpBehaviour
{
    public GameObject[] m_handles;
    void Start()
    {
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
