using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace PizzaTycoon
{
    // "-uidump=PATH": writes visible button / district screen positions + game state so an external
    // driver can play with the real mouse.
    public class UiDump : MonoBehaviour
    {
        public string path; float next;
        static string E(string s) => s.Replace("\\", "\\\\").Replace("\"", "'").Replace("\n", " ");
        void Update()
        {
            if (Time.unscaledTime < next) return; next = Time.unscaledTime + 0.2f;
            var g = GameController.I; var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;
            sb.Append("{\"w\":").Append(Screen.width).Append(",\"h\":").Append(Screen.height);
            if (g != null && g.State != null && g.InGame)
            {
                var mine = g.State.RestaurantsOf(0); float rep = 0; foreach (var r in mine) rep += r.reputation; if (mine.Count > 0) rep /= mine.Count;
                sb.Append(",\"ingame\":true,\"day\":").Append(g.State.day).Append(",\"cash\":").Append(g.State.Player.cash.ToString("0", ci))
                  .Append(",\"loan\":").Append(g.State.Player.loan.ToString("0", ci)).Append(",\"rest\":").Append(mine.Count).Append(",\"rep\":").Append(rep.ToString("0", ci))
                  .Append(",\"won\":").Append(g.State.won ? "true" : "false").Append(",\"bankrupt\":").Append(g.State.bankrupt ? "true" : "false").Append(",\"city\":\"").Append(E(g.State.City.name)).Append("\"");
            }
            else sb.Append(",\"ingame\":false");
            sb.Append(",\"buttons\":[");
            bool first = true;
            foreach (var b in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!b.isActiveAndEnabled || !b.interactable) continue;
                var rt = (RectTransform)b.transform; Vector2 p = RectTransformUtility.WorldToScreenPoint(null, rt.position);
                var mask = b.GetComponentInParent<RectMask2D>();
                if (mask != null && !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)mask.transform, p, null)) continue;
                var t = b.GetComponentInChildren<Text>();
                if (!first) sb.Append(","); first = false;
                sb.Append("{\"t\":\"").Append(E(t != null ? t.text : "")).Append("\",\"x\":").Append(p.x.ToString("0", ci)).Append(",\"y\":").Append(p.y.ToString("0", ci)).Append("}");
            }
            sb.Append("],\"districts\":[");
            first = true;
            if (g != null && g.InGame && Camera.main != null)
                foreach (var d in FindObjectsByType<DistrictTag>(FindObjectsSortMode.None))
                {
                    Vector3 p = Camera.main.WorldToScreenPoint(d.transform.position);
                    if (p.z < 0 || p.x < 60 || p.x > Screen.width - 440 || p.y < 100 || p.y > Screen.height - 110) continue;
                    if (!first) sb.Append(","); first = false;
                    sb.Append("{\"i\":").Append(d.index).Append(",\"x\":").Append(p.x.ToString("0", ci)).Append(",\"y\":").Append(p.y.ToString("0", ci)).Append("}");
                }
            sb.Append("]}");
            try { File.WriteAllText(path + ".tmp", sb.ToString()); File.Copy(path + ".tmp", path, true); } catch { }
        }
    }
}
