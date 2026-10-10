using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PizzaTycoon
{
    // "-uitest": drives the real UI by invoking button handlers like a player would.
    public class UiTest : MonoBehaviour
    {
        int errors, clicks;
        static Button Find(string contains)
        {
            foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled || !b.interactable) continue;
                var t = b.GetComponentInChildren<Text>();
                if (t != null && t.text.Contains(contains)) return b;
            }
            return null;
        }
        bool Click(string s)
        {
            var b = Find(s);
            if (b == null) { Debug.Log("[uitest] MISSING button: " + s); errors++; return false; }
            b.onClick.Invoke(); clicks++; return true;
        }
        IEnumerator ClickAll()
        {
            var list = new List<Button>(FindObjectsByType<Button>(FindObjectsSortMode.None));
            foreach (var b in list)
            {
                if (b == null || !b.isActiveAndEnabled || !b.interactable) continue;
                var t = b.GetComponentInChildren<Text>();
                string n = t != null ? t.text : "";
                if (n == "X" || n.Contains("Закрыть") || n.Contains("главное") || n.Contains("Выход")) continue; // would leave the screen
                b.onClick.Invoke(); clicks++;
                yield return null;
            }
        }
        public IEnumerator Run()
        {
            Application.logMessageReceived += (c, s, t) => { if (t == LogType.Exception || t == LogType.Error) { errors++; Debug.Log("[uitest] ERR " + s); } };
            var g = GameController.I;
            yield return null; yield return null;
            Click("Новая игра"); yield return null;
            Click("Рим"); yield return null; yield return null;
            Debug.Log("[uitest] in game=" + g.InGame + " city=" + g.State.City.name);
            g.SelectDistrict(3); yield return null;
            Click("Построить"); yield return null;
            Debug.Log("[uitest] restaurants owned after Build click=" + g.State.RestaurantsOf(0).Count);
            g.SelectDistrict(0); yield return null; Click("Построить"); yield return null;
            g.SelectDistrict(2); yield return null; Click("Построить"); yield return null;
            Debug.Log("[uitest] owned=" + g.State.RestaurantsOf(0).Count + " cash=" + Mathf.RoundToInt(g.State.Player.cash));
            Click("||"); Click(">>>"); yield return null; Debug.Log("[uitest] speed=" + g.Speed);
            // let real time pass at speed 3 so the day loop runs
            float t0 = Time.time; int d0 = g.State.day;
            while (Time.time - t0 < 6f) yield return null;
            Debug.Log("[uitest] days advanced " + (g.State.day - d0));
            foreach (var tab in new[] { "Рестораны", "Меню", "Персонал", "Интерьер", "Реклама", "Финансы", "Рынок", "Конкуренты", "Штаб", "Клиенты", "Новости" })
            {
                g.UI.OpenTab(tab); yield return null;
                yield return ClickAll(); yield return null;
                Debug.Log("[uitest] tab " + tab + " ok");
            }
            g.UI.CloseWindow(); yield return null;
            // sidebar buttons
            yield return ClickAll();
            Click("Сохранить"); yield return null;
            Click("Цель"); yield return null; Click("Закрыть"); yield return null;
            // win flow
            g.State.won = true; g.UI.ShowWin(); yield return null; Click("Следующий город"); yield return null; yield return null;
            Debug.Log("[uitest] after win city=" + g.State.City.name + " day=" + g.State.day);
            // bankrupt flow
            g.UI.ShowBankrupt(); yield return null; Click("В главное меню"); yield return null; yield return null;
            Debug.Log("[uitest] back at menu, InGame=" + g.InGame);
            // load
            g.State = null;
            Click("Продолжить"); yield return null; yield return null;
            Debug.Log("[uitest] loaded InGame=" + g.InGame + " city=" + (g.State != null ? g.State.City.name : "null"));
            // esc menu
            g.UI.ToggleEscapeMenu(); yield return null; Click("Продолжить"); yield return null;
            Debug.Log("[uitest] DONE clicks=" + clicks + " errors=" + errors);
            Application.Quit();
        }
    }
}
