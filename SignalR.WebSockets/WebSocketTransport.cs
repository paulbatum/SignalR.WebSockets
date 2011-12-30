using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Web.WebSockets;
using SignalR.Abstractions;

namespace SignalR.Transports
{
    public class WebSocketTransport : WebSocketHandler, ITransport
    {
        private readonly HostContext _context;
        private readonly IJsonSerializer _stringifier;
        private readonly CancellationTokenSource _connectionTokenSource;
        private IReceivingConnection _connection;

        public WebSocketTransport(HostContext context, IJsonSerializer stringifier)
        {
            _context = context;
            _stringifier = stringifier;
            _connectionTokenSource = new CancellationTokenSource();
        }

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public new event Action<Exception> Error;

        public Func<Task> ProcessRequest(IReceivingConnection connection)
        {
            // Never time out for websocket requests            
            connection.ReceiveTimeout = TimeSpan.FromTicks(Int32.MaxValue - 1);

            var httpContext = (HttpContextBase)_context.Items["aspnet.HttpContext"];            
            httpContext.AcceptWebSocketRequest(this);

            _connection = connection;

            return () => Task.FromResult<object>(null);
        }

        public override void OnOpen()
        {
            if (Connected != null)
            {
                Connected();
            }

            ProcessMessages(_connection);
        }

        public override void OnMessage(string message)
        {
            if (Received != null)
            {
                Received(message);
            }
        }

        public override void OnClose()
        {
            if (Disconnected != null)
            {
                Disconnected();
            }

            _connectionTokenSource.Cancel();
        }

        public override void OnError()
        {
            if (Error != null)
            {
                Error(base.Error);
            }
        }

        public Task Send(object value)
        {            
            base.Send(_stringifier.Stringify(value));
            return Task.FromResult<object>(null);
        }

        private async Task ProcessMessages(IReceivingConnection connection)
        {
            long? lastMessageId = null;
            while (!_connectionTokenSource.IsCancellationRequested)
            {
                PersistentResponse response = lastMessageId == null ? await connection.ReceiveAsync() : await connection.ReceiveAsync(lastMessageId.Value);
                lastMessageId = response.MessageId;
                Send(response);
            }
        }


        public string ConnectionId
        {
            get
            {
                return _context.Request.QueryString["connectionId"];
            }
        }

        public IEnumerable<string> Groups
        {
            get
            {
                string groupValue = _context.Request.QueryString["groups"];

                if (String.IsNullOrEmpty(groupValue))
                {
                    return Enumerable.Empty<string>();
                }

                return groupValue.Split(',');
            }
        }
    }
}
