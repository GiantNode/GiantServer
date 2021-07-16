﻿using CommandLine;
using Giant.Core;
using Giant.DB;
using Giant.EnumUtil;
using Giant.Model;
using Giant.Net;
using Giant.Redis;
using System;
using System.Reflection;
using System.Threading;

namespace Giant.Framework
{
    public class AppComponent : InitSystem<string[]>
    {
        public override void Init(string[] args)
        {
            AppOption appOption = null;
            Parser.Default.ParseArguments<AppOption>(args)
                .WithNotParsed(error => throw new Exception("CommandLine param error !"))
                .WithParsed(options => { appOption = options; });

            IdGenerator.AppId = appOption.AppId;
            Logger.Log.Init(true, appOption.AppType.ToString(), appOption.AppId, appOption.SubId);

            //注册Event
            Scene.EventSystem.Add(Assembly.GetEntryAssembly());
            Scene.EventSystem.Add(typeof(NetProxyComponent).Assembly);

            // 异步方法全部会回掉到主线程
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);

            //xml表
            Scene.Pool.AddComponent<DataComponent>();
            Scene.Pool.AddComponent<AppConfigDataList>();
            Scene.Pool.AddComponent<NetGraphDataList>();

            Scene.AppConfig = Scene.Pool.GetComponent<AppConfigDataList>().GetNetConfig(appOption.AppType);

            Scene.Pool.AddComponent<KeyboardInputEventComponent>();
            Scene.Pool.AddComponent<OpcodeComponent>();
            Scene.Pool.AddComponent<MessageDispatcherComponent>();
            Scene.Pool.AddComponent<TimerComponent>();
            Scene.Pool.AddComponent<NetProxyComponent>();
            Scene.Pool.AddComponent<ConsoleComponent>();

            DBConfigDataList component = Scene.Pool.AddComponent<DBConfigDataList>();
            if (Scene.AppConfig.AppType.NeedDBService())
            {
                Scene.Pool.AddComponent<DBServiceComponent, DBType, DBConfig>(DBType.MongoDB, component.DBConfig);
            }

            //Redis
            if (Scene.AppConfig.AppType.NeedRedisServer())
            {
                Scene.Pool.AddComponent<RedisComponent, RedisConfig>(component.RedisConfig);
            }

            //网络
            if (!string.IsNullOrEmpty(Scene.AppConfig.InnerAddress))
            {
                Scene.Pool.AddComponent<InnerNetComponent, NetworkType, string>(NetworkType.Tcp, Scene.AppConfig.InnerAddress);
            }
            if (!string.IsNullOrEmpty(Scene.AppConfig.OutterAddress))
            {
                Scene.Pool.AddComponent<OuterNetComponent, NetworkType, string>(NetworkType.Tcp, Scene.AppConfig.OutterAddress);
            }
        }
    }
}
