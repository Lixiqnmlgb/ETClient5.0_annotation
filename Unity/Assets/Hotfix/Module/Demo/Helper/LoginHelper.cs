using System;
using ETModel;

namespace ETHotfix
{
    public static class LoginHelper
    {
        public static async ETVoid OnLoginAsync(string account)
        {
            try
            {
                //创建一个会话实体session
                ETModel.Session session = ETModel.Game.Scene.GetComponent<NetOuterComponent>().Create(GlobalConfigComponent.Instance.GlobalProto.Address);

                //热更层也创建一个Seesion,将Model层的session传递过去
                //热更层的Seesion创建后,会调用Awake方法,在内部关联了Model层的session
                //以后调用热更层的Seesion 就是调用间接的调用了主工程的 Seesion
                Session realmSession = ComponentFactory.Create<Session, ETModel.Session>(session);
                //await等待服务器响应 r2CLogin这个是响应后 解包->反序列化得到的对象 里面已经包含服务器发送过来的数据
                R2C_Login r2CLogin = (R2C_Login) await realmSession.Call(new C2R_Login() { Account = account, Password = "111111" });
                realmSession.Dispose();

                //服务器返回了网关地址
                //那么就根据网关地址创建一个新的Session 连接到网关去
                ETModel.Session gateSession = ETModel.Game.Scene.GetComponent<NetOuterComponent>().Create(r2CLogin.Address);
                //在Scene实体中添加SessionComponent组件 并且缓存Session对象 以后就可以直接获取来发送消息
                ETModel.Game.Scene.AddComponent<ETModel.SessionComponent>().Session = gateSession;

                //这里跟上面逻辑一样,创建热更层的Session,关联到主工程
                Game.Scene.AddComponent<SessionComponent>().Session = ComponentFactory.Create<Session, ETModel.Session>(gateSession);
                G2C_LoginGate g2CLoginGate = (G2C_LoginGate)await SessionComponent.Instance.Session.Call(new C2G_LoginGate() { Key = r2CLogin.Key });
                Log.Info("登陆gate成功!");

                // 创建Player
                Player player = ETModel.ComponentFactory.CreateWithId<Player>(g2CLoginGate.PlayerId);
                PlayerComponent playerComponent = ETModel.Game.Scene.GetComponent<PlayerComponent>();
                playerComponent.MyPlayer = player;

                Game.EventSystem.Run(EventIdType.LoginFinish);

                // 测试消息有成员是class类型
                G2C_PlayerInfo g2CPlayerInfo = (G2C_PlayerInfo) await SessionComponent.Instance.Session.Call(new C2G_PlayerInfo());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        } 
    }
}