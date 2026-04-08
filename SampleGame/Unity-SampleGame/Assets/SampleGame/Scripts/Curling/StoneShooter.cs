/*
 * Curling Game - StoneShooter
 *
 * Handles player input for aiming and throwing a curling stone.
 *
 * Flow:
 *   1. Player clicks to start the aim phase — a directional arrow appears.
 *   2. Player moves the mouse left/right to adjust aim.
 *   3. Player holds the throw button to charge power (shown by a power bar).
 *   4. Player releases to launch the stone.
 *
 * Attach to an empty "Shooter" GameObject in the scene.
 * Assign the StonePrefab and AimArrow references in the Inspector.
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
        public float PowerChargeRate = 8f;  // units per second

        [Header("Spin")]
        [Tooltip("+1 = clockwise curl right, -1 = counter-clockwise curl left")]
        public float SpinDirection = 1f;

        // ── State ────────────────────────────────────────────────────────────
        public float CurrentPower { get; private set; }  // 0..1, read by UI
        public bool IsAiming { get; private set; }
        public bool IsCharging { get; private set; }

        private Vector3 _aimDirection;
        private GameObject _previewStone;  // ghost stone shown during aiming
        private CurlingGameManager _gameManager;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _gameManager = FindFirstObjectByType<CurlingGameManager>();
            _aimDirection = Vector3.forward; // default: straight down the sheet
        }

        private void Update()
        {
            if (!_gameManager.IsPlayerTurn)
                return;

            HandleAimInput();
            HandleSpinToggle();
            HandleThrowInput();
            UpdateAimVisual();
        }

        // ── Input handlers ───────────────────────────────────────────────────

        private void HandleAimInput()
        {
            if (!IsAiming)
            {
                // Begin aiming on left-click press
                if (Input.GetMouseButtonDown(0))
                {
                    IsAiming = true;
                    SpawnPreviewStone();
                }
                return;
            }

            /*   Rotate aim direction left/right using horizontal mouse movement.
             *   Sensitivity is intentionally low — real curling has tiny aim
             *   adjustments that make a big difference down the sheet.        */
            float mouseX = Input.GetAxis("Mouse X");
            _aimDirection = Quaternion.Euler(0f, mouseX * 2f, 0f) * _aimDirection;
        }

        private void HandleSpinToggle()
        {
            // Q / E keys toggle which way the stone curls
            if (Input.GetKeyDown(KeyCode.Q)) SpinDirection = -1f;
            if (Input.GetKeyDown(KeyCode.E)) SpinDirection = 1f;
        }

        private void HandleThrowInput()
        {
            if (!IsAiming)
                return;

            if (Input.GetMouseButton(0))
            {
                /*   Hold left-click to charge throw power.
                 *   Power is clamped 0→1, then mapped to min/max speed.   */
                IsCharging = true;
                CurrentPower = Mathf.Clamp01(CurrentPower + Time.deltaTime * PowerChargeRate / MaxThrowSpeed);
            }

            if (Input.GetMouseButtonUp(0) && IsCharging)
            {
                LaunchStone();
            }
        }

        // ── Stone launch ─────────────────────────────────────────────────────

        private void LaunchStone()
        {
            // Remove ghost, spawn real stone
            if (_previewStone != null)
                Destroy(_previewStone);

            GameObject stoneObj = Instantiate(StonePrefab, ThrowOrigin.position, Quaternion.identity);
            CurlingStone stone = stoneObj.GetComponent<CurlingStone>();
            stone.TeamIndex = _gameManager.CurrentTeam;
            stone.OnStoneStopped += _gameManager.OnStoneStopped;

            float speed = Mathf.Lerp(MinThrowSpeed, MaxThrowSpeed, CurrentPower);
            stone.Throw(_aimDirection.normalized, speed, SpinDirection);

            // Reset shooter state
            IsAiming = false;
            IsCharging = false;
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

            // Disable physics and stone script on the preview
            Rigidbody rb = _previewStone.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            CurlingStone cs = _previewStone.GetComponent<CurlingStone>();
            if (cs != null) cs.enabled = false;

            // Make it semi-transparent if it has a Renderer
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
            if (!IsAiming || AimLine == null)
                return;

            AimLine.enabled = true;
            AimLine.SetPosition(0, ThrowOrigin.position);
            AimLine.SetPosition(1, ThrowOrigin.position + _aimDirection.normalized * 5f);
        }
    }
}
