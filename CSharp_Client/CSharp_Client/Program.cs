using System;
using CVSP;


namespace CSharp_Client
{
	class Program
	{
		static unsafe void Main(string[] args)
		{
			System.Console.SetWindowSize(70, 20);

			SocketConnector connector = new SocketConnector();
			string messageInput;

			Console.WriteLine("RPCInfo의 크기: " + sizeof(RPCInfo));

			connector.ConnectToServer("127.0.0.1");

			if (connector.bIsConnected)
			{
				Console.WriteLine("서버 연결 성공!");
			}

			while (connector.bIsConnected)
			{
				messageInput = Console.ReadLine();

				if (messageInput == "EXIT")
				{
					connector.SendEndConnectionMessage();
				}
				else
				{
					connector.SendWithPayload(SpecificationCVSP.CVSP_CHATTINGREQ, SpecificationCVSP.CVSP_SUCCESS, messageInput);
				}
			}
		}
	}
}