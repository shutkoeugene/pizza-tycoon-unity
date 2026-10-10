using System.Collections.Generic;
using UnityEngine;

namespace PizzaTycoon
{
    public static class Simulation
    {
        public static readonly float[] AdCost = { 0, 50, 150, 400 };
        static readonly float[] AdBoost = { 0f, 0.15f, 0.4f, 0.8f };
        public static readonly float[] CityAdCost = { 0, 120, 320, 800 };
        static readonly float[] CityAdBoost = { 0f, 0.12f, 0.3f, 0.6f };
        public static readonly float[] MayorCost = { 0, 90, 220 };
        static readonly float[] MayorBoost = { 1f, 1.06f, 1.12f };
        public static readonly float[] WarehouseCost = { 0, 6000, 12000 };
        static readonly float[] WageMotivation = { 30f, 55f, 80f, 95f };

        public static readonly string[] SabotageNames = { "Подбросить крыс", "Вызвать санинспекцию", "Налёт полиции", "Нанять головорезов", "Поджог" };
        public static readonly float[] SabotageCost = { 800, 500, 1500, 3500, 9000 };
        public static readonly int[] SabotageHq = { 3, 1, 3, 3, 4 };

        public static float MarketingPrice(int level) => AdCost[level];
        public static int GuardsNeeded(Restaurant r) => r.size + 1;

        // ------------------------------------------------------------ pricing & ratings
        public static float FairPrice(GameState g, MenuItem m, Restaurant r)
        {
            var d = g.City.districts[r.district];
            return g.ItemCost(m, r.supplier) * 4.2f * (0.75f + d.wealth * 0.6f);
        }

        public static float TastePoints(GameState g, MenuItem m, int gi, DistrictDef d)
        {
            var grp = GameData.Groups[gi];
            float match = 0.5f;
            foreach (var id in m.ingredients)
            {
                var ing = GameData.Ingredients.Find(x => x.id == id);
                if (ing.taste == grp.taste) match += 0.12f;
                if (ing.taste == d.preference) match += 0.04f;
                if (g.trend.Contains(id)) match += 0.05f;
            }
            return Mathf.Min(match, 1.15f);
        }

        public static float PriceScore(GameState g, MenuItem m, Restaurant r, int gi)
        {
            var grp = GameData.Groups[gi];
            float want = FairPrice(g, m, r) * grp.priceLevel;
            float ratio = m.price / Mathf.Max(0.3f, want);
            if (ratio < 1f) return 1f - grp.cheapPenalty * (1f - ratio);
            return Mathf.Clamp01(Mathf.Pow(1f / ratio, 1.6f));
        }

        public static float MenuScore(GameState g, Restaurant r, int gi)
        {
            var d = g.City.districts[r.district];
            float sum = 0; int n = 0;
            foreach (var m in r.menu)
            {
                if (!m.enabled || m.ingredients.Count == 0) continue;
                float q = GameState.ItemQuality(m, r.supplier);
                sum += 0.35f * q + 0.35f * TastePoints(g, m, gi, d) + 0.3f * PriceScore(g, m, r, gi);
                n++;
            }
            if (n == 0) return 0;
            float variety = Mathf.Min(1.2f, 0.8f + n * 0.08f);
            return Mathf.Clamp01(sum / n * variety);
        }

        public static float DecorScore(Restaurant r, int gi)
        {
            var grp = GameData.Groups[gi];
            float fit = System.Array.IndexOf(grp.liked, r.style) >= 0 ? 1f : System.Array.IndexOf(grp.ok, r.style) >= 0 ? 0.72f : 0.35f;
            if (r.plants && fit < 1f) fit = Mathf.Min(1f, fit + 0.12f);
            float mul = r.decor == 0 ? 0.9f : r.decor == 1 ? 1f : 1.1f;
            float cond = 0.6f + 0.4f * r.condition / 100f;
            float clean = 0.6f + 0.4f * r.cleanliness / 100f;
            return Mathf.Clamp01(fit * mul * cond * clean + (r.plants ? 0.05f : 0f));
        }

        public static float StaffScore(GameState g, Restaurant r, int gi)
        {
            var grp = GameData.Groups[gi];
            float skill = 0.45f + r.skill * 0.1f;
            float hm = 0.55f + 0.45f * (r.health + r.motivation) / 200f;
            float adj = r.manager ? 0.08f : -0.12f;
            int need = GuardsNeeded(r);
            if (g.companies[r.owner].hq >= 2)
            {
                if (r.guards * 2 < need) adj -= 0.08f;
                else if (r.guards <= need) adj += 0.03f * grp.guardSensitivity;
                else adj -= grp.guardSensitivity * 0.06f * (r.guards - need);
            }
            return Mathf.Clamp01(skill * hm + adj);
        }

        public static float Rating(GameState g, Restaurant r, int gi)
            => (MenuScore(g, r, gi) + DecorScore(r, gi) + StaffScore(g, r, gi)) / 3f;

        public static float WeightedRating(GameState g, Restaurant r)
        {
            var mix = g.City.districts[r.district].mix; float w = 0;
            for (int i = 0; i < 8; i++) w += mix[i] * r.rating[i];
            return w;
        }

        static float AdFactor(GameState g, Restaurant r, int gi)
        {
            var c = g.companies[r.owner];
            float branch = AdBoost[r.marketing];
            if (r.marketing > 0 && r.adTarget >= 0) branch *= gi == r.adTarget ? 1.6f : 0.6f;
            float city = CityAdBoost[c.cityAd] * (c.hq >= 4 ? 1.2f : 1f);
            if (c.cityAd > 0 && c.cityAdTarget >= 0) city *= gi == c.cityAdTarget ? 1.6f : 0.6f;
            float boost = Mathf.Max(branch, city) + 0.25f * Mathf.Min(branch, city);
            if (r.marketing > 0 && r.adPromise > 0) boost *= PromiseTrue(g, r) ? 1.15f : 0.4f;
            return 1f + boost;
        }

        public static bool PromiseTrue(GameState g, Restaurant r)
        {
            switch (r.adPromise)
            {
                case 1:
                    { float q = 0; int n = 0; foreach (var m in r.menu) if (m.enabled) { q += GameState.ItemQuality(m, r.supplier); n++; } return n > 0 && q / n >= 0.65f; }
                case 2:
                    { int ok = 0, n = 0; foreach (var m in r.menu) if (m.enabled) { n++; if (m.price <= FairPrice(g, m, r)) ok++; } return n > 0 && ok * 2 >= n; }
                case 3:
                    { float s = 0; for (int i = 0; i < 8; i++) s += StaffScore(g, r, i); return s / 8f >= 0.68f; }
            }
            return true;
        }

        public static float Attraction(GameState g, Restaurant r, int gi)
        {
            var c = g.companies[r.owner];
            float rep = 0.4f + r.reputation / 100f * 1.2f;
            float star = 1f + 0.12f * r.stars;
            float mayor = c.cash >= 0 ? MayorBoost[c.mayor] : 1f;
            return r.rating[gi] * rep * AdFactor(g, r, gi) * star * mayor;
        }

        public static float Attraction(GameState g, Restaurant r)   // average, for UI/AI
        {
            var mix = g.City.districts[r.district].mix; float a = 0;
            for (int i = 0; i < 8; i++) a += mix[i] * Attraction(g, r, i);
            return a;
        }

        static float CityAdSpill(Company c) => 1f + CityAdBoost[c.cityAd] * 0.5f;

        // ------------------------------------------------------------ day step
        public static void Step(GameState g)
        {
            var rng = new System.Random(g.seed + g.day * 7919);
            var city = g.City;

            for (int i = 0; i < g.ingredientMul.Count; i++)
                g.ingredientMul[i] = Mathf.Clamp(g.ingredientMul[i] + ((float)rng.NextDouble() - 0.5f) * 0.06f, 0.7f, 1.8f);
            if (g.eventDaysLeft > 0) { g.eventDaysLeft--; if (g.eventDaysLeft == 0) g.demandMul = 1f; }
            else if (rng.NextDouble() < 0.04)
            {
                string[] ev = { "Футбольный матч в городе: спрос вырос!", "Проливные дожди: люди сидят дома.", "Городской фестиваль: все на улицах!", "Жара: пиццу заказывают меньше." };
                float[] mul = { 1.35f, 0.75f, 1.5f, 0.85f };
                int k = rng.Next(ev.Length);
                g.demandMul = mul[k]; g.eventDaysLeft = 3 + rng.Next(3); g.eventText = ev[k];
                g.AddNews(ev[k]);
            }
            if (g.day % 30 == 0) { g.RollTrends(rng); g.AddNews("В моде: " + string.Join(", ", g.trend.ConvertAll(id => GameData.Ingredients.Find(x => x.id == id).name)) + "."); }
            if (g.day % 60 == 0) CityDevelopment(g, rng);

            // ratings and attractions
            var attr = new Dictionary<int, float[]>();
            var distAttr = new float[city.districts.Count, 8];
            foreach (var r in g.restaurants)
            {
                for (int gi = 0; gi < 8; gi++) r.rating[gi] = Rating(g, r, gi);
                var a = new float[8];
                for (int gi = 0; gi < 8; gi++)
                {
                    a[gi] = r.closedDays > 0 ? 0f : Attraction(g, r, gi);
                    distAttr[r.district, gi] += a[gi];
                }
                attr[r.id] = a;
            }

            float weekday = (g.day % 7 == 5 || g.day % 7 == 6) ? 1.25f : 1f;
            const float K = 0.5f;
            int playerCust = 0; var rivalCust = new float[g.companies.Count];
            foreach (var r in g.restaurants)
            {
                var d = city.districts[r.district];
                var taken = new float[8];
                float baseDem = (100f + 520f * d.population) * city.baseDemand * g.demandMul * weekday * (0.6f + d.wealth * 0.5f);
                for (int gi = 0; gi < 8; gi++)
                {
                    taken[gi] = baseDem * d.mix[gi] * attr[r.id][gi] / (distAttr[r.district, gi] + K);
                    for (int j = 0; j < city.districts.Count; j++)
                    {
                        if (j == r.district) continue;
                        var cd = city.districts[j];
                        if (Mathf.Abs(cd.cell.x - d.cell.x) + Mathf.Abs(cd.cell.y - d.cell.y) != 1) continue;
                        float dm = (100f + 520f * cd.population) * city.baseDemand * g.demandMul * weekday * 0.2f * cd.mix[gi];
                        taken[gi] += dm * attr[r.id][gi] / (distAttr[j, gi] + attr[r.id][gi] + K);
                    }
                }
                RunDay(g, r, taken, rng);
                if (r.owner == 0) playerCust += r.last.customers + r.deliveryOrders; else rivalCust[r.owner] += r.last.customers + r.deliveryOrders;
            }
            g.lastPlayerCustomers = playerCust;
            float best = 0; for (int i = 1; i < rivalCust.Length; i++) best = Mathf.Max(best, rivalCust[i]);
            g.lastBestRivalCustomers = Mathf.RoundToInt(best);

            // company finance
            for (int ci = 0; ci < g.companies.Count; ci++)
            {
                var c = g.companies[ci];
                if (c.loan > 0) c.cash -= c.loan * 0.0006f;
                int nrest = g.restaurants.FindAll(x => x.owner == ci).Count;
                if (nrest > 0)
                {
                    c.cash -= GameState.HqUpkeep(c.hq);
                    c.cash -= CityAdCost[c.cityAd] + MayorCost[c.mayor];
                }
            }
            foreach (var r in g.restaurants) g.companies[r.owner].cash += r.last.Profit;

            Incidents(g, rng);
            StaffAndStars(g, rng);
            RunCompetitors(g, rng);

            g.cashHistory.Add(g.Player.cash);
            if (g.cashHistory.Count > 120) g.cashHistory.RemoveAt(0);
            g.day++;

            if (g.Player.cash < -8000 && g.Player.loan >= 49000) { g.bankrupt = true; g.AddNews("Вы обанкротились."); }
            if (!g.won && g.day >= 90 && AllObjectivesDone(g))
            {
                g.won = true;
                g.AddNews("Задачи города выполнены! Можно перейти в следующий город.");
            }
        }

        static void CityDevelopment(GameState g, System.Random rng)
        {
            int di = rng.Next(g.City.districts.Count);
            var d = g.City.districts[di];
            float dw = ((float)rng.NextDouble() - 0.4f) * 0.3f, dp = ((float)rng.NextDouble() - 0.4f) * 0.3f;
            d.wealth = Mathf.Clamp(d.wealth + dw, 0.05f, 1f); d.population = Mathf.Clamp(d.population + dp, 0.2f, 1f);
            d.rentFactor = 0.6f + d.wealth * 0.6f + d.population * 0.6f;
            d.mix = GameData.MixFor(d);
            g.ModDistrict(di, d.wealth, d.population);
            g.AddNews("Решение мэра: район «" + d.name + "» перестраивается, профиль клиентов изменился.");
        }

        static void RunDay(GameState g, Restaurant r, float[] takenByGroup, System.Random rng)
        {
            var s = new DayStats { day = g.day };
            var size = GameData.Sizes[r.size];
            var company = g.companies[r.owner];
            float wanted = 0; foreach (var t in takenByGroup) wanted += t;
            r.deliveryOrders = 0;

            if (r.closedDays > 0)
            {
                r.closedDays--;
                s.costWages = (r.cooks + r.waiters) * g.StaffWage(r.skill, r.wage) * 0.6f;
                s.costRent = g.DailyRent(r);
                s.costOther = 25f;
                r.last = s; AddTotal(r, s); return;
            }

            float skill = 0.8f + r.skill * 0.12f;
            int cooks = Mathf.Min(r.cooks, size.kitchenSlots);
            float cap = Mathf.Min(cooks * 95f * skill, r.waiters * 85f * skill, size.seats * 7f);
            cap *= r.shifts >= 3 ? 1.35f : 1f;
            cap *= r.manager ? 1f : 0.85f;
            cap *= 0.7f + 0.3f * Mathf.Clamp(r.motivation, 0f, 100f) / 100f;
            if (r.trainDays > 0) { cap *= 0.5f; r.trainDays--; }

            int enabled = r.menu.FindAll(m => m.enabled && m.ingredients.Count > 0).Count;
            if (enabled == 0) wanted = 0;

            float served = Mathf.Min(wanted, cap) * (0.93f + (float)rng.NextDouble() * 0.14f);
            served = Mathf.Min(served, cap);
            s.customers = Mathf.RoundToInt(served);
            s.unserved = Mathf.RoundToInt(Mathf.Max(0, wanted - served));

            if (r.delivery && enabled > 0)
            {
                float spare = Mathf.Max(0, cap - served);
                float avgA = 0; for (int i = 0; i < 8; i++) avgA += r.rating[i] / 8f;
                r.deliveryOrders = Mathf.RoundToInt(Mathf.Min(spare * 0.8f, (25f + 60f * avgA) * CityAdSpill(company)));
            }

            float tot = 0; var w = new List<float>();
            var d = g.City.districts[r.district];
            foreach (var m in r.menu)
            {
                float x = 0;
                if (m.enabled && m.ingredients.Count > 0)
                {
                    for (int gi = 0; gi < 8; gi++)
                        x += d.mix[gi] * (0.3f + 0.5f * TastePoints(g, m, gi, d) + 0.5f * PriceScore(g, m, r, gi));
                    x *= 0.6f + GameState.ItemQuality(m, r.supplier);
                }
                w.Add(x); tot += x;
            }
            float loyalty = GameState.Loyalty(r);
            float waste = (r.manager ? 0.03f : 0.09f) * (1f - 0.4f * company.warehouse) * (r.supplier == 0 ? 1.5f : 1f);
            int orders = s.customers + r.deliveryOrders;
            for (int i = 0; i < r.menu.Count; i++)
            {
                if (tot <= 0) break;
                int n = Mathf.RoundToInt(orders * 1.5f * w[i] / tot);
                s.pizzas += n;
                s.revenue += n * r.menu[i].price;
                s.costIngredients += n * g.ItemCost(r.menu[i], r.supplier, loyalty) * (1f + waste);
            }
            float shiftMul = r.shifts >= 3 ? 1.5f : 1f;
            s.costWages = (r.cooks + r.waiters) * g.StaffWage(r.skill, r.wage) * shiftMul
                          + (r.manager ? 60f + r.skill * 6f : 0f)
                          + r.guards * GameState.GuardWage(r.guardSkill)
                          + (r.delivery ? 60f : 0f);
            s.costRent = g.DailyRent(r);
            s.costMarketing = AdCost[r.marketing];
            int chain = g.restaurants.FindAll(x => x.owner == r.owner).Count;
            s.costOther = 25f + size.seats * 0.8f + 45f * (chain - 1);
            // progressive profit tax keeps late-game fortunes in check
            float preTax = s.revenue - s.costIngredients - s.costWages - s.costRent - s.costMarketing - s.costOther;
            if (preTax > 800f) s.costOther += 0.35f * (preTax - 800f);

            float satisfaction = 50f;
            if (s.customers + s.unserved > 0)
            {
                float servedRatio = s.customers / (float)(s.customers + s.unserved);
                satisfaction = servedRatio * 35f + WeightedRating(g, r) * 65f;
            }
            r.reputation = Mathf.Clamp(r.reputation + (satisfaction - r.reputation) * 0.05f, 0, 100);
            if (r.marketing > 0 && r.adPromise > 0 && !PromiseTrue(g, r)) r.reputation = Mathf.Max(0, r.reputation - 0.4f);
            float cleanRate = (r.waiters * r.skill * 1.2f) * (r.manager ? 1.15f : 0.9f);
            r.cleanliness = Mathf.Clamp(r.cleanliness - s.customers * 0.02f + cleanRate, 20, 100);
            r.condition = Mathf.Clamp(r.condition - 0.03f - s.customers * 0.0012f, 15, 100);

            r.last = s; AddTotal(r, s);
        }

        static void AddTotal(Restaurant r, DayStats s)
        {
            r.total.customers += s.customers; r.total.pizzas += s.pizzas; r.total.revenue += s.revenue;
            r.total.costIngredients += s.costIngredients; r.total.costWages += s.costWages;
            r.total.costRent += s.costRent; r.total.costMarketing += s.costMarketing; r.total.costOther += s.costOther;
        }

        static void Incidents(GameState g, System.Random rng)
        {
            foreach (var r in g.restaurants.ToArray())
            {
                bool mine = r.owner == 0;
                if (g.day > 25 && rng.NextDouble() < 0.012f * Mathf.Pow(0.65f, r.guards))
                {
                    float cost = 200 + (float)rng.NextDouble() * 600;
                    g.companies[r.owner].cash -= cost; r.reputation = Mathf.Max(0, r.reputation - 3);
                    if (mine) g.AddNews("Рэкетиры разгромили зал в «" + r.name + "»: убыток " + UIKit.Money(cost) + ". Наймите охрану.");
                }
                if (rng.NextDouble() < 0.012f && r.cleanliness < 60f && g.companies[r.owner].mayor < 2)
                {
                    float fine = 600 + (float)rng.NextDouble() * 400;
                    g.companies[r.owner].cash -= fine; r.reputation = Mathf.Max(0, r.reputation - 4);
                    if (mine) g.AddNews("Санинспекция оштрафовала «" + r.name + "» на " + UIKit.Money(fine) + ": грязно!");
                }
            }
        }

        static void StaffAndStars(GameState g, System.Random rng)
        {
            foreach (var r in g.restaurants)
            {
                float target = WageMotivation[Mathf.Clamp(r.wage, 0, 3)] + (r.manager ? 5f : 0f);
                r.motivation = Mathf.Clamp(r.motivation + (target - r.motivation) * 0.1f, -20f, 100f);
                r.health = Mathf.Clamp(r.health + (r.shifts >= 3 ? -0.4f : 0.12f), 10f, 100f);
                r.supplierDays++;
                if (r.closedDays > 0) continue;

                float bs = r.rating[(int)Group.BigShots];
                bool ok = r.stars == 0 ? bs >= 0.78f : bs >= 0.86f;
                if (r.stars < 2)
                {
                    if (ok) r.starProgress++; else r.starProgress = Mathf.Max(0, r.starProgress - 1);
                    int need = r.stars == 0 ? 15 : 25;
                    if (r.starProgress >= need)
                    {
                        r.stars++; r.starProgress = 0;
                        if (r.owner == 0) g.AddNews("Мэр присвоил ресторану «" + r.name + "» гурманскую звезду! (" + r.stars + ")");
                    }
                }
                if (r.stars > 0 && bs < 0.70f)
                {
                    r.starLoss++;
                    if (r.starLoss >= 10) { r.stars--; r.starLoss = 0; if (r.owner == 0) g.AddNews("Мэр отобрал звезду у «" + r.name + "»: богачам больше не нравится."); }
                }
                else r.starLoss = 0;
            }
        }

        // ------------------------------------------------------------ competitors
        public static int DominantGroup(DistrictDef d)
        {
            int b = 0; for (int i = 1; i < 8; i++) if (d.mix[i] > d.mix[b]) b = i; return b;
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

                if (c.hq >= 3 && c.cash > 25000 && rng.NextDouble() < 0.02f * c.aggression && g.day > 40)
                {
                    var targets = g.RestaurantsOf(0);
                    if (targets.Count > 0)
                    {
                        int kind = rng.Next(0, c.hq >= 4 && c.aggression > 1.2f ? 5 : 3);
                        Sabotage(g, ci, targets[rng.Next(targets.Count)], kind);
                    }
                }

                if (g.day % 4 != ci % 4) continue;

                foreach (var r in mine)
                {
                    var d = g.City.districts[r.district];
                    foreach (var m in r.menu)
                    {
                        float fair = FairPrice(g, m, r) * GameData.Groups[DominantGroup(d)].priceLevel;
                        float target = fair * (0.95f + (float)rng.NextDouble() * 0.2f);
                        m.price = Mathf.Round(Mathf.Lerp(m.price, target, 0.5f) * 10f) / 10f;
                    }
                    r.marketing = c.cash > 20000 ? Mathf.Min(3, Mathf.FloorToInt(c.aggression * 1.5f)) : 0;
                    if (c.cash > 10000) r.manager = true;
                    if (c.hq >= 2) r.guards = Mathf.Min(GuardsNeeded(r), r.guards + 1);
                    if (c.cash > 12000 && r.skill < 4 && rng.NextDouble() < 0.1) { r.skill++; c.cash -= 600f * r.skill; }
                    if (c.cash > 30000 && r.decor < 2 && rng.NextDouble() < 0.1) { r.decor++; c.cash -= 2500f * (r.size + 1); }
                    if (c.cash > 40000 && c.aggression > 1f) r.supplier = 2;
                    if (r.menu.Count < 5 && rng.NextDouble() < 0.2)
                    {
                        var rec = GameData.Recipes[rng.Next(GameData.Recipes.Count)];
                        if (!r.menu.Exists(m => m.name == rec.name)) r.menu.Add(g.DefaultItem(rec.id));
                    }
                }
                if (c.hq < 4 && c.cash > GameState.HqCost[c.hq + 1] * 1.8f && rng.NextDouble() < 0.3f) { c.cash -= GameState.HqCost[c.hq + 1]; c.hq++; }
                if (mine.Count > 2 && c.cash > 30000 && c.cityAd == 0 && rng.NextDouble() < 0.2f) c.cityAd = 1;

                if (rng.NextDouble() < 0.25 * c.aggression && mine.Count < GameState.MaxRestaurants(c.hq))
                {
                    int best = -1; float bestScore = -1;
                    for (int d = 0; d < g.City.districts.Count; d++)
                    {
                        int count = g.restaurants.FindAll(r => r.district == d).Count;
                        if (count >= 4) continue;
                        float score = g.City.districts[d].population * (0.5f + g.City.districts[d].wealth) / (1f + count * 1.5f);
                        if (score > bestScore) { bestScore = score; best = d; }
                    }
                    if (best >= 0)
                    {
                        var grp = GameData.Groups[DominantGroup(g.City.districts[best])];
                        int style = grp.liked[0];
                        if (c.cash < 25000 && style > 2) style = grp.ok.Length > 0 ? grp.ok[0] : 1;
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
        }

        // ------------------------------------------------------------ player & shared actions
        public static string Build(GameState g, int district, int style, int size)
        {
            float cost = g.BuildCost(district, style, size);
            if (g.restaurants.FindAll(r => r.owner == 0).Count >= GameState.MaxRestaurants(g.Player.hq)) return "Лимит ресторанов для этого уровня штаба. Улучшите штаб.";
            if (g.Player.cash < cost) return "Недостаточно денег.";
            if (g.restaurants.FindAll(r => r.district == district).Count >= 4) return "В районе уже нет места.";
            g.Player.cash -= cost;
            var nr = g.CreateRestaurant(0, district, style, size);
            nr.reputation = 25f;
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

        public static string UpgradeHq(GameState g)
        {
            var c = g.Player;
            if (c.hq >= 4) return "Штаб уже на максимальном уровне.";
            float cost = GameState.HqCost[c.hq + 1];
            if (c.cash < cost) return "Недостаточно денег (" + UIKit.Money(cost) + ").";
            c.cash -= cost; c.hq++;
            g.AddNews("Штаб улучшен до уровня " + c.hq + ".");
            return null;
        }

        public static string BuyWarehouse(GameState g)
        {
            var c = g.Player;
            if (c.warehouse >= 2) return "Склад уже улучшен полностью.";
            float cost = WarehouseCost[c.warehouse + 1];
            if (c.cash < cost) return "Недостаточно денег (" + UIKit.Money(cost) + ").";
            c.cash -= cost; c.warehouse++; return null;
        }

        public static float TrainCost(Restaurant r, int kind, bool offShift)
        {
            int staff = r.cooks + r.waiters;
            float cost = kind == 0 ? 600f * r.skill * staff * 0.5f : 150f * staff;
            return offShift ? cost * 1.5f : cost;
        }

        public static string Train(GameState g, Restaurant r, int kind, bool offShift)
        {
            float cost = TrainCost(r, kind, offShift);
            if (kind == 0 && r.skill >= 5) return "Навык уже максимальный.";
            if (kind == 1 && r.health >= 99f) return "Здоровье в норме.";
            if (g.Player.cash < cost) return "Недостаточно денег (" + UIKit.Money(cost) + ").";
            g.Player.cash -= cost;
            if (kind == 0) r.skill++; else r.health = Mathf.Min(100f, r.health + 25f);
            if (!offShift) r.trainDays = 3;
            return null;
        }

        public static float StyleCost(GameState g, Restaurant r, int style) => style == r.style ? 0f : 0.35f * g.BuildCost(r.district, style, r.size);
        public static string ChangeStyle(GameState g, Restaurant r, int style)
        {
            if (style == r.style) return null;
            float cost = StyleCost(g, r, style);
            if (g.Player.cash < cost) return "Недостаточно денег (" + UIKit.Money(cost) + ").";
            g.Player.cash -= cost; r.style = style; return null;
        }

        public static float RestoreCost(Restaurant r) => 500f * (r.size + 1);
        public static string Restore(GameState g, Restaurant r)
        {
            float cost = RestoreCost(r);
            if (g.Player.cash < cost) return "Недостаточно денег (" + UIKit.Money(cost) + ").";
            g.Player.cash -= cost; r.condition = 100f; return null;
        }

        public static float DecorCost(Restaurant r) => (r.decor == 0 ? 2500f : 7000f) * (r.size + 1);
        public static string UpgradeDecor(GameState g, Restaurant r)
        {
            if (r.decor >= 2) return "Интерьер уже роскошный.";
            float cost = DecorCost(r);
            if (g.Player.cash < cost) return "Недостаточно денег (" + UIKit.Money(cost) + ").";
            g.Player.cash -= cost; r.decor++; return null;
        }

        public static string BuyPlants(GameState g, Restaurant r)
        {
            if (r.plants) return "Растения уже есть.";
            if (g.Player.cash < 400) return "Недостаточно денег.";
            g.Player.cash -= 400; r.plants = true; return null;
        }

        public static void SetSupplier(Restaurant r, int s)
        {
            if (r.supplier != s) { r.supplier = s; r.supplierDays = 0; }
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

        // kind: see SabotageNames. Returns a message for the UI.
        public static string Sabotage(GameState g, int attacker, Restaurant target, int kind)
        {
            var a = g.companies[attacker];
            if (a.hq < SabotageHq[kind]) return "Нужен штаб уровня " + SabotageHq[kind] + ".";
            if (a.cash < SabotageCost[kind]) return "Недостаточно денег (" + UIKit.Money(SabotageCost[kind]) + ").";
            a.cash -= SabotageCost[kind];
            var rng = new System.Random(g.seed + g.day * 31 + target.id * 17 + kind);
            float success = 0.85f * Mathf.Pow(0.75f, target.guards);
            float detect = 0.2f + 0.05f * target.guards;
            string victimMsg = null, result;
            bool ok = rng.NextDouble() < success;
            bool mine = target.owner == 0;
            if (!ok) result = "Акция провалилась: охрана справилась.";
            else
            {
                switch (kind)
                {
                    case 0: target.cleanliness = Mathf.Max(20, target.cleanliness - 35); target.reputation = Mathf.Max(0, target.reputation - 4); victimMsg = "В «" + target.name + "» обнаружены крысы!"; result = "Крысы подброшены."; break;
                    case 1:
                        if (target.cleanliness < 75 && g.companies[target.owner].mayor < 2) { g.companies[target.owner].cash -= 1500; target.closedDays = Mathf.Max(target.closedDays, 2); target.reputation = Mathf.Max(0, target.reputation - 5); victimMsg = "Санинспекция закрыла «" + target.name + "» на 2 дня."; result = "Санинспекция закрыла ресторан."; }
                        else result = "Санинспекция ничего не нашла.";
                        break;
                    case 2:
                        if (g.companies[target.owner].mayor >= 2) result = "Мэр покрыл жертву: налёт отменён.";
                        else { target.closedDays = Mathf.Max(target.closedDays, 3); target.reputation = Mathf.Max(0, target.reputation - 8); victimMsg = "Полиция устроила налёт на «" + target.name + "»."; result = "Налёт полиции удался."; }
                        break;
                    case 3: target.motivation = Mathf.Max(-20, target.motivation - 35); target.health = Mathf.Max(10, target.health - 25); target.cleanliness = Mathf.Max(20, target.cleanliness - 20); target.reputation = Mathf.Max(0, target.reputation - 8); target.closedDays = Mathf.Max(target.closedDays, 1); victimMsg = "Головорезы напали на персонал «" + target.name + "»."; result = "Головорезы поработали."; break;
                    default: target.closedDays = Mathf.Max(target.closedDays, 12); target.condition = Mathf.Max(15, target.condition - 60); target.reputation = Mathf.Max(0, target.reputation - 20); victimMsg = "Пожар в «" + target.name + "»!"; result = "Ресторан подожжён."; break;
                }
            }
            if (mine && victimMsg != null) g.AddNews(victimMsg);
            if (rng.NextDouble() < detect)
            {
                a.cash -= 3000;
                foreach (var r in g.RestaurantsOf(attacker)) r.reputation = Mathf.Max(0, r.reputation - 6);
                g.AddNews(a.name + " уличён в саботаже против «" + target.name + "»: штраф $3 000.");
                result += " Вас поймали! Штраф $3 000.";
            }
            return result;
        }

        // ------------------------------------------------------------ objectives
        public static bool IsDone(GameState g, Objective o)
        {
            var mine = g.RestaurantsOf(0);
            switch (o.kind)
            {
                case ObjKind.Restaurants: return mine.Count >= o.value;
                case ObjKind.Reputation: { float rep = 0; foreach (var r in mine) rep += r.reputation; return mine.Count > 0 && rep / mine.Count >= o.value; }
                case ObjKind.Capital: return g.Player.cash - g.Player.loan >= o.value;
                case ObjKind.Stars: { int st = 0; foreach (var r in mine) st += r.stars; return st >= o.value; }
                case ObjKind.Hq: return g.Player.hq >= o.value;
                default: return g.lastPlayerCustomers > 0 && g.lastPlayerCustomers >= g.lastBestRivalCustomers;
            }
        }

        public static bool AllObjectivesDone(GameState g)
        {
            foreach (var o in g.City.objectives) if (!IsDone(g, o)) return false;
            return true;
        }

        public static void AdvanceCity(GameState g)
        {
            int next = (g.cityIndex + 1) % GameData.Cities.Count;
            var n = GameState.NewGame(next, Mathf.Max(15000f, (g.Player.cash - g.Player.loan) * 0.3f + 15000f), g.Player.name);
            n.citiesCompleted = g.citiesCompleted + 1;
            n.AddNews("Новый город — " + n.City.name + ". Откройте «Цель», чтобы увидеть задачи.");
            g.cityIndex = n.cityIndex; g.day = n.day; g.nextId = n.nextId; g.seed = n.seed;
            g.citiesCompleted = n.citiesCompleted; g.companies = n.companies; g.restaurants = n.restaurants;
            g.news = n.news; g.ingredientMul = n.ingredientMul; g.cashHistory = n.cashHistory; g.trend = n.trend;
            g.demandMul = 1f; g.eventDaysLeft = 0; g.won = false; g.bankrupt = false; g.mods = n.mods;
        }
    }
}
