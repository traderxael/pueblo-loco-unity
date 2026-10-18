using System;
using System.Collections.Generic;
using UnityEngine;
using PuebloLoco.Core;

namespace PuebloLoco.Gameplay
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }
        public long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        public event Action OnStateChanged;
        /// <summary>Solo cuando cambia la grilla (construir, adorno, ampliar). Evita
        /// reconstruir el pueblo entero al cobrar monedas o completar tareas.</summary>
        public event Action OnTownChanged;
        public event Action<List<GameEvent>> OnGameEvents;

        void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            State = SaveSystem.Load();
        }

        void Start()
        {
            OnStateChanged?.Invoke();
        }

        void Update()
        {
            var tick = Economy.Tick(State, NowMs);
            if (tick.events != null && tick.events.Count > 0)
                OnGameEvents?.Invoke(tick.events);
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) SaveSystem.Save(State);
        }

        void OnApplicationQuit()
        {
            SaveSystem.Save(State);
        }

        public void Save() => SaveSystem.Save(State);

        /// <summary>Fuerza un refresco de UI/grilla (lo usa el modo capturas y debug).</summary>
        public void NotifyChanged()
        {
            OnStateChanged?.Invoke();
            OnTownChanged?.Invoke();
        }

        public ActionResult PlaceBuilding(string defId, int x, int y)
        {
            var r = Economy.PlaceBuilding(State, defId, x, y, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                OnTownChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
                Save();
            }
            return r;
        }

        public ActionResult CollectBuilding(string buildingId)
        {
            var r = Economy.CollectBuilding(State, buildingId, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
            }
            return r;
        }

        public ActionResult CollectAll()
        {
            var r = Economy.CollectAll(State, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
            }
            return r;
        }

        public ActionResult StartTask(string charDefId, string taskId)
        {
            var r = Economy.StartTask(State, charDefId, taskId, NowMs);
            if (r.ok) OnStateChanged?.Invoke();
            return r;
        }

        public ActionResult CompleteTask(string charDefId)
        {
            var r = Economy.CompleteTask(State, charDefId, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
            }
            return r;
        }

        public ActionResult RushTask(string charDefId)
        {
            var r = Economy.RushTask(State, charDefId, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
            }
            return r;
        }

        public ActionResult UnlockCharacter(string defId)
        {
            var r = Economy.UnlockCharacter(State, defId, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
                Save();
            }
            return r;
        }

        public ActionResult ExpandLand()
        {
            var r = Economy.ExpandLand(State, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                OnTownChanged?.Invoke();
                Save();
            }
            return r;
        }

        public ActionResult BuyDecor(string defId)
        {
            var r = Economy.BuyDecor(State, defId, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                OnTownChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
                Save();
            }
            return r;
        }

        public ActionResult ClaimDaily(string id)
        {
            var r = Economy.ClaimDaily(State, id, NowMs);
            if (r.ok)
            {
                OnStateChanged?.Invoke();
                if (r.events != null && r.events.Count > 0) OnGameEvents?.Invoke(r.events);
            }
            return r;
        }
    }
}
