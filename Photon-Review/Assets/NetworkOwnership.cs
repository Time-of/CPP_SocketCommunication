using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Entities;
using UnityEngine;


/// <author>이성수</author>
/// <summary>
/// 네트워크 오너십 정보를 가진 컴포넌트 클래스.
/// id가 NetworkConnectionManager의 playerId와 동일하다면 bIsMine은 true, 아니라면 false로 간주.
/// </summary>
public class NetworkOwnership : MonoBehaviour
{
	public int id = -1;
	public bool bIsMine { get { return id == NetworkConnectionManager.instance.playerId; } private set { } }


	public static NetworkOwnership Instantiate(string resourceName, Vector3 position, Quaternion rotation)
	{
		Debug.Log("NetworkOwnership Instantiate 시도!");
		NetworkConnectionManager.instance.SendObjectSpawnInfo(resourceName, position, rotation);
		Debug.Log("NetworkOwnership 생성 명령 전송 완료!");

		// 본인만 바로 생성, 다른 사람들은 메시지를 받아 생성. (서버 쪽에서 날린 본인에게는 생성 명령을 주지 않음)
		// 그래서 타인에게는 [네트워크 딜레이, 큐에 밀린 작업 수, 리소스 Load] 만큼의 오버헤드가 생긴다.
		// @todo 나중에 개선 가능하다면 개선해보기.
		return _InternalInstantiate(resourceName, position, rotation, NetworkConnectionManager.instance.playerId);
	}

	
	public static NetworkOwnership _InternalInstantiate(string resourceName, Vector3 position, Quaternion rotation, int ownerId)
	{
		var prefab = Resources.Load<NetworkOwnership>(resourceName);

		if (prefab == null)
		{
			Debug.LogError("네트워크 스폰: 프리팹 리소스 [" + resourceName + "] 로드 실패! NetworkOwnership 클래스가 없는 것은 아닐까요?");
			return null;
		}

		NetworkOwnership ownershipObject = GameObject.Instantiate<NetworkOwnership>(prefab, position, rotation);

		ownershipObject.id = ownerId;

		return ownershipObject;
	}
}
