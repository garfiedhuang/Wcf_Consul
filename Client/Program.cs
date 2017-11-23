using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Linq;
using System.ServiceModel;
using Contracts;

namespace Client
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            DateTime dt1 = System.DateTime.Now;
            for (int i = 0; i < 10000; i++)
            {
                RunClientTest(i);
                Thread.Sleep(1000);
            }
            DateTime dt2 = System.DateTime.Now;
            TimeSpan ts = dt2.Subtract(dt1);
            Console.WriteLine("example1 time {0}", ts.TotalMilliseconds);
            Console.ReadLine();
        }

        public static Consul.AgentService FindServiceEndpoint(string name)
        {
            Consul.ConsulClient _client = new Consul.ConsulClient();
            var res = _client.Agent.Services().Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                throw new ApplicationException($"Failed to query services");
            }

            var rnd = new Random();
            var now = DateTime.UtcNow;
            var targets = res.Response
                             .Values
                             .Where(x => x.Service == name)
                             .ToList();
            while (0 < targets.Count)
            {
                var choice = rnd.Next(targets.Count);
                var target = targets[choice];
                return target;
            }
            throw new ApplicationException($"Can't find service {name}");
        }

        private static void RunClientTest(int i)
        {
            var agentService = FindServiceEndpoint("mywcf");
            EndpointAddress edpHttp = new EndpointAddress(agentService.ID);
            WSHttpBinding httpBinding = new WSHttpBinding(SecurityMode.None);
            ChannelFactory<ITest> factory = new ChannelFactory<ITest>(httpBinding);
            ITest iService = factory.CreateChannel(edpHttp);
            try
            {
                var reply = iService.GetData(i);
                Console.WriteLine($"Success attempt: {i} thread: {Thread.CurrentThread.ManagedThreadId} reply:{reply} from port:{agentService.Port}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failure attempt: {i} thread: {Thread.CurrentThread.ManagedThreadId} error: {ex.Message}");
            }
        }
    }
}