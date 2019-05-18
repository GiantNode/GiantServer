﻿using Giant.Log;
using Giant.Net;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Giant.Frame
{
    public class BaseService
    {
        delegate bool ControlCtrlHandle(int ctrlType);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlHandle HandlerRoutine, bool Add);
        private static readonly ControlCtrlHandle cancelHandler = new ControlCtrlHandle(HandleMathord);

        private static bool HandleMathord(int ctrlType)
        {
            switch (ctrlType)
            {
                case 0:
                    Logger.Warn("无法使用 Ctrl+C 强制关闭窗口"); //Ctrl+C关闭
                    return true;
                case 2:
                    Logger.Warn("工具被强制关闭");//按控制台关闭按钮关闭
                    return true;
            }

            return false;
        }

        public NetworkService NetworkService { get; protected set; }



        public virtual void Init()
        {
            SetConsoleCtrlHandler(cancelHandler, true);

            // 异步方法全部会回掉到主线程
            SynchronizationContext.SetSynchronizationContext(OneThreadSynchronizationContext.Instance);

            //框架的各种初始化工作

        }

        public virtual void Update()
        {
            try
            {
                OneThreadSynchronizationContext.Instance.Update();

                this.NetworkService.Update();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// 自动注册消息处理调用
        /// </summary>
        protected void BindResponser(Assembly assembly)
        {
            Type handlerType = typeof(MessageHandlerAttribute);
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                Attribute attribute = type.GetCustomAttribute(handlerType);
                if (attribute == null)
                {
                    continue;
                }

                IMHandler handler = Activator.CreateInstance(type) as IMHandler;

                this.NetworkService.MessageDispatcher.RegisterHandler(handler);
            }
        }

    }
}
