/*
 * Curling Game - TouchInputHelper
 *
 * Abstracts touch (iOS) and mouse (Editor/desktop) into one API so that
 * StoneShooter and SweepSystem work on both platforms without #ifdefs.
 *
 * On iOS:   reads Input.touches
 * In Editor: maps mouse buttons/position to the same fields
 *
 * Usage:
 *   var touch = TouchInputHelper.GetPrimaryTouch();
 *   if (touch.Began)  { ... }
 *   if (touch.Held)   { ... }
 *   if (touch.Ended)  { ... }
 *   Vector2 pos = touch.ScreenPosition;
 *   Vector2 delta = touch.DeltaPosition;
 */
using UnityEngine;

namespace Curling
{
    public readonly struct TouchData
    {
        // ── Touch lifecycle flags ────────────────────────────────────────────
        public readonly bool Began;   // first frame the finger touched down
        public readonly bool Held;    // finger is currently held down
        public readonly bool Ended;   // first frame the finger lifted

        // ── Position data ────────────────────────────────────────────────────
        public readonly Vector2 ScreenPosition;   // current pixel position
        public readonly Vector2 DeltaPosition;    // movement since last frame
        public readonly bool IsActive;            // any touch / mouse contact at all

        public TouchData(bool began, bool held, bool ended,
                         Vector2 screenPos, Vector2 delta, bool isActive)
        {
            Began = began;
            Held = held;
            Ended = ended;
            ScreenPosition = screenPos;
            DeltaPosition = delta;
            IsActive = isActive;
        }

        /// <summary>A TouchData representing no input.</summary>
        public static readonly TouchData None = new TouchData(
            false, false, false, Vector2.zero, Vector2.zero, false);
    }

    public static class TouchInputHelper
    {
        /// <summary>
        /// Returns normalised input for the primary finger (touch index 0)
        /// or the left mouse button when running in the Editor.
        /// </summary>
        public static TouchData GetPrimaryTouch()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return GetMouseAsFakeTouch();
#else
            return GetFirstRealTouch();
#endif
        }

        // ── Platform implementations ─────────────────────────────────────────

        private static TouchData GetFirstRealTouch()
        {
            /*   iOS provides Input.touches[], one entry per active finger.
             *   We only care about finger 0 (primary/single touch).       */
            if (Input.touchCount == 0)
                return TouchData.None;

            Touch t = Input.GetTouch(0);

            bool began  = t.phase == TouchPhase.Began;
            bool held   = t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary;
            bool ended  = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;

            return new TouchData(began, held, ended,
                                 t.position, t.deltaPosition, true);
        }

        private static Vector2 _lastMousePos;

        private static TouchData GetMouseAsFakeTouch()
        {
            /*   In the Unity Editor we simulate a single touch using the
             *   left mouse button so designers can test without a device.  */
            bool began  = Input.GetMouseButtonDown(0);
            bool held   = Input.GetMouseButton(0) && !began;
            bool ended  = Input.GetMouseButtonUp(0);
            bool active = began || held || ended;

            Vector2 currentPos = Input.mousePosition;
            Vector2 delta      = active ? currentPos - _lastMousePos : Vector2.zero;
            _lastMousePos      = currentPos;

            return new TouchData(began, held, ended, currentPos, delta, active);
        }
    }
}
