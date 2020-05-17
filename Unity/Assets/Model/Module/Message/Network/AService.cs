using System;
using System.Net;

namespace ETModel
{
	public enum NetworkProtocol
	{
		KCP,
		TCP,
		WebSocket,
	}

    //网络服务基类
	public abstract class AService: Component
	{
		public abstract AChannel GetChannel(long id);

		private Action<AChannel> acceptCallback;

        //连接时的回调
		public event Action<AChannel> AcceptCallback
		{
			add
			{
				this.acceptCallback += value;
			}
			remove
			{
				this.acceptCallback -= value;
			}
		}
		
        //当连接时候
		protected void OnAccept(AChannel channel)
		{
			this.acceptCallback.Invoke(channel);
		}
        //连接通道
		public abstract AChannel ConnectChannel(IPEndPoint ipEndPoint);
		
		public abstract AChannel ConnectChannel(string address);

		public abstract void Remove(long channelId);

		public abstract void Update();
	}
}