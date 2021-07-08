﻿using Giant.Core;
using Giant.EnumUtil;
using Giant.Logger;
using Giant.Msg;
using Giant.Net;
using System.Threading.Tasks;

namespace Giant.Framework
{
    public class FrontendComponent : BaseServerComponent, IInitSystem<AppConfig>
    {
        public AppConfig AppConfig { get; private set; }

        public void Init(AppConfig appConfig)
        {
            AppConfig = appConfig;

            Scene.EventSystem.Handle(EventType.InitFrontend, this);
        }

        public void Start()
        {
            Session?.Dispose();
            Session = Scene.Pool.GetComponent<InnerNetComponent>().Create(AppConfig.InnerAddress);
            Session.OnConnectCallback += OnConnected;
            Session.Start();
        }

        public void Send(IMessage message)
        {
            Session.Notify(message);
        }

        public Task<IResponse> Call(IRequest request)
        {
            return Session.Call(request);
        }

        public override void Dispose()
        {
            base.Dispose();
            Session?.Dispose();
        }

        private void OnConnected(Session session, bool connState)
        {
            if (IsDisposed) return;

            if (connState)
            {
                RegistService();

                //连接上之后添加心跳
                RemoveComponent<HeartBeatComponent>();
                AddComponent<HeartBeatComponent, Session, int>(Session, 20);
            }
            else
            {
                Reconnect();
            }
        }

        private async void Reconnect()
        {
            await Task.Delay(3000);//3后重新连接
            Log.Warn($"app {Scene.AppConfig.AppType} {Scene.AppConfig.AppId} {Scene.AppConfig.SubId} connect to {AppConfig.AppType} {AppConfig.AppId} {Session.RemoteIPEndPoint}");

            if (IsDisposed) return;

            Session.Connect();
        }

        private async void RegistService()
        {
            Log.Warn($"app {Scene.AppConfig.AppType} {Scene.AppConfig.AppId} {Scene.AppConfig.SubId} start registe to {AppConfig.AppType} {AppConfig.AppId} ...");

            Msg_RegistService_Req request = new Msg_RegistService_Req()
            {
                AppId = Scene.AppConfig.AppId,
                SubId = Scene.AppConfig.SubId,
                AppType = (int)Scene.AppConfig.AppType,
            };

            IResponse response = await Session.Call(request);
            Msg_RegistService_Rep message = response as Msg_RegistService_Rep;

            Log.Warn($"app {Scene.AppConfig.AppType} {Scene.AppConfig.AppId} {Scene.AppConfig.SubId} registed to {(AppType)message.AppType} {message.AppId} success !");
        }
    }
}
