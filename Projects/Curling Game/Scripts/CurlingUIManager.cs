/*
 * Curling Game - CurlingUIManager
 *
 * Drives all HUD elements:
 *   • Scoreboard (running totals per team, per end)
 *   • Power bar (shows throw charge level)
 *   • Turn indicator (whose turn it is, spin direction)
 *   • Game-over panel
 *
 * Uses Unity's built-in UI system (UnityEngine.UI).
 * Wire up references in the Inspector.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Curling
{
    public class CurlingUIManager : MonoBehaviour
    {
        // ── Inspector references ─────────────────────────────────────────────
        [Header("HUD")]
        public Text EndLabel;           // "End 3 / 10"
        public Text TurnLabel;          // "Red's turn"
        public Text SpinLabel;          // "Spin: ← Left"
        public Text ScoreLabel;         // "Red 4  –  Yellow 3"
        public Slider PowerSlider;      // fill = CurrentPower

        [Header("Sweep Indicator")]
        public Image SweepIcon;         // lights up while sweeping

        [Header("Game Over Panel")]
        public GameObject GameOverPanel;
        public Text WinnerLabel;        // "Red wins! 7 – 5"
        public Button RestartButton;

        // ── Private references ───────────────────────────────────────────────
        private CurlingGameManager _gameManager;
        private StoneShooter _shooter;
        private SweepSystem _sweeper;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _gameManager = FindFirstObjectByType<CurlingGameManager>();
            _shooter = FindFirstObjectByType<StoneShooter>();
            _sweeper = FindFirstObjectByType<SweepSystem>();

            // Subscribe to game events for panel updates
            _gameManager.OnGameComplete += ShowGameOverPanel;

            if (GameOverPanel != null)
                GameOverPanel.SetActive(false);

            if (RestartButton != null)
                RestartButton.onClick.AddListener(RestartGame);
        }

        private void Update()
        {
            if (_gameManager.GameOver)
                return;

            UpdateEndLabel();
            UpdateTurnLabel();
            UpdateScoreLabel();
            UpdatePowerBar();
            UpdateSweepIcon();
            UpdateSpinLabel();
        }

        // ── Per-frame UI updates ─────────────────────────────────────────────

        private void UpdateEndLabel()
        {
            if (EndLabel == null) return;
            EndLabel.text = $"End {_gameManager.CurrentEnd} / {_gameManager.TotalEnds}";
        }

        private void UpdateTurnLabel()
        {
            if (TurnLabel == null) return;

            if (_gameManager.IsPlayerTurn)
            {
                string team = _gameManager.TeamName(_gameManager.CurrentTeam);
                int remaining = _gameManager.StonesRemaining();
                TurnLabel.text = $"{team}'s turn  ({remaining} stones left)";
            }
            else
            {
                TurnLabel.text = "Stone in motion…";
            }
        }

        private void UpdateScoreLabel()
        {
            if (ScoreLabel == null) return;
            int[] s = _gameManager.TotalScore;
            ScoreLabel.text = $"{_gameManager.Team0Name}  {s[0]}  –  {s[1]}  {_gameManager.Team1Name}";
        }

        private void UpdatePowerBar()
        {
            if (PowerSlider == null || _shooter == null) return;
            PowerSlider.value = _shooter.CurrentPower;

            // Hide bar when not charging
            PowerSlider.gameObject.SetActive(_shooter.IsCharging);
        }

        private void UpdateSweepIcon()
        {
            if (SweepIcon == null || _sweeper == null) return;
            SweepIcon.color = _sweeper.IsSweeping
                ? Color.cyan
                : new Color(1f, 1f, 1f, 0.3f);
        }

        private void UpdateSpinLabel()
        {
            if (SpinLabel == null || _shooter == null) return;
            string dir = _shooter.SpinDirection > 0 ? "→ Right  (E)" : "← Left  (Q)";
            SpinLabel.text = $"Spin: {dir}";
        }

        // ── Game over ────────────────────────────────────────────────────────

        private void ShowGameOverPanel(int[] finalScores)
        {
            if (GameOverPanel != null)
                GameOverPanel.SetActive(true);

            if (WinnerLabel != null)
            {
                string champion = finalScores[0] > finalScores[1]
                    ? _gameManager.Team0Name
                    : finalScores[1] > finalScores[0]
                        ? _gameManager.Team1Name
                        : "Tie";

                WinnerLabel.text = $"{champion} wins!\n" +
                                   $"{_gameManager.Team0Name} {finalScores[0]}  –  " +
                                   $"{finalScores[1]} {_gameManager.Team1Name}";
            }
        }

        private void RestartGame()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
