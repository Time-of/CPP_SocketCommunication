using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Collections;
using System.Net;
using System.Threading;
using UnityEngine;
using CVSP;
using System.Security.Cryptography;
using Unity.Assertions;

[RequireComponent(typeof(NetworkConnectionManager))]
public class SocketConnector : MonoBehaviour
{
	private Socket socket;
	private byte[] readBuffer = new byte[SpecificationCVSP.CVSP_BUFFERSIZE];
	private BinaryFormatter formatter;
	private bool bIsNewUser;
	private ArrayList transBuffer;

	public bool bIsConnected { get; private set; }

	public bool bIsJoinned { get; private set; }


	public unsafe object ByteToStructure(Byte[] data, Type type)
	{
		if (data == null)
		{
			Debug.Log("ByteToStructure: data가 유효하지 않음!!!");
			return null;
		}

		// 언매니지드 영역에 메모리 할당 (배열 크기만큼)
		IntPtr buffer = Marshal.AllocHGlobal(data.Length);

		Marshal.Copy(data, 0, buffer, data.Length);

		object obj = Marshal.PtrToStructure(buffer, type);

		Marshal.FreeHGlobal(buffer);

		return (Marshal.SizeOf(obj) == data.Length) ? obj : null;
	}


	public int ByteToInt(Byte[] data)
	{
		if (data == null)
		{
			Debug.Log("ByteToInt: data가 유효하지 않음!!!");
			return -2147483648;
		}

		return BitConverter.ToInt32(data);
	}


	public unsafe Byte[] StructureToByte(object obj)
	{
		int dataSize = Marshal.SizeOf(obj);

		IntPtr buffer = Marshal.AllocHGlobal(dataSize);

		Marshal.StructureToPtr(obj, buffer, false);

		Byte[] data = new byte[dataSize];

		Marshal.Copy(buffer, data, 0, dataSize);

		Marshal.FreeHGlobal(buffer);

		return data;
	}


	public SocketConnector()
	{
		socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
		bIsNewUser = false;
		transBuffer = new ArrayList();
	}


	public bool ConnectToServer(string ipAddressString, int port)
	{
		if (socket.Connected)
		{
			return true;
		}

		try
		{
			IPAddress parsedIP = IPAddress.Parse(ipAddressString);
			socket.Connect(new IPEndPoint(parsedIP, port));

			// EUC-KR 인코딩을 위해 싱글톤 인스턴스 등록
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
		}
		catch (Exception e)
		{
			Debug.Log("서버 연결 실패! : " + e.Message);
			return false;
		}

		if (socket.Connected)
		{
			Thread recvThread = new Thread(ReceiveThread);
			recvThread.Start();
			bIsConnected = true;
			return true;
		}
		else return false;
	}


	public bool ConnectToServer(string ipAddressString)
	{
		return ConnectToServer(ipAddressString, SpecificationCVSP.ServerPort);
	}


	public void OnDestroy()
	{
		if (bIsConnected)
		{
			SendEndConnectionMessage();
		}
	}


	public void Stop()
	{
		lock (socket)
		{
			socket.Close();
		}

		bIsConnected = false;
	}


	public void SendEndConnectionMessage()
	{
		Send(SpecificationCVSP.CVSP_LEAVEREQ, SpecificationCVSP.CVSP_SUCCESS);
		Stop();
	}


	public int Send(byte cmd, byte option)
	{
		CVSPHeader header = new CVSPHeader();
		header.cmd = cmd;
		header.option = option;
		header.packetLength = (ushort)(4); // 하드코딩, CVSP 구조체의 크기.

		byte[] buffer = new Byte[header.packetLength];
		StructureToByte(header).CopyTo(buffer, 0);

		return socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
	}


	public int SendWithPayload(byte cmd, byte option, object payload)
	{
		byte[] payloadByte = StructureToByte(payload);

		return _InternalSendWithPayloadByte(cmd, option, payloadByte);
	}


	public int SendWithPayload(byte cmd, byte option, byte[] payloadByte)
	{
		return _InternalSendWithPayloadByte(cmd, option, payloadByte);
	}


	public int SendWithPayload(byte cmd, byte option, string message)
	{
		if (message.Length == 0) return 0;

		//byte[] payloadByte = Encoding.ASCII.GetBytes(message);

		byte[] payloadByte = GetEucKrEncoding().GetBytes(message); //Encoding.GetEncoding("euc-kr").GetBytes(message);

		return _InternalSendWithPayloadByte(cmd, option, payloadByte);
	}


	public int SendObjectSpawnInfo(string resourceName, Vector3 position, Quaternion rotation, int owner_Id)
	{
		Debug.Log("오브젝트 스폰 정보 취합 시작...");
		ObjectSpawnInfo info = new()
		{
			posX = position.x,
			posY = position.y,
			posZ = position.z,
			quatX = rotation.x,
			quatY = rotation.y,
			quatZ = rotation.z,
			quatW = rotation.w,

			objectName = resourceName,
			ownerId = owner_Id
		};

		return SendWithPayload(SpecificationCVSP.CVSP_SPAWN_OBJECT_REQ, SpecificationCVSP.CVSP_SUCCESS, info);
	}


	// 현재는 void 타입에, 파라미터 없는 경우만 지원
	public int SendRPCToAll(int id, string funcName)
	{
		RPCInfoNoParam info = new() { ownerId = id, functionName = funcName };
		return SendWithPayload(SpecificationCVSP.CVSP_RPC_NOPARAM_REQ, SpecificationCVSP.CVSP_RPCTARGET_ALL, info);
	}


	// 서버를 거쳐서 서버가 모두에게 뿌림
	public int SendRPCToAll(int id, string funcName, params object[] parameters)
	{
		if (parameters.Length > 0)
		{
			var params_types = SerializeObjects(parameters);
			RPCInfo info = new() { ownerId = id, functionName = funcName, rpcParams = params_types.Item1, rpcParamTypes = params_types.Item2 };
			return SendWithPayload(SpecificationCVSP.CVSP_RPC_REQ, SpecificationCVSP.CVSP_RPCTARGET_ALL, info);
		}
		else
		{
			return SendRPCToAll(id, funcName);
		}
	}


	// 서버에게만 RPC 요청
	public int SendRPCToServer(int id, string funcName, params object[] parameters)
	{
		if (parameters.Length > 0)
		{
			var params_types = SerializeObjects(parameters);
			RPCInfo info = new() { ownerId = id, functionName = funcName, rpcParams = params_types.Item1, rpcParamTypes = params_types.Item2 };
			return SendWithPayload(SpecificationCVSP.CVSP_RPC_REQ, SpecificationCVSP.CVSP_RPCTARGET_SERVER, info);
		}
		else
		{
			Debug.LogWarning("서버에만 보내는 NOPARAM RPC는 아직 미구현...");
			return 0;
		}
	}


	Encoding GetEucKrEncoding()
	{
		const int eucKrCodepage = 51949;
		return System.Text.Encoding.GetEncoding(eucKrCodepage);
	}


	// 반복되는 공통 기능 묶기.
	private int _InternalSendWithPayloadByte(byte cmd, byte option, byte[] payloadByte)
	{
		CVSPHeader header = new CVSPHeader();
		header.cmd = cmd;
		header.option = option;
		header.packetLength = (ushort)(4 + payloadByte.Length); // 하드코딩, CVSP 구조체의 크기 + payload 크기

		byte[] buffer = new byte[header.packetLength];
		StructureToByte(header).CopyTo(buffer, 0);
		payloadByte.CopyTo(buffer, 4); // 하드코딩, 페이로드는 버퍼 다음에 붙여주기.

		return socket.Send(buffer, 0, buffer.Length, SocketFlags.None);
	}


	public (byte[], byte[]) SerializeObjects(params object[] parameters)
	{
		if (parameters == null
			|| parameters.Length == 0)
		{
			Debug.Log("직렬화할 파라미터들이 아무것도 없음!");
			return (null, null);
		}

		int byteSize = 0;

		List<KeyValuePair<byte[], ushort>> bytesList = new();
		byte[] types = new byte[8];

		// 네트워크 전송/받은 후 역직렬화 시 쓰레기 값 탐지 용도
		if (parameters.Length < 8)
		{
			types[parameters.Length] = RPCValueType.UNDEFINED;
		}

		int index = 0;

		foreach (var param in parameters)
		{
			if (param.GetType() == typeof(int))
			{
				byteSize += 4;
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes((int)param), 4));
				types[index] = RPCValueType.INT;
			}
			else if (param.GetType() == typeof(float))
			{
				byteSize += 4;
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes((float)param), 4));
				types[index] = RPCValueType.FLOAT;
			}
			else if (param.GetType() == typeof(string))
			{
				ushort length = (ushort)(param as string).Length;
				byteSize += length + 2;
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(length), 2));
				bytesList.Add(new KeyValuePair<byte[], ushort>(GetEucKrEncoding().GetBytes((string)param), length));
				types[index] = RPCValueType.STRING;
			}
			else if (param.GetType() == typeof(Vector3))
			{
				byteSize += 12;
				Vector3 vec = (Vector3)param;
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(vec.x), 4));
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(vec.y), 4));
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(vec.z), 4));
				types[index] = RPCValueType.VEC3;
			}
			else if (param.GetType() == typeof(Quaternion))
			{
				byteSize += 16;
				Quaternion quat = (Quaternion)param;
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(quat.x), 4));
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(quat.y), 4));
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(quat.z), 4));
				bytesList.Add(new KeyValuePair<byte[], ushort>(BitConverter.GetBytes(quat.w), 4));
				types[index] = RPCValueType.QUAT;
			}

			++index;
		}

		byte[] bytes = new byte[byteSize];
		ushort head = 0;

		foreach (var pair in bytesList)
		{
			ushort size = pair.Value;
			Array.Copy(pair.Key, 0, bytes, head, size);
			head += size;
		}

		return (bytes, types);
	}


	// 이게 맞나 싶기도 한데...
	public object[] DeserializeObjects(byte[] bytesToDeserialize, byte[] typeBytes)
	{
		// 네트워크 통신 갔다오니까 쓰레기 값으로 96 / 8칸씩 꽉 차서, 별 쓸모가 없네...
		
		if (bytesToDeserialize == null
			|| typeBytes == null
			|| bytesToDeserialize.Length == 0
			|| typeBytes.Length == 0)
		{
			Debug.Log("역직렬화할 데이터들이 없거나 유효하지 않음!");
			return null;
		}
		

		int validTypeSize = 0;
		foreach (var type in typeBytes)
		{
			// 유효 타입 범위 하드코딩
			if (type > RPCValueType.UNDEFINED && type <= RPCValueType.QUAT)
			{
				++validTypeSize;
			}
			else
			{
				break;
			}
		}

		if (validTypeSize == 0)
		{
			Debug.Log("역직렬화할 데이터가 유효하지 않음!");
			return null;
		}

		object[] result = new object[validTypeSize];
		int size = bytesToDeserialize.Length;
		int typeIndex = 0;
		int resultIndex = 0;

		Debug.Log("역직렬화: byte size: " + size + ", typeBytes size: " + typeBytes.Length);

		for (int head = 0; head < size && typeIndex < validTypeSize;)
		{
			Debug.Log("시작! head: " + head + ", typeIndex: " + typeIndex + ", resultIndex: " + resultIndex);
			Debug.Log("typeBytes[typeIndex]: " + typeBytes[typeIndex]);

			switch (typeBytes[typeIndex])
			{
				case RPCValueType.INT:
					result[resultIndex] = BitConverter.ToInt32(bytesToDeserialize, head);
					head += 4;
					break;

				case RPCValueType.FLOAT:
					result[resultIndex] = BitConverter.ToSingle(bytesToDeserialize, head);
					head += 4;
					break;

				case RPCValueType.STRING:
					ushort length = BitConverter.ToUInt16(bytesToDeserialize, head);
					head += 2;
					result[resultIndex] = GetEucKrEncoding().GetString(bytesToDeserialize, head, length);
					head += length;
					break;

				case RPCValueType.VEC3:
					Vector3 vec = new Vector3();
					vec.x = BitConverter.ToSingle(bytesToDeserialize, head);
					vec.y = BitConverter.ToSingle(bytesToDeserialize, head + 4);
					vec.z = BitConverter.ToSingle(bytesToDeserialize, head + 8);
					result[resultIndex] = vec;
					head += 12;
					break;

				case RPCValueType.QUAT:
					Quaternion quat = new Quaternion();
					quat.x = BitConverter.ToSingle(bytesToDeserialize, head);
					quat.y = BitConverter.ToSingle(bytesToDeserialize, head + 4);
					quat.z = BitConverter.ToSingle(bytesToDeserialize, head + 8);
					quat.w = BitConverter.ToSingle(bytesToDeserialize, head + 12);
					result[resultIndex] = quat;
					head += 16;
					break;

				default:
					Debug.LogWarning("head: " + head + ", typeIndex: " + typeIndex + ", resultIndex: " + resultIndex);
					Debug.LogWarning("typeBytes[typeIndex]: " + typeBytes[typeIndex]);
					Debug.LogError("역직렬화 실패! 타입이 정해지지 않았거나 유효하지 않습니다!");
					return null;

				// 웬만해서는 실행 안 될 것임...
				case RPCValueType.UNDEFINED:
					Debug.LogWarning("역직렬화 중, UNDEFINED를 만나 바로 return됨!");
					return result;
			}

			++typeIndex;
			++resultIndex;
		}

		return result;
	}


	public void ReceiveThread()
	{
		int readBytesResult = 0;
		byte[] headerBuffer = new Byte[4];
		byte[] payloadByte;

		CVSPHeader header = new CVSPHeader();

		try
		{
			while (bIsConnected)
			{
				readBytesResult = socket.Receive(headerBuffer, 4, SocketFlags.Peek);

				if (readBytesResult < 0 || readBytesResult != 4)
				{
					Debug.LogWarning("경고: Receive 중 헤더 읽기 실패!: " + readBytesResult);
					continue;
				}

				header = (CVSPHeader)Convert.ChangeType(ByteToStructure(headerBuffer, header.GetType()), header.GetType());

				readBytesResult = socket.Receive(readBuffer, header.packetLength, SocketFlags.None);
				payloadByte = new byte[header.packetLength - headerBuffer.Length];

				// headerBuffer.Length는 4
				// readBuffer에 헤더 버퍼 이후의 위치에 페이로드만 복사
				Buffer.BlockCopy(readBuffer, headerBuffer.Length, payloadByte, 0, header.packetLength - headerBuffer.Length);


				// 채팅 응답
				if (header.cmd == SpecificationCVSP.CVSP_CHATTINGRES)
				{
					string message = GetEucKrEncoding().GetString(payloadByte); //Encoding.ASCII.GetString(payloadByte);

					Debug.Log("서버: " + message);

					// 유니티에서 스레드로 UI에 직접적인 간섭 불가능하므로 이런 방식을 채택
					NetworkConnectionManager.instance.chattingQueue.Enqueue(message);
				}


				// 트랜스폼 정보 응답
				else if (header.cmd == SpecificationCVSP.CVSP_MONITORINGMSG)
				{
					TransformInfo info = new();
					info = (TransformInfo)ByteToStructure(payloadByte, info.GetType());
					NetworkConnectionManager.instance.EnqueueTransformInfo(info);
				}


				// RPC 응답 (파라미터 보유)
				else if (header.cmd == SpecificationCVSP.CVSP_RPC_RES)
				{
					if (header.option == SpecificationCVSP.CVSP_SUCCESS)
					{
						RPCInfo info = new();
						info = (RPCInfo)ByteToStructure(payloadByte, info.GetType());

						Debug.Log("<color.green>RPC</color> [" + info.functionName + "] <color.green>성공적으로 응답 받음!</color>");

						NetworkConnectionManager.instance.rpcQueue.Enqueue(info);
					}
					else
					{
						Debug.LogWarning("RPC 응답을 받았으나, 옵션이 성공이 아님!");
					}
				}


				// RPC 응답 (파라미터 미보유)
				else if (header.cmd == SpecificationCVSP.CVSP_RPC_NOPARAM_RES)
				{
					if (header.option == SpecificationCVSP.CVSP_SUCCESS)
					{
						RPCInfoNoParam info = new();
						info = (RPCInfoNoParam)ByteToStructure(payloadByte, info.GetType());
						RPCInfo noParamInfo = new()
						{
							ownerId = info.ownerId,
							functionName = info.functionName,
							rpcParams = null,
							rpcParamTypes = null
						};

						Debug.Log("<color.green>RPC</color> [" + noParamInfo.functionName + "] <color.green>성공적으로 응답 받음!</color>");

						NetworkConnectionManager.instance.rpcQueue.Enqueue(noParamInfo);
					}
					else
					{
						Debug.LogWarning("RPC 파라미터 없는 응답을 받았으나, 옵션이 성공이 아님!");
					}
				}


				// Join 응답
				else if (header.cmd == SpecificationCVSP.CVSP_JOINRES)
				{
					Debug.Log("서버로부터 Join 응답 도착!");

					if (!bIsJoinned)
					{
						if (header.option == SpecificationCVSP.CVSP_SUCCESS)
						{
							bIsJoinned = true;

							int playerId = ByteToInt(payloadByte);

							Debug.Log("Join 상태 true, 나의 id: " + playerId);

							NetworkConnectionManager.instance.OnJoinSuccessed(playerId);

							PlayerInfo info = new()
							{
								id = playerId,
								nickname = NetworkConnectionManager.instance.localNickname
							};

							// 플레이어 생성 요청
							NetworkConnectionManager.instance.AddPlayer(info);
						}
					}
					else
					{
						if (header.option == SpecificationCVSP.CVSP_NEW_USER)
						{
							PlayerInfo info = new();
							info = (PlayerInfo)ByteToStructure(payloadByte, info.GetType());

							if (info.id != NetworkConnectionManager.instance.playerId)
							{
								Debug.Log("플레이어 [" + info.id + "] " + info.nickname + " 의 정보 받음!");

								// 플레이어 생성 요청
								NetworkConnectionManager.instance.AddPlayer(info);
							}
						}
						else
						{
							Debug.Log("Join에 실패했거나, 이미 Join 상태인데 Join 응답 받았음");
						}
					}
				}


				// 오브젝트 스폰 응답
				else if (header.cmd == SpecificationCVSP.CVSP_SPAWN_OBJECT_RES)
				{
					Debug.Log("서버로부터 Object Spawn 응답 도착!");

					if (header.option == SpecificationCVSP.CVSP_SUCCESS)
					{
						// 받은 payload를 해석
						ObjectSpawnInfo info = new();
						info = (ObjectSpawnInfo)Convert.ChangeType(ByteToStructure(payloadByte, info.GetType()), info.GetType());

						NetworkConnectionManager.instance.AddObjectSpawnInfoToActionQueue(
							info.objectName,
							new Vector3(info.posX, info.posY, info.posZ),
							new Quaternion(info.quatX, info.quatY, info.quatZ, info.quatW),
							info.ownerId
							);
					}
					else
					{
						Debug.LogWarning("서버로부터 도착한 Object Spawn 응답의 option이 CVSP_SUCCESS가 아님");
					}
				}
			}
		}
		catch (Exception e)
		{
			Debug.Log("오류: 서버와 연결을 종료합니다. 메시지: " + e.Message);
			Stop();

			bIsConnected = false;
			return;
		}
	}
}

