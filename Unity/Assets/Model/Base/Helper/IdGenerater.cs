namespace ETModel
{
	public static class IdGenerater
	{
        //Int64  //等于long, 占8个字节. -9223372036854775808 9223372036854775807

        private static long instanceIdGenerator;
		
		private static long appId;
		
		public static long AppId
		{
			set
			{
				appId = value;
                //左移48位 相当于appid* (2^48)
                instanceIdGenerator = appId << 48;
			}
		}

		private static ushort value;

        //生成ID的算法 通过时间过去式的思想 使之不会出现重复的ID
		public static long GenerateId()
		{
            //客户端现在的秒数
			long time = TimeHelper.ClientNowSeconds();

			return (appId << 48) + (time << 16) + ++value;
		}
		
        //生成实例ID
		public static long GenerateInstanceId()
		{
			return ++instanceIdGenerator;
		}

        //获取AppID 等于v右移48 等于v/ (2^48)
        public static int GetAppId(long v)
		{
			return (int)(v >> 48);
		}
	}
}