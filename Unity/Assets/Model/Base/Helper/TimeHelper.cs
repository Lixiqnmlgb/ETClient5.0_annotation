using System;

namespace ETModel
{
	public static class TimeHelper
    {
        //C#中时间的Ticks属性是一个很大的长整数，单位是 100 毫微秒。

        //1秒=1000毫秒；
        //1毫秒=1000微秒；
        //1微秒=1纳秒
        //而1毫秒 = 10000ticks；所以1ticks=100纳秒=0.1微秒
        //ticks这个属性值是指从0001年1月1日12：00:00开始到此时的以ticks为单位的时间，就是以ticks表示的时间的间隔数。
        //使用DateTime.Now.Ticks返回的是一个long型的数值。
        //而如果是UtcNow,就是格林治时间 从1970, 1, 1开始 
        private static readonly long epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
		/// <summary>
		/// 客户端时间
		/// </summary>
		/// <returns></returns>
		public static long ClientNow()
		{
			return (DateTime.UtcNow.Ticks - epoch) / 10000;
		}

		public static long ClientNowSeconds()
		{
			return (DateTime.UtcNow.Ticks - epoch) / 10000000;
		}

		public static long Now()
		{
			return ClientNow();
		}
    }
}