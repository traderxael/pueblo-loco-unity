using System.Collections.Generic;
using UnityEngine;
using PuebloLoco.Core;

namespace PuebloLoco.Gameplay
{
    /// <summary>
    /// Controla a un vecino: camina por el pueblo, trabaja cuando tiene una
    /// tarea activa y saluda si lo tocás. Toda la animación pasa por el
    /// Animator (Speed / Working / Wave).
    /// </summary>
    public class VillagerAI : MonoBehaviour
    {
        public string defId;
        public CharacterDef def;
        public Animator anim;
        public float moveSpeed = 1.15f;

        enum Mode { Idle, Walk, Work }

        Mode mode = Mode.Idle;
        Vector3 target;
        float idleTimer;
        bool working;
        bool hasWorkSpot;
        Vector3 workSpot;
        float animSpeed;
        static readonly HashSet<Vector2Int> occupied = new HashSet<Vector2Int>();
        Vector3 lastPosition;
        float stuckTimer;

        public void Setup(string id, Animator a)
        {
            defId = id;
            def = Catalog.Character(id);
            anim = a;
            lastPosition = transform.position;
            PickWanderTarget();
            idleTimer = Random.Range(1.5f, 4.5f);
            if (anim != null) anim.SetBool("Working", false);
        }

        public void PlayWave()
        {
            if (anim != null) anim.SetTrigger("Wave");
        }

        public static void RefreshOccupancy()
        {
            occupied.Clear();
            var gm = GameManager.Instance;
            if (gm == null) return;
            foreach (var b in gm.State.buildings)
            {
                var d = Catalog.Building(b.def);
                int size = d != null ? Mathf.Max(1, d.size) : 1;
                for (int dx = 0; dx < size; dx++)
                    for (int dy = 0; dy < size; dy++)
                        occupied.Add(new Vector2Int(b.x + dx, b.y + dy));
            }
            foreach (var d in gm.State.decor) occupied.Add(new Vector2Int(d.x, d.y));
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var entry = gm.State.characters.Find(c => c.def == defId);
            if (entry == null || !entry.unlocked)
            {
                if (gameObject.activeSelf) gameObject.SetActive(false);
                return;
            }
            if (!gameObject.activeSelf) gameObject.SetActive(true);

            bool taskActive = entry.task != null && !entry.task.done;
            if (taskActive != working)
            {
                working = taskActive;
                hasWorkSpot = false;
                mode = Mode.Idle;
                idleTimer = Random.Range(0.3f, 1.0f);
                if (anim != null) anim.SetBool("Working", working);
            }

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            if (working)
            {
                if (!hasWorkSpot)
                {
                    workSpot = FindWorkSpot();
                    hasWorkSpot = true;
                }
                float d = Flat(workSpot - transform.position).magnitude;
                if (d > 0.22f) MoveTowards(workSpot, dt);
                else
                {
                    SetSpeed(0f);
                    LookAt(Camera.main != null ? Camera.main.transform.position : Vector3.forward, dt, 2f);
                }
            }
            else
            {
                switch (mode)
                {
                    case Mode.Idle:
                        SetSpeed(0f);
                        idleTimer -= dt;
                        if (idleTimer <= 0f)
                        {
                            PickWanderTarget();
                            mode = Mode.Walk;
                        }
                        break;
                    case Mode.Walk:
                        if (MoveTowards(target, dt))
                        {
                            mode = Mode.Idle;
                            idleTimer = Random.Range(2.5f, 6.5f);
                        }
                        break;
                }
            }

            UpdateAnimSpeed(dt);
        }

        void UpdateAnimSpeed(float dt)
        {
            if (anim == null) return;
            float measured = Flat(transform.position - lastPosition).magnitude / Mathf.Max(dt, 0.0001f);
            animSpeed = Mathf.Lerp(animSpeed, measured, 1f - Mathf.Exp(-10f * dt));
            anim.SetFloat("Speed", animSpeed);
            lastPosition = transform.position;
        }

        void SetSpeed(float s)
        {
            animSpeed = Mathf.Lerp(animSpeed, s, 0.25f);
            if (anim != null) anim.SetFloat("Speed", animSpeed);
        }

        bool MoveTowards(Vector3 dest, float dt)
        {
            Vector3 delta = Flat(dest - transform.position);
            if (delta.magnitude <= 0.08f) return true;

            Vector3 dir = delta.normalized;
            Vector3 next = transform.position + dir * moveSpeed * dt;

            // no meterse en edificios: si el paso cae en una celda ocupada, se rodea
            if (IsBlocked(next))
            {
                Vector3 side = Vector3.Cross(Vector3.up, dir) * (Random.value > 0.5f ? 1f : -1f);
                next = transform.position + (dir * 0.35f + side * 0.65f).normalized * moveSpeed * dt;
                if (IsBlocked(next)) return false;
            }

            next = ClampToLand(next);
            transform.position = next;
            LookAt(next - dir, dt, 8f);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir, Vector3.up), 1f - Mathf.Exp(-9f * dt));
            return false;
        }

        void LookAt(Vector3 worldPoint, float dt, float sharpness)
        {
            Vector3 dir = Flat(worldPoint - transform.position);
            if (dir.sqrMagnitude < 0.0004f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(dir.normalized, Vector3.up), 1f - Mathf.Exp(-sharpness * dt));
        }

        bool IsBlocked(Vector3 world)
        {
            var grid = GridBuildSystem.Instance;
            if (grid == null) return false;
            int gx, gy;
            if (!grid.WorldToGrid(world, out gx, out gy)) return true;
            return occupied.Contains(new Vector2Int(gx, gy));
        }

        Vector3 ClampToLand(Vector3 p)
        {
            var gm = GameManager.Instance;
            if (gm == null) return p;
            int size = Catalog.LAND_TIERS[gm.State.landTier].size;
            float half = size * 0.5f - 0.65f;
            p.x = Mathf.Clamp(p.x, -half, half);
            p.z = Mathf.Clamp(p.z, -half, half);
            return p;
        }

        void PickWanderTarget()
        {
            var gm = GameManager.Instance;
            var grid = GridBuildSystem.Instance;
            if (gm == null || grid == null) { target = transform.position; return; }

            int size = Catalog.LAND_TIERS[gm.State.landTier].size;
            for (int i = 0; i < 14; i++)
            {
                int x = Random.Range(0, size);
                int y = Random.Range(0, size);
                if (occupied.Contains(new Vector2Int(x, y))) continue;
                Vector3 w = grid.GridToWorld(x, y);
                w.x += Random.Range(-0.3f, 0.3f);
                w.z += Random.Range(-0.3f, 0.3f);
                target = w;
                return;
            }
            target = Vector3.zero;
        }

        Vector3 FindWorkSpot()
        {
            var gm = GameManager.Instance;
            if (gm == null) return transform.position;
            var buildings = gm.State.buildings;
            if (buildings.Count > 0)
            {
                var b = buildings[Random.Range(0, buildings.Count)];
                var grid = GridBuildSystem.Instance;
                if (grid != null)
                {
                    Vector3 p = grid.GridToWorld(b.x, b.y);
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    if (dir.sqrMagnitude < 0.1f) dir = Vector2.right;
                    return ClampToLand(p + new Vector3(dir.x, 0f, dir.y) * 1.15f);
                }
            }
            return ClampToLand(new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)));
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
