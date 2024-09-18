using UnityEngine;
using UnityEngine.InputSystem;

public class ThrowItem : MonoBehaviour
{
    [SerializeField] private Transform _throwInitialPoint;
    [SerializeField] private GameObject _stonePrefab;

    private bool _isPlayerThrow;
    private Vector3 _targetPos;

    private void Awake()
    {
        _isPlayerThrow = false;
    }

    public void StartThrow(Animator animator, Vector3? targetPos)
    {
        _isPlayerThrow = !targetPos.HasValue;
        if (targetPos.HasValue)
            _targetPos = targetPos.Value;
        animator.SetTrigger(AnimationHandler.instance.animIDThrow);
    }

    public void ThrowItemProjectile(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            var stonePos = _throwInitialPoint.transform.position;
            var stone = Instantiate(_stonePrefab, stonePos, Quaternion.Euler(Vector3.zero), _throwInitialPoint.transform);
            // rest is handled in projectile script on every stone

            var target = _isPlayerThrow ? GetMousePos() : _targetPos;
            stone.GetComponent<Projectile>().Throw(target);
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
}