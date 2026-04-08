/*
 * Curling Game - CurlingStone
 *
 * Represents a single curling stone on the ice sheet.
 * Handles physics-based sliding with realistic curl (rotation-induced drift)
 * and friction that gradually brings the stone to rest.
 *
 * Attach to a stone prefab with a Rigidbody component.
 */
using UnityEngine;

namespace Curling
{
    public class CurlingStone : MonoBehaviour
    {
        // ── Public configuration ────────────────────────────────────────────
        [Header("Stone Settings")]
        public int TeamIndex = 0;           // 0 = red team, 1 = yellow team
        public float IceFriction = 0.98f;   // velocity multiplier per frame (close to 1 = slippery)
        public float CurlStrength = 0.012f; // how much angular spin affects lateral drift
        public float StopThreshold = 0.05f; // speed below which stone is considered stopped

        // ── State ───────────────────────────────────────────────────────────
        public bool IsMoving { get; private set; }
        public bool HasBeenThrown { get; private set; }

        // ── Private references ──────────────────────────────────────────────
        private Rigidbody _rb;
        private float _spinDirection = 0f;  // +1 clockwise, -1 counter-clockwise

        // ── Events ──────────────────────────────────────────────────────────
        public System.Action<CurlingStone> OnStoneStopped;

        // ───────────────────────────────────────────────────────────────────
        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();

            // Curling stones slide on a flat plane — lock Y and rotation axes
            // so the stone doesn't tip or fly off the ice.
            _rb.constraints = RigidbodyConstraints.FreezePositionY
                            | RigidbodyConstraints.FreezeRotationX
                            | RigidbodyConstraints.FreezeRotationZ;
        }

        private void FixedUpdate()
        {
            if (!HasBeenThrown)
                return;

            ApplyIceFriction();
            ApplyCurl();
            CheckIfStopped();
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Launch the stone in the given world-space direction with the given speed.
        /// spinDirection: +1 for clockwise (curls right), -1 for counter-clockwise (curls left).
        /// </summary>
        public void Throw(Vector3 direction, float speed, float spinDirection)
        {
            _spinDirection = Mathf.Sign(spinDirection);
            _rb.linearVelocity = direction.normalized * speed;
            HasBeenThrown = true;
            IsMoving = true;
        }

        /// <summary>
        /// Apply friction reduction while the stone is sweeping near it.
        /// Call this every FixedUpdate frame that sweeping is active.
        /// </summary>
        public void ApplySweepBoost(float frictionReduction)
        {
            // Sweeping raises the pebbled ice temperature slightly,
            // reducing friction and helping the stone travel farther.
            _rb.linearVelocity *= (1f + frictionReduction);
        }

        // ── Private helpers ──────────────────────────────────────────────────

        private void ApplyIceFriction()
        {
            /*   Each frame we reduce velocity by the friction coefficient.
             *   IceFriction = 0.98 means the stone keeps 98% of its speed
             *   every physics step — a gradual, realistic deceleration.  */
            _rb.linearVelocity *= IceFriction;
        }

        private void ApplyCurl()
        {
            if (_rb.linearVelocity.magnitude < 0.1f)
                return;

            /*   Real curling stones curl because the leading edge grips
             *   ice pebbles more than the trailing edge when spinning.
             *   We simulate this by nudging velocity perpendicular to
             *   the direction of travel, scaled by CurlStrength.          */
            Vector3 velocity = _rb.linearVelocity;
            Vector3 right = Vector3.Cross(velocity.normalized, Vector3.up);
            /*   _spinDirection flips which way the stone drifts.          */
            _rb.linearVelocity += right * _spinDirection * CurlStrength * velocity.magnitude;
        }

        private void CheckIfStopped()
        {
            if (IsMoving && _rb.linearVelocity.magnitude < StopThreshold)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
                IsMoving = false;
                OnStoneStopped?.Invoke(this);
            }
        }
    }
}
