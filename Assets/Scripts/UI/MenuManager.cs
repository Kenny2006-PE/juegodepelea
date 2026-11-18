using TMPro;
using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Manages the styled pre-match anime fighter selection screen:
    /// - Mode Selection: 1P vs CPU or 1P vs 2P (Local)
    /// - Fighter Card Selection with glowing borders for P1 and P2/CPU
    /// - AI Difficulty Selector (Easy / Normal / Hard) when 1P vs CPU is active
    /// - Dynamic Matchup Preview & Fight CTA
    /// </summary>
    public class MenuManager : MonoBehaviour
    {
        [Header("Root & Game Reference")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameManager gameManager;

        [Header("Mode Selection")]
        [SerializeField] private UnityEngine.UI.Button modeVsCpuButton;
        [SerializeField] private UnityEngine.UI.Image modeVsCpuBorder;
        [SerializeField] private UnityEngine.UI.Button modeLocal2PButton;
        [SerializeField] private UnityEngine.UI.Image modeLocal2PBorder;

        [Header("Player 1 Fighter Cards")]
        [SerializeField] private UnityEngine.UI.Button p1TanjiroButton;
        [SerializeField] private UnityEngine.UI.Image p1TanjiroBorder;
        [SerializeField] private UnityEngine.UI.Button p1MadaraButton;
        [SerializeField] private UnityEngine.UI.Image p1MadaraBorder;

        [Header("Player 2 / CPU Fighter Cards")]
        [SerializeField] private TextMeshProUGUI p2SectionTitleText;
        [SerializeField] private TextMeshProUGUI p2RoleBadgeText;
        [SerializeField] private UnityEngine.UI.Button p2TanjiroButton;
        [SerializeField] private UnityEngine.UI.Image p2TanjiroBorder;
        [SerializeField] private UnityEngine.UI.Button p2MadaraButton;
        [SerializeField] private UnityEngine.UI.Image p2MadaraBorder;

        [Header("AI Difficulty Selection")]
        [SerializeField] private GameObject difficultySection;
        [SerializeField] private UnityEngine.UI.Button diffEasyButton;
        [SerializeField] private UnityEngine.UI.Image diffEasyBorder;
        [SerializeField] private UnityEngine.UI.Button diffNormalButton;
        [SerializeField] private UnityEngine.UI.Image diffNormalBorder;
        [SerializeField] private UnityEngine.UI.Button diffHardButton;
        [SerializeField] private UnityEngine.UI.Image diffHardBorder;

        [Header("Summary & Action")]
        [SerializeField] private TextMeshProUGUI matchupP1Text;
        [SerializeField] private TextMeshProUGUI matchupP2Text;
        [SerializeField] private TextMeshProUGUI matchupSummaryText;
        [SerializeField] private TextMeshProUGUI controlsHintText;
        [SerializeField] private UnityEngine.UI.Button fightButton;

        [Header("Theme Palette")]
        [SerializeField] private Color goldAccent = new Color(0.98f, 0.74f, 0.22f, 1f);
        [SerializeField] private Color cardIdleBg = new Color(0.11f, 0.13f, 0.19f, 0.96f);
        [SerializeField] private Color borderIdleColor = new Color(0.24f, 0.28f, 0.38f, 0.55f);

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = FindObjectOfType<GameManager>();
            }

            WireButtons();
        }

        private void Start()
        {
            RefreshUI();
        }

        private void WireButtons()
        {
            if (modeVsCpuButton != null)
                modeVsCpuButton.onClick.AddListener(() => SelectMode(GameMode.SinglePlayerVsCPU));

            if (modeLocal2PButton != null)
                modeLocal2PButton.onClick.AddListener(() => SelectMode(GameMode.LocalTwoPlayer));

            if (p1TanjiroButton != null)
                p1TanjiroButton.onClick.AddListener(() => SelectPlayer1(FighterId.Tanjiro));

            if (p1MadaraButton != null)
                p1MadaraButton.onClick.AddListener(() => SelectPlayer1(FighterId.Madara));

            if (p2TanjiroButton != null)
                p2TanjiroButton.onClick.AddListener(() => SelectPlayer2(FighterId.Tanjiro));

            if (p2MadaraButton != null)
                p2MadaraButton.onClick.AddListener(() => SelectPlayer2(FighterId.Madara));

            if (diffEasyButton != null)
                diffEasyButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Easy));

            if (diffNormalButton != null)
                diffNormalButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Normal));

            if (diffHardButton != null)
                diffHardButton.onClick.AddListener(() => SelectDifficulty(AIDifficulty.Hard));

            if (fightButton != null)
                fightButton.onClick.AddListener(OnFightClicked);
        }

        public void ShowMenu()
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(true);
            }
            RefreshUI();
        }

        public void HideMenu()
        {
            if (menuRoot != null)
            {
                menuRoot.SetActive(false);
            }
        }

        public void SelectMode(GameMode mode)
        {
            MatchSettings.CurrentMode = mode;
            RefreshUI();
        }

        public void SelectPlayer1(FighterId fighter)
        {
            MatchSettings.Player1Fighter = fighter;
            MatchSettings.Player2Fighter = MatchSettings.GetOppositeFighter(fighter);
            RefreshUI();
        }

        public void SelectPlayer2(FighterId fighter)
        {
            MatchSettings.Player2Fighter = fighter;
            RefreshUI();
        }

        public void SelectDifficulty(AIDifficulty difficulty)
        {
            MatchSettings.CurrentDifficulty = difficulty;
            RefreshUI();
        }

        private void OnFightClicked()
        {
            HideMenu();
            if (gameManager != null)
            {
                gameManager.StartNewMatch();
            }
        }

        private void RefreshUI()
        {
            bool isCpu = MatchSettings.CurrentMode == GameMode.SinglePlayerVsCPU;

            // Mode Tabs
            ApplyTabVisual(modeVsCpuButton, modeVsCpuBorder, isCpu, goldAccent);
            ApplyTabVisual(modeLocal2PButton, modeLocal2PBorder, !isCpu, goldAccent);

            // Fighter Cards
            Color tanjiroColor = MatchSettings.GetAccentColor(FighterId.Tanjiro);
            Color madaraColor = MatchSettings.GetAccentColor(FighterId.Madara);

            ApplyCardVisual(p1TanjiroButton, p1TanjiroBorder, MatchSettings.Player1Fighter == FighterId.Tanjiro, tanjiroColor);
            ApplyCardVisual(p1MadaraButton, p1MadaraBorder, MatchSettings.Player1Fighter == FighterId.Madara, madaraColor);

            ApplyCardVisual(p2TanjiroButton, p2TanjiroBorder, MatchSettings.Player2Fighter == FighterId.Tanjiro, tanjiroColor);
            ApplyCardVisual(p2MadaraButton, p2MadaraBorder, MatchSettings.Player2Fighter == FighterId.Madara, madaraColor);

            if (p2SectionTitleText != null)
            {
                p2SectionTitleText.text = isCpu ? "OPONENTE CPU" : "JUGADOR 2";
            }

            if (p2RoleBadgeText != null)
            {
                p2RoleBadgeText.text = isCpu ? "CPU" : "P2";
            }

            if (difficultySection != null)
            {
                difficultySection.SetActive(isCpu);
            }

            ApplyTabVisual(diffEasyButton, diffEasyBorder, MatchSettings.CurrentDifficulty == AIDifficulty.Easy, new Color(0.16f, 0.78f, 0.45f, 1f));
            ApplyTabVisual(diffNormalButton, diffNormalBorder, MatchSettings.CurrentDifficulty == AIDifficulty.Normal, goldAccent);
            ApplyTabVisual(diffHardButton, diffHardBorder, MatchSettings.CurrentDifficulty == AIDifficulty.Hard, new Color(0.90f, 0.22f, 0.24f, 1f));

            string p1Name = MatchSettings.GetDisplayName(MatchSettings.Player1Fighter);
            string p2Name = MatchSettings.GetDisplayName(MatchSettings.Player2Fighter);
            string rightRole = isCpu
                ? $"CPU · {MatchSettings.GetDifficultyLabel(MatchSettings.CurrentDifficulty)}"
                : "P2 LOCAL";

            if (matchupP1Text != null)
            {
                matchupP1Text.text = $"{p1Name}  <size=72%><color=#9AA6C0>[P1]</color></size>";
                matchupP1Text.color = MatchSettings.GetAccentColor(MatchSettings.Player1Fighter);
            }

            if (matchupP2Text != null)
            {
                matchupP2Text.text = $"<size=72%><color=#9AA6C0>[{rightRole}]</color></size>  {p2Name}";
                matchupP2Text.color = MatchSettings.GetAccentColor(MatchSettings.Player2Fighter);
            }

            if (matchupSummaryText != null)
            {
                matchupSummaryText.text = $"{p1Name} (P1)   VS   {p2Name} ({rightRole})";
            }

            if (controlsHintText != null)
            {
                controlsHintText.text = isCpu
                    ? "CONTROLES P1:   [A / D] Mover   •   [S] Bloqueo   •   [F] Jab Cross   •   [G] Patada Fuerte (Derribo)"
                    : "P1: [A/D] Mover · [S] Bloqueo · [F] Jab · [G] Patada     |     P2: [←/→] Mover · [↓] Bloqueo · [J/1] Jab · [K/2] Patada";
            }
        }

        private void ApplyTabVisual(UnityEngine.UI.Button button, UnityEngine.UI.Image border, bool selected, Color accent)
        {
            if (button != null)
            {
                UnityEngine.UI.Image bg = button.GetComponent<UnityEngine.UI.Image>();
                if (bg != null)
                {
                    bg.color = selected
                        ? Color.Lerp(cardIdleBg, accent, 0.32f)
                        : cardIdleBg;
                }
            }

            if (border != null)
            {
                border.color = selected ? accent : borderIdleColor;
            }
        }

        private void ApplyCardVisual(UnityEngine.UI.Button button, UnityEngine.UI.Image border, bool selected, Color accent)
        {
            if (button != null)
            {
                UnityEngine.UI.Image bg = button.GetComponent<UnityEngine.UI.Image>();
                if (bg != null)
                {
                    bg.color = selected
                        ? Color.Lerp(cardIdleBg, accent, 0.28f)
                        : cardIdleBg;
                }
            }

            if (border != null)
            {
                border.color = selected ? accent : borderIdleColor;
            }
        }
    }
}
