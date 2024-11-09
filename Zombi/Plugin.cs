using Exiled.API.Features;
using Exiled.CustomRoles.API;
using Exiled.CustomRoles.API.Features;
using System;
using Server = Exiled.Events.Handlers.Server;

namespace SerpentsHand
{
    public class Plugin : Plugin<Config>
    {
        public override string Name => "Get Third partied lol";
        public override string Author => "Noobest1001 (Oliver)";
        public override Version RequiredExiledVersion => new Version(8, 3, 0);
        public override Version Version => new Version(0, 0, 1);

        public bool IsSpawnable = false;

        private EventHandlers eventHandlers;

        public override void OnEnabled()
        {
            Config.SerpentsHand.Register();
            eventHandlers = new EventHandlers(this);

            Server.RoundStarted += eventHandlers.OnRoundStarted;
            Server.RespawningTeam += eventHandlers.OnRespawningTeam;
            Server.EndingRound += eventHandlers.OnEndingRound;
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            CustomRole.UnregisterRoles();
            Server.RoundStarted -= eventHandlers.OnRoundStarted;
            Server.RespawningTeam -= eventHandlers.OnRespawningTeam;
            Server.EndingRound -= eventHandlers.OnEndingRound;

            eventHandlers = null;
            base.OnDisabled();
        }
    }
}
