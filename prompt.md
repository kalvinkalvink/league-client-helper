

### Phase 1: The Foundation (Infrastructure & Models)
**Use this prompt first.** It sets up the folder structure, the DI container, the logging, and the settings persistence.

> **Prompt:** > I am building a .NET MAUI 9 (Windows-only) application called "LolClientHelper" based on the attached specification. 

**Goal:** Generate the initial project infrastructure, models, and base services.
1. **Architecture:** Use MVVM with `CommunityToolkit.Mvvm`.
2. **Models:** Implement `AppSettings.cs`, `LcuCredentials.cs`, `GameState.cs` (Enum), and `SummonerInfo.cs`.
3. **Infrastructure:**
   - `SettingsService`: Handles JSON persistence in `%APPDATA%`.
   - `LoggingService`: Rolling file logger for 7 days in `%APPDATA%`.
   - `MauiProgram.cs`: Setup Dependency Injection for all services.
4. **Project Setup:** Include the `app.manifest` with `requireAdministrator` level and the `net9.0-windows10.0.19041.0` target.

[Please refer to Section 2, 7, 10, and 13 of the spec.md]

---

### Phase 2: The LCU Core (Services & WAMP)
**Use this prompt second.** This is the "brain" of the app that talks to League of Legends.

**Prompt:** > Now, implement the LCU Communication Layer for LolClientHelper.

**Goal:** Build the services that detect the League process and handle API traffic.
1. **LeagueProcessService:** Use `Get-CimInstance Win32_Process` to parse `--app-port` and `--remoting-auth-token`.
2. **LcuApiService:** >    - Implement the `HttpClient` with the SSL bypass for `127.0.0.1`.
   - Add methods for the GET/POST/PUT endpoints listed in the spec (Gameflow, Chat, Lobby).
3. **WebSocketService:** >    - Use `ClientWebSocket` to connect to the LCU WAMP server.
   - Implement the WAMP "Subscribe" logic using the `[5, "topic"]` format.
   - Topics: `OnJsonApiEvent_lol-gameflow_v1_gameflow_phase_POST` and `OnJsonApiEvent_lol-matchmaking_v1_ready-check_*`.
4. **GameStateService:** Orchestrates the transition between WebSocket events and HTTP polling fallback.

[Please refer to Section 3, 4, and 5 of the spec.md]

---

### Phase 3: The UI & ViewModels (Final Piece)
**Use this prompt last.** This builds the visual interface and the multi-window logic.

**Prompt:** > Finally, generate the UI and ViewModels for LolClientHelper.

**Goal:** Implement the user interface, localization, and window management.
1. **ViewModels:** `MainViewModel` and `SettingsViewModel`. Implement the logic for "Auto Accept," "Fake Rank" updates, and "Invite All Friends."
2. **MainWindow.xaml:** A tabbed layout (Vertical) as per the spec.
3. **SettingsWindow.xaml:** A separate non-modal window. Implement the **Singleton Pattern** in code-behind to ensure only one instance exists.
4. **i18n:** Create the `Strings.en.resx` structure and a helper to switch cultures at runtime.
5. **Theme:** Implement the Dark/Light theme switching using MAUI `ResourceDictionaries`.

[Please refer to Section 8, 11, and 14 of the spec.md]

---

### A few "Pro" tips for when you run these:

* **WAMP Handshake:** In Phase 2, the LCU requires a "Hello" message before you can subscribe. Most AIs forget this. Make sure it sends `[1, "wamp", 2, {"roles": {}}]` (or similar) before the `[5, "topic"]` call.
* **XAML Namespaces:** MAUI can be finicky with Windows-specific namespaces. If you see errors about `Microsoft.UI.Xaml`, tell the AI: *"Ensure you are using the WinUI 3 compatibility layer for the Windows-specific windowing logic."*
* **The "Fake Rank" JSON:** Remind the AI that `rankedLeagueTier` must be ALL CAPS (e.g., `CHALLENGER`), otherwise the LCU API will ignore the request.

Your spec is so tight that you should get about 90-95% working code on the first try with these prompts. Good luck with the climb to Challenger! (Even if it is just a fake rank).