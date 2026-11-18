using UnityEngine;

namespace FightingGame
{
    public enum GameMode
    {
        SinglePlayerVsCPU = 0,
        LocalTwoPlayer = 1
    }

    public enum FighterId
    {
        Tanjiro = 0,
        Madara = 1
    }

    public enum AIDifficulty
    {
        Easy = 0,
        Normal = 1,
        Hard = 2
    }

    /// <summary>
    /// Stores the active match configuration selected in the main menu.
    /// Shared across MenuManager, GameManager, UIManager, and FighterAIController.
    /// </summary>
    public static class MatchSettings
    {
        public static GameMode CurrentMode { get; set; } = GameMode.SinglePlayerVsCPU;
        public static FighterId Player1Fighter { get; set; } = FighterId.Tanjiro;
        public static FighterId Player2Fighter { get; set; } = FighterId.Madara;
        public static AIDifficulty CurrentDifficulty { get; set; } = AIDifficulty.Normal;

        public static int RoundsToWin { get; set; } = 2; // Best of 3
        public static float RoundDuration { get; set; } = 99f;

        public static string GetDisplayName(FighterId id)
        {
            switch (id)
            {
                case FighterId.Tanjiro:
                    return "TANJIRO";
                case FighterId.Madara:
                    return "MADARA";
                default:
                    return id.ToString().ToUpperInvariant();
            }
        }

        public static string GetFullName(FighterId id)
        {
            switch (id)
            {
                case FighterId.Tanjiro:
                    return "Tanjiro Kamado";
                case FighterId.Madara:
                    return "Madara Uchiha";
                default:
                    return id.ToString();
            }
        }

        public static Color GetAccentColor(FighterId id)
        {
            switch (id)
            {
                case FighterId.Tanjiro:
                    return new Color(0.10f, 0.78f, 0.56f, 1f); // Demon Slayer emerald/teal
                case FighterId.Madara:
                    return new Color(0.84f, 0.16f, 0.24f, 1f); // Uchiha Sharingan crimson
                default:
                    return Color.white;
            }
        }

        public static FighterId GetOppositeFighter(FighterId id)
        {
            return id == FighterId.Tanjiro ? FighterId.Madara : FighterId.Tanjiro;
        }

        public static string GetDifficultyLabel(AIDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AIDifficulty.Easy:
                    return "FÁCIL";
                case AIDifficulty.Normal:
                    return "NORMAL";
                case AIDifficulty.Hard:
                    return "DIFÍCIL";
                default:
                    return difficulty.ToString();
            }
        }
    }
}
