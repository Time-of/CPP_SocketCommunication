using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CVSP;
using UnityEngine.UIElements;
using UnityEngine.UI;
using System;
using UnityEngine.Events;


[RequireComponent(typeof(SocketConnector))]
public class NetworkConnectionManager : MonoBehaviour
{
	[SerializeField]
	private string TestRoomName = "HELLOWORLD";

	[SerializeField]
	private GameManager gameManager;

	public string localNickname { get; private set; }

	public static NetworkConnectionManager instance;

	#region ä��
	[SerializeField]
	private TMP_Text chattingBox;

	[SerializeField]
	private ScrollRect chattingScrollBox;

	public Queue<string> chattingQueue = new();

	private WaitForSeconds chattingPeekDelay = new(0.1f);
	#endregion


	#region ���� ���
	public SocketConnector socketConnector { get; private set; }

	public Queue<Action> actionQueue = new();
	public Queue<RPCInfo> rpcQueue = new();

	public Queue<TransformInfo> transformInfoQueue = new();

	public UnityAction OnJoinSuccessedDelegate;

	public int playerId { get; private set; }

	// id, �÷��̾� ��ųʸ�
	public Dictionary<int, PlayerController> playerMap = new();
	#endregion


	private void Awake()
	{
		if (instance == null)
		{
			instance = this;
			DontDestroyOnLoad(gameObject);
		}

		socketConnector = GetComponent<SocketConnector>();
		//PhotonNetwork.AutomaticallySyncScene = true;

		chattingScrollBox.gameObject.SetActive(false);

		playerId = -1;
	}


	private void OnDestroy()
	{
		instance = null;
	}


	private void Start()
	{
		//PhotonNetwork.GameVersion = "0.01";
		//PhotonNetwork.NickName = "MyName";

		//PhotonNetwork.ConnectUsingSettings();
	}


	private void Update()
	{
		while (actionQueue.Count > 0)
		{
			actionQueue.Dequeue().Invoke();
		}

		while (rpcQueue.Count > 0)
		{
			var info = rpcQueue.Dequeue();
			PlayerController pc = null;

			if (playerMap.TryGetValue(info.ownerId, out pc))
			{
				var method = pc.GetType().GetMethod(info.functionName);

				var deserializedParams = socketConnector.DeserializeObjects(info.rpcParams, info.rpcParamTypes);
				//if (deserializedParams != null)
				//{
				//	Debug.Log("������ȭ ���: " + deserializedParams[0]);
				//}
				method.Invoke(pc, deserializedParams);
			}
		}

		while (transformInfoQueue.Count > 0)
		{
			var info = transformInfoQueue.Dequeue();
			PlayerController pc = null;

			if (playerMap.TryGetValue(info.ownerId, out pc))
			{
				pc.UpdateNetworkingTransform(new Vector3(info.posX, info.posY, info.posZ), new Quaternion(info.quatX, info.quatY, info.quatZ, info.quatW));
			}
		}
	}


	public void OnConnectedToMaster()
	{
		Debug.Log("Connected to Master");
		
	}


	public bool ConfirmNicknameAndJoinRandomRoom(string newNickname)
	{
		//if (!PhotonNetwork.IsConnected) return false;

		//PhotonNetwork.JoinRandomRoom();

		// �켱�� ���⼭ ������ �����ϴ� ������ ����
		bool result = socketConnector.ConnectToServer("127.0.0.1");
		if (!result) return false;

		chattingScrollBox.gameObject.SetActive(false);

		OnConnectedToMaster();

		localNickname = newNickname;

		StartCoroutine(PeekChattingMessagesCoroutine());

		Debug.Log("<color=yellow>���� ����. Join ��û ����.</color>");
		socketConnector.SendWithPayload(SpecificationCVSP.CVSP_JOINREQ, SpecificationCVSP.CVSP_SUCCESS, localNickname);

		return result;
	}


	public void OnJoinSuccessed(int NewPlayerId)
	{
		playerId = NewPlayerId;
		Debug.Log("Join ����. id: " + playerId);

		actionQueue.Enqueue(() => OnJoinSuccessedDelegate.Invoke());
		actionQueue.Enqueue(() => chattingScrollBox.gameObject.SetActive(true));
	}


	public void OnJoinRoomFailed(short returnCode, string message)
	{
		//PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.Log("<color=orange>���� �� ���� ����!</color>");

		//PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public void OnCreatedRoom()
	{
		//Debug.Log("<color=yellow>���� �����߽��ϴ�. �� �̸�:</color> " + PhotonNetwork.CurrentRoom.Name);
		Debug.Log("<color=yellow>���� �����߽��ϴ�.</color>");
	}


	#region ä�� ���
	public void SendChat(string message)
	{
		if (!socketConnector.bIsConnected)
		{
			Debug.LogWarning("ä��: ������ ������� ����!!");
			return;
		}
		else if (!socketConnector.bIsJoinned)
		{
			Debug.LogWarning("ä��: Join ���� �ʾ���!!");
			return;
		}

		string chattingText = localNickname + ": " + message + "\n";

		//photonView.RPC("RPCSendChatAll", RpcTarget.All, chattingText);
		// ������ ������
		socketConnector.SendWithPayload(SpecificationCVSP.CVSP_CHATTINGREQ, SpecificationCVSP.CVSP_SUCCESS, chattingText);
	}


	//[PunRPC]
	public IEnumerator RPCSendChatAll(string message)
	{
		chattingBox.text += message;
		yield return null;
		chattingScrollBox.verticalNormalizedPosition = 0.0f;
	}


	private IEnumerator PeekChattingMessagesCoroutine()
	{
		Debug.Log("ä�� �޽��� ��ŷ ����!");

		while (socketConnector.bIsConnected)
		{
			if (chattingQueue.Count > 0)
			{
				string receivedMessage = chattingQueue.Dequeue();

				// ���� ��� ���, �̰� RPC�� �ƴ�.
				StartCoroutine(RPCSendChatAll(receivedMessage));
			}

			yield return chattingPeekDelay;
		}

		Debug.Log("ä�� �޽��� ��ŷ ����!");
	}
	#endregion


	#region ������Ʈ ���� ���
	public void SendObjectSpawnInfo(string resourceName, Vector3 position, Quaternion rotation)
	{
		socketConnector.SendObjectSpawnInfo(resourceName, position, rotation, playerId);
		Debug.Log("���� Ŀ���Ϳ� ������Ʈ ���� ���� ���� �Ϸ�!");
	}

	
	public void AddObjectSpawnInfoToActionQueue(string resourceName, Vector3 position, Quaternion rotation, int ownerId)
	{
		actionQueue.Enqueue(() => NetworkOwnership._InternalInstantiate(resourceName, position, rotation, ownerId));
	}
	#endregion


	#region Ʈ������
	public void SendTransformExceptScale(Transform tr)
	{
		TransformInfo info = new()
		{
			posX = tr.position.x,
			posY = tr.position.y,
			posZ = tr.position.z,
			quatX = tr.rotation.x,
			quatY = tr.rotation.y,
			quatZ = tr.rotation.z,
			quatW = tr.rotation.w,
			ownerId = playerId
		};

		socketConnector.SendWithPayload(SpecificationCVSP.CVSP_OPERATIONREQ, SpecificationCVSP.CVSP_SUCCESS, info);
	}

	
	public void EnqueueTransformInfo(TransformInfo info)
	{
		transformInfoQueue.Enqueue(info);
	}
	#endregion


	#region RPC
	// �������� ����� �ƴ� �� ���� (������ ������� ����)
	// ���� ����� �÷��̾� ĳ���Ϳ����� �����
	public void SendRPCToAll(int id, string funcName, params object[] param)
	{
		socketConnector.SendRPCToAll(id, funcName, param);
	}
	#endregion
}
