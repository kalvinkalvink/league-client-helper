# SPEC: LolClientHelper (.NET MAUI)

## 1. Project Overview

| Attribute | Value |
|-----------|-------|
| **Project Name** | LolClientHelper |
| **Type** | Desktop Application |
| **Framework** | .NET MAUI (.NET 9) |
| **Supported Platform** | Windows 10+ only |
| **Core Function** | League of Legends client automation via LCU API |

## 2. Technology Stack

| Component | Technology |
|-----------|------------|
| **Framework** | .NET MAUI 9 |
| **Language** | C# 12 |
| **Target Platform** | Windows (`net9.0-windows10.0.19041.0`) |
| **Architecture** | MVVM |
| **HTTP Client** | HttpClient (System.Net.Http) |
| **JSON** | System.Text.Json |
| **Dependency Injection** | Microsoft.Extensions.DependencyInjection |
| **Settings Persistence** | JSON file in `%APPDATA%\LolClientHelper\` |
| **Process Query** | `Get-CimInstance Win32_Process` |
| **Logging** | Rolling file logs in `%APPDATA%\LolClientHelper\logs\` |

## 3. LCU API Authentication

### 3.1 Token Retrieval (Windows)

```powershell
Get-CimInstance Win32_Process -Filter "Name='LeagueClientUx.exe'" | Select-Object -ExpandProperty CommandLine
```

Parses:
- `--app-port=XXXX`
- `--remoting-auth-token=YYYYYYYY-YYYY-YYYY-YYYY-YYYYYYYYYYYY`

### 3.2 Authentication

- **URL**: `https://127.0.0.1:{PORT}`
- **Auth**: Basic Auth with `"riot:{TOKEN}"` (Base64)
- **SSL**: Bypass for self-signed localhost certificate

**SSL Bypass Implementation:**
```csharp
var handler = new HttpClientHandler();
handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
{
    // Accept all certificates for localhost/127.0.0.1
    if (message.RequestUri.Host == "127.0.0.1" || message.RequestUri.Host == "localhost")
        return true;
    return errors == SslPolicyErrors.None;
};
var httpClient = new HttpClient(handler);
```

### 3.3 Process Query (Requires Admin)

**Important:** Querying another process's CommandLine requires administrative privileges.

**app.manifest:**
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="urn:schemas-microsoft-com:asm.v1">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v2">
    <security>
      <requestedExecutionLevel level="requireAdministrator" uiAccess="false" />
    </security>
  </trustInfo>
</assembly>
```

This ensures the app runs with elevated privileges to query LeagueClientUx.exe process details.

### 3.3 Connection & Retry Logic

**Initial Connection:**
1. Find `LeagueClientUx.exe` process
2. Parse `--app-port` and `--remoting-auth-token`
3. If not found → retry every 10 seconds indefinitely
4. Log: `"Waiting for League client to start..."` (WARNING level)

**WebSocket Connection (WAMP):**
- Connect to `wss://127.0.0.1:{port}/` using the base port (app-port + 1 typically)
- Authenticate with the same riot token
- Subscribe to game flow events for real-time state tracking

**Reconnection:**
1. If LCU connection fails during operation (network error, 401, 5xx)
2. Wait 10 seconds before retry
3. Max 3 retries with 10s interval
4. Log each retry attempt
5. After max retries → show "Connection Lost" status, stop retrying

**Heartbeat:**
- For WebSocket: monitor connection state
- For HTTP fallback: Ping `/lol-login/v1/session` every 30 seconds

## 4. Game Flow States

| State | Description |
|-------|-------------|
| `None` | In lobby / Main menu |
| `Matchmaking` | Searching for match |
| `ReadyCheck` | Match found |
| `ChampSelect` | Champion selection |
| `InProgress` | Game in progress |
| `PreEndOfGame` | Post-game honors |
| `WaitingForStats` | Stats page |
| `EndOfGame` | Can queue again |
| `Reconnect` | Reconnection required |

**State Tracking**: App polls `/lol-gameflow/v1/gameflow-phase` at interval defined in settings (default 500ms) to detect state changes.

**State Change Handling**:
- None → Matchmaking: Auto start game enabled → poll search endpoint
- ReadyCheck → None: Auto accept match → POST accept
- InProgress → PreEndOfGame: Auto reenter lobby enabled → POST honor, then POST play-again

## 5. LCU API Endpoints

### 5.1 HTTP Endpoints

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `/lol-gameflow/v1/gameflow-phase` | GET | Game state polling (fallback) |
| `/lol-matchmaking/v1/ready-check/accept` | POST | Accept match |
| `/lol-lobby/v2/lobby/matchmaking/search` | POST | Start queue |
| `/lol-gameflow/v1/reconnect` | POST | Reconnect |
| `/lol-honor-v2/v1/honor-player/` | POST | Skip honors |
| `/lol-lobby/v2/play-again` | POST | Queue again |
| `/lol-lobby/v2/received-invitations` | GET | Pending invites |
| `/lol-lobby/v2/received-invitations/{id}/accept` | POST | Accept invitation |
| `/lol-lobby/v2/lobby/invitations` | POST | Invite friends to lobby |
| `/lol-chat/v1/friends` | GET | Friend list |
| `/lol-login/v1/session` | GET | Login session |
| `/lol-summoner/v1/current-summoner` | GET | Summoner info |
| `/lol-chat/v1/me` | PUT | Set rank/status |
| `/lol-ranked/v1/ranked-stats/{puuid}` | GET | Rank info |
| `/lol-match-history/v1/products/lol/{puuid}/matches` | GET | Match history |

### 5.2 WebSocket (WAMP) Subscriptions

Connect to: `wss://127.0.0.1:{base_port}/`

**Implementation:**
- Use `System.Net.WebSockets.ClientWebSocket` for the connection
- WAMP uses a specific message format for subscriptions
- Send subscribe message as JSON array: `[5, "topic_name"]`

**WAMP Message Format:**
```csharp
// Subscribe message structure
[5, "OnJsonApiEvent_lol-gameflow_v1_gameflow_phase_POST"]

// Example WAMP handshake and subscribe:
await ws.SendAsync(Encoding.UTF8.GetBytes("[1,\"wamp\",2,\"\""+realm+"\"]"), ...);  // Hello
await ws.SendAsync(Encoding.UTF8.GetBytes("[5,\"OnJsonApiEvent_lol-gameflow_v1_gameflow_phase_POST\"]"), ...);  // Subscribe
```

| Topic | Event | Description |
|-------|-------|-------------|
| `OnJsonApiEvent_lol-gameflow_v1_gameflow_phase_POST` | gameflow phase changed | Real-time game state tracking |
| `OnJsonApiEvent_lol-matchmaking_v1_ready-check_*` | ready check received | Auto-accept when match found |
| `OnJsonApiEvent_guilds_jwt-token-received_*` | token refresh | Auto-reconnect on token refresh |

**Behavior:**
- Use WebSocket for game state tracking (real-time, low latency)
- Fall back to HTTP polling if WebSocket connection fails
- Handle reconnection automatically on WebSocket disconnect

## 6. Features

### 6.1 Auto Features

| Feature | Default | Setting Key | Behavior |
|---------|---------|-------------|----------|
| Auto Start Game | Off | `AutoStartGame` | Polls `/lol-lobby/v2/lobby/matchmaking/search` until it succeeds or fails due to unmet requirements |
| Auto Accept Match | On | `AutoAcceptMatch` | POST to `/lol-matchmaking/v1/ready-check/accept` on ready check |
| Auto Skip Like | On | `AutoSkipLike` | POST with empty body to `/lol-honor-v2/v1/honor-player/` after game ends |
| Auto Reenter Lobby | On | `AutoReenterLobby` | When game status changes from InProgress → PreEndOfGame, call honor API then `/lol-lobby/v2/play-again` |
| Auto Accept Game Invite | On | `AutoAcceptInvite` | GET `/lol-lobby/v2/received-invitations`, then POST accept for each |

### 6.2 Rank (Fake Rank) Settings

Used for displaying fake rank when others hover over your profile.

| Setting | Options | Default |
|---------|---------|---------|
| Queue Type | RANKED_SOLO_5x5, RANKED_FLEX_SR, RANKED_FLEX_TT, RANKED_TFT | RANKED_SOLO_5x5 |
| Tier | IRON, BRONZE, SILVER, GOLD, PLATINUM, DIAMOND, MASTER, GRANDMASTER, CHALLENGER | CHALLENGER |
| Division | IV, III, II, I | I |

**API**: PUT `/lol-chat/v1/me`
```json
{"lol": {"rankedLeagueQueue": "...", "rankedLeagueTier": "...", "rankedLeagueDivision": "..."}}
```

### 6.3 Friend Management

- Get friends from `/lol-chat/v1/friends`
- Extract unique friend groups from each friend's `groupName` field
- Invite all friends (filtered by selected group) via POST to `/lol-lobby/v2/lobby/invitations`
- Auto-accept incoming game invites

## 7. Logging System

### 7.1 File Location

```
%APPDATA%\LolClientHelper\logs\log-{date}.txt
```

### 7.2 Log Levels

| Level | Use Case |
|-------|----------|
| DEBUG | API requests/responses, token parsing, state changes |
| INFO | Connection status, feature triggers, settings changes |
| WARNING | Retry attempts, non-fatal errors |
| ERROR | API failures, process detection failures |

### 7.3 Log Format

```
[2026-04-25 12:00:00.123] [INFO] [MainService] Connected to LCU on port 12345
[2026-04-25 12:00:01.456] [DEBUG] [LcuApi] GET /lol-gameflow/v1/gameflow-phase -> 200 OK
[2026-04-25 12:00:01.789] [INFO] [GameState] State changed: None -> ReadyCheck
[2026-04-25 12:00:01.790] [INFO] [AutoAccept] Triggered - accepting match
[2026-04-25 12:00:02.012] [DEBUG] [LcuApi] POST /lol-matchmaking/v1/ready-check/accept -> 204 No Content
```

### 7.4 Rolling Policy

- Keep last 7 days of logs
- Auto-delete logs older than 7 days
- One file per day

## 8. Dark Theme

### 8.1 Color Palette

| Element | Dark Color | Light Color |
|---------|-----------|-------------|
| Background | #1E1E1E | #FFFFFF |
| Surface | #2D2D2D | #F5F5F5 |
| Primary | #00D4FF (LoL blue) | #00D4FF |
| Text Primary | #FFFFFF | #000000 |
| Text Secondary | #AAAAAA | #666666 |
| Border | #3D3D3D | #E0E0E0 |
| Accent | #C89B3C (LoL gold) | #C89B3C |
| Error | #FF4444 | #FF4444 |
| Success | #00CC66 | #00CC66 |

### 8.2 Theme Options

- **Auto**: Follow system theme
- **Light**: Force light theme
- **Dark**: Force dark theme (default)

## 9. Architecture

```
LolClientHelper/
├── App.xaml(.cs)                    # App entry, DI setup
├── MauiProgram.cs                   # MAUI configuration
├── MainWindow.xaml(.cs)             # Main window
├── SettingsWindow.xaml(.cs)           # Settings window (non-modal)
│
├── Services/
│   ├── LcuApiService.cs           # LCU HTTP client
│   ├── GameStateService.cs        # Game state (WebSocket + HTTP fallback)
│   ├── LeagueProcessService.cs     # Process detection & token parsing
│   ├── SettingsService.cs         # Settings persistence
│   ├── LoggingService.cs          # Logging
│   └── WebSocketService.cs       # WAMP WebSocket client for real-time events
│
├── Models/
│   ├── GameState.cs                 # Game flow enum
│   ├── LcuCredentials.cs            # Port + Token
│   ├── SummonerInfo.cs               # Summoner data
│   ├── AppSettings.cs               # User settings model
│   ├── Friend.cs                   # Friend data
│   └── Invitation.cs               # Invitation data
│
├── ViewModels/
│   ├── MainViewModel.cs             # Main page logic
│   └── SettingsViewModel.cs         # Settings management
│
└── Resources/
    └── Strings/                    # Localization
        ├── Strings.en.resx
        ├── Strings.zh-CN.resx
        └── Strings.zh-TW.resx
```

## 10. Settings Service

### 10.1 File Location

```
%APPDATA%\LolClientHelper\settings.json
```

### 10.2 Data Model

```csharp
public class AppSettings
{
    // Window State
    public double WindowX { get; set; } = 100;
    public double WindowY { get; set; } = 100;
    public double WindowWidth { get; set; } = 1000;
    public double WindowHeight { get; set; } = 800;

    // Game Auto
    public bool AutoStartGame { get; set; } = false;
    public bool AutoAcceptMatch { get; set; } = true;
    public bool AutoSkipLike { get; set; } = true;
    public bool AutoReenterLobby { get; set; } = true;

    // Main Page
    public bool AutoAcceptInvite { get; set; } = true;

    // Lobby
    public string FriendFilterGroup { get; set; } = "All";

    // Game Status - Ranking
    public string QueueType { get; set; } = "RANKED_SOLO_5x5";
    public string Tier { get; set; } = "CHALLENGER";
    public string Division { get; set; } = "I";
    public string Status { get; set; } = "chat";

    // Advanced Settings
    public int PollIntervalMs { get; set; } = 500;
    public bool StartMinimized { get; set; } = false;

    // Appearance
    public string Theme { get; set; } = "Dark";  // Auto, Light, Dark
    public bool ChangeRankingOnStart { get; set; } = false;

    // Localization
    public string Language { get; set; } = "en";

    // Debug
    public bool DebugLogging { get; set; } = false;
}
```

### 10.3 Interface

```csharp
public interface ISettingsService
{
    AppSettings Load();
    void Save(AppSettings settings);
    void RestoreWindowPosition(Window window);
    void SaveWindowPosition(Window window);
    AppSettings Current { get; }
}
```

### 10.4 Behavior

1. **On App Start**:
   - Load settings from JSON file
   - Restore window position/size
   - Apply settings to all services
   - If Language not set in settings, auto-detect from system locale (fallback to English)

2. **On Setting Change**:
   - Save immediately to disk
   - Notify affected services

3. **On App Close**:
   - Save window position
   - Persist all settings
   - Close application (no tray)

## 11. UI Structure

### 11.1 Main Window (Resizable)

- Default size: 1000x800
- Minimum size: 400x600
- Regular Windows title bar with native controls (minimize, maximize, close)
- Toolbar below title bar with File and Settings buttons

```
+-------------------------------------+
| [File] [Settings]                 |
+-------------------------------------+
| +--------+ +-----------------------+ |
| | Tabs   | | Tab Content          | |
| |        | |                       | |
| | Game   | | [Content area        | |
| | Auto   | |  based on            | |
| |        | |  selected tab]        | |
| | Main   | |                       | |
| | Page   | |                       | |
| |        | |                       | |
| | Lobby  | |                       | |
| |        | |                       | |
| | Game   | |                       | |
| | Status | |                       | |
| +--------+ +-----------------------+ |
+-------------------------------------+
| Status: Connecting... (right align) |
+-------------------------------------+
```

- **File** button: Opens dropdown menu with "Settings" option
- **Settings** button: Opens settings popup window directly
- **Tabs** on the left side, vertical navigation
- **Status bar**: Bottom of window, displays connection status (right-aligned)
  - "Connecting..." when LCU not found
  - "Connected" when connected
  - Current game state (e.g., "In Game", "Lobby", "Champ Select", "Matchmaking")

### 11.2 Tab Contents

#### 11.2.1 Game Auto Tab

```
+-------------------------------+
| Game Auto                    |
+-------------------------------+
| [x] Auto Start Game          |
| [x] Auto Accept Match        |
| [x] Auto Skip Like           |
| [x] Auto Reenter Lobby       |
+-------------------------------+
```

#### 11.2.2 Main Page Tab

```
+-------------------------------+
| Main Page                     |
+-------------------------------+
| [x] Auto Accept Game Invite   |
+-------------------------------+
```

#### 11.2.3 Lobby Tab

```
+-------------------------------+
| Lobby                         |
+-------------------------------+
| [Invite All Friends]          |
| Filter: [All              v]  |
+-------------------------------+
```

- **Invite All Friends** button: invites all online friends to lobby
- **Filter dropdown**: filters friends by group (default: "All")
  - Options: All, Custom groups from friend's list

#### 11.2.4 Game Status Tab

```
+-------------------------------+
| Game Status                   |
+-------------------------------+
| Game Mode:  [Select         v] |
| Ranking:   [Select         v] |
| Level:     [Select         v] |
|                               |
| Status:   [Select         v]  |
+-------------------------------+
```

- **Game Mode dropdown**: RANKED_SOLO_5x5, RANKED_FLEX_SR, RANKED_FLEX_TT, RANKED_TFT
- **Ranking dropdown**: IRON, BRONZE, SILVER, GOLD, PLATINUM, DIAMOND, MASTER, GRANDMASTER, CHALLENGER
- **Level dropdown**: IV, III, II, I
- **Status dropdown**: chat, away, dnd, offline, mobile (Online, In Game, AFK, Offline, Mobile Online)
- Status changes are applied immediately when user selects from dropdown
- Uses LCU API: PUT `/lol-chat/v1/me` with `availability` field

### 11.3 Settings Window (Separate Non-Modal Window)

```
+-------------------------------------+
|  Settings                    [x]    |
+-------------------------------------+
|  General                            |
|  + Poll Interval (ms): [500    v]  |
|  + Language:      [English    v]   |
|                                     |
|  Appearance                         |
|  | Theme: [Dark              v]     |
|  |   Auto / Light / Dark          |
|                                     |
|  Advanced                           |
|  | [x] Change ranking on start    |
|                                     |
|  Reset                              |
|  [Reset to Defaults]                |
+-------------------------------------+
```

- **Poll Interval**: Polling interval in milliseconds (fallback when WebSocket fails)
- **Language**: Dropdown to select language (English, 简体中文, 繁體中文)
- **Theme**: Auto / Light / Dark (default: Dark)
- **Change ranking on start**: Whether to change game ranking settings on app start
- Window is a **singleton**: only one instance allowed, focus existing if already open

### 11.4 Window Management

- **Main Window**: Standard Windows title bar (native min/max/close)
- **Settings Window**: 
  - Separate non-modal window that can stay open while using main window
  - **Singleton pattern**: Only one instance allowed
  - If user clicks Settings while window is open, focus the existing window instead of creating a new one
  - Track window instance in a static field to prevent spawning multiple windows
  ```csharp
  private static SettingsWindow? _instance;
  public static void Show()
  {
      if (_instance == null || _instance.IsClosed)
      {
          _instance = new SettingsWindow();
          _instance.Closed += (s, e) => _instance = null;
          _instance.Show();
      }
      else
      {
          _instance.Activate();
      }
  }
  ```
- This window can stay open while using the main window

### 11.4 Title Bar Controls

- **File** button: Opens dropdown menu with "Settings" option
- **Settings** button: Opens settings popup window directly
- Standard window controls: Minimize, Maximize, Close (native Windows title bar)

## 12. Dependencies (NuGet)

- `Microsoft.Maui.Controls` (via .NET MAUI)
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`
- `CommunityToolkit.Mvvm`

## 13. Build Configuration

| Setting | Value |
|---------|-------|
| Target Framework | `net9.0-windows10.0.19041.0` |
| Min Windows Version | 10.0.17763.0 |
| Output Type | Exe |
| Windows Package Type | None |

## 14. i18n (Internationalization)

### 14.1 Supported Languages

| Language | Code |
|----------|------|
| English | en |
| Chinese (Simplified) | zh-CN |
| Chinese (Traditional) | zh-TW |

### 14.2 Translation Keys

```json
{
  "app.title": "LolClientHelper",
  "menu.file": "File",
  "menu.settings": "Settings",
  
  "tab.game_auto": "Game Auto",
  "tab.main_page": "Main Page",
  "tab.lobby": "Lobby",
  "tab.game_status": "Game Status",
  
  "setting.poll_interval": "Poll Interval (ms)",
  "setting.theme": "Theme",
  "setting.theme_auto": "Auto",
  "setting.theme_light": "Light",
  "setting.theme_dark": "Dark",
  "setting.change_ranking_on_start": "Change ranking on start",
  "setting.language": "Language",
  "setting.reset": "Reset to Defaults",
  
  "checkbox.auto_start_game": "Auto Start Game",
  "checkbox.auto_accept_match": "Auto Accept Match",
  "checkbox.auto_skip_like": "Auto Skip Like",
  "checkbox.auto_reenter_lobby": "Auto Reenter Lobby",
  "checkbox.auto_accept_game_invite": "Auto Accept Game Invite",
  
  "button.invite_all_friends": "Invite All Friends",
  "filter.all": "All",
  "filter.group": "Filter by Group",
  
  "dropdown.game_mode": "Game Mode",
  "dropdown.ranking": "Ranking",
  "dropdown.level": "Level",
  "dropdown.status": "Status",
  
  "queue.ranked_solo_5x5": "Ranked Solo",
  "queue.ranked_flex_sr": "Ranked Flex SR",
  "queue.ranked_flex_tt": "Ranked Flex TT",
  "queue.ranked_tft": "Ranked TFT",
  
  "tier.iron": "Iron",
  "tier.bronze": "Bronze",
  "tier.silver": "Silver",
  "tier.gold": "Gold",
  "tier.platinum": "Platinum",
  "tier.diamond": "Diamond",
  "tier.master": "Master",
  "tier.grandmaster": "Grandmaster",
  "tier.challenger": "Challenger",
  
  "division.iv": "IV",
  "division.iii": "III",
  "division.ii": "II",
  "division.i": "I",
  
  "status.chat": "Online",
  "status.away": "Away",
  "status.dnd": "In Game",
  "status.offline": "Offline",
  "status.mobile": "Mobile Online",
  
  "window.settings": "Settings",
  "button.close": "Close"
}
```

### 14.3 Translation Loading

- Default language: English (en)
- Language detection order:
  1. Use saved preference from settings
  2. Fall back to system locale
  3. Fall back to English if translation not found

### 14.4 Translation File Format

```
Resources/
├── Strings/
│   ├── Strings.en.resx (default)
│   ├── Strings.zh-CN.resx
│   └── Strings.zh-TW.resx
```

## 15. Configuration Defaults

| Setting | Default |
|---------|---------|
| Poll Interval | 500ms |
| Default Queue | RANKED_SOLO_5x5 |
| Default Tier | CHALLENGER |
| Default Division | I |
| Theme | Dark |
| Debug Logging | false |
| Start Minimized | false |
| Window Position | Centered |
| Window Size | 1000x800 |