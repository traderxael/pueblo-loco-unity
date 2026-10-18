using System;
using System.IO;
using UnityEngine;

namespace PuebloLoco.Core
{
    public static class SaveSystem
    {
        const string SAVE_FILE = "puebloloco.save.json";

        static string SavePath => Path.Combine(Application.persistentDataPath, SAVE_FILE);

        public static void Save(GameState s)
        {
            s.lastSeen = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string json = JsonUtility.ToJson(s, true);
            File.WriteAllText(SavePath, json);
        }

        public static GameState Load()
        {
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (!File.Exists(SavePath)) return CreateFresh(now);
            try
            {
                string json = File.ReadAllText(SavePath);
                var state = JsonUtility.FromJson<GameState>(json);
                if (state == null || state.v != Catalog.VERSION) return CreateFresh(now);
                return ApplyOffline(state, now);
            }
            catch
            {
                return CreateFresh(now);
            }
        }

        public static void Delete()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }

        static GameState CreateFresh(long now)
        {
            var fresh = GameState.Create(now);
            fresh.SeedStarter(now);
            Economy.EvaluateQuests(fresh, now);
            return fresh;
        }

        static GameState ApplyOffline(GameState s, long now)
        {
            long elapsed = Math.Max(0, Math.Min(now - (s.lastSeen == 0 ? now : s.lastSeen), 7L * 24 * 3600 * 1000));
            s.lastSeen = now;
            Economy.UpdateDaily(s, now);
            Economy.UpdateWeekly(s, now);
            foreach (var b in s.buildings)
            {
                var def = Catalog.Building(b.def);
                if (def == null) continue;
                long step = def.interval * 1000L;
                long cycles = Math.Min(elapsed, Catalog.OFFLINE_CAP_MS) / step;
                b.ready += (int)(cycles * def.coins);
                b.lastAt = now;
            }
            foreach (var c in s.characters)
                if (c.task != null && !c.task.done && now >= c.task.endAt) c.task.done = true;
            Economy.EvaluateQuests(s, now);
            return s;
        }
    }
}
