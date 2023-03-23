
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class HandController : UdonSharpBehaviour
{
    [Header("Info")]
    [Tooltip("Is the player currently trying to grab")]
    public bool Grabbing = false;
    [Tooltip("The currently grabbed game object")]
    public GameObject GrabbedObject = null;
    [Tooltip("The parent grabbed object")]
    public GameObject GrabbedParent = null;
    [Tooltip("The parent local grab point position")]
    public Vector3 GrabbedPoint;
    [Tooltip("Is the currently grabbed object a handle")]
    public bool GrabbedHandle = false;
    [Header("Scripts")]
    public PIDController PIDPosition;
    public PIDController PIDRotation1;
    public PIDController PIDRotation2;
    [Header("Settings")]
    public bool LeftHand = false;
    public float GrabRadius = 0.2f;
    public LayerMask GrabLayerMask;
    [Header("Debug")]
    [Tooltip("Assign an empty game object for local debug testing")]
    public GameObject DebugGameObject = null;
    public GameObject m_DemoSphere = null;
    [HideInInspector]
    public VRCPlayerApi Player;
    [HideInInspector]
    public Vector3 HandPosition;
    public Quaternion HandRotation;

    void Start()
    {
        Player = Networking.LocalPlayer;
        HandPosition = Player.GetBonePosition(LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        HandRotation = Player.GetBoneRotation(LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
    }

    void FixedUpdate()
    {
        HandPosition = Player.GetBonePosition(LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        HandRotation = Player.GetBoneRotation(LeftHand ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        if (m_DemoSphere != null)
        {
            m_DemoSphere.transform.position = GetHandPosition();
        }
        var color = Color.black;
        color.a = 0.2f;
        if (!Grabbing)
        {
            color.b = 1;
            GrabbedObject = null;
            return;
        }
        if (!TryGrab())
        {
            color.r = 1;
            m_DemoSphere.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
            return;
        }
        color.g = 1;
        m_DemoSphere.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
        ApplyGrab();
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (LeftHand && HandType.LEFT == args.handType)
        {
            Grabbing = value;
        }
        else if (!LeftHand && HandType.RIGHT == args.handType)
        {
            Grabbing = value;
        }
    }

    void ApplyGrab()
    {
        if (GrabbedObject == null)
        {
            return;
        }
        // TODO other logic for general purpose grabbing instead of just handles

        if (GrabbedHandle)
        {
            ApplyHandleGrab();
        }
    }

    void ApplyHandleGrab()
    {
        var rb = GrabbedParent.GetComponent<Rigidbody>();
        var point = GrabbedParent.transform.TransformPoint(GrabbedPoint);
        var handPos = GetHandPosition();
        var handRot = GetHandRotation();


        // position
        // remove all velocity
        rb.AddForce(-rb.velocity, ForceMode.VelocityChange);
        rb.AddForce(-Physics.gravity, ForceMode.Acceleration);
        rb.AddForce(PIDPosition.CorrectionV3(handPos, point, Time.deltaTime), ForceMode.Acceleration);

        // rotation
        rb.MoveRotation(handRot);
        rb.AddTorque(-rb.angularVelocity);
    }

    Vector3 GetHandPosition()
    {
        if (DebugGameObject != null)
        {
            return DebugGameObject.transform.position;
        }
        return HandPosition;
    }

    Quaternion GetHandRotation()
    {
        if (DebugGameObject != null)
        {
            return DebugGameObject.transform.rotation;
        }
        return HandRotation;
    }

    bool TryGrab()
    {
        Player.PlayHapticEventInHand(LeftHand ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right, 0.1f, 0.05f, 0.1f);
        debugDrawVector(Vector3.up, GetHandPosition(), Color.red, GrabRadius);
        debugDrawVector(-Vector3.up, GetHandPosition(), Color.red, GrabRadius);
        debugDrawVector(Vector3.right, GetHandPosition(), Color.green, GrabRadius);
        debugDrawVector(-Vector3.right, GetHandPosition(), Color.green, GrabRadius);
        debugDrawVector(Vector3.forward, GetHandPosition(), Color.blue, GrabRadius);
        debugDrawVector(-Vector3.forward, GetHandPosition(), Color.blue, GrabRadius);

        Collider[] colliders = Physics.OverlapSphere(GetHandPosition(), GrabRadius, GrabLayerMask, QueryTriggerInteraction.UseGlobal);
        if (colliders.Length == 0)
        {
            GrabbedObject = null;
            return false;
        }
        foreach (var collider in colliders)
        {
            var Grabbable = collider.gameObject.GetComponentInParent<Grabbable>();
            if (Grabbable == null)
            {
                GrabbedParent = null;
                GrabbedObject = null;
                continue;
            }
            GrabbedParent = Grabbable.gameObject;
            GrabbedObject = collider.gameObject;
            GrabbedHandle = Grabbable.IsHandle(GrabbedObject);
            GrabbedPoint = GrabbedParent.transform.InverseTransformPoint(GrabbedHandle ? GrabbedObject.transform.position : transform.position);
            return true;
        }
        return false;
    }

    private void debugDrawVector(Vector3 vector, Vector3 pos, Color color, float len = 0.2f)
    {
        Debug.DrawLine(pos, pos + vector, color);
    }
}
