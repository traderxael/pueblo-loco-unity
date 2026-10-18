using System.Collections.Generic;
using UnityEngine;
using PuebloLoco.Core;

namespace PuebloLoco.Gameplay
{
    /// <summary>
    /// Crea y mantiene un vecino animado por cada personaje desbloqueado.
    /// Usa los modelos con esqueleto generados en Blender (Villager/Kid/Elder)
    /// y les pinta la ropa/pelo según el catálogo.
    /// </summary>
    public class VillagerManager : MonoBehaviour
    {
        public static VillagerManager Instance { get; private set; }

        readonly Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();
        public bool forceAll; // modo demo/capturas

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged += Sync;
            Sync();
        }

        void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnStateChanged -= Sync;
        }

        public void Sync()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            VillagerAI.RefreshOccupancy();

            var wanted = new List<string>();
            foreach (var c in gm.State.characters)
                if (c.unlocked || forceAll) wanted.Add(c.def);

            foreach (var def in new List<string>(spawned.Keys))
            {
                if (wanted.Contains(def)) continue;
                if (spawned[def] != null) Destroy(spawned[def]);
                spawned.Remove(def);
            }

            foreach (var def in wanted)
                if (!spawned.ContainsKey(def)) Spawn(def);
        }

        public GameObject Get(string defId)
        {
            GameObject go;
            return spawned.TryGetValue(defId, out go) ? go : null;
        }

        GameObject Spawn(string defId)
        {
            var def = Catalog.Character(defId);
            if (def == null) return null;

            string modelName;
            float scale;
            switch (defId)
            {
                case "nina": modelName = "Kid"; scale = 0.44f; break;
                case "ramon": modelName = "Elder"; scale = 0.50f; break;
                case "sabio": modelName = "SK_Sabio"; scale = 0.55f; break;
                case "guardia": modelName = "SK_Guardia"; scale = 0.56f; break;
                default: modelName = "Villager"; scale = 0.55f; break;
            }

            var go = new GameObject("Villager_" + defId);
            go.transform.SetParent(transform, false);
            go.transform.position = SpawnPoint(spawned.Count);

            var prefab = Resources.Load<GameObject>("Characters/" + modelName);
            Animator anim = null;
            if (prefab != null)
            {
                var model = Instantiate(prefab, go.transform);
                model.name = modelName;
                model.transform.localPosition = Vector3.zero;
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale = Vector3.one * scale;

                foreach (var col in model.GetComponentsInChildren<Collider>()) Destroy(col);
                Tint(model, def);

                anim = model.GetComponentInChildren<Animator>();
                if (anim == null) anim = model.AddComponent<Animator>();
                var ctrl = Resources.Load<RuntimeAnimatorController>("Animations/" + modelName);
                if (ctrl != null) anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;
                anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
            else
            {
                Debug.LogWarning("[VILLAGERS] falta Resources/Characters/" + modelName + ".fbx");
                var fallback = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                fallback.transform.SetParent(go.transform, false);
                fallback.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            }

            var view = go.AddComponent<CharacterView>();
            view.Setup(defId, prefab);

            var ai = go.AddComponent<VillagerAI>();
            ai.Setup(defId, anim);

            var capsule = go.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.5f, 0f);
            capsule.radius = 0.26f;
            capsule.height = 1.1f;

            spawned[defId] = go;
            return go;
        }

        Vector3 SpawnPoint(int index)
        {
            var gm = GameManager.Instance;
            int size = gm != null ? Catalog.LAND_TIERS[gm.State.landTier].size : 6;
            float half = size * 0.5f;
            float angle = (index % 8) * 45f * Mathf.Deg2Rad;
            float radius = Mathf.Min(half - 1.2f, 2.2f);
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        static void Tint(GameObject model, CharacterDef def)
        {
            Color shirt = Parse(def.color, new Color(0.25f, 0.55f, 0.85f));
            Color hair = Parse(def.hair, new Color(0.22f, 0.14f, 0.1f));
            Color skin = new Color(0.93f, 0.72f, 0.58f);
            Color pants = Color.Lerp(shirt, Color.black, 0.55f);
            Color shoes = new Color(0.12f, 0.11f, 0.14f);

            foreach (var r in model.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (mats[i] == null) continue;
                    string n = mats[i].name.ToLowerInvariant();
                    Color c;
                    if (n.Contains("skin")) c = skin;
                    else if (n.Contains("shirt") || n.Contains("robe")) c = shirt;
                    else if (n.Contains("pants") || n.Contains("trousers")) c = pants;
                    else if (n.Contains("hair")) c = hair;
                    else if (n.Contains("shoes") || n.Contains("boots")) c = shoes;
                    else if (n.Contains("armor")) c = Color.Lerp(shirt, new Color(0.32f, 0.22f, 0.06f), 0.45f);
                    else if (n.Contains("trim") || n.Contains("gold")) c = new Color(0.85f, 0.65f, 0.18f);
                    else continue;

                    var m = new Material(Shader.Find("Standard"));
                    m.name = mats[i].name;
                    m.color = c;
                    m.SetFloat("_Glossiness", 0.15f);
                    m.SetFloat("_Metallic", 0f);
                    mats[i] = m;
                    changed = true;
                }
                if (changed) r.sharedMaterials = mats;
            }
        }

        static Color Parse(string hex, Color fallback)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(hex, out c) ? c : fallback;
        }
    }
}

