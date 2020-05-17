using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.IO;

namespace ETModel
{
	/// <summary>
	/// 封装Socket,将回调push到主线程处理
	/// </summary>
	public sealed class TChannel: AChannel
	{
		private Socket socket;
		private SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
		private SocketAsyncEventArgs outArgs = new SocketAsyncEventArgs();

		private readonly CircularBuffer recvBuffer = new CircularBuffer();//存储接收消息的循环队列
		private readonly CircularBuffer sendBuffer = new CircularBuffer();//存储发送消息的循环队列

		private readonly MemoryStream memoryStream;

		private bool isSending;

		private bool isRecving;

		private bool isConnected;

		private readonly PacketParser parser;

		private readonly byte[] packetSizeCache;
		
		//构造函数
		public TChannel(IPEndPoint ipEndPoint, TService service): base(service, ChannelType.Connect)
		{
			//包体大小的占位长度 2个字节
			int packetSize = service.PacketSizeLength;
			this.packetSizeCache = new byte[packetSize];
			this.memoryStream = service.MemoryStreamManager.GetStream("message", ushort.MaxValue);
			//创建Socket
			this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			this.socket.NoDelay = true;
			//对解码器进行构造 传递包体大小占用的长度 接收的buffer 写入的内存流
			this.parser = new PacketParser(packetSize, this.recvBuffer, this.memoryStream);
			this.innArgs.Completed += this.OnComplete;//接收异步操作完成时
			this.outArgs.Completed += this.OnComplete;//发送异步操作完成时

			this.RemoteAddress = ipEndPoint;

			this.isConnected = false;
			this.isSending = false;
		}
		
		public TChannel(Socket socket, TService service): base(service, ChannelType.Accept)
		{
			int packetSize = service.PacketSizeLength;
			this.packetSizeCache = new byte[packetSize];
			this.memoryStream = service.MemoryStreamManager.GetStream("message", ushort.MaxValue);
			
			this.socket = socket;
			this.socket.NoDelay = true;
			this.parser = new PacketParser(packetSize, this.recvBuffer, this.memoryStream);
			this.innArgs.Completed += this.OnComplete;
			this.outArgs.Completed += this.OnComplete;

			this.RemoteAddress = (IPEndPoint)socket.RemoteEndPoint;
			
			this.isConnected = true;
			this.isSending = false;
		}

		//释放的时候
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();
			
			this.socket.Close();
			this.innArgs.Dispose();
			this.outArgs.Dispose();
			this.innArgs = null;
			this.outArgs = null;
			this.socket = null;
			this.memoryStream.Dispose();
		}
		
		private TService GetService()
		{
			return (TService)this.Service;
		}

		public override MemoryStream Stream
		{
			get
			{
				return this.memoryStream;
			}
		}

		/// <summary>
		/// 进入连接->连接完成后继续调用该方法->开始进行接收消息
		/// </summary>
		public override void Start()
		{
			//如果不处于连接状态 那么进行异步连接服务器
			if (!this.isConnected)
			{
				//异步连接
				this.ConnectAsync(this.RemoteAddress);
				//返回 
				return;
			}

			//如果不是处于接收状态 那么进行异步接收
			if (!this.isRecving)
			{
				this.isRecving = true;
				this.StartRecv();
			}

			//缓存该通道到TService中
			this.GetService().MarkNeedStartSend(this.Id);
		}
		
		/// <summary>
		/// 对外的发送接口
		/// </summary>
		/// <param name="stream"></param>
		public override void Send(MemoryStream stream)
		{
			if (this.IsDisposed)
			{
				throw new Exception("TChannel已经被Dispose, 不能发送消息");
			}

			switch (this.GetService().PacketSizeLength)
			{
				//包体长度 用4个字节表示
				case Packet.PacketSizeLength4:
					if (stream.Length > ushort.MaxValue * 16)
					{
						throw new Exception($"send packet too large: {stream.Length}");
					}
					//缓存包体长度
					this.packetSizeCache.WriteTo(0, (int) stream.Length);
					break;

				//包体长度 用2个字节表示
				case Packet.PacketSizeLength2:
					if (stream.Length > ushort.MaxValue)
					{
						throw new Exception($"send packet too large: {stream.Length}");
					}
					//缓存包体长度
					this.packetSizeCache.WriteTo(0, (ushort) stream.Length);
					break;
				default:
					throw new Exception("packet size must be 2 or 4!");
			}
			//写入缓存的包体长度
			this.sendBuffer.Write(this.packetSizeCache, 0, this.packetSizeCache.Length);
			//写入要发送的报文
			this.sendBuffer.Write(stream);
			//将该通道标记为要发送状态
			this.GetService().MarkNeedStartSend(this.Id);
		}

		//提交到主线程
		private void OnComplete(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Connect:
					OneThreadSynchronizationContext.Instance.Post(this.OnConnectComplete, e);
					break;
				case SocketAsyncOperation.Receive:
					OneThreadSynchronizationContext.Instance.Post(this.OnRecvComplete, e);
					break;
				case SocketAsyncOperation.Send:
					OneThreadSynchronizationContext.Instance.Post(this.OnSendComplete, e);
					break;
				case SocketAsyncOperation.Disconnect:
					OneThreadSynchronizationContext.Instance.Post(this.OnDisconnectComplete, e);
					break;
				default:
					throw new Exception($"socket error: {e.LastOperation}");
			}
		}

		/// <summary>
		/// 异步连接
		/// </summary>
		/// <param name="ipEndPoint"></param>
		public void ConnectAsync(IPEndPoint ipEndPoint)
		{
			this.outArgs.RemoteEndPoint = ipEndPoint;
			//进行异步连接
			if (this.socket.ConnectAsync(this.outArgs))
			{
				return;
			}
			OnConnectComplete(this.outArgs);
		}

		/// <summary>
		/// 当连接完成
		/// </summary>
		/// <param name="o"></param>
		private void OnConnectComplete(object o)
		{
			if (this.socket == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs) o;
			
			if (e.SocketError != SocketError.Success)
			{
				this.OnError((int)e.SocketError);	
				return;
			}

			e.RemoteEndPoint = null;
			this.isConnected = true;

			//重新调用Start进行接收消息
			this.Start();
		}

		private void OnDisconnectComplete(object o)
		{
			SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
			this.OnError((int)e.SocketError);
		}

		/// <summary>
		/// 开始异步接收消息
		/// </summary>
		private void StartRecv()
		{
			int size = this.recvBuffer.ChunkSize - this.recvBuffer.LastIndex;
			//接收的数据 放到接收的循环队列中的lastBuffer 从上一次写入的位置继续写
			this.RecvAsync(this.recvBuffer.Last, this.recvBuffer.LastIndex, size);
		}

		/// <summary>
		/// 异步接收消息
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		public void RecvAsync(byte[] buffer, int offset, int count)
		{
			try
			{
				//设置要用于异步套接字方法的数据缓冲区,偏移,和缓冲区能写入的数量
				this.innArgs.SetBuffer(buffer, offset, count);
			}
			catch (Exception e)
			{
				throw new Exception($"socket set buffer error: {buffer.Length}, {offset}, {count}", e);
			}
			//开始一个异步请求以便从连接的 Socket 对象中接收数据。
			//接收数据存储到innArgs设置的缓冲区
			if (this.socket.ReceiveAsync(this.innArgs))
			{
				return;
			}
			//接收完成的时候
			OnRecvComplete(this.innArgs);
		}

		/// <summary>
		/// 接收完成时
		/// </summary>
		/// <param name="o"></param>
		private void OnRecvComplete(object o)
		{
			if (this.socket == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs) o;

			if (e.SocketError != SocketError.Success)
			{
				this.OnError((int)e.SocketError);
				return;
			}

			//获取在套接字操作中传输的字节数 
			//如果等于0
			if (e.BytesTransferred == 0)
			{
				this.OnError(ErrorCode.ERR_PeerDisconnect);
				return;
			}

			//上一次的索引加上本次接收到的数据数量
			this.recvBuffer.LastIndex += e.BytesTransferred;
			//如果刚好等于8192了 说明缓冲区写满了
			if (this.recvBuffer.LastIndex == this.recvBuffer.ChunkSize)
			{
				this.recvBuffer.AddLast();//将recvBuffer中的lastBuffer指向另一个可用的数组
				this.recvBuffer.LastIndex = 0;
			}

			// 收到消息回调
			while (true)
			{
				try
				{
					//进行解析 把完整的报文(去掉包头表示包体大小的字段)写入内存流中
					//如果解析失败就跳出
					if (!this.parser.Parse())
					{
						break;
					}
				}
				catch (Exception ee)
				{
					Log.Error($"ip: {this.RemoteAddress} {ee}");
					this.OnError(ErrorCode.ERR_SocketError);
					return;
				}
				try
				{
					//将上面解析得到的memoryStream传递给OnRead
					//执行读取的委托 外部谁注册了 就由谁进行读取 数据在内存流中 当作委托参数进行传递给委托的注册方了.
					//这个框架中 由Session在Awake方法中 进行读取的委托注册
					this.OnRead(this.parser.GetPacket());
				}
				catch (Exception ee)
				{
					Log.Error(ee);
				}
			}

			if (this.socket == null)
			{
				return;
			}
			//继续下一次的异步接收
			this.StartRecv();
		}

		public bool IsSending => this.isSending;

		/// <summary>
		/// 开始进入发送的逻辑
		/// </summary>
		public void StartSend()
		{
			if(!this.isConnected)
			{
				return;
			}
			
			// 没有数据需要发送
			if (this.sendBuffer.Length == 0)
			{
				this.isSending = false;
				return;
			}
			//计算循环队列中 要发送的数据大小
			this.isSending = true;
			int sendSize = this.sendBuffer.ChunkSize - this.sendBuffer.FirstIndex;
			if (sendSize > this.sendBuffer.Length)
			{
				sendSize = (int)this.sendBuffer.Length;
			}
			//发送接口
			this.SendAsync(this.sendBuffer.First, this.sendBuffer.FirstIndex, sendSize);
		}

		/// <summary>
		/// 调用socket最终的发送接口
		/// </summary>
		/// <param name="buffer"></param>
		/// <param name="offset"></param>
		/// <param name="count"></param>
		public void SendAsync(byte[] buffer, int offset, int count)
		{
			try
			{
				this.outArgs.SetBuffer(buffer, offset, count);
			}
			catch (Exception e)
			{
				throw new Exception($"socket set buffer error: {buffer.Length}, {offset}, {count}", e);
			}
			//终于用到socket来进行发送了
			if (this.socket.SendAsync(this.outArgs))
			{
				return;
			}
			//发送完成
			OnSendComplete(this.outArgs);
		}

		/// <summary>
		/// 当发送完成
		/// </summary>
		/// <param name="o"></param>
		private void OnSendComplete(object o)
		{
			if (this.socket == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs) o;

			if (e.SocketError != SocketError.Success)
			{
				this.OnError((int)e.SocketError);
				return;
			}
			
			if (e.BytesTransferred == 0)
			{
				this.OnError(ErrorCode.ERR_PeerDisconnect);
				return;
			}
			//第一个索引+=传输的字节数
			this.sendBuffer.FirstIndex += e.BytesTransferred;
			//如果等于ChunkSize
			if (this.sendBuffer.FirstIndex == this.sendBuffer.ChunkSize)
			{
				this.sendBuffer.FirstIndex = 0;
				//移除掉第一个(也就是已发送的数据)
				this.sendBuffer.RemoveFirst();
			}
			//继续调用StartSend 内部会判断 如果没有要发送的数据了 会跳出
			this.StartSend();
		}
	}
}