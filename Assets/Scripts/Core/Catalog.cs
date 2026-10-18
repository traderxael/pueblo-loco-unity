using System;
using System.Collections.Generic;

namespace PuebloLoco.Core
{
    [Serializable]
    public class BuildingDef
    {
        public string def, name, kind;
        public int cost, size, interval, coins, xp, level;
        public string color, roof;

        public BuildingDef(string def, string name, int cost, int size, int interval,
            int coins, int xp, int level, string color, string roof, string kind)
        {
            this.def = def; this.name = name; this.cost = cost; this.size = size;
            this.interval = interval; this.coins = coins; this.xp = xp;
            this.level = level; this.color = color; this.roof = roof; this.kind = kind;
        }
    }

    [Serializable]
    public class TaskDef
    {
        public string id, name;
        public int sec, coins, xp;

        public TaskDef(string id, string name, int sec, int coins, int xp)
        {
            this.id = id; this.name = name; this.sec = sec; this.coins = coins; this.xp = xp;
        }
    }

    [Serializable]
    public class CharacterDef
    {
        public string def, name, color, hair;
        public int donas;
        public List<TaskDef> tasks = new List<TaskDef>();

        public CharacterDef(string def, string name, int donas, string color, string hair, params TaskDef[] tasks)
        {
            this.def = def; this.name = name; this.donas = donas; this.color = color; this.hair = hair;
            this.tasks.AddRange(tasks);
        }

        public TaskDef Task(string id) => tasks.Find(t => t.id == id);
    }

    [Serializable]
    public class DecorDef
    {
        public string def, name, color;
        public int cost, level;

        public DecorDef(string def, string name, int cost, int level, string color)
        {
            this.def = def; this.name = name; this.cost = cost; this.level = level; this.color = color;
        }
    }

    [Serializable]
    public class LandTier
    {
        public int size, cost;

        public LandTier(int size, int cost) { this.size = size; this.cost = cost; }
    }

    [Serializable]
    public class QuestObjective
    {
        public string type, def;
        public int amount, level;

        public QuestObjective(string type, string def = null, int amount = 0, int level = 0)
        {
            this.type = type; this.def = def; this.amount = amount; this.level = level;
        }
    }

    [Serializable]
    public class QuestReward
    {
        public int coins, donas, xp;

        public QuestReward(int coins = 0, int donas = 0, int xp = 0)
        {
            this.coins = coins; this.donas = donas; this.xp = xp;
        }
    }

    [Serializable]
    public class QuestDef
    {
        public string id, name, story;
        public List<QuestObjective> objs = new List<QuestObjective>();
        public QuestReward reward;

        public QuestDef(string id, string name, string story, QuestReward reward, params QuestObjective[] objs)
        {
            this.id = id; this.name = name; this.story = story; this.reward = reward;
            this.objs.AddRange(objs);
        }
    }

    [Serializable]
    public class AchievementDef
    {
        public string id, name, desc;
        public int donas;
        public Func<GameState, bool> check;

        public AchievementDef(string id, string name, string desc, int donas, Func<GameState, bool> check)
        {
            this.id = id; this.name = name; this.desc = desc; this.donas = donas; this.check = check;
        }
    }

    [Serializable]
    public class DailyDef
    {
        public string id, name, desc, stat;
        public int goal, donas;

        public DailyDef(string id, string name, string desc, int goal, string stat, int donas)
        {
            this.id = id; this.name = name; this.desc = desc; this.goal = goal;
            this.stat = stat; this.donas = donas;
        }
    }

    [Serializable]
    public class EventDef
    {
        public string id, name, desc, ico, kind, target;
        public double mult;

        public EventDef(string id, string name, string desc, string ico, string kind, string target, double mult)
        {
            this.id = id; this.name = name; this.desc = desc; this.ico = ico;
            this.kind = kind; this.target = target; this.mult = mult;
        }
    }

    [Serializable]
    public class RankDef
    {
        public int min;
        public string name, ico;

        public RankDef(int min, string name, string ico) { this.min = min; this.name = name; this.ico = ico; }
    }

    public static class Catalog
    {
        public const int VERSION = 1;
        public const long OFFLINE_CAP_MS = 8L * 60 * 60 * 1000;
        public const int RUSH_WINDOW_SEC = 600;

        public static readonly BuildingDef[] BUILDINGS =
        {
            new BuildingDef("casa",      "Casa de Luis",      0,     1, 30,   8,    10,  1,  "#f5c96b", "#c0392b", "house"),
            new BuildingDef("kiosco",    "Kiosco de Diarios", 150,   1, 45,   16,   20,  1,  "#7ec8e3", "#2980b9", "kiosk"),
            new BuildingDef("floreria",  "Florería",          1200,  1, 90,   34,   25,  2,  "#a8e6a1", "#27ae60", "shop"),
            new BuildingDef("panaderia", "Panadería",         600,   2, 120,  48,   35,  2,  "#f0b27a", "#d35400", "shop"),
            new BuildingDef("taller",    "Taller Mecánico",   3000,  2, 240,  98,   50,  3,  "#95a5a6", "#5d6d7e", "garage"),
            new BuildingDef("casahan",   "Casa Han",          900,   1, 75,   30,   30,  3,  "#d9b382", "#5d4037", "house"),
            new BuildingDef("cafeteria", "Cafetería",         7500,  2, 300,  165,  65,  4,  "#d7bde2", "#7d3c98", "shop"),
            new BuildingDef("cine",      "Cine del Barrio",   18000, 2, 480,  330,  90,  5,  "#f1948a", "#943126", "cinema"),
            new BuildingDef("pagoda",    "Pagoda del Dragón", 6000,  2, 300,  210,  80,  6,  "#b03a2e", "#1e8449", "temple"),
            new BuildingDef("mercado",   "Mercado del Barrio",1500,  2, 120,  55,   35,  3,  "#e8b04b", "#b03a2e", "market"),
            new BuildingDef("muralla",   "Muralla",           2500,  2, 150,  45,   45,  4,  "#8d8d8d", "#5d4037", "wall"),
            new BuildingDef("templo",    "Templo Taoísta",    9000,  2, 360,  260,  90,  6,  "#c0392b", "#1e8449", "temple"),
            new BuildingDef("granja",    "Granja de Arroz",   1100,  2, 90,   40,   30,  3,  "#8bc34a", "#689f38", "farm"),
            new BuildingDef("herreria",  "Herrería",          2200,  1, 150,  70,   45,  4,  "#6d4c41", "#37474f", "shop"),
            new BuildingDef("posada",    "Posada",            4000,  2, 240,  120,  60,  5,  "#ffcc80", "#8d6e63", "inn"),
            new BuildingDef("campanario","Campanario",        7000,  1, 300,  180,  75,  7,  "#b71c1c", "#37474f", "tower"),
            new BuildingDef("escuela",   "Escuela",           35000, 2, 600,  520,  120, 6,  "#f9e79f", "#b7950b", "school"),
            new BuildingDef("hospital",  "Hospital",          80000, 3, 720,  960,  180, 8,  "#ffffff", "#c0392b", "hospital"),
            new BuildingDef("comisaria", "Comisaría",         150000,2, 900,  1500, 250, 10, "#aed6f1", "#1a5276", "office"),
            new BuildingDef("estadio",   "Estadio",           400000,3, 1200, 3200, 400, 12, "#abebc6", "#148f77", "stadium"),
            new BuildingDef("torre",     "Torre de Oficinas", 900000,2, 1800, 6000, 600, 14, "#d5dbdb", "#2e4053", "tower"), };


        public static readonly CharacterDef[] CHARACTERS =
        {
            new CharacterDef("luis",   "Luis",       0,  "#3498db", "#5d4037",
                new TaskDef("regar",   "Regar las plantas",     300,   60,   20),
                new TaskDef("pintar",  "Pintar la reja",        1800,  400,  90),
                new TaskDef("siesta",  "Dormir una siesta",     14400, 1800, 400)),
            new CharacterDef("nina",   "Nina",       0,  "#e91e63", "#f9e79f",
                new TaskDef("tarea",   "Hacer la tarea",        1200,  150,  50),
                new TaskDef("jugar",   "Jugar en la plaza",     600,   90,   30)),
            new CharacterDef("carmen", "Carmen",     10, "#e67e22", "#212121",
                new TaskDef("hornear", "Hornear pan",           900,   180,  45),
                new TaskDef("entregar","Entregar pedidos",      3600,  800,  160)),
            new CharacterDef("bruno",  "Bruno",      10, "#8d6e63", "#8d6e63",
                new TaskDef("cartero", "Perseguir al cartero",  300,   70,   25),
                new TaskDef("excavar", "Excavar el jardín",     1500,  300,  80)),
            new CharacterDef("ramon",  "Don Ramón",  15, "#607d8b", "#9e9e9e",
                new TaskDef("aceite",  "Cambiar aceite",        1800,  500,  100),
                new TaskDef("motor",   "Reparar motor",         7200,  2200, 380)),
            new CharacterDef("ana",    "Profe Ana",  20, "#9b59b6", "#4e342e",
                new TaskDef("corregir","Corregir exámenes",     3600,  700,  150),
                new TaskDef("clase",   "Preparar la clase",     2700,  450,  110)),
            new CharacterDef("sol",    "Dra. Sol",   30, "#00acc1", "#263238",
                new TaskDef("guardia", "Turno de guardia",      7200,  2000, 350),
                new TaskDef("cirugia", "Cirugía",               28800, 8000, 900)),
            new CharacterDef("alcalde","Alcalde Vera",50,"#c0392b", "#bdc3c7",
                new TaskDef("discurso","Discurso en la plaza",  10800, 3500, 500),
                new TaskDef("parque",  "Inaugurar el parque",   43200, 12000,1200)),
            new CharacterDef("sabio",  "Sabio Chen", 20, "#8e44ad", "#ecf0f1",
                new TaskDef("estudiar","Estudiar los clásicos",  1800,  320,  70),
                new TaskDef("meditar", "Meditar en el jardín",   900,   160,  40)),
            new CharacterDef("guardia","Guardia Wu", 25, "#b9770e", "#17202a",
                new TaskDef("patrullar","Patrullar el pueblo",   1200,  260,  55),
                new TaskDef("entrenar", "Entrenar con la lanza", 2400,  520,  100)),

            new CharacterDef("comerciante", "Comerciante Wen", 15, "#16a085", "#3e2723",
                new TaskDef("vender",   "Atender el mercado",      1500,  240,  55),
                new TaskDef("negociar", "Negociar con mercaderes", 3600,  700,  140)),
            new CharacterDef("campesino", "Campesino Li", 15, "#8d6e63", "#4e342e",
                new TaskDef("sembrar",  "Sembrar arroz",           2400,  380,  80),
                new TaskDef("cosechar", "Cosechar la cosecha",     5400,  980,  190)),        };

        public static readonly DecorDef[] DECOR =
        {
            new DecorDef("farol",   "Farol",   250,   2, "#f4d03f"),
            new DecorDef("arbusto", "Arbusto", 400,   2, "#52be80"),
            new DecorDef("banco",   "Banco",   800,   3, "#a04000"),
            new DecorDef("fuente",  "Fuente",  3000,  4, "#5dade2"),
            new DecorDef("estatua", "Estatua", 12000, 6, "#b2babb"),
        };

        public static readonly LandTier[] LAND_TIERS =
        {
            new LandTier(6,  0),
            new LandTier(8,  2000),
            new LandTier(10, 25000),
            new LandTier(12, 300000),
            new LandTier(14, 1800000),
            new LandTier(16, 7000000),
            new LandTier(18, 25000000),
            new LandTier(20, 80000000),
        };

        public static readonly QuestDef[] QUESTS =
        {
            new QuestDef("q1", "¡Bienvenido a Pueblo Loco!", "Construye tu primera casa junto al kiosco.",
                new QuestReward(donas:10, xp:50), new QuestObjective("build", "casa")),
            new QuestDef("q2", "El barrio despierta", "Recolecta 60 monedas de tus edificios.",
                new QuestReward(donas:15, xp:120), new QuestObjective("coinsEarned", amount:60)),
            new QuestDef("q3", "Pan fresco cada mañana", "Levanta la panadería del barrio.",
                new QuestReward(donas:20, xp:150), new QuestObjective("build", "panaderia")),
            new QuestDef("q4", "Conoce a Carmen", "Desbloquea a Carmen con donas.",
                new QuestReward(donas:25, xp:180), new QuestObjective("unlock", "carmen")),
            new QuestDef("q5", "Manos a la obra", "Abre el taller mecánico y llega a nivel 4.",
                new QuestReward(donas:30, xp:220), new QuestObjective("build","taller"), new QuestObjective("level", level:4)),
            new QuestDef("q6", "Educación primero", "Construye la escuela y completa 10 tareas.",
                new QuestReward(donas:40, xp:300), new QuestObjective("build","escuela"), new QuestObjective("tasksDone", amount:10)),
            new QuestDef("q7", "Salud para todos", "El barrio necesita un hospital.",
                new QuestReward(donas:50, xp:400), new QuestObjective("build", "hospital")),
            new QuestDef("q8", "Zona comercial", "Ten 5 edificios productivos en pie.",
                new QuestReward(donas:60, xp:500), new QuestObjective("buildings", amount:5)),
            new QuestDef("q9", "Torre del progreso", "Construye la Torre de Oficinas a nivel 10.",
                new QuestReward(donas:100, xp:700), new QuestObjective("build","torre"), new QuestObjective("level", level:10)),
            new QuestDef("q10","El corazón del pueblo", "Decora con 4 adornos y llega a nivel 12.",
                new QuestReward(donas:150, xp:900), new QuestObjective("decor", amount:4), new QuestObjective("level", level:12)),
            new QuestDef("q11","¡Alcalde electo!", "Desbloquea al Alcalde Vera.",
                new QuestReward(donas:200, xp:1100), new QuestObjective("unlock", "alcalde")),
            new QuestDef("q12","Pueblo Loco legendario", "Alcanza el nivel 15 para consagrar tu barrio.",
                new QuestReward(donas:500, xp:1500), new QuestObjective("level", level:15)),
            new QuestDef("q13","El valle se extiende", "Ampliá el terreno a 14x14.",
                new QuestReward(donas:80, xp:1200), new QuestObjective("land", amount:4)),
            new QuestDef("q14","Emperador del valle", "Llevá el territorio hasta 20x20.",
                new QuestReward(donas:250, xp:5000), new QuestObjective("land", amount:7)),
        };

        public static readonly AchievementDef[] ACHIEVEMENTS =
        {
            new AchievementDef("first-build",   "Primer ladrillo",       "Construye tu primer edificio", 5,  s => s.buildings.Count >= 2),
            new AchievementDef("ten-tasks",     "Manos a la obra",       "Completa 10 tareas",            10, s => s.stats.tasksDone >= 10),
            new AchievementDef("hundred-tasks", "Vecindario trabajador", "Completa 100 tareas",           50, s => s.stats.tasksDone >= 100),
            new AchievementDef("level-5",       "Barrio creciente",      "Alcanza el nivel 5",            10, s => s.level >= 5),
            new AchievementDef("level-10",      "Orgullo del pueblo",    "Alcanza el nivel 10",           25, s => s.level >= 10),
            new AchievementDef("level-15",      "Leyenda local",         "Alcanza el nivel 15",           60, s => s.level >= 15),
            new AchievementDef("rich",          "Caja fuerte",           "Ten 100.000 monedas a la vez",  20, s => s.coins >= 100000),
            new AchievementDef("dona-hoarder",  "Coleccionista de donas","Acumula 200 donas",             0,  s => s.donas >= 200),
            new AchievementDef("expander",      "Visionario",            "Expande el terreno a 10x10",    15, s => s.landTier >= 2),
            new AchievementDef("land-14",       "Conquistador",          "Expande el terreno a 14x14",    40, s => s.landTier >= 4),
            new AchievementDef("land-20",       "Emperador del valle",   "Expande el terreno a 20x20",   150, s => s.landTier >= 7),
            new AchievementDef("decorator",     "Paisajista",            "Coloca 6 adornos",              20, s => s.decor.Count >= 6),
            new AchievementDef("metropolis",    "Metrópolis",            "Ten 20 edificios en pie",       40, s => s.buildings.Count >= 20),
            new AchievementDef("all-quests",    "Alcalde honorario",     "Completa las 12 misiones",      100,s => s.questIndex >= QUESTS.Length),
        };

        public static readonly DailyDef[] DAILY_POOL =
        {
            new DailyDef("d-tasks",    "Jornada completa",   "Completa 3 tareas hoy",       3,   "tasksDone",      10),
            new DailyDef("d-coins",    "Buen comercio",      "Recolecta 500 monedas hoy",   500, "coinsEarned",    8),
            new DailyDef("d-build",    "Albañil del día",    "Construye 2 edificios hoy",   2,   "buildingsBuilt", 10),
            new DailyDef("d-level",    "Día de aprendizaje", "Sube 1 nivel hoy",            1,   "levelsGained",   12),
            new DailyDef("d-big",      "Gran recolección",   "Recolecta 2.000 monedas hoy", 2000,"coinsEarned",    15),
            new DailyDef("d-marathon", "Maratón vecinal",    "Completa 8 tareas hoy",       8,   "tasksDone",      20),
        };

        public static readonly EventDef[] EVENTS_POOL =
        {
            new EventDef("cine",    "Semana del Cine",    "El cine produce monedas x2",        "🎬", "building", "cine",      2),
            new EventDef("pan",     "Fiesta del Pan",     "Las panaderías producen x2",        "🥐", "building", "panaderia", 2),
            new EventDef("diarios", "Feria de Diarios",   "Los kioscos producen x3",           "📰", "building", "kiosco",    3),
            new EventDef("donas",   "Días de Dona",       "Las misiones diarias pagan doble",  "🍩", "daily",    null,       2),
            new EventDef("empleo",  "Feria del Empleo",   "Las tareas de vecinos pagan +50%",  "💼", "task",     null,       1.5),
            new EventDef("boom",    "Boom Constructor",   "Construir otorga el doble de XP",   "🏗️", "xp",       null,       2),
            new EventDef("rebaja",  "Semana de Rebajas",  "Acelerar tareas cuesta media dona", "⚡", "rush",     null,       0.5),
        };

        public static readonly RankDef[] RANKS =
        {
            new RankDef(0,     "Pueblo fantasma", "👻"),
            new RankDef(2000,  "Aldea",           "🌾"),
            new RankDef(6000,  "Villa",           "🏘️"),
            new RankDef(15000, "Ciudad",          "🏙️"),
            new RankDef(30000, "Metrópolis",      "🌆"),
            new RankDef(60000, "Leyenda local",   "👑"),
        };

        public static BuildingDef Building(string def) => Array.Find(BUILDINGS, b => b.def == def);
        public static CharacterDef Character(string def) => Array.Find(CHARACTERS, c => c.def == def);
        public static DecorDef Decor(string def) => Array.Find(DECOR, d => d.def == def);
        public static QuestDef Quest(string id) => Array.Find(QUESTS, q => q.id == id);
        public static DailyDef Daily(string id) => Array.Find(DAILY_POOL, d => d.id == id);
        public static EventDef Event(string id) => Array.Find(EVENTS_POOL, e => e.id == id);

        public static int HashStr(string s)
        {
            int h = 0;
            for (int i = 0; i < s.Length; i++) h = (int)((h * 31L + s[i]) | 0);
            return h == int.MinValue ? int.MaxValue : Math.Abs(h);
        }
    }
}
