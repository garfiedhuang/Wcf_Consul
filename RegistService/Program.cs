using System;
using System.Threading.Tasks;
using Consul;
using System.Net;
using Contracts;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Runtime.InteropServices;

namespace Server
{
    public static class ConsulConfig
    {
        public static readonly TimeSpan BlacklistPeriod = TimeSpan.FromMinutes(2);
        public static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan CriticalInterval = TimeSpan.FromSeconds(30);
    }

    public class Service : ITest
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }
    }

    public class ServerEntity : IDisposable
    {
        private  string serviceId = null;
        private  ConsulClient _client = new ConsulClient();
        public void RegistServer(int port)
        {
            var hostName = "127.0.0.1";
            serviceId = $"http://{hostName}:{port}/Service/";
            var checkId = $"http://{hostName}:{port}";
            var name = "mywcf";
            var acr = new AgentCheckRegistration
            {
                HTTP = serviceId,
                Name = checkId,
                ID = checkId,
                Interval = ConsulConfig.CheckInterval,
                DeregisterCriticalServiceAfter = ConsulConfig.CriticalInterval
            };

            var asr = new AgentServiceRegistration
            {
                Address = hostName,
                ID = serviceId,
                Name = name,
                Port = port,
                Check = acr
            };

            var res = _client.Agent.ServiceRegister(asr).Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Failed to register service {name} on port {port}");
            }
            Console.ReadLine();
        }


        public void UnregisterService()
        {
            var res = _client.Agent.ServiceDeregister(serviceId).Result;
        }

        public void Dispose()
        {
            UnregisterService();
        }

    }

    internal class Program
    {

        public delegate bool ControlCtrlDelegate(int CtrlType);
        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleCtrlHandler(ControlCtrlDelegate HandlerRoutine, bool Add);
        private static ControlCtrlDelegate cancelHandler = new ControlCtrlDelegate(HandlerRoutine);
        public static bool HandlerRoutine(int CtrlType)
        {
            switch (CtrlType)
            {
                case 0:
                    Console.WriteLine("0工具被强制关闭"); //Ctrl+C关闭  
                    break;
                case 2:
                    server.Dispose();
                    break;
            }
            Console.ReadLine();
            return false;
        }

        public static ServerEntity server = new ServerEntity();
        public static void Main(string[] args)
        {
            SetConsoleCtrlHandler(cancelHandler, true);
            int port = 0;
            if (args.Length == 0)
            {
                Console.WriteLine("输入端口号参数");
                port = int.Parse(Console.ReadLine());
            }
            else
            {
                port = int.Parse(args[0]);
            }
            Uri url = new Uri($"http://127.0.0.1:{port}/Service/");
            ServiceHost host = new ServiceHost(typeof(Service), url);
            // HTTP方式  
            WSHttpBinding httpBinding = new WSHttpBinding(SecurityMode.None);
            host.AddServiceEndpoint(typeof(ITest), httpBinding, $"http://127.0.0.1:{port}/Service/");
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
            host.Description.Behaviors.Add(smb);
            // 打开服务  
            host.Open();
            Console.WriteLine("服务已启动。");
            server.RegistServer(port);
        }

    }
}