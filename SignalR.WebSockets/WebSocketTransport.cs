using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Web.WebSockets;

namespace SignalR.Transports {
    public class WebSocketTransport : WebSocketHandler, ITransport {
        private readonly HttpContextBase _context;
        private readonly IJsonStringifier _stringifier;
        private readonly CancellationTokenSource _connectionTokenSource;
        private IConnection _connection;

        public WebSocketTransport(HttpContextBase context, IJsonStringifier stringifier) {
            _context = context;
            _stringifier = stringifier;
            _connectionTokenSource = new CancellationTokenSource();
        }

        public event Action<string> Received;

        public event Action Connected;

        public event Action Disconnected;

        public new event Action<Exception> Error;

        public Func<Task> ProcessRequest(IConnection connection) {
            // Never time out for websocket requests
            connection.ReceiveTimeout = TimeSpan.FromTicks(Int32.MaxValue - 1);

            _context.AcceptWebSocketRequest(this);

            _connection = connection;

            return () => Task.FromResult<object>(null);
        }

        public override void OnOpen() {
            if (Connected != null) {
                Connected();
            }

            ProcessMessages(_connection);
        }

        public override void OnMessage(string message) {
            if (Received != null) {
                Received(message);
            }
        }

        public override void OnClose() {
            if (Disconnected != null) {
                Disconnected();
            }

            _connectionTokenSource.Cancel();
        }

        public override void OnError() {
            if (Error != null) {
                Error(base.Error);
            }
        }

        public void Send(object value) {
            base.Send(_stringifier.Stringify(value));
        }

        private async Task ProcessMessages(IConnection connection) {
            while (!_connectionTokenSource.IsCancellationRequested) {
                PersistentResponse response = await connection.ReceiveAsync();
                Send(response);
            }
        }
    }
}
