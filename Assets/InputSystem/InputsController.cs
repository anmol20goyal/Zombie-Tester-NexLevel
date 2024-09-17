using UnityEngine;
using UnityEngine.InputSystem;

public class InputsController : MonoBehaviour
{
	public Vector2 move;
	public Vector2 look;
	public bool jump;
	public bool sprint;
	public bool throwItem;
	[SerializeField] private bool _canJump;

	#region Input Functions

	public void OnMove(InputValue value)
	{
		MoveInput(value.Get<Vector2>());
	}

	public void OnLook(InputValue value)
	{
		LookInput(value.Get<Vector2>());
	}

	public void OnJump(InputValue value)
	{
		if (!_canJump) return;
		JumpInput(value.isPressed);
	}

	public void OnSprint(InputValue value)
	{
		SprintInput(value.isPressed);
	}

	public void OnThrow(InputValue value)
	{
		ThrowItem(value.isPressed);
	}

	#endregion

	public void MoveInput(Vector2 newMoveDirection)
	{
		move = newMoveDirection;
	}

	public void LookInput(Vector2 newLookDirection)
	{
		look = newLookDirection;
	}

	public void JumpInput(bool newJumpState)
	{
		jump = newJumpState;
	}

	public void SprintInput(bool newSprintState)
	{
		sprint = newSprintState;
	}

	public void ThrowItem(bool newThrowState)
	{
		throwItem = newThrowState;
	}
}