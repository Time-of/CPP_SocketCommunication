using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using CVSP;
using UnityEngine.UIElements;
using UnityEngine.UI;

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
	#endregion


	private void Awake()
	{
		if (instance == null) instance = this;
		socketConnector = GetComponent<SocketConnector>();
		//PhotonNetwork.AutomaticallySyncScene = true;

		chattingScrollBox.gameObject.SetActive(false);
	}


	private void Start()
	{
		//PhotonNetwork.GameVersion = "0.01";
		//PhotonNetwork.NickName = "MyName";

		//PhotonNetwork.ConnectUsingSettings();
	}


	public void OnConnectedToMaster()
	{
		Debug.Log("Connected to Master");

	}


	public bool ConfirmNicknameAndJoinRandomRoom(string newNickname)
	{
		//if (!PhotonNetwork.IsConnected) return false;

		localNickname = newNickname;
		//PhotonNetwork.JoinRandomRoom();

		// �켱�� ���⼭ ������ �����ϴ� ������ ����
		bool result = socketConnector.ConnectToServer("127.0.0.1");
		StartCoroutine(PeekChattingMessagesCoroutine());

		chattingScrollBox.gameObject.SetActive(result);

		return result;
	}


	public void OnJoinedRoom()
	{
		//Debug.Log("<color=yellow>�� ���� ����. �� �̸�:</color> " + PhotonNetwork.CurrentRoom.Name);
		Debug.Log("<color=yellow>�� ���� ����.</color>");

		gameManager.SpawnPlayer();
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
		Debug.Log("<color=yellow>���� �����߽��ϴ�.");
	}


	#region ä�� ���
	public void SendChat(string message)
	{
		if (!socketConnector.bIsConnected)
		{
			//Debug.LogWarning("playerPhotonViewRef�� null�Դϴ�!!");
			Debug.LogWarning("����� ������� ����!!");
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

				// ���� ��� ���, �̰� RPC�� �ƴ�??
				StartCoroutine(RPCSendChatAll(receivedMessage));
			}

			yield return chattingPeekDelay;
		}

		Debug.Log("ä�� �޽��� ��ŷ ����!");
	}
	#endregion
}
