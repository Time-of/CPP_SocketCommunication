using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Photon.Pun;
using TMPro;
using System.IO;

public class PlayerController : NetworkOwnership
{
	private float h = 0f;
	private float v = 0f;

	public float speed = 10.0f;
	public float rotationSpeedMult = 100.0f;
	private Animator animComp;

	public float aimYaw { get; private set; }
	public float aimPitch { get; private set; }

	private Vector3 velocity;

	public string nickname { get; set; }

	[SerializeField]
	private TMP_Text nicknameTextHUD;

	[SerializeField]
	private Projectile dagger;

	[SerializeField]
	private Transform attackPos;



	[Header("<��Ʈ��ŷ>")]
	[SerializeField]
	protected float NetworkingRotationInterpSpeed = 25.0f;

	protected Vector3 NetworkingPosition;

	protected Quaternion NetworkingRotation;

	protected float NetworkingDistance;


	private void Awake()
	{
		animComp = GetComponent<Animator>();
	}


	private void Start()
	{
		if (bIsMine)
		{
			Camera.main.GetComponent<FollowingCamera>().InitializeFollowCamera(this);
		}

		NetworkConnectionManager.instance.playerCharacterMap.Add(ownerPlayer.id, this);
		SetMyNickname(ownerPlayer.nickname);

		// SetMyNickname RPC
		//NetworkConnectionManager.instance.SendRPCToAll(owner.id, "SetMyNickname", NetworkConnectionManager.instance.localNickname);

		//if (photonView.IsMine)
		//photonView.RPC("SetMyNickname", RpcTarget.AllBuffered, NetworkConnectionManager.instance.localNickname);
	}


	public void UpdateNetworkingTransform(Vector3 pos, Quaternion rot)
	{
		NetworkingPosition = pos;
		NetworkingRotation = rot;
	}


	//[PunRPC]
	public void SetMyNickname(string localNickname)
	{
		nickname = localNickname;
		nicknameTextHUD.text = nickname;
	}


	private void Update()
	{
		if (bIsMine)
		{
			h = Input.GetAxis("Horizontal");
			v = Input.GetAxis("Vertical");
			aimYaw += Input.GetAxis("Mouse X");
			aimPitch += -Input.GetAxis("Mouse Y");
			aimPitch = Mathf.Clamp(aimPitch, -5.0f, 45.0f);

			velocity = new Vector3(h, 0.0f, v).normalized * Time.deltaTime * speed;
			transform.Translate(velocity);
			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				Quaternion.Euler(0.0f, aimYaw, 0.0f),
				Time.deltaTime * rotationSpeedMult
				);

			if (Input.GetMouseButtonDown(0)) PerformAttack();

			NetworkConnectionManager.instance.SendTransformExceptScale(transform);
		}
		else
		{
			PerformUpdateNotMineTransform();
		}

		animComp.SetFloat("Speed", velocity.magnitude);
	}


	/*
	void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
	{
		if (stream.IsWriting)
		{
			stream.SendNext(transform.position);

			stream.SendNext(transform.rotation.eulerAngles.y);

			stream.SendNext(velocity);
		}
		else
		{
			NetworkingPosition = (Vector3)stream.ReceiveNext();

			NetworkingRotation = Quaternion.Euler(0.0f, (float)stream.ReceiveNext(), 0.0f);

			velocity = (Vector3)stream.ReceiveNext();

			float NetworkingLag = Mathf.Abs((float)(PhotonNetwork.Time - info.SentServerTime));

			NetworkingPosition += velocity * NetworkingLag;

			NetworkingDistance = Vector3.Distance(transform.position, NetworkingPosition);
		}
	}
	*/


	void PerformUpdateNotMineTransform()
	{
		//transform.position = Vector3.MoveTowards(transform.position, NetworkingPosition,
		//	NetworkingDistance * (1.0f / PhotonNetwork.SerializationRate));
		Vector3 oldPos = transform.position;

		transform.position = Vector3.Lerp(transform.position, NetworkingPosition,
			20.0f * Time.deltaTime);
		transform.rotation = Quaternion.Slerp(transform.rotation, NetworkingRotation,
			NetworkingRotationInterpSpeed * Time.fixedDeltaTime);

		velocity = transform.position - oldPos;
	}


	private void PerformAttack()
	{
		//photonView.RPC("RPCPerformAttackAll", RpcTarget.All);
		NetworkConnectionManager.instance.SendRPCToAll(ownerPlayer.id, "RPCPerformAttackAll");
	}


	//[PunRPC]
	public void RPCPerformAttackAll()
	{
		animComp.SetTrigger("PerformAttack");

		// @todo: ������Ÿ�� ����
		Projectile proj = Instantiate(dagger, attackPos.position, attackPos.rotation);
		proj.owner = gameObject;
		proj.speed = 3.0f;
	}


	public void TakeDamage()
	{
		animComp.SetTrigger("Damaged");
	}
}
