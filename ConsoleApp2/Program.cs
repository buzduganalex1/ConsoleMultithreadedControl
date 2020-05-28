using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public class Program
{
	const int STD_INPUT_HANDLE = -10;

	[DllImport("kernel32.dll", SetLastError = true)]
	internal static extern IntPtr GetStdHandle(int nStdHandle);

	[DllImport("kernel32.dll", SetLastError = true)]
	static extern bool CancelIoEx(IntPtr handle, IntPtr lpOverlapped);

	public static Dictionary<CancellationTokenSource, Task> tasks = new Dictionary<CancellationTokenSource, Task>();

	// Incoming data from the client.  
	public static string data = null;

	public static void StartListening()
	{
		// Data buffer for incoming data.  
		byte[] bytes = new Byte[1024];

		// Establish the local endpoint for the socket.  
		// Dns.GetHostName returns the name of the
		// host running the application.  
		IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
		IPAddress ipAddress = ipHostInfo.AddressList[0];
		IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

		// Create a TCP/IP socket.  
		Socket listener = new Socket(ipAddress.AddressFamily,
			SocketType.Stream, ProtocolType.Tcp);

		// Bind the socket to the local endpoint and
		// listen for incoming connections.  
		try
		{
			listener.Bind(localEndPoint);
			listener.Listen(10);

			// Start listening for connections.  
			while (true)
			{
				Console.WriteLine("Waiting for a connection...");
				// Program is suspended while waiting for an incoming connection.  
				Socket handler = listener.Accept();

				var handle = GetStdHandle(STD_INPUT_HANDLE);
				CancelIoEx(handle, IntPtr.Zero);

				SomethingToInvokeItAll(handler, bytes);
			}
		}
		catch (Exception e)
		{
			Console.WriteLine(e.ToString());
		}

		Console.WriteLine("\nPress ENTER to continue...");
		Console.Read();
	}

	public static int Main(String[] args)
	{
		var task = new Task(() => { StartListening(); });
		task.Start();
		while (true)
		{
		}
	}

	static void SomethingToInvokeItAll(Socket socket, byte[] bytes)
	{
		foreach(var task in tasks)
		{
			task.Key.Cancel();
		}

		var cancellationTokenSource = new CancellationTokenSource();
		SomethingToWaitForTheTaskToFail(socket, bytes, cancellationTokenSource);
	}

	static void SomethingToWaitForTheTaskToFail(Socket socket, byte[] bytes, CancellationTokenSource source)
	{
		try
		{
			var t = new Task(() => ActionToExecuteInTask(socket, bytes, source.Token), source.Token);
			tasks.Add(source, t);
			t.Start();
		}
		catch (Exception)
		{
			Console.WriteLine("Ok");
		}
	}

	static void ActionToExecuteInTask(Socket handler, byte[] bytes, CancellationToken ct)
	{
		data = null;

		// An incoming connection needs to be processed.  
		while (true)
		{
			int bytesRec = handler.Receive(bytes);
			data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
			if (data.IndexOf("<EOF>") > -1)
			{
				break;
			}
		}

		// Show the data on the console.  
		Console.WriteLine("Text received : {0}", data);

		Console.ReadLine();

		//ct.ThrowIfCancellationRequested();

		// Echo the data back to the client.  
		//byte[] msg = Encoding.ASCII.GetBytes(data);

		//handler.Send(msg);
		//handler.Shutdown(SocketShutdown.Both);
		//handler.Close();
	}

}