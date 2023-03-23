
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
    public GameObject NearestGrabbableObject = null;
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
    public float GrabRadius = 0.1f;
    public LayerMask GrabLayerMask;
    public Vector3 HandleOffset = new Vector3(-38, 0, 0);

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
    }

    void FixedUpdate()
    {
        HandPosition = Player.GetTrackingData(LeftHand ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).position;
        HandRotation = Player.GetTrackingData(LeftHand ? VRCPlayerApi.TrackingDataType.LeftHand : VRCPlayerApi.TrackingDataType.RightHand).rotation;
        HandleGrabbing();
    }

    public void HandleGrabbing()
    {
        if (m_DemoSphere != null)
        {
            m_DemoSphere.transform.position = GetHandPosition();
        }

        // demo color config
        var color = Color.black;
        color.a = 0.2f;

        // check for nearest grabbable object
        var newGrabbableObject = GetNearestGrabbableObject();
        if (newGrabbableObject != NearestGrabbableObject)
        {
            InputManager.EnableObjectHighlight(NearestGrabbableObject, false);
            NearestGrabbableObject = newGrabbableObject;
        }

        // clean up if no longer grabbing
        if (!Grabbing)
        {
            color.b = 1;
            if (GrabbedObject != null)
            {
                InputManager.EnableObjectHighlight(GrabbedObject, false);
            }
            GrabbedObject = null;
            if (NearestGrabbableObject != null)
            {
                InputManager.EnableObjectHighlight(NearestGrabbableObject, true);
            }
            return;
        }

        // if grabbing an object already then apply grab
        if (GrabbedObject != null)
        {
            ApplyGrab();
            return;
        }

        // If no nearest object then nothing to do
        if (NearestGrabbableObject == null)
        {
            // turn debug to red if nothing to grab
            color.r = 1;
            color.g = 0;
            m_DemoSphere.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
            return;
        }

        // grab the nearest object
        var Grabbable = NearestGrabbableObject.GetComponentInParent<Grabbable>();
        GrabbedParent = Grabbable.gameObject;
        GrabbedObject = NearestGrabbableObject;
        GrabbedHandle = Grabbable.IsHandle(GrabbedObject);
        GrabbedPoint = GrabbedParent.transform.InverseTransformPoint(GrabbedHandle ? GrabbedObject.transform.position : transform.position);

        // turn debug to green on grab
        color.r = 0;
        color.g = 1;
        m_DemoSphere.GetComponent<MeshRenderer>().material.SetColor("_Color", color);
    }

    public override void InputUse(bool value, UdonInputEventArgs args)
    {
        if (!Player.IsUserInVR())
        {
            Grabbing = value;
        }
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
        var rot = HandRotation;
        // VR fix
        if (Player.IsUserInVR())
        {
            rot = Quaternion.LookRotation(rot * Vector3.right, rot * Vector3.forward);
            rot *= Quaternion.Euler(HandleOffset);
        }
        else
        {
            var headPosition = Player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;
            var headRotation = Player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;
            Vector3 headLook = headRotation * Vector3.forward;
            var lookPoint = headRotation * Vector3.forward * 1000;
            var vector = lookPoint - headPosition;
            rot = Quaternion.LookRotation(vector, headRotation * Vector3.up);
        }

        return rot;
    }

    GameObject GetNearestGrabbableObject()
    {
        Collider[] colliders = Physics.OverlapSphere(GetHandPosition(), GrabRadius, GrabLayerMask);
        if (colliders.Length == 0)
        {
            return null;
        }
        foreach (var collider in colliders)
        {
            var grabbable = collider.gameObject.GetComponentInParent<Grabbable>();
            if (grabbable == null)
            {
                continue;
            }
            return collider.gameObject;
        }
        return null;
    }

    private void debugDrawVector(Vector3 vector, Vector3 pos, Color color, float len = 0.2f)
    {
        Debug.DrawLine(pos, pos + vector, color);
    }
}
