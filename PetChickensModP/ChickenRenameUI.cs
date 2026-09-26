using System.Collections.Generic;
using UnityEngine;

namespace PetChickensMod
{
    public class ChickenRenameUI : MonoBehaviour
    {
        public static ChickenRenameUI Instance { get; private set; }

        // Name queued up for the next chicken placed in a coop
        private static string _pendingName;

        public static string ConsumePendingName()
        {
            string n = _pendingName;
            _pendingName = null;
            return n;
        }

        private bool           _active;
        private bool           _focusNext;
        private string         _input    = "";
        private int            _targetId = -1;   // -1 = pending (item in hand), ≥0 = live entity
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
                // Escape as fallback — IMGUI handles Enter more reliably via Event.current
                if (Input.GetKeyDown(KeyCode.Escape))
                    Close();
                return;
            }

            // Right-click while cursor is locked (not in any game menu)
            if (!Input.GetMouseButtonDown(1)) return;
            if (Cursor.lockState != CursorLockMode.Locked) return;

            World world = GameManager.Instance?.World;
            if (world == null) return;

            EntityPlayerLocal player = world.GetPrimaryPlayer();
            if (player == null) return;

            // Only trigger when holding the domesticatedChicken item
            ItemValue held = player.inventory?.holdingItemItemValue;
            if (held == null || held.IsEmpty()) return;
            if (held.ItemClass?.GetItemName() != "domesticatedChicken") return;

            // If there's a live pet chicken nearby, rename it directly
            var nearby = new List<Entity>();
            world.GetEntitiesInBounds(typeof(EntityAnimal),
                new Bounds(player.position, Vector3.one * 20f), nearby);

            Entity closest = null;
            float bestDist = 8f;
            foreach (Entity e in nearby)
            {
                if (!ChickenNestManager.IsPetChicken(e)) continue;
                float d = Vector3.Distance(e.position, player.position);
                if (d < bestDist) { closest = e; bestDist = d; }
            }

            if (closest != null)
            {
                ChickenNestManager.TryGetName(closest.entityId, out string current);
                Open(closest.entityId, current ?? "");
            }
            else
            {
                // No entity nearby — set the pending name for the next coop placement
                Open(-1, _pendingName ?? "");
            }
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
            if (name.Length > 0)
            {
                if (_targetId >= 0)
                    ChickenNestManager.SetName(_targetId, name);  // rename live entity
                else
                    _pendingName = name;                           // queue for next placement
            }
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
            // Detect Enter/Escape inside the IMGUI event loop — reliable when text field has focus
            if (Event.current.type == EventType.KeyDown)
            {
                if (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)
                {
                    SaveAndClose();
                    Event.current.Use();
                    return;
                }
                if (Event.current.keyCode == KeyCode.Escape)
                {
                    Close();
                    Event.current.Use();
                    return;
                }
            }

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
