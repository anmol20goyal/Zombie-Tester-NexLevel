using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class LocomotionController : MonoBehaviour
{
    #region PlayerVariables

    [Header("*****Player*****")]
    [SerializeField] private float _moveSpeed = 2.0f;
    [SerializeField] private float _sprintSpeed = 5.335f;
    [Range(0.0f, 0.3f)]
    [SerializeField] private float _rotationSmoothTime = 0.12f;
    [SerializeField] private float _speedChangeRate = 10.0f;

    [Space(10)]
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _gravity = -15.0f;

    [SerializeField] private float _fallTimeout = 0.15f;

    private float _speed;
    private float _animationBlend;
    private float _targetRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;

    #endregion

    #region Sound

    [SerializeField] private AudioClip _landingAudioClip;
    [SerializeField] private AudioClip[] _footstepAudioClips;

    #endregion

    #region CameraVariables

    [Header("*****Cinemachine*****")]
    [SerializeField] private GameObject _cinemachineCameraTarget;
    [SerializeField] private float _topClamp = 70.0f;
    [SerializeField] private float _bottomClamp = -30.0f;
    [SerializeField] private bool _lockCameraPosition = false;
    private float _cinemachineTargetYaw;
    private float _cinemachineTargetPitch;
    private const float _camRotThreshold = 0.01f;

    #endregion

    #region Player Grounded

    [Header("*****Player Grounded*****")]
    [SerializeField] private bool _isGrounded = true;
    [SerializeField] private float _groundedOffset = -0.14f;
    [SerializeField] private float _groundedRadius = 0.28f; //Should match the radius of the CharacterController
    [SerializeField] private LayerMask _groundLayers;
    private float _fallTimeoutDelta;

    #endregion

    #region Animation IDs

    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDThrow;
    private int _animIDDead;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    #endregion

    #region GameObjects

    [Header("*****GameObjects*****")]
    [SerializeField] private GameObject _stone;
    [SerializeField] private GameObject _throwInitialPoint;
    private Animator _animator;
    private CharacterController _charController;
    private InputsController _inputController;
    private GameObject _mainCamera;


    #endregion

    private void Awake()
    {
        if (_mainCamera == null)
            _mainCamera = Camera.main.gameObject;
    }

    private void Start()
    {
        _cinemachineTargetYaw = _cinemachineCameraTarget.transform.rotation.eulerAngles.y;

        _animator = GetComponent<Animator>();
        _charController = GetComponent<CharacterController>();
        _inputController = GetComponent<InputsController>();

        AssignAnimationIDs();

        // reset our timeouts on start
        _fallTimeoutDelta = _fallTimeout;
    }

    private void Update()
    {
        JumpAndGravity();
        GroundedCheck();
        Move();
        ThrowItem();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void AssignAnimationIDs()
    {
        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDThrow = Animator.StringToHash("Throw");
        //_animIDDead = Animator.StringToHash("Dead");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
        _isGrounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);

        _animator.SetBool(_animIDGrounded, _isGrounded);
    }

    private void CameraRotation()
    {
        if (_inputController.look.sqrMagnitude >= _camRotThreshold && !_lockCameraPosition)
        {
            _cinemachineTargetYaw += _inputController.look.x;
            _cinemachineTargetPitch += _inputController.look.y;
        }

        // clamp our rotations so our values are limited 360 degrees
        _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
        _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _bottomClamp, _topClamp);

        _cinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch, _cinemachineTargetYaw, 0.0f);
    }

    private void Move()
    {
        float targetSpeed = _inputController.sprint ? _sprintSpeed : _moveSpeed;

        if (_inputController.move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_charController.velocity.x, 0.0f, _charController.velocity.z).magnitude;

        float speedOffset = 0.1f;
        float inputMagnitude = _inputController.move.magnitude;

        // accelerate or decelerate to target speed
        if (currentHorizontalSpeed < targetSpeed - speedOffset ||
            currentHorizontalSpeed > targetSpeed + speedOffset)
        {
            _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                Time.deltaTime * _speedChangeRate);
            _speed = Mathf.Round(_speed * 1000f) / 1000f;
        }
        else
        {
            _speed = targetSpeed;
        }

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * _speedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = new Vector3(_inputController.move.x, 0.0f, _inputController.move.y).normalized;

        if (_inputController.move != Vector2.zero)
        {
            _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                              _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                _rotationSmoothTime);

            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        // move player
        _charController.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                         new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        // update animator
        _animator.SetFloat(_animIDSpeed, _animationBlend);
        _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
    }

    private void JumpAndGravity()
    {
        if (_isGrounded)
        {
            _fallTimeoutDelta = _fallTimeout;

            // update animator
            _animator.SetBool(_animIDJump, false);
            _animator.SetBool(_animIDFreeFall, false);

            // stop velocity dropping to infinite when grounded
            if (_verticalVelocity < 0.0f) _verticalVelocity = 0f;

            // Jump
            if (_inputController.jump)
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

                // update animator
                _animator.SetBool(_animIDJump, true);
            }
        }
        else
        {
            if (_fallTimeoutDelta >= 0.0f)
                _fallTimeoutDelta -= Time.deltaTime;
            else
                _animator.SetBool(_animIDFreeFall, true);

            _inputController.jump = false;
        }

        _verticalVelocity += _gravity * Time.deltaTime;
    } 

    private float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void ThrowItem()
    {
        if (_inputController.throwItem)
        {
            _inputController.throwItem = false;
            // update animator
            _animator.SetTrigger(_animIDThrow);
        }
    }

    public void ThrowItemProjectile(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            var stonePos = _throwInitialPoint.transform.position;
            Instantiate(_stone, stonePos, Quaternion.Euler(Vector3.zero), _throwInitialPoint.transform);
            // stone projectile to where clicked -> get mouse pos for direction
        }
    }

    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new(1.0f, 0.0f, 0.0f, 0.35f);

        if (_isGrounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z),
            _groundedRadius);
    }

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (_footstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, _footstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(_footstepAudioClips[index], transform.TransformPoint(_charController.center));
            }
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            AudioSource.PlayClipAtPoint(_landingAudioClip, transform.TransformPoint(_charController.center));
        }
    }

    public void OnPlayerDeath(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            UIController.instance.PlayerDead();
        }
    }
}