using System;
using System.IO;
using System.Net;

namespace ETModel
{
	public enum ChannelType
	{
		Connect,
		Accept,
	}

    /// <summary>
    /// 通道
    /// </summary>
	public abstract class AChannel: ComponentWithId
	{
		public ChannelType ChannelType { get; }

		public AService Service { get; }

		public abstract MemoryStream Stream { get; }
		
		public int Error { get; set; }

		public IPEndPoint RemoteAddress { get; protected set; }

		private Action<AChannel, int> errorCallback;

        //错误回调
		public event Action<AChannel, int> ErrorCallback
		{
			add
			{
				this.errorCallback += value;
			}
			remove
			{
				this.errorCallback -= value;
			}
		}

        //读取回调
        private Action<MemoryStream> readCallback;
		//readCallback调度的时候 相当于调度了ReadCallback
		public event Action<MemoryStream> ReadCallback
		{
			add
			{
				this.readCallback += value;
			}
			remove
			{
				this.readCallback -= value;
			}
		}
		//读取回调
		protected void OnRead(MemoryStream memoryStream)
		{
			this.readCallback.Invoke(memoryStream);
		}
        //错误
		protected void OnError(int e)
		{
			this.Error = e;
			this.errorCallback?.Invoke(this, e);
		}

		protected AChannel(AService service, ChannelType channelType)
		{
            //生成ID
			this.Id = IdGenerater.GenerateId();
            //接受连接 或者 进行连接
			this.ChannelType = channelType;
            //网络服务
			this.Service = service;
		}

        
		public abstract void Start();
		
        //发送
		public abstract void Send(MemoryStream stream);
		//释放
		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			base.Dispose();

			this.Service.Remove(this.Id);
		}
	}
}