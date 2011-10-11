using System.Web;
using SignalR.Infrastructure;
using SignalR.Transports;

[assembly: PreApplicationStartMethod(typeof(SignalR.WebStart.PreApplicationStart), "Start")]

namespace SignalR.WebStart {
    public static class PreApplicationStart {
        public static void Start() {            
            TransportManager.Register("webSockets", context => new WebSocketTransport(context, DependencyResolver.Resolve<IJsonStringifier>()));
        }
    }
}