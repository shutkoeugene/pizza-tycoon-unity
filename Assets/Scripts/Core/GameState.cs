using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace PizzaTycoon
{
    [Serializable]
    public class MenuItem
    {
        public string name;
        public List<string> ingredients = new List<string>();
        public float price;
        public bool enabled = true;
    }

    [Serializable]
    public class DayStats
    {
        public int day;
        public int customers, unserved, pizzas;
        public float revenue, costIngredients, costWages, costRent, costMarketing, costOther;
        public float Profit => revenue - costIngredients - costWages - costRent - costMarketing - costOther;
    }

    [Serializable]
    public class Restaurant
    {
        public int id;
        public int owner;           // company index; 0 = player
        public int district;
        public int style, size;
        public string name;
        public List<MenuItem> menu = new List<MenuItem>();
        public int cooks = 1, waiters = 1;
        public int skill = 1;       // staff training 1..5
        public int marketing;       // 0..3 branch advertising
        public int adTarget = -1;   // -1 everyone, else Group index
        public int adPromise;       // 0 none, 1 quality, 2 price, 3 service
        public int supplier = 1;    // 0 cheap, 1 standard, 2 premium
        public int supplierDays;    // loyalty: days with the same supplier
        public float reputation = 30f;   // 0..100
        public float cleanliness = 80f;  // 0..100
        // staff
        public bool manager;
        public int guards;
        public int guardSkill = 1;
        public int wage = 1;        // 0..3 wage level
        public int shifts = 2;      // 2 or 3
        public float motivation = 60f, health = 85f;
        public int trainDays;       // days of reduced capacity while training on shift
        public bool delivery;
        // interior
        public int decor = 1;       // 0 basic, 1 good, 2 luxurious
        public bool plants;
        public float condition = 100f;  // furniture condition 0..100
        // status
        public int closedDays;
        public int stars;
        public int starProgress, starLoss;
        public float[] rating = new float[8];   // today's rating per customer group 0..1
        public int deliveryOrders;
        public int openedDay;
        public DayStats last = new DayStats();
        public DayStats total = new DayStats();
    }

    [Serializable]
    public class Company
    {
        public string name;
        public float cash;
        public float loan;
        public bool isPlayer;
        public Color color;
        public float aggression = 1f;
        public int hq = 1;          // headquarters level 1..4
        public int cityAd;          // 0..3 city-wide advertising
        public int cityAdTarget = -1;
        public int mayor;           // 0..2 mayor bribe level
        public int warehouse;       // 0..2 cooled warehouse
    }

    [Serializable]
    public class NewsItem
    {
        public int day;
        public string text;
    }

    [Serializable]
    public class DistrictMod { public int index; public float wealth, population; }

    [Serializable]
    public class GameState
    {
        public int version = 1;
        public int cityIndex;
        public int day = 1;
        public int nextId = 1;
        public int seed;
        public int citiesCompleted;
        public bool bankrupt, won;
        public List<Company> companies = new List<Company>();
        public List<Restaurant> restaurants = new List<Restaurant>();
        public List<NewsItem> news = new List<NewsItem>();
        public List<float> ingredientMul = new List<float>();
        public List<float> cashHistory = new List<float>();
        public float demandMul = 1f;
        public int eventDaysLeft;
        public string eventText = "";
        public List<string> trend = new List<string>();
        public int lastPlayerCustomers, lastBestRivalCustomers;

        public List<DistrictMod> mods = new List<DistrictMod>();
        [NonSerialized] CityDef cityCache;

        // per-game copy of the city: districts can change (mayor's decisions) without touching static data
        public CityDef City
        {
            get
            {
                var src = GameData.Cities[cityIndex];
                if (cityCache == null || cityCache.id != src.id)
                {
                    cityCache = new CityDef
                    {
                        id = src.id, name = src.name, districtsX = src.districtsX, districtsY = src.districtsY, baseDemand = src.baseDemand,
                        priceLevel = src.priceLevel, accent = src.accent, goalReputation = src.goalReputation, goalCash = src.goalCash, objectives = src.objectives
                    };
                    for (int i = 0; i < src.districts.Count; i++)
                    {
                        var d = src.districts[i];
                        var c = new DistrictDef { name = d.name, wealth = d.wealth, population = d.population, rentFactor = d.rentFactor, preference = d.preference, cell = d.cell };
                        var m = mods.Find(x => x.index == i);
                        if (m != null) { c.wealth = m.wealth; c.population = m.population; c.rentFactor = 0.6f + c.wealth * 0.6f + c.population * 0.6f; }
                        c.mix = GameData.MixFor(c);
                        cityCache.districts.Add(c);
                    }
                }
                return cityCache;
            }
        }

        public void ModDistrict(int index, float wealth, float population)
        {
            var m = mods.Find(x => x.index == index);
            if (m == null) { m = new DistrictMod { index = index }; mods.Add(m); }
            m.wealth = wealth; m.population = population;
        }
        public Company Player => companies[0];

        public static GameState NewGame(int cityIdx, float startCash, string playerName)
        {
            var g = new GameState { cityIndex = cityIdx, seed = Environment.TickCount };
            g.companies.Add(new Company { name = playerName, cash = startCash, isPlayer = true, color = new Color(0.95f, 0.75f, 0.1f) });
            Color[] cc = { new Color(0.8f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f), new Color(0.3f, 0.75f, 0.35f) };
            string[] names = { "Mamma Mia", "Pizza Express", "Bella Napoli", "Don Corleone", "Luigi's", "Vesuvio" };
            var rng = new System.Random(g.seed);
            for (int i = 0; i < 3; i++)
                g.companies.Add(new Company
                {
                    name = names[(i * 2 + rng.Next(2)) % names.Length], cash = 20000 + rng.Next(10000),
                    color = cc[i], aggression = 0.7f + (float)rng.NextDouble() * 0.8f
                });
            foreach (var _ in GameData.Ingredients) g.ingredientMul.Add(1f);
            g.RollTrends(rng);
            // each competitor starts with one restaurant
            for (int i = 1; i < g.companies.Count; i++)
            {
                int d = rng.Next(g.City.districts.Count);
                var r = g.CreateRestaurant(i, d, GameData.Groups[Simulation.DominantGroup(g.City.districts[d])].liked[0] == 4 ? 3 : GameData.Groups[Simulation.DominantGroup(g.City.districts[d])].liked[0], rng.Next(0, 2));
                r.reputation = 35f;
            }
            g.AddNews("Добро пожаловать в " + g.City.name + "! Откройте первый ресторан.");
            return g;
        }

        public Restaurant CreateRestaurant(int owner, int district, int style, int size)
        {
            var r = new Restaurant { id = nextId++, owner = owner, district = district, style = style, size = size, openedDay = day };
            r.name = companies[owner].name + " — " + City.districts[district].name;
            r.cooks = 1 + size; r.waiters = 1 + size;
            var recs = new List<string> { "margh", "salami", "funghi" };
            if (owner != 0)
            {
                // competitors tune their menu to the district's dominant customer group
                var taste = GameData.Groups[Simulation.DominantGroup(City.districts[district])].taste;
                var ranked = new List<RecipeDef>(GameData.Recipes);
                ranked.Sort((a, b) => CountTaste(b, taste).CompareTo(CountTaste(a, taste)));
                recs.Clear(); for (int i = 0; i < 4 && i < ranked.Count; i++) recs.Add(ranked[i].id);
            }
            foreach (var rec in recs) r.menu.Add(DefaultItem(rec));
            restaurants.Add(r);
            return r;
        }

        public void RollTrends(System.Random rng)
        {
            trend.Clear();
            var pool = new List<string>();
            foreach (var i in GameData.Ingredients) if (i.id != "dough") pool.Add(i.id);
            for (int k = 0; k < 4 && pool.Count > 0; k++) { int j = rng.Next(pool.Count); trend.Add(pool[j]); pool.RemoveAt(j); }
        }

        static int CountTaste(RecipeDef rec, Taste t)
        {
            int n = 0; foreach (var id in rec.ingredients) if (GameData.Ingredients.Find(x => x.id == id).taste == t) n++; return n;
        }

        public float IngredientCost(string id, int supplier = 1, float loyalty = 0f)
        {
            int i = GameData.Ingredients.FindIndex(x => x.id == id);
            var def = GameData.Ingredients[i];
            float sup = supplier == 0 ? 0.8f : supplier == 2 ? 1.45f : 1f;
            return def.cost * City.priceLevel * ingredientMul[i] * sup * (1f - loyalty);
        }

        // loyalty discount: up to 25% after 150 days with one supplier
        public static float Loyalty(Restaurant r) => r == null ? 0f : Mathf.Clamp01(r.supplierDays / 150f) * 0.25f;

        public float ItemCost(MenuItem m, int supplier, float loyalty = 0f)
        {
            float c = 0;
            foreach (var id in m.ingredients) c += IngredientCost(id, supplier, loyalty);
            return c;
        }
        public float ItemCost(MenuItem m, Restaurant r) => ItemCost(m, r.supplier, Loyalty(r));

        public static float ItemQuality(MenuItem m, int supplier)
        {
            if (m.ingredients.Count == 0) return 0;
            float q = 0;
            foreach (var id in m.ingredients) q += GameData.Ingredients.Find(x => x.id == id).quality;
            q /= m.ingredients.Count;
            // richness bonus for more ingredients
            q += Mathf.Min(0.15f, m.ingredients.Count * 0.02f);
            q *= supplier == 0 ? 0.8f : supplier == 2 ? 1.25f : 1f;
            return Mathf.Clamp01(q);
        }

        public MenuItem DefaultItem(string recipeId)
        {
            var rec = GameData.Recipes.Find(x => x.id == recipeId);
            var m = new MenuItem { name = rec.name, ingredients = new List<string>(rec.ingredients) };
            m.price = Mathf.Round(ItemCost(m, 1) * 4.2f * 10f) / 10f;
            return m;
        }

        public void AddNews(string text)
        {
            news.Insert(0, new NewsItem { day = day, text = text });
            if (news.Count > 60) news.RemoveAt(news.Count - 1);
        }

        public List<Restaurant> RestaurantsOf(int owner) => restaurants.FindAll(r => r.owner == owner);

        public float BuildCost(int district, int style, int size)
        {
            var d = City.districts[district];
            return GameData.Sizes[size].buildCost * GameData.Styles[style].costMul * (0.7f + d.rentFactor * 0.3f);
        }

        public float DailyRent(Restaurant r)
        {
            var d = City.districts[r.district];
            return GameData.Sizes[r.size].rent * 0.6f * d.rentFactor * City.priceLevel;
        }

        public static readonly float[] WageMul = { 0.8f, 1f, 1.25f, 1.5f };
        public float StaffWage(int skill, int wage = 1) => (22f + skill * 6f) * WageMul[Mathf.Clamp(wage, 0, 3)];
        public static float GuardWage(int skill) => 35f + skill * 8f;
        public static readonly float[] HqCost = { 0, 0, 12000, 30000, 70000 };   // cost to reach level index
        public static float HqUpkeep(int level) => 25f * level;
        public static int MaxRestaurants(int hq) => 2 + 2 * hq;

        // ---- persistence ----
        static string SavePath(int slot) => Path.Combine(Application.persistentDataPath, "save" + slot + ".json");
        public static bool HasSave(int slot) => File.Exists(SavePath(slot));
        public void Save(int slot) { File.WriteAllText(SavePath(slot), JsonUtility.ToJson(this)); }
        public static GameState Load(int slot)
        {
            try { return JsonUtility.FromJson<GameState>(File.ReadAllText(SavePath(slot))); }
            catch (Exception e) { Debug.LogWarning("Load failed: " + e.Message); return null; }
        }
    }
}
