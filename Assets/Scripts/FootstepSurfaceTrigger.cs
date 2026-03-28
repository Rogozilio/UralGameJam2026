using UnityEngine;

namespace Scripts
{
    [RequireComponent(typeof(Collider))]
    public class FootstepSurfaceTrigger : MonoBehaviour
    {
        [Header("Trigger Settings")]
        [SerializeField] private FootstepSurfaceType surfaceType = FootstepSurfaceType.Default;
        [SerializeField] private string targetTag = "Player";

        [Header("Exit Settings")]
        [SerializeField] private bool resetOnExit = true;
        [SerializeField] private FootstepSurfaceType exitSurfaceType = FootstepSurfaceType.Default;

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(targetTag))
                return;

            var footstepAudio = other.GetComponentInParent<FootstepAudio>();

            if (footstepAudio == null)
                return;

            footstepAudio.SetSurfaceType(surfaceType);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!resetOnExit || !other.CompareTag(targetTag))
                return;

            var footstepAudio = other.GetComponentInParent<FootstepAudio>();

            if (footstepAudio == null)
                return;

            footstepAudio.SetSurfaceType(exitSurfaceType);
        }
    }
}
