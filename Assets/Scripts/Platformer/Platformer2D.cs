using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(AudioSource))]
[RequireComponent(typeof(BoxCollider2D))]
public class Platformer2D : MonoBehaviour
{
    [Header("Movement")]
    [Range(0f, 50f)] public float MovementSpeed = 10f;
    [Range(0f, 1000f)] public float JumpStrength = 300f;

    [Header("Crouch")]
    [Range(0.25f, 1f)] public float CrouchColliderHeightMultiplier = 0.42f;
    [Range(0.1f, 1f)] public float CrouchSpeedMultiplier = 0.55f;
    [Range(0f, 0.2f)] public float StandUpCheckSkin = 0.03f;
    public Transform CrouchVisualRoot;
    public Vector2 CrouchVisualScale = new(1.08f, 0.62f);
    public Vector2 CrouchVisualOffset = new(0f, -0.24f);

    [Header("Physics")]
    public Vector2 MaxVelocity = new(10f, 30f);
    public Vector2 ApproximateZeroVelocity = new(0.01f, 0.01f);

    [Header("Dash")]
    [Range(0f, 40f)] public float DashSpeed = 13.5f;
    [Range(0.02f, 0.5f)] public float DashSeconds = 0.13f;
    [Range(0f, 2f)] public float DashCooldownSeconds = 0.45f;
    [Range(0f, 100f)] public float DashBlockPushForce = 28f;
    [Range(0f, 3f)] public float DashBlockBurstRadius = 1.15f;
    [Range(0f, 2f)] public float DashBlockUpwardBoost = 0.45f;
    public Vector2 DashHitBoxSize = new(1.1f, 0.9f);
    public Vector2 DashHitBoxOffset = new(0.76f, 0.05f);

    [Header("Animator")]
    public string MovingAnimBoolParamName = "Moving";
    public string JumpingAnimBoolParamName = "Jumping";
    public string FallingAnimBoolParamName = "Falling";
    public string CrouchingAnimBoolParamName = "Crouching";

    [Header("Dust Effect")]
    public GameObject Dust;
    public string MovingStartDustAnimStateName = "Dust_OnRunStart";
    public string StoppingDustAnimStateName = "Dust_OnRunStop";
    public string JumpingDustAnimStateName = "Dust_OnJump";
    public string LandingDustAnimStateName = "Dust_OnFall";
    [Header("Dust Effect / Offsets")]
    public float MovingStartDustOffset = -0.3f;
    public float StoppingDustOffset = 0.3f;

    [Header("Audio")]
    public bool AudioEnabled = true;
    public AudioClip[] MovingSoundFXs;
    public AudioClip JumpingSoundFX;
    public AudioClip LandingSoundFX;

    private Rigidbody2D _rigidBody2D;
    private Animator _animator;
    private AudioSource _audioSource;
    private BoxCollider2D _boxCollider2D;
    private float _originalDrag;
    private Vector2 _standingColliderSize;
    private Vector2 _standingColliderOffset;
    private Vector2 _crouchingColliderSize;
    private Vector2 _crouchingColliderOffset;
    private Vector3 _standingVisualLocalPosition;
    private Vector3 _standingVisualLocalScale;
    private bool _crouchRequested;
    private bool _hasCrouchingAnimParam;
    private float _dashUntil;
    private float _nextDashTime;
    private float _dashDirection = 1f;
    private float _baseMovementSpeed;
    private float _baseMaxVelocityX;
    private bool _temporarySpeedBoostActive;
    private readonly HashSet<Rigidbody2D> _dashPushedBodies = new();

    public bool IsMoving => Mathf.Abs(_rigidBody2D.linearVelocity.x) > ApproximateZeroVelocity.x;
    public bool IsJumping => _rigidBody2D.linearVelocity.y > ApproximateZeroVelocity.y;
    public bool IsFalling => _rigidBody2D.linearVelocity.y < -ApproximateZeroVelocity.y;
    public bool IsDashing => Time.time < _dashUntil;
    public bool IsCrouching { get; private set; }

    private void Awake()
    {
        _rigidBody2D = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
        _audioSource = GetComponent<AudioSource>();
        _boxCollider2D = GetComponent<BoxCollider2D>();
        _originalDrag = _rigidBody2D.linearDamping;
        _standingColliderSize = _boxCollider2D.size;
        _standingColliderOffset = _boxCollider2D.offset;
        _baseMovementSpeed = MovementSpeed;
        _baseMaxVelocityX = MaxVelocity.x;
        if (!CrouchVisualRoot)
            CrouchVisualRoot = transform.Find("KayipKristal_ExplorerStyle");
        if (CrouchVisualRoot)
        {
            _standingVisualLocalPosition = CrouchVisualRoot.localPosition;
            _standingVisualLocalScale = CrouchVisualRoot.localScale;
        }
        ConfigureCrouchCollider();
        _hasCrouchingAnimParam = HasAnimatorBool(CrouchingAnimBoolParamName);
    }

    private void FixedUpdate()
    {
        ApplyDashVelocity();
        _rigidBody2D.linearDamping = IsFalling ? 0f : _originalDrag;
        UpdateCrouch();
        Animate();
    }

    private void Animate()
    {
        _animator.SetBool(MovingAnimBoolParamName, IsMoving);
        _animator.SetBool(JumpingAnimBoolParamName, IsJumping);
        _animator.SetBool(FallingAnimBoolParamName, IsFalling);
        if (_hasCrouchingAnimParam) _animator.SetBool(CrouchingAnimBoolParamName, IsCrouching);
    }

    private bool HasAnimatorBool(string parameterName)
    {
        if (!_animator || string.IsNullOrEmpty(parameterName)) return false;
        foreach (AnimatorControllerParameter parameter in _animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Bool && parameter.name == parameterName) return true;
        }
        return false;
    }

    private void ConfigureCrouchCollider()
    {
        float crouchHeight = _standingColliderSize.y * CrouchColliderHeightMultiplier;
        float heightDifference = _standingColliderSize.y - crouchHeight;
        _crouchingColliderSize = new Vector2(_standingColliderSize.x, crouchHeight);
        _crouchingColliderOffset = new Vector2(_standingColliderOffset.x, _standingColliderOffset.y - heightDifference * 0.5f);
    }

    private void ApplyCollider(Vector2 size, Vector2 offset)
    {
        _boxCollider2D.size = size;
        _boxCollider2D.offset = offset;
    }

    private void ApplyCrouchVisual(bool crouching)
    {
        if (!CrouchVisualRoot) return;

        if (crouching)
        {
            CrouchVisualRoot.localPosition = _standingVisualLocalPosition + new Vector3(CrouchVisualOffset.x, CrouchVisualOffset.y, 0f);
            CrouchVisualRoot.localScale = new Vector3(_standingVisualLocalScale.x * CrouchVisualScale.x, _standingVisualLocalScale.y * CrouchVisualScale.y, _standingVisualLocalScale.z);
            return;
        }

        CrouchVisualRoot.localPosition = _standingVisualLocalPosition;
        CrouchVisualRoot.localScale = _standingVisualLocalScale;
    }

    private void UpdateCrouch()
    {
        if (_crouchRequested)
        {
            if (!IsCrouching)
            {
                IsCrouching = true;
                ApplyCollider(_crouchingColliderSize, _crouchingColliderOffset);
                ApplyCrouchVisual(true);
            }
            return;
        }

        if (!IsCrouching || !CanStandUp()) return;
        IsCrouching = false;
        ApplyCollider(_standingColliderSize, _standingColliderOffset);
        ApplyCrouchVisual(false);
    }

    private bool CanStandUp()
    {
        float scaleX = Mathf.Abs(transform.lossyScale.x);
        float scaleY = Mathf.Abs(transform.lossyScale.y);
        float extraHeight = (_standingColliderSize.y - _crouchingColliderSize.y) * scaleY;
        if (extraHeight <= 0f) return true;

        Bounds currentBounds = _boxCollider2D.bounds;
        Vector2 checkSize = new Vector2(_standingColliderSize.x * scaleX * 0.85f, Mathf.Max(0.01f, extraHeight - StandUpCheckSkin));
        Vector2 checkCenter = new Vector2(currentBounds.center.x, currentBounds.max.y + checkSize.y * 0.5f + StandUpCheckSkin);
        Collider2D[] hits = Physics2D.OverlapBoxAll(checkCenter, checkSize, transform.eulerAngles.z);
        foreach (Collider2D hit in hits)
        {
            if (!hit || hit.isTrigger || hit.attachedRigidbody == _rigidBody2D) continue;
            return false;
        }
        return true;
    }

    private void CreateDust(string animatorStateName, float xOffset = 0f)
    {
        if (!Dust) return;
        float direction = Mathf.Sign(transform.localScale.x);
        Vector3 position = new Vector3(transform.position.x + direction * xOffset, transform.position.y, transform.position.z);
        GameObject dust = Instantiate(Dust, position, Quaternion.identity);
        Vector3 scale = dust.transform.localScale;
        scale.x = direction * Mathf.Abs(scale.x);
        dust.transform.localScale = scale;
        Animator dustAnimator = dust.GetComponent<Animator>();
        dustAnimator.Play(animatorStateName);
    }

    public void FlipXScale(float direction)
    {
        if (direction == 0f) return;
        direction = Mathf.Sign(direction);
        float absLocalScaleX = Mathf.Abs(transform.localScale.x);
        transform.localScale = new Vector3(direction * absLocalScaleX, transform.localScale.y, transform.localScale.z);
    }

    public void SetCrouching(bool crouching)
    {
        if (IsDashing)
        {
            _crouchRequested = false;
            return;
        }

        _crouchRequested = crouching;
    }

    public void Move(float direction, bool autoFlipXScale = true)
    {
        if (IsDashing) return;
        if (direction == 0f) return;
        direction = Mathf.Clamp(direction, -1f, 1f);
        float velocityX = _rigidBody2D.linearVelocity.x;
        if (Mathf.Abs(velocityX) >= MaxVelocity.x && Mathf.Sign(velocityX) == Mathf.Sign(direction)) return;
        float movementSpeed = IsCrouching ? MovementSpeed * CrouchSpeedMultiplier : MovementSpeed;
        _rigidBody2D.AddForce(direction * movementSpeed * Vector2.right);
        if (autoFlipXScale) FlipXScale(direction);
    }

    public void Jump()
    {
        if (IsDashing) return;
        if (IsJumping || IsFalling) return;
        if (Mathf.Abs(_rigidBody2D.linearVelocity.y) >= MaxVelocity.y) return;
        _rigidBody2D.AddForce(JumpStrength * Vector2.up);
        CreateDust(JumpingDustAnimStateName);
    }

    public void Dash(float inputDirection = 0f)
    {
        if (Time.time < _nextDashTime || IsDashing) return;

        _dashDirection = Mathf.Abs(inputDirection) > 0.1f ? Mathf.Sign(inputDirection) : Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(_dashDirection, 0f))
            _dashDirection = 1f;

        _nextDashTime = Time.time + DashCooldownSeconds;
        _dashUntil = Time.time + DashSeconds;
        _dashPushedBodies.Clear();
        _crouchRequested = false;
        if (IsCrouching)
        {
            IsCrouching = false;
            ApplyCollider(_standingColliderSize, _standingColliderOffset);
            ApplyCrouchVisual(false);
        }

        FlipXScale(_dashDirection);
        ApplyDashVelocity();
        CreateDust(MovingStartDustAnimStateName, MovingStartDustOffset);
        PushDashBlocks();
    }

    private void ApplyDashVelocity()
    {
        if (!IsDashing) return;

        Vector2 velocity = _rigidBody2D.linearVelocity;
        _rigidBody2D.linearVelocity = new Vector2(_dashDirection * DashSpeed, velocity.y);
        PushDashBlocks();
    }

    private void PushDashBlocks()
    {
        if (DashBlockPushForce <= 0f) return;

        Vector2 offset = new Vector2(DashHitBoxOffset.x * _dashDirection, DashHitBoxOffset.y);
        Vector2 center = (Vector2)transform.position + offset;
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, DashHitBoxSize, 0f);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D body = hit != null ? hit.attachedRigidbody : null;
            if (!IsPushableDynamicBlock(body)) continue;

            PushDashBlock(body, 1f);
            PushNearbyDashBlocks(body);
        }
    }

    private bool IsPushableDynamicBlock(Rigidbody2D body)
    {
        if (body == null || body == _rigidBody2D || body.bodyType != RigidbodyType2D.Dynamic) return false;
        if (_dashPushedBodies.Contains(body)) return false;
        if (body.GetComponentInParent<Actor>() != null) return false;
        if (body.GetComponentInParent<Collectable>() != null) return false;

        string lowerName = body.gameObject.name.ToLowerInvariant();
        return lowerName.Contains("box") || lowerName.Contains("crate") || lowerName.Contains("block");
    }

    private void PushNearbyDashBlocks(Rigidbody2D primaryBody)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(primaryBody.position, DashBlockBurstRadius);
        foreach (Collider2D hit in hits)
        {
            Rigidbody2D body = hit != null ? hit.attachedRigidbody : null;
            if (!IsPushableDynamicBlock(body)) continue;
            PushDashBlock(body, 0.7f);
        }
    }

    private void PushDashBlock(Rigidbody2D body, float multiplier)
    {
        _dashPushedBodies.Add(body);
        Vector2 pushDirection = new(_dashDirection, DashBlockUpwardBoost);
        body.WakeUp();
        body.AddForce(pushDirection.normalized * DashBlockPushForce * multiplier, ForceMode2D.Impulse);
        body.AddTorque(-_dashDirection * DashBlockPushForce * 0.22f * multiplier, ForceMode2D.Impulse);
    }

    public void ApplyTemporarySpeedBoost(float movementSpeed, float maxVelocityX)
    {
        _temporarySpeedBoostActive = true;
        MovementSpeed = Mathf.Max(_baseMovementSpeed, movementSpeed);
        MaxVelocity = new Vector2(Mathf.Max(_baseMaxVelocityX, maxVelocityX), MaxVelocity.y);
    }

    public void ClearTemporarySpeedBoost()
    {
        if (!_temporarySpeedBoostActive) return;

        _temporarySpeedBoostActive = false;
        MovementSpeed = _baseMovementSpeed;
        MaxVelocity = new Vector2(_baseMaxVelocityX, MaxVelocity.y);
    }

    #region Animation Events
    public void OnMovingStart()
    {
        CreateDust(MovingStartDustAnimStateName, MovingStartDustOffset);
    }

    public void OnMoving()
    {
        if (AudioEnabled && MovingSoundFXs != null && MovingSoundFXs.Length > 0)
            AudioManager.Instance.PlaySoundFXLocally(_audioSource, MovingSoundFXs[Random.Range(0, MovingSoundFXs.Length)]);
    }

    public void OnStopping()
    {
        if (AudioEnabled && MovingSoundFXs != null && MovingSoundFXs.Length > 0)
            AudioManager.Instance.PlaySoundFXLocally(_audioSource, MovingSoundFXs[0]);
        CreateDust(StoppingDustAnimStateName, StoppingDustOffset);
    }

    public void OnJumping()
    {
        if (AudioEnabled && JumpingSoundFX != null) AudioManager.Instance.PlaySoundFXLocally(_audioSource, JumpingSoundFX);
    }

    public void OnLanding()
    {
        if (AudioEnabled && LandingSoundFX != null) AudioManager.Instance.PlaySoundFXLocally(_audioSource, LandingSoundFX);
        CreateDust(LandingDustAnimStateName);
    }
    #endregion
}
