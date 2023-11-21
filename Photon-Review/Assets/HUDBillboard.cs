using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDBillboard : MonoBehaviour
{
	void LateUpdate()
	{
		transform.rotation = 
			Quaternion.LookRotation(Camera.main.transform.position - transform.position, Vector3.up);
	}
}
