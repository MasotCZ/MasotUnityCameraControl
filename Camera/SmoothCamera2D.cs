using UnityEngine;

namespace Masot.CameraControl
{
    [RequireComponent(typeof(Camera))]
    public class SmoothCamera2D : MonoBehaviour
    {
        public float smoothTime = 0.15f;
        public Transform target;
        private Vector3 velocity = Vector3.zero;
        private Camera mainCamera;

        private void Start()
        {
            mainCamera = GetComponent<Camera>();
        }

        // Update is called once per frame
        private void FixedUpdate()
        {
            if (target)
            {
                //Vector3 point = mainCamera.WorldToViewportPoint(target.position);
                //Vector3 delta = target.position - mainCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z)); //(new Vector3(0.5, 0.5, point.z));
                //Vector3 destination = transform.position + delta;
                //transform.position = Vector3.SmoothDamp(transform.position, destination, ref velocity, dampTime);

                transform.position = Vector3.SmoothDamp(transform.position, new Vector3(target.position.x, target.position.y, mainCamera.transform.position.z), ref velocity, smoothTime);
            }
        }
    }
}