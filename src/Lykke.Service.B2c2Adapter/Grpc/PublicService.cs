using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lykke.Service.B2c2Adapter.Settings;
using Swisschain.Liquidity.ApiContract;

namespace Lykke.Service.B2c2Adapter.Grpc
{
    public class PublicService : PublicGrpc.PublicGrpcBase
    {
        private readonly B2c2AdapterSettings _settings;

        public PublicService(B2c2AdapterSettings settings)
        {
            _settings = settings;
        }
        public override async Task<VenueNameResponse> GetVenueName(Empty request, ServerCallContext context)
        {
            return new() {Name = _settings.VenueName};
        }
    }
}
