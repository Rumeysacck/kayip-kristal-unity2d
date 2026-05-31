using UnityEngine;

[RequireComponent(typeof(Platformer2D))]
public class Platformer2DController : MonoBehaviour
{
    [Header("Controls")]
    public string MovementButtonName = "Horizontal";
    public KeyCode JumpKey = KeyCode.W;
    public KeyCode AlternateJumpKey = KeyCode.UpArrow;
    public KeyCode DashKey = KeyCode.Space;
    public KeyCode CrouchKey = KeyCode.S;
    public KeyCode AlternateCrouchKey = KeyCode.DownArrow;

    private Platformer2D _platformer2D;
    private float _horizontalAxis;
    private bool _jumpButtonPressed;
    private bool _dashButtonPressed;

    private void Awake() => _platformer2D = GetComponent<Platformer2D>();

    private void Update()
    {
        _horizontalAxis = Input.GetAxis(MovementButtonName);
        if (Input.GetKeyDown(JumpKey) || Input.GetKeyDown(AlternateJumpKey)) _jumpButtonPressed = true;
        if (Input.GetKeyDown(DashKey)) _dashButtonPressed = true;
    }

    private void FixedUpdate() => Move();

    private void Move()
    {
        _platformer2D.SetCrouching(Input.GetKey(CrouchKey) || Input.GetKey(AlternateCrouchKey));
        _platformer2D.Move(_horizontalAxis);
        if (_dashButtonPressed)
        {
            _platformer2D.Dash(_horizontalAxis);
            _dashButtonPressed = false;
        }
        if (_jumpButtonPressed)
        {
            _platformer2D.Jump();
            _jumpButtonPressed = false;
        }
    }
}
