/*
 * Curling Game - AITeam
 *
 * A simple AI opponent for team 1 (Yellow).
 * When it is the AI's turn the AITeam waits a short moment, then aims
 * at the button with a small random error and throws.
 *
 * Difficulty levels adjust:
 *   Easy   – large aim error, random power
 *   Medium – moderate error, reasonable power
 *   Hard   – small error, near-optimal power
 *
 * Attach to the same GameObject as StoneShooter and CurlingGameManager.
 */
using System.Collections;
using UnityEngine;

namespace Curling
{
    public class AITeam : MonoBehaviour
    {
        // ── Inspector fields ────────────────────────────────────────────────
        [Header("AI Settings")]
        public int AITeamIndex = 1;         // which team the AI controls

        public enum Difficulty { Easy, Medium, Hard }
        public Difficulty AIDifficulty = Difficulty.Medium;

        [Tooltip("Seconds the AI 'thinks' before throwing")]
        public float ThinkTime = 1.2f;

        [Header("References")]
        public GameObject StonePrefab;
        public Transform ThrowOrigin;

        // ── Error ranges per difficulty ──────────────────────────────────────
        //   (maximum angle deviation from perfect aim, in degrees)
        private static readonly float[] AimErrorByDifficulty  = { 15f, 7f, 2f };
        //   (power deviation from optimal, 0–1 scale)
        private static readonly float[] PowerErrorByDifficulty = { 0.25f, 0.12f, 0.04f };

        // ── Private references ───────────────────────────────────────────────
        private CurlingGameManager _gameManager;
        private ScoreZone _house;
        private bool _isThinking = false;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _gameManager = FindFirstObjectByType<CurlingGameManager>();
            _house = FindFirstObjectByType<ScoreZone>();
        }

        private void Update()
        {
            if (_gameManager.GameOver || _isThinking)
                return;

            // AI acts only on its own team's turn
            if (_gameManager.IsPlayerTurn && _gameManager.CurrentTeam == AITeamIndex)
            {
                StartCoroutine(ThinkAndThrow());
            }
        }

        // ── AI throw sequence ────────────────────────────────────────────────

        private IEnumerator ThinkAndThrow()
        {
            _isThinking = true;

            // Simulate "thinking" delay
            yield return new WaitForSeconds(ThinkTime);

            // Aim from throw origin toward the button, then add error
            Vector3 toButton = _house.transform.position - ThrowOrigin.position;
            toButton.y = 0f;

            float aimError = AimErrorByDifficulty[(int)AIDifficulty];
            float randomAngle = Random.Range(-aimError, aimError);
            Vector3 aimDir = Quaternion.Euler(0f, randomAngle, 0f) * toButton.normalized;

            // Choose a power close to optimal (enough to reach the house)
            float powerError = PowerErrorByDifficulty[(int)AIDifficulty];
            float optimalPower = 0.65f; // tuned for default field length
            float power = Mathf.Clamp01(optimalPower + Random.Range(-powerError, powerError));

            // AI randomly picks spin direction for variety
            float spin = Random.value > 0.5f ? 1f : -1f;

            ThrowAIStone(aimDir, power, spin);

            _isThinking = false;
        }

        private void ThrowAIStone(Vector3 aimDirection, float power, float spinDirection)
        {
            GameObject stoneObj = Instantiate(StonePrefab, ThrowOrigin.position, Quaternion.identity);
            CurlingStone stone = stoneObj.GetComponent<CurlingStone>();
            stone.TeamIndex = AITeamIndex;
            stone.OnStoneStopped += _gameManager.OnStoneStopped;

            // Map normalised power to speed range (match StoneShooter defaults)
            float speed = Mathf.Lerp(5f, 20f, power);
            stone.Throw(aimDirection, speed, spinDirection);

            _gameManager.StoneThrown(stone);
        }
    }
}
