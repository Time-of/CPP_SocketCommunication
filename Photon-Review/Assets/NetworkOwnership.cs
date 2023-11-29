using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;


/// <author>이성수</author>
/// <summary>
/// 네트워크 오너십 정보를 가진 컴포넌트 클래스.
/// id가 NetworkConnectionManager의 playerId와 동일하다면 bIsMine은 true, 아니라면 false로 간주.
/// </summary>
public class NetworkOwnership : Instantiatable
{
	public Player ownerPlayer;

	public int ownerIdForDebug = -1;

	public bool bIsMine { get { return ownerPlayer != null && ownerPlayer.id == NetworkConnectionManager.instance.playerId; } private set { } }

	private void SetOwner(Player NewOwner) { ownerPlayer = NewOwner; }


	public static Instantiatable RequestInstantiate(string resourceName, Vector3 position, Quaternion rotation, params object[] args)
	{
		//if ((int)args[0] == NetworkConnectionManager.instance.playerId)
		{
			NetworkConnectionManager.instance.SendObjectSpawnInfo(resourceName, position, rotation);
			Debug.Log("생성 명령 전송 완료!");
		}

		return _InternalInstantiate(resourceName, position, rotation, args);
	}

	
	public static Instantiatable _InternalInstantiate(string resourceName, Vector3 position, Quaternion rotation, params object[] args)
	{
		var prefab = Resources.Load<Instantiatable>(resourceName);

		if (prefab == null)
		{
			Debug.LogError("네트워크 스폰: 프리팹 리소스 [" + resourceName + "] 로드 실패! NetworkOwnership 클래스가 없는 것은 아닐까요?");
			return null;
		}

		Instantiatable ownershipObject = Instantiate<Instantiatable>(prefab, position, rotation);
		ownershipObject.OnCreated(args);

		return ownershipObject;
	}


	public override void OnCreated(params object[] args)
	{
		Player owner = null;
		if (NetworkConnectionManager.instance.playerMap.TryGetValue((int)args[0], out owner))
		{
			SetOwner(owner);
			ownerIdForDebug = owner.id;
			Debug.Log("NetworkOwnership OnCreated: 오너 설정 완료! " + owner);
		}
		else
		{
			Debug.LogWarning("네트워크 스폰: Owner가 존재하지 않거나 NetworkOwnership을 가지고 있지 않아요!");
		}
	}
}
