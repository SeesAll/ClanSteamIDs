using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("ClanSteamIDs", "SeesAll", "1.0.3")]
    [Description("Provides SteamID lookup for individual players and full clan rosters using the Clans plugin.")]
    public class ClanSteamIDs : RustPlugin
    {
        [PluginReference] private Plugin Clans;

        private const string PermPlayer = "clansteamids.player";
        private const string PermClan = "clansteamids.clan";

        private void Init()
        {
            permission.RegisterPermission(PermPlayer, this);
            permission.RegisterPermission(PermClan, this);
        }

        [ChatCommand("playersteamid")]
        private void ChatCmdPlayerSteamId(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, PermPlayer))
            {
                ReplyToPlayer(player, "You do not have permission to use this command.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                ReplyToPlayer(player, "Usage: /playersteamid <player name or steamid>");
                return;
            }

            HandlePlayerLookup(string.Join(" ", args), message => ReplyToPlayer(player, message));
        }

        [ChatCommand("clanallsteamid")]
        private void ChatCmdClanAllSteamId(BasePlayer player, string command, string[] args)
        {
            if (!HasPermission(player, PermClan))
            {
                ReplyToPlayer(player, "You do not have permission to use this command.");
                return;
            }

            if (args == null || args.Length == 0)
            {
                ReplyToPlayer(player, "Usage: /clanallsteamid <clan tag>");
                return;
            }

            HandleClanLookup(args[0], message => ReplyToPlayer(player, message));
        }

        [ConsoleCommand("playersteamid")]
        private void ConsoleCmdPlayerSteamId(ConsoleSystem.Arg arg)
        {
            if (!HasConsoleOrPlayerPermission(arg, PermPlayer))
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                ReplyToArg(arg, "Usage: playersteamid <player name or steamid>");
                return;
            }

            HandlePlayerLookup(string.Join(" ", arg.Args), message => ReplyToArg(arg, message));
        }

        [ConsoleCommand("clanallsteamid")]
        private void ConsoleCmdClanAllSteamId(ConsoleSystem.Arg arg)
        {
            if (!HasConsoleOrPlayerPermission(arg, PermClan))
                return;

            if (arg.Args == null || arg.Args.Length == 0)
            {
                ReplyToArg(arg, "Usage: clanallsteamid <clan tag>");
                return;
            }

            HandleClanLookup(arg.Args[0], message => ReplyToArg(arg, message));
        }

        private void HandlePlayerLookup(string input, Action<string> reply)
        {
            PlayerLookupResult lookup = FindPlayerByNameOrId(input);
            if (!lookup.Success)
            {
                reply(lookup.Message);
                return;
            }

            BasePlayer target = lookup.Player;
            string clanTag = GetClanOf(target.UserIDString);
            string clanText = string.IsNullOrEmpty(clanTag) ? "None" : clanTag;

            reply($"Player: {target.displayName} | SteamID: {target.UserIDString} | Clan: {clanText}");
        }

        private void HandleClanLookup(string tag, Action<string> reply)
        {
            if (Clans == null)
            {
                reply("Clans plugin not found.");
                return;
            }

            ClanInfo clanInfo = GetClanInfoByTag(tag);
            if (clanInfo == null)
            {
                reply($"Clan '{tag}' was not found, or the installed Clans plugin did not return usable clan data.");
                return;
            }

            if (clanInfo.MemberIds.Count == 0)
            {
                reply($"Clan [{clanInfo.Tag}] has no members.");
                return;
            }

            reply($"Clan [{clanInfo.Tag}] SteamIDs ({clanInfo.MemberIds.Count} members):");

            clanInfo.MemberIds.Sort(StringComparer.Ordinal);

            for (int i = 0; i < clanInfo.MemberIds.Count; i++)
            {
                string memberId = clanInfo.MemberIds[i];
                string name = ResolvePlayerName(memberId);
                reply($"{name} - {memberId}");
            }
        }

        private string GetClanOf(string playerId)
        {
            if (Clans == null || string.IsNullOrEmpty(playerId))
                return null;

            return Clans.Call("GetClanOf", playerId) as string;
        }

        private ClanInfo GetClanInfoByTag(string tag)
        {
            if (Clans == null || string.IsNullOrWhiteSpace(tag))
                return null;

            object result = Clans.Call("GetClan", tag);
            if (result == null)
                return null;

            JObject clanObject = result as JObject;
            if (clanObject == null)
            {
                try
                {
                    clanObject = JObject.FromObject(result);
                }
                catch
                {
                    return null;
                }
            }

            if (clanObject == null)
                return null;

            string resolvedTag = clanObject["tag"]?.ToString();
            if (string.IsNullOrWhiteSpace(resolvedTag))
                resolvedTag = tag;

            JArray membersArray = clanObject["members"] as JArray;
            List<string> memberIds = new List<string>();
            HashSet<string> uniqueIds = new HashSet<string>(StringComparer.Ordinal);

            if (membersArray != null)
            {
                for (int i = 0; i < membersArray.Count; i++)
                {
                    string memberId = membersArray[i]?.ToString();
                    if (string.IsNullOrWhiteSpace(memberId))
                        continue;

                    if (uniqueIds.Add(memberId))
                        memberIds.Add(memberId);
                }
            }

            return new ClanInfo
            {
                Tag = resolvedTag,
                MemberIds = memberIds
            };
        }

        private PlayerLookupResult FindPlayerByNameOrId(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return PlayerLookupResult.Fail("No player name or SteamID was provided.");

            input = input.Trim();

            if (ulong.TryParse(input, out ulong steamId))
            {
                BasePlayer byId = BasePlayer.FindByID(steamId) ?? BasePlayer.FindSleeping(steamId);
                if (byId != null)
                    return PlayerLookupResult.Ok(byId);
            }

            List<BasePlayer> matches = new List<BasePlayer>();
            HashSet<ulong> seen = new HashSet<ulong>();

            BasePlayer exactMatch = ScanPlayers(BasePlayer.activePlayerList, input, matches, seen);
            if (exactMatch != null)
                return PlayerLookupResult.Ok(exactMatch);

            exactMatch = ScanPlayers(BasePlayer.sleepingPlayerList, input, matches, seen);
            if (exactMatch != null)
                return PlayerLookupResult.Ok(exactMatch);

            if (matches.Count == 1)
                return PlayerLookupResult.Ok(matches[0]);

            if (matches.Count > 1)
                return PlayerLookupResult.Fail($"Multiple players matched '{input}'. Please be more specific or use the SteamID.");

            return PlayerLookupResult.Fail($"No matching player found for '{input}'.");
        }

        private BasePlayer ScanPlayers(IEnumerable<BasePlayer> players, string input, List<BasePlayer> matches, HashSet<ulong> seen)
        {
            foreach (BasePlayer player in players)
            {
                if (player == null || !seen.Add(player.userID))
                    continue;

                if (IsExactPlayerMatch(player, input))
                    return player;

                if (IsPartialPlayerMatch(player, input))
                    matches.Add(player);
            }

            return null;
        }

        private bool IsExactPlayerMatch(BasePlayer player, string input)
        {
            return player.UserIDString.Equals(input, StringComparison.OrdinalIgnoreCase) ||
                   player.displayName.Equals(input, StringComparison.OrdinalIgnoreCase);
        }

        private bool IsPartialPlayerMatch(BasePlayer player, string input)
        {
            return player.displayName.IndexOf(input, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private string ResolvePlayerName(string steamId)
        {
            if (ulong.TryParse(steamId, out ulong userId))
            {
                BasePlayer player = BasePlayer.FindByID(userId) ?? BasePlayer.FindSleeping(userId);
                if (player != null && !string.IsNullOrWhiteSpace(player.displayName))
                    return player.displayName;

                IPlayer covalencePlayer = covalence.Players.FindPlayerById(steamId);
                if (covalencePlayer != null && !string.IsNullOrWhiteSpace(covalencePlayer.Name))
                    return covalencePlayer.Name;
            }

            return "Unknown";
        }

        private bool HasPermission(BasePlayer player, string perm)
        {
            return player != null && permission.UserHasPermission(player.UserIDString, perm);
        }

        private bool HasConsoleOrPlayerPermission(ConsoleSystem.Arg arg, string perm)
        {
            if (arg == null)
                return false;

            if (arg.Connection == null)
                return true;

            BasePlayer player = arg.Connection.player as BasePlayer;
            if (player == null)
            {
                ReplyToArg(arg, "Unable to determine player identity for permission check.");
                return false;
            }

            if (!permission.UserHasPermission(player.UserIDString, perm))
            {
                ReplyToArg(arg, "You do not have permission to use this command.");
                return false;
            }

            return true;
        }

        private void ReplyToPlayer(BasePlayer player, string message)
        {
            if (player == null || string.IsNullOrWhiteSpace(message))
                return;

            SendReply(player, message);
        }

        private void ReplyToArg(ConsoleSystem.Arg arg, string message)
        {
            if (arg == null || string.IsNullOrWhiteSpace(message))
                return;

            if (arg.Connection != null)
            {
                BasePlayer player = arg.Connection.player as BasePlayer;
                if (player != null)
                {
                    SendReply(player, message);
                    return;
                }
            }

            Puts(message);
        }

        private class ClanInfo
        {
            public string Tag;
            public List<string> MemberIds = new List<string>();
        }

        private class PlayerLookupResult
        {
            public bool Success;
            public BasePlayer Player;
            public string Message;

            public static PlayerLookupResult Ok(BasePlayer player)
            {
                return new PlayerLookupResult
                {
                    Success = true,
                    Player = player
                };
            }

            public static PlayerLookupResult Fail(string message)
            {
                return new PlayerLookupResult
                {
                    Success = false,
                    Message = message
                };
            }
        }
    }
}
