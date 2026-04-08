/*
 * Curling Game - SweepSystem  (iOS touch edition)
 *
 * Allows the player to sweep in front of a moving stone to help it
 * travel farther and reduce curl.
 *
 * iOS gesture:
 *   While a stone is moving, tap and hold anywhere near it to sweep.
 *   Multi-touch is supported — a second finger can sweep while the
 *   first tracks the stone (useful for two-player local co-op on iPad).
 *
 * Uses TouchInputHelper for Editor/iOS compatibility.
 * Attach to the same GameObject as StoneShooter.
 */
using UnityEngine;

namespace Curling
{
    public class SweepSystem : MonoBehaviour
    {
        // ── Inspector fields ────────────────────────────────────────────────
        [Header("Sweep Settings")]
        [Tooltip("Extra velocity multiplier applied to the stone per FixedUpdate while sweeping")]
        public float SweepBoost = 0.002f;

        [Tooltip("Maximum distance (world units) a touch can be from the stone to count as sweeping")]
        public float SweepRadius = 1.5f;

        // ── State (read by CurlingUIManager) ────────────────────────────────
        public bool IsSweeping { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────
        private CurlingStone _activeStone;
        private Camera _cam;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _cam = Camera.main;
        }

        /// <summary>Called by CurlingGameManager when a stone is thrown.</summary>
        public void TrackStone(CurlingStone stone)
        {
            _activeStone = stone;
        }

        private void Update()
        {
            if (_activeStone == null || !_activeStone.IsMoving)
            {
                IsSweeping = false;
                return;
            }

            IsSweeping = AnyTouchNearStone();
        }

        private void FixedUpdate()
        {
            if (IsSweeping && _activeStone != null && _activeStone.IsMoving)
                _activeStone.ApplySweepBoost(SweepBoost);
        }

        // ── Touch detection ──────────────────────────────────────────────────

        private bool AnyTouchNearStone()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            return MouseNearStone();
#else
            return FingerNearStone();
#endif
        }

        private bool MouseNearStone()
        {
            /*   Editor fallback: left-click held near the stone sweeps it.  */
            if (!Input.GetMouseButton(0))
                return false;

            return ScreenPointNearStone(Input.mousePosition);
        }

        private bool FingerNearStone()
        {
            /*   Check every active touch — any finger near the stone sweeps.
             *   On iPhone a player will typically use their non-throwing thumb.
             *   On iPad two players can sweep simultaneously.               */
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch t = Input.GetTouch(i);
                if (t.phase == TouchPhase.Canceled || t.phase == TouchPhase.Ended)
                    continue;

                if (ScreenPointNearStone(t.position))
                    return true;
            }
            return false;
        }

        private bool ScreenPointNearStone(Vector2 screenPoint)
        {
            Ray ray = _cam.ScreenPointToRay(screenPoint);
            Plane icePlane = new Plane(Vector3.up, Vector3.zero);

            if (!icePlane.Raycast(ray, out float distance))
                return false;

            Vector3 worldPoint = ray.GetPoint(distance);
            return Vector3.Distance(worldPoint, _activeStone.transform.position) <= SweepRadius;
        }
    }
}
