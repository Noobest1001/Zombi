using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.Events.EventArgs.Server;
using Exiled.Loader;
using MEC;
using PlayerRoles;
using Respawning;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Zombi
{
    internal sealed class EventHandlers
    {
        private Plugin plugin;
        public EventHandlers(Plugin plugin) => this.plugin = plugin;

        private int Respawns = 0;
        private int SHRespawns = 0;
        private CoroutineHandle calcuationCoroutine;

        public void OnRoundStarted()
        {
            plugin.IsSpawnable = false;
            Respawns = 0;
            SHRespawns = 0;

            if (calcuationCoroutine.IsRunning)
                Timing.KillCoroutines(calcuationCoroutine);

            calcuationCoroutine = Timing.RunCoroutine(spawnCalculation());
        }

        private IEnumerator<float> spawnCalculation()
        {
            while (true)
            {
                yield return Timing.WaitForSeconds(1f);

                if (Round.IsEnded)
                    break;

                if (Math.Round(Respawn.TimeUntilSpawnWave.TotalSeconds, 0) != plugin.Config.SpawnWaveCalculation)
                    continue;

                if (Respawn.NextKnownTeam == SpawnableTeamType.ChaosInsurgency)
                    plugin.IsSpawnable = Loader.Random.Next(100) <= plugin.Config.Zombi.SpawnChance && Respawns >= plugin.Config.Zombi.RespawnDelay && SHRespawns < plugin.Config.Zombi.MaxSpawns;
            }
        }

        public void OnRespawningTeam(RespawningTeamEventArgs ev)
        {
            if (plugin.IsSpawnable)
            {
                bool scpAlive = Player.List.Count(x => x.Role.Team == Team.SCPs) > 0;
                if (!scpAlive && !plugin.Config.Zombi.CanSpawnWithoutScps)
                    return;

                List<Player> players = new List<Player>();
                if (ev.Players.Count > plugin.Config.Zombi.MaxSquad)
                    players = ev.Players.GetRange(0, plugin.Config.Zombi.MaxSquad);
                else
                    players = ev.Players.GetRange(0, ev.Players.Count);

                foreach (Player player in players)
                {
                    if (player is null)
                        continue;
                    plugin.Config.Zombi.AddRole(player);
                }
                SHRespawns++;
                if (!string.IsNullOrEmpty(plugin.Config.Zombi.EntryAnnoucement))
                    Cassie.Message(plugin.Config.Zombi.EntryAnnoucement, isSubtitles: plugin.Config.Zombi.Subtitles);

                if (plugin.Config.Zombi.EntryBroadcast.Duration > 0 || !string.IsNullOrEmpty(plugin.Config.Zombi.EntryBroadcast.Content))
                    foreach (Player player in Player.List.Where(x => x.Role.Team == Team.SCPs))
                        player.Broadcast(plugin.Config.Zombi.EntryBroadcast);

                plugin.IsSpawnable = false;
                ev.IsAllowed = false;
                ev.NextKnownTeam = SpawnableTeamType.None;
            }

            Respawns++;
        }

        public void OnEndingRound(EndingRoundEventArgs ev)
        {
            bool mtfAlive = false;
            bool ciAlive = false;
            bool scpAlive = false;
            bool dclassAlive = false;
            bool scientistsAlive = false;
            bool shAlive = plugin.Config.Zombi.TrackedPlayers.Count > 0;

            foreach (Player player in Player.List)
            {
                switch (player.Role.Team)
                {
                    case Team.FoundationForces:
                        mtfAlive = true;
                        break;
                    case Team.ChaosInsurgency:
                        ciAlive = true;
                        break;
                    case Team.SCPs:
                        scpAlive = true;
                        break;
                    case Team.ClassD:
                        dclassAlive = true;
                        break;
                    case Team.Scientists:
                        scientistsAlive = true;
                        break;
                }

                if ((shAlive && ((ciAlive && !plugin.Config.Zombi.ScpsWinWithChaos) || dclassAlive || mtfAlive || scientistsAlive))
                    || (shAlive && scpAlive && !mtfAlive && !dclassAlive && !scientistsAlive)
                    || ((shAlive || scpAlive) && ciAlive && !plugin.Config.Zombi.ScpsWinWithChaos))
                    break;
            }

            if (shAlive && ((ciAlive && !plugin.Config.Zombi.ScpsWinWithChaos) || dclassAlive || mtfAlive || scientistsAlive))
                ev.IsRoundEnded = false;
            else if (shAlive && scpAlive && !mtfAlive && !dclassAlive && !scientistsAlive)
            {
                if (!plugin.Config.Zombi.ScpsWinWithChaos)
                {
                    if (!ciAlive)
                    {
                        ev.LeadingTeam = LeadingTeam.Anomalies;
                        ev.IsRoundEnded = true;
                    }
                }
                else
                {
                    ev.LeadingTeam = LeadingTeam.Anomalies;
                    ev.IsRoundEnded = true;
                }
            }
            else if ((shAlive || scpAlive) && ciAlive && !plugin.Config.Zombi.ScpsWinWithChaos)
                ev.IsRoundEnded = false;
        }
    }
}
