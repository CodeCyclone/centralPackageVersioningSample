using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Serilog;

namespace GrpcClient
{
    public class HealthClient
    {
        private string ConnectionUrl;

        private ILogger Logger { get; }

        public HealthClient(string connectionUrl, Serilog.ILogger logger = null) 
        {
            this.Logger = logger;
            if (string.IsNullOrEmpty(connectionUrl))
            {
#if DEBUG
                connectionUrl = "https://localhost:5001";
#else
                connectionUrl = "https://notarealaddress.azurewebsites.net";
#endif
            }

            this.ConnectionUrl = connectionUrl;
            this.Logger?.Debug("HealthClient connection url set to {ConnectionUrl}", this.ConnectionUrl);
        }


        public async Task <Health.V1.HealthStatus> GetServerHealthStatus(bool checkDbStatus = false)
        {
            using var channel = this.CreateChannel();
            var client = new Health.V1.Health.HealthClient(channel);

            try
            {
                this.Logger?.Verbose("Sending Health Status request to server");
                var response = await client.CheckHealthAsync(new Health.V1.HealthCheckRequest() { CheckDatabaseStatus = checkDbStatus });
                return response;
            }
            catch (Exception ex)
            {
                if (this.Logger == null)
                {
                    Console.WriteLine(ex);
                }
                else
                {
                    this.Logger?.Error(ex, "Failed sending get server health status request");
                }

                throw;
            }
        }

        private GrpcChannel CreateChannel()
        {
            // return GrpcChannel.ForAddress(this.ConnectionUrl); // Uncomment once App Service supports HTTP2 and we move off Grpc.Net.Client.Web
            // https://docs.microsoft.com/en-us/aspnet/core/grpc/browser?view=aspnetcore-5.0#configure-grpc-web-with-the-net-grpc-client
            return GrpcChannel.ForAddress(this.ConnectionUrl, new GrpcChannelOptions
            {
                HttpHandler = new GrpcWebHandler(new HttpClientHandler())
            });
        }
    }
}
