using UnityEngine;

namespace PuebloLoco.Gameplay
{
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Config")]
        public float panSpeed = 0.05f;
        public float zoomSpeed = 2f;
        public float minZoom = 4f;
        public float maxZoom = 15f;
        public float angleX = 50f;
        public float angleY = 45f;

        Camera cam;
        Vector3 target;
        float distance = 8f;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            target = new Vector3(3, 0, 3);
            Apply();
        }

        void Update()
        {
            // Pan: WASD o flechas
            Vector3 dir = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) dir += new Vector3(1, 0, 1);
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) dir += new Vector3(-1, 0, -1);
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) dir += new Vector3(-1, 0, 1);
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) dir += new Vector3(1, 0, -1);
            target += dir * panSpeed * distance * Time.deltaTime * 60f;

            // Zoom: rueda del ratón
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
                distance = Mathf.Clamp(distance - scroll * zoomSpeed * 5f, minZoom, maxZoom);

            // Touch pan (móvil)
            if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
            {
                Vector2 delta = Input.GetTouch(0).deltaPosition;
                Vector3 right = transform.right, fwd = transform.forward;
                right.y = 0; fwd.y = 0;
                right.Normalize(); fwd.Normalize();
                target += (-right * delta.x + fwd * delta.y) * panSpeed * distance * 0.02f;
            }

            // Touch zoom (2 dedos)
            if (Input.touchCount == 2)
            {
                float prevDist = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude -
                                 (Input.GetTouch(0).deltaPosition - Input.GetTouch(1).deltaPosition).magnitude;
                float currDist = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
                distance = Mathf.Clamp(distance - (currDist - prevDist) * 0.01f * zoomSpeed, minZoom, maxZoom);
            }

            Apply();
        }

        void Apply()
        {
            Quaternion rot = Quaternion.Euler(angleX, angleY, 0);
            transform.position = target - rot * Vector3.forward * distance;
            transform.rotation = rot;
        }

        public void FocusOn(Vector3 worldPos)
        {
            target = worldPos;
        }

        public void SetDistance(float d)
        {
            distance = Mathf.Clamp(d, minZoom, maxZoom);
        }
    }
}
