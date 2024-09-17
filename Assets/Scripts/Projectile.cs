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

    private void Start()
    {
        _mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        var direc = _mousePos - transform.position;
        _rb.velocity = -direc.normalized * _launchVel;
    }

    private void Update()
    {
        Destroy(gameObject, _destroyAfterSec);
    }
}