# DeepSeek Harness Desktop

DSh（DeepSeek Harness）桌面包：一个 Windows 系统托盘程序，用于一键启动、打开、重启和退出本机的 DSh Web 服务（默认端口 `3080`），并以 Edge 应用窗口方式打开 DSh Web GUI。

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
