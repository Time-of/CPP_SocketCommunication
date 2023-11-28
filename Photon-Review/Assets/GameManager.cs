//using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	private void Awake()
	{
		//SceneManager.sceneLoaded += OnSceneLoaded;
	}


	private void Start()
	{
		NetworkConnectionManager.instance.OnJoinSuccessedDelegate += SpawnPlayer;
	}


	void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
	{
		//if (!PhotonNetwork.IsConnected) return;

		SpawnPlayer();
	}



	public void SpawnPlayer()
	{
		Debug.Log("���� �Ŵ������� �÷��̾� ������ �õ��մϴ�...");
		StartCoroutine(SpawnPlayerCoroutine());
	}



	private IEnumerator SpawnPlayerCoroutine()
	{
		Debug.Log("���� �Ŵ������� �÷��̾� ���� ����");
		yield return new WaitForSeconds(1.0f);

		//PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);

		var result = NetworkOwnership.Instantiate("Player", Vector3.zero, Quaternion.identity);
		
		Debug.Log("�÷��̾� ���� ���: " + (result != null));
	}
}
