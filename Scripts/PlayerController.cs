
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Common;

[UdonBehaviourSyncMode(BehaviourSyncMode.None)]
public class PlayerController : UdonSharpBehaviour
{
    public LayerMask HandTriggerLayerMask;
    public float HandTriggerRadius = 0.2f;
    [HideInInspector]
    public VRCPlayerApi Player;
    [HideInInspector]
    public VRCPlayerApi.TrackingData Head;
    [HideInInspector]
    public VRCPlayerApi.TrackingData LeftHand;
    [HideInInspector]
    public VRCPlayerApi.TrackingData RightHand;

    public bool LeftHandGrabbed = false;
    public bool RightHandGrabbed = false;

    public GameObject LeftGrabbedObject;
    public GameObject RightGrabbedObject;

    public GameObject DebugLeftHand;
    public GameObject DebugRightHand;

    [Header("Handle Angle PID")]
    public float pFactor = 33f;
    public float iFactor = 0;
    public float dFactor = 0.22f;

    private Vector3 angularCorrectionIntegral;
    private Vector3 angularCorrectionLastError;


    void Start()
    {
        HandTriggerLayerMask = LayerMask.GetMask("GunHandle");
        Player = Networking.LocalPlayer;
        Head = Player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
        LeftHand = Player.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);
        RightHand = Player.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);
    }

    void FixedUpdate()
    {
        // If player is trying to grab then check cast
        if (LeftHandGrabbed)
        {
            // get an object or null for the first in range that is valid
            var obj = CastHandRay(DebugLeftHand.transform.position);
            LeftGrabbedObject = obj;
        }
        else
        {
            LeftGrabbedObject = null;
        }
        if (LeftGrabbedObject == null)
        {
            return;
        }

        // Get the transform (pos/rot) of the object grabbed
        var trans = LeftGrabbedObject.transform;

        // Get the parents rigid body
        var rb = LeftGrabbedObject.transform.root.GetComponent<Rigidbody>();

        // Get the offset of the grabbed object and it's parent
        var offset = trans.position - rb.transform.position;

        // Move the parent so the grabbed part is where the hand is
        rb.MovePosition(DebugLeftHand.transform.position - offset);

        // Negate gravity
        rb.AddForce(-Physics.gravity * rb.mass, ForceMode.Force);

        // Try to correct angular velocity
        correctAngularVelocity(rb, rb.transform.forward, DebugLeftHand.transform.forward);
    }

    private void correctAngularVelocity(Rigidbody rb, Vector3 expected, Vector3 current)
    {
        var currentError = (current - expected) * -1;
        var timeFrame = Time.deltaTime;

        angularCorrectionIntegral += currentError * timeFrame;
        var deriv = (currentError - angularCorrectionLastError) / timeFrame;
        angularCorrectionLastError = currentError;
        var correction = currentError * pFactor + angularCorrectionIntegral * iFactor + deriv * dFactor;
        correction = Vector3.Cross(current, expected) * correction.magnitude;
        rb.AddTorque(correction);
        rb.AddTorque(-rb.angularVelocity);
    }

    public override void InputGrab(bool value, UdonInputEventArgs args)
    {
        if (HandType.LEFT == args.handType)
        {
            LeftHandGrabbed = value;
        }
        else
        {
            RightHandGrabbed = value;
        }
    }

    GameObject CastHandRay(Vector3 position)
    {
        Collider[] colliders = Physics.OverlapSphere(position, HandTriggerRadius, HandTriggerLayerMask, QueryTriggerInteraction.UseGlobal);
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
