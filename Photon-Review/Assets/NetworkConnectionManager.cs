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

	#region 채팅
	[SerializeField]
	private TMP_Text chattingBox;

	[SerializeField]
	private ScrollRect chattingScrollBox;

	public Queue<string> chattingQueue = new();

	private WaitForSeconds chattingPeekDelay = new(0.1f);
	#endregion


	#region 소켓 통신
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

		// 우선은 여기서 서버에 연결하는 것으로 구현
		bool result = socketConnector.ConnectToServer("127.0.0.1");
		if (!result) return false;

		chattingScrollBox.gameObject.SetActive(false);

		OnConnectedToMaster();

		localNickname = newNickname;

		StartCoroutine(PeekChattingMessagesCoroutine());

		Debug.Log("<color=yellow>연결 성공. Join 요청 전송.</color>");
		socketConnector.SendWithPayload(SpecificationCVSP.CVSP_JOINREQ, SpecificationCVSP.CVSP_SUCCESS, localNickname);

		return result;
	}


	public void OnJoinSuccessed()
	{
		Debug.Log("Join 성공.");

		actionQueue.Enqueue(() => OnJoinSuccessedDelegate.Invoke());
		actionQueue.Enqueue(() => chattingScrollBox.gameObject.SetActive(true));
	}


	public void OnJoinRoomFailed(short returnCode, string message)
	{
		//PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public void OnJoinRandomFailed(short returnCode, string message)
	{
		Debug.Log("<color=orange>랜덤 방 입장 실패!</color>");

		//PhotonNetwork.CreateRoom(TestRoomName, new RoomOptions { MaxPlayers = 4 });
	}


	public void OnCreatedRoom()
	{
		//Debug.Log("<color=yellow>방을 생성했습니다. 방 이름:</color> " + PhotonNetwork.CurrentRoom.Name);
		Debug.Log("<color=yellow>방을 생성했습니다.</color>");
	}


	#region 채팅 기능
	public void SendChat(string message)
	{
		if (!socketConnector.bIsConnected)
		{
			Debug.LogWarning("서버에 연결되지 않음!!");
			return;
		}
		else if (!socketConnector.bIsJoinned)
		{
			Debug.LogWarning("Join 되지 않았음!!");
			return;
		}

		string chattingText = localNickname + ": " + message + "\n";

		//photonView.RPC("RPCSendChatAll", RpcTarget.All, chattingText);
		// 서버에 보내기
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
		Debug.Log("채팅 메시지 피킹 시작!");

		while (socketConnector.bIsConnected)
		{
			if (chattingQueue.Count > 0)
			{
				string receivedMessage = chattingQueue.Dequeue();

				// 기존 기능 사용, 이건 RPC는 아님.
				StartCoroutine(RPCSendChatAll(receivedMessage));
			}

			yield return chattingPeekDelay;
		}

		Debug.Log("채팅 메시지 피킹 종료!");
	}
	#endregion
}
