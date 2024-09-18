using UnityEngine;

public class ThrowItem : MonoBehaviour
{
    [SerializeField] private Transform _throwInitialPoint;
    [SerializeField] private GameObject _stonePrefab;

    private Vector3 _targetPos;

    public void StartThrow(Animator animator, Vector3 targetPos)
    {
        _targetPos = targetPos;
        animator.SetTrigger(AnimationHandler.instance.animIDThrow);
    }

    public void ThrowItemProjectile(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            var stonePos = _throwInitialPoint.transform.position;
            var stone = Instantiate(_stonePrefab, stonePos, Quaternion.Euler(Vector3.zero));
            stone.GetComponent<Projectile>().Throw(_targetPos);
            // rest is handled in projectile script on every stone
        }
    }
}