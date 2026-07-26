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

## Features

- **Watchlist** — track multiple servers, each with its own list of usernames to watch for
- **Native notifications** — real Windows toast popups, not a console window
- **Tray-first** — runs quietly in the background, minimal footprint
- **Double-click UI** — a lightweight window behind the tray icon for anything beyond quick tray-menu actions (watchlist management, join history, etc. — details TBD)
- **Configurable poll interval** — tune how often it checks, per server
- **Runs at startup** *(planned)* — optional launch-on-login

## Status

🚧 Early development — not yet functional. Building in the open.

- [ ] SA-MP UDP query client (send/parse `c`/`d` opcode packets)
- [ ] Polling loop with per-server backoff on timeout
- [ ] Join-detection diffing logic
- [ ] Tray icon + native toast notifications
- [ ] Watchlist config (multiple servers/usernames) + tray menu management
- [ ] Double-click UI window
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
