# DeepSeek Harness Desktop

> DeepSeek Harness 桌面包——常驻 Windows 托盘的启动器，一键启动、打开、重启、退出本地 DSh Web 服务。
> *A Windows tray launcher for DeepSeek Harness: one-click start, open, restart and exit of your local DSh web service.*

## 快速开始 / Quick Start

1. 下载 `dist/dsh-tray.exe`（已编译，无需任何构建步骤，双击即用）/ *Download `dist/dsh-tray.exe` — pre-built, double-click and run.*
2. 左键单击托盘图标 = 打开 DSh / *Left-click the tray icon = open DSh*
3. 右键菜单：**打开 DSh / 重启服务 / 退出** / *Right-click menu: **Open DSh / Restart Service / Exit***

> 提示：把 exe 放入启动文件夹（`shell:startup`）可实现开机自启。/ *Tip: drop the exe into the startup folder (`shell:startup`) for auto-start.*

## 功能 / Features

| 中文 | English |
| --- | --- |
| **打开 DSh**：服务未运行则自动拉起，就绪后打开 Edge 应用窗口 | **Open DSh**: auto-spawns the service if not running, opens the Edge app window when ready |
| **重启服务**：杀掉占用 3080 端口的进程（无论谁启动的），等端口释放后重新拉起 | **Restart Service**: kills whatever holds port 3080 (whoever started it), waits, then respawns |
| **退出**：退出托盘，并终止由它启动的服务进程 | **Exit**: quits the tray and its spawned service tree |
| 托盘气泡提示重启/失败状态，日志写入 `dist/logs/` | Balloon tips for restart/failure status; logs in `dist/logs/` |

## 为什么选它 / Why This Wrapper

- **轻量**：单文件约 53 KB，而 Electron 方案动辄上百 MB / *Ultra-light (~53 KB single exe) vs. 100 MB+ Electron builds*
- **零额外依赖**：仅需 Node.js（DSh 本身就要），无需安装任何运行时 / *No extra runtime — just Node.js, which DSh already needs*
- **自动适配**：自动使用 npx 缓存中最新版 DSh，升级无需改配置 / *Auto-picks the newest DSh in the npx cache — no config after upgrades*
- **不怕重复启动**：检测到服务已在运行就直接打开，绝不重复拉起 / *Port-aware: never spawns a duplicate if the service is already running*

## 构建 / Build（可选 / Optional）

```powershell
powershell -ExecutionPolicy Bypass -File tray\build.ps1
```

## 许可证 / License

[MIT](LICENSE)
