using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PizzaTycoon
{
    public partial class GameUI
    {
        // ---- helpers
        Restaurant Switcher(Transform c)
        {
            var r = G.CurrentRestaurant();
            if (r == null) { UIKit.Label(c, "Нет ресторанов. Постройте первый в выбранном районе.", 24, UIKit.Dim); return null; }
            var mine = S.RestaurantsOf(0);
            int idx = mine.IndexOf(r);
            var sw = UIKit.HBox(c, 10, 46);
            UIKit.Btn(sw, "<", () => { G.SelectedRestaurant = mine[(idx + mine.Count - 1) % mine.Count].id; Refresh(); }, null, 22, 56, 42);
            var nm = UIKit.Label(sw, r.name + (r.stars > 0 ? "   (звёзд: " + r.stars + ")" : ""), 24, UIKit.Accent, TextAnchor.MiddleCenter, FontStyle.Bold); UIKit.Size(nm.gameObject, -1, -1, 1);
            UIKit.Btn(sw, ">", () => { G.SelectedRestaurant = mine[(idx + 1) % mine.Count].id; Refresh(); }, null, 22, 56, 42);
            return r;
        }

        void Heading(Transform c, string text)
        {
            var l = UIKit.Label(c, text, 26, UIKit.Accent, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(l.gameObject, -1, 40);
        }

        void Choice(Transform c, string[] labels, int current, Action<int> set, int fontSize = 16)
        {
            var row = UIKit.HBox(c, 8, 48);
            row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            for (int i = 0; i < labels.Length; i++) { int ii = i; UIKit.Btn(row, labels[i], () => { set(ii); Refresh(); }, current == i ? UIKit.Accent : UIKit.PanelLight, fontSize, -1, 48); }
        }

        void Act(string err, string ok = null)
        {
            if (err != null) Toast(err); else if (ok != null) Toast(ok);
            Refresh();
        }

        // ---- staff
        void TabStaff(Transform c)
        {
            var r = Switcher(c); if (r == null) return;
            var size = GameData.Sizes[r.size];
            float wage = S.StaffWage(r.skill, r.wage);

            Heading(c, "Состав смены");
            UIKit.Row(c, "Зарплата сотрудника в день (с уровнем оплаты)", UIKit.Money(wage));
            Stepper(c, "Повара (макс. " + size.kitchenSlots + ")", r.cooks.ToString(), () => { r.cooks--; Refresh(); }, () => { r.cooks++; Refresh(); }, r.cooks > 1, r.cooks < size.kitchenSlots);
            Stepper(c, "Обслуживающие", r.waiters.ToString(), () => { r.waiters--; Refresh(); }, () => { r.waiters++; Refresh(); }, r.waiters > 1, r.waiters < size.kitchenSlots + 2);
            Stepper(c, "Курьеры (возят продукты и доставку; нужно " + ((Mathf.Min(r.cooks, size.kitchenSlots) + 1) / 2) + ")", r.runners.ToString(), () => { r.runners--; Refresh(); }, () => { r.runners++; Refresh(); }, r.runners > 0, r.runners < 6);
            Choice(c, new[] { "2 смены", "3 смены (+35% мощность, ×1.5 расходы, вред здоровью)" }, r.shifts >= 3 ? 1 : 0, i => r.shifts = i == 1 ? 3 : 2);

            Heading(c, "Менеджер");
            Choice(c, new[] { "Без менеджера (-15% мощность, хуже оценка)", "Менеджер: $" + (60 + r.skill * 6) + "/день" }, r.manager ? 1 : 0, i => r.manager = i == 1);

            if (r.manager)
            {
                var q = UIKit.Label(c, "Приказы менеджеру: качество пиццы (выше качество, но он выбрасывает дешёвые продукты) и готовность угодить (лучше сервис, дороже)", 16, UIKit.Dim); UIKit.Size(q.gameObject, -1, 44);
                Choice(c, new[] { "Качество: низкое", "Качество: среднее", "Качество: максимум" }, r.mgrQuality, i => r.mgrQuality = i);
                Choice(c, new[] { "Угождать: слабо", "Угождать: средне", "Угождать: максимум" }, r.mgrService, i => r.mgrService = i);
            }

            Heading(c, "Возраст персонала: " + r.staffAge + " (богачи любят ~40, студенты и подростки ~24)");
            var ar = UIKit.HBox(c, 8, 48);
            ar.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            UIKit.Btn(ar, "Моложе (-5): " + UIKit.Money(Simulation.StaffAgeCost(r)), () => Act(Simulation.RefreshStaffAge(S, r, -5)), null, 16, -1, 48);
            UIKit.Btn(ar, "Старше (+5): " + UIKit.Money(Simulation.StaffAgeCost(r)), () => Act(Simulation.RefreshStaffAge(S, r, 5)), null, 16, -1, 48);

            Heading(c, "Мотивация и здоровье");
            Meter(c, "Мотивация", Mathf.Clamp01(r.motivation / 100f), r.motivation < 20 ? UIKit.Bad : UIKit.Good);
            Meter(c, "Здоровье", Mathf.Clamp01(r.health / 100f), r.health < 30 ? UIKit.Bad : UIKit.Good);
            Choice(c, new[] { "Низкая оплата (x0.8)", "Обычная", "Высокая (x1.25)", "Максимальная (x1.5)" }, r.wage, i => r.wage = i);

            Heading(c, "Обучение (навык " + r.skill + "/5)");
            string[] opt = { "На смене", "Вне смены (×1.5, без потери мощности)" };
            var tr = UIKit.HBox(c, 8, 50);
            tr.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            var b1 = UIKit.Btn(tr, "Навык (на смене): " + UIKit.Money(Simulation.TrainCost(r, 0, false)), () => Act(Simulation.Train(S, r, 0, false)), UIKit.Good, 16, -1, 50); b1.interactable = r.skill < 5;
            var b2 = UIKit.Btn(tr, "Навык (вне смены): " + UIKit.Money(Simulation.TrainCost(r, 0, true)), () => Act(Simulation.Train(S, r, 0, true)), UIKit.Good, 16, -1, 50); b2.interactable = r.skill < 5;
            var b3 = UIKit.Btn(tr, "Здоровье (вне смены): " + UIKit.Money(Simulation.TrainCost(r, 1, true)), () => Act(Simulation.Train(S, r, 1, true)), UIKit.Good, 16, -1, 50); b3.interactable = r.health < 99f;
            var l = UIKit.Label(c, "Обучение на смене снижает мощность вдвое на 3 дня.", 16, UIKit.Dim); UIKit.Size(l.gameObject, -1, 24);

            Heading(c, "Охрана (нужна с уровня штаба 2)");
            int need = Simulation.GuardsNeeded(r);
            Stepper(c, "Охранники (нужно " + need + ")", r.guards.ToString(), () => { r.guards--; Refresh(); }, () => { r.guards++; Refresh(); }, r.guards > 0, r.guards < 5);
            var gs = UIKit.Btn(c, "Обучить охрану до уровня " + (r.guardSkill + 1) + ": $800", () =>
            {
                if (S.Player.cash < 800) { Toast("Недостаточно денег."); return; }
                S.Player.cash -= 800; r.guardSkill++; Refresh();
            }, null, 18, -1, 44);
            gs.interactable = r.guardSkill < 3;
            UIKit.Row(c, "Охрана снижает шанс рэкета и саботажа; лишние охранники отпугивают клиентов", "", UIKit.Dim, 16);
            UIKit.Row(c, "Мощность", "~" + Mathf.RoundToInt(Mathf.Min(r.cooks * 95f, r.waiters * 85f, size.seats * 7f) * (0.8f + r.skill * 0.12f) * (r.shifts >= 3 ? 1.35f : 1f) * (r.manager ? 1f : 0.85f)) + " гостей/день");
        }

        // ---- interior
        void TabInterior(Transform c)
        {
            var r = Switcher(c); if (r == null) return;
            Heading(c, "Стиль (смена: 35% стоимости постройки)");
            var sr = UIKit.HBox(c, 6, 50);
            sr.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            for (int i = 0; i < GameData.Styles.Length; i++)
            {
                int ii = i;
                UIKit.Btn(sr, GameData.Styles[i].name + (i == r.style ? "" : "\n" + UIKit.Money(Simulation.StyleCost(S, r, i))), () => { Act(Simulation.ChangeStyle(S, r, ii)); G.RefreshWorld(); },
                    r.style == i ? UIKit.Accent : UIKit.PanelLight, 15, -1, 56);
            }
            Heading(c, "Отделка и мебель");
            string[] decorNames = { "Скромная", "Хорошая", "Роскошная" };
            UIKit.Row(c, "Отделка: " + decorNames[r.decor], r.decor >= 2 ? "максимум" : UIKit.Money(Simulation.DecorCost(r)));
            var up = UIKit.Btn(c, "Улучшить отделку", () => Act(Simulation.UpgradeDecor(S, r)), UIKit.Good, 20, -1, 46); up.interactable = r.decor < 2;
            var pl = UIKit.Btn(c, r.plants ? "Растения есть (смягчают стилевой слом)" : "Купить растения: $400", () => Act(Simulation.BuyPlants(S, r)), null, 20, -1, 46); pl.interactable = !r.plants;
            Meter(c, "Состояние мебели", r.condition / 100f, r.condition < 40 ? UIKit.Bad : UIKit.Good);
            Meter(c, "Чистота", r.cleanliness / 100f, r.cleanliness < 50 ? UIKit.Bad : UIKit.Good);
            UIKit.Btn(c, "Реставрация мебели: " + UIKit.Money(Simulation.RestoreCost(r)), () => Act(Simulation.Restore(S, r)), null, 20, -1, 46);

            Heading(c, "Печи, музыка, расстановка");
            UIKit.Row(c, "Печи: уровень " + r.oven + " (+8% мощности и качества за уровень)", r.oven >= 3 ? "максимум" : UIKit.Money(Simulation.OvenPrice(r)));
            var ob = UIKit.Btn(c, "Купить печи лучше", () => Act(Simulation.BuyOven(S, r)), UIKit.Good, 20, -1, 46); ob.interactable = r.oven < 3;
            Choice(c, Simulation.MusicNames, r.music, i => Act(Simulation.SetMusic(S, r, i)));
            var lb2 = UIKit.Btn(c, r.layout >= 2 ? "Расстановка столов: симметричная (идеал)" : (r.layout == 0 ? "Улучшить тесную расстановку: " : "Симметричная расстановка (штаб 2+): ") + (r.layout >= 2 ? "" : UIKit.Money(Simulation.LayoutPrice(r))), () => Act(Simulation.UpgradeLayout(S, r)), null, 20, -1, 46);
            lb2.interactable = r.layout < 2;
            if (r.ratsDays > 0)
            {
                var rl = UIKit.Label(c, "В кухне крысы! Ещё " + r.ratsDays + " дн. Инспекция оштрафует и сузит круг влияния.", 18, UIKit.Bad); UIKit.Size(rl.gameObject, -1, 30);
                UIKit.Btn(c, "Дезинфекция: $400", () => Act(Simulation.Disinfect(S, r)), UIKit.Bad, 20, -1, 46);
            }
            UIKit.Row(c, "Круг влияния ресторана", Mathf.RoundToInt(r.influence * 100) + "%  (растёт от призов и конкурсов, падает от штрафов инспекции)");

            Heading(c, "Как стиль нравится группам клиентов");
            for (int i = 0; i < 8; i++)
            {
                var gd = GameData.Groups[i];
                string fit = Array.IndexOf(gd.liked, r.style) >= 0 ? "любят" : Array.IndexOf(gd.ok, r.style) >= 0 ? "допускают" : "не любят";
                var color = fit == "любят" ? UIKit.Good : fit == "допускают" ? UIKit.TextCol : UIKit.Bad;
                var h = UIKit.HBox(c, 8, 30);
                var n = UIKit.Label(h, gd.name, 20, gd.color); UIKit.Size(n.gameObject, 170);
                var likes = string.Join(", ", gd.liked.Select(x => GameData.Styles[x].name));
                var lb = UIKit.Label(h, "любят: " + likes, 18, UIKit.Dim); UIKit.Size(lb.gameObject, -1, -1, 1);
                var f = UIKit.Label(h, fit + " (оценка " + Mathf.RoundToInt(Simulation.DecorScore(r, i) * 100) + "%)", 18, color, TextAnchor.MiddleRight); UIKit.Size(f.gameObject, 260);
            }
        }

        // ---- advertising
        void TabAds(Transform c)
        {
            var r = Switcher(c); if (r == null) return;
            var groupNames = new List<string> { "Все" }; groupNames.AddRange(GameData.Groups.Select(x => x.name));

            Heading(c, "Реклама ресторана");
            Choice(c, new[] { "Нет", "Листовки $50", "Радио $150", "ТВ $400" }, r.marketing, i => r.marketing = i);
            Heading(c, "Целевая группа рекламы");
            Choice(c, groupNames.ToArray(), r.adTarget + 1, i => r.adTarget = i - 1, 14);
            Heading(c, "Обещание в рекламе (ложное обещание срезает эффект и репутацию)");
            Choice(c, new[] { "Без обещаний", "Качество", "Низкие цены", "Сервис" }, r.adPromise, i => r.adPromise = i);
            if (r.adPromise > 0 && r.marketing > 0)
            {
                bool ok = Simulation.PromiseTrue(S, r);
                var l = UIKit.Label(c, ok ? "Обещание выполняется: реклама работает сильнее." : "Обещание НЕ выполняется: эффект рекламы упал, репутация падает!", 20, ok ? UIKit.Good : UIKit.Bad);
                UIKit.Size(l.gameObject, -1, 30);
            }

            Heading(c, "Реклама на весь город (выгодна при 3+ ресторанах)");
            Choice(c, new[] { "Нет", "Плакаты $120/д", "Радио $320/д", "ТВ $800/д" }, S.Player.cityAd, i => S.Player.cityAd = i);
            Choice(c, groupNames.ToArray(), S.Player.cityAdTarget + 1, i => S.Player.cityAdTarget = i - 1, 14);
            var tip = UIKit.Label(c, "Городская и локальная реклама вместе дорого стоят и дают лишь небольшой прирост. На уровне штаба 4 городская реклама сильнее на 20%.", 16, UIKit.Dim); UIKit.Size(tip.gameObject, -1, 44);

            Heading(c, "Плакаты в районах ($60/день каждый; перехватывают прохожих у конкурентов)");
            var pg = UIKit.Rect("Posters", c);
            var pgl = pg.gameObject.AddComponent<GridLayoutGroup>();
            pgl.cellSize = new Vector2(250, 44); pgl.spacing = new Vector2(8, 8);
            pgl.constraint = GridLayoutGroup.Constraint.FixedColumnCount; pgl.constraintCount = 5;
            pg.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int di = 0; di < S.City.districts.Count; di++)
            {
                int dd = di; bool on = S.Player.posters.Contains(di);
                UIKit.Btn(pg, (on ? "[x] " : "") + S.City.districts[di].name, () => { Simulation.TogglePoster(S, dd); Refresh(); }, on ? UIKit.Good : UIKit.PanelLight, 16, 250, 44);
            }

            Heading(c, "Доставка (нужны курьеры)");
            Choice(c, new[] { "Без доставки", "Доставка по городу: использует свободную мощность, до 45 заказов на курьера" }, r.delivery ? 1 : 0, i => r.delivery = i == 1);
            UIKit.Row(c, "Курьеров / доставлено вчера", r.runners + " / " + r.deliveryOrders + " заказов");
        }

        // ---- headquarters
        void TabHq(Transform c)
        {
            var p = S.Player;
            Heading(c, "Штаб: уровень " + p.hq + " из 4");
            UIKit.Row(c, "Лимит ресторанов", S.RestaurantsOf(0).Count + " / " + GameState.MaxRestaurants(p.hq));
            UIKit.Row(c, "Содержание штаба в день", UIKit.Money(GameState.HqUpkeep(p.hq)));
            var up = UIKit.Btn(c, p.hq >= 4 ? "Штаб на максимуме" : "Улучшить штаб до уровня " + (p.hq + 1) + ": " + UIKit.Money(GameState.HqCost[Mathf.Min(4, p.hq + 1)]), () => Act(Simulation.UpgradeHq(S)), UIKit.Good, 22, -1, 52);
            up.interactable = p.hq < 4;
            var l = UIKit.Label(c, "Ур. 2: охрана в ресторанах. Ур. 3: криминальные акции. Ур. 4: поджоги, городская реклама сильнее. Золотое правило: штаб сразу, ресторан позже, склад только если нужно.", 17, UIKit.Dim); UIKit.Size(l.gameObject, -1, 52);

            Heading(c, "Мэр (взятка в день)");
            Choice(c, new[] { "Нет", "Благосклонность $90/д (+6% клиентов)", "Покровительство $220/д (+12%, защита от инспекций и налётов)" }, p.mayor, i => p.mayor = i);
            if (p.cash < 0) { var w = UIKit.Label(c, "Баланс отрицательный: взятки и реклама не оплачиваются, мэр не помогает!", 18, UIKit.Bad); UIKit.Size(w.gameObject, -1, 30); }

            int eff = Simulation.MayorLevel(S, 0);
            var ml = UIKit.Label(c, p.mayor == 0 ? "Мэр не на вашей стороне: звёзд не будет." : eff >= p.mayor ? "Мэр принимает вашу взятку (действует уровень " + eff + ")." : "Конкурент заплатил мэру больше: ваша взятка не действует!", 18, p.mayor > 0 && eff >= p.mayor ? UIKit.Good : UIKit.Bad); UIKit.Size(ml.gameObject, -1, 30);
            var ml2 = UIKit.Label(c, "Звёзды выдаёт мэр при визите (раз в 15 дней) и только если он на вашей стороне.", 16, UIKit.Dim); UIKit.Size(ml2.gameObject, -1, 26);

            Heading(c, "Полиция (взносы в день)");
            Choice(c, new[] { "Нет", "Участок $60/д (рэкет x0.6, саботаж против вас слабее)", "Усиленный $150/д (рэкет x0.35)" }, p.police, i => p.police = i);

            Heading(c, "Склад (охлаждение уменьшает потери продуктов)");
            UIKit.Row(c, "Уровень склада", p.warehouse + " / 2");
            var wb = UIKit.Btn(c, p.warehouse >= 2 ? "Склад улучшен полностью" : "Улучшить склад: " + UIKit.Money(Simulation.WarehouseCost[Mathf.Min(2, p.warehouse + 1)]), () => Act(Simulation.BuyWarehouse(S)), null, 20, -1, 46);
            wb.interactable = p.warehouse < 2;

            Heading(c, "Подполье (штаб 3+): грязных денег $" + Mathf.RoundToInt(p.dirty).ToString("N0"));
            var ur = UIKit.HBox(c, 8, 50);
            ur.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
            var rb = UIKit.Btn(ur, "Ограбить банк (экипаж $3 000, шанс 55%, до $50 000)", () => { var res = Simulation.RobBank(S); Act(res, res); }, UIKit.Bad, 17, -1, 50); rb.interactable = p.hq >= 3;
            var lw = UIKit.Btn(ur, "Отмыть деньги (курс 60%)", () => { var res = Simulation.Launder(S); Act(res, res); }, UIKit.PanelLight, 17, -1, 50); lw.interactable = p.dirty >= 1f;

            Heading(c, "Саботаж конкурентов");
            var warn = UIKit.Label(c, "Риск разоблачения: штраф $3 000 и потеря репутации. Охрана жертвы снижает шанс успеха.", 17, UIKit.Dim); UIKit.Size(warn.gameObject, -1, 28);
            var rivals = S.restaurants.Where(x => x.owner != 0).ToList();
            if (rivals.Count == 0) UIKit.Label(c, "Конкурентов нет.", 20, UIKit.Dim);
            foreach (var rv in rivals)
            {
                var card = UIKit.Box(c, UIKit.PanelLight, "Rival", true);
                var v = UIKit.VBox(card.transform, 4, 10); UIKit.Stretch(v);
                var co = S.companies[rv.owner];
                var t = UIKit.Label(v, rv.name + "  (реп. " + Mathf.RoundToInt(rv.reputation) + ", охрана " + rv.guards + (rv.closedDays > 0 ? ", закрыт " + rv.closedDays + " дн." : "") + ")", 20, co.color, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(t.gameObject, -1, 28);
                var row = UIKit.HBox(v, 6, 40);
                row.GetComponent<HorizontalLayoutGroup>().childForceExpandWidth = true;
                for (int k = 0; k < Simulation.SabotageNames.Length; k++)
                {
                    int kk = k; var target = rv;
                    var btn = UIKit.Btn(row, Simulation.SabotageNames[k] + "\n" + UIKit.Money(Simulation.SabotageCost[k]), () => { var res = Simulation.Sabotage(S, 0, target, kk); Act(res, res); }, UIKit.Bad, 13, -1, 40);
                    btn.interactable = p.hq >= Simulation.SabotageHq[k];
                }
                UIKit.Size(card.gameObject, -1, 100);
            }
        }

        // ---- customers
        void TabClients(Transform c)
        {
            var r = Switcher(c); if (r == null) return;
            var d = S.City.districts[r.district];
            Heading(c, "Клиенты района «" + d.name + "»");
            UIKit.Label(c, "В моде: " + string.Join(", ", S.trend.Select(id => GameData.Ingredients.Find(x => x.id == id).name)) + ". Оценка группы = (меню + интерьер + персонал) / 3.", 18, UIKit.Dim);
            var hdr = UIKit.HBox(c, 8, 26);
            foreach (var (txt, w) in new[] { ("Группа", 170), ("Доля", 80), ("Меню", 80), ("Интерьер", 90), ("Персонал", 90), ("Оценка", 90) })
            { var l = UIKit.Label(hdr, txt, 17, UIKit.Dim, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(l.gameObject, w); }
            for (int i = 0; i < 8; i++)
            {
                var gd = GameData.Groups[i];
                var card = UIKit.Box(c, UIKit.PanelLight, "G", true);
                var v = UIKit.VBox(card.transform, 2, 8); UIKit.Stretch(v);
                var h = UIKit.HBox(v, 8, 30);
                var n = UIKit.Label(h, gd.name, 20, gd.color, TextAnchor.MiddleLeft, FontStyle.Bold); UIKit.Size(n.gameObject, 170);
                float m = Simulation.MenuScore(S, r, i), de = Simulation.DecorScore(r, i), st = Simulation.StaffScore(S, r, i), tot = (m + de + st) / 3f;
                foreach (var (val, w, col) in new[] { (Mathf.RoundToInt(d.mix[i] * 100) + "%", 80, UIKit.TextCol), (Mathf.RoundToInt(m * 100) + "%", 80, UIKit.TextCol),
                         (Mathf.RoundToInt(de * 100) + "%", 90, UIKit.TextCol), (Mathf.RoundToInt(st * 100) + "%", 90, UIKit.TextCol), (Mathf.RoundToInt(tot * 100) + "%", 90, tot >= 0.7f ? UIKit.Good : tot < 0.5f ? UIKit.Bad : UIKit.TextCol) })
                { var l = UIKit.Label(h, val, 20, col); UIKit.Size(l.gameObject, w); }
                var desc = UIKit.Label(v, gd.desc + "  |  любимые стили: " + string.Join(", ", gd.liked.Select(x => GameData.Styles[x].name)), 15, UIKit.Dim); UIKit.Size(desc.gameObject, -1, 22);
                UIKit.Size(card.gameObject, -1, 76);
            }
            var bsl = UIKit.Label(c, "Звёзды: мэр при визите (раз в 15 дней, нужна взятка) даёт по звезде за каждые 3 пункта оценки богачей выше " + Simulation.StarMinimum + "%. Оценка богачей сейчас " + Mathf.RoundToInt(r.rating[0] * 100) + "% => звёзд " + Simulation.StarsFor(r) + " (сейчас " + r.stars + ").", 18, UIKit.Accent);
            UIKit.Size(bsl.gameObject, -1, 52);
        }
    }
}
