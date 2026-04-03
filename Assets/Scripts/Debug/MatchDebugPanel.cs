using UnityEngine;
using Resonance.Match;
using Resonance.Assemblies.SharedGameLogic;

namespace Resonance.DebugTools
{
    public class MatchDebugPanel : MonoBehaviour
    {
        #region Public Methods
        public void DrawPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== MATCH DEBUG ===");
            GUILayout.Space(10);

            if (MatchLogicNetworkAdapter.Instance == null)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("MatchLogicNetworkAdapter not found!");
                GUI.color = Color.white;
                GUILayout.EndVertical();
                return;
            }

            DrawMatchStateSection();
            GUILayout.Space(10);
            DrawArenaSection();
            GUILayout.Space(10);
            DrawStatsSection();

            GUILayout.EndVertical();
        }
        #endregion

        #region Draw Methods
        private void DrawMatchStateSection()
        {
            GUILayout.BeginVertical("box");

            var roundManager = MatchLogicNetworkAdapter.Instance.ActiveRoundManager;
            if (roundManager == null)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("No active round manager.");
                GUI.color = Color.white;
                GUILayout.EndVertical();
                return;
            }

            Color stateColor = roundManager.MatchState switch
            {
                BaseMatchState.MatchActive => Color.green,
                BaseMatchState.Countdown => Color.yellow,
                BaseMatchState.MatchEnded => Color.red,
                _ => Color.white
            };

            GUI.color = stateColor;
            GUILayout.Label($"Match State: {roundManager.MatchState}");
            GUI.color = Color.white;

            GUILayout.EndVertical();
        }

        private void DrawArenaSection()
        {
            if (ArenaRoundManagerBridge.Instance == null)
            {
                return;
            }

            GUILayout.BeginVertical("box");
            GUILayout.Label("Arena:");
            GUILayout.Space(5);

            if (GUILayout.Button("End Match (no winner)", GUILayout.Height(35)))
            {
                EndMatch();
            }

            GUILayout.EndVertical();
        }

        private void DrawStatsSection()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("Stats:");
            GUILayout.Space(5);

            if (MatchStatBridge.Instance == null)
            {
                GUI.color = Color.yellow;
                GUILayout.Label("MatchStatBridge not found!");
                GUI.color = Color.white;
                GUILayout.EndVertical();
                return;
            }

            if (GUILayout.Button("Reset All Stats", GUILayout.Height(35)))
            {
                ResetAllStats();
            }

            GUILayout.EndVertical();
        }
        #endregion

        #region Actions
        private void EndMatch()
        {
            if (ArenaRoundManagerBridge.Instance == null)
            {
                return;
            }

            Debug.Log("[MatchDebugPanel] Ending match with no winner via debug tools");
            ArenaRoundManagerBridge.Instance.EndMatch(null);

            // bypass cursor lock
            DebugMenuManager.Instance._showMenu = false;
        }

        private void ResetAllStats()
        {
            if (MatchStatBridge.Instance == null)
            {
                return;
            }

            Debug.Log("[MatchDebugPanel] Resetting all stats via debug tools");
            MatchStatBridge.Instance.ResetAllStats();
        }
        #endregion
    }
}
