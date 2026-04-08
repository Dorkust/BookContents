/*
 * Curling Game - ScoreZone
 *
 * Represents the "house" — the concentric rings at the end of the ice sheet.
 * Calculates which team's stones are closest to the centre (the "button")
 * and how many score.
 *
 * In curling only ONE team scores per end: the team with the stone closest
 * to the button. They score one point for every stone that is closer to the
 * button than the nearest opponent stone.
 *
 * Attach to an empty GameObject at the centre of the house.
 */
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Curling
{
    public class ScoreZone : MonoBehaviour
    {
        // ── Inspector fields ────────────────────────────────────────────────
        [Header("House Dimensions")]
        [Tooltip("Radius of the outermost scoring ring (12-foot ring)")]
        public float HouseRadius = 1.83f; // metres — regulation 6-foot radius

        // ── Scoring ──────────────────────────────────────────────────────────

        /// <summary>
        /// Calculate scores for this end.
        /// Returns an array where index = team index and value = points scored.
        /// </summary>
        public int[] CalculateEndScore(List<CurlingStone> stonesInPlay)
        {
            int[] scores = new int[2]; // two teams

            // Only count stones inside the house
            List<CurlingStone> inHouse = stonesInPlay
                .Where(s => DistanceToButton(s) <= HouseRadius)
                .OrderBy(s => DistanceToButton(s))
                .ToList();

            if (inHouse.Count == 0)
                return scores; // blank end — no score

            /*   The stone closest to the button determines the scoring team. */
            int scoringTeam = inHouse[0].TeamIndex;

            /*   Count consecutive scoring-team stones before the first
             *   opponent stone appears.                                    */
            foreach (CurlingStone stone in inHouse)
            {
                if (stone.TeamIndex == scoringTeam)
                    scores[scoringTeam]++;
                else
                    break; // opponent stone closer than remaining — stop counting
            }

            return scores;
        }

        /// <summary>Distance from a stone to the centre of the house.</summary>
        public float DistanceToButton(CurlingStone stone)
        {
            Vector3 buttonPos = new Vector3(transform.position.x, 0f, transform.position.z);
            Vector3 stonePos = new Vector3(stone.transform.position.x, 0f, stone.transform.position.z);
            return Vector3.Distance(buttonPos, stonePos);
        }

        /// <summary>True if the stone is within the house circle.</summary>
        public bool IsInHouse(CurlingStone stone)
        {
            return DistanceToButton(stone) <= HouseRadius;
        }

        // ── Debug visual ─────────────────────────────────────────────────────
        private void OnDrawGizmos()
        {
            /*   Draw the house rings in the editor so designers can see
             *   the scoring area without running the game.               */
            DrawRing(HouseRadius, Color.red);
            DrawRing(HouseRadius * 0.667f, Color.white); // 8-foot ring
            DrawRing(HouseRadius * 0.333f, Color.blue);  // 4-foot ring
            DrawRing(0.15f, Color.white);                // button
        }

        private void DrawRing(float radius, Color color)
        {
            Gizmos.color = color;
            int segments = 40;
            float angleStep = 360f / segments;
            Vector3 prev = transform.position + new Vector3(radius, 0f, 0f);
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 next = transform.position + new Vector3(
                    Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }
    }
}
