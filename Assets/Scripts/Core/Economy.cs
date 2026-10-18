using System;
using System.Collections.Generic;
using System.Linq;

namespace PuebloLoco.Core
{
    public class ActionResult
    {
        public bool ok;
        public string reason;
        public int amount, refund, coins, xp, gained, donas;
        public List<GameEvent> events = new List<GameEvent>();
        public static ActionResult Fail(string reason) => new ActionResult { ok = false, reason = reason };
        public static ActionResult Success() => new ActionResult { ok = true };
    }

    public class GameEvent
    {
        public string type;
        public QuestDef quest;
        public AchievementDef ach;
        public QuestReward reward;
    }

    public class TickResult
    {
        public long coinsAccrued;
        public int tasksReady, levels;
        public List<GameEvent> events = new List<GameEvent>();
    }

    public class DailyProgress
    {
        public DailyDef def;
        public int progress, goal;
        public bool done, claimed;
    }

    public static class Economy
    {
        /* ------------------------------------------------------------ progresión */

        public static int XpForLevel(int level) => (int)Math.Round(80.0 * Math.Pow(level, 1.55));

        /* ------------------------------------------------------------ utilidades */

        public static Dictionary<string, PlacedBuilding> OccupancyMap(GameState s)
        {
            var map = new Dictionary<string, PlacedBuilding>();
            foreach (var b in s.buildings)
            {
                var def = Catalog.Building(b.def);
                if (def == null) continue;
                for (int dx = 0; dx < def.size; dx++)
                    for (int dy = 0; dy < def.size; dy++)
                        map[(b.x + dx) + "," + (b.y + dy)] = b;
            }
            foreach (var d in s.decor) map[d.x + "," + d.y] = null;
            return map;
        }

        public static ActionResult CanPlaceBuilding(GameState s, string defId, int x, int y)
        {
            var d = Catalog.Building(defId);
            if (d == null) return ActionResult.Fail("Edificio desconocido");
            if (s.level < d.level) return ActionResult.Fail($"Requiere nivel {d.level}");
            if (s.coins < d.cost) return ActionResult.Fail("Monedas insuficientes");
            int size = Catalog.LAND_TIERS[s.landTier].size;
            if (x < 0 || y < 0 || x + d.size > size || y + d.size > size)
                return ActionResult.Fail("Fuera del terreno");
            var occ = OccupancyMap(s);
            for (int dx = 0; dx < d.size; dx++)
                for (int dy = 0; dy < d.size; dy++)
                    if (occ.ContainsKey((x + dx) + "," + (y + dy)))
                        return ActionResult.Fail("Espacio ocupado");
            return ActionResult.Success();
        }

        public static (int x, int y)? FreeTile(GameState s)
        {
            int size = Catalog.LAND_TIERS[s.landTier].size;
            var occ = OccupancyMap(s);
            // RNG propio: new Random() se siembra con el reloj, así que dos llamadas
            // en el mismo tick devolvían la misma tesela.
            var rng = new Random(Catalog.HashStr(s.coins + "|" + s.decor.Count + "|" + s.buildings.Count));
            for (int tries = 0; tries < 500; tries++)
            {
                int x = rng.Next(size), y = rng.Next(size);
                if (!occ.ContainsKey(x + "," + y) && x > 0 && y > 0) return (x, y);
            }
            for (int x2 = 1; x2 < size - 1; x2++)
                for (int y2 = 1; y2 < size - 1; y2++)
                    if (!occ.ContainsKey(x2 + "," + y2)) return (x2, y2);
            return null;
        }

        /* ------------------------------------------------------------- XP */

        public static int AddXp(GameState s, int amount)
        {
            if (amount <= 0) return 0;
            s.xp += amount;
            int gained = 0;
            while (s.xp >= XpForLevel(s.level)) { s.xp -= XpForLevel(s.level); s.level++; gained++; }
            if (gained > 0) { s.stats.levelsGained += gained; BumpDaily(s, "levelsGained", gained); }
            return gained;
        }

        /* ------------------------------------------------------------ eventos semanales */

        public static string TodayKey(long nowMs)
        {
            var d = DateTimeOffset.FromUnixTimeMilliseconds(nowMs).LocalDateTime;
            return d.Year + "-" + d.Month.ToString("00") + "-" + d.Day.ToString("00");
        }

        public static string WeekKey(long nowMs)
        {
            var d = DateTimeOffset.FromUnixTimeMilliseconds(nowMs).UtcDateTime;
            var target = new DateTime(d.Year, d.Month, d.Day, 0, 0, 0, DateTimeKind.Utc);
            int dayNum = ((int)target.DayOfWeek + 6) % 7;
            target = target.AddDays(-dayNum + 3);
            var firstThursday = new DateTime(target.Year, 1, 4, 0, 0, 0, DateTimeKind.Utc);
            int fd = ((int)firstThursday.DayOfWeek + 6) % 7;
            firstThursday = firstThursday.AddDays(-fd + 3);
            int week = 1 + (int)Math.Round((target - firstThursday).TotalDays / 7.0);
            return target.Year + "-W" + week;
        }

        public static DailyDef[] PickDaily(string dateKey)
        {
            var pool = new List<DailyDef>(Catalog.DAILY_POOL);
            var picks = new List<DailyDef>();
            int h = Catalog.HashStr(dateKey);
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                picks.Add(pool[h % pool.Count]);
                pool.RemoveAt(h % pool.Count);
                h = (pool.Count > 0 ? h / pool.Count : 0) + 7;
            }
            return picks.ToArray();
        }

        public static void UpdateDaily(GameState s, long now)
        {
            string key = TodayKey(now);
            if (s.daily == null || s.daily.date != key)
            {
                s.daily = new DailyState { date = key };
                foreach (var d in PickDaily(key)) s.daily.picked.Add(d.id);
            }
            if (s.daily.claimed == null) s.daily.claimed = new List<string>();
        }

        public static void UpdateWeekly(GameState s, long now)
        {
            string key = WeekKey(now);
            if (s.weekly == null || s.weekly.week != key)
            {
                int idx = Catalog.HashStr(key) % Catalog.EVENTS_POOL.Length;
                s.weekly = new WeeklyState { week = key, defId = Catalog.EVENTS_POOL[idx].id };
            }
        }

        public static EventDef ActiveEvent(GameState s)
        {
            if (s.weekly == null) return null;
            return Catalog.Event(s.weekly.defId);
        }

        public static double EventMult(GameState s, string kind)
        {
            var e = ActiveEvent(s);
            if (e == null || e.kind != kind) return 1;
            return e.mult;
        }

        public static List<DailyProgress> DailyProgressList(GameState s, long now)
        {
            UpdateDaily(s, now);
            var result = new List<DailyProgress>();
            foreach (var id in s.daily.picked)
            {
                var d = Catalog.Daily(id);
                if (d == null) continue;
                int cur = Math.Min(d.goal, GetDailyStat(s, d.stat));
                result.Add(new DailyProgress
                {
                    def = d, progress = cur, goal = d.goal,
                    done = cur >= d.goal,
                    claimed = s.daily.claimed != null && s.daily.claimed.Contains(id)
                });
            }
            return result;
        }

        static int GetDailyStat(GameState s, string stat) => stat switch
        {
            "tasksDone" => s.daily.stats.tasksDone,
            "coinsEarned" => (int)Math.Min(int.MaxValue, s.daily.stats.coinsEarned),
            "buildingsBuilt" => s.daily.stats.buildingsBuilt,
            "levelsGained" => s.daily.stats.levelsGained,
            _ => 0
        };

        static void BumpDaily(GameState s, string field, int n = 1)
        {
            if (s.daily == null) return;
            switch (field)
            {
                case "tasksDone": s.daily.stats.tasksDone += n; break;
                case "coinsEarned": s.daily.stats.coinsEarned += n; break;
                case "buildingsBuilt": s.daily.stats.buildingsBuilt += n; break;
                case "levelsGained": s.daily.stats.levelsGained += n; break;
            }
        }

        public static ActionResult ClaimDaily(GameState s, string id, long now)
        {
            UpdateDaily(s, now);
            if (s.daily.claimed.Contains(id)) return ActionResult.Fail("Ya reclamada");
            var d = Catalog.Daily(id);
            if (d == null || !s.daily.picked.Contains(id)) return ActionResult.Fail("No es una misión de hoy");
            if (GetDailyStat(s, d.stat) < d.goal) return ActionResult.Fail("Aún no completada");
            s.daily.claimed.Add(id);
            int pay = (int)Math.Floor(d.donas * EventMult(s, "daily"));
            s.donas += pay;
            var r = ActionResult.Success();
            r.donas = pay;
            r.events = EvaluateQuests(s, now);
            return r;
        }

        /* ------------------------------------------------------------- acciones */

        public static ActionResult PlaceBuilding(GameState s, string defId, int x, int y, long now)
        {
            var check = CanPlaceBuilding(s, defId, x, y);
            if (!check.ok) return check;
            var d = Catalog.Building(defId);
            s.coins -= d.cost;
            s.buildings.Add(new PlacedBuilding { id = GameState.NextId(), def = defId, x = x, y = y, lastAt = now, ready = 0 });
            s.stats.buildingsBuilt++;
            BumpDaily(s, "buildingsBuilt");
            AddXp(s, (int)Math.Floor(d.xp * EventMult(s, "xp")));
            var r = ActionResult.Success();
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static ActionResult UnlockCharacter(GameState s, string defId, long now)
        {
            var c = Catalog.Character(defId);
            if (c == null) return ActionResult.Fail("Personaje desconocido");
            var entry = s.characters.Find(ch => ch.def == defId);
            if (entry == null) return ActionResult.Fail("Personaje desconocido");
            if (entry.unlocked) return ActionResult.Fail("Ya está desbloqueado");
            if (s.donas < c.donas) return ActionResult.Fail("Donas insuficientes");
            s.donas -= c.donas;
            entry.unlocked = true;
            var r = ActionResult.Success();
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static ActionResult BuyDecor(GameState s, string defId, long now)
        {
            var d = Catalog.Decor(defId);
            if (d == null) return ActionResult.Fail("Adorno desconocido");
            if (s.level < d.level) return ActionResult.Fail($"Requiere nivel {d.level}");
            if (s.coins < d.cost) return ActionResult.Fail("Monedas insuficientes");
            var tile = FreeTile(s);
            if (tile == null) return ActionResult.Fail("No hay espacio libre");
            s.coins -= d.cost;
            s.decor.Add(new PlacedDecor { id = GameState.NextId(), def = defId, x = tile.Value.x, y = tile.Value.y });
            var r = ActionResult.Success();
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static ActionResult ExpandLand(GameState s, long now)
        {
            int i = s.landTier + 1;
            if (i >= Catalog.LAND_TIERS.Length) return ActionResult.Fail("Terreno máximo alcanzado");
            var next = Catalog.LAND_TIERS[i];
            if (s.coins < next.cost) return ActionResult.Fail("Monedas insuficientes");
            s.coins -= next.cost;
            s.landTier++;
            var r = ActionResult.Success();
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static ActionResult SellBuilding(GameState s, string buildingId, long now)
        {
            var b = s.buildings.Find(x => x.id == buildingId);
            if (b == null) return ActionResult.Fail("Edificio no encontrado");
            var def = Catalog.Building(b.def);
            int refund = (int)Math.Floor(def.cost * 0.5);
            s.coins += refund;
            s.buildings.RemoveAll(x => x.id == buildingId);
            var r = ActionResult.Success();
            r.refund = refund;
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static ActionResult SellDecor(GameState s, string decorId, long now)
        {
            var d = s.decor.Find(x => x.id == decorId);
            if (d == null) return ActionResult.Fail("Adorno no encontrado");
            var def = Catalog.Decor(d.def);
            int refund = (int)Math.Floor(def.cost * 0.5);
            s.coins += refund;
            s.decor.RemoveAll(x => x.id == decorId);
            var r = ActionResult.Success();
            r.refund = refund;
            r.events = EvaluateQuests(s, now);
            return r;
        }

        /* --------------------------------------------------------- producción */

        public static void AccrueBuilding(PlacedBuilding b, long now)
        {
            var def = Catalog.Building(b.def);
            if (def == null) return;
            long elapsed = now - b.lastAt;
            long step = def.interval * 1000L;
            if (elapsed < step) return;
            long cycles = elapsed / step;
            long cap = Math.Max(1, Catalog.OFFLINE_CAP_MS / step);
            if (cycles > cap) cycles = cap;
            b.ready += (int)(cycles * def.coins);
            b.lastAt += cycles * step;
        }

        static double BuildingMult(GameState s, string defId)
        {
            var e = ActiveEvent(s);
            if (e != null && e.kind == "building" && e.target == defId) return e.mult;
            return 1;
        }

        public static ActionResult CollectBuilding(GameState s, string buildingId, long now)
        {
            var b = s.buildings.Find(x => x.id == buildingId);
            if (b == null) return ActionResult.Fail("Edificio no encontrado");
            AccrueBuilding(b, now);
            if (b.ready <= 0) return ActionResult.Fail("Nada que recolectar");
            int amount = (int)Math.Floor(b.ready * (double)BuildingMult(s, b.def));
            s.coins += amount;
            s.stats.coinsEarned += amount;
            BumpDaily(s, "coinsEarned", amount);
            b.ready = 0;
            b.lastAt = now;
            var r = ActionResult.Success();
            r.amount = amount;
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static ActionResult CollectAll(GameState s, long now)
        {
            long total = 0;
            foreach (var b in s.buildings)
            {
                AccrueBuilding(b, now);
                total += (long)Math.Floor(b.ready * (double)BuildingMult(s, b.def));
                b.ready = 0;
                b.lastAt = now;
            }
            if (total <= 0) return ActionResult.Fail("Nada que recolectar");
            s.coins += total;
            s.stats.coinsEarned += total;
            BumpDaily(s, "coinsEarned", (int)Math.Min(int.MaxValue, total));
            var r = ActionResult.Success();
            r.amount = (int)Math.Min(int.MaxValue, total);
            r.events = EvaluateQuests(s, now);
            return r;
        }

        /* -------------------------------------------------------------- tareas */

        public static ActionResult StartTask(GameState s, string charDefId, string taskId, long now)
        {
            var entry = s.characters.Find(c => c.def == charDefId);
            if (entry == null || !entry.unlocked) return ActionResult.Fail("Personaje no disponible");
            if (entry.task != null) return ActionResult.Fail(entry.task.done ? "Reclama la recompensa primero" : "Ya tiene una tarea en curso");
            var t = Catalog.Character(charDefId)?.Task(taskId);
            if (t == null) return ActionResult.Fail("Tarea desconocida");
            entry.task = new ActiveTask { taskId = taskId, endAt = now + t.sec * 1000L, done = false };
            return ActionResult.Success();
        }

        public static ActionResult CompleteTask(GameState s, string charDefId, long now)
        {
            var entry = s.characters.Find(c => c.def == charDefId);
            if (entry == null || entry.task == null) return ActionResult.Fail("Sin tarea");
            if (!entry.task.done && now < entry.task.endAt) return ActionResult.Fail("La tarea aún no termina");
            var t = Catalog.Character(charDefId)?.Task(entry.task.taskId);
            if (t == null) { entry.task = null; return ActionResult.Fail("Tarea desconocida"); }
            int pay = (int)Math.Floor(t.coins * EventMult(s, "task"));
            s.coins += pay;
            s.stats.coinsEarned += pay;
            s.stats.tasksDone++;
            BumpDaily(s, "tasksDone");
            BumpDaily(s, "coinsEarned", pay);
            int gained = AddXp(s, t.xp);
            entry.task = null;
            var r = ActionResult.Success();
            r.coins = pay; r.xp = t.xp; r.gained = gained;
            r.events = EvaluateQuests(s, now);
            return r;
        }

        public static int RushCost(GameState s, string charDefId, long now)
        {
            var e = s.characters.Find(c => c.def == charDefId);
            if (e == null || e.task == null || e.task.done) return 0;
            double remain = Math.Max(0, (e.task.endAt - now) / 1000.0);
            int baseCost = Math.Max(1, (int)Math.Ceiling(remain / 600.0));
            return Math.Max(1, (int)Math.Floor(baseCost * EventMult(s, "rush")));
        }

        public static ActionResult RushTask(GameState s, string charDefId, long now)
        {
            var entry = s.characters.Find(c => c.def == charDefId);
            if (entry == null) return ActionResult.Fail("Personaje no disponible");
            if (entry.task == null) return ActionResult.Fail("Sin tarea");
            int cost = RushCost(s, charDefId, now);
            if (s.donas < cost) return ActionResult.Fail("Donas insuficientes");
            s.donas -= cost;
            entry.task.done = true;
            entry.task.endAt = now;
            return CompleteTask(s, charDefId, now);
        }

        /* ---------------------------------------------------------------- tick */

        public static TickResult Tick(GameState s, long now)
        {
            UpdateDaily(s, now);
            UpdateWeekly(s, now);
            var out_ = new TickResult();
            foreach (var b in s.buildings)
            {
                int before = b.ready;
                AccrueBuilding(b, now);
                out_.coinsAccrued += b.ready - before;
            }
            foreach (var c in s.characters)
                if (c.task != null && !c.task.done && now >= c.task.endAt) { c.task.done = true; out_.tasksReady++; }
            out_.events = EvaluateQuests(s, now);
            return out_;
        }

        /* -------------------------------------------------------------- misiones */

        public static QuestDef ActiveQuest(GameState s) =>
            s.questIndex < Catalog.QUESTS.Length ? Catalog.QUESTS[s.questIndex] : null;

        public static bool ObjectiveDone(GameState s, QuestObjective o) => o.type switch
        {
            "build" => s.buildings.Any(b => b.def == o.def),
            "unlock" => s.characters.Any(c => c.def == o.def && c.unlocked),
            "level" => s.level >= o.level,
            "coinsEarned" => s.stats.coinsEarned >= o.amount,
            "tasksDone" => s.stats.tasksDone >= o.amount,
            "buildings" => s.buildings.Count >= o.amount,
            "decor" => s.decor.Count >= o.amount,
            "land" => s.landTier >= o.amount,
            _ => false
        };

        public static List<GameEvent> EvaluateQuests(GameState s, long now)
        {
            var events = new List<GameEvent>();
            int guard = 0;
            while (guard++ < 50)
            {
                var q = ActiveQuest(s);
                if (q == null) break;
                if (!q.objs.All(o => ObjectiveDone(s, o))) break;
                var r = q.reward;
                if (r.coins > 0) { s.coins += r.coins; s.stats.coinsEarned += r.coins; }
                if (r.donas > 0) s.donas += r.donas;
                if (r.xp > 0) AddXp(s, r.xp);
                s.questIndex++;
                events.Add(new GameEvent { type = "quest", quest = q, reward = r });
            }
            foreach (var a in Catalog.ACHIEVEMENTS)
            {
                if (s.HasAchievement(a.id)) continue;
                if (a.check(s))
                {
                    s.achievements.Add(a.id);
                    if (a.donas > 0) s.donas += a.donas;
                    events.Add(new GameEvent { type = "achievement", ach = a });
                }
            }
            return events;
        }

        /* ------------------------------------------------------------ ranking */

        public static (long total, int byBuildings, int byLevel, int byQuests, int byTasks, int byDecor, int byLand) TownScore(GameState s)
        {
            int byBuildings = s.buildings.Count * 100;
            int byLevel = s.level * 250;
            int byQuests = s.questIndex * 500;
            int byTasks = s.stats.tasksDone * 10;
            int byDecor = s.decor.Count * 150;
            int byLand = s.landTier * 300;
            long total = byBuildings + byLevel + byQuests + byTasks + byDecor + byLand;
            return (total, byBuildings, byLevel, byQuests, byTasks, byDecor, byLand);
        }

        public static RankDef TownRank(long score)
        {
            var r = Catalog.RANKS[0];
            foreach (var rank in Catalog.RANKS) if (score >= rank.min) r = rank;
            return r;
        }
    }
}
