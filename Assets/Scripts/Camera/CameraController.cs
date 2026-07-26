using UnityEngine;
using ConductorSymphony.Player;

namespace ConductorSymphony.CameraControl
{
    public class CameraController : MonoBehaviour
    {
        [Header("Target Tracking")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);

        private void Start()
        {
            if (target == null && PlayerController.Instance != null)
            {
                target = PlayerController.Instance.transform;
            }
        }

        private void LateUpdate()
        {
            if (target == null) return;

            // Instantly lock camera to player position so player is strictly centered on screen
            transform.position = target.position + offset;
        }
    }
}
