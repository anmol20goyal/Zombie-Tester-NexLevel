using UnityEngine;
using UnityEngine.InputSystem;

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
    private bool _canMove;

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

    #region Scripts

    [SerializeField] private ThrowItem _throwItem_S;
    [SerializeField] private DamageControl _damageControl_S;

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

        // reset our timeouts on start
        _fallTimeoutDelta = _fallTimeout;
        _canMove = false;
        _inputController.enabled = false;
    }

    public void StartGame()
    {
        _canMove = true;
        _inputController.enabled = true;
    }

    private void Update()
    {
        if (!_canMove) return;
        JumpAndGravity();
        GroundedCheck();
        Move();
        ThrowStone();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
        _isGrounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);

        _animator.SetBool(AnimationHandler.instance.animIDGrounded, _isGrounded);
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
        if (!_canMove) return;
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
        _animator.SetFloat(AnimationHandler.instance.animIDSpeed, _animationBlend);
        _animator.SetFloat(AnimationHandler.instance.animIDMotionSpeed, inputMagnitude);
    }

    private void JumpAndGravity()
    {
        if (_isGrounded)
        {
            _fallTimeoutDelta = _fallTimeout;

            // update animator
            _animator.SetBool(AnimationHandler.instance.animIDJump, false);
            _animator.SetBool(AnimationHandler.instance.animIDFreeFall, false);

            // stop velocity dropping to infinite when grounded
            if (_verticalVelocity < 0.0f) _verticalVelocity = 0f;

            // Jump
            if (_inputController.jump)
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);

                // update animator
                _animator.SetBool(AnimationHandler.instance.animIDJump, true);
            }
        }
        else
        {
            if (_fallTimeoutDelta >= 0.0f)
                _fallTimeoutDelta -= Time.deltaTime;
            else
                _animator.SetBool(AnimationHandler.instance.animIDFreeFall, true);

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

    #region ThrowItems

    private void ThrowStone()
    {
        if (_inputController.throwItem)
        {
            _inputController.throwItem = false;
            var target = GetMousePos();
            transform.LookAt(new Vector3(target.x, transform.position.y, target.z));
            _throwItem_S.StartThrow(_animator, target);
        }
    }

    private Vector3 GetMousePos()
    {
        var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, float.MaxValue))
        {
            return hit.point;
        }

        return Vector3.zero;
    }

    #endregion

    /*private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new(1.0f, 0.0f, 0.0f, 0.35f);

        if (_isGrounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z),
            _groundedRadius);
    }*/

    #region Animation Events

    // for player walk sound
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

    // for jump land sound
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

    #endregion

    #region Collision Methods

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stone"))
        {
            _damageControl_S.StoneHit();
        }

        if (other.CompareTag("Enemy"))
        {
            _damageControl_S.EnemyFinalAttack();
            _canMove = false;
            Debug.Log("death started");
        }
    }

    #endregion
}