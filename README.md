# Toreno

> Somewhere between missions, Mike Toreno always seemed to know where CJ was.
> This is that, but for your SA-MP server.

**Toreno** is a Windows tray application that watches a SA-MP server's player list in real time and fires a native Windows notification the instant a specific player connects.

Point it at a server, give it one or more usernames to watch for, and forget about it — it sits quietly in your tray until someone on the list logs in.

## How it works

SA-MP servers expose a public UDP query protocol (the same one server browsers use) — no login, no exploitation, just a documented status endpoint. Toreno speaks that protocol directly:

1. Sends a query packet to the target server (`SAMP` magic bytes + IP + port + opcode)
2. Parses the returned player list from the raw response
3. Diffs it against the last known state to detect **joins**, not just "still online"
4. Fires a native toast notification when a watched username appears

No screen scraping, no game client required, no polling the server harder than a normal server browser would.

### Known limitation

Servers with a large max-slot count (roughly >100) disable the player-list query opcode entirely, as an anti-UDP-amplification measure — the server simply won't answer that part of the protocol, for anyone. Toreno can't see individual player names on those servers, and doesn't try to work around that by connecting as a fake game client — that would mean running an unauthorized bot connection against a server's own rules, which is out of scope on purpose. When you add a server, Toreno checks and clearly flags whether it supports player-list queries, so you know immediately whether a server is watchable.

## Features

- **Watchlist** — track multiple servers, each with its own list of usernames to watch for
- **Capability check on add** — querying a server you add immediately tells you whether it supports player-list queries at all, with a clear warning if it doesn't (see Known limitation above)
- **Native notifications** — real Windows toast popups, not a console window
- **Tray-first** — runs quietly in the background, minimal footprint
- **Double-click UI** — a lightweight window behind the tray icon for watchlist management (add/remove servers and usernames)
- **Configurable poll interval** — tune how often it checks, per server
- **Runs at startup** *(planned)* — optional launch-on-login

## Status

🚧 Early development — not yet functional end-to-end (no live polling/notifications yet). Building in the open.

- [x] SA-MP UDP query client (send/parse `i`/`c` opcode packets)
- [x] Polling loop with per-server exponential backoff on repeated failure
- [x] Join-detection diffing logic
- [x] Tray icon + native toast notifications
- [x] Watchlist config (multiple servers/usernames), persisted to `%APPDATA%\Toreno\config.json`
- [x] Double-click UI window — add/remove servers, manage watched usernames per server, capability warning on add
- [ ] Packaged installer

## Tech stack

- C# / .NET 8
- `System.Net.Sockets.UdpClient` for the SA-MP query protocol
- WPF for the app shell, with `Hardcodet.NotifyIcon.Wpf` for the tray icon (gives double-click-to-open-window behavior for free, and keeps the door open for a richer XAML UI later)
- `CommunityToolkit.Notifications` for native Windows toasts
- JSON config stored in `%APPDATA%\Toreno`

## Configuration (planned format)

```json
{
  "pollIntervalSeconds": 15,
  "servers": [
    {
      "name": "My Favorite Server",
      "address": "127.0.0.1:7777",
      "watchUsernames": ["CJ", "Sweet"]
    }
  ]
}
```

## License

MIT — see [LICENSE](LICENSE).
