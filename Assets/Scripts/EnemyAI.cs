using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    #region GameObjects

    [Header("*****GameObjects*****")]
    [SerializeField] private Animator _animator;
    private NavMeshAgent _agent;
    [SerializeField] private Transform _player;
    [SerializeField] private LayerMask _whatIsGround, _whatIsPlayer;
    [SerializeField] private AudioSource _audioS;

    #endregion

    #region Scripts

    [SerializeField] private DamageControl _damageControl_S;
    [SerializeField] private ThrowItem _throwItem_S;

    #endregion

    #region Variables

    [Header("*****Variables*****")]
    [SerializeField] private bool _canMove;
    [SerializeField] private bool _jumpAttack;
    [SerializeField] private Vector3 _newJumpPos;
    [SerializeField] private float _walkSpeed = 0.5f;
    [SerializeField] private float _runningSpeed = 5.335f;
    [SerializeField] private float _jumpMoveSpeed = 1f;
    private bool _alreadyAttacked;
    private bool _alreadyThrown;
    
    //States
    [SerializeField] private float _sightRange;
    [SerializeField] private float _attackRange;
    [SerializeField] private float _jumpAttackRange = 2f;
    [SerializeField] private bool _playerInSightRange;
    [SerializeField] private bool _playerInAttackRange;
    [SerializeField] private bool _playerInJumpAttackRange;
    [SerializeField] private float _health;

    //Patroling
    [SerializeField] private Vector3 _walkPoint;
    [SerializeField] private float _walkPointRange;
    private bool _walkPointSet;

    #endregion

    #region SoundClips

    [Header("*****Sounds*****")]
    [SerializeField] private AudioClip _crawlClip;

    #endregion

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        _alreadyAttacked = false;
        _alreadyThrown = false;
        _walkPointSet = false;
    }

    public void StartGame()
    {
        _canMove = true;
        _audioS.enabled = true;
    }

    private void Update()
    {
        if (_jumpAttack)
        {
            MoveEnemyOnJumpAttack();
        }

        if (_damageControl_S.currentHealth <= 0)
        {
            EnemyDied();
        }

        if (!_canMove) return;

        //Check for sight and attack range
        _playerInSightRange = Physics.CheckSphere(transform.position, _sightRange, _whatIsPlayer);
        _playerInAttackRange = Physics.CheckSphere(transform.position, _attackRange, _whatIsPlayer);
        _playerInJumpAttackRange = Physics.CheckSphere(transform.position, _jumpAttackRange, _whatIsPlayer);

        if (!_playerInSightRange && !_playerInAttackRange) Patroling();
        if (_playerInSightRange && !_playerInAttackRange) ThrowAtPlayer(); // throw items
        if (_playerInAttackRange && _playerInSightRange) AttackPlayer(); // run to player
        if (_playerInAttackRange && _playerInSightRange && _playerInJumpAttackRange) JumpAttackPlayer(); // run and jump attack
    }

    #region Patroling

    private void Patroling()
    {
        if (!_walkPointSet) SearchWalkPoint();

        if (_walkPointSet)
        {
            _agent.SetDestination(_walkPoint);
            _agent.speed = _walkSpeed;
            _agent.acceleration = 1;
            _animator.ResetTrigger(AnimationHandler.instance.animIDThrow);
            _animator.SetFloat(AnimationHandler.instance.animIDSpeed, 1);
            _animator.SetFloat(AnimationHandler.instance.animIDMotionSpeed, 1);
        }

        Vector3 distanceToWalkPoint = transform.position - _walkPoint;

        //Walkpoint reached
        if (distanceToWalkPoint.magnitude < 1f)
            _walkPointSet = false;
    }

    private void SearchWalkPoint()
    {
        //Calculate random point in range
        float randomZ = Random.Range(-_walkPointRange, _walkPointRange);
        float randomX = Random.Range(-_walkPointRange, _walkPointRange);

        _walkPoint = new Vector3(transform.position.x + randomX, transform.position.y, transform.position.z + randomZ);

        if (NavMesh.SamplePosition(_walkPoint, out NavMeshHit hit, 2, NavMesh.AllAreas))
        {
            _walkPoint = hit.position;
            _walkPointSet = true;
        }
    }

    #endregion

    #region ThrowAtSight

    private void ThrowAtPlayer()
    {
        if (!_alreadyThrown)
        {
            _alreadyThrown = true;
            StopMoving();
            transform.LookAt(_player);
            _throwItem_S.StartThrow(_animator, _player.position);
        }
    }

    public void ResetThrow()
    {
        _alreadyThrown = false;
    }

    #endregion

    #region AttackPlayer

    private void AttackPlayer()
    {
        _agent.SetDestination(_player.position);
        _agent.speed = _runningSpeed;
        _agent.acceleration = 5;
        _animator.ResetTrigger(AnimationHandler.instance.animIDThrow);
        _animator.SetFloat(AnimationHandler.instance.animIDSpeed, 1.5f);
        _animator.SetFloat(AnimationHandler.instance.animIDMotionSpeed, 1);

        transform.LookAt(_player);
    }

    private void JumpAttackPlayer()
    {
        if (_alreadyAttacked) return;

        StopMoving();
        _animator.SetTrigger(AnimationHandler.instance.animIDAttack);
        _alreadyAttacked = true;
    }

    public void OnJumpAttack(AnimationEvent animationEvent)
    {
        // move player to the jump location
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            var playerPos = new Vector3(_player.position.x - 0.5f, _player.position.y, _player.position.z);
            float distance = Vector3.Distance(transform.position, playerPos);
            distance = Mathf.Clamp(distance, 0f, _jumpAttackRange);

            Vector3 targetPosition = playerPos - transform.position;
            targetPosition.Normalize();

            _newJumpPos = transform.position + (targetPosition * distance);

            _jumpAttack = true;
        }
    }

    public void OnJumpFinished(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            _jumpAttack = false;
            _alreadyAttacked = false;
        }
    }

    private void MoveEnemyOnJumpAttack()
    {
        if (_canMove)
            transform.position = Vector3.Lerp(transform.position, _newJumpPos, _jumpMoveSpeed * Time.deltaTime);
    }

    #endregion

    /*private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _sightRange);
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, _jumpAttackRange);
    }*/

    private void StopMoving(bool idle = true)
    {
        if (idle)
        {
            _animator.SetFloat(AnimationHandler.instance.animIDSpeed, 0);
            _animator.SetFloat(AnimationHandler.instance.animIDMotionSpeed, 0);
        }
        _agent.SetDestination(transform.position);
    }

    #region Collision Methods

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stone"))
        {
            _damageControl_S.StoneHit();
        }

        if (other.CompareTag("Player"))
        {
            _canMove = false;
            _jumpAttack = false;
            StopMoving();
        }
    }

    #endregion

    #region Death Methods

    private void EnemyDied()
    {
        StopMoving(true);
        _canMove = false;
        _jumpAttack = false;
    }

    public void EnemyDead(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
            UIController.instance.EnemyDead();
    }

    public void EnemyCrawl(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            UIController.instance.EnemyCrawl();
            SoundManager.instance.PlayAudio(_crawlClip, true);
            StopMoving(false);
            _canMove = false;
            _jumpAttack = false;
        }
    }

    #endregion
}
