using UnityEngine;

namespace ETModel
{
    /// <summary>
    /// 游戏的主核心
    /// </summary>
	public static class Game
	{
		private static EventSystem eventSystem;

		public static EventSystem EventSystem
		{
			get
			{
				return eventSystem ?? (eventSystem = new EventSystem());
			}
		}
		
		private static Scene scene;

		public static Scene Scene
		{
			get
			{
				if (scene != null)
				{
					return scene;
				}
                //ClientMain
                scene = new Scene() { Name = "ClientM" };
				return scene;
			}
		}

		private static ObjectPool objectPool;
        //对象池 Object
		public static ObjectPool ObjectPool
		{
			get
			{
				if (objectPool != null)
				{
					return objectPool;
				}
				objectPool = new ObjectPool() { Name = "ClientM" };
				return objectPool;
			}
		}

        //热更
		private static Hotfix hotfix;

		public static Hotfix Hotfix
		{
			get
			{
				return hotfix ?? (hotfix = new Hotfix());
			}
		}

        //关闭回收
		public static void Close()
		{
			scene?.Dispose();
			scene = null;
			
			objectPool?.Dispose();
			objectPool = null;
			
			hotfix = null;
			
			eventSystem = null;
		}
	}
}