using UnityEngine;

public class AnimationHandler : MonoBehaviour
{
    public static AnimationHandler instance;

    #region Animation IDs

    [HideInInspector] public int animIDSpeed;
    [HideInInspector] public int animIDGrounded;
    [HideInInspector] public int animIDJump;
    [HideInInspector] public int animIDThrow;
    [HideInInspector] public int animIDDead;
    [HideInInspector] public int animIDFreeFall;
    [HideInInspector] public int animIDMotionSpeed;
    [HideInInspector] public int animIDAttack;

    #endregion

    private void Awake()
    {
        if (instance == null) 
            instance = this;
        else
            Destroy(this);

        AssignAnimationIDs();
    }


    private void AssignAnimationIDs()
    {
        animIDSpeed = Animator.StringToHash("Speed");
        animIDGrounded = Animator.StringToHash("Grounded");
        animIDJump = Animator.StringToHash("Jump");
        animIDThrow = Animator.StringToHash("Throw");
        animIDDead = Animator.StringToHash("Dead");
        animIDFreeFall = Animator.StringToHash("FreeFall");
        animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        animIDAttack = Animator.StringToHash("Attack");
    }
}
