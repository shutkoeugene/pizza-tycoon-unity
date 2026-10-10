using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon
{
    public enum Taste { Veg, Meat, Sea, Classic }

    public enum Group { BigShots, Yuppies, Students, Kids, Teens, BlueCollar, Tourists, Goths }

    public class GroupDef
    {
        public string name, desc;
        public Taste taste;
        public float priceLevel;      // multiplier of the "fair" price they are happy to pay
        public float cheapPenalty;    // how suspicious they are of cheap food
        public int[] liked, ok;       // interior style indices
        public float guardSensitivity;
        public Color color;
    }

    public enum ObjKind { Restaurants, Reputation, Capital, Stars, Hq, Outsell }

    public class Objective
    {
        public ObjKind kind; public float value;
        public Objective(ObjKind k, float v) { kind = k; value = v; }
        public string Text()
        {
            switch (kind)
            {
                case ObjKind.Restaurants: return "Ресторанов: " + (int)value;
                case ObjKind.Reputation: return "Средняя репутация: " + (int)value;
                case ObjKind.Capital: return "Капитал: $" + ((int)value).ToString("N0");
                case ObjKind.Stars: return "Гурманские звёзды: " + (int)value;
                case ObjKind.Hq: return "Уровень штаба: " + (int)value;
                default: return "Обойти лучшего конкурента по числу гостей";
            }
        }
    }

    public class IngredientDef
    {
        public string id, name;
        public float cost;      // base cost per portion
        public float quality;   // 0..1
        public Taste taste;
        public Color color;
        public bool exotic;     // insects, snake, kangaroo: loved by goths/teens, shunned by the rich
    }

    public class RecipeDef
    {
        public string id, name;
        public string[] ingredients;
    }

    public class DistrictDef
    {
        public string name;
        public float wealth;       // 0..1
        public float population;   // 0..1
        public float rentFactor;
        public Taste preference;
        public Vector2Int cell;
        public float[] mix = new float[8];   // share of each customer group
    }

    public class CityDef
    {
        public string id, name;
        public int districtsX = 4, districtsY = 3;
        public float baseDemand = 1f;
        public float priceLevel = 1f;
        public Color accent;
        public int goalReputation;  // kept for display/compat; real goals are in objectives
        public int goalCash;
        public List<Objective> objectives = new List<Objective>();
        public List<DistrictDef> districts = new List<DistrictDef>();
    }

    public class StoreStyle
    {
        public string id, name;
        public float costMul, appeal;
    }

    public class StoreSize
    {
        public string id, name;
        public int seats, kitchenSlots;
        public float rent, buildCost;
    }

    public static class GameData
    {
        public static readonly List<IngredientDef> Ingredients = new List<IngredientDef>();
        public static readonly List<RecipeDef> Recipes = new List<RecipeDef>();
        public static readonly List<CityDef> Cities = new List<CityDef>();

        // style indices: 0 loft, 1 simple, 2 rustic, 3 modern, 4 elite
        public static readonly GroupDef[] Groups =
        {
            new GroupDef { name = "Богачи",    desc = "Любят дорого и со вкусом; чувствительны к охране", taste = Taste.Classic, priceLevel = 1.35f, cheapPenalty = 0.8f, liked = new[]{4,3}, ok = new[]{2}, guardSensitivity = 1.0f, color = new Color(0.95f, 0.8f, 0.2f) },
            new GroupDef { name = "Яппи",      desc = "Морепродукты и овощи, современный стиль",           taste = Taste.Sea,     priceLevel = 1.3f, cheapPenalty = 0.4f, liked = new[]{3,4}, ok = new[]{0,1}, guardSensitivity = 0.6f, color = new Color(0.4f, 0.7f, 1f) },
            new GroupDef { name = "Студенты",  desc = "Дёшево и вегетарианское",                           taste = Taste.Veg,     priceLevel = 0.7f, cheapPenalty = 0.05f, liked = new[]{0,1}, ok = new[]{2}, guardSensitivity = 0.5f, color = new Color(0.5f, 0.85f, 0.5f) },
            new GroupDef { name = "Дети",      desc = "Сладкие и простые пиццы, недорого",                 taste = Taste.Classic, priceLevel = 0.6f, cheapPenalty = 0.05f, liked = new[]{1,2}, ok = new[]{0,3}, guardSensitivity = 0.8f, color = new Color(1f, 0.6f, 0.8f) },
            new GroupDef { name = "Подростки", desc = "Мясные пиццы, лофт и модерн",                       taste = Taste.Meat,    priceLevel = 0.8f, cheapPenalty = 0.1f, liked = new[]{0,3}, ok = new[]{1}, guardSensitivity = 0.9f, color = new Color(0.9f, 0.4f, 0.4f) },
            new GroupDef { name = "Рабочие",   desc = "Сытное мясное, деревенский стиль, недорого",        taste = Taste.Meat,    priceLevel = 0.8f, cheapPenalty = 0.05f, liked = new[]{2,1}, ok = new[]{0}, guardSensitivity = 0.4f, color = new Color(0.7f, 0.55f, 0.35f) },
            new GroupDef { name = "Туристы",   desc = "Классика и морепродукты, живописный стиль",         taste = Taste.Sea,     priceLevel = 1.1f, cheapPenalty = 0.3f, liked = new[]{2,4}, ok = new[]{1,3}, guardSensitivity = 0.7f, color = new Color(0.95f, 0.65f, 0.3f) },
            new GroupDef { name = "Готы",      desc = "Овощи и необычное, лофт и деревенский стиль",       taste = Taste.Veg,     priceLevel = 0.9f, cheapPenalty = 0.1f, liked = new[]{0,2}, ok = new[]{3}, guardSensitivity = 0.9f, color = new Color(0.6f, 0.4f, 0.8f) },
        };

        public static readonly StoreStyle[] Styles =
        {
            new StoreStyle { id = "roh",  name = "Лофт",       costMul = 0.8f, appeal = 0.8f },
            new StoreStyle { id = "einf", name = "Простой",    costMul = 1.0f, appeal = 1.0f },
            new StoreStyle { id = "rust", name = "Деревенский",costMul = 1.3f, appeal = 1.15f },
            new StoreStyle { id = "mode", name = "Модерн",     costMul = 1.7f, appeal = 1.3f },
            new StoreStyle { id = "edel", name = "Элитный",    costMul = 2.4f, appeal = 1.6f },
        };

        public static readonly StoreSize[] Sizes =
        {
            new StoreSize { id = "klein",  name = "Малый",   seats = 12, kitchenSlots = 2, rent = 120, buildCost = 4000 },
            new StoreSize { id = "mittel", name = "Средний", seats = 24, kitchenSlots = 4, rent = 260, buildCost = 9000 },
            new StoreSize { id = "gross",  name = "Большой", seats = 40, kitchenSlots = 6, rent = 480, buildCost = 17000 },
        };

        static void Ing(string id, string name, float cost, float q, Taste t, Color c, bool exotic = false)
            => Ingredients.Add(new IngredientDef { id = id, name = name, cost = cost, quality = q, taste = t, color = c, exotic = exotic });

        static void Rec(string id, string name, params string[] ing)
            => Recipes.Add(new RecipeDef { id = id, name = name, ingredients = ing });

        static GameData()
        {
            Ing("dough",   "Тесто",      0.30f, 0.5f, Taste.Classic, new Color(0.95f, 0.85f, 0.6f));
            Ing("tomato",  "Томаты",     0.25f, 0.6f, Taste.Classic, new Color(0.85f, 0.15f, 0.1f));
            Ing("cheese",  "Сыр",        0.45f, 0.7f, Taste.Classic, new Color(1f, 0.9f, 0.4f));
            Ing("mush",    "Грибы",      0.40f, 0.6f, Taste.Veg,     new Color(0.75f, 0.65f, 0.5f));
            Ing("salami",  "Салями",     0.60f, 0.7f, Taste.Meat,    new Color(0.7f, 0.2f, 0.2f));
            Ing("ham",     "Ветчина",    0.65f, 0.7f, Taste.Meat,    new Color(0.95f, 0.6f, 0.6f));
            Ing("pine",    "Ананас",     0.50f, 0.5f, Taste.Veg,     new Color(1f, 0.85f, 0.2f));
            Ing("olive",   "Оливки",     0.35f, 0.6f, Taste.Veg,     new Color(0.2f, 0.25f, 0.1f));
            Ing("pepper",  "Перец",      0.35f, 0.6f, Taste.Veg,     new Color(0.2f, 0.7f, 0.2f));
            Ing("onion",   "Лук",        0.20f, 0.5f, Taste.Veg,     new Color(0.9f, 0.85f, 0.9f));
            Ing("shrimp",  "Креветки",   1.10f, 0.9f, Taste.Sea,     new Color(1f, 0.55f, 0.4f));
            Ing("tuna",    "Тунец",      0.90f, 0.8f, Taste.Sea,     new Color(0.8f, 0.5f, 0.45f));
            Ing("anchovy", "Анчоусы",    0.70f, 0.7f, Taste.Sea,     new Color(0.5f, 0.4f, 0.35f));
            Ing("basil",   "Базилик",    0.25f, 0.7f, Taste.Veg,     new Color(0.1f, 0.5f, 0.15f));
            Ing("spinach", "Шпинат",     0.30f, 0.6f, Taste.Veg,     new Color(0.1f, 0.4f, 0.1f));
            Ing("corn",    "Кукуруза",   0.25f, 0.5f, Taste.Veg,     new Color(1f, 0.9f, 0.1f));
            Ing("bacon",   "Бекон",      0.75f, 0.75f, Taste.Meat,   new Color(0.8f, 0.35f, 0.3f));
            Ing("prosc",   "Прошутто",   1.20f, 0.95f, Taste.Meat,   new Color(0.85f, 0.35f, 0.4f));
            Ing("sardine", "Сардины",    0.55f, 0.6f, Taste.Sea,     new Color(0.6f, 0.65f, 0.7f));
            Ing("caviar",  "Икра",       2.00f, 1.0f, Taste.Sea,     new Color(0.15f, 0.12f, 0.12f));
            Ing("banana",  "Банан",      0.35f, 0.5f, Taste.Classic, new Color(1f, 0.95f, 0.5f));
            Ing("chili",   "Чили",       0.30f, 0.6f, Taste.Meat,    new Color(0.85f, 0.1f, 0.05f));
            Ing("oregano", "Орегано",    0.20f, 0.7f, Taste.Classic, new Color(0.25f, 0.5f, 0.2f));
            Ing("mozz",    "Моцарелла",  0.80f, 0.9f, Taste.Classic, new Color(0.98f, 0.98f, 0.9f));
            Ing("eggplant","Баклажан",   0.35f, 0.6f, Taste.Veg,     new Color(0.35f, 0.15f, 0.4f));
            Ing("artichok","Артишоки",   0.85f, 0.85f, Taste.Veg,    new Color(0.4f, 0.55f, 0.35f));
            Ing("sausage", "Колбаски",   0.60f, 0.65f, Taste.Meat,   new Color(0.75f, 0.35f, 0.25f));
            Ing("squid",   "Кальмар",    1.00f, 0.85f, Taste.Sea,    new Color(0.9f, 0.85f, 0.8f));
            Ing("rucola",  "Руккола",    0.40f, 0.75f, Taste.Veg,    new Color(0.2f, 0.55f, 0.15f));
            Ing("gorgon",  "Горгонзола", 0.90f, 0.85f, Taste.Classic,new Color(0.8f, 0.85f, 0.9f));
            Ing("beef",    "Говядина",   0.95f, 0.8f, Taste.Meat,    new Color(0.55f, 0.2f, 0.15f));
            Ing("ants",    "Муравьи",    0.25f, 0.4f, Taste.Veg,     new Color(0.2f, 0.1f, 0.05f), true);
            Ing("snake",   "Змея",       1.00f, 0.6f, Taste.Meat,    new Color(0.4f, 0.5f, 0.2f), true);
            Ing("kangaroo","Кенгуру",    1.10f, 0.8f, Taste.Meat,    new Color(0.6f, 0.3f, 0.2f), true);
            Ing("locust",  "Саранча",    0.30f, 0.4f, Taste.Veg,     new Color(0.6f, 0.55f, 0.2f), true);
            Ing("maggots", "Личинки",    0.20f, 0.3f, Taste.Meat,    new Color(0.95f, 0.92f, 0.8f), true);
            Ing("cockles", "Сердцевидки",0.70f, 0.7f, Taste.Sea,     new Color(0.85f, 0.8f, 0.7f));
            Ing("octopus", "Осьминог",   1.00f, 0.85f, Taste.Sea,    new Color(0.7f, 0.35f, 0.4f));
            Ing("crab",    "Краб",       1.30f, 0.9f, Taste.Sea,     new Color(0.9f, 0.4f, 0.3f));
            Ing("lobster", "Омар",       2.20f, 1.0f, Taste.Sea,     new Color(0.85f, 0.2f, 0.15f));
            Ing("mussels", "Мидии",      0.60f, 0.7f, Taste.Sea,     new Color(0.25f, 0.25f, 0.35f));
            Ing("bamboo",  "Бамбук",     0.30f, 0.6f, Taste.Veg,     new Color(0.7f, 0.8f, 0.4f));
            Ing("goatch",  "Козий сыр",  0.90f, 0.8f, Taste.Classic, new Color(0.95f, 0.95f, 0.85f));

            Rec("margh",  "Маргарита",        "dough", "tomato", "cheese", "basil");
            Rec("funghi", "Фунги",            "dough", "tomato", "cheese", "mush");
            Rec("salami", "Салями",           "dough", "tomato", "cheese", "salami");
            Rec("hawaii", "Гавайская",        "dough", "tomato", "cheese", "ham", "pine");
            Rec("tonno",  "Тонно",            "dough", "tomato", "cheese", "tuna", "onion");
            Rec("quat",   "Четыре сезона",    "dough", "tomato", "cheese", "ham", "mush", "olive", "pepper");
            Rec("mare",   "Дары моря",        "dough", "tomato", "cheese", "shrimp", "tuna", "anchovy");
            Rec("veg",    "Вегетарианская",   "dough", "tomato", "cheese", "pepper", "corn", "spinach", "onion");
            Rec("prosc",  "Прошутто",         "dough", "tomato", "cheese", "prosc", "basil");
            Rec("meat",   "Мясная",           "dough", "tomato", "cheese", "salami", "bacon", "ham");
            Rec("diavola","Дьявола",          "dough", "tomato", "cheese", "salami", "chili", "onion");
            Rec("sardin", "Сардина",          "dough", "tomato", "cheese", "sardine", "olive", "oregano");
            Rec("bufala", "Буфала",           "dough", "tomato", "mozz", "basil", "oregano");
            Rec("gourmet","Гурман",           "dough", "tomato", "mozz", "prosc", "rucola", "artichok");
            Rec("caviar", "Королевская",      "dough", "mozz", "caviar", "shrimp", "squid");
            Rec("fruit",  "Фруктовая",        "dough", "tomato", "cheese", "banana", "pine", "corn");
            Rec("sausage","Колбасная",        "dough", "tomato", "cheese", "sausage", "pepper", "onion");
            Rec("quattro","Четыре сыра",      "dough", "mozz", "gorgon", "cheese", "basil");
            Rec("eggp",   "Баклажанная",      "dough", "tomato", "cheese", "eggplant", "basil", "olive");
            Rec("snakep", "Змеиная",          "dough", "tomato", "cheese", "snake", "chili", "onion");
            Rec("antp",   "Муравьиная",       "dough", "tomato", "cheese", "ants", "olive", "oregano");
            Rec("outback","Австралийская",    "dough", "tomato", "cheese", "kangaroo", "pepper", "corn");
            Rec("bugs",   "Хруст",            "dough", "tomato", "cheese", "locust", "maggots", "ants");
            Rec("octo",   "Осьминожья",       "dough", "tomato", "mozz", "octopus", "cockles", "mussels");

            BuildCities();
        }

        static void City(string id, string name, float demand, float price, Color accent, int goalRep, int goalCash, params (string n, float w, float p, Taste t)[] ds)
        {
            var c = new CityDef { id = id, name = name, baseDemand = demand, priceLevel = price, accent = accent, goalReputation = goalRep, goalCash = goalCash };
            // deterministic layout: districts on a grid
            int count = ds.Length;
            c.districtsX = 4;
            c.districtsY = Mathf.CeilToInt(count / 4f);
            for (int i = 0; i < count; i++)
            {
                c.districts.Add(new DistrictDef
                {
                    name = ds[i].n, wealth = ds[i].w, population = ds[i].p, preference = ds[i].t,
                    rentFactor = 0.6f + ds[i].w * 0.6f + ds[i].p * 0.6f,
                    cell = new Vector2Int(i % 4, i / 4)
                });
            }
            foreach (var d in c.districts) d.mix = MixFor(d);
            Cities.Add(c);
        }

        static bool Has(string n, params string[] keys) { foreach (var k in keys) if (n.Contains(k)) return true; return false; }

        public static float[] MixFor(DistrictDef d)
        {
            float w = d.wealth, p = d.population;
            var m = new float[8];
            m[(int)Group.BigShots] = 1.3f * Mathf.Pow(w, 2.2f);
            m[(int)Group.Yuppies] = 0.4f + 0.7f * w * p;
            m[(int)Group.Students] = 0.1f + 0.5f * (1 - w) * p;
            m[(int)Group.Kids] = 0.35f * (1 - w * 0.6f);
            m[(int)Group.Teens] = 0.1f + 0.45f * (1 - w * 0.5f) * p;
            m[(int)Group.BlueCollar] = 0.8f * (1 - w);
            m[(int)Group.Tourists] = 0.15f + 0.4f * w;
            m[(int)Group.Goths] = 0.12f + 0.1f * (1 - w);
            switch (d.preference)
            {
                case Taste.Veg: m[(int)Group.Students] *= 1.6f; m[(int)Group.Goths] *= 1.5f; m[(int)Group.Yuppies] *= 1.2f; break;
                case Taste.Meat: m[(int)Group.BlueCollar] *= 1.6f; m[(int)Group.Teens] *= 1.5f; break;
                case Taste.Sea: m[(int)Group.Tourists] *= 1.6f; m[(int)Group.Yuppies] *= 1.3f; break;
                default: m[(int)Group.Kids] *= 1.4f; m[(int)Group.BigShots] *= 1.2f; m[(int)Group.Tourists] *= 1.2f; break;
            }
            string n = d.name;
            if (Has(n, "Универс", "Латин", "Кройцберг", "Ньютаун")) m[(int)Group.Students] *= 2.5f;
            if (Has(n, "Порт", "Док", "Речной", "Вокзал", "Фавела", "Бронкс", "Бельвиль", "Окраина", "Пригород", "Бутово", "Вальекас", "Брикстон")) m[(int)Group.BlueCollar] *= 2f;
            if (Has(n, "Сити", "Дефанс", "Уолл", "Центр", "Чамартин")) { m[(int)Group.Yuppies] *= 2.2f; m[(int)Group.BigShots] *= 1.5f; }
            if (Has(n, "Ватикан", "Манхэттен", "Бухта", "Рокс", "Монмартр", "Елисейские", "Тверская", "Арбат", "Копакабана", "Гиндза", "Соль")) m[(int)Group.Tourists] *= 2f;
            if (Has(n, "Парк", "Ретиро", "Груневальд", "Уимблдон", "Мэнли")) m[(int)Group.Kids] *= 1.6f;
            if (Has(n, "Сохо", "Кэмден", "Сибуя", "Акихабара", "Лапа", "Ла-Латина", "Роппонги")) { m[(int)Group.Teens] *= 1.8f; m[(int)Group.Goths] *= 1.8f; }
            float sum = 0; foreach (var x in m) sum += x;
            for (int i = 0; i < m.Length; i++) m[i] /= sum;
            return m;
        }

        static void BuildCities()
        {
            City("rom", "Рим", 1.0f, 1.0f, new Color(0.85f, 0.3f, 0.2f), 40, 30000,
                ("Центр", .8f, .9f, Taste.Classic), ("Ватикан", .6f, .5f, Taste.Classic), ("Трастевере", .5f, .8f, Taste.Meat), ("Университет", .3f, .9f, Taste.Veg),
                ("Порт", .3f, .6f, Taste.Sea), ("Вокзал", .4f, 1f, Taste.Classic), ("Парк", .7f, .3f, Taste.Veg), ("Окраина", .2f, .7f, Taste.Meat));
            City("par", "Париж", 1.05f, 1.2f, new Color(0.3f, 0.4f, 0.8f), 50, 45000,
                ("Монмартр", .6f, .8f, Taste.Veg), ("Елисейские", .95f, .6f, Taste.Classic), ("Латинский", .4f, 1f, Taste.Classic), ("Марэ", .7f, .7f, Taste.Meat),
                ("Берси", .5f, .5f, Taste.Sea), ("Дефанс", .9f, .8f, Taste.Classic), ("Бельвиль", .2f, .9f, Taste.Meat), ("Пригород", .3f, .6f, Taste.Veg));
            City("lon", "Лондон", 1.05f, 1.25f, new Color(0.7f, 0.15f, 0.2f), 55, 55000,
                ("Сити", 1f, .6f, Taste.Classic), ("Сохо", .7f, .9f, Taste.Veg), ("Кэмден", .4f, .9f, Taste.Meat), ("Челси", .9f, .6f, Taste.Classic),
                ("Док", .3f, .6f, Taste.Sea), ("Кингс-Кросс", .5f, .8f, Taste.Classic), ("Брикстон", .25f, 1f, Taste.Meat), ("Уимблдон", .6f, .5f, Taste.Veg));
            City("ber", "Берлин", 0.95f, 0.9f, new Color(0.3f, 0.3f, 0.3f), 60, 60000,
                ("Митте", .6f, .9f, Taste.Classic), ("Кройцберг", .3f, 1f, Taste.Veg), ("Шарлоттенбург", .8f, .6f, Taste.Classic), ("Пренцлауэр", .5f, .8f, Taste.Veg),
                ("Шпрее", .4f, .5f, Taste.Sea), ("Темпельхоф", .4f, .6f, Taste.Meat), ("Марцан", .2f, .9f, Taste.Meat), ("Груневальд", .9f, .3f, Taste.Classic));
            City("mad", "Мадрид", 1.0f, 0.95f, new Color(0.9f, 0.7f, 0.2f), 65, 70000,
                ("Соль", .7f, 1f, Taste.Classic), ("Саламанка", .95f, .5f, Taste.Classic), ("Маласанья", .4f, .8f, Taste.Veg), ("Ла-Латина", .3f, .8f, Taste.Meat),
                ("Ретиро", .8f, .4f, Taste.Veg), ("Аточа", .4f, .7f, Taste.Classic), ("Вальекас", .2f, .9f, Taste.Meat), ("Чамартин", .8f, .6f, Taste.Sea));
            City("mos", "Москва", 1.1f, 1.0f, new Color(0.7f, 0.1f, 0.1f), 70, 85000,
                ("Тверская", .9f, .9f, Taste.Classic), ("Арбат", .7f, .8f, Taste.Veg), ("Китай-город", .8f, .7f, Taste.Meat), ("Замоскворечье", .6f, .6f, Taste.Classic),
                ("Речной вокзал", .4f, .8f, Taste.Sea), ("Измайлово", .3f, 1f, Taste.Meat), ("Сити", 1f, .5f, Taste.Classic), ("Бутово", .2f, 1f, Taste.Meat));
            City("yor", "Нью-Йорк", 1.2f, 1.4f, new Color(0.2f, 0.5f, 0.7f), 72, 110000,
                ("Манхэттен", 1f, 1f, Taste.Classic), ("Бруклин", .5f, 1f, Taste.Meat), ("Квинс", .3f, .9f, Taste.Classic), ("Бронкс", .2f, .8f, Taste.Meat),
                ("Гарлем", .3f, .8f, Taste.Veg), ("Уолл-стрит", 1f, .5f, Taste.Classic), ("Сохо", .7f, .7f, Taste.Veg), ("Порт", .3f, .5f, Taste.Sea));
            City("rio", "Рио-де-Жанейро", 1.0f, 0.8f, new Color(0.1f, 0.7f, 0.3f), 74, 120000,
                ("Копакабана", .7f, 1f, Taste.Sea), ("Ипанема", .9f, .7f, Taste.Classic), ("Лапа", .4f, .9f, Taste.Meat), ("Фавела", .1f, 1f, Taste.Meat),
                ("Ботафого", .6f, .6f, Taste.Veg), ("Центр", .5f, .8f, Taste.Classic), ("Порт", .3f, .5f, Taste.Sea), ("Тижука", .5f, .5f, Taste.Veg));
            City("syd", "Сидней", 1.0f, 1.15f, new Color(0.1f, 0.6f, 0.8f), 72, 140000,
                ("Бухта", .9f, .8f, Taste.Sea), ("Рокс", .7f, .7f, Taste.Classic), ("Бонди", .6f, .8f, Taste.Veg), ("Парраматта", .4f, .9f, Taste.Meat),
                ("Мэнли", .7f, .5f, Taste.Sea), ("Ньютаун", .4f, .9f, Taste.Veg), ("Центр", .8f, 1f, Taste.Classic), ("Пригород", .3f, .6f, Taste.Meat));
            City("tok", "Токио", 1.25f, 1.3f, new Color(0.9f, 0.3f, 0.5f), 66, 180000,
                ("Гиндза", 1f, .8f, Taste.Classic), ("Сибуя", .7f, 1f, Taste.Veg), ("Синдзюку", .8f, 1f, Taste.Meat), ("Акихабара", .5f, .9f, Taste.Classic),
                ("Цукидзи", .6f, .6f, Taste.Sea), ("Асакуса", .5f, .7f, Taste.Sea), ("Роппонги", .9f, .6f, Taste.Meat), ("Икебукуро", .5f, .9f, Taste.Veg));

            Obj("rom", R(3), Rep(40), Cap(30000));
            Obj("par", R(3), Rep(50), Stars(1), Cap(40000));
            Obj("lon", R(4), Rep(55), Cap(55000), Outsell());
            Obj("ber", R(4), Rep(60), Hq(3), Cap(60000));
            Obj("mad", R(4), Rep(62), Stars(1), Cap(70000));
            Obj("mos", R(5), Rep(65), Hq(3), Cap(85000));
            Obj("yor", R(5), Rep(66), Stars(1), Outsell(), Cap(110000));
            Obj("rio", R(5), Rep(68), Stars(1), Hq(4), Cap(120000));
            Obj("syd", R(6), Rep(70), Stars(2), Cap(140000));
            Obj("tok", R(6), Rep(72), Stars(2), Hq(4), Cap(180000));
        }

        static Objective R(float v) => new Objective(ObjKind.Restaurants, v);
        static Objective Rep(float v) => new Objective(ObjKind.Reputation, v);
        static Objective Cap(float v) => new Objective(ObjKind.Capital, v);
        static Objective Stars(float v) => new Objective(ObjKind.Stars, v);
        static Objective Hq(float v) => new Objective(ObjKind.Hq, v);
        static Objective Outsell() => new Objective(ObjKind.Outsell, 1);
        static void Obj(string id, params Objective[] os)
        {
            var c = Cities.Find(x => x.id == id);
            c.objectives.AddRange(os);
            foreach (var o in os) { if (o.kind == ObjKind.Reputation) c.goalReputation = (int)o.value; if (o.kind == ObjKind.Capital) c.goalCash = (int)o.value; }
        }
    }
}
