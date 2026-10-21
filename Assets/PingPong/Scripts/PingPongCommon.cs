using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PingPong
{
    /// <summary>Which side of the table. The player is on -Z, the computer on +Z.</summary>
    public enum Side { Player = 0, AI = 1 }

    /// <summary>Table dimensions (real-world, in metres) and helpers.</summary>
    public static class Table
    {
        public const float Length = 2.74f;
        public const float Width = 1.525f;
        public const float Height = 0.76f;        // height of the playing surface
        public const float NetHeight = 0.1525f;
        public const float NetOverhang = 0.1525f; // how far the net reaches past the table edge
        public const float HalfLength = Length * 0.5f;
        public const float HalfWidth = Width * 0.5f;

        public static Side SideOf(float z) { return z < 0f ? Side.Player : Side.AI; }

        /// <summary>Z direction towards a side (player = -1, AI = +1).</summary>
        public static float Dir(Side s) { return s == Side.Player ? -1f : 1f; }

        public static Side Other(Side s) { return s == Side.Player ? Side.AI : Side.Player; }

        public static bool IsOverTable(Vector3 p)
        {
            return Mathf.Abs(p.x) <= HalfWidth && Mathf.Abs(p.z) <= HalfLength;
        }
    }

    /// <summary>Input that works with both the old Input Manager and the new Input System.</summary>
    public static class PPInput
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        /// <summary>Left click or Space – serve / new game.</summary>
        public static bool ActionPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                bool mouse = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
                bool key = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
                return mouse || key;
#else
                return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space);
#endif
            }
        }

        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }

        /// <summary>Returns 0, 1 or 2 for keys 1, 2, 3 (difficulty), otherwise -1.</summary>
        public static int DifficultyKey()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return -1;
            if (k.digit1Key.wasPressedThisFrame) return 0;
            if (k.digit2Key.wasPressedThisFrame) return 1;
            if (k.digit3Key.wasPressedThisFrame) return 2;
#else
            if (Input.GetKeyDown(KeyCode.Alpha1)) return 0;
            if (Input.GetKeyDown(KeyCode.Alpha2)) return 1;
            if (Input.GetKeyDown(KeyCode.Alpha3)) return 2;
#endif
            return -1;
        }
    }
}
