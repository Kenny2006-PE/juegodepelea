using System.Collections;
using UnityEngine;

namespace FightingGame
{
    public enum MatchPhase
    {
        Menu = 0,
        RoundIntro = 1,
        Fighting = 2,
        RoundEnd = 3,
        MatchOver = 4
    }

    /// <summary>
    /// Orchestrates the 1v1 fighting match lifecycle:
    /// Spawning selected fighters, wiring Human/AI inputs, Best-of-3 rounds,
    /// countdowns, 99s timer, K.O. & Time Over detection, Rematch, and Return to Menu.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Fighter Prefabs")]
        [SerializeField] private FighterController tanjiroPrefab;
        [SerializeField] private FighterController madaraPrefab;

        [Header("Arena Spawn Points")]
        [SerializeField] private Transform p1SpawnPoint;
        [SerializeField] private Transform p2SpawnPoint;

        [Header("Scene Systems")]
        [SerializeField] private FightingCamera fightingCamera;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private MenuManager menuManager;
        [SerializeField] private CombatFeedback combatFeedback;

        public MatchPhase CurrentPhase { get; private set; } = MatchPhase.Menu;

        private FighterController p1Fighter;
        private FighterController p2Fighter;

        private int currentRound;
        private int p1RoundsWon;
        private int p2RoundsWon;
        private float roundTimeRemaining;
        private bool roundEndedByKO;

        private Coroutine activeMatchRoutine;

        private void Awake()
        {
            if (fightingCamera == null) fightingCamera = FindObjectOfType<FightingCamera>();
            if (uiManager == null) uiManager = FindObjectOfType<UIManager>();
            if (menuManager == null) menuManager = FindObjectOfType<MenuManager>();
            if (combatFeedback == null) combatFeedback = FindObjectOfType<CombatFeedback>();

            if (uiManager != null)
            {
                uiManager.OnRematchClicked += HandleRematch;
                uiManager.OnBackToMenuClicked += ReturnToMenu;
            }
        }

        private void OnDestroy()
        {
            if (uiManager != null)
            {
                uiManager.OnRematchClicked -= HandleRematch;
                uiManager.OnBackToMenuClicked -= ReturnToMenu;
            }
        }

        private void Start()
        {
            ReturnToMenu();
        }

        public void StartNewMatch()
        {
            StopActiveRoutine();
            CleanupSpawnedFighters();

            if (!SpawnConfiguredFighters())
            {
                Debug.LogError("[GameManager] Missing fighter prefab references! Run 'Tools/Setup Fighting Game/Full Setup (All Steps)' first.");
                ReturnToMenu();
                return;
            }

            currentRound = 0;
            p1RoundsWon = 0;
            p2RoundsWon = 0;

            menuManager?.HideMenu();
            uiManager?.SetHUDVisible(true);
            uiManager?.ConfigureMatchHUD(
                MatchSettings.Player1Fighter,
                MatchSettings.Player2Fighter,
                MatchSettings.CurrentMode,
                MatchSettings.CurrentDifficulty);

            activeMatchRoutine = StartCoroutine(MatchLoopCoroutine());
        }

        public void HandleRematch()
        {
            StartNewMatch();
        }

        public void ReturnToMenu()
        {
            StopActiveRoutine();
            CleanupSpawnedFighters();

            CurrentPhase = MatchPhase.Menu;
            uiManager?.SetHUDVisible(false);
            menuManager?.ShowMenu();
        }

        private bool SpawnConfiguredFighters()
        {
            FighterController p1Prefab = GetPrefabFor(MatchSettings.Player1Fighter);
            FighterController p2Prefab = GetPrefabFor(MatchSettings.Player2Fighter);

            if (p1Prefab == null || p2Prefab == null)
            {
                return false;
            }

            Vector3 p1Pos = p1SpawnPoint != null ? p1SpawnPoint.position : new Vector3(-2.4f, 0f, 0f);
            Vector3 p2Pos = p2SpawnPoint != null ? p2SpawnPoint.position : new Vector3(2.4f, 0f, 0f);

            p1Fighter = Instantiate(p1Prefab, p1Pos, Quaternion.Euler(0f, 90f, 0f));
            p1Fighter.name = $"{MatchSettings.GetDisplayName(MatchSettings.Player1Fighter)}_P1";
            p1Fighter.SetFighterId(MatchSettings.Player1Fighter);

            p2Fighter = Instantiate(p2Prefab, p2Pos, Quaternion.Euler(0f, -90f, 0f));
            string p2Suffix = MatchSettings.CurrentMode == GameMode.SinglePlayerVsCPU ? "CPU" : "P2";
            p2Fighter.name = $"{MatchSettings.GetDisplayName(MatchSettings.Player2Fighter)}_{p2Suffix}";
            p2Fighter.SetFighterId(MatchSettings.Player2Fighter);

            // Attach decoupled input components cleanly based on selected mode
            IFighterInput p1Input = AttachHumanInput(p1Fighter.gameObject, PlayerSlot.Player1);
            IFighterInput p2Input = MatchSettings.CurrentMode == GameMode.SinglePlayerVsCPU
                ? AttachAIInput(p2Fighter, MatchSettings.CurrentDifficulty)
                : AttachHumanInput(p2Fighter.gameObject, PlayerSlot.Player2);

            p1Fighter.InitializeMatchReferences(p2Fighter, p1Input, combatFeedback, fightingCamera);
            p2Fighter.InitializeMatchReferences(p1Fighter, p2Input, combatFeedback, fightingCamera);

            p1Fighter.Health.OnHealthChanged += HandleP1HealthChanged;
            p2Fighter.Health.OnHealthChanged += HandleP2HealthChanged;
            p1Fighter.Health.OnKO += HandleFighterKO;
            p2Fighter.Health.OnKO += HandleFighterKO;

            fightingCamera?.SetTargets(p1Fighter.transform, p2Fighter.transform);

            return true;
        }

        private static IFighterInput AttachHumanInput(GameObject fighterObj, PlayerSlot slot)
        {
            RemoveExistingInputs(fighterObj);
            FighterInputHandler handler = fighterObj.AddComponent<FighterInputHandler>();
            handler.Configure(slot);
            return handler;
        }

        private static IFighterInput AttachAIInput(FighterController fighter, AIDifficulty difficulty)
        {
            RemoveExistingInputs(fighter.gameObject);
            FighterAIController ai = fighter.gameObject.AddComponent<FighterAIController>();
            ai.Configure(fighter, difficulty);
            return ai;
        }

        private static void RemoveExistingInputs(GameObject fighterObj)
        {
            FighterInputHandler oldHuman = fighterObj.GetComponent<FighterInputHandler>();
            if (oldHuman != null) Destroy(oldHuman);

            FighterAIController oldAi = fighterObj.GetComponent<FighterAIController>();
            if (oldAi != null) Destroy(oldAi);
        }

        private FighterController GetPrefabFor(FighterId id)
        {
            return id == FighterId.Tanjiro ? tanjiroPrefab : madaraPrefab;
        }

        private IEnumerator MatchLoopCoroutine()
        {
            int roundsToWin = Mathf.Max(1, MatchSettings.RoundsToWin);

            while (p1RoundsWon < roundsToWin && p2RoundsWon < roundsToWin)
            {
                currentRound++;
                yield return StartCoroutine(PlaySingleRoundCoroutine(currentRound));

                if (p1RoundsWon >= roundsToWin || p2RoundsWon >= roundsToWin)
                {
                    break;
                }

                yield return new WaitForSeconds(0.8f);
            }

            ShowMatchWinner();
        }

        private IEnumerator PlaySingleRoundCoroutine(int roundNumber)
        {
            CurrentPhase = MatchPhase.RoundIntro;
            roundEndedByKO = false;
            roundTimeRemaining = MatchSettings.RoundDuration;

            Vector3 p1Pos = p1SpawnPoint != null ? p1SpawnPoint.position : new Vector3(-2.4f, 0f, 0f);
            Vector3 p2Pos = p2SpawnPoint != null ? p2SpawnPoint.position : new Vector3(2.4f, 0f, 0f);

            p1Fighter.ResetForNewRound(p1Pos, faceRight: true);
            p2Fighter.ResetForNewRound(p2Pos, faceRight: false);
            p1Fighter.SetControlLocked(true);
            p2Fighter.SetControlLocked(true);

            uiManager?.ResetHealthBarsImmediate();
            uiManager?.UpdateRoundNumber(roundNumber);
            uiManager?.UpdateTimer(roundTimeRemaining);

            // Countdown: ROUND X -> FIGHT!
            uiManager?.ShowAnnouncement($"ROUND {roundNumber}", new Color(1f, 0.85f, 0.2f, 1f));
            yield return new WaitForSeconds(1.25f);

            uiManager?.ShowAnnouncement("¡PELEAR!", new Color(1f, 0.30f, 0.18f, 1f));
            combatFeedback?.PlayRoundBell();
            yield return new WaitForSeconds(0.65f);

            uiManager?.HideAnnouncement();

            // Enable controls & AI
            CurrentPhase = MatchPhase.Fighting;
            p1Fighter.SetControlLocked(false);
            p2Fighter.SetControlLocked(false);

            // Active fight loop
            while (CurrentPhase == MatchPhase.Fighting)
            {
                if (roundEndedByKO || p1Fighter.Health.IsDead || p2Fighter.Health.IsDead)
                {
                    break;
                }

                roundTimeRemaining -= Time.deltaTime;
                uiManager?.UpdateTimer(roundTimeRemaining);

                if (roundTimeRemaining <= 0f)
                {
                    roundTimeRemaining = 0f;
                    uiManager?.UpdateTimer(0f);
                    break;
                }

                yield return null;
            }

            // Round finished
            CurrentPhase = MatchPhase.RoundEnd;
            p1Fighter.SetControlLocked(true);
            p2Fighter.SetControlLocked(true);

            bool wasKO = roundEndedByKO || p1Fighter.Health.IsDead || p2Fighter.Health.IsDead;
            if (wasKO)
            {
                uiManager?.ShowAnnouncement("K.O.!", new Color(1f, 0.20f, 0.15f, 1f));
            }
            else
            {
                uiManager?.ShowAnnouncement("TIME OVER", new Color(1f, 0.78f, 0.18f, 1f));
            }

            yield return new WaitForSeconds(1.8f);

            // Evaluate winner (higher HP% if Time Over, or surviving fighter on KO)
            float p1Pct = p1Fighter.Health.HealthNormalized;
            float p2Pct = p2Fighter.Health.HealthNormalized;

            if (p1Pct > p2Pct)
            {
                p1RoundsWon++;
                uiManager?.ShowAnnouncement(
                    $"{MatchSettings.GetDisplayName(MatchSettings.Player1Fighter)} (P1) GANA EL ROUND",
                    MatchSettings.GetAccentColor(MatchSettings.Player1Fighter));
            }
            else if (p2Pct > p1Pct)
            {
                p2RoundsWon++;
                string p2Tag = MatchSettings.CurrentMode == GameMode.SinglePlayerVsCPU ? "CPU" : "P2";
                uiManager?.ShowAnnouncement(
                    $"{MatchSettings.GetDisplayName(MatchSettings.Player2Fighter)} ({p2Tag}) GANA EL ROUND",
                    MatchSettings.GetAccentColor(MatchSettings.Player2Fighter));
            }
            else
            {
                uiManager?.ShowAnnouncement("EMPATE", Color.white);
            }

            uiManager?.UpdateRoundIndicators(p1RoundsWon, p2RoundsWon);
            yield return new WaitForSeconds(1.6f);
            uiManager?.HideAnnouncement();
        }

        private void ShowMatchWinner()
        {
            CurrentPhase = MatchPhase.MatchOver;
            p1Fighter?.SetControlLocked(true);
            p2Fighter?.SetControlLocked(true);

            bool p1Won = p1RoundsWon >= p2RoundsWon;
            FighterId winnerId = p1Won ? MatchSettings.Player1Fighter : MatchSettings.Player2Fighter;
            string winnerRole = p1Won
                ? "JUGADOR 1"
                : (MatchSettings.CurrentMode == GameMode.SinglePlayerVsCPU ? "CPU" : "JUGADOR 2");

            string title = $"¡{MatchSettings.GetDisplayName(winnerId)} GANA!";
            string subtitle = $"VICTORIA PARA {winnerRole}  ({p1RoundsWon} - {p2RoundsWon})";

            uiManager?.ShowVictoryPanel(title, subtitle, MatchSettings.GetAccentColor(winnerId));
        }

        private void HandleP1HealthChanged(float current, float max)
        {
            uiManager?.SetPlayer1Health(current, max);
        }

        private void HandleP2HealthChanged(float current, float max)
        {
            uiManager?.SetPlayer2Health(current, max);
        }

        private void HandleFighterKO()
        {
            if (CurrentPhase == MatchPhase.Fighting)
            {
                roundEndedByKO = true;
            }
        }

        private void StopActiveRoutine()
        {
            if (activeMatchRoutine != null)
            {
                StopCoroutine(activeMatchRoutine);
                activeMatchRoutine = null;
            }
        }

        private void CleanupSpawnedFighters()
        {
            if (p1Fighter != null)
            {
                if (p1Fighter.Health != null)
                {
                    p1Fighter.Health.OnHealthChanged -= HandleP1HealthChanged;
                    p1Fighter.Health.OnKO -= HandleFighterKO;
                }
                Destroy(p1Fighter.gameObject);
                p1Fighter = null;
            }

            if (p2Fighter != null)
            {
                if (p2Fighter.Health != null)
                {
                    p2Fighter.Health.OnHealthChanged -= HandleP2HealthChanged;
                    p2Fighter.Health.OnKO -= HandleFighterKO;
                }
                Destroy(p2Fighter.gameObject);
                p2Fighter = null;
            }
        }
    }
}
