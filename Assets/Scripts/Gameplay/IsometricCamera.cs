using UnityEngine;

namespace PuebloLoco.Gameplay
{
    public class IsometricCamera : MonoBehaviour
    {
        [Header("Config")]
        public float panSpeed = 2.5f;
        public float zoomSpeed = 2f;
        public float minZoom = 4f;
        public float maxZoom = 15f;
        public float angleX = 50f;
        public float angleY = 45f;

        Camera cam;
        Vector3 target;
        float distance = 8f;
        float pinchPrev;

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            // La grilla está centrada en el origen; arrancar en (3,0,3) dejaba
            // la cámara mirando la esquina del mapa.
            target = Vector3.zero;
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
            // Normalizar: sin esto, dos teclas en diagonal mueven sqrt(2) más rápido.
            if (dir.sqrMagnitude > 0f) dir.Normalize();
            target += dir * panSpeed * Time.deltaTime;
            ClampTarget();

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
                target += (-right * delta.x + fwd * delta.y) * panSpeed * 0.02f;
            }

            // Touch zoom (2 dedos): hay que guardar la distancia del frame anterior.
            // Calcular "prev" con las posiciones actuales y sus deltas actuales
            // produce un valor que nunca cambia de verdad.
            if (Input.touchCount == 2)
            {
                float currDist = (Input.GetTouch(0).position - Input.GetTouch(1).position).magnitude;
                if (pinchPrev > 0f)
                    distance = Mathf.Clamp(distance - (currDist - pinchPrev) * 0.01f * zoomSpeed, minZoom, maxZoom);
                pinchPrev = currDist;
            }
            else
            {
                pinchPrev = 0f;
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
            ClampTarget();
        }

        public void SetDistance(float d)
        {
            distance = Mathf.Clamp(d, minZoom, maxZoom);
        }

        /// <summary>Impide dejar la cámara fuera del terreno.</summary>
        void ClampTarget()
        {
            var gm = Gameplay.GameManager.Instance;
            if (gm == null || gm.State == null) return;
            int size = Core.Catalog.LAND_TIERS[gm.State.landTier].size;
            float limit = size * 0.5f + 1.5f;
            target.x = Mathf.Clamp(target.x, -limit, limit);
            target.z = Mathf.Clamp(target.z, -limit, limit);
        }
    }
}
