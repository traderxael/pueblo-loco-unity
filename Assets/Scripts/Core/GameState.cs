using System;
using System.Collections.Generic;

namespace PuebloLoco.Core
{
    [Serializable]
    public class PlacedBuilding
    {
        public string id, def;
        public int x, y;
        public long lastAt;
        public int ready;
    }

    [Serializable]
    public class PlacedDecor
    {
        public string id, def;
        public int x, y;
    }

    [Serializable]
    public class ActiveTask
    {
        public string taskId;
        public long endAt;
        public bool done;
    }

    [Serializable]
    public class CharacterState
    {
        public string def;
        public bool unlocked;
        public ActiveTask task;
    }

    [Serializable]
    public class Stats
    {
        public long coinsEarned;
        public int tasksDone, buildingsBuilt, levelsGained;
    }

    [Serializable]
    public class DailyState
    {
        public string date;
        public List<string> picked = new List<string>();
        public Stats stats = new Stats();
        public List<string> claimed = new List<string>();
    }

    [Serializable]
    public class WeeklyState
    {
        public string week;
        public string defId;
    }

    [Serializable]
    public class GameState
    {
        public int v;
        public long coins, donas, xp;
        public int level;
        public int landTier;
        public List<PlacedBuilding> buildings = new List<PlacedBuilding>();
        public List<PlacedDecor> decor = new List<PlacedDecor>();
        public List<CharacterState> characters = new List<CharacterState>();
        public int questIndex;
        public int tutorial;
        public Stats stats = new Stats();
        public List<string> achievements = new List<string>();
        public DailyState daily;
        public WeeklyState weekly;
        public long lastSeen;

        public static GameState Create(long now)
        {
            var s = new GameState
            {
                v = Catalog.VERSION,
                coins = 500,
                donas = 30,
                xp = 0,
                level = 1,
                landTier = 0,
                tutorial = 0,
                lastSeen = now
            };
            foreach (var c in Catalog.CHARACTERS)
                s.characters.Add(new CharacterState { def = c.def, unlocked = c.donas == 0 });
            return s;
        }

        public void SeedStarter(long now)
        {
            var mid = Catalog.LAND_TIERS[landTier].size / 2;
            var b = new PlacedBuilding { id = NextId(), def = "kiosco", x = mid - 1, y = mid, lastAt = now, ready = 0 };
            buildings.Add(b);
            stats.buildingsBuilt++;
        }

        private static long _idCounter = 1;
        private static readonly System.Random _rng = new System.Random();

        public static string NextId()
        {
            // Un solo RNG para toda la sesión: new Random() por llamada sembraba con
            // el reloj y podía repetir sufijo entre llamadas seguidas.
            long n = System.Threading.Interlocked.Increment(ref _idCounter);
            int salt;
            lock (_rng) { salt = _rng.Next(1000000); }
            return "e" + n + "_" + salt;
        }

        public bool HasAchievement(string id) => achievements.Contains(id);
    }
}
