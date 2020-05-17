using System;
using System.Threading;
using UnityEngine;

namespace ETModel
{
	public class Init : MonoBehaviour
	{
		private void Start()
		{
			this.StartAsync().Coroutine();
		}
		
		private async ETVoid StartAsync()
		{
			try
			{
                //上下文同步
				SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);

                //禁止销毁 在切换场景的时候
				DontDestroyOnLoad(gameObject);

                //遍历Model的程序集 缓存各个加了特性标签的对象
				Game.EventSystem.Add(DLLType.Model, typeof(Init).Assembly);

				Game.Scene.AddComponent<TimerComponent>();//计时器
				Game.Scene.AddComponent<GlobalConfigComponent>();//全局配置
				Game.Scene.AddComponent<NetOuterComponent>();//外网组件 提供创建网络通道 在每个通道内部维护消息是收发处理
				Game.Scene.AddComponent<ResourcesComponent>();//资源组件 提供AB加载
				Game.Scene.AddComponent<PlayerComponent>();//玩家组件 提供玩家管理
				Game.Scene.AddComponent<UnitComponent>();//单位组件 它的实体加了HideInHierarchy 表示在层级视图中隐藏
                Game.Scene.AddComponent<UIComponent>();//UI组件 

				// 下载ab包
				await BundleHelper.DownloadBundle();

                //加载热更新代码
				Game.Hotfix.LoadHotfixAssembly();

				// 加载资源配置
				Game.Scene.GetComponent<ResourcesComponent>().LoadBundle("config.unity3d");
				Game.Scene.AddComponent<ConfigComponent>();
				UnitConfig unit=(UnitConfig)Game.Scene.GetComponent<ConfigComponent>().Get(typeof(UnitConfig), 1);
				Game.Scene.GetComponent<ResourcesComponent>().UnloadBundle("config.unity3d");
                //操作码 协议号
				Game.Scene.AddComponent<OpcodeTypeComponent>();
                //消息分发
				Game.Scene.AddComponent<MessageDispatcherComponent>();
                //初始化IL
				Game.Hotfix.GotoHotfix();
                //测试热修复订阅事件
                Game.EventSystem.Run(EventIdType.TestHotfixSubscribMonoEvent, "TestHotfixSubscribMonoEvent");
			}
			catch (Exception e)
			{
				Log.Error(e);
			}
		}

        //每帧更新 热更dll的事件 与 游戏系统本身的事件
		private void Update()
		{
			OneThreadSynchronizationContext.Instance.Update();
            //?.是否为空的判断 不为空则:热更新中函数不等于空就执行
            Game.Hotfix.Update?.Invoke();
            //事件系统
			Game.EventSystem.Update();
		}

        //固定更新 热更dll的事件 与 游戏系统本身的事件
        private void LateUpdate()
		{
			Game.Hotfix.LateUpdate?.Invoke();
			Game.EventSystem.LateUpdate();
		}

        //游戏退出 热更dll的事件 与 游戏系统本身的事件
        private void OnApplicationQuit()
		{
			Game.Hotfix.OnApplicationQuit?.Invoke();
			Game.Close();
		}
	}
}