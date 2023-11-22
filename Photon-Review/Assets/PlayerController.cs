using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using Photon.Pun;
using TMPro;

public class PlayerController : MonoBehaviour
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



	[Header("<네트워킹>")]

	[SerializeField]
	protected float NetworkingRotationInterpSpeed = 25.0f;

	protected Vector3 NetworkingPosition;

	protected Quaternion NetworkingRotation;

	protected float NetworkingDistance;


	private void Awake()
	{
		animComp = GetComponent<Animator>();

		// @todo: 내꺼에서만 작동하게 만들기
		//if (photonView.IsMine)
		{
			Camera.main.GetComponent<FollowingCamera>().InitializeFollowCamera(this);
		}
	}


	private void Start()
	{
		//if (photonView.IsMine)
			//photonView.RPC("SetMyNickname", RpcTarget.AllBuffered, NetworkConnectionManager.instance.localNickname);
	}


	//[PunRPC]
	private void SetMyNickname(string localNickname)
	{
		nickname = localNickname;
		nicknameTextHUD.text = nickname;
	}


	private void Update()
	{
		//if (photonView.IsMine)
		if (true)
		{
			h = Input.GetAxis("Horizontal");
			v = Input.GetAxis("Vertical");
			aimYaw += Input.GetAxis("Mouse X");
			aimPitch += -Input.GetAxis("Mouse Y");
			aimPitch = Mathf.Clamp(aimPitch, -60.0f, 60.0f);

			velocity = new Vector3(h, 0.0f, v).normalized * Time.deltaTime * speed;
			transform.Translate(velocity);
			transform.rotation = Quaternion.Slerp(
				transform.rotation,
				Quaternion.Euler(0.0f, aimYaw, 0.0f),
				Time.deltaTime * rotationSpeedMult
				);

			if (Input.GetMouseButtonDown(0)) PerformAttack();
		}
		else
		{
			UpdateNetworkingTransform();
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


	void UpdateNetworkingTransform()
	{
		/*
		if (!photonView.IsMine)
		{
			transform.position = Vector3.MoveTowards(transform.position, NetworkingPosition,
				NetworkingDistance * (1.0f / PhotonNetwork.SerializationRate));

			transform.rotation = Quaternion.Slerp(transform.rotation, NetworkingRotation,
				NetworkingRotationInterpSpeed * Time.fixedDeltaTime);
		}
		*/
	}


	private void PerformAttack()
	{
		//photonView.RPC("RPCPerformAttackAll", RpcTarget.All);
	}


	//[PunRPC]
	public void RPCPerformAttackAll()
	{
		animComp.SetTrigger("PerformAttack");

		// @todo: 프로젝타일 수정
		Projectile proj = Instantiate(dagger, attackPos.position, attackPos.rotation);
		proj.owner = gameObject;
		proj.speed = 3.0f;
	}


	public void TakeDamage()
	{
		animComp.SetTrigger("Damaged");
	}
}
