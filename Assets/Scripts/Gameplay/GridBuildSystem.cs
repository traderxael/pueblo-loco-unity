using System.Collections.Generic;
using UnityEngine;
using PuebloLoco.Core;

namespace PuebloLoco.Gameplay
{
    public class GridBuildSystem : MonoBehaviour
    {
        public static GridBuildSystem Instance { get; private set; }

        [Header("Prefabs (se cargan de Resources si están vacíos)")]
        public GameObject grassTilePrefab;
        public GameObject pathTilePrefab;
        public GameObject treePrefab;
        public Dictionary<string, GameObject> buildingPrefabs = new Dictionary<string, GameObject>();

        const float TILE = 1f;
        Transform tileRoot;
        Transform buildingRoot;
        readonly Dictionary<string, GameObject> placedViews = new Dictionary<string, GameObject>();

        // modo colocación
        string placingDef;
        GameObject ghost;
        bool placing;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            tileRoot = new GameObject("Tiles").transform;
            buildingRoot = new GameObject("Buildings").transform;
            LoadPrefabs();
            GameManager.Instance.OnStateChanged += RebuildAll;
            RebuildAll();
        }

        void LoadPrefabs()
        {
            if (grassTilePrefab == null) grassTilePrefab = Resources.Load<GameObject>("GrassTile");
            if (pathTilePrefab == null) pathTilePrefab = Resources.Load<GameObject>("PathTile");
            if (treePrefab == null) treePrefab = Resources.Load<GameObject>("Tree");

            // OJO: los FBX en Resources empiezan con mayúscula (Casa.fbx).
            var map = new Dictionary<string, string>
            {
                { "casa", "Casa" },
                { "kiosco", "Kiosco" },
                { "panaderia", "Panaderia" },
            };
            foreach (var kv in map)
            {
                var prefab = Resources.Load<GameObject>(kv.Value);
                if (prefab != null) buildingPrefabs[kv.Key] = prefab;
                else Debug.LogWarning("[BUILDING] falta Resources/" + kv.Value + ".fbx");
            }
        }

        public Vector3 GridToWorld(int x, int y)
        {
            var s = GameManager.Instance.State;
            int size = Catalog.LAND_TIERS[s.landTier].size;
            float offset = -size * TILE * 0.5f;
            return new Vector3(offset + (x + 0.5f) * TILE, 0, offset + (y + 0.5f) * TILE);
        }

        public bool WorldToGrid(Vector3 world, out int gx, out int gy)
        {
            var s = GameManager.Instance.State;
            int size = Catalog.LAND_TIERS[s.landTier].size;
            float offset = -size * TILE * 0.5f;
            gx = Mathf.FloorToInt((world.x - offset) / TILE);
            gy = Mathf.FloorToInt((world.z - offset) / TILE);
            return gx >= 0 && gy >= 0 && gx < size && gy < size;
        }

        public void RebuildAll()
        {
            foreach (Transform t in tileRoot) Destroy(t.gameObject);
            foreach (Transform t in buildingRoot) Destroy(t.gameObject);
            placedViews.Clear();

            var s = GameManager.Instance.State;
            int size = Catalog.LAND_TIERS[s.landTier].size;

            // tiles
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                {
                    var prefab = grassTilePrefab;
                    if (prefab == null) continue;
                    // OJO: conservar la rotación del prefab (los FBX vienen con -90° en X)
                    var go = Instantiate(prefab, tileRoot);
                    go.transform.localPosition = GridToWorld(x, y);
                    go.name = $"Tile_{x}_{y}";
                }

            // árboles decorativos en bordes
            if (treePrefab != null)
            {
                for (int i = 0; i < size; i += 2)
                {
                    SpawnTree(-1, i); SpawnTree(size, i);
                    SpawnTree(i, -1); SpawnTree(i, size);
                }
            }

            // edificios
            foreach (var b in s.buildings)
            {
                var def = Catalog.Building(b.def);
                GameObject prefab = buildingPrefabs.TryGetValue(b.def, out var p) ? p : CreateFallback(def);
                var go = Instantiate(prefab, buildingRoot);
                go.transform.localPosition = GridToWorld(b.x, b.y);
                go.name = $"Building_{b.id}";
                go.transform.localScale = Vector3.one * def.size;
                TintBuilding(go, def);
                var view = go.AddComponent<BuildingView>();
                view.buildingId = b.id;
                view.def = def;
                placedViews[b.id] = go;
            }

            // adornos (fallback cubes)
            foreach (var d in s.decor)
            {
                var def = Catalog.Decor(d.def);
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.transform.position = GridToWorld(d.x, d.y) + Vector3.up * 0.25f;
                go.transform.localScale = new Vector3(0.3f, 0.25f, 0.3f);
                go.transform.SetParent(buildingRoot);
                ColorUtility.TryParseHtmlString(def.color, out var c);
                go.GetComponent<Renderer>().material.color = c;
                go.name = $"Decor_{d.id}";
            }
        }

        void SpawnTree(int x, int y)
        {
            if (treePrefab == null) return;
            var go = Instantiate(treePrefab, tileRoot);
            go.transform.localPosition = GridToWorld(x, y);
            go.transform.localScale = Vector3.one * 0.8f;
        }

        /// <summary>Pinta el edificio por nombre de material (pared, techo, puerta, ventana).</summary>
        void TintBuilding(GameObject go, BuildingDef def)
        {
            Color wall, roof;
            if (!ColorUtility.TryParseHtmlString(def.color, out wall)) wall = Color.white;
            if (!ColorUtility.TryParseHtmlString(def.roof, out roof)) roof = Color.gray;
            Color door = Color.Lerp(roof, Color.black, 0.55f);
            Color window = new Color(0.62f, 0.86f, 0.95f);

            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                    Color c;
                    if (n.Contains("window")) c = window;
                    else if (n.Contains("door")) c = door;
                    else if (n.Contains("roof") || n.Contains("awning") || n.Contains("pole") || n.Contains("counter")) c = roof;
                    else c = wall;

                    var m = new Material(Shader.Find("Standard"));
                    m.name = mats[i] != null ? mats[i].name : "bmat";
                    m.color = c;
                    m.SetFloat("_Glossiness", 0.12f);
                    mats[i] = m;
                }
                r.sharedMaterials = mats;
            }
        }

        GameObject CreateFallback(BuildingDef def)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.localScale = new Vector3(0.8f, 1.2f, 0.8f);
            ColorUtility.TryParseHtmlString(def.color, out var c);
            go.GetComponent<Renderer>().material.color = c;
            return go;
        }

        /* ------------------------------------------------ modo colocación */

        public void BeginPlacing(string defId)
        {
            placingDef = defId;
            placing = true;
            if (ghost != null) Destroy(ghost);
            var def = Catalog.Building(defId);
            GameObject prefab = buildingPrefabs.TryGetValue(defId, out var p) ? p : CreateFallback(def);
            ghost = Instantiate(prefab);
            ghost.transform.localScale = Vector3.one * def.size;
            SetGhostAlpha(ghost, 0.5f);
        }

        public void CancelPlacing()
        {
            placing = false;
            placingDef = null;
            if (ghost != null) { Destroy(ghost); ghost = null; }
        }

        void Update()
        {
            if (!placing || ghost == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 100f))
            {
                if (WorldToGrid(hit.point, out int gx, out int gy))
                {
                    ghost.transform.position = GridToWorld(gx, gy);
                    var check = Economy.CanPlaceBuilding(GameManager.Instance.State, placingDef, gx, gy);
                    SetGhostColor(ghost, check.ok ? Color.green : Color.red);
                    if (Input.GetMouseButtonDown(0) && check.ok)
                    {
                        var r = GameManager.Instance.PlaceBuilding(placingDef, gx, gy);
                        if (r.ok) CancelPlacing();
                    }
                }
            }
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
                CancelPlacing();
        }

        void SetGhostAlpha(GameObject go, float a)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var m = r.material;
                m.SetFloat("_Mode", 3);
                var c = m.color; c.a = a; m.color = c;
            }
        }

        void SetGhostColor(GameObject go, Color tint)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                r.material.color = Color.Lerp(r.material.color, tint, 0.3f);
        }
    }
}
