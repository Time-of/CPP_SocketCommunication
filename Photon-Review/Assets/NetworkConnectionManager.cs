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

	public UnityAction OnJoinSuccessedDelegate;
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


	public void OnJoinSuccessed()
	{
		Debug.Log("Join ����.");

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
			Debug.LogWarning("������ ������� ����!!");
			return;
		}
		else if (!socketConnector.bIsJoinned)
		{
			Debug.LogWarning("Join ���� �ʾ���!!");
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
}
