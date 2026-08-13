using UnityEngine;

// BEHAVIOR CHANGE (Phase 3): this used to set virtualCam.Follow/LookAt directly
// on every scene load, racing CameraLead's own Follow-target assignment with no
// defined ordering between them. CameraLead + CameraTargetBinder are now the
// sole owners of vcam.Follow. Left in place (rather than deleted) as a marker in
// case boss-arena-specific vcam framing (this used CameraDistance 12 vs 8) is
// deliberately reintroduced later through CameraTargetBinder/CameraLead instead.
public class SceneTransitionHandler : MonoBehaviour
{
}