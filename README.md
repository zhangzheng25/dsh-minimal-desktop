# DeepSeek Harness Desktop

> **一句话简介**：DeepSeek Harness 桌面包——常驻 Windows 托盘的启动器，一键启动、打开、重启、退出本机 DSh Web 服务，让 DeepSeek Harness 用起来像原生桌面应用。
>
> **One-liner**: A Windows tray launcher for DeepSeek Harness — one-click start, open, restart and exit of your local DSh web service, making it feel like a native desktop app.

DSh（DeepSeek Harness）桌面包：一个 Windows 系统托盘程序，用于一键启动、打开、重启和退出本机的 DSh Web 服务（默认端口 `3080`），并以 Edge 应用窗口方式打开 DSh Web GUI——让 DeepSeek Harness 用起来像一款原生桌面应用，而不是一条命令行 + 一个浏览器标签页。

*DSh (DeepSeek Harness) Desktop: a Windows system-tray program that starts, opens, restarts and exits your local DSh web service (default port `3080`) with one click, and opens the DSh Web GUI in an Edge app window — so DeepSeek Harness feels like a native desktop app instead of a command line plus a browser tab.*

---

## 与其他框架封装方案相比的优势 / Advantages over Other Framework Wrappers

把本地 AI 服务（DSh 等）封装成桌面应用，常见做法是 Electron、Tauri、pywebview 等方案，本项目选择了截然不同的路线：**纯 C# WinForms + 系统自带编译器**，在体积、依赖和复杂度上全面占优。

*To wrap a local AI service (like DSh) into a desktop app, common choices are Electron, Tauri, pywebview, etc. This project takes a completely different route: **pure C# WinForms compiled with the built-in system compiler**, winning on size, dependencies and complexity.*

| 封装方案 / Wrapper | 程序体积 / Size | 额外运行时 / Extra runtime | 构建复杂度 / Build complexity |
| --- | --- | --- | --- |
| **本项目 / This project (C# WinForms)** | **约 53 KB 单文件 exe / ~53 KB single exe** | **无（.NET 4.0 系统自带）/ None (built into Windows)** | **一条命令 / One command** |
| Electron 封装 / Electron | 100 MB 以上 / 100 MB+ | 内置整套 Chromium/Node / Bundles Chromium & Node | npm + electron-builder 打包 / npm + electron-builder |
| Tauri 封装 / Tauri | 数 MB ~ 10 MB | 需 Rust 工具链 / Requires Rust toolchain | Rust + 前端构建链 / Rust + frontend toolchain |
| pywebview 封装 / pywebview | 较小 / Small | 需 Python 环境 / Requires Python | pip 依赖 + 打包 / pip deps + packaging |

具体优势 / Key advantages:

- **极致轻量 / Ultra-light**: 单文件 exe 仅约 53 KB，秒开、零安装、免管理；Electron 方案动辄上百 MB。*A single ~53 KB exe — opens instantly with zero installation; Electron solutions run into hundreds of MB.*
- **零运行时依赖 / Zero extra runtime**: .NET Framework 4.0 是 Windows 自带组件，程序只依赖 DSh 本身就要用的 Node.js，不额外增加任何运行时。*.NET Framework 4.0 ships with Windows; the only external dependency is Node.js, which DSh already needs anyway.*
- **一条命令构建 / One-command build**: `build.ps1` 调用系统内置的 `csc.exe` 直接编译，无 node_modules、无打包工具、无配置文件。*`build.ps1` compiles with the built-in `csc.exe` — no node_modules, no bundlers, no config files.*
- **自动适配环境 / Auto environment detection**: 自动探测 Node.js 安装路径，并在 npx 缓存中自动选用**最新版本**的 DSh，升级 DSh 后无需改动任何配置。*Auto-detects the Node.js path and picks the **newest** DSh from the npx cache — no config changes needed after upgrading DSh.*
- **端口感知、不怕重复启动 / Port-aware, no duplicate instances**: 启动前先检测 3080 端口——服务已在运行就直接打开，绝不重复拉起。*Checks port 3080 first — if the service is already running, it just opens it instead of spawning a duplicate.*
- **服务可自愈 / Self-healing service**: 内置"重启服务"，能杀掉任何占用端口的进程（无论是否由本程序启动），等待端口释放后重新拉起，服务挂了一键恢复，无需命令行杀进程。*Built-in "Restart Service" kills whatever holds the port (even if not started by this app), waits for it to free up, then respawns — one click recovery without touching the command line.*
- **全程留痕 / Full logging**: 托盘日志（`app.log`）与服务输出日志（`server.log`）分离，出问题可快速定位。*Separate tray log (`app.log`) and server output log (`server.log`) for fast troubleshooting.*

## 日常使用的便捷性 / Convenience in Daily Use

- **双击即用 / Double-click to run**: 运行 `dsh-tray.exe` 即常驻系统托盘，无安装向导、无配置界面。*Run `dsh-tray.exe` and it lives in the system tray — no wizard, no settings UI.*
- **一键打开 / One-click open**: 左键单击托盘图标，自动完成"检测服务 → 拉起服务 → 等待就绪 → 打开窗口"全流程。*Left-click the tray icon; it handles "detect → spawn → wait ready → open window" automatically.*
- **桌面应用体验 / Desktop-app experience**: 以 Edge 应用窗口（`--app`）打开，无浏览器标签栏、无地址栏，界面专注沉浸。*Opens in an Edge app window (`--app`) — no tabs, no address bar, fully immersive.*
- **右键菜单三件事 / Three menu items**: 打开 DSh / 重启服务 / 退出，所有操作一目了然。*Open DSh / Restart Service / Exit — everything at a glance.*
- **单实例保护 / Single instance**: 重复运行不会开多个托盘，而是通知已运行的实例弹出窗口。*Running it again doesn't create a second tray — it asks the existing instance to show itself.*
- **建议开机自启 / Optional auto-start**: 把 exe 放入启动文件夹（`shell:startup`）即可开机自动驻留，随时一键打开 DSh。*Drop the exe into the startup folder (`shell:startup`) for auto-start, so DSh is one click away after boot.*

## 功能 / Features

- **打开 DSh / Open DSh**: 检测 `127.0.0.1:3080` 上的 DSh 服务，未运行时自动拉起，就绪后打开 Edge 应用窗口。*Checks the DSh service on `127.0.0.1:3080`; spawns it if not running, then opens the Edge app window when ready.*
- **重启服务 / Restart Service**: 杀掉当前占用 3080 端口的进程（无论是否由托盘启动），等待端口释放后重新启动服务，并自动打开 DSh 窗口。*Kills whatever currently holds port 3080 (whether or not the tray started it), waits for the port to free up, then restarts the service and opens DSh.*
- **退出 / Exit**: 结束托盘程序，并连带终止由它启动的 DSh 服务进程树。*Quits the tray and terminates the DSh process tree it spawned.*
- 托盘气泡提示：重启/失败时给出可见反馈。*Balloon tips give visible feedback on restart / failure.*
- 运行日志：写入 `dist/logs/app.log`（托盘）与 `dist/logs/server.log`（服务进程输出）。*Logs to `dist/logs/app.log` (tray) and `dist/logs/server.log` (server output).*

## 环境要求 / Requirements

- Windows 7 及以上（.NET Framework 4.0，系统自带）/ Windows 7+ (.NET Framework 4.0, built-in)
- Node.js（自动从 `C:\Program Files\nodejs\node.exe` 探测）/ Node.js (auto-detected at `C:\Program Files\nodejs\node.exe`)
- DSh 已通过 npx 安装（自动在 `%LOCALAPPDATA%\npm-cache\_npx` 下查找最新版本）/ DSh installed via npx (newest version auto-found under `%LOCALAPPDATA%\npm-cache\_npx`)

## 目录结构 / Directory Structure

```
quick_start/
├── tray/
│   ├── dsh-tray.cs     # 托盘程序源码（C# / WinForms）source code
│   ├── build.ps1       # 构建脚本（调用系统 csc.exe 编译）build script
│   └── icon.ico        # 托盘图标 tray icon
└── dist/               # 构建输出（git 忽略）build output (git-ignored)
    └── dsh-tray.exe
```

## 构建 / Build

```powershell
powershell -ExecutionPolicy Bypass -File tray\build.ps1
```

输出到 `dist\dsh-tray.exe`。/ *Outputs to `dist\dsh-tray.exe`.*

## 使用 / Usage

直接运行 `dist\dsh-tray.exe`（建议放入启动文件夹实现开机自启），托盘图标出现后：

*Just run `dist\dsh-tray.exe` (optionally place it in the startup folder). Once the tray icon appears:*

- 左键单击图标 = 打开 DSh / *Left-click the icon = open DSh*
- 右键图标弹出菜单：打开 DSh / 重启服务 / 退出 / *Right-click for the menu: Open DSh / Restart Service / Exit*

## 许可证 / License

[MIT](LICENSE)
