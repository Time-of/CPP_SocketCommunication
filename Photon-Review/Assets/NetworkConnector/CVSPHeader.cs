
namespace CVSP
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Runtime.Serialization.Formatters.Binary;
	using System.IO;
	using System.Runtime.InteropServices;


	// CVSP(Collaborative Virtual Service Protocol)을 객체 직렬화하여
	//  전송할 수 있도록 해 주는 구조체
	[System.Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct CVSPHeader
	{
		public byte cmd;
		public byte option;
		public ushort packetLength;
	}


	/// <summary>
	/// 64바이트짜리, 오브젝트 스폰 정보.
	/// </summary>
	[System.Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct ObjectSpawnInfo
	{
		[MarshalAs(UnmanagedType.R4)] public float posX;
		[MarshalAs(UnmanagedType.R4)] public float posY;
		[MarshalAs(UnmanagedType.R4)] public float posZ;
		[MarshalAs(UnmanagedType.R4)] public float quatX;
		[MarshalAs(UnmanagedType.R4)] public float quatY;
		[MarshalAs(UnmanagedType.R4)] public float quatZ;
		[MarshalAs(UnmanagedType.R4)] public float quatW;
		[MarshalAs(UnmanagedType.R4)] public int ownerId;

		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
		public string objectName;
	}


	[System.Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct TransformInfo
	{
		[MarshalAs(UnmanagedType.R4)] public float posX;
		[MarshalAs(UnmanagedType.R4)] public float posY;
		[MarshalAs(UnmanagedType.R4)] public float posZ;
		[MarshalAs(UnmanagedType.R4)] public float quatX;
		[MarshalAs(UnmanagedType.R4)] public float quatY;
		[MarshalAs(UnmanagedType.R4)] public float quatZ;
		[MarshalAs(UnmanagedType.R4)] public float quatW;
		[MarshalAs(UnmanagedType.R4)] public int ownerId;
	};


	[System.Serializable]
	[StructLayout(LayoutKind.Sequential)]
	public unsafe struct RPCInfo
	{
		[MarshalAs(UnmanagedType.R4)] public int ownerId;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
		public string functionName;
	}


// CVSP 프로토콜의 커맨드, 옵션 등 내부 플래그들을 정의하여
//  구체화하는 클래스
public sealed class SpecificationCVSP
	{
		// 프로토콜 버전
		public static byte CVSP_VER = (byte)0x01;

		// 프로토콜 커맨드(cmd)
		public static byte CVSP_JOINREQ = (byte)0x01;
		public static byte CVSP_JOINRES = (byte)0x02;
		public static byte CVSP_CHATTINGREQ = (byte)0x03;
		public static byte CVSP_CHATTINGRES = (byte)0x04;
		public static byte CVSP_OPERATIONREQ = (byte)0x05;
		public static byte CVSP_MONITORINGMSG = (byte)0x06;
		public static byte CVSP_LEAVEREQ = (byte)0x07;
		public static byte CVSP_SPAWN_OBJECT_REQ = (byte)0x08;
		public static byte CVSP_SPAWN_OBJECT_RES = (byte)0x09;
		public static byte CVSP_RPC_REQ = (byte)0x10;
		public static byte CVSP_RPC_RES = (byte)0x11;

		// 프로토콜 옵션
		public static byte CVSP_SUCCESS = (byte)0x01;
		public static byte CVSP_FAIL = (byte)0x02;
		public static byte CVSP_NEW_USER = (byte)0x03;
		public static byte CVSP_RPCTARGET_ALL = (byte)0x04;

		public static int CVSP_SIZE = 4;
		public static int CVSP_BUFFERSIZE = 4096;

		public static int ServerPort = 5004;
	}
}
