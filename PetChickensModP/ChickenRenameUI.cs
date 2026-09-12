using System.Collections.Generic;
using UnityEngine;

namespace PetChickensMod
{
    public class ChickenRenameUI : MonoBehaviour
    {
        public static ChickenRenameUI Instance { get; private set; }

        private const KeyCode RenameKey = KeyCode.N;

        private bool           _active;
        private bool           _focusNext;
        private string         _input    = "";
        private int            _targetId = -1;
        private Rect           _windowRect;
        private CursorLockMode _savedLock;
        private bool           _savedVisible;

        void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Update()
        {
            CoopWatcher.Tick();

            if (_active)
            {
                // Keyboard-driven save/cancel — reliable in Update, unlike IMGUI events
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    SaveAndClose();
                else if (Input.GetKeyDown(KeyCode.Escape))
                    Close();
                return; // don't process N key while typing
            }

            if (!Input.GetKeyDown(RenameKey)) return;

            World world = GameManager.Instance?.World;
            if (world == null) return;

            EntityPlayerLocal player = world.GetPrimaryPlayer();
            if (player == null) return;

            var nearby = new List<Entity>();
            world.GetEntitiesInBounds(typeof(EntityAnimal),
                new Bounds(player.position, Vector3.one * 12f), nearby);

            Entity closest = null;
            float bestDist = 6f;
            foreach (Entity e in nearby)
            {
                if (!ChickenNestManager.IsPetChicken(e)) continue;
                float d = Vector3.Distance(e.position, player.position);
                if (d < bestDist) { closest = e; bestDist = d; }
            }

            if (closest == null) return;

            ChickenNestManager.TryGetName(closest.entityId, out string current);
            Open(closest.entityId, current ?? "");
        }

        void Open(int entityId, string currentName)
        {
            _active     = true;
            _focusNext  = true;
            _targetId   = entityId;
            _input      = currentName;
            _windowRect = new Rect(Screen.width / 2f - 155f, Screen.height / 2f - 60f, 310f, 120f);

            // Release cursor so the player can click IMGUI buttons
            _savedLock    = Cursor.lockState;
            _savedVisible = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        void SaveAndClose()
        {
            string name = _input.Trim();
            if (name.Length > 0 && _targetId >= 0)
                ChickenNestManager.SetName(_targetId, name);
            Close();
        }

        void Close()
        {
            _active   = false;
            _targetId = -1;
            _input    = "";

            // Restore cursor state
            Cursor.lockState = _savedLock;
            Cursor.visible   = _savedVisible;
        }

        void OnGUI()
        {
            if (!_active) return;
            _windowRect = GUILayout.Window(88321, _windowRect, DrawWindow, "Name Your Chicken");
        }

        void DrawWindow(int _id)
        {
            GUILayout.Space(6f);
            GUI.SetNextControlName("ChickenRenameField");
            _input = GUILayout.TextField(_input, 40, GUILayout.Width(290f));

            if (_focusNext && Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("ChickenRenameField");
                _focusNext = false;
            }

            GUILayout.Space(8f);

            if (GUILayout.Button("OK"))
                SaveAndClose();

            if (GUILayout.Button("Cancel"))
                Close();

            GUI.DragWindow();
        }
    }
}
