/*
 * Curling Game - CurlingUIManager  (iOS touch edition)
 *
 * Drives all HUD elements:
 *   • Scoreboard (running totals per team, per end)
 *   • Power bar (shows throw charge level)
 *   • Turn indicator (whose turn it is, spin direction)
 *   • Spin toggle buttons (replaces Q/E keyboard shortcuts on iOS)
 *   • Sweep indicator
 *   • Game-over panel with restart button
 *
 * Uses Unity's built-in UI system (UnityEngine.UI).
 * Wire up all references in the Inspector.
 */
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Curling
{
    public class CurlingUIManager : MonoBehaviour
    {
        // ── Inspector references ─────────────────────────────────────────────
        [Header("HUD Labels")]
        public Text EndLabel;           // "End 3 / 10"
        public Text TurnLabel;          // "Red's turn  (6 stones left)"
        public Text SpinLabel;          // "Spin: → Right"
        public Text ScoreLabel;         // "Red 4  –  Yellow 3"

        [Header("Power Bar")]
        public Slider PowerSlider;      // fill = CurrentPower (0..1)

        [Header("Spin Toggle Buttons")]
        [Tooltip("On-screen button that sets spin to left curl (replaces Q key)")]
        public Button SpinLeftButton;
        [Tooltip("On-screen button that sets spin to right curl (replaces E key)")]
        public Button SpinRightButton;

        [Header("Sweep Indicator")]
        public Image SweepIcon;         // tinted cyan while player is sweeping

        [Header("Game Over Panel")]
        public GameObject GameOverPanel;
        public Text WinnerLabel;        // "Red wins!  7 – 5"
        public Button RestartButton;

        // ── Private references ───────────────────────────────────────────────
        private CurlingGameManager _gameManager;
        private StoneShooter _shooter;
        private SweepSystem _sweeper;

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _gameManager = FindFirstObjectByType<CurlingGameManager>();
            _shooter     = FindFirstObjectByType<StoneShooter>();
            _sweeper     = FindFirstObjectByType<SweepSystem>();

            _gameManager.OnGameComplete += ShowGameOverPanel;

            if (GameOverPanel != null)
                GameOverPanel.SetActive(false);

            // Restart button
            if (RestartButton != null)
                RestartButton.onClick.AddListener(RestartGame);

            // Spin buttons — wire directly to StoneShooter methods
            if (SpinLeftButton != null)
                SpinLeftButton.onClick.AddListener(_shooter.SetSpinLeft);

            if (SpinRightButton != null)
                SpinRightButton.onClick.AddListener(_shooter.SetSpinRight);
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
            UpdateSpinButtonHighlights();
        }

        // ── Per-frame HUD updates ────────────────────────────────────────────

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
            string dir = _shooter.SpinDirection > 0 ? "→ Right" : "← Left";
            SpinLabel.text = $"Spin: {dir}";
        }

        private void UpdateSpinButtonHighlights()
        {
            /*   Highlight the active spin button so the player can
             *   see the current selection at a glance on a small screen. */
            if (_shooter == null) return;

            bool rightActive = _shooter.SpinDirection > 0;

            if (SpinRightButton != null)
            {
                ColorBlock cb = SpinRightButton.colors;
                cb.normalColor = rightActive ? Color.cyan : Color.white;
                SpinRightButton.colors = cb;
            }

            if (SpinLeftButton != null)
            {
                ColorBlock cb = SpinLeftButton.colors;
                cb.normalColor = rightActive ? Color.white : Color.cyan;
                SpinLeftButton.colors = cb;
            }
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
