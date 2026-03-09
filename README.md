
# ClanSteamIDs
**Author:** SeesAll  
**Version:** 1.0.3  
**Game:** Rust (uMod / Oxide)

ClanSteamIDs is a lightweight utility plugin that allows server administrators and authorized users to quickly retrieve SteamIDs for individual players or for all members of a clan.

This plugin integrates with the popular **Clans** plugin by **k1lly0u** and supports both the **free** and **premium** versions.

It is designed to be extremely lightweight and only performs work when commands are executed.

---

# Features

- Retrieve the SteamID of a player by name or SteamID
- Retrieve all SteamIDs belonging to a clan
- Works with **offline clan members**
- Compatible with both **free and premium Clans plugins**
- Permission-based command usage
- Chat and console commands supported
- No config required
- No background processing (zero performance impact when idle)

---

# Requirements

This plugin requires the **Clans plugin** by **k1lly0u**.

Supported versions:
- Free Clans plugin (uMod)
- Premium Clans plugin

If the Clans plugin is not installed, clan lookup commands will return an error.

---

# Installation

1. Download `ClanSteamIDs.cs`
2. Place the file into your server's:

```
/server/oxide/plugins/
```

3. Reload the plugin or restart the server:

```
oxide.reload ClanSteamIDs
```

---

# Permissions

Two permissions are included to allow granular control.

### Allow player SteamID lookups
```
clansteamids.player
```

### Allow clan roster SteamID lookups
```
clansteamids.clan
```

### Example permission commands

Grant to a group:
```
oxide.grant group admin clansteamids.player
oxide.grant group admin clansteamids.clan
```

Grant to a user:
```
oxide.grant user 76561198000000000 clansteamids.clan
```

---

# Chat Commands

### Get a player's SteamID

```
/playersteamid <player name or steamid>
```

Example:
```
/playersteamid SeesAll
```

Output example:
```
Player: SeesAll | SteamID: 76561198000000000 | Clan: HAVEN
```

---

### Get all SteamIDs from a clan

```
/clanallsteamid <clan tag>
```

Example:
```
/clanallsteamid HAVEN
```

Output example:
```
Clan [HAVEN] SteamIDs (3 members):
SeesAll - 76561198000000000
PlayerTwo - 76561198000000001
PlayerThree - 76561198000000002
```

Offline clan members are included.

---

# Console Commands

These commands can also be run from the server console or RCON.

### Player SteamID lookup

```
playersteamid <player name or steamid>
```

### Clan SteamID lookup

```
clanallsteamid <clan tag>
```

---

# Notes

### Offline Players

Clan member SteamIDs are retrieved directly from the Clans plugin data, meaning **offline members are included**.

### Player Name Resolution

If a clan member has never been loaded by the current server session, their name may appear as:

```
Unknown
```

Their SteamID will still be returned correctly.

---

# Performance

This plugin is designed to have **negligible performance impact**.

Characteristics:

- No timers
- No repeating hooks
- No background processing
- No data files
- No database usage

The plugin only executes logic when a command is used.

---

# Compatibility

Tested with:

- Clans (free version)
- Clans (premium version)

Both plugins expose compatible APIs used by ClanSteamIDs.

---

# Troubleshooting

### Clan lookup returns "Clans plugin not found"

Ensure the Clans plugin is installed and loaded.

Check with:

```
oxide.plugins
```

---

### No matching player found

The player must be:

- Online
- Sleeping
- Previously known to the server

For completely offline players, use the **clan lookup command** instead.

---

# Roadmap (Possible Future Features)

Potential future improvements:

- Optional config file
- Chat output formatting options
- Large clan output to console only
- Command aliases (ex: `/psid`, `/csid`)
- Logging of lookups

---

# License

This plugin is provided free for use by Rust server owners.

You may modify and redistribute it as needed.
