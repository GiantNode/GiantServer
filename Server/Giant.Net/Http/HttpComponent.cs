﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Giant.Core;
using Giant.Logger;
using Giant.Util;

namespace Giant.Net
{
    public class HttpComponent : Component, IInitSystem<int>
    {
        private HttpListener httpListener;
        private readonly Dictionary<string, MethodInfo> getMethodes = new();
        private readonly Dictionary<string, MethodInfo> postMethodes = new();
        private readonly Dictionary<MethodInfo, BaseHttpHandler> methodClassMap = new();

        public void Init(int port)
        {
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://*:{port}/");
                Log.Debug("Http listen port" + port);

                Load();

                httpListener.Start();
                AcceptAsync();
            }
            catch (HttpListenerException e)
            {
                if (e.ErrorCode == 5)
                {
                    throw new Exception($"CMD管理员中输入: netsh http add urlacl url=http://*:{port}/ user=Everyone", e);
                }
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        private async void AcceptAsync()
        {
            while (true)
            {
                var context = await httpListener.GetContextAsync();
                await DoContext(context);
                context.Response.Close();
            }
        }

        public void Load()
        {
            getMethodes.Clear();
            postMethodes.Clear();
            methodClassMap.Clear();

            HttpHandlerAttribute attribute;
            var types = Scene.EventSystem.GetTypes(typeof(HttpHandlerAttribute));
            if (types == null) return;

            foreach (var type in types)
            {
                attribute = type.GetCustomAttribute<HttpHandlerAttribute>();

                if (attribute == null) continue;

                if (!type.IsSubclassOf(typeof(BaseHttpHandler))) continue;

                if (string.IsNullOrEmpty(attribute.Path)) continue;

                var handler = Activator.CreateInstance(type) as BaseHttpHandler;
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    GetAttribute getAttribute = method.GetCustomAttribute<GetAttribute>();
                    if (getAttribute != null)
                    {
                        getMethodes.Add(attribute.Path + method.Name, method);
                    }

                    PostAttribute postAttribute = method.GetCustomAttribute<PostAttribute>();
                    if (postAttribute != null)
                    {
                        postMethodes.Add(attribute.Path + method.Name, method);
                    }

                    if (getAttribute != null || postAttribute != null)
                    {
                        methodClassMap.Add(method, handler);
                    }
                }
            }
        }

        private async Task DoContext(HttpListenerContext context)
        {
            try
            {
                string content = "";
                MethodInfo method = null;
                switch (context.Request.HttpMethod)
                {
                    case "GET":
                        getMethodes.TryGetValue(context.Request.Url.AbsolutePath, out method);
                        break;
                    case "POST":
                        postMethodes.TryGetValue(context.Request.Url.AbsolutePath, out method);
                        using (StreamReader reader = new(context.Request.InputStream))
                        {
                            content = await reader.ReadToEndAsync();
                        }
                        break;
                }

                if (method == null)
                {
                    Log.Error($"have not got method {context.Request.Url} path {context.Request.Url.AbsolutePath}");
                    return;
                }

                methodClassMap.TryGetValue(method, out BaseHttpHandler httpHandler);
                if (httpHandler == null)
                {
                    Log.Error($"have not got class {method.Name}");
                    return;
                }

                object[] args = InjectParameters(context, method, content);
                object result = method.Invoke(httpHandler, args);
                if (result is Task<HttpResult> task)
                {
                    await task;
                    result = task.Result;
                }

                if (result != null)
                {
                    using StreamWriter writer = new(context.Response.OutputStream);
                    writer.Write(result.ToJson());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        /// <summary>
        /// 注入参数
        /// </summary>
        /// <param name="context"></param>
        /// <param name="methodInfo"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        private static object[] InjectParameters(HttpListenerContext context, MethodInfo methodInfo, string content)
        {
            context.Response.StatusCode = 200;
            ParameterInfo[] parameterInfos = methodInfo.GetParameters();
            object[] args = new object[parameterInfos.Length];
            for (int i = 0; i < parameterInfos.Length; i++)
            {
                ParameterInfo item = parameterInfos[i];

                if (item.ParameterType == typeof(HttpListenerRequest))
                {
                    args[i] = context.Request;
                    continue;
                }

                if (item.ParameterType == typeof(HttpListenerResponse))
                {
                    args[i] = context.Response;
                    continue;
                }

                try
                {
                    switch (context.Request.HttpMethod)
                    {
                        case "POST":
                            if (item.Name == "content") // 约定参数名称为content,只传string类型。也可以是byte[]，有需求可以改。
                            {
                                args[i] = content;
                            }
                            else if (item.ParameterType.IsClass && item.ParameterType != typeof(string) && !string.IsNullOrEmpty(content))
                            {
                                object entity = content.FromJson(item.ParameterType);
                                args[i] = entity;
                            }
                            break;
                        case "GET":
                            string query = context.Request.QueryString[item.Name];
                            if (query != null)
                            {
                                object value = Convert.ChangeType(query, item.ParameterType);
                                args[i] = value;
                            }
                            break;
                        default:
                            args[i] = null;
                            break;
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                    args[i] = null;
                }
            }

            return args;
        }

    }
}
