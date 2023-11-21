using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowingCamera : MonoBehaviour
{
	private PlayerController controller;

	[SerializeField]
	private float cameraLagSpeed = 10.0f;

	[SerializeField]
	private float cameraRotationLagSpeed = 10.0f;

	[SerializeField]
	private Vector3 cameraLocalPosition = new Vector3(0.0f, 1.0f, -4.0f);



	public void InitializeFollowCamera(PlayerController newController)
	{
		controller = newController;
	}



	private void LateUpdate()
	{
		if (controller == null) return;

		transform.rotation = Quaternion.Slerp(
			transform.rotation,
			Quaternion.Euler(controller.aimPitch, controller.aimYaw, 0.0f),
			cameraRotationLagSpeed * Time.deltaTime);

		transform.position = Vector3.Lerp(
			transform.position,
			controller.transform.position + transform.rotation * cameraLocalPosition,
			cameraLagSpeed * Time.deltaTime);
	}
}
