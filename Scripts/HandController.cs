
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
    [Header("Scripts")]
    public PIDController AngularUpPIDController;
    public PIDController AngularRightPIDController;
    public PIDController AngularForwardPIDController;
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
        GrabLayerMask = LayerMask.GetMask("GunHandle");
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
        GrabbedObject = TryGrab();
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
        ApplyHandleGrab();
    }

    void ApplyHandleGrab()
    {
        var handleTransform = GrabbedObject.transform;
        var rb = GrabbedObject.transform.root.GetComponent<Rigidbody>();
        var offset = handleTransform.position - rb.transform.position;

        // position
        rb.MovePosition(GetHandPosition() - offset);
        rb.AddForce(-Physics.gravity * rb.mass, ForceMode.Force);

        // rotation
        var expFor = GetHandRotation() * Vector3.forward;
        var curFor = rb.transform.forward;

        var expUp = Vector3.Cross(Vector3.Cross(expFor, Vector3.up), Vector3.up);
        var curUp = Vector3.Cross(Vector3.Cross(curFor, Vector3.up), Vector3.up);
        var expRight = Vector3.Cross(Vector3.Cross(expFor, Vector3.right), Vector3.right);
        var curRight = Vector3.Cross(Vector3.Cross(curFor, Vector3.right), Vector3.right);
        var expForward = Vector3.Cross(Vector3.Cross(expFor, Vector3.forward), Vector3.forward);
        var curForward = Vector3.Cross(Vector3.Cross(curFor, Vector3.forward), Vector3.forward);

        CorrectHandleAngle(AngularUpPIDController, rb, expUp, curUp);
        CorrectHandleAngle(AngularRightPIDController, rb, expRight, curRight);
        CorrectHandleAngle(AngularForwardPIDController, rb, expForward, curForward);
        rb.AddTorque(-rb.angularVelocity);
    }

    private void CorrectHandleAngle(PIDController PIDController, Rigidbody rb, Vector3 current, Vector3 expected)
    {
        Vector3 correction = PIDController.CorrectionV3(expected, current, Time.deltaTime);
        correction = Vector3.Cross(current, expected) * correction.magnitude;
        rb.AddTorque(correction);
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

    GameObject TryGrab()
    {
        Collider[] colliders = Physics.OverlapSphere(GetHandPosition(), GrabRadius, GrabLayerMask, QueryTriggerInteraction.UseGlobal);
        if (colliders.Length == 0)
        {
            return null;
        }
        foreach (var collider in colliders)
        {
            return collider.gameObject;
        }
        return null;
    }
}
