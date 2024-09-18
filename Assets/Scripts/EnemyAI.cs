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

    //Attacking
    [SerializeField] private GameObject _projectile;
    [SerializeField] private ThrowItem _throwItem_S;

    #endregion

    #region Variables

    [Header("*****Variables*****")]
    [SerializeField] private bool _canMove;
    [SerializeField] private bool _jumpAttack;
    [SerializeField] private Vector3 _newJumpPos;
    [SerializeField] private float _runningSpeed = 5.335f;
    [SerializeField] private float _jumpMoveSpeed = 1f;
    private bool _alreadyAttacked;
    
    //States
    [SerializeField] private float _sightRange;
    [SerializeField] private float _attackRange;
    [SerializeField] private float _jumpAttackRange = 2f;
    [SerializeField] private bool _playerInSightRange;
    [SerializeField] private bool _playerInAttackRange;
    [SerializeField] private bool _playerInJumpAttackRange;
    [SerializeField] private float _timeBetweenAttacks;
    [SerializeField] private float _health;

    //Patroling
    [SerializeField] private Vector3 _walkPoint;
    [SerializeField] private float _walkPointRange;
    private bool _walkPointSet;

    #endregion

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Update()
    {
        if (_jumpAttack)
        {
            MoveEnemyOnJumpAttack();
        }

        if (!_canMove) return;

        //Check for sight and attack range
        _playerInSightRange = Physics.CheckSphere(transform.position, _sightRange, _whatIsPlayer);
        _playerInAttackRange = Physics.CheckSphere(transform.position, _attackRange, _whatIsPlayer);
        _playerInJumpAttackRange = Physics.CheckSphere(transform.position, _jumpAttackRange, _whatIsPlayer);

        if (!_playerInSightRange && !_playerInAttackRange) Patroling();
        if (_playerInSightRange && !_playerInAttackRange) ThrowAtPlayer(); // throw items
        if (_playerInAttackRange && _playerInSightRange) AttackPlayer(); // run and jump attack
    }

    #region Patroling

    private void Patroling()
    {
        if (!_walkPointSet) SearchWalkPoint();

        if (_walkPointSet)
        {
            _agent.SetDestination(_walkPoint);
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
        StopMoving();
        transform.LookAt(_player);
        _animator.SetTrigger(AnimationHandler.instance.animIDThrow);
        _throwItem_S.StartThrow(_animator, _player.position);
    }

    #endregion

    #region AttackPlayer

    private void AttackPlayer()
    {
        _agent.speed = _runningSpeed;
        _animator.SetFloat(AnimationHandler.instance.animIDSpeed, 1.5f);
        _animator.SetFloat(AnimationHandler.instance.animIDMotionSpeed, 1);

        transform.LookAt(_player);

        if (!_alreadyAttacked && _playerInJumpAttackRange)
        {
            // check if the player is within 2m radius....then start the jump attack animation
            _animator.SetTrigger(AnimationHandler.instance.animIDAttack);
            _alreadyAttacked = true;
            Invoke(nameof(ResetAttack), _timeBetweenAttacks);
        }
    }

    private void ResetAttack()
    {
        _alreadyAttacked = false;
    }

    public void OnJumpAttack(AnimationEvent animationEvent)
    {
        // move player to the jump location
        // position of a point on a circle is (a+rCos(angle), b+rSin(angle))
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            var center = new Vector2(transform.position.x, transform.position.z);
            var radius = 2f;
            var angle = transform.rotation.eulerAngles.y - 90;

            var rCos = radius * Mathf.Cos(angle);
            var rSin = radius * Mathf.Sin(angle);

            _newJumpPos = new Vector3(center.x + rCos, transform.position.y, center.y + rSin);
            _jumpAttack = true;
        }
    }

    public void OnJumpFinished(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            _jumpAttack = false;
        }
    }

    private void MoveEnemyOnJumpAttack()
    {
        transform.position = Vector3.Lerp(transform.position, _newJumpPos, _jumpMoveSpeed * Time.deltaTime);
    }

    #endregion

   

    public void TakeDamage(int damage)
    {
        _health -= damage;

        if (_health <= 0) Invoke(nameof(DestroyEnemy), 0.5f);
    }

    private void DestroyEnemy()
    {
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _sightRange);
    }


    private void StopMoving(bool idle = true)
    {
        if (idle)
        {
            _animator.SetFloat(AnimationHandler.instance.animIDSpeed, 0);
            _animator.SetFloat(AnimationHandler.instance.animIDMotionSpeed, 0);
        }
        _agent.SetDestination(transform.position);
    }
}
