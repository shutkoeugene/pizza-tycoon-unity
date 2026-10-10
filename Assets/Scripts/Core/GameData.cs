using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon
{
    public enum Taste { Veg, Meat, Sea, Classic }

    public class IngredientDef
    {
        public string id, name;
        public float cost;      // base cost per portion
        public float quality;   // 0..1
        public Taste taste;
        public Color color;
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
    }

    public class CityDef
    {
        public string id, name;
        public int districtsX = 4, districtsY = 3;
        public float baseDemand = 1f;
        public float priceLevel = 1f;
        public Color accent;
        public int goalReputation;  // reputation needed to finish city
        public int goalCash;
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

        static void Ing(string id, string name, float cost, float q, Taste t, Color c)
            => Ingredients.Add(new IngredientDef { id = id, name = name, cost = cost, quality = q, taste = t, color = c });

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
            Cities.Add(c);
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
        }
    }
}
