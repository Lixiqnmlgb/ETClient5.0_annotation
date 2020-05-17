using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ETModel
{
	public class OneThreadSynchronizationContext : SynchronizationContext
	{
		public static OneThreadSynchronizationContext Instance { get; } = new OneThreadSynchronizationContext();

        //获取当前的线程ID
		private readonly int mainThreadId = Thread.CurrentThread.ManagedThreadId;

		// 线程同步队列,发送接收socket回调都放到该队列,由poll线程统一执行
		private readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();

		private Action a;

        //每一帧从队列里取出执行压入的函数
		public void Update()
		{
			while (true)
			{
				if (!this.queue.TryDequeue(out a))
				{
					return;
				}
				a();
			}
		}

        //实际就是调用SynchronizationContext的Post方法进行同步到同个线程
        public override void Post(SendOrPostCallback callback, object state)
		{
            //判断当前线程等于该线程 那么有方法就直接执行 
			if (Thread.CurrentThread.ManagedThreadId == this.mainThreadId)
			{
				callback(state);
				return;
			}
			//否则就压入到队列中
			this.queue.Enqueue(() => { callback(state); });
		}
	}
}
