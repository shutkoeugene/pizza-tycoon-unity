using UnityEngine;
using UnityEngine.EventSystems;

namespace PizzaTycoon
{
    public class GameController : MonoBehaviour
    {
        public static GameController I;

        public GameState State;
        public CityView City;
        public CameraRig Rig;
        public GameUI UI;
        public int Speed = 1;           // 0 paused, 1..3
        public float DayLength = 12f;   // seconds at speed 1
        public float DayTime;           // 0..DayLength
        public int SelectedDistrict = -1;
        public int SelectedRestaurant = -1; // restaurant id
        public bool InGame;

        void Awake() { I = this; }

        public Restaurant CurrentRestaurant()
        {
            if (State == null) return null;
            var r = State.restaurants.Find(x => x.id == SelectedRestaurant && x.owner == 0);
            if (r == null)
            {
                var mine = State.RestaurantsOf(0);
                if (mine.Count > 0) { r = mine[0]; SelectedRestaurant = r.id; }
            }
            return r;
        }

        public void StartNew(int cityIdx, string playerName)
        {
            State = GameState.NewGame(cityIdx, 25000f, playerName);
            EnterGame();
        }

        public void LoadGame(int slot)
        {
            var s = GameState.Load(slot);
            if (s == null) { UI.Toast("Сохранение не найдено."); return; }
            State = s;
            if (State.ingredientMul.Count != GameData.Ingredients.Count)
            {
                State.ingredientMul.Clear();
                foreach (var _ in GameData.Ingredients) State.ingredientMul.Add(1f);
            }
            EnterGame();
        }

        void EnterGame()
        {
            InGame = true; Speed = 1; DayTime = 0;
            SelectedDistrict = -1; SelectedRestaurant = -1;
            City.Rebuild(State);
            var cd = State.City;
            Rig.Init(City.CityCenter(), new Vector3(-20, 0, -20), new Vector3((cd.districtsX) * CityView.Cell, 0, (cd.districtsY) * CityView.Cell));
            Rig.FocusOn(City.CityCenter(), 85f);
            UI.ShowHud();
        }

        public void ExitToMenu() { InGame = false; Speed = 0; UI.ShowMainMenu(); }

        public void RefreshWorld() { City.Rebuild(State); City.Highlight(SelectedDistrict); }

        public void SelectDistrict(int i)
        {
            SelectedDistrict = i;
            City.Highlight(i);
            if (i >= 0) Rig.FocusOn(City.DistrictCenter(i), 42f);
            UI.RefreshSidebar();
        }

        void Update()
        {
            if (!InGame || State == null) return;

            if (Input.GetKeyDown(KeyCode.Space)) Speed = Speed == 0 ? 1 : 0;
            if (Input.GetKeyDown(KeyCode.Alpha1)) Speed = 1;
            if (Input.GetKeyDown(KeyCode.Alpha2)) Speed = 2;
            if (Input.GetKeyDown(KeyCode.Alpha3)) Speed = 3;
            if (Input.GetKeyDown(KeyCode.Escape)) UI.ToggleEscapeMenu();

            // click on world
            if (Input.GetMouseButtonUp(0) && !(EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
            {
                if (!UI.ModalOpen && (Input.mousePosition - clickStart).sqrMagnitude < 36f)
                {
                    var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit;
                    if (Physics.Raycast(ray, out hit, 500f))
                    {
                        var rt = hit.collider.GetComponentInParent<RestaurantTag>();
                        var dt = hit.collider.GetComponent<DistrictTag>();
                        if (rt != null)
                        {
                            var r = State.restaurants.Find(x => x.id == rt.restaurantId);
                            if (r != null)
                            {
                                SelectDistrict(r.district);
                                if (r.owner == 0) SelectedRestaurant = r.id;
                                UI.RefreshSidebar();
                            }
                        }
                        else if (dt != null) SelectDistrict(dt.index);
                    }
                }
            }
            if (Input.GetMouseButtonDown(0)) clickStart = Input.mousePosition;

            if (Speed > 0 && !State.bankrupt)
            {
                DayTime += Time.deltaTime * Speed;
                if (DayTime >= DayLength)
                {
                    DayTime -= DayLength;
                    Simulation.Step(State);
                    UI.OnNewDay();
                    if (State.bankrupt) { Speed = 0; UI.ShowBankrupt(); }
                    else if (State.won && !wonShown) { wonShown = true; Speed = 0; UI.ShowWin(); }
                    if (!State.won) wonShown = false;
                    if (State.day % 30 == 0) State.Save(0); // autosave
                }
            }
            City.SetTimeOfDay(DayTime / DayLength);
        }

        Vector3 clickStart;
        bool wonShown;
    }
}
