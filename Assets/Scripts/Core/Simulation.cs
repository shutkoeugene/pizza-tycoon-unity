using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon
{
    public static class Simulation
    {
        static readonly float[] MarketingCost = { 0, 50, 150, 400 };
        static readonly float[] MarketingBoost = { 0f, 0.15f, 0.4f, 0.8f };

        public static float MarketingPrice(int level) => MarketingCost[level];

        public static float FairPrice(GameState g, MenuItem m, Restaurant r)
        {
            var d = g.City.districts[r.district];
            return g.ItemCost(m, r.supplier) * 3.6f * (0.75f + d.wealth * 0.6f);
        }

        // 0..1 how attractive the menu is for this district
        static float MenuScore(GameState g, Restaurant r, DistrictDef d, out float avgPrice, out float avgQuality)
        {
            float sum = 0; int n = 0; avgPrice = 0; avgQuality = 0;
            foreach (var m in r.menu)
            {
                if (!m.enabled || m.ingredients.Count == 0) continue;
                float q = GameState.ItemQuality(m, r.supplier);
                float match = 0.5f;
                foreach (var id in m.ingredients)
                    if (GameData.Ingredients.Find(x => x.id == id).taste == d.preference) match += 0.12f;
                match = Mathf.Min(match, 1.1f);
                float fair = FairPrice(g, m, r);
                float priceScore = Mathf.Clamp01(Mathf.Pow(fair / Mathf.Max(0.5f, m.price), 1.6f));
                sum += (0.35f * q + 0.35f * match + 0.3f * priceScore);
                avgPrice += m.price; avgQuality += q; n++;
            }
            if (n == 0) return 0;
            avgPrice /= n; avgQuality /= n;
            // variety bonus
            float variety = Mathf.Min(1.2f, 0.8f + n * 0.08f);
            return Mathf.Clamp01(sum / n) * variety;
        }

        public static float Attraction(GameState g, Restaurant r)
        {
            var d = g.City.districts[r.district];
            float ap, aq;
            float menu = MenuScore(g, r, d, out ap, out aq);
            float style = GameData.Styles[r.style].appeal;
            float rep = 0.4f + r.reputation / 100f * 1.2f;
            float mk = 1f + MarketingBoost[r.marketing];
            float clean = 0.5f + r.cleanliness / 200f;
            return menu * style * rep * mk * clean;
        }

        public static void Step(GameState g)
        {
            var rng = new System.Random(g.seed + g.day * 7919);
            var city = g.City;

            // random market and events
            for (int i = 0; i < g.ingredientMul.Count; i++)
                g.ingredientMul[i] = Mathf.Clamp(g.ingredientMul[i] + ((float)rng.NextDouble() - 0.5f) * 0.06f, 0.7f, 1.8f);
            if (g.eventDaysLeft > 0) { g.eventDaysLeft--; if (g.eventDaysLeft == 0) { g.demandMul = 1f; } }
            else if (rng.NextDouble() < 0.04)
            {
                string[] ev = { "Футбольный матч в городе: спрос вырос!", "Проливные дожди: люди сидят дома.", "Городской фестиваль: все на улицах!", "Жара: пиццу заказывают меньше." };
                float[] mul = { 1.35f, 0.75f, 1.5f, 0.85f };
                int k = rng.Next(ev.Length);
                g.demandMul = mul[k]; g.eventDaysLeft = 3 + rng.Next(3); g.eventText = ev[k];
                g.AddNews(ev[k]);
            }

            // demand per district
            var share = new Dictionary<int, float>();
            var attr = new Dictionary<int, float>();
            var distAttr = new float[city.districts.Count];
            foreach (var r in g.restaurants)
            {
                float a = Attraction(g, r);
                attr[r.id] = a;
                distAttr[r.district] += a;
            }

            float weekday = (g.day % 7 == 5 || g.day % 7 == 6) ? 1.25f : 1f;
            foreach (var r in g.restaurants)
            {
                var d = city.districts[r.district];
                float K = 0.5f; // people who eat elsewhere / at home
                float demand = (100f + 520f * d.population) * city.baseDemand * g.demandMul * weekday * (0.6f + d.wealth * 0.5f);
                float taken = demand * attr[r.id] / (distAttr[r.district] + K);
                // neighbouring districts spill over
                float spill = 0;
                for (int j = 0; j < city.districts.Count; j++)
                {
                    if (j == r.district) continue;
                    var cd = city.districts[j];
                    if (Mathf.Abs(cd.cell.x - d.cell.x) + Mathf.Abs(cd.cell.y - d.cell.y) != 1) continue;
                    float dm = (100f + 520f * cd.population) * city.baseDemand * g.demandMul * weekday * 0.2f;
                    spill += dm * attr[r.id] / (distAttr[j] + attr[r.id] + K);
                }
                taken += spill;
                RunDay(g, r, Mathf.Max(0, taken), rng);
            }

            // finance
            foreach (var c in g.companies)
            {
                if (c.loan > 0) { float interest = c.loan * 0.0006f; c.cash -= interest; }
            }
            foreach (var r in g.restaurants)
            {
                var c = g.companies[r.owner];
                c.cash += r.last.Profit;
            }

            RunCompetitors(g, rng);

            g.cashHistory.Add(g.Player.cash);
            if (g.cashHistory.Count > 120) g.cashHistory.RemoveAt(0);
            g.day++;

            // end conditions
            if (g.Player.cash < -8000 && g.Player.loan >= 49000) { g.bankrupt = true; g.AddNews("Вы обанкротились."); }
            float rep = 0; int pc = 0;
            foreach (var r in g.RestaurantsOf(0)) { rep += r.reputation; pc++; }
            if (pc > 0) rep /= pc;
            if (!g.won && g.day >= 90 && pc >= 3 && rep >= city.goalReputation && g.Player.cash - g.Player.loan >= city.goalCash)
            {
                g.won = true;
                g.AddNews("Цель города достигнута! Вы можете перейти в следующий город.");
            }
        }

        static void RunDay(GameState g, Restaurant r, float wanted, System.Random rng)
        {
            var s = new DayStats { day = g.day };
            var size = GameData.Sizes[r.size];
            float skill = 0.8f + r.skill * 0.12f;
            float cookCap = r.cooks * 95f * skill;
            float waiterCap = r.waiters * 85f * skill;
            float seatCap = size.seats * 7f;
            int kitchenLimit = size.kitchenSlots;
            if (r.cooks > kitchenLimit) cookCap = kitchenLimit * 95f * skill; // overcrowded kitchen
            float cap = Mathf.Min(cookCap, waiterCap, seatCap);

            int enabled = r.menu.FindAll(m => m.enabled && m.ingredients.Count > 0).Count;
            if (enabled == 0) wanted = 0;

            float served = Mathf.Min(wanted, cap);
            // random noise
            served *= 0.93f + (float)rng.NextDouble() * 0.14f;
            served = Mathf.Min(served, cap);
            s.customers = Mathf.RoundToInt(served);
            s.unserved = Mathf.RoundToInt(Mathf.Max(0, wanted - served));

            // allocate pizzas to menu items by attractiveness
            var d = g.City.districts[r.district];
            float tot = 0; var w = new List<float>();
            foreach (var m in r.menu)
            {
                float x = 0;
                if (m.enabled && m.ingredients.Count > 0)
                {
                    float fair = FairPrice(g, m, r);
                    x = Mathf.Pow(Mathf.Clamp01(fair / Mathf.Max(0.5f, m.price)), 1.2f) + 0.3f;
                    foreach (var id in m.ingredients)
                        if (GameData.Ingredients.Find(q => q.id == id).taste == d.preference) x += 0.1f;
                }
                w.Add(x); tot += x;
            }
            float perCustomer = 1.5f;
            for (int i = 0; i < r.menu.Count; i++)
            {
                if (tot <= 0) break;
                int n = Mathf.RoundToInt(s.customers * perCustomer * w[i] / tot);
                s.pizzas += n;
                s.revenue += n * r.menu[i].price;
                s.costIngredients += n * g.ItemCost(r.menu[i], r.supplier);
            }
            s.costWages = (r.cooks + r.waiters) * g.StaffWage(r.skill);
            s.costRent = g.DailyRent(r);
            s.costMarketing = MarketingCost[r.marketing];
            int chain = g.restaurants.FindAll(x => x.owner == r.owner).Count;
            s.costOther = 25f + (size.seats * 0.8f) + 35f * (chain - 1);

            // reputation & cleanliness
            float satisfaction = 50f;
            if (s.customers + s.unserved > 0)
            {
                float servedRatio = s.customers / (float)(s.customers + s.unserved);
                float ap, aq; MenuScore(g, r, d, out ap, out aq);
                satisfaction = (servedRatio * 40f + aq * 40f + (GameData.Styles[r.style].appeal - 0.6f) * 20f);
            }
            r.reputation = Mathf.Clamp(r.reputation + (satisfaction - r.reputation) * 0.05f, 0, 100);
            r.cleanliness = Mathf.Clamp(r.cleanliness - s.customers * 0.02f + (r.waiters * r.skill * 1.2f), 20, 100);

            r.last = s;
            r.total.customers += s.customers; r.total.pizzas += s.pizzas; r.total.revenue += s.revenue;
            r.total.costIngredients += s.costIngredients; r.total.costWages += s.costWages;
            r.total.costRent += s.costRent; r.total.costMarketing += s.costMarketing; r.total.costOther += s.costOther;
        }

        static void RunCompetitors(GameState g, System.Random rng)
        {
            for (int ci = 1; ci < g.companies.Count; ci++)
            {
                var c = g.companies[ci];
                var mine = g.RestaurantsOf(ci);
                if (c.cash < 0 && mine.Count > 1 && rng.NextDouble() < 0.1)
                {
                    var worst = mine[0];
                    foreach (var r in mine) if (r.total.revenue < worst.total.revenue) worst = r;
                    g.restaurants.Remove(worst);
                    c.cash += 2000;
                    g.AddNews(c.name + " закрыл ресторан: " + worst.name);
                    continue;
                }
                if (g.day % 4 != ci % 4) continue;

                // adjust prices & marketing
                foreach (var r in mine)
                {
                    foreach (var m in r.menu)
                    {
                        float fair = FairPrice(g, m, r);
                        float target = fair * (0.95f + (float)rng.NextDouble() * 0.25f);
                        m.price = Mathf.Round(Mathf.Lerp(m.price, target, 0.5f) * 10f) / 10f;
                    }
                    r.marketing = c.cash > 20000 ? Mathf.Min(3, Mathf.FloorToInt(c.aggression * 1.5f)) : 0;
                    if (r.menu.Count < 5 && rng.NextDouble() < 0.2)
                    {
                        var rec = GameData.Recipes[rng.Next(GameData.Recipes.Count)];
                        if (!r.menu.Exists(m => m.name == rec.name)) r.menu.Add(g.DefaultItem(rec.id));
                    }
                }

                // expand
                if (rng.NextDouble() < 0.25 * c.aggression && mine.Count < 5)
                {
                    int best = -1; float bestScore = -1;
                    for (int d = 0; d < g.City.districts.Count; d++)
                    {
                        int count = g.restaurants.FindAll(r => r.district == d).Count;
                        float score = g.City.districts[d].population * (0.5f + g.City.districts[d].wealth) / (1f + count * 1.5f);
                        if (score > bestScore) { bestScore = score; best = d; }
                    }
                    int style = c.aggression > 1.2f ? 3 : 1;
                    int size = c.cash > 40000 ? 2 : (c.cash > 15000 ? 1 : 0);
                    float cost = g.BuildCost(best, style, size);
                    if (c.cash > cost * 1.5f)
                    {
                        c.cash -= cost;
                        g.CreateRestaurant(ci, best, style, size);
                        g.AddNews(c.name + " открыл новый ресторан в районе " + g.City.districts[best].name + ".");
                    }
                }
            }
        }

        // ---- player actions ----
        public static string Build(GameState g, int district, int style, int size)
        {
            float cost = g.BuildCost(district, style, size);
            if (g.Player.cash < cost) return "Недостаточно денег.";
            if (g.restaurants.FindAll(r => r.district == district).Count >= 4) return "В районе уже нет места.";
            g.Player.cash -= cost;
            g.CreateRestaurant(0, district, style, size).reputation = 25f;
            g.AddNews("Открыт ресторан в районе " + g.City.districts[district].name + ".");
            return null;
        }

        public static string Upgrade(GameState g, Restaurant r, int newStyle, int newSize)
        {
            float cost = Mathf.Max(0, g.BuildCost(r.district, newStyle, newSize) - g.BuildCost(r.district, r.style, r.size) * 0.6f);
            if (g.Player.cash < cost) return "Недостаточно денег (" + Mathf.RoundToInt(cost) + " $).";
            g.Player.cash -= cost; r.style = newStyle; r.size = newSize;
            return null;
        }

        public static void Close(GameState g, Restaurant r)
        {
            g.Player.cash += g.BuildCost(r.district, r.style, r.size) * 0.4f;
            g.restaurants.Remove(r);
        }

        public static string TakeLoan(GameState g, float amount)
        {
            if (g.Player.loan + amount > 50000) return "Лимит займа 50 000 $.";
            g.Player.loan += amount; g.Player.cash += amount; return null;
        }

        public static string RepayLoan(GameState g, float amount)
        {
            amount = Mathf.Min(amount, g.Player.loan);
            if (g.Player.cash < amount) return "Недостаточно денег.";
            g.Player.loan -= amount; g.Player.cash -= amount; return null;
        }

        public static void AdvanceCity(GameState g)
        {
            int next = (g.cityIndex + 1) % GameData.Cities.Count;
            var n = GameState.NewGame(next, Mathf.Max(15000f, (g.Player.cash - g.Player.loan) * 0.3f + 15000f), g.Player.name);
            n.citiesCompleted = g.citiesCompleted + 1;
            n.AddNews("Новый город — " + n.City.name + ". Цель: репутация " + n.City.goalReputation + ", капитал " + n.City.goalCash + " $.");
            g.cityIndex = n.cityIndex; g.day = n.day; g.nextId = n.nextId; g.seed = n.seed;
            g.citiesCompleted = n.citiesCompleted; g.companies = n.companies; g.restaurants = n.restaurants;
            g.news = n.news; g.ingredientMul = n.ingredientMul; g.cashHistory = n.cashHistory;
            g.demandMul = 1f; g.eventDaysLeft = 0; g.won = false; g.bankrupt = false;
        }
    }
}
