# DeepSeek Harness Desktop

DSh（DeepSeek Harness）桌面包：一个 Windows 系统托盘程序，用于一键启动、打开、重启和退出本机的 DSh Web 服务（默认端口 `3080`），并以 Edge 应用窗口方式打开 DSh Web GUI——让 DeepSeek Harness 用起来像一款原生桌面应用，而不是一条命令行 + 一个浏览器标签页。

## 与其他框架封装方案相比的优势

把本地 AI 服务（DSh 等）封装成桌面应用，常见做法是 Electron、Tauri、pywebview 等方案，本项目选择了截然不同的路线：**纯 C# WinForms + 系统自带编译器**，在体积、依赖和复杂度上全面占优：

| 封装方案 | 程序体积 | 额外运行时 | 构建/打包复杂度 |
| --- | --- | --- | --- |
| **本项目（C# WinForms）** | **约 53 KB 单文件 exe** | **无（.NET Framework 4.0 Windows 自带）** | **一条 PowerShell 命令** |
| Electron 封装 | 100 MB 以上 | 内置整套 Chromium/Node | 需 npm 安装 + electron-builder 打包 |
| Tauri 封装 | 数 MB ~ 10 MB | 需 Rust 工具链 | 需 Rust + 前端构建链 |
| pywebview 封装 | 较小 | 需 Python 环境 | 需 pip 安装依赖并打包 |

具体优势：

- **极致轻量**：单文件 exe 仅约 53 KB，秒开、零安装、免管理，而 Electron 方案动辄上百 MB
- **零运行时依赖**：.NET Framework 4.0 是 Windows 自带组件，程序只依赖 DSh 本身就要用的 Node.js，不额外增加任何运行时
- **一条命令构建**：`build.ps1` 调用系统内置的 `csc.exe` 直接编译，无 node_modules、无打包工具、无配置文件
- **自动适配环境**：自动探测 Node.js 安装路径，并在 npx 缓存中自动选用**最新版本**的 DSh，升级 DSh 后无需改动任何配置
- **端口感知、不怕重复启动**：启动前先检测 3080 端口——服务已在运行就直接打开，绝不重复拉起第二个实例
- **服务可自愈**：内置"重启服务"，能杀掉任何占用端口的进程（无论是否由本程序启动），等待端口释放后重新拉起，服务挂了一键恢复，无需命令行杀进程
- **全程留痕**：托盘日志（`app.log`）与服务输出日志（`server.log`）分离，出问题可快速定位

## 日常使用的便捷性

- **双击即用**：运行 `dsh-tray.exe` 即常驻系统托盘，无安装向导、无配置界面
- **一键打开**：左键单击托盘图标，自动完成"检测服务 → 拉起服务 → 等待就绪 → 打开窗口"全流程
- **桌面应用体验**：以 Edge 应用窗口（`--app`）打开，无浏览器标签栏、无地址栏，界面专注沉浸
- **右键菜单三件事**：打开 DSh / 重启服务 / 退出，所有操作一目了然
- **单实例保护**：重复运行不会开多个托盘，而是通知已运行的实例弹出窗口
- **建议开机自启**：把 exe 放入启动文件夹（`shell:startup`）即可开机自动驻留，随时一键打开 DSh

## 功能

- **打开 DSh**：检测 `127.0.0.1:3080` 上的 DSh 服务，未运行时自动拉起，就绪后打开 Edge 应用窗口
- **重启服务**：杀掉当前占用 3080 端口的进程（无论是否由托盘启动），等待端口释放后重新启动服务，并自动打开 DSh 窗口
- **退出**：结束托盘程序，并连带终止由它启动的 DSh 服务进程树
- 托盘气泡提示：重启/失败时给出可见反馈
- 运行日志：写入 `dist/logs/app.log`（托盘）与 `dist/logs/server.log`（服务进程输出）

## 环境要求

- Windows 7 及以上（.NET Framework 4.0，系统自带）
- Node.js（自动从 `C:\Program Files\nodejs\node.exe` 探测）
- DSh 已通过 npx 安装（自动在 `%LOCALAPPDATA%\npm-cache\_npx` 下查找最新版本）

## 目录结构

```
quick_start/
├── tray/
│   ├── dsh-tray.cs     # 托盘程序源码（C# / WinForms）
│   ├── build.ps1       # 构建脚本（调用系统 csc.exe 编译）
│   └── icon.ico        # 托盘图标
└── dist/               # 构建输出（git 忽略）
    └── dsh-tray.exe
```

## 构建

```powershell
powershell -ExecutionPolicy Bypass -File tray\build.ps1
```

输出到 `dist\dsh-tray.exe`。

## 使用

直接运行 `dist\dsh-tray.exe`（建议放入启动文件夹实现开机自启），托盘图标出现后：

- 左键单击图标 = 打开 DSh
- 右键图标弹出菜单：打开 DSh / 重启服务 / 退出

## 许可证

[MIT](LICENSE)
