using System;
using System.IO;

namespace ETModel
{
	public class SessionCallbackComponent: Component
	{
		//热更层的Session注册了该委托
		public Action<Session, ushort, MemoryStream> MessageCallback;
		public Action<Session> DisposeCallback;

		public override void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}
			base.Dispose();

			this.DisposeCallback?.Invoke(this.GetParent<Session>());
		}
	}
}
