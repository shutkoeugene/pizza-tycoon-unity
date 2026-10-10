using System.Collections;
using System.Linq;
using UnityEngine;

namespace PizzaTycoon
{
    // "-playtest": a bot plays full games through the public player actions (no flags forced).
    public class PlayTest : MonoBehaviour
    {
        public IEnumerator Run()
        {
            var g = GameController.I;
            yield return null;
            int wins = 0;
            string only = null; foreach (var a in System.Environment.GetCommandLineArgs()) if (a.StartsWith("-playcity=")) only = a.Substring(10);
            for (int ci = 0; ci < GameData.Cities.Count; ci++)
            {
                if (only != null && GameData.Cities[ci].id != only) continue;
                g.StartNew(ci, "Bot"); yield return null;
                var s = g.State;
                while (!s.won && !s.bankrupt && s.day < 900)
                {
                    if (s.day % 3 == 0) GoodBotTurn(s);
                    Simulation.Step(s);
                    if (only != null && (s.day == 40 || s.day == 120 || s.day == 300)) foreach (var r in s.RestaurantsOf(0))
                        Debug.Log("[playtest]   day " + s.day + " " + r.name + " size=" + r.size + " style=" + r.style + " rev=" + Mathf.RoundToInt(r.last.revenue) + " ing=" + Mathf.RoundToInt(r.last.costIngredients) + " wages=" + Mathf.RoundToInt(r.last.costWages) + " rent=" + Mathf.RoundToInt(r.last.costRent) + " oth=" + Mathf.RoundToInt(r.last.costOther) + " mkt=" + Mathf.RoundToInt(r.last.costMarketing) + " cust=" + r.last.customers + " unserved=" + r.last.unserved + " deliv=" + r.deliveryOrders + " rep=" + Mathf.RoundToInt(r.reputation) + " cash=" + Mathf.RoundToInt(s.Player.cash) + " rating=" + string.Join(",", r.rating.Select(x => Mathf.RoundToInt(x * 100))));
                }
                var mine = s.RestaurantsOf(0);
                string pend = string.Join("; ", s.City.objectives.Where(o => !Simulation.IsDone(s, o)).Select(o => o.Text()));
                Debug.Log("[playtest] GOOD " + s.City.name + ": won=" + s.won + " bankrupt=" + s.bankrupt + " day=" + s.day + " restaurants=" + mine.Count +
                          " rep=" + (mine.Count > 0 ? Mathf.RoundToInt(mine.Average(r => r.reputation)) : 0) + " stars=" + mine.Sum(r => r.stars) + " hq=" + s.Player.hq +
                          " capital=" + Mathf.RoundToInt(s.Player.cash - s.Player.loan) + (pend.Length > 0 ? " PENDING: " + pend : ""));
                if (s.won) wins++;
                yield return null;
            }
            Debug.Log("[playtest] good bot won " + wins + "/" + GameData.Cities.Count);

            // bad player: max loan, big elite places, nothing on the menu
            g.StartNew(0, "Bad"); yield return null;
            var b = g.State;
            Simulation.TakeLoan(b, 50000);
            for (int d = 0; d < 4; d++) Simulation.Build(b, d, 4, 2);
            foreach (var r in b.RestaurantsOf(0)) foreach (var m in r.menu) m.enabled = false;
            while (!b.bankrupt && b.day < 1500) { Simulation.Step(b); if (b.day % 20 == 0) Simulation.TakeLoan(b, 50000); }
            Debug.Log("[playtest] BAD bankrupt=" + b.bankrupt + " day=" + b.day + " cash=" + Mathf.RoundToInt(b.Player.cash));
            Application.Quit();
        }

        static void Optimize(GameState s, Restaurant r, bool flagship)
        {
            var c = s.Player; var d = s.City.districts[r.district];
            int dom = flagship ? (int)Group.BigShots : Simulation.DominantGroup(d);
            var grp = GameData.Groups[dom];
            // menu: best recipes for the target group's taste (and in-fashion ingredients)
            if (r.menu.Count < 5 || s.day % 30 == 0)
            {
                var ranked = GameData.Recipes.OrderByDescending(rec => rec.ingredients.Count(id => GameData.Ingredients.Find(x => x.id == id).taste == grp.taste) * 2 + rec.ingredients.Count(id => s.trend.Contains(id))).ToList();
                foreach (var rec in ranked.Take(5)) if (!r.menu.Exists(x => x.name == rec.name)) { if (r.menu.Count >= 6) r.menu.RemoveAt(r.menu.Count - 1); r.menu.Add(s.DefaultItem(rec.id)); }
            }
            foreach (var m in r.menu) m.price = Mathf.Round(Simulation.FairPrice(s, m, r) * grp.priceLevel * 0.97f * 10f) / 10f;
            if (c.cash > 3000) r.manager = true;
            if (r.manager && c.cash > 10000 && r.supplier != 0) { r.mgrQuality = 2; r.mgrService = 2; }
            r.runners = Mathf.Max(r.runners, (r.cooks + 1) / 2);
            // ovens, music, layout, staff age
            if (c.cash > 12000 && r.oven < 3 && c.cash > Simulation.OvenPrice(r) * 2.5f) Simulation.BuyOven(s, r);
            int[] bestMusic = { 1, 1, 2, 2, 3, 2, 2, 3 };
            if (c.cash > 5000) Simulation.SetMusic(s, r, bestMusic[dom]);
            if (c.cash > 8000 && r.layout < 2 && (r.layout == 0 || c.hq >= 2)) Simulation.UpgradeLayout(s, r);
            int agePref = Simulation.AgePref[dom];
            if (c.cash > 9000 && Mathf.Abs(r.staffAge - agePref) >= 5) Simulation.RefreshStaffAge(s, r, r.staffAge > agePref ? -5 : 5);
            if (r.ratsDays > 0) Simulation.Disinfect(s, r);
            if (c.cash > 6000) r.wage = 2;
            if (c.cash > 30000) r.wage = 3;
            if (c.hq >= 2) r.guards = Mathf.Min(Simulation.GuardsNeeded(r), r.guards + 1);
            if (r.skill < 5 && c.cash > 9000) Simulation.Train(s, r, 0, true);
            if (r.health < 70 && c.cash > 5000) Simulation.Train(s, r, 1, true);
            if (flagship || c.cash > 40000) { if (c.cash > 20000) Simulation.BuyPlants(s, r); if (c.cash > 25000 && r.decor < 2) Simulation.UpgradeDecor(s, r); if (c.cash > 40000) Simulation.SetSupplier(r, 2); }
            if (r.condition < 60 && c.cash > 5000) Simulation.Restore(s, r);
            // style: flagship targets the Big Shots, others the dominant group
            int style = flagship ? 4 : grp.liked[0];
            if (r.style != style && c.cash > 15000) Simulation.ChangeStyle(s, r, style);
            r.marketing = c.cash > 15000 ? 1 : 0;
            r.cooks = Mathf.Min(GameData.Sizes[r.size].kitchenSlots, 1 + r.size + (c.cash > 20000 && r.last.unserved > r.last.customers * 0.4f ? 1 : 0));
            r.waiters = Mathf.Min(GameData.Sizes[r.size].kitchenSlots + 2, r.cooks + 1);
        }

        static void GoodBotTurn(GameState s)
        {
            var c = s.Player; var mine = s.RestaurantsOf(0);
            // flagship = restaurant in the richest district
            Restaurant flag = mine.OrderByDescending(r => s.City.districts[r.district].wealth).FirstOrDefault();
            bool wantStars = s.City.objectives.Any(o => o.kind == ObjKind.Stars);
            foreach (var r in mine) Optimize(s, r, wantStars && r == flag);
            // close chronically unprofitable places (keep at least 3)
            if (s.day % 15 == 0 && mine.Count > 3)
            {
                var worst = mine.Where(r => s.day - r.openedDay > 60 && r.last.Profit < -30f).OrderBy(r => r.last.Profit).FirstOrDefault();
                if (worst != null) { Simulation.Close(s, worst); mine = s.RestaurantsOf(0); }
            }
            // bribe the mayor when stars are needed; police when rich
            if (s.City.objectives.Any(o => o.kind == ObjKind.Stars) && c.cash > 8000) c.mayor = Mathf.Max(c.mayor, 1);
            if (c.cash > 20000 && s.day > 25) c.police = Mathf.Max(c.police, 1);
            // grow existing restaurants when rich
            foreach (var r in mine) if (r.size < 2 && c.cash > 45000 && s.day % 9 == 0) Simulation.Upgrade(s, r, r.style, r.size + 1);
            // headquarters
            if (c.hq < 4 && c.cash > GameState.HqCost[c.hq + 1] * 1.4f && (c.cash > 150000 || mine.Count >= GameState.MaxRestaurants(c.hq) - 1 && mine.Count >= 3 || s.City.objectives.Any(o => o.kind == ObjKind.Hq && o.value > c.hq && mine.Count >= 3))) Simulation.UpgradeHq(s);
            if (c.cash > 30000 && mine.Count > 2) { c.cityAd = 1; }
            if (c.cash > 50000 && c.warehouse < 2) Simulation.BuyWarehouse(s);
            // expand
            int target = s.City.objectives.Where(o => o.kind == ObjKind.Restaurants).Select(o => (int)o.value).DefaultIfEmpty(3).Max();
            if (mine.Count < Mathf.Max(target, 4) + 1 || (c.cash > 100000 && mine.Count < GameState.MaxRestaurants(c.hq)))
            {
                int best = -1; float bs = -1;
                for (int d = 0; d < s.City.districts.Count; d++)
                {
                    int n = s.restaurants.Count(r => r.district == d);
                    if (n >= 4) continue;
                    var dd = s.City.districts[d];
                    float sc = (dd.population * 0.6f + dd.wealth * (wantStars && mine.Count == 0 ? 1.5f : 1.0f)) / (1f + n);
                    if (sc > bs) { bs = sc; best = d; }
                }
                if (best >= 0)
                {
                    var bg = GameData.Groups[Simulation.DominantGroup(s.City.districts[best])];
                    var styles = new System.Collections.Generic.List<int>();
                    if (mine.Count == 0 && wantStars) styles.Add(4);
                    styles.Add(bg.liked[0]); if (bg.liked.Length > 1) styles.Add(bg.liked[1]); styles.AddRange(bg.ok);
                    bool built = false;
                    foreach (int size in new[] { 1, 0 })
                    {
                        foreach (int st in styles)
                        {
                            if (c.cash > s.BuildCost(best, st, size) * 1.25f && Simulation.Build(s, best, st, size) == null) { built = true; break; }
                        }
                        if (built) break;
                    }
                }
            }
            // outsell the best rival: delivery uses spare capacity
            foreach (var r in mine) r.delivery = c.cash > 8000;
        }
    }
}
