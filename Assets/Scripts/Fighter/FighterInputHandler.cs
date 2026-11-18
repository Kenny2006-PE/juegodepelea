using UnityEngine;

namespace FightingGame
{
    public enum PlayerSlot
    {
        Player1 = 0,
        Player2 = 1
    }

    /// <summary>
    /// Reads keyboard inputs for either Player 1 or Player 2 with a short input buffer
    /// for responsive fighting game controls.
    ///
    /// Player 1: Move (A/D), Block (S), Jab Cross (F), Roundhouse Kick (G)
    /// Player 2: Move (Left/Right Arrows), Block (Down Arrow), Jab Cross (J / Numpad 1), Roundhouse Kick (K / Numpad 2)
    /// </summary>
    public class FighterInputHandler : MonoBehaviour, IFighterInput
    {
        [SerializeField] private PlayerSlot playerSlot = PlayerSlot.Player1;
        [SerializeField] private float inputBufferTime = 0.14f;

        private bool inputEnabled = true;
        private float jabBufferTimer;
        private float kickBufferTimer;

        public PlayerSlot Slot => playerSlot;
        public float MoveX { get; private set; }
        public bool IsBlocking { get; private set; }

        public void Configure(PlayerSlot slot)
        {
            playerSlot = slot;
            ClearBufferedInputs();
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                MoveX = 0f;
                IsBlocking = false;
                ClearBufferedInputs();
            }
        }

        public void ClearBufferedInputs()
        {
            jabBufferTimer = 0f;
            kickBufferTimer = 0f;
        }

        public bool ConsumeJab()
        {
            if (jabBufferTimer > 0f)
            {
                jabBufferTimer = 0f;
                return true;
            }
            return false;
        }

        public bool ConsumeKick()
        {
            if (kickBufferTimer > 0f)
            {
                kickBufferTimer = 0f;
                return true;
            }
            return false;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            if (jabBufferTimer > 0f) jabBufferTimer -= dt;
            if (kickBufferTimer > 0f) kickBufferTimer -= dt;

            if (!inputEnabled)
            {
                MoveX = 0f;
                IsBlocking = false;
                return;
            }

            if (playerSlot == PlayerSlot.Player1)
            {
                ReadPlayer1Input();
            }
            else
            {
                ReadPlayer2Input();
            }
        }

        private void ReadPlayer1Input()
        {
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.D)) horizontal += 1f;

            MoveX = horizontal;
            IsBlocking = Input.GetKey(KeyCode.S);

            if (Input.GetKeyDown(KeyCode.F)) jabBufferTimer = inputBufferTime;
            if (Input.GetKeyDown(KeyCode.G)) kickBufferTimer = inputBufferTime;
        }

        private void ReadPlayer2Input()
        {
            float horizontal = 0f;
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;

            MoveX = horizontal;
            IsBlocking = Input.GetKey(KeyCode.DownArrow);

            if (Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Keypad1))
            {
                jabBufferTimer = inputBufferTime;
            }

            if (Input.GetKeyDown(KeyCode.K) || Input.GetKeyDown(KeyCode.Keypad2))
            {
                kickBufferTimer = inputBufferTime;
            }
        }
    }
}
