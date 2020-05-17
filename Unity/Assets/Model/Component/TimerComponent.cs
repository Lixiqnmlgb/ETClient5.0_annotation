using System.Collections.Generic;
using System.Threading;

namespace ETModel
{
	public struct Timer
	{
		public long Id { get; set; }
		public long Time { get; set; }
		public ETTaskCompletionSource tcs;
	}

	[ObjectSystem]
	public class TimerComponentUpdateSystem : UpdateSystem<TimerComponent>
	{
		public override void Update(TimerComponent self)
		{
			self.Update();
		}
	}

    /// <summary>
    /// 计时器 组件
    /// </summary>
	public class TimerComponent : Component
	{
		private readonly Dictionary<long, Timer> timers = new Dictionary<long, Timer>();

		/// <summary>
		/// key: time, value: timer id
		/// </summary>
		private readonly MultiMap<long, long> timeId = new MultiMap<long, long>();

		private readonly Queue<long> timeOutTime = new Queue<long>();
		
		private readonly Queue<long> timeOutTimerIds = new Queue<long>();

		// 记录最小时间，不用每次都去MultiMap取第一个值
		private long minTime;

		public void Update()
		{
			if (this.timeId.Count == 0)
			{
				return;
			}
			//获得现在的时间
			long timeNow = TimeHelper.Now();
			//判断当前时间 如果是小于最近要触发事件的时间 那么就返回
			if (timeNow < this.minTime)
			{
				return;
			}
			//如果是到了最近要触发事件的时间了
			//从缓存timeId取出所有的定时事件
			//一个时间可能同时要触发好几个事情,所以value是 List<long>,存储这个时间下所有要触发的事件ID
			//进行遍历
			foreach (KeyValuePair<long, List<long>> kv in this.timeId.GetDictionary())
			{
				long k = kv.Key;
				//判断事件的时间是否大于当前时间
				if (k > timeNow)
				{
                    //那么更新最近要触发事件的时间 然后跳出循环
					minTime = k;
					break;
				}
				//否则 都压入到timeOutTime队列中 以下进行遍历 依次取出事件进行调用
				this.timeOutTime.Enqueue(k);
			}
			//如果等待处理的事件数量大于0
			while(this.timeOutTime.Count > 0)
			{
                //出列
				long time = this.timeOutTime.Dequeue();
                //通过时间找到事件ID
				foreach(long timerId in this.timeId[time])
				{
					//将每个事件ID压入到timeOutTimerIds(超时列表)
					this.timeOutTimerIds.Enqueue(timerId);	
				}
				this.timeId.Remove(time);
			}
			//遍历超时列表
			while(this.timeOutTimerIds.Count > 0)
			{
				long timerId = this.timeOutTimerIds.Dequeue();
				//从缓存根据事件ID获取到timer对象 里面缓存了ETTaskCompletionSource对象tcs
				Timer timer;
				if (!this.timers.TryGetValue(timerId, out timer))
				{
					continue;
				}
				this.timers.Remove(timerId);
				//简单的说通过调用tcs的SetResult方法,即可返回await等待的地方 执行下面的代码
				//往复杂的说,await后面的代码,会被封装到委托中,在setresult的时候,执行该委托,等于执行这后半部分的代码
				timer.tcs.SetResult();
			}
		}

		private void Remove(long id)
		{
			//从管理所有timer对象的字典中移除掉这个timer
			this.timers.Remove(id);
		}

		//等待到设定的时间 tillTime
		public ETTask WaitTillAsync(long tillTime, CancellationToken cancellationToken)
		{
			ETTaskCompletionSource tcs = new ETTaskCompletionSource();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = tillTime, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
            //缓存最近时间点的Task
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
            //取消的事件 就是将存储的字典的相同key移除掉
			cancellationToken.Register(() => { this.Remove(timer.Id); });
			return tcs.Task;
		}

		//等待到设定的时间 tillTime
		public ETTask WaitTillAsync(long tillTime)
		{
			ETTaskCompletionSource tcs = new ETTaskCompletionSource();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = tillTime, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
			return tcs.Task;
		}

		//从现在起 等待N毫秒后
		public ETTask WaitAsync(long time, CancellationToken cancellationToken)
		{
			ETTaskCompletionSource tcs = new ETTaskCompletionSource();
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = TimeHelper.Now() + time, tcs = tcs };
			this.timers[timer.Id] = timer;
			this.timeId.Add(timer.Time, timer.Id);
			if (timer.Time < this.minTime)
			{
				this.minTime = timer.Time;
			}
			//注册取消时候执行的回调 ->调用Remove方法,从缓存中移除掉这个事件ID
			cancellationToken.Register(() => { this.Remove(timer.Id); });
			return tcs.Task;
		}

        //从现在起 等待N毫秒后
        public ETTask WaitAsync(long time)
		{
			ETTaskCompletionSource tcs = new ETTaskCompletionSource();
			//创建Timer 并且生成ID 设定触发时间=当前时间+等待的时间 缓存ETTaskCompletionSource对象tcs
			Timer timer = new Timer { Id = IdGenerater.GenerateId(), Time = TimeHelper.Now() + time, tcs = tcs };
			//管理所有timer对象的字典
			this.timers[timer.Id] = timer;
			//管理所有时间相关的事件 key:时间 value:timerid 通过timerid可以找到以上的Timer对象 
			//然后获取它的tcs ...
			this.timeId.Add(timer.Time, timer.Id);
			//这个事件的时间跟当前最近要触发的时间进行比较 如果小于 那么最近要触发的事件是这个 timer.Time
			if (timer.Time < this.minTime)
			{
				//则进行更新最近要触发的time
				this.minTime = timer.Time;
			}
			//返回tcs.Task 而非tcs,所以等待的是其内部的Task对象
			return tcs.Task;
		}
	}
}