/*
 * Curling Game - StoneShooter  (iOS touch edition)
 *
 * Handles player input for aiming and throwing a curling stone.
 * Uses TouchInputHelper so the same code runs on iOS and in the Editor.
 *
 * Touch gesture flow:
 *   Phase 1 – AIM
 *     • Tap anywhere to enter aim mode; a ghost stone appears.
 *     • Drag left/right to rotate the aim direction.
 *   Phase 2 – CHARGE
 *     • Lift finger, then tap-and-hold to charge throw power.
 *     • Power bar fills while the finger is held.
 *   Phase 3 – THROW
 *     • Release to launch the stone.
 *
 *   Spin direction is toggled by the on-screen buttons wired in
 *   CurlingUIManager (SpinLeftButton / SpinRightButton).
 *
 * Attach to an empty "Shooter" GameObject in the scene.
 */
using UnityEngine;

namespace Curling
{
    public class StoneShooter : MonoBehaviour
    {
        // ── Inspector fields ────────────────────────────────────────────────
        [Header("References")]
        public GameObject StonePrefab;      // Prefab with CurlingStone + Rigidbody
        public Transform ThrowOrigin;       // Where the stone is placed before release
        public LineRenderer AimLine;        // Visual aim guide (optional)

        [Header("Throw Settings")]
        public float MinThrowSpeed = 5f;
        public float MaxThrowSpeed = 20f;
        public float PowerChargeRate = 8f;  // units of power per second

        [Header("Aim Sensitivity")]
        [Tooltip("How many degrees per pixel of horizontal finger drag")]
        public float AimSensitivity = 0.3f;

        [Header("Spin")]
        [Tooltip("+1 = clockwise curl right, -1 = counter-clockwise curl left")]
        public float SpinDirection = 1f;

        // ── State (read by CurlingUIManager) ────────────────────────────────
        public float CurrentPower { get; private set; }   // 0..1
        public bool IsAiming { get; private set; }
        public bool IsCharging { get; private set; }

        // ── Private ──────────────────────────────────────────────────────────
        private enum ThrowPhase { Idle, Aiming, Charging }
        private ThrowPhase _phase = ThrowPhase.Idle;

        private Vector3 _aimDirection;
        private GameObject _previewStone;
        private CurlingGameManager _gameManager;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _gameManager = FindFirstObjectByType<CurlingGameManager>();
            _aimDirection = Vector3.forward;
        }

        private void Update()
        {
            if (!_gameManager.IsPlayerTurn)
                return;

            TouchData touch = TouchInputHelper.GetPrimaryTouch();

            switch (_phase)
            {
                case ThrowPhase.Idle:     HandleIdlePhase(touch);     break;
                case ThrowPhase.Aiming:   HandleAimingPhase(touch);   break;
                case ThrowPhase.Charging: HandleChargingPhase(touch); break;
            }

            UpdateAimVisual();

            // Keep public flags in sync for the UI
            IsAiming   = _phase == ThrowPhase.Aiming;
            IsCharging = _phase == ThrowPhase.Charging;
        }

        // ── Phase handlers ───────────────────────────────────────────────────

        private void HandleIdlePhase(TouchData touch)
        {
            /*   First tap → enter aim mode and show ghost stone.           */
            if (touch.Began)
            {
                _phase = ThrowPhase.Aiming;
                SpawnPreviewStone();
            }
        }

        private void HandleAimingPhase(TouchData touch)
        {
            /*   While the finger drags, rotate the aim direction.
             *   Horizontal drag maps to yaw rotation.                      */
            if (touch.Held || touch.Began)
            {
                float degrees = touch.DeltaPosition.x * AimSensitivity;
                _aimDirection = Quaternion.Euler(0f, degrees, 0f) * _aimDirection;
            }

            /*   Lifting the finger ends aiming and moves to charging.      */
            if (touch.Ended)
            {
                _phase = ThrowPhase.Charging;
                CurrentPower = 0f;
            }
        }

        private void HandleChargingPhase(TouchData touch)
        {
            /*   Hold to charge; release to throw.                          */
            if (touch.Began || touch.Held)
            {
                CurrentPower = Mathf.Clamp01(
                    CurrentPower + Time.deltaTime * PowerChargeRate / MaxThrowSpeed);
            }

            if (touch.Ended)
            {
                LaunchStone();
            }
        }

        // ── Spin toggle (called by UI buttons) ───────────────────────────────

        /// <summary>Called by the "Spin Left" on-screen button.</summary>
        public void SetSpinLeft()  => SpinDirection = -1f;

        /// <summary>Called by the "Spin Right" on-screen button.</summary>
        public void SetSpinRight() => SpinDirection =  1f;

        // ── Stone launch ─────────────────────────────────────────────────────

        private void LaunchStone()
        {
            if (_previewStone != null)
                Destroy(_previewStone);

            GameObject stoneObj = Instantiate(StonePrefab, ThrowOrigin.position, Quaternion.identity);
            CurlingStone stone  = stoneObj.GetComponent<CurlingStone>();
            stone.TeamIndex = _gameManager.CurrentTeam;
            stone.OnStoneStopped += _gameManager.OnStoneStopped;

            float speed = Mathf.Lerp(MinThrowSpeed, MaxThrowSpeed, CurrentPower);
            stone.Throw(_aimDirection.normalized, speed, SpinDirection);

            _phase = ThrowPhase.Idle;
            CurrentPower = 0f;

            _gameManager.StoneThrown(stone);

            if (AimLine != null)
                AimLine.enabled = false;
        }

        // ── Visual helpers ───────────────────────────────────────────────────

        private void SpawnPreviewStone()
        {
            if (_previewStone != null)
                Destroy(_previewStone);

            _previewStone = Instantiate(StonePrefab, ThrowOrigin.position, Quaternion.identity);

            Rigidbody rb = _previewStone.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            CurlingStone cs = _previewStone.GetComponent<CurlingStone>();
            if (cs != null) cs.enabled = false;

            Renderer r = _previewStone.GetComponent<Renderer>();
            if (r != null)
            {
                Color c = r.material.color;
                c.a = 0.4f;
                r.material.color = c;
            }
        }

        private void UpdateAimVisual()
        {
            if (_phase != ThrowPhase.Aiming || AimLine == null)
            {
                if (AimLine != null) AimLine.enabled = false;
                return;
            }

            AimLine.enabled = true;
            AimLine.SetPosition(0, ThrowOrigin.position);
            AimLine.SetPosition(1, ThrowOrigin.position + _aimDirection.normalized * 5f);
        }
    }
}
