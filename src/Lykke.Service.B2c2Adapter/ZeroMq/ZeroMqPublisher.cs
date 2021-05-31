using System;
using Common.Log;
using Lykke.Common.Log;
using NetMQ.Sockets;

namespace Lykke.Service.B2c2Adapter.ZeroMq
{
    public class ZeroMqPublisher : IDisposable
    {
        private readonly PublisherSocket _pubSocket;
        private bool _manualStop;
        private readonly ILog _logger;

        public ZeroMqPublisher(ILogFactory logFactory, string address)
        {
            _logger = logFactory.CreateLog(this);
            _pubSocket = new PublisherSocket();
            _pubSocket.Options.SendHighWatermark = 1000;
            _pubSocket.Bind(address);
        }

        public void Start(Action<PublisherSocket> send)
        {
            _logger.Info("0mq sending has been started");

            while (!_manualStop)
            {
                send(_pubSocket);
            }
        }
        
        public void Stop()
        {
            _logger.Info("0mq sending has been stopped.");

            _manualStop = true;
        }

        public void Dispose()
        {
            _pubSocket.Dispose();
        }
    }
}
