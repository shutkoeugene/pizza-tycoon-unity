using System;
using System.Collections;
using UnityEngine;

namespace PizzaTycoon
{
    // Run the built game with "-selftest" to smoke-test every city, tab and the simulation.
    public class SelfTest : MonoBehaviour
    {
        public IEnumerator Run()
        {
            var g = GameController.I; var ui = g.UI;
            int errors = 0;
            Application.logMessageReceived += (c, s, t) => { if (t == LogType.Exception || t == LogType.Error) errors++; };
            for (int ci = 0; ci < GameData.Cities.Count; ci++)
            {
                g.StartNew(ci, "Test");
                yield return null;
                for (int d = 0; d < g.State.City.districts.Count && d < 3; d++)
                {
                    g.SelectDistrict(d);
                    Debug.Log("[selftest] " + Simulation.Build(g.State, d, d % 5, d % 3));
                    g.RefreshWorld();
                    yield return null;
                }
                g.State.Player.cash += 0;
                foreach (var tab in new[] { "Рестораны", "Меню", "Персонал", "Интерьер", "Реклама", "Финансы", "Рынок", "Конкуренты", "Штаб", "Клиенты", "Новости" })
                { ui.OpenTab(tab); yield return null; }
                ui.CloseWindow();
                for (int day = 0; day < 120; day++) Simulation.Step(g.State);
                ui.OpenTab("Финансы"); yield return null; ui.CloseWindow();
                var p = g.State.RestaurantsOf(0);
                float profit = 0; foreach (var r in p) profit += r.last.Profit;
                foreach (var r in p) Debug.Log("[selftest]   rev=" + Mathf.RoundToInt(r.last.revenue) + " cust=" + r.last.customers + " unserved=" + r.last.unserved + " profit=" + Mathf.RoundToInt(r.last.Profit) + " rep=" + Mathf.RoundToInt(r.reputation));
                Debug.Log("[selftest] " + g.State.City.name + " day=" + g.State.day + " cash=" + Mathf.RoundToInt(g.State.Player.cash) + " lastDayProfit=" + Mathf.RoundToInt(profit) + " won=" + g.State.won);
                g.State.Save(2); var l = GameState.Load(2);
                if (l == null || l.day != g.State.day) { Debug.LogError("[selftest] save/load mismatch"); }
                if (g.State.won) { Simulation.AdvanceCity(g.State); g.RefreshWorld(); yield return null; }
            }
            // exotic toppings: goths love them, the rich recoil
            g.StartNew(0, "Exo"); yield return null;
            Simulation.Build(g.State, 0, 1, 1);
            var er = g.State.RestaurantsOf(0)[0];
            er.menu.Clear(); er.menu.Add(g.State.DefaultItem("margh"));
            float gothN = Simulation.MenuScore(g.State, er, (int)Group.Goths), bigN = Simulation.MenuScore(g.State, er, (int)Group.BigShots);
            er.menu.Clear(); er.menu.Add(g.State.DefaultItem("snakep"));
            float gothE = Simulation.MenuScore(g.State, er, (int)Group.Goths), bigE = Simulation.MenuScore(g.State, er, (int)Group.BigShots);
            Debug.Log("[selftest] exotic goths " + gothN.ToString("0.00") + "->" + gothE.ToString("0.00") + " bigshots " + bigN.ToString("0.00") + "->" + bigE.ToString("0.00") + " exoticCount=" + Simulation.ExoticCount(er));
            // bomb sent to a competitor
            var bomber = g.State; bomber.Player.hq = 3; bomber.Player.cash = 50000;
            var victim = bomber.restaurants.Find(x => x.owner != 0);
            string msg = null;
            for (int t = 0; t < 12 && (victim.closedDays == 0); t++) { bomber.day++; msg = Simulation.Sabotage(bomber, 0, victim, 5); }
            Debug.Log("[selftest] bomb: " + msg + " closedDays=" + victim.closedDays + " rep=" + Mathf.RoundToInt(victim.reputation));
            Debug.Log("[selftest] DONE errors=" + errors);
            Application.Quit();
        }
    }
}

namespace PizzaTycoon
{
    public class AutoStart : MonoBehaviour
    {
        public System.Collections.IEnumerator Run()
        {
            yield return null;
            var g = GameController.I;
            g.StartNew(0, "Вы");
            Simulation.Build(g.State, 0, 1, 1);
            Simulation.Build(g.State, 1, 3, 0);
            g.RefreshWorld();
            g.SelectDistrict(0);
            string shotPath = null; foreach (var a in System.Environment.GetCommandLineArgs()) if (a.StartsWith("-shot=")) shotPath = a.Substring(6);
            if (shotPath != null)
            {
                Simulation.Build(g.State, 3, 4, 2); g.RefreshWorld(); g.SelectDistrict(0);
                g.Speed = 0; g.DayTime = g.DayLength * 0.45f; g.Rig.FocusOn(g.City.DistrictCenter(0) + new Vector3(10f, 0, 8f), 62f);
                yield return new WaitForSecondsRealtime(4f);
                ScreenCapture.CaptureScreenshot(shotPath); yield return new WaitForSecondsRealtime(2f); Application.Quit();
            }

        }
    }
}
