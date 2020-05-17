namespace ETModel
{
	[ObjectSystem]
	public class GlobalConfigComponentAwakeSystem : AwakeSystem<GlobalConfigComponent>
	{
		public override void Awake(GlobalConfigComponent t)
		{
			t.Awake();
		}
	}

	public class GlobalConfigComponent : Component
	{
		public static GlobalConfigComponent Instance;
		public GlobalProto GlobalProto;

		public void Awake()
		{
			Instance = this;
			//获得json格式的字符串 由配置生成
			string configStr = ConfigHelper.GetGlobal();
			//反序列化得到GlobalProto对象
			this.GlobalProto = JsonHelper.FromJson<GlobalProto>(configStr);
		}
	}
}