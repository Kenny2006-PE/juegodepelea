using System;
using TMPro;
using UnityEngine;

namespace FightingGame
{
    /// <summary>
    /// Controls the arcade Combat HUD:
    /// - Glossy health bars with delayed damage trail & low-HP danger pulse
    /// - Dynamic character nameplates, role badges, and numeric HP readouts
    /// - Center medallion 99s timer & Best-of-3 diamond round gems
    /// - Animated punch-in center announcements (ROUND / FIGHT / K.O.)
    /// - Animated Victory modal overlay (Rematch / Back to Menu)
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Root Panels")]
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private RectTransform victoryCardRect;

        [Header("Player 1 HUD")]
        [SerializeField] private TextMeshProUGUI p1NameText;
        [SerializeField] private TextMeshProUGUI p1RoleBadgeText;
        [SerializeField] private UnityEngine.UI.Image p1RoleBadgeBg;
        [SerializeField] private TextMeshProUGUI p1HealthValueText;
        [SerializeField] private UnityEngine.UI.Image p1HealthFill;
        [SerializeField] private UnityEngine.UI.Image p1DamageTrailFill;
        [SerializeField] private UnityEngine.UI.Image p1BarBorder;
        [SerializeField] private UnityEngine.UI.Image[] p1RoundPips;

        [Header("Player 2 / CPU HUD")]
        [SerializeField] private TextMeshProUGUI p2NameText;
        [SerializeField] private TextMeshProUGUI p2RoleBadgeText;
        [SerializeField] private UnityEngine.UI.Image p2RoleBadgeBg;
        [SerializeField] private TextMeshProUGUI p2HealthValueText;
        [SerializeField] private UnityEngine.UI.Image p2HealthFill;
        [SerializeField] private UnityEngine.UI.Image p2DamageTrailFill;
        [SerializeField] private UnityEngine.UI.Image p2BarBorder;
        [SerializeField] private UnityEngine.UI.Image[] p2RoundPips;

        [Header("Center Timer & Announcements")]
        [SerializeField] private TextMeshProUGUI timerText;
        [SerializeField] private TextMeshProUGUI roundInfoText;
        [SerializeField] private GameObject announcementContainer;
        [SerializeField] private CanvasGroup announcementCanvasGroup;
        [SerializeField] private RectTransform announcementTextRect;
        [SerializeField] private TextMeshProUGUI announcementText;
        [SerializeField] private TextMeshProUGUI hudControlsStripText;

        [Header("Victory Overlay")]
        [SerializeField] private UnityEngine.UI.Image victoryTopAccent;
        [SerializeField] private TextMeshProUGUI victoryTitleText;
        [SerializeField] private TextMeshProUGUI victorySubtitleText;
        [SerializeField] private UnityEngine.UI.Button rematchButton;
        [SerializeField] private UnityEngine.UI.Button backToMenuButton;

        [Header("Visual Tuning")]
        [SerializeField] private float healthLerpSpeed = 12f;
        [SerializeField] private float trailLerpSpeed = 2.2f;
        [SerializeField] private Color roundWonColor = new Color(1f, 0.80f, 0.16f, 1f);
        [SerializeField] private Color roundEmptyColor = new Color(0.18f, 0.20f, 0.28f, 0.90f);
        [SerializeField] private Color lowHealthAlertColor = new Color(1f, 0.22f, 0.18f, 1f);

        private Color p1BaseBarColor = Color.white;
        private Color p2BaseBarColor = Color.white;

        private float p1TargetFill = 1f;
        private float p2TargetFill = 1f;
        private float p1CurrentFill = 1f;
        private float p2CurrentFill = 1f;
        private float p1TrailFill = 1f;
        private float p2TrailFill = 1f;

        private float announcementAnimTimer;
        private float victoryAnimTimer;
        private bool animatingAnnouncement;
        private bool animatingVictory;

        public event Action OnRematchClicked;
        public event Action OnBackToMenuClicked;

        private void Awake()
        {
            if (rematchButton != null)
            {
                rematchButton.onClick.AddListener(() => OnRematchClicked?.Invoke());
            }

            if (backToMenuButton != null)
            {
                backToMenuButton.onClick.AddListener(() => OnBackToMenuClicked?.Invoke());
            }
        }

        public void SetHUDVisible(bool visible)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(visible);
            }

            if (!visible)
            {
                HideVictoryPanel();
                HideAnnouncement();
            }
        }

        public void ConfigureMatchHUD(FighterId p1Id, FighterId p2Id, GameMode mode, AIDifficulty difficulty)
        {
            p1BaseBarColor = MatchSettings.GetAccentColor(p1Id);
            p2BaseBarColor = MatchSettings.GetAccentColor(p2Id);

            if (p1NameText != null)
            {
                p1NameText.text = MatchSettings.GetDisplayName(p1Id);
                p1NameText.color = Color.white;
            }

            if (p1RoleBadgeText != null)
            {
                p1RoleBadgeText.text = "P1";
            }

            if (p1RoleBadgeBg != null)
            {
                p1RoleBadgeBg.color = p1BaseBarColor;
            }

            if (p1BarBorder != null)
            {
                p1BarBorder.color = Color.Lerp(p1BaseBarColor, Color.white, 0.25f);
            }

            string p2Role = mode == GameMode.SinglePlayerVsCPU
                ? $"CPU · {MatchSettings.GetDifficultyLabel(difficulty)}"
                : "P2 LOCAL";

            if (p2NameText != null)
            {
                p2NameText.text = MatchSettings.GetDisplayName(p2Id);
                p2NameText.color = Color.white;
            }

            if (p2RoleBadgeText != null)
            {
                p2RoleBadgeText.text = p2Role;
            }

            if (p2RoleBadgeBg != null)
            {
                p2RoleBadgeBg.color = p2BaseBarColor;
            }

            if (p2BarBorder != null)
            {
                p2BarBorder.color = Color.Lerp(p2BaseBarColor, Color.white, 0.25f);
            }

            if (hudControlsStripText != null)
            {
                hudControlsStripText.text = mode == GameMode.SinglePlayerVsCPU
                    ? "P1:   [A / D] Mover   •   [S] Bloqueo   •   [F] Jab Cross   •   [G] Patada Fuerte (Derribo)"
                    : "P1: [A/D] Mover · [S] Bloqueo · [F] Jab · [G] Patada     |     P2: [←/→] Mover · [↓] Bloqueo · [J/1] Jab · [K/2] Patada";
            }

            ResetHealthBarsImmediate();
            UpdateRoundIndicators(0, 0);
            UpdateTimer(MatchSettings.RoundDuration);
            HideVictoryPanel();
            HideAnnouncement();
        }

        public void ResetHealthBarsImmediate()
        {
            p1TargetFill = p1CurrentFill = p1TrailFill = 1f;
            p2TargetFill = p2CurrentFill = p2TrailFill = 1f;

            if (p1HealthValueText != null) p1HealthValueText.text = "100";
            if (p2HealthValueText != null) p2HealthValueText.text = "100";

            if (p1HealthFill != null) p1HealthFill.color = p1BaseBarColor;
            if (p2HealthFill != null) p2HealthFill.color = p2BaseBarColor;

            ApplyHealthFills();
        }

        public void SetPlayer1Health(float current, float max)
        {
            p1TargetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (p1HealthValueText != null)
            {
                p1HealthValueText.text = Mathf.CeilToInt(Mathf.Max(0f, current)).ToString();
            }
        }

        public void SetPlayer2Health(float current, float max)
        {
            p2TargetFill = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (p2HealthValueText != null)
            {
                p2HealthValueText.text = Mathf.CeilToInt(Mathf.Max(0f, current)).ToString();
            }
        }

        public void UpdateTimer(float remainingSeconds)
        {
            if (timerText == null) return;
            int displaySeconds = Mathf.Clamp(Mathf.CeilToInt(remainingSeconds), 0, 99);
            timerText.text = displaySeconds.ToString("00");

            if (displaySeconds <= 15)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 8f);
                timerText.color = Color.Lerp(new Color(1f, 0.25f, 0.20f, 1f), new Color(1f, 0.85f, 0.3f, 1f), pulse);
            }
            else
            {
                timerText.color = Color.white;
            }
        }

        public void UpdateRoundNumber(int roundIndex)
        {
            if (roundInfoText != null)
            {
                roundInfoText.text = $"ROUND {roundIndex}";
            }
        }

        public void UpdateRoundIndicators(int p1Wins, int p2Wins)
        {
            if (p1RoundPips != null)
            {
                for (int i = 0; i < p1RoundPips.Length; i++)
                {
                    if (p1RoundPips[i] != null)
                    {
                        p1RoundPips[i].color = i < p1Wins ? roundWonColor : roundEmptyColor;
                    }
                }
            }

            if (p2RoundPips != null)
            {
                for (int i = 0; i < p2RoundPips.Length; i++)
                {
                    if (p2RoundPips[i] != null)
                    {
                        p2RoundPips[i].color = i < p2Wins ? roundWonColor : roundEmptyColor;
                    }
                }
            }
        }

        public void ShowAnnouncement(string message, Color color)
        {
            if (announcementContainer != null)
            {
                announcementContainer.SetActive(true);
            }

            if (announcementText != null)
            {
                announcementText.text = message;
                announcementText.color = color;
            }

            announcementAnimTimer = 0f;
            animatingAnnouncement = true;

            if (announcementCanvasGroup != null) announcementCanvasGroup.alpha = 0f;
            if (announcementTextRect != null) announcementTextRect.localScale = Vector3.one * 1.45f;
        }

        public void HideAnnouncement()
        {
            animatingAnnouncement = false;
            if (announcementContainer != null)
            {
                announcementContainer.SetActive(false);
            }
        }

        public void ShowVictoryPanel(string winnerTitle, string subtitle, Color accentColor)
        {
            HideAnnouncement();

            if (victoryPanel != null)
            {
                victoryPanel.SetActive(true);
            }

            if (victoryTopAccent != null)
            {
                victoryTopAccent.color = accentColor;
            }

            if (victoryTitleText != null)
            {
                victoryTitleText.text = winnerTitle;
                victoryTitleText.color = Color.white;
            }

            if (victorySubtitleText != null)
            {
                victorySubtitleText.text = subtitle;
                victorySubtitleText.color = Color.Lerp(accentColor, Color.white, 0.45f);
            }

            victoryAnimTimer = 0f;
            animatingVictory = true;
            if (victoryCardRect != null)
            {
                victoryCardRect.localScale = Vector3.one * 0.85f;
            }
        }

        public void HideVictoryPanel()
        {
            animatingVictory = false;
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(false);
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            p1CurrentFill = Mathf.Lerp(p1CurrentFill, p1TargetFill, dt * healthLerpSpeed);
            p2CurrentFill = Mathf.Lerp(p2CurrentFill, p2TargetFill, dt * healthLerpSpeed);

            if (p1TrailFill > p1TargetFill)
            {
                p1TrailFill = Mathf.MoveTowards(p1TrailFill, p1TargetFill, dt * trailLerpSpeed * 0.35f);
            }
            else
            {
                p1TrailFill = p1TargetFill;
            }

            if (p2TrailFill > p2TargetFill)
            {
                p2TrailFill = Mathf.MoveTowards(p2TrailFill, p2TargetFill, dt * trailLerpSpeed * 0.35f);
            }
            else
            {
                p2TrailFill = p2TargetFill;
            }

            UpdateLowHealthPulse(p1HealthFill, p1BaseBarColor, p1TargetFill);
            UpdateLowHealthPulse(p2HealthFill, p2BaseBarColor, p2TargetFill);
            ApplyHealthFills();

            UpdateAnnouncementAnimation(Time.unscaledDeltaTime);
            UpdateVictoryAnimation(Time.unscaledDeltaTime);
        }

        private void UpdateLowHealthPulse(UnityEngine.UI.Image barFill, Color baseColor, float normalizedHp)
        {
            if (barFill == null) return;

            if (normalizedHp > 0f && normalizedHp <= 0.25f)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 9f);
                barFill.color = Color.Lerp(baseColor, lowHealthAlertColor, pulse * 0.85f);
            }
            else
            {
                barFill.color = baseColor;
            }
        }

        private void UpdateAnnouncementAnimation(float dt)
        {
            if (!animatingAnnouncement) return;

            announcementAnimTimer += dt;
            float t = Mathf.Clamp01(announcementAnimTimer / 0.24f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (announcementCanvasGroup != null)
            {
                announcementCanvasGroup.alpha = eased;
            }

            if (announcementTextRect != null)
            {
                float scale = Mathf.Lerp(1.42f, 1f, eased);
                announcementTextRect.localScale = Vector3.one * scale;
            }

            if (t >= 1f)
            {
                animatingAnnouncement = false;
            }
        }

        private void UpdateVictoryAnimation(float dt)
        {
            if (!animatingVictory || victoryCardRect == null) return;

            victoryAnimTimer += dt;
            float t = Mathf.Clamp01(victoryAnimTimer / 0.28f);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            victoryCardRect.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, eased);

            if (t >= 1f)
            {
                animatingVictory = false;
            }
        }

        private void ApplyHealthFills()
        {
            if (p1HealthFill != null) p1HealthFill.fillAmount = p1CurrentFill;
            if (p1DamageTrailFill != null) p1DamageTrailFill.fillAmount = p1TrailFill;

            if (p2HealthFill != null) p2HealthFill.fillAmount = p2CurrentFill;
            if (p2DamageTrailFill != null) p2DamageTrailFill.fillAmount = p2TrailFill;
        }
    }
}
