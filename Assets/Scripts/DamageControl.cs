using System;
using UnityEngine;

public class DamageControl : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    [SerializeField] private int _maxHealth;
    [SerializeField] private int _stoneDamage;
    [HideInInspector] public int currentHealth;

    private void Start()
    {
        currentHealth = _maxHealth;
    }

    public void StoneHit()
    {
        currentHealth -= _stoneDamage;
        if (currentHealth <= 0)
            GameOver();
    }

    public void EnemyFinalAttack()
    {
        currentHealth = 0;
        GameOver();
    }

    private void GameOver()
    {
        _animator.SetTrigger(AnimationHandler.instance.animIDDead);
    }
}