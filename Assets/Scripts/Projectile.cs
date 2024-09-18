using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] private float _launchVel;
    [SerializeField] private float _destroyAfterSec;

    private Vector3 _mousePos;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void Throw(Vector3 targetPos)
    {
        var direc = targetPos - transform.position;
        _rb.velocity = direc.normalized * _launchVel;
    }

    private void Update()
    {
        Destroy(this.gameObject, _destroyAfterSec);
    }
}