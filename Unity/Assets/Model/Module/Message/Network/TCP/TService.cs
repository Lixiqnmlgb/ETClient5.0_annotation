using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Microsoft.IO;

namespace ETModel
{
    /// <summary>
    /// TCPServer
    /// </summary>
	public sealed class TService : AService
	{
		private readonly Dictionary<long, TChannel> idChannels = new Dictionary<long, TChannel>();

		private readonly SocketAsyncEventArgs innArgs = new SocketAsyncEventArgs();
		private Socket acceptor;
		
		public RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();
		
		public HashSet<long> needStartSendChannel = new HashSet<long>();
		
		public int PacketSizeLength { get; }
		
		/// <summary>
		/// 即可做client也可做server
		/// </summary>
		public TService(int packetSizeLength, IPEndPoint ipEndPoint, Action<AChannel> acceptCallback)
		{
			this.PacketSizeLength = packetSizeLength;
            //接受连接的 回调
			this.AcceptCallback += acceptCallback;
			
			this.acceptor = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            //SocketOptionName.ReuseAddress 复用端口
            //端口复用真正的用处主要在于服务器编程：当服务器需要重启时，经常会碰到端口尚未完全关闭的情况
            //这时如果不设置端口复用，则无法完成绑定，因为端口还处于被别的套接口绑定的状态之中。
            this.acceptor.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //完成时的回调
			this.innArgs.Completed += this.OnComplete;
			
			this.acceptor.Bind(ipEndPoint);
            //监听最多1000个连接
			this.acceptor.Listen(1000);
            //异步接受连接
			this.AcceptAsync();
		}

        //构造函数 设置包体长度用多少个字节表示
		public TService(int packetSizeLength)
		{
			this.PacketSizeLength = packetSizeLength;
		}
		
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			
			base.Dispose();

			foreach (long id in this.idChannels.Keys.ToArray())
			{
				TChannel channel = this.idChannels[id];
				channel.Dispose();
			}
			this.acceptor?.Close();
			this.acceptor = null;
			this.innArgs.Dispose();
		}

		private void OnComplete(object sender, SocketAsyncEventArgs e)
		{
			switch (e.LastOperation)
			{
				case SocketAsyncOperation.Accept:
					OneThreadSynchronizationContext.Instance.Post(this.OnAcceptComplete, e);
					break;
				default:
					throw new Exception($"socket accept error: {e.LastOperation}");
			}
		}
		//异步接受连接
		public void AcceptAsync()
		{
			this.innArgs.AcceptSocket = null;
			if (this.acceptor.AcceptAsync(this.innArgs))
			{
				return;
			}
			OnAcceptComplete(this.innArgs);
		}

        //异步接受连接的回调
		private void OnAcceptComplete(object o)
		{
			if (this.acceptor == null)
			{
				return;
			}
			SocketAsyncEventArgs e = (SocketAsyncEventArgs)o;
			
			if (e.SocketError != SocketError.Success)
			{
				Log.Error($"accept error {e.SocketError}");
				this.AcceptAsync();
				return;
			}
			TChannel channel = new TChannel(e.AcceptSocket, this);
			this.idChannels[channel.Id] = channel;

			try
			{
				this.OnAccept(channel);
			}
			catch (Exception exception)
			{
				Log.Error(exception);
			}

			if (this.acceptor == null)
			{
				return;
			}
			
			this.AcceptAsync();
		}
		
        //获取通道
		public override AChannel GetChannel(long id)
		{
			TChannel channel = null;
			this.idChannels.TryGetValue(id, out channel);
			return channel;
		}

        //连接通道
		public override AChannel ConnectChannel(IPEndPoint ipEndPoint)
		{
			TChannel channel = new TChannel(ipEndPoint, this);
			this.idChannels[channel.Id] = channel;

			return channel;
		}
        //连接通道
        public override AChannel ConnectChannel(string address)
		{
			IPEndPoint ipEndPoint = NetworkHelper.ToIPEndPoint(address);
			return this.ConnectChannel(ipEndPoint);
		}
        //标记有通道需要进行发送消息 那么在Update里就会进行逻辑处理
		//也就是将通道的ID压入到哈希表中 Update里通过ID取到通道,然后调用发送接口
		public void MarkNeedStartSend(long id)
		{
			this.needStartSendChannel.Add(id);
		}
        //移除
		public override void Remove(long id)
		{
			TChannel channel;
			if (!this.idChannels.TryGetValue(id, out channel))
			{
				return;
			}
			if (channel == null)
			{
				return;
			}
			this.idChannels.Remove(id);
			channel.Dispose();
		}

        //每帧检查 是否有通道要进行发送
		public override void Update()
		{
			foreach (long id in this.needStartSendChannel)
			{
				TChannel channel;
				if (!this.idChannels.TryGetValue(id, out channel))
				{
					continue;
				}

				if (channel.IsSending)
				{
					continue;
				}

				try
				{
                    //调用通道开始发送
					channel.StartSend();
				}
				catch (Exception e)
				{
					Log.Error(e);
				}
			}
			//清理
			this.needStartSendChannel.Clear();
		}
	}
}