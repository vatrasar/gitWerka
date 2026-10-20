<div align="center">

<img src="Assets/icon.svg" alt="GitWerka Logo" width="130" height="130" />

# GitWerka

  **A modern, lightning-fast, and elegant desktop Git client.**  
  *Built with C# .NET 10, Avalonia UI, and ReactiveUI.*

<br />

  [![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
  [![Avalonia UI](https://img.shields.io/badge/Avalonia_UI-11.2-8A2BE2?style=for-the-badge&logo=avalonia&logoColor=white)](https://avaloniaui.net/)
  [![ReactiveUI](https://img.shields.io/badge/Architecture-ReactiveUI_MVI-E0234E?style=for-the-badge)](https://reactiveui.net/)
  [![Platform](https://img.shields.io/badge/Platform-Linux%20|%20Windows%20|%20macOS-2563EB?style=for-the-badge)](https://github.com/vatrasar/gitWerka)
  [![License](https://img.shields.io/badge/License-MIT-F59E0B?style=for-the-badge)](LICENSE)

</div>

<br />

---

## 📖 Overview

**GitWerka** is a cross-platform desktop Git GUI designed from the ground up for speed, visual clarity, and streamlined daily developer workflows. Inspired by the visual ergonomics of tools like *Gitnuro* and *GitKraken*, GitWerka runs entirely on native UI technologies—**without Electron or web wrappers**—delivering near-instant startup, smooth animations, and a minimal memory footprint.

Whether inspecting complex branch topologies, resolving merge conflicts line-by-line, performing interactive rebases, or letting AI summarize staged changes into Conventional Commits, GitWerka provides a delightful developer experience.

---

## ✨ Key Features

### 🌳 Visual Commit Graph

* **Multi-Lane Topology**: Real-time rendering of branch lanes, merge lines, and commit ancestry with dynamic lane column sizing.
* **Responsive Commit Cards**: Clean display of commit messages, tags, branch pointers, commit SHAs, authors, and dates.
* **Instant Filtering & Search**: Filter commits dynamically by commit message, author name, or hash.
* **Comprehensive Context Actions**: Checkout, Cherry-pick, Revert, Reset (`Soft`, `Mixed`, `Hard`), Branch/Tag creation, and Rebase directly from commit context menus.

### ⚡ Working Tree & GitKraken-Style `// WIP` Row

* **Uncommitted Changes in Graph**: Active changes appear right at the top of your commit log as a `// WIP` item.
* **Granular Staging**: Inspect file diffs with line-level clarity, stage/unstage individual files, or stage all in one click.
* **Commit Composer**: Write clean commit messages with amend support, co-authors, and validation checks.

### 🤖 AI-Assisted Commit Message Generator

* **Automatic Change Summaries**: One-click commit message generation analyzing your staged diffs via OpenRouter API.
* **Customizable Styles & Models**: Configure preferred AI models, Conventional Commit formats, and token limits.
* **Resilient Rate-Limit Handling**: Automatic backoff and retry mechanism for API rate limits (HTTP 429).

### ⚔️ 3-Panel Merge Conflict Resolver

* **GitKraken-Inspired Workflow**: Dedicated 3-panel split screen comparing **Ours (Local)** vs **Theirs (Remote)** with a live **Interactive Merged Output**.
* **Line-by-Line Selection**: Take individual lines or whole hunks with intuitive visual checkmark toggles.
* **Live Editable Editor**: Fine-tune the merged output directly in the built-in code editor with line numbers and source indicators (`A` / `B`).
* **Conflict Navigation**: Jump effortlessly between conflicts with Previous/Next controls, resolve, and stage directly.

### 🔀 Visual Interactive Rebase

* **Visual Step Planner**: Plan your rebase with dedicated actions: `Pick`, `Reword`, `Edit`, `Squash`, `Fixup`, and `Drop`.
* **Reorder Steps**: Move commits up or down visually before executing the rebase safely.

### 👤 Multi-Profile & Identity Management

* **Git Author Identities**: Maintain multiple author configurations (name & email) for personal and work projects.
* **GitHub Credentials**: Securely store GitHub credentials and access tokens.
* **Quick Identity Switching**: Switch active profiles per repository with a single click from the toolbar, preventing accidental commits under the wrong email.

### ⭐ Favorites Drawer & Repository Hub

* **Repository Hub**: Recent repositories list, quick search, clone from remote URL, or initialize a new local repository.
* **Favorites Dock**: Categorize repositories into custom expandable groups (e.g. *Client Work*, *Open Source*, *Personal*).
* **One-Click Quick Switch**: Switch between repositories without returning to the welcome screen.

### 🚀 Desktop & System Shortcuts

* **External Launchers**: Open the active repository in your system file manager, default terminal, or preferred IDE (VS Code, Rider, Sublime, etc.) with a single click.
* **Background Sync**: Automatic repository status refresh every 5 seconds and configurable periodic remote fetch.

---

## 🛠️ Tech Stack & Architecture

GitWerka follows clean architectural principles and decoupled domain design:

| Layer / Component      | Technology                                              | Description                                                                               |
| ---------------------- | ------------------------------------------------------- | ----------------------------------------------------------------------------------------- |
| **Language & Runtime** | C# 13 / .NET 10                                         | Latest performance improvements, modern C# idioms, and native AOT capabilities            |
| **GUI Framework**      | [Avalonia UI 11.2](https://avaloniaui.net/)             | Cross-platform XAML framework with Fluent Theme and custom dark palette                   |
| **State & Navigation** | [ReactiveUI](https://reactiveui.net/)                   | Reactive Functional MVVM/MVI architecture, immutable state records, and decoupled routing |
| **Git Engine**         | [LibGit2Sharp](https://github.com/libgit2/libgit2sharp) | High-performance native LibGit2 bindings for reliable Git operations                      |
| **Persistence**        | EF Core 10 + SQLite                                     | Local storage for recent repositories, profiles, credentials, and settings                |
| **Icons & Design**     | Material.Icons.Avalonia                                 | Modern Fluent/Material iconography                                                        |

---

## 🚀 Getting Started

### Prerequisites

* [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
* [Git](https://git-scm.com/) installed on your system
* Linux dependencies (for Ubuntu/Debian):
  
  ```bash
  sudo apt install -y libfontconfig1 libx11-6 libice6 libsm6 libxrandr2 libxrender1 libxtst6 libgl1
  ```

### Build & Run Locally

1. **Clone the repository:**
   
   ```bash
   git clone https://github.com/vatrasar/gitWerka.git
   cd gitWerka
   ```

2. **Restore dependencies:**
   
   ```bash
   dotnet restore
   ```

3. **Run the application:**
   
   ```bash
   dotnet run --project GitWerka.csproj
   ```

---

## 📦 Debian Package (.deb)

GitWerka provides an automated packaging script to build a self-contained Debian package for Linux:

```bash
# Build the .deb package (from project root)
./build-deb.sh 2.1.0

# Install the generated package
sudo dpkg -i build/gitwerka_2.1.0_amd64.deb

# Launch GitWerka
gitwerka
```

The package automatically installs:

* Self-contained binaries into `/opt/GitWerka`
* System launcher symlinks into `/usr/bin/gitwerka` and `/usr/bin/GitWerka`
* Desktop entry with icon into `/usr/share/applications/GitWerka.desktop`
* Scalable SVG icons into `/usr/share/icons/hicolor/scalable/apps/` and `/usr/share/pixmaps/`

---

## ⚙️ Configuration & AI Setup

GitWerka allows fine-grained customization directly from the in-app **Settings** modal:

* **AI Commit Generator**: Set your [OpenRouter](https://openrouter.ai/) API key, specify any supported LLM model (e.g. `anthropic/claude-3.5-sonnet`, `openai/gpt-4o-mini`), and choose commit formatting preferences.
* **Default Projects Directory**: Configure your default repository workspace folder for streamlined cloning and creation.
* **Graph Display**: Customize the maximum number of visible branch lanes displayed simultaneously in the commit graph.

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome!

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'feat: add some amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

<br />

<div align="center">
  <sub>Crafted with passion for clean code and great Git ergonomics.</sub>
</div>
