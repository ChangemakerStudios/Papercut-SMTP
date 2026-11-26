# Release Notes

## Papercut SMTP v8.2.0 [Unreleased]

### Before You Update

- **Windows Service Zip Has No Tray App** - The Windows Service download no longer includes the Service Tray Manager (`TrayNotification` folder). The desktop app now does its job: it finds an installed Papercut Service, shows its mail and gives new-mail notifications. If you start the tray app from a shortcut or at login, remove that and install the desktop app instead.

## Papercut SMTP v8.1.1 [2026-11-25]

### Security

- **Please Update** - In 8.0.0 through 8.1.0, anyone who could reach the Papercut Service's web port could add an Invoke Process rule through the API and make the service run any program on its machine. The Docker image listens on every interface by default, so this includes anyone on your network. Invoke Process rules are now off unless you set `EnableInvokeProcessRule=true` in the service's configuration, which can't be changed from the web UI or the API. See [GHSA-p66m-wwm8-j346](https://github.com/ChangemakerStudios/Papercut-SMTP/security/advisories/GHSA-p66m-wwm8-j346). Reported by EZO Labs Security. (Thanks!)
- **Port 37403 Closed** - The service no longer listens on port 37403. Only the v7 desktop app used it, and any program on the same machine could send it rule changes.
- **SMTP Passwords Not Sent Back** - The rules API no longer returns the passwords saved in Forward and Relay rules. Leave the password blank when editing to keep it. If you change the server, port, SSL or username, enter it again.

### Before You Update

- **If You Use Invoke Process Rules** - They stop running after this update until you set `EnableInvokeProcessRule=true` in `appsettings.json` or as an environment variable and restart. Only do that if the web UI can be reached from your machine alone or sits behind authentication. See [Invoke Process rules](https://www.papercut-smtp.com/service/#invoke-process-rules).

## Papercut SMTP v8.1.0 [2026-11-15]

### New Features

- **Search** - A search box at the top of the message list finds messages by any text in them: subject, sender and recipient addresses, body, link URLs and attachment names. Any 3 or more characters match inside longer words, so part of an address, a code or a token works. Press `/` to jump to it. While searching, **Select all** selects every result so you can delete them in one go. Search also works in the API (`GET /api/messages?search=...`) and the MCP `list_messages` tool. Fixes [#250](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/250) (Thanks, [VenkatRamanan6](https://github.com/VenkatRamanan6), [trendy-software](https://github.com/trendy-software) and [agail](https://github.com/agail)!)
- **Read Status That Sticks** - A message stays unread until you open it, and Papercut remembers this across restarts. Before, anything older than 5 minutes showed as read. **Mark all read** is in the list header, and right-click a message to mark it read or unread. After updating, the mail already in your inbox starts out read.
- **Keyboard Shortcuts** - `j` / `k` for the next and previous message, `Del` to delete, `f` to forward, `1`-`5` for the message tabs, `/` to search, `Ctrl+,` for Options, and `?` for the full list.
- **Right-Click Menu** - Right-click a message for Open, Forward, Mark as read or unread, Copy sender address, Save as .eml and Delete. With several messages selected, it acts on all of them. In the desktop app, the browser's own menu (Back, Refresh, Print) is gone: inside an email you get Open link, Copy link and Copy, and text fields keep Cut, Copy and Paste.

### Improvements

- **Faster Message List** - The list reads from the new index instead of opening every message file, so scrolling through a large inbox no longer stalls, and more messages load at a time.
- **Search Index** - Search and read status are kept in `papercut-index.db` beside your messages (in Docker, in the `/app/Incoming` volume). It is built in the background on the first start and is rebuilt from the messages if deleted. If it can't be created, Papercut works as before.
- **Sponsor and Star Links** - The footer links to GitHub Sponsors and to starring the project. Papercut is free and built by one person, and both help.

### Bug Fixes

- **Deleted Messages Staying in the List** - Messages deleted outside the window, by a Cleanup Mail rule, another browser tab or the API, now disappear from the list straight away. If the open message was deleted, the view clears.
- **Esc Not Closing Dialogs** - Esc now closes Options, Rules, the log and the shortcuts list even when you were last clicking in a message.

## Papercut SMTP v8.0.2 [2026-11-08]

### Bug Fixes

- **Old Interface After Updating** - After an update, the desktop app or a browser could keep showing the previous version's interface for up to 10 minutes. From the next update on, the new interface loads straight away. If this update still looks like the old version, press Ctrl+F5.

## Papercut SMTP v8.0.1 [2026-11-08]

### Bug Fixes

- **Messages Stuck Loading** - Opening a message while the Headers, Body, Sections or Raw tab was selected left the pane on the loading spinner until the window was reloaded.
- **Scroll Position** - Opening another message now starts at the top instead of keeping the previous message's scroll position.

## Papercut SMTP v8.0.0 [2026-11-08]

The desktop app is rebuilt. It's now a tray app that shows the same web UI as the Papercut Service, and it runs on macOS as well as Windows. Updating from v7 is a normal in-place update: your messages, SMTP IP and port, message folders, notification setting and window size all come with you.

### Security

- **Please Update** - In 7.8.0 and earlier, any web page could call the Papercut Service's API, so a site you visited could read or delete your captured mail. Updating to 8.0.0 fixes this.

### Before You Update

- **Forwarding Rules Don't Carry Over** - v7 kept its rules in a folder that updates replace, so they're gone by the time v8 starts, unless an installed Papercut Service has a copy, which v8 imports. Papercut shows a notice once if you need to re-create them under **Rules**. From v8 on, rules survive updates.
- **Other Settings Not Carried Over** - Forward dialog settings, theme, sort order and zoom go back to their defaults.
- **If You Also Run the Windows Service** - While the Windows Service is running, the desktop app uses it instead of starting its own, and the two no longer sync settings. Mail goes to the service's own SMTP port, which defaults to 2525, not the desktop's 25. Check it under **Options**.
- **Web Pages on Other Origins Are Blocked** - Web pages on another origin can no longer call the service. If you call it from a browser app, add that origin to `CorsAllowedOrigins`. Scripts, curl and MCP clients aren't affected. Behind a reverse proxy that passes neither `Host` nor `X-Forwarded-Host`, which includes nginx by default, the web UI is a blank page until you fix the proxy or set `CorsAllowedOrigins`. Host names a website could take over are blocked too, for every caller: anything other than an IP address, `localhost`, a single-word name or a private suffix such as `.local` needs its URL in `CorsAllowedOrigins`. See [Calling the API from a web page](https://www.papercut-smtp.com/service/#calling-the-api-from-a-web-page).

### New Features

- **New Desktop App** - A tray icon with new-mail notifications and run at startup, plus a window that shows the web UI in the system's webview. It includes its own .NET runtime, and it uses a Papercut Service already running on your machine, such as in Docker, if it finds one.
- **macOS** - Papercut now runs on Apple Silicon and Intel Macs, from the menu bar. SMTP defaults to port 2525 there. This is the first macOS release, and it isn't signed yet, so the first launch needs System Settings -> Privacy & Security -> Open Anyway. Please report any problems you find.
- **Rebuilt Web UI** - The web UI is rebuilt on Angular 21 and replaces the old desktop window. It adds rules management, an Options dialog, a live service log viewer, Forward / Delete / Delete All, ctrl/shift multi-select, virtual scrolling with a message count, a sort direction toggle, read tracking, a System / Light / Dark theme, and a collapsible message list. Message HTML is rendered on the server through a MIME-aware pipeline, and emails keep their own colors in dark mode.
- **MCP Server** - The service can expose its messages to AI assistants over the Model Context Protocol. It is off by default; see [MCP](https://www.papercut-smtp.com/mcp/) in the docs.
- **Create Messages Directly** - `POST` or `PUT` to `api/messages`, or use the MCP `create_message` tool, to add a message without sending it over SMTP. The body can be markdown or HTML.
- **SMTP Rate Limiting** - Optional, configurable rate limiting for incoming SMTP messages, for testing how your app handles a throttling mail server. Addresses [#199](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/199)
- **Documentation Site** - New docs at [papercut-smtp.com](https://www.papercut-smtp.com/). Fixes [#220](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/220) (Thanks, [Carsak](https://github.com/Carsak)!)

### Improvements

- **Service Tray Notifications** - The Service Tray Manager now gets new-mail notifications over SignalR. It treats any reachable service as running, and Open Web UI works for services that are not registered.
- **Safer Message View** - `javascript:`, `data:` and `vbscript:` links are blocked, and links open in a new tab.
- **Bounded Memory** - Reading raw messages and MIME sections no longer loads unbounded data into memory.
- **WinGet** - Fixed the package ID and PR creation for WinGet publishing, and added ProductCode and ReleaseDate to the manifests. Relates to [#376](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/376) (Thanks, [AntekOlszewski](https://github.com/AntekOlszewski)!)
- **Cleanup** - Removed the unused Electron.NET integration. Added an Apache 2.0 LICENSE file.

### Bug Fixes

- **Forwarding Rules Corrupting Messages** - Fixed messages and attachments getting corrupted when several forwarding rules matched the same message. Each rule now works on its own copy read from disk. Fixes [#379](https://github.com/ChangemakerStudios/Papercut-SMTP/issues/379) (Thanks for the report, [dpinkham6](https://github.com/dpinkham6)!)
- **Duplicate New-Message Events** - Each received message now raises one new-message event, not several.
- **Web UI Under a Path Prefix** - The web UI now loads correctly when the service runs under an `HttpPathPrefix`.
- **SMTP Rebind in the Service** - Changing the SMTP IP or port at runtime now moves the running listener. Before this fix, the old port stayed open and the new listener died.
- **Log File Access** - The log viewer opens the log file with shared access, so it no longer conflicts with the logger.

### Contributors

Special thanks to [dpinkham6](https://github.com/dpinkham6), [AntekOlszewski](https://github.com/AntekOlszewski), and [Carsak](https://github.com/Carsak) for their reports and suggestions!

Papercut SMTP is free and open source. If it saves you time, consider [sponsoring the project](https://github.com/sponsors/Jaben).
