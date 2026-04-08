/*
 * Curling Game - SweepSystem
 *
 * Allows the player to sweep in front of a moving stone to help it
 * travel farther and reduce curl.
 *
 * Sweeping works by reducing ice friction for the active stone while
 * the player rapidly clicks (or holds) near the stone's path.
 *
 * Attach to the same GameObject as StoneShooter.
 */
using UnityEngine;

namespace Curling
{
    public class SweepSystem : MonoBehaviour
    {
        // ── Inspector fields ────────────────────────────────────────────────
        [Header("Sweep Settings")]
        [Tooltip("How much extra velocity the stone gains per sweep frame")]
        public float SweepBoost = 0.002f;

        [Tooltip("How far in front of the stone the player must click to sweep")]
        public float SweepRadius = 1.5f;

        // ── State ────────────────────────────────────────────────────────────
        public bool IsSweeping { get; private set; }

        private CurlingStone _activeStone;
        private Camera _cam;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _cam = Camera.main;
        }

        /// <summary>Called by the GameManager when a stone is thrown.</summary>
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

            HandleSweepInput();
        }

        // ── Input ────────────────────────────────────────────────────────────

        private void HandleSweepInput()
        {
            IsSweeping = false;

            if (!Input.GetMouseButton(0))
                return;

            /*   Cast a ray from the camera through the mouse position.
             *   If the click lands close enough to the stone, sweep!    */
            Ray ray = _cam.ScreenPointToRay(Input.mousePosition);
            Plane icePlane = new Plane(Vector3.up, Vector3.zero);

            if (icePlane.Raycast(ray, out float distance))
            {
                Vector3 clickPoint = ray.GetPoint(distance);
                float distToStone = Vector3.Distance(clickPoint, _activeStone.transform.position);

                if (distToStone <= SweepRadius)
                {
                    IsSweeping = true;
                }
            }
        }

        private void FixedUpdate()
        {
            if (IsSweeping && _activeStone != null && _activeStone.IsMoving)
            {
                _activeStone.ApplySweepBoost(SweepBoost);
            }
        }
    }
}
