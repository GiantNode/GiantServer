﻿using Giant.Data;
using Giant.Log;
using Giant.Msg;
using Giant.Net;
using Giant.Share;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Server.Frame
{
    public class FrontendServer : BaseServer
    {
        private CancellationTokenSource cancellation;
        private long lastHeatBeatTime = TimeHelper.NowSeconds;

        public AppConfig AppConfig { get; private set; }
        public FrontendServerManager FrontendManager { get; private set; }
        public bool IsConnected => Session != null && Session.IsConnected;

        public FrontendServer(FrontendServerManager manager, AppConfig appConfig)
        {
            this.AppConfig = appConfig;
            this.FrontendManager = manager;
        }

        public void Start()
        {
            Session = Framework.InnerNetworkService.Create(AppConfig.InnerAddress);
            Session.OnConnectCallback += OnConnected;
            Session.Start();
        }

        public virtual void Update()
        {
            try
            {
                CheckHeartBeat();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Send(IMessage message)
        {
            Session.Notify(message);
        }

        private void CheckHeartBeat()
        {
            if (!IsConnected)
            {
                return;
            }

            if (TimeHelper.NowSeconds - lastHeatBeatTime < 30)
            {
                return;
            }

            HeartBeat();
            lastHeatBeatTime = TimeHelper.NowSeconds;
        }

        private async void HeartBeat()
        {
            Msg_HeartBeat_Ping ping = new Msg_HeartBeat_Ping
            {
                AppType = (int)Framework.AppType,
                AppId = Framework.AppId,
                SubId = Framework.SubId,
            };

            cancellation?.Cancel();
            cancellation = new CancellationTokenSource(3000);

            if (await Session.Call(ping, cancellation.Token) is Msg_HeartBeat_Pong message)
            {
                Logger.Info($"heart beat pong from appType {(AppType)message.AppType} appId {message.AppId} subId {message.SubId}");
            }

            cancellation.Dispose();
            cancellation = null;
        }

        private void OnConnected(Session session, bool connState)
        {
            if (connState)
            {
                RegistService();
            }
            else
            {
                CheckConnect();
            }
        }

        private async void CheckConnect()
        {
            await Task.Delay(3000);//3后重新连接
            Logger.Warn($"app {Framework.AppType} {Framework.AppId} {Framework.SubId} connect to {AppConfig.AppType} {AppConfig.AppId} {Session.RemoteIPEndPoint}");

            this.Start();
        }

        private async void RegistService()
        {
            Logger.Warn($"app {Framework.AppType} {Framework.AppId} {Framework.SubId} start registe to {AppConfig.AppType} {AppConfig.AppId} ...");

            Msg_RegistService_Req request = new Msg_RegistService_Req()
            {
                AppId = Framework.AppId,
                SubId = Framework.SubId,
                AppType = (int)Framework.AppType,
            };

            IResponse response = await Session.Call(request);
            Msg_RegistService_Rep message = response as Msg_RegistService_Rep;

            Logger.Warn($"app {Framework.AppType} {Framework.AppId} {Framework.SubId} registed to {(AppType)message.AppType} {message.AppId} success !");
        }
    }
}