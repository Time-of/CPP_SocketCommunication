﻿using System;
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
		RPCInfo info = new() { ownerId = id, functionName = funcName };
		return SendWithPayload(SpecificationCVSP.CVSP_RPC_REQ, SpecificationCVSP.CVSP_RPCTARGET_ALL, info);
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


				// RPC 응답
				else if (header.cmd == SpecificationCVSP.CVSP_RPC_RES)
				{
					if (header.option == SpecificationCVSP.CVSP_SUCCESS)
					{
						Debug.Log("RPC 응답 성공적으로 받음!");

						RPCInfo info = new();
						info = (RPCInfo)ByteToStructure(payloadByte, info.GetType());

						NetworkConnectionManager.instance.rpcQueue.Enqueue(info);
					}
					else
					{
						Debug.LogWarning("RPC 응답을 받았으나, 옵션이 성공이 아님!");
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
						}
					}
					else
					{
						if (header.option == SpecificationCVSP.CVSP_NEW_USER)
						{
							// (하드코딩) 다른 플레이어가 들어온 경우, 다른 플레이어의 캐릭터 스폰
							int otherPlayerId = ByteToInt(payloadByte);

							Debug.Log("다른 플레이어, " + otherPlayerId + " 가 게임에 참가함!");

							NetworkConnectionManager.instance.AddObjectSpawnInfoToActionQueue(
								"Player",
								Vector3.zero,
								Quaternion.identity,
								otherPlayerId
								);
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

