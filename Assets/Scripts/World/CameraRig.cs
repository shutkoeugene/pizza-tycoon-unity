using UnityEngine;
using UnityEngine.EventSystems;

namespace PizzaTycoon
{
    public class CameraRig : MonoBehaviour
    {
        public Vector3 focus;
        public float yaw = 30f, pitch = 45f, distance = 70f;
        Vector3 targetFocus; float targetDist, targetYaw;
        Camera cam;
        Vector3 min = new Vector3(-30, 0, -30), max = new Vector3(120, 0, 120);

        public void Init(Vector3 center, Vector3 areaMin, Vector3 areaMax)
        {
            cam = GetComponent<Camera>();
            focus = targetFocus = center;
            targetDist = distance; targetYaw = yaw;
            min = areaMin; max = areaMax;
            Apply(1f);
        }

        public void FocusOn(Vector3 p, float dist = 38f) { targetFocus = p; targetDist = dist; }

        void Update()
        {
            if (cam == null) return;
            bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            float speed = targetDist * 0.9f * Time.unscaledDeltaTime;
            Vector3 fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward;
            Vector3 right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
            Vector3 mv = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) mv += fwd;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) mv -= fwd;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) mv += right;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) mv -= right;
            targetFocus += mv * speed;

            if (Input.GetKey(KeyCode.Q)) targetYaw -= 70f * Time.unscaledDeltaTime;
            if (Input.GetKey(KeyCode.E)) targetYaw += 70f * Time.unscaledDeltaTime;

            if (!overUI)
            {
                float sc = Input.GetAxis("Mouse ScrollWheel");
                targetDist = Mathf.Clamp(targetDist * (1f - sc * 1.5f), 14f, 140f);
                if (Input.GetMouseButton(1) || Input.GetMouseButton(2))
                {
                    float mx = Input.GetAxis("Mouse X"), my = Input.GetAxis("Mouse Y");
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetMouseButton(2))
                        targetFocus -= (right * mx + fwd * my) * targetDist * 0.05f;
                    else { targetYaw += mx * 4f; pitch = Mathf.Clamp(pitch - my * 3f, 20f, 80f); }
                }
            }
            targetFocus.x = Mathf.Clamp(targetFocus.x, min.x, max.x);
            targetFocus.z = Mathf.Clamp(targetFocus.z, min.z, max.z);
            Apply(1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }

        void Apply(float k)
        {
            focus = Vector3.Lerp(focus, targetFocus, k);
            distance = Mathf.Lerp(distance, targetDist, k);
            yaw = Mathf.LerpAngle(yaw, targetYaw, k);
            var rot = Quaternion.Euler(pitch, yaw, 0);
            transform.position = focus - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }
    }
}
