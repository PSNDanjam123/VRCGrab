
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
    [HideInInspector]
    public VRCPlayerApi Player;
    [HideInInspector]
    public VRCPlayerApi.TrackingData HandTrackingData;

    void Start()
    {
        Player = Networking.LocalPlayer;
        HandTrackingData = Player.GetTrackingData(LeftHand ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand);
    }

    void FixedUpdate()
    {
        if (!Grabbing)
        {
            GrabbedObject = null;
            return;
        }
        if (!TryGrab())
        {
            return;
        }
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
        var rotationChange = handRot * Quaternion.Inverse(rb.transform.rotation);

        rotationChange.ToAngleAxis(out float angle, out Vector3 axis);

        if (Mathf.Approximately(angle, 0))
        {
            return; // no need
        }

        angle *= Mathf.Deg2Rad;

        var target = axis * PIDRotation1.CorrectionFloat(0, angle, Time.deltaTime);
        rb.AddTorque(target, ForceMode.Force);
    }

    Vector3 GetHandPosition()
    {
        if (DebugGameObject != null)
        {
            return DebugGameObject.transform.position;
        }
        return HandTrackingData.position;
    }

    Quaternion GetHandRotation()
    {
        if (DebugGameObject != null)
        {
            return DebugGameObject.transform.rotation;
        }
        return HandTrackingData.rotation;
    }

    bool TryGrab()
    {
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
