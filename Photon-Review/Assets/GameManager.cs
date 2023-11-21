using Photon.Pun;
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



	void OnSceneLoaded(Scene loadedScene, LoadSceneMode mode)
	{
		if (!PhotonNetwork.IsConnected) return;

		SpawnPlayer();
	}



	public void SpawnPlayer()
	{
		StartCoroutine(SpawnPlayerCoroutine());
	}



	private IEnumerator SpawnPlayerCoroutine()
	{
		yield return new WaitForSeconds(1.0f);

		PhotonNetwork.Instantiate("Player", Vector3.zero, Quaternion.identity);
	}
}
