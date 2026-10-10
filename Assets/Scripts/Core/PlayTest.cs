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
            for (int ci = 0; ci < GameData.Cities.Count; ci++)
            {
                g.StartNew(ci, "Bot"); yield return null;
                var s = g.State; int day0 = s.day;
                while (!s.won && !s.bankrupt && s.day < 700)
                {
                    if (s.day % 5 == 0) GoodBotTurn(s);
                    Simulation.Step(s);
                }
                var mine = s.RestaurantsOf(0);
                Debug.Log("[playtest] GOOD " + s.City.name + ": won=" + s.won + " bankrupt=" + s.bankrupt + " day=" + s.day + " restaurants=" + mine.Count +
                          " rep=" + Mathf.RoundToInt(mine.Average(r => r.reputation)) + " capital=" + Mathf.RoundToInt(s.Player.cash - s.Player.loan) +
                          " goal=" + s.City.goalReputation + "/" + s.City.goalCash);
                if (s.won) wins++;
                yield return null;
            }
            Debug.Log("[playtest] good bot won " + wins + "/" + GameData.Cities.Count);

            // bad player: takes max loan, builds big elite places, serves nothing
            g.StartNew(0, "Bad"); yield return null;
            var b = g.State;
            Simulation.TakeLoan(b, 50000);
            for (int d = 0; d < 4; d++) Simulation.Build(b, d, 4, 2);
            foreach (var r in b.RestaurantsOf(0)) foreach (var m in r.menu) m.enabled = false;
            while (!b.bankrupt && b.day < 1500) { Simulation.Step(b); if (b.day % 20 == 0) Simulation.TakeLoan(b, 50000); }
            Debug.Log("[playtest] BAD bankrupt=" + b.bankrupt + " day=" + b.day + " cash=" + Mathf.RoundToInt(b.Player.cash));
            Application.Quit();
        }

        static void GoodBotTurn(GameState s)
        {
            var mine = s.RestaurantsOf(0);
            // fair prices, wider menu, better staff
            foreach (var r in mine)
            {
                foreach (var m in r.menu) m.price = Mathf.Round(Simulation.FairPrice(s, m, r) * 0.95f * 10f) / 10f;
                if (r.menu.Count < 6) { var rec = GameData.Recipes[r.menu.Count % GameData.Recipes.Count]; if (!r.menu.Exists(x => x.name == rec.name)) r.menu.Add(s.DefaultItem(rec.id)); }
                var sz = GameData.Sizes[r.size]; if (r.last.unserved > r.last.customers * 0.5f && s.Player.cash > 40000) { if (r.cooks < sz.kitchenSlots) r.cooks++; if (r.waiters < sz.kitchenSlots + 2) r.waiters++; }
                r.marketing = s.Player.cash > 15000 ? 1 : 0; if (s.Player.cash > 40000) r.supplier = 2; if (s.Player.cash > 60000 && r.style < 3 && s.day % 15 == 0) Simulation.Upgrade(s, r, r.style + 1, r.size);
                if (r.skill < 5 && s.Player.cash > 8000) { s.Player.cash -= 600f * r.skill * (r.cooks + r.waiters) * 0.5f; r.skill++; }
            }
            // expand into best district with room
            int best = -1; float bs = -1;
            for (int d = 0; d < s.City.districts.Count; d++)
            {
                int n = s.restaurants.Count(r => r.district == d);
                if (n >= 4) continue;
                float sc = s.City.districts[d].population / (1f + n);
                if (sc > bs) { bs = sc; best = d; }
            }
            if (best >= 0 && mine.Count < 8 && s.Player.cash > s.BuildCost(best, 2, 1) * 1.4f) Simulation.Build(s, best, 2, 1);
        }
    }
}
