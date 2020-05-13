using System;
using Topshelf;

namespace Covid.UserService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<OldRabbitService>();
                x.EnableServiceRecovery(r => r.RestartService(TimeSpan.FromSeconds(10)));
                x.SetServiceName("OldRabbitService");
                x.StartAutomatically();
            });
        }
    }
}
