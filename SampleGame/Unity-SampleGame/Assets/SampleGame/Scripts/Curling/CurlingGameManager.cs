/*
 * Curling Game - CurlingGameManager
 *
 * Central game-state machine. Manages:
 *   • Ends (rounds) and turns within each end
 *   • Alternating throws between two teams
 *   • Calling score calculation after all stones are thrown
 *   • Tracking cumulative scores across ends
 *
 * Regulation curling: 2 teams, 8 stones each per end, up to 10 ends.
 * This implementation uses configurable counts for flexibility.
 *
 * Attach to an empty "GameManager" GameObject in the scene.
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Curling
{
    public class CurlingGameManager : MonoBehaviour
    {
        // ── Inspector fields ────────────────────────────────────────────────
        [Header("Game Rules")]
        public int TotalEnds = 10;
        public int StonesPerTeamPerEnd = 8;

        [Header("Team Names")]
        public string Team0Name = "Red";
        public string Team1Name = "Yellow";

        [Header("References")]
        public ScoreZone HouseScoreZone;
        public StoneShooter PlayerShooter;
        public SweepSystem Sweeper;

        // ── State ────────────────────────────────────────────────────────────
        public int CurrentEnd { get; private set; } = 1;
        public int CurrentTeam { get; private set; } = 0;   // who throws next
        public bool IsPlayerTurn { get; private set; } = true;
        public bool GameOver { get; private set; } = false;

        // Scores[end][team] — cumulative running totals per team
        public int[] TotalScore { get; private set; } = new int[2];

        // Stones in play this end
        private List<CurlingStone> _stonesInPlay = new List<CurlingStone>();
        private int _throwsThisEnd = 0;
        private int _totalThrowsPerEnd; // StonesPerTeamPerEnd * 2

        // ── Events (subscribe in UI scripts) ────────────────────────────────
        public System.Action<int[]> OnEndComplete;  // passes end scores
        public System.Action<int[]> OnGameComplete; // passes final scores

        // ───────────────────────────────────────────────────────────────────
        private void Start()
        {
            _totalThrowsPerEnd = StonesPerTeamPerEnd * 2;
            IsPlayerTurn = true;
            Debug.Log($"Curling game started! {TotalEnds} ends, {StonesPerTeamPerEnd} stones each.");
        }

        // ── Called by StoneShooter after a throw ─────────────────────────────

        /// <summary>
        /// Register a thrown stone and advance the turn.
        /// </summary>
        public void StoneThrown(CurlingStone stone)
        {
            _stonesInPlay.Add(stone);
            _throwsThisEnd++;

            // Let the sweep system track this stone
            if (Sweeper != null)
                Sweeper.TrackStone(stone);

            IsPlayerTurn = false; // wait until the stone stops
        }

        // ── Called by CurlingStone.OnStoneStopped ───────────────────────────

        public void OnStoneStopped(CurlingStone stone)
        {
            // Are all throws for this end complete?
            if (_throwsThisEnd >= _totalThrowsPerEnd)
            {
                StartCoroutine(FinishEnd());
                return;
            }

            // Alternate teams
            CurrentTeam = 1 - CurrentTeam;
            IsPlayerTurn = true;

            Debug.Log($"End {CurrentEnd} | Throw {_throwsThisEnd}/{_totalThrowsPerEnd} | {TeamName(CurrentTeam)}'s turn");
        }

        // ── End / Game resolution ────────────────────────────────────────────

        private IEnumerator FinishEnd()
        {
            // Brief pause so the last stone is seen settling
            yield return new WaitForSeconds(1.5f);

            int[] endScores = HouseScoreZone.CalculateEndScore(_stonesInPlay);

            // Add to running totals
            for (int t = 0; t < 2; t++)
                TotalScore[t] += endScores[t];

            string winner = endScores[0] > endScores[1]
                ? Team0Name
                : endScores[1] > endScores[0] ? Team1Name : "Nobody";

            Debug.Log($"End {CurrentEnd} over! {winner} scores. " +
                      $"Total: {Team0Name} {TotalScore[0]} – {Team1Name} {TotalScore[1]}");

            OnEndComplete?.Invoke(endScores);

            if (CurrentEnd >= TotalEnds)
            {
                EndGame();
                yield break;
            }

            StartNextEnd(endScores);
        }

        private void StartNextEnd(int[] lastEndScores)
        {
            CurrentEnd++;
            _throwsThisEnd = 0;
            _stonesInPlay.Clear();

            /*   In curling, the team that was scored upon (the losing team
             *   of that end) throws first in the next end (they have "hammer"
             *   advantage).  If it was a blank end, the same team keeps hammer. */
            if (lastEndScores[0] > 0)
                CurrentTeam = 1; // team 1 (Yellow) lost → throws first
            else if (lastEndScores[1] > 0)
                CurrentTeam = 0; // team 0 (Red) lost → throws first

            IsPlayerTurn = true;
            Debug.Log($"--- Starting End {CurrentEnd} | {TeamName(CurrentTeam)} throws first ---");
        }

        private void EndGame()
        {
            GameOver = true;
            IsPlayerTurn = false;

            string champion = TotalScore[0] > TotalScore[1] ? Team0Name
                            : TotalScore[1] > TotalScore[0] ? Team1Name
                            : "Nobody — it's a tie!";

            Debug.Log($"Game over! Winner: {champion} | " +
                      $"{Team0Name} {TotalScore[0]} – {Team1Name} {TotalScore[1]}");

            OnGameComplete?.Invoke(TotalScore);
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        public string TeamName(int teamIndex)
        {
            return teamIndex == 0 ? Team0Name : Team1Name;
        }

        /// <summary>
        /// Returns how many stones the current team still has to throw this end.
        /// </summary>
        public int StonesRemaining()
        {
            int thrown = _throwsThisEnd / 2 + (_throwsThisEnd % 2 == 0 ? 0 : 1);
            return StonesPerTeamPerEnd - thrown;
        }
    }
}
