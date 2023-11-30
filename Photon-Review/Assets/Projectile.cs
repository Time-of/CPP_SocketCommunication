using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Photon.Pun;



public class Projectile : MonoBehaviour
{
	public float speed = 3.0f;

	public GameObject owner;


	private void Start()
	{
		Destroy(gameObject, 3.0f);
	}


	void Update()
	{
		RaycastHit hit;
		bool bHit = Physics.Linecast(transform.position,
			transform.position + transform.forward * speed * Time.deltaTime,
			out hit,
			1 << LayerMask.NameToLayer("Player"),
			QueryTriggerInteraction.Ignore
			);

		if (bHit && hit.collider.gameObject != owner)
		{
			PlayerController PC = hit.collider.gameObject.GetComponent<PlayerController>();

			if (PC != null)
			{
				PC.TakeDamage(3.0f);
				Destroy(gameObject);
			}
		}

		transform.position += (transform.forward * speed * Time.deltaTime);
	}
}
