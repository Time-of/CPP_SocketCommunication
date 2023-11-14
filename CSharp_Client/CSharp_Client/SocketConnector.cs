using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using System.Collections;
using System.Net;

namespace CVSP
{
	public class SocketConnector
	{
		private Socket socket;
		private byte[] readBuffer = new byte[SpecificationCVSP.CVSP_BUFFERSIZE];
		private BinaryFormatter formatter;
		private bool bIsNewUser;
		private ArrayList transBuffer;

		public bool bIsConnected { get; private set; }


		public unsafe object ByteToStructure(Byte[] data, Type type)
		{
			// 언매니지드 영역에 메모리 할당 (배열 크기만큼)
			IntPtr buffer = Marshal.AllocHGlobal(data.Length);
			
			Marshal.Copy(data, 0, buffer, data.Length);

			object obj = Marshal.PtrToStructure(buffer, type);

			Marshal.FreeHGlobal(buffer);

			return (Marshal.SizeOf(obj) != data.Length) ? obj : null;
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
				socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
				IPAddress parsedIP = IPAddress.Parse(ipAddressString);
				socket.Connect(new IPEndPoint(parsedIP, port));
			}
			catch (Exception e)
			{
				Console.WriteLine("서버 연결 실패! : " + e.Message);
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
			CVSP header = new CVSP();
			header.cmd = cmd;
			header.option = option;
			header.packetLength = (short)(4); // 하드코딩, CVSP 구조체의 크기.

			byte[] buffer = new byte[header.packetLength];
			StructureToByte(header).CopyTo(buffer, 0);
			socket.Send(buffer, 0, buffer.Length, SocketFlags.None); // Send가 2회?

			int result = socket.Send(buffer, 0, buffer.Length, SocketFlags.None);

			return result;
		}
	}
}
