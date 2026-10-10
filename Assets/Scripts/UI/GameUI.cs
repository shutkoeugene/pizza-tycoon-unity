using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PizzaTycoon
{
    public partial class GameUI : MonoBehaviour
    {
        GameController G => GameController.I;
        GameState S => G.State;

        Canvas canvas;
        RectTransform hud, sidebar, mainMenu, windowRoot, toastRoot, escMenu, overlay;
        Text txtCity, txtDay, txtCash, txtDebt, txtRep, txtEvent, txtSpeed;
        RectTransform windowContent;
        ScrollRect windowScroll;
        string currentTab = "Рестораны", lastTab = "";
        Image dayBar;

        int buildStyle = 1, buildSize = 0;
        readonly HashSet<string> designSel = new HashSet<string>();
        public bool ModalOpen => windowRoot != null || mainMenu != null || escMenu != null || overlay != null;

        // -------------------------------------------------- setup
        public void Init()
        {
            UIKit.Init();
            canvas = UIKit.CreateCanvas();
            canvas.transform.SetParent(transform, false);
            toastRoot = UIKit.Rect("Toasts", canvas.transform);
            UIKit.Anchor(toastRoot, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -90), new Vector2(700, 300));
            var vl = toastRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandHeight = false; vl.spacing = 6;
            vl.childAlignment = TextAnchor.UpperCenter;
            ShowMainMenu();
        }

        void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) { t.GetChild(i).gameObject.SetActive(false); Destroy(t.GetChild(i).gameObject); }
        }

        void Update()
        {
            if (hud != null && S != null && G.InGame)
            {
                txtCity.text = S.City.name;
                txtDay.text = "День " + S.day;
                txtCash.text = UIKit.Money(S.Player.cash);
                txtCash.color = S.Player.cash >= 0 ? UIKit.Good : UIKit.Bad;
                txtDebt.text = "Долг " + UIKit.Money(S.Player.loan);
                var mine = S.RestaurantsOf(0);
                float rep = mine.Count == 0 ? 0 : mine.Average(r => r.reputation);
                int stars = 0; foreach (var rr in mine) stars += rr.stars;
                txtRep.text = "Реп. " + Mathf.RoundToInt(rep) + "  Звёзды " + stars + "  Штаб " + S.Player.hq;
                txtEvent.text = S.eventDaysLeft > 0 ? S.eventText : "";
                txtSpeed.text = G.Speed == 0 ? "ПАУЗА" : new string('>', G.Speed);
                dayBar.fillAmount = G.DayTime / G.DayLength;
            }
        }

        public void Toast(string msg)
        {
            if (toastRoot == null) return;
            var b = UIKit.Box(toastRoot, new Color(0.1f, 0.12f, 0.18f, 0.95f), "Toast", true);
            var l = UIKit.Label(b.transform, msg, 22, UIKit.TextCol, TextAnchor.MiddleCenter);
            UIKit.Stretch(l.rectTransform, 14, 14, 8, 8);
            UIKit.Size(b.gameObject, -1, 46);
            Destroy(b.gameObject, 4f);
            if (toastRoot.childCount > 5) Destroy(toastRoot.GetChild(0).gameObject);
        }

        // -------------------------------------------------- main menu
        public void ShowMainMenu()
        {
            CloseWindow(); CloseEsc();
            if (hud != null) Destroy(hud.gameObject);
            if (sidebar != null) Destroy(sidebar.gameObject);
            if (mainMenu != null) Destroy(mainMenu.gameObject);
            if (overlay != null) { Destroy(overlay.gameObject); overlay = null; }
            mainMenu = UIKit.Rect("MainMenu", canvas.transform);
            UIKit.Stretch(mainMenu);
            var bg = mainMenu.gameObject.AddComponent<Image>();
            bg.color = new Color(0.05f, 0.06f, 0.1f, 0.78f);

            var col = UIKit.VBox(mainMenu, 14, 0);
            UIKit.Anchor(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560, 700));
            var t = UIKit.Label(col, "PIZZA TYCOON", 64, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Size(t.gameObject, -1, 100);
            var sub = UIKit.Label(col, "Построй пиццерийную империю в десяти городах мира", 22, UIKit.Dim, TextAnchor.MiddleCenter);
            UIKit.Size(sub.gameObject, -1, 40);
            UIKit.Btn(col, "Новая игра", () => ShowCityPicker(), UIKit.Accent, 30, -1, 64);
            var cont = UIKit.Btn(col, "Продолжить", () => { Destroy(mainMenu.gameObject); G.LoadGame(1); }, null, 30, -1, 64);
            cont.interactable = GameState.HasSave(1);
            var auto = UIKit.Btn(col, "Автосохранение", () => { Destroy(mainMenu.gameObject); G.LoadGame(0); }, null, 26, -1, 56);
            auto.interactable = GameState.HasSave(0);
            UIKit.Btn(col, "Выход", () => Application.Quit(), UIKit.Bad, 26, -1, 56);
            var help = UIKit.Label(col, "WASD — движение камеры, Q/E — поворот, колесо — масштаб, ПКМ — вращение, Пробел — пауза, 1-3 — скорость", 16, UIKit.Dim, TextAnchor.MiddleCenter);
            UIKit.Size(help.gameObject, -1, 60);
        }

        void ShowCityPicker()
        {
            ClearChildren(mainMenu);
            var col = UIKit.VBox(mainMenu, 10, 0);
            UIKit.Anchor(col, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900, 820));
            var t = UIKit.Label(col, "Выберите город", 44, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIKit.Size(t.gameObject, -1, 70);
            var grid = UIKit.Rect("Grid", col);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(280, 90); gl.spacing = new Vector2(12, 12); gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 3;
            UIKit.Size(grid.gameObject, -1, 420);
            for (int i = 0; i < GameData.Cities.Count; i++)
            {
                int idx = i; var c = GameData.Cities[i];
                var b = UIKit.Btn(grid, c.name + "\nЦель: реп. " + c.goalReputation, () =>
                {
                    Destroy(mainMenu.gameObject); mainMenu = null; G.StartNew(idx, "Вы");
                }, Color.Lerp(UIKit.PanelLight, c.accent, 0.45f), 22, 280, 90);
            }
            UIKit.Btn(col, "Назад", () => ShowMainMenu(), null, 24, -1, 52);
        }

        // -------------------------------------------------- HUD
        public void ShowHud()
        {
            if (mainMenu != null) { Destroy(mainMenu.gameObject); mainMenu = null; }
            if (hud != null) Destroy(hud.gameObject);
            hud = UIKit.Rect("HUD", canvas.transform); UIKit.Stretch(hud);
            hud.SetAsFirstSibling();

            var top = UIKit.Box(hud, UIKit.Panel, "TopBar");
            UIKit.Anchor(top.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(-24, 64));
            var row = UIKit.HBox(top.transform, 24);
            UIKit.Stretch(row, 20, 20, 4, 4);
            row.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            txtCity = UIKit.Label(row, "", 30, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(txtCity.gameObject, 260);
            txtDay = UIKit.Label(row, "", 26); UIKit.Size(txtDay.gameObject, 140);
            txtCash = UIKit.Label(row, "", 30, UIKit.Good, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(txtCash.gameObject, 220);
            txtDebt = UIKit.Label(row, "", 22, UIKit.Dim); UIKit.Size(txtDebt.gameObject, 200);
            txtRep = UIKit.Label(row, "", 22); txtRep.horizontalOverflow = HorizontalWrapMode.Overflow; UIKit.Size(txtRep.gameObject, 400);
            txtEvent = UIKit.Label(row, "", 20, UIKit.Accent); UIKit.Size(txtEvent.gameObject, -1, -1, 1);
            UIKit.Btn(row, "||", () => G.Speed = 0, null, 22, 52, 44);
            UIKit.Btn(row, ">", () => G.Speed = 1, null, 22, 52, 44);
            UIKit.Btn(row, ">>", () => G.Speed = 2, null, 22, 52, 44);
            UIKit.Btn(row, ">>>", () => G.Speed = 3, null, 22, 64, 44);
            txtSpeed = UIKit.Label(row, "", 22, UIKit.Dim, TextAnchor.MiddleCenter); UIKit.Size(txtSpeed.gameObject, 90);

            var bar = UIKit.Box(top.transform, UIKit.Accent, "DayBar", true);
            bar.type = Image.Type.Filled; bar.fillMethod = Image.FillMethod.Horizontal; bar.sprite = UIKit.RoundSmall;
            UIKit.Anchor(bar.rectTransform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 2), new Vector2(-24, 5));
            dayBar = bar;

            // bottom toolbar
            var tb = UIKit.Box(hud, UIKit.Panel, "Toolbar");
            UIKit.Anchor(tb.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(12, 12), new Vector2(1530, 68));
            var trow = UIKit.HBox(tb.transform, 8); UIKit.Stretch(trow, 10, 10, 8, 8);
            string[] tabs = { "Рестораны", "Меню", "Персонал", "Интерьер", "Реклама", "Финансы", "Рынок", "Конкуренты", "Штаб", "Клиенты", "Новости" };
            foreach (var tab in tabs) { var tt = tab; UIKit.Btn(trow, tab, () => OpenTab(tt), null, 16, 122, 52); }

            var sv = UIKit.Btn(hud, "Сохранить", () => { S.Save(1); Toast("Игра сохранена."); }, null, 20, 150, 52);
            UIKit.Anchor((RectTransform)sv.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1552, 20), new Vector2(150, 52));
            var ms = UIKit.Btn(hud, "Цель", () => ShowGoal(), UIKit.Accent, 20, 100, 52);
            UIKit.Anchor((RectTransform)ms.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(1712, 20), new Vector2(100, 52));

            BuildSidebar();
            RefreshSidebar();
            Toast("Выберите район на карте и постройте ресторан.");
        }

        void BuildSidebar()
        {
            if (sidebar != null) Destroy(sidebar.gameObject);
            var box = UIKit.Box(hud, UIKit.Panel, "Sidebar");
            sidebar = box.rectTransform;
            UIKit.Anchor(sidebar, new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-12, -30), new Vector2(400, -190));
        }

        public void RefreshSidebar()
        {
            if (sidebar == null) return;
            ClearChildren(sidebar);
            RectTransform content;
            var sc = UIKit.Scroll(sidebar, out content);
            UIKit.Stretch(sc, 12, 12, 12, 12);
            int di = G.SelectedDistrict;
            if (di < 0)
            {
                UIKit.Label(content, "Нажмите на район города, чтобы увидеть информацию и построить ресторан.", 20, UIKit.Dim);
                return;
            }
            var city = S.City; var d = city.districts[di];
            var title = UIKit.Label(content, d.name, 34, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Size(title.gameObject, -1, 44);
            Meter(content, "Богатство", d.wealth, new Color(0.9f, 0.8f, 0.3f));
            Meter(content, "Население", d.population, new Color(0.4f, 0.7f, 0.95f));
            UIKit.Row(content, "Вкусы", TasteName(d.preference));
            UIKit.Row(content, "Аренда (коэф.)", d.rentFactor.ToString("0.00"));
            var order = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7 };
            order.Sort((x, y) => d.mix[y].CompareTo(d.mix[x]));
            UIKit.Label(content, "Клиенты района", 20, UIKit.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int k = 0; k < 3; k++) Meter(content, GameData.Groups[order[k]].name + " " + Mathf.RoundToInt(d.mix[order[k]] * 100) + "%", d.mix[order[k]] * 2f, GameData.Groups[order[k]].color);
            UIKit.Row(content, "Любимый стиль главной группы", GameData.Styles[GameData.Groups[order[0]].liked[0]].name);
            UIKit.Row(content, "Ваших ресторанов / лимит", S.RestaurantsOf(0).Count + " / " + GameState.MaxRestaurants(S.Player.hq));

            var present = S.restaurants.Where(r => r.district == di).ToList();
            UIKit.Label(content, "Рестораны в районе: " + present.Count + "/4", 22, UIKit.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold);
            foreach (var r in present)
            {
                var owner = S.companies[r.owner];
                var line = UIKit.Box(content, UIKit.PanelLight, "R", true);
                var h = UIKit.HBox(line.transform, 8); UIKit.Stretch(h, 10, 10, 6, 6);
                var sw = UIKit.Box(h, owner.color, "Color", true); UIKit.Size(sw.gameObject, 14, 14);
                var lb = UIKit.Label(h, owner.name + " (" + GameData.Sizes[r.size].name + ", реп. " + Mathf.RoundToInt(r.reputation) + ")", 18);
                UIKit.Size(lb.gameObject, -1, -1, 1);
                UIKit.Size(line.gameObject, -1, 46);
                if (r.owner == 0)
                {
                    var rr = r;
                    var go = UIKit.Btn(h, "Управл.", () => { G.SelectedRestaurant = rr.id; OpenTab("Меню"); }, UIKit.Accent, 16, 100, 34);
                }
            }

            if (present.Count < 4)
            {
                UIKit.Label(content, "Построить ресторан", 24, UIKit.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold);
                var sr = UIKit.HBox(content, 4, 44);
                sr.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                for (int i = 0; i < GameData.Styles.Length; i++)
                {
                    int ii = i;
                    UIKit.Btn(sr, GameData.Styles[i].name, () => { buildStyle = ii; RefreshSidebar(); }, buildStyle == i ? UIKit.Accent : UIKit.PanelLight, 13, -1, 44);
                }
                var zr = UIKit.HBox(content, 4, 44);
                zr.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                for (int i = 0; i < GameData.Sizes.Length; i++)
                {
                    int ii = i;
                    UIKit.Btn(zr, GameData.Sizes[i].name + " (" + GameData.Sizes[i].seats + ")", () => { buildSize = ii; RefreshSidebar(); }, buildSize == i ? UIKit.Accent : UIKit.PanelLight, 15, -1, 44);
                }
                float cost = S.BuildCost(di, buildStyle, buildSize);
                UIKit.Row(content, "Стоимость", UIKit.Money(cost), S.Player.cash >= cost ? UIKit.Good : UIKit.Bad);
                UIKit.Row(content, "Аренда в день", UIKit.Money(GameData.Sizes[buildSize].rent * d.rentFactor * city.priceLevel));
                var bb = UIKit.Btn(content, "Построить", () =>
                {
                    var err = Simulation.Build(S, di, buildStyle, buildSize);
                    if (err != null) Toast(err);
                    else { Toast("Ресторан построен!"); G.RefreshWorld(); var mine = S.RestaurantsOf(0); G.SelectedRestaurant = mine[mine.Count - 1].id; RefreshSidebar(); }
                }, UIKit.Good, 24, -1, 54);
            }
        }

        string TasteName(Taste t)
        {
            switch (t) { case Taste.Veg: return "Вегетарианские"; case Taste.Meat: return "Мясные"; case Taste.Sea: return "Морепродукты"; default: return "Классика"; }
        }

        void Meter(Transform parent, string label, float v, Color c)
        {
            var h = UIKit.HBox(parent, 8, 28);
            var l = UIKit.Label(h, label, 18, UIKit.Dim); UIKit.Size(l.gameObject, 140);
            var bg = UIKit.Box(h, new Color(1, 1, 1, 0.1f), "BG", true); UIKit.Size(bg.gameObject, -1, 16, 1);
            var f = UIKit.Box(bg.transform, c, "Fill", true);
            f.rectTransform.anchorMin = Vector2.zero; f.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(v), 1);
            f.rectTransform.offsetMin = f.rectTransform.offsetMax = Vector2.zero;
        }

        public void OnNewDay()
        {
            RefreshSidebar();
            if (windowRoot != null) RebuildWindow();
            var first = S.news.FirstOrDefault();
            if (first != null && first.day >= S.day - 1 && newsShownDay != first.day + first.text.GetHashCode())
            {
                newsShownDay = first.day + first.text.GetHashCode();
                Toast(first.text);
            }
        }
        int newsShownDay = -1;

        // -------------------------------------------------- windows
        public void OpenTab(string tab)
        {
            currentTab = tab;
            if (windowRoot == null) CreateWindow();
            RebuildWindow();
        }

        void CreateWindow()
        {
            windowRoot = UIKit.Rect("Window", canvas.transform); UIKit.Stretch(windowRoot);
            var dim = windowRoot.gameObject.AddComponent<Image>(); dim.color = new Color(0, 0, 0, 0.45f);
            var panel = UIKit.Box(windowRoot, UIKit.Panel, "Panel");
            UIKit.Anchor(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(1560, 860));
            var tabs = UIKit.HBox(panel.transform, 6);
            UIKit.Anchor(tabs, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(-24, 52));
            string[] names = { "Рестораны", "Меню", "Персонал", "Интерьер", "Реклама", "Финансы", "Рынок", "Конкуренты", "Штаб", "Клиенты", "Новости" };
            foreach (var n in names) { var nn = n; UIKit.Btn(tabs, n, () => { currentTab = nn; RebuildWindow(); }, null, 16, 118, 46); }
            var close = UIKit.Btn(tabs, "X", () => CloseWindow(), UIKit.Bad, 20, 52, 46);
            RectTransform content;
            var sc = UIKit.Scroll(panel.transform, out content);
            UIKit.Stretch(sc, 20, 20, 74, 16);
            windowContent = content;
            windowScroll = sc.GetComponent<ScrollRect>();
        }

        public void CloseWindow()
        {
            if (windowRoot != null) Destroy(windowRoot.gameObject);
            windowRoot = null; windowContent = null; lastTab = "";
        }

        void RebuildWindow()
        {
            if (windowContent == null) return;
            float pos = lastTab == currentTab ? windowScroll.verticalNormalizedPosition : 1f; lastTab = currentTab;
            ClearChildren(windowContent);
            var h = UIKit.Label(windowContent, currentTab, 38, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UIKit.Size(h.gameObject, -1, 52);
            switch (currentTab)
            {
                case "Рестораны": TabRestaurants(windowContent); break;
                case "Меню": TabMenu(windowContent); break;
                case "Персонал": TabStaff(windowContent); break;
                case "Интерьер": TabInterior(windowContent); break;
                case "Реклама": TabAds(windowContent); break;
                case "Штаб": TabHq(windowContent); break;
                case "Клиенты": TabClients(windowContent); break;
                case "Финансы": TabFinance(windowContent); break;
                case "Рынок": TabMarket(windowContent); break;
                case "Конкуренты": TabCompetitors(windowContent); break;
                case "Новости": TabNews(windowContent); break;
            }
            Canvas.ForceUpdateCanvases();
            windowScroll.verticalNormalizedPosition = pos;
        }

        // -------------------------------------------------- tabs
        void Refresh() { RebuildWindow(); RefreshSidebar(); }

        void Stepper(Transform parent, string label, string value, Action minus, Action plus, bool canMinus = true, bool canPlus = true)
        {
            var h = UIKit.HBox(parent, 8, 44);
            var l = UIKit.Label(h, label, 22, UIKit.TextCol); UIKit.Size(l.gameObject, -1, -1, 1);
            var m = UIKit.Btn(h, "-", minus, null, 24, 50, 40); m.interactable = canMinus;
            var v = UIKit.Label(h, value, 22, UIKit.TextCol, TextAnchor.MiddleCenter, FontStyle.Bold); UIKit.Size(v.gameObject, 130);
            var p = UIKit.Btn(h, "+", plus, null, 24, 50, 40); p.interactable = canPlus;
        }

        void TabRestaurants(Transform c)
        {
            var mine = S.RestaurantsOf(0);
            if (mine.Count == 0)
            {
                UIKit.Label(c, "У вас пока нет ресторанов. Выберите район на карте и постройте первый.", 24, UIKit.Dim);
                return;
            }
            foreach (var r in mine)
            {
                var rr = r;
                var card = UIKit.Box(c, UIKit.PanelLight, "Card");
                var v = UIKit.VBox(card.transform, 6, 14); UIKit.Stretch(v);
                var head = UIKit.Label(v, r.name, 26, G.SelectedRestaurant == r.id ? UIKit.Accent : UIKit.TextCol, TextAnchor.MiddleLeft, FontStyle.Bold);
                UIKit.Size(head.gameObject, -1, 34);
                UIKit.Row(v, GameData.Styles[r.style].name + ", " + GameData.Sizes[r.size].name + " (" + GameData.Sizes[r.size].seats + " мест)", "реп. " + Mathf.RoundToInt(r.reputation) + "  чист. " + Mathf.RoundToInt(r.cleanliness));
                UIKit.Row(v, "Вчера: гостей " + r.last.customers + ", отказ " + r.last.unserved + ", пицц " + r.last.pizzas, "Прибыль " + UIKit.Money(r.last.Profit), r.last.Profit >= 0 ? UIKit.Good : UIKit.Bad);
                UIKit.Row(v, "Всего: выручка " + UIKit.Money(r.total.revenue), "Дней: " + (S.day - r.openedDay));
                UIKit.Row(v, "Гурманские звёзды: " + r.stars + (r.closedDays > 0 ? "   ЗАКРЫТ ещё " + r.closedDays + " дн." : ""), "оценка богачей " + Mathf.RoundToInt(r.rating[0] * 100) + "%");
                var btns = UIKit.HBox(v, 8, 44);
                UIKit.Btn(btns, "Выбрать", () => { G.SelectedRestaurant = rr.id; G.SelectDistrict(rr.district); Refresh(); }, UIKit.Accent, 18, 130, 40);
                if (r.size < 2)
                    UIKit.Btn(btns, "Расширить", () => { var e = Simulation.Upgrade(S, rr, rr.style, rr.size + 1); if (e != null) Toast(e); else { G.RefreshWorld(); Refresh(); } }, null, 18, 150, 40);
                if (r.style < 4)
                    UIKit.Btn(btns, "Улучшить стиль", () => { var e = Simulation.Upgrade(S, rr, rr.style + 1, rr.size); if (e != null) Toast(e); else { G.RefreshWorld(); Refresh(); } }, null, 18, 190, 40);
                UIKit.Btn(btns, "Закрыть", () =>
                {
                    ConfirmClose(rr);
                }, UIKit.Bad, 18, 110, 40);
                UIKit.Size(card.gameObject, -1, 262);
            }
        }

        void ConfirmClose(Restaurant r)
        {
            Simulation.Close(S, r);
            G.RefreshWorld();
            Toast("Ресторан закрыт, возвращено 40% стоимости.");
            Refresh();
        }

        void TabMenu(Transform c)
        {
            var r = G.CurrentRestaurant();
            if (r == null) { UIKit.Label(c, "Нет ресторанов.", 24, UIKit.Dim); return; }
            var mine = S.RestaurantsOf(0);
            int idx = mine.IndexOf(r);
            var sw = UIKit.HBox(c, 10, 46);
            UIKit.Btn(sw, "<", () => { G.SelectedRestaurant = mine[(idx + mine.Count - 1) % mine.Count].id; Refresh(); }, null, 22, 56, 42);
            var nm = UIKit.Label(sw, r.name, 24, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold); UIKit.Size(nm.gameObject, -1, -1, 1);
            UIKit.Btn(sw, ">", () => { G.SelectedRestaurant = mine[(idx + 1) % mine.Count].id; Refresh(); }, null, 22, 56, 42);

            foreach (var m in r.menu.ToList())
            {
                var mm = m;
                var card = UIKit.Box(c, UIKit.PanelLight, "Item", true);
                var v = UIKit.VBox(card.transform, 4, 10); UIKit.Stretch(v);
                float cost = S.ItemCost(m, r);
                float fair = Simulation.FairPrice(S, m, r);
                float q = GameState.ItemQuality(m, r.supplier);
                var head = UIKit.HBox(v, 8, 36);
                var title = UIKit.Label(head, m.name, 24, m.enabled ? UIKit.TextCol : UIKit.Dim, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(title.gameObject, -1, -1, 1);
                UIKit.Btn(head, m.enabled ? "В меню" : "Выключена", () => { mm.enabled = !mm.enabled; Refresh(); }, m.enabled ? UIKit.Good : UIKit.PanelLight, 16, 120, 34);
                UIKit.Btn(head, "Удалить", () => { r.menu.Remove(mm); Refresh(); }, UIKit.Bad, 16, 100, 34);
                var ing = string.Join(", ", m.ingredients.Select(id => GameData.Ingredients.Find(x => x.id == id).name));
                var il = UIKit.Label(v, ing, 16, UIKit.Dim); UIKit.Size(il.gameObject, -1, 24);
                var info = UIKit.HBox(v, 8, 38);
                var cl = UIKit.Label(info, "Себест. " + UIKit.Money(cost) + "  рек. цена " + fair.ToString("0.0") + "  качество " + Mathf.RoundToInt(q * 100) + "%", 18, UIKit.Dim); UIKit.Size(cl.gameObject, -1, -1, 1);
                UIKit.Btn(info, "-", () => { mm.price = Mathf.Max(0.5f, mm.price - 0.5f); Refresh(); }, null, 22, 44, 34);
                var pl = UIKit.Label(info, "$" + m.price.ToString("0.0"), 22, m.price > fair * 1.25f ? UIKit.Bad : (m.price < cost ? UIKit.Bad : UIKit.Good), TextAnchor.MiddleCenter, FontStyle.Bold); UIKit.Size(pl.gameObject, 90);
                UIKit.Btn(info, "+", () => { mm.price += 0.5f; Refresh(); }, null, 22, 44, 34);
                UIKit.Size(card.gameObject, -1, 140);
            }

            var add = UIKit.Label(c, "Добавить рецепт", 26, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(add.gameObject, -1, 40);
            var grid = UIKit.Rect("Recipes", c);
            var gl = grid.gameObject.AddComponent<GridLayoutGroup>();
            gl.cellSize = new Vector2(250, 44); gl.spacing = new Vector2(8, 8);
            gl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; gl.constraintCount = 4;
            var cs = grid.gameObject.AddComponent<ContentSizeFitter>(); cs.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var rec in GameData.Recipes)
            {
                var rc = rec;
                bool has = r.menu.Exists(x => x.name == rc.name);
                var b = UIKit.Btn(grid, rc.name, () => { r.menu.Add(S.DefaultItem(rc.id)); Refresh(); }, null, 18, 250, 44);
                b.interactable = !has;
            }

            var sup = UIKit.Label(c, "Поставщик (лояльность: до -25% цены через 150 дней)", 26, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(sup.gameObject, -1, 40);
            var sr = UIKit.HBox(c, 8, 50);
            sr.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            string[] sn = { "Дешёвый (-20% цена, -20% качество)", "Стандартный", "Премиум (+45% цена, +25% качество)" };
            for (int i = 0; i < 3; i++) { int ii = i; UIKit.Btn(sr, sn[i], () => { Simulation.SetSupplier(r, ii); Refresh(); }, r.supplier == i ? UIKit.Accent : UIKit.PanelLight, 16, -1, 50); }
            UIKit.Row(c, "Дней с этим поставщиком", r.supplierDays + "  (скидка " + Mathf.RoundToInt(GameState.Loyalty(r) * 100) + "%)");

            var dz = UIKit.Label(c, "Конструктор пиццы", 26, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(dz.gameObject, -1, 40);
            var ex = UIKit.Label(c, "* Экзотика (муравьи, змея, кенгуру, саранча, личинки): готы и подростки в восторге, богатые и дети в ужасе; санинспекция заглядывает чаще.", 16, UIKit.Dim); UIKit.Size(ex.gameObject, -1, 44);
            var ig = UIKit.Rect("Ingr", c);
            var igl = ig.gameObject.AddComponent<GridLayoutGroup>();
            igl.cellSize = new Vector2(180, 44); igl.spacing = new Vector2(8, 8);
            igl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; igl.constraintCount = 5;
            ig.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            foreach (var ingr in GameData.Ingredients)
            {
                var ii = ingr;
                if (ii.id == "dough") continue;
                bool on = designSel.Contains(ii.id);
                UIKit.Btn(ig, ii.name + (ii.exotic ? "*" : "") + " " + S.IngredientCost(ii.id, r.supplier).ToString("0.00"), () => { if (on) designSel.Remove(ii.id); else designSel.Add(ii.id); Refresh(); },
                    on ? UIKit.Good : UIKit.PanelLight, 16, 180, 44);
            }
            var tmp = new MenuItem(); tmp.ingredients.Add("dough"); tmp.ingredients.AddRange(designSel);
            float dcost = S.ItemCost(tmp, r);
            UIKit.Row(c, "Себестоимость собственной пиццы", UIKit.Money(dcost) + ", качество " + Mathf.RoundToInt(GameState.ItemQuality(tmp, r.supplier) * 100) + "%");
            var create = UIKit.Btn(c, "Создать и добавить в меню", () =>
            {
                var m = new MenuItem { name = "Авторская #" + (r.menu.Count + 1), ingredients = tmp.ingredients.ToList() };
                m.price = Mathf.Round(S.ItemCost(m, r.supplier) * 4.2f * 10f) / 10f;
                r.menu.Add(m); designSel.Clear(); Refresh();
            }, UIKit.Good, 22, -1, 50);
            create.interactable = designSel.Count >= 2;
        }

        void TabFinance(Transform c)
        {
            var p = S.Player;
            var mine = S.RestaurantsOf(0);
            float rev = mine.Sum(r => r.last.revenue), ing = mine.Sum(r => r.last.costIngredients), wages = mine.Sum(r => r.last.costWages),
                  rent = mine.Sum(r => r.last.costRent), mkt = mine.Sum(r => r.last.costMarketing), oth = mine.Sum(r => r.last.costOther);
            UIKit.Row(c, "Наличные", UIKit.Money(p.cash), p.cash >= 0 ? UIKit.Good : UIKit.Bad, 24);
            UIKit.Row(c, "Долг", UIKit.Money(p.loan), p.loan > 0 ? UIKit.Bad : UIKit.TextCol, 24);
            UIKit.Row(c, "Процент по долгу в день", UIKit.Money(p.loan * 0.0006f));
            var lr = UIKit.HBox(c, 8, 50);
            UIKit.Btn(lr, "Взять $5 000", () => { var e = Simulation.TakeLoan(S, 5000); if (e != null) Toast(e); Refresh(); }, null, 20, 200, 46);
            UIKit.Btn(lr, "Взять $20 000", () => { var e = Simulation.TakeLoan(S, 20000); if (e != null) Toast(e); Refresh(); }, null, 20, 220, 46);
            UIKit.Btn(lr, "Погасить $5 000", () => { var e = Simulation.RepayLoan(S, 5000); if (e != null) Toast(e); Refresh(); }, UIKit.Good, 20, 220, 46);
            UIKit.Btn(lr, "Погасить всё", () => { var e = Simulation.RepayLoan(S, p.loan); if (e != null) Toast(e); Refresh(); }, UIKit.Good, 20, 200, 46);

            var t = UIKit.Label(c, "Вчерашний день", 28, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(t.gameObject, -1, 40);
            UIKit.Row(c, "Выручка", UIKit.Money(rev), UIKit.Good);
            UIKit.Row(c, "Продукты", UIKit.Money(-ing));
            UIKit.Row(c, "Зарплаты", UIKit.Money(-wages));
            UIKit.Row(c, "Аренда", UIKit.Money(-rent));
            UIKit.Row(c, "Реклама", UIKit.Money(-mkt));
            UIKit.Row(c, "Прочее", UIKit.Money(-oth));
            float profit = rev - ing - wages - rent - mkt - oth;
            UIKit.Row(c, "Прибыль", UIKit.Money(profit), profit >= 0 ? UIKit.Good : UIKit.Bad, 26);

            var ch = UIKit.Label(c, "Динамика капитала", 28, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(ch.gameObject, -1, 40);
            var chart = UIKit.Box(c, UIKit.PanelLight, "Chart", true);
            UIKit.Size(chart.gameObject, -1, 220);
            var hist = S.cashHistory;
            if (hist.Count > 1)
            {
                float mn = hist.Min(), mx = hist.Max();
                if (mx - mn < 1) mx = mn + 1;
                var area = UIKit.Rect("Area", chart.transform); UIKit.Stretch(area, 10, 10, 10, 10);
                var hl = area.gameObject.AddComponent<HorizontalLayoutGroup>();
                hl.childForceExpandWidth = true; hl.childForceExpandHeight = true; hl.childControlWidth = true; hl.childControlHeight = true;
                hl.spacing = 1; hl.childAlignment = TextAnchor.LowerCenter;
                foreach (var v in hist)
                {
                    var col = UIKit.Rect("c", area);
                    var le = col.gameObject.AddComponent<LayoutElement>();
                    var bar = UIKit.Box(col, v >= 0 ? UIKit.Good : UIKit.Bad, "bar", true);
                    float k = Mathf.Clamp01((v - mn) / (mx - mn));
                    bar.rectTransform.anchorMin = new Vector2(0, 0); bar.rectTransform.anchorMax = new Vector2(1, Mathf.Max(0.03f, k));
                    bar.rectTransform.offsetMin = bar.rectTransform.offsetMax = Vector2.zero;
                }
            }
        }

        void TabMarket(Transform c)
        {
            UIKit.Label(c, "Цены на ингредиенты в городе (за порцию). В моде: " + string.Join(", ", S.trend.Select(id => GameData.Ingredients.Find(x => x.id == id).name)) + " (+ к оценке блюда).", 20, UIKit.Dim);
            for (int i = 0; i < GameData.Ingredients.Count; i++)
            {
                var ing = GameData.Ingredients[i];
                float cost = S.IngredientCost(ing.id, 1);
                float ratio = S.ingredientMul[i];
                var h = UIKit.HBox(c, 10, 38);
                var sw = UIKit.Box(h, ing.color, "sw", true); UIKit.Size(sw.gameObject, 26, 26);
                var l = UIKit.Label(h, ing.name + (ing.exotic ? " [экзотика]" : "") + "   (" + TasteName(ing.taste) + ", кач. " + Mathf.RoundToInt(ing.quality * 100) + "%)", 22); UIKit.Size(l.gameObject, -1, -1, 1);
                var p = UIKit.Label(h, "$" + cost.ToString("0.00"), 22, UIKit.TextCol, TextAnchor.MiddleRight, FontStyle.Bold); UIKit.Size(p.gameObject, 100);
                var d = UIKit.Label(h, (ratio >= 1 ? "+" : "") + Mathf.RoundToInt((ratio - 1) * 100) + "%", 22, ratio > 1.1f ? UIKit.Bad : (ratio < 0.92f ? UIKit.Good : UIKit.Dim), TextAnchor.MiddleRight); UIKit.Size(d.gameObject, 80);
            }
        }

        void TabCompetitors(Transform c)
        {
            for (int i = 0; i < S.companies.Count; i++)
            {
                var co = S.companies[i]; var rs = S.RestaurantsOf(i);
                var card = UIKit.Box(c, UIKit.PanelLight, "Card", true);
                var v = UIKit.VBox(card.transform, 6, 14); UIKit.Stretch(v);
                var t = UIKit.Label(v, co.name + (co.isPlayer ? " (вы)" : ""), 26, co.color, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(t.gameObject, -1, 34);
                UIKit.Row(v, "Ресторанов / уровень штаба", rs.Count + " / " + co.hq + (rs.Sum(r => r.stars) > 0 ? "   звёзд: " + rs.Sum(r => r.stars) : ""));
                UIKit.Row(v, "Средняя репутация", rs.Count == 0 ? "-" : Mathf.RoundToInt(rs.Average(r => r.reputation)).ToString());
                UIKit.Row(v, "Гостей вчера", rs.Sum(r => r.last.customers).ToString());
                UIKit.Row(v, "Средняя цена", rs.Count == 0 ? "-" : "$" + rs.SelectMany(r => r.menu).Where(m => m.enabled).DefaultIfEmpty(new MenuItem()).Average(m => m.price).ToString("0.0"));
                if (co.isPlayer) UIKit.Row(v, "Капитал", UIKit.Money(co.cash - co.loan));
                UIKit.Size(card.gameObject, -1, 200);
            }
        }

        void TabNews(Transform c)
        {
            foreach (var n in S.news)
            {
                var h = UIKit.HBox(c, 10, 34);
                var d = UIKit.Label(h, "День " + n.day, 18, UIKit.Dim); UIKit.Size(d.gameObject, 90);
                var t = UIKit.Label(h, n.text, 20); UIKit.Size(t.gameObject, -1, -1, 1);
            }
        }

        // -------------------------------------------------- misc screens
        void ShowGoal()
        {
            var c = S.City;
            var sb = new System.Text.StringBuilder("Чтобы покорить " + c.name + " (не раньше 90-го дня):\n\n");
            foreach (var o in c.objectives)
                sb.Append(Simulation.IsDone(S, o) ? "[v] " : "[  ] ").Append(o.Text()).Append('\n');
            sb.Append("\nПокорено городов: " + S.citiesCompleted);
            Dialog("Задачи города", sb.ToString(), new[] { ("Закрыть", (Action)(() => CloseOverlay())) });
        }

        public void ShowBankrupt()
        {
            Dialog("Банкротство", "Вы не смогли расплатиться с кредиторами. Игра окончена.",
                new[] { ("В главное меню", (Action)(() => { CloseOverlay(); G.ExitToMenu(); })) });
        }

        public void ShowWin()
        {
            Dialog("Город покорён!", "Вы стали королём пиццы в городе " + S.City.name + ". Перейти в следующий город?",
                new[] { ("Следующий город", (Action)(() => { CloseOverlay(); Simulation.AdvanceCity(S); G.SelectedDistrict = -1; G.RefreshWorld(); RefreshSidebar(); G.Speed = 1; Toast("Новый город: " + S.City.name); })),
                        ("Остаться здесь", (Action)(() => { CloseOverlay(); G.Speed = 1; })) });
        }

        void Dialog(string title, string body, (string, Action)[] buttons)
        {
            CloseOverlay();
            overlay = UIKit.Rect("Overlay", canvas.transform); UIKit.Stretch(overlay);
            overlay.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var box = UIKit.Box(overlay, UIKit.Panel, "Dialog");
            UIKit.Anchor(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760, 580));
            var v = UIKit.VBox(box.transform, 14, 24); UIKit.Stretch(v);
            var t = UIKit.Label(v, title, 40, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold); UIKit.Size(t.gameObject, -1, 60);
            var b = UIKit.Label(v, body, 24, UIKit.TextCol, TextAnchor.UpperLeft); UIKit.Size(b.gameObject, -1, 300);
            foreach (var bt in buttons) UIKit.Btn(v, bt.Item1, bt.Item2, UIKit.Accent, 24, -1, 52);
        }

        void CloseOverlay() { if (overlay != null) Destroy(overlay.gameObject); overlay = null; }

        public void ToggleEscapeMenu()
        {
            if (windowRoot != null) { CloseWindow(); return; }
            if (overlay != null) return;
            if (escMenu != null) { CloseEsc(); return; }
            escMenu = UIKit.Rect("Esc", canvas.transform); UIKit.Stretch(escMenu);
            escMenu.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, 0.6f);
            var v = UIKit.VBox(escMenu, 12, 0);
            UIKit.Anchor(v, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 360));
            var t = UIKit.Label(v, "Пауза", 44, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold); UIKit.Size(t.gameObject, -1, 70);
            prevSpeed = G.Speed; G.Speed = 0;
            UIKit.Btn(v, "Продолжить", () => CloseEsc(), UIKit.Accent, 26, -1, 56);
            UIKit.Btn(v, "Сохранить", () => { S.Save(1); Toast("Игра сохранена."); }, null, 26, -1, 56);
            UIKit.Btn(v, "В главное меню", () => { CloseEsc(); G.ExitToMenu(); }, UIKit.Bad, 26, -1, 56);
        }
        int prevSpeed = 1;
        void CloseEsc() { if (escMenu != null) { Destroy(escMenu.gameObject); escMenu = null; G.Speed = prevSpeed; } }
    }
}
