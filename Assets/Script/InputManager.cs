using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private PlayerControls playerControls;
    private PlayerController currentPlayer;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        playerControls = new PlayerControls();
    }

    private void OnEnable()
    {
        if (playerControls != null)
        {
            playerControls.Snake.Enable();
        }
    }

    private void OnDisable()
    {
        if (playerControls != null)
        {
            playerControls.Snake.Disable();
        }
    }

    public void RegisterPlayer(PlayerController player)
    {
        this.currentPlayer = player;
    }

    void Update()
    {
        if (currentPlayer == null) return;

        Vector2 input = playerControls.Snake.Move.ReadValue<Vector2>();
        if (input == Vector2.zero) return;

        Vector2 potentialMoveDir = Vector2.zero;
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            potentialMoveDir.x = Mathf.Sign(input.x);
        }
        else
        {
            potentialMoveDir.y = Mathf.Sign(input.y);
        }

        currentPlayer.AttemptMove(potentialMoveDir);
    }
}