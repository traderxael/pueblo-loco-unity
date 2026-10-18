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
        [System.NonSerialized] public Dictionary<string, GameObject> buildingPrefabs = new Dictionary<string, GameObject>();
        [System.NonSerialized] public Dictionary<string, GameObject> decorPrefabs = new Dictionary<string, GameObject>();

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
            GameManager.Instance.OnTownChanged += RebuildAll;
            RebuildAll();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null) GameManager.Instance.OnTownChanged -= RebuildAll;
            if (Instance == this) Instance = null;
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

            // edificios y adornos temáticos de la China antigua
            var chinaBuildings = new Dictionary<string, string>
            {
                { "casahan", "China/SM_Building_CasaHan" },
                { "pagoda", "China/SM_Building_Pagoda_05" },
            };
            foreach (var kv in chinaBuildings)
            {
                var prefab = Resources.Load<GameObject>(kv.Value);
                if (prefab != null) buildingPrefabs[kv.Key] = prefab;
                else Debug.LogWarning("[BUILDING] falta Resources/" + kv.Value + ".fbx");
            }

            var decorMap = new Dictionary<string, string>
            {
                { "farol", "China/SM_Prop_Lantern" },
                { "estatua", "China/SM_Prop_Ding" },
            };
            foreach (var kv in decorMap)
            {
                var prefab = Resources.Load<GameObject>(kv.Value);
                if (prefab != null) decorPrefabs[kv.Key] = prefab;
                else Debug.LogWarning("[DECOR] falta Resources/" + kv.Value + ".fbx");
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
                go.transform.localScale = Vector3.one * (def.size * VisualScale(def.def));
                TintBuilding(go, def);
                var view = go.AddComponent<BuildingView>();
                view.buildingId = b.id;
                view.def = def;
                placedViews[b.id] = go;
            }

            // adornos temáticos
            foreach (var d in s.decor)
            {
                var def = Catalog.Decor(d.def);
                Vector3 pos = GridToWorld(d.x, d.y);
                GameObject decor;
                if (decorPrefabs.TryGetValue(d.def, out var dp) && dp != null)
                {
                    decor = Instantiate(dp, buildingRoot);
                    decor.transform.localPosition = pos;
                    decor.transform.localScale = Vector3.one * (d.def == "farol" ? 0.62f : 0.8f);
                    decor.name = $"Decor_{d.id}";
                    ApplyPalette(decor, null, null);
                }
                else
                {
                    decor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    decor.transform.position = pos + Vector3.up * 0.25f;
                    decor.transform.localScale = new Vector3(0.3f, 0.25f, 0.3f);
                    decor.transform.SetParent(buildingRoot);
                    ColorUtility.TryParseHtmlString(def.color, out var c);
                    decor.GetComponent<Renderer>().material.color = c;
                    decor.name = $"Decor_{d.id}";
                }
            }
        }

        void SpawnTree(int x, int y)
        {
            if (treePrefab == null) return;
            var go = Instantiate(treePrefab, tileRoot);
            go.transform.localPosition = GridToWorld(x, y);
            go.transform.localScale = Vector3.one * 0.8f;
        }

        // ------------------------------------------------------------------
        // materiales: colores + texturas CC0 (Poly Haven) para China
        // ------------------------------------------------------------------
        static readonly Dictionary<string, Texture2D> texCache = new Dictionary<string, Texture2D>();

        static Texture2D Tex(string name)
        {
            if (texCache.TryGetValue(name, out var t)) return t;
            t = Resources.Load<Texture2D>("Textures/China/" + name);
            texCache[name] = t;
            return t;
        }

        static void TexFor(string n, out Texture2D diff, out Texture2D norm, out Vector2 tiling)
        {
            string ln = n.ToLowerInvariant();
            diff = null; norm = null; tiling = Vector2.one;
            if (ln.Contains("roof") || ln.Contains("tile") || ln.Contains("awning"))
            {
                diff = Tex("tex_roof_diff"); norm = Tex("tex_roof_nor"); tiling = new Vector2(2f, 2f);
            }
            else if (ln.Contains("stone"))
            {
                diff = Tex("tex_stone_diff"); norm = Tex("tex_stone_nor"); tiling = new Vector2(1.5f, 1.5f);
            }
            else if (ln.Contains("wood") || ln.Contains("door") || ln.Contains("window"))
            {
                diff = Tex("tex_wood_diff"); norm = Tex("tex_wood_nor"); tiling = new Vector2(2f, 2f);
            }
            else if (ln.Contains("wall") || ln.Contains("plaster") || ln.Contains("paper") || ln.Contains("lime"))
            {
                diff = Tex("tex_plaster_diff"); norm = Tex("tex_plaster_nor"); tiling = new Vector2(2.5f, 2.5f);
            }
        }

        /// <summary>Material Standard: con textura si el nombre lo indica, si no color plano.</summary>
        Material MakeMaterial(string name, Color tint)
        {
            var m = new Material(Shader.Find("Standard"));
            m.name = name;
            TexFor(name, out var diff, out var norm, out var tiling);
            if (diff != null)
            {
                m.mainTexture = diff;
                m.mainTextureScale = tiling;
                m.color = Color.Lerp(Color.white, tint, 0.35f);
                if (norm != null)
                {
                    m.SetTexture("_BumpMap", norm);
                    m.SetFloat("_BumpScale", 0.6f);
                    m.EnableKeyword("_NORMALMAP");
                }
            }
            else
            {
                m.color = tint;
            }
            m.SetFloat("_Glossiness", 0.12f);
            return m;
        }

        /// <summary>Pinta el edificio con los colores del catálogo y la paleta china.</summary>
        void TintBuilding(GameObject go, BuildingDef def)
        {
            Color wall, roof;
            if (!ColorUtility.TryParseHtmlString(def.color, out wall)) wall = new Color(0.82f, 0.62f, 0.38f);
            if (!ColorUtility.TryParseHtmlString(def.roof, out roof)) roof = new Color(0.16f, 0.28f, 0.22f);

            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                    Color c;
                    if (n.Contains("window")) c = new Color(0.62f, 0.86f, 0.95f);
                    else if (n.Contains("paper")) c = new Color(0.92f, 0.87f, 0.72f);
                    else if (n.Contains("door")) c = new Color(0.28f, 0.16f, 0.09f);
                    else if (n.Contains("wood")) c = new Color(0.34f, 0.19f, 0.10f);
                    else if (n.Contains("roof") || n.Contains("tile") || n.Contains("awning")) c = roof;
                    else if (n.Contains("stone")) c = new Color(0.55f, 0.54f, 0.50f);
                    else if (n.Contains("gold")) c = new Color(0.85f, 0.65f, 0.18f);
                    else if (n.Contains("red")) c = new Color(0.62f, 0.12f, 0.10f);
                    else if (n.Contains("bronze")) c = new Color(0.33f, 0.47f, 0.38f);
                    else c = wall;

                    mats[i] = MakeMaterial(mats[i] != null ? mats[i].name : "bmat", c);
                }
                r.sharedMaterials = mats;
            }
        }

        /// <summary>Pinta un decor por nombre de material (paleta China); null = color por defecto.</summary>
        void ApplyPalette(GameObject go, Color? tintA, Color? tintB)
        {
            var renderers = go.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                {
                    string n = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                    Color c;
                    if (n.Contains("gold") || n.Contains("trim")) c = tintA ?? new Color(0.85f, 0.65f, 0.18f);
                    else if (n.Contains("red")) c = tintA ?? new Color(0.62f, 0.12f, 0.10f);
                    else if (n.Contains("paper")) c = tintA ?? new Color(0.92f, 0.87f, 0.72f);
                    else if (n.Contains("bronze")) c = tintB ?? new Color(0.33f, 0.47f, 0.38f);
                    else if (n.Contains("stone")) c = tintB ?? new Color(0.46f, 0.45f, 0.42f);
                    else if (n.Contains("wood")) c = tintB ?? new Color(0.34f, 0.19f, 0.10f);
                    else if (n.Contains("wall")) c = tintB ?? new Color(0.82f, 0.62f, 0.38f);
                    else continue;

                    mats[i] = MakeMaterial(mats[i] != null ? mats[i].name : "dmat", c);
                }
                r.sharedMaterials = mats;
            }
        }

        /// <summary>Ajuste de escala para que los modelos importados entren en la grilla de 1 m.</summary>
        static float VisualScale(string def)
        {
            switch (def)
            {
                case "pagoda": return 0.19f;
                case "casahan": return 0.45f;
                default: return 1f;
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





