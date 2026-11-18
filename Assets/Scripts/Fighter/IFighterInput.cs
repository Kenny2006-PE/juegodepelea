namespace FightingGame
{
    /// <summary>
    /// Decoupled input contract implemented by both FighterInputHandler (Human P1/P2)
    /// and FighterAIController (CPU), allowing any fighter prefab to be driven by any controller.
    /// </summary>
    public interface IFighterInput
    {
        float MoveX { get; }
        bool IsBlocking { get; }

        bool ConsumeJab();
        bool ConsumeKick();

        void SetInputEnabled(bool enabled);
        void ClearBufferedInputs();
    }
}
