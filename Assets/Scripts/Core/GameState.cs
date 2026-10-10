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
        public int marketing;       // 0..3
        public int supplier = 1;    // 0 cheap, 1 standard, 2 premium
        public float reputation = 30f;   // 0..100
        public float cleanliness = 80f;  // 0..100
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
    }

    [Serializable]
    public class NewsItem
    {
        public int day;
        public string text;
    }

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

        public CityDef City => GameData.Cities[cityIndex];
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
            // each competitor starts with one restaurant
            for (int i = 1; i < g.companies.Count; i++)
            {
                int d = rng.Next(g.City.districts.Count);
                var r = g.CreateRestaurant(i, d, rng.Next(1, 4), rng.Next(0, 2));
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
            foreach (var rec in new[] { "margh", "salami", "funghi" })
                r.menu.Add(DefaultItem(rec));
            restaurants.Add(r);
            return r;
        }

        public float IngredientCost(string id, int supplier = 1)
        {
            int i = GameData.Ingredients.FindIndex(x => x.id == id);
            var def = GameData.Ingredients[i];
            float sup = supplier == 0 ? 0.8f : supplier == 2 ? 1.45f : 1f;
            return def.cost * City.priceLevel * ingredientMul[i] * sup;
        }

        public float ItemCost(MenuItem m, int supplier)
        {
            float c = 0;
            foreach (var id in m.ingredients) c += IngredientCost(id, supplier);
            return c;
        }

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
            m.price = Mathf.Round(ItemCost(m, 1) * 3.6f * 10f) / 10f;
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

        public float StaffWage(int skill) => 28f + skill * 8f;

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
