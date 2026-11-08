using System.Diagnostics;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;

namespace EmpireAtWar.Launcher;

/// <summary>Checks GitHub Releases, applies an update when available, then starts the game. Update failures never block play.</summary>
internal sealed class LauncherForm : Form
{
    private const string REPOSITORY_URL = "https://github.com/GolinSky/empire-at-war";
    private const string GAME_EXE = "EmpireAtWar.exe";

    private readonly LauncherLog _log;
    private readonly UpdateManager _updateManager;
    private readonly Label _status = new() { Dock = DockStyle.Top, Height = 40, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Top, Height = 24 };
    private readonly Button _play = new() { Text = "Play", Width = 120, Enabled = false };
    private readonly Button _skip = new() { Text = "Skip update", Width = 120, Enabled = false };
    private readonly CancellationTokenSource _skipUpdate = new();

    public LauncherForm(LauncherLog log)
    {
        _log = log;
        // Prereleases are accepted: Jenkins drafts default to prerelease and the manual GitHub Publish is the release gate.
        _updateManager = new UpdateManager(new GithubSource(REPOSITORY_URL, accessToken: null, prerelease: true));

        Text = $"Empire at War {_updateManager.CurrentVersion}";
        ClientSize = new Size(420, 130);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        Padding = new Padding(12);

        FlowLayoutPanel buttons = new() { Dock = DockStyle.Bottom, Height = 36, FlowDirection = FlowDirection.RightToLeft };
        buttons.Controls.Add(_play);
        buttons.Controls.Add(_skip);
        Controls.Add(buttons);
        Controls.Add(_progress);
        Controls.Add(_status);

        _play.Click += (_, _) => LaunchGame();
        _skip.Click += (_, _) => _skipUpdate.Cancel();
        Shown += async (_, _) => await UpdateThenLaunchAsync();
    }

    private async Task UpdateThenLaunchAsync()
    {
        if (!_updateManager.IsInstalled)
        {
            _log.Write("Not a Velopack install; update check skipped.");
            LaunchGame();
            return;
        }

        try
        {
            _status.Text = "Checking for updates...";
            UpdateInfo? update = await _updateManager.CheckForUpdatesAsync();
            if (update == null)
            {
                _log.Write($"Up to date: {_updateManager.CurrentVersion}.");
                LaunchGame();
                return;
            }

            string version = update.TargetFullRelease.Version.ToString();
            _status.Text = $"Downloading update {version}...";
            _skip.Enabled = true;
            _log.Write($"Downloading {version} (from {_updateManager.CurrentVersion}).");
            await _updateManager.DownloadUpdatesAsync(update, percent => BeginInvoke(() => _progress.Value = percent), cancelToken: _skipUpdate.Token);
            _log.Write($"Applying {version}.");
            _updateManager.ApplyUpdatesAndRestart(update.TargetFullRelease);
        }
        catch (OperationCanceledException)
        {
            _log.Write("Update skipped by the player.");
            LaunchGame();
        }
        catch (Exception exception)
        {
            // Offline, GitHub rate limit or a broken release must not block play.
            _log.Write($"Update failed: {exception}");
            _status.Text = "Update unavailable. Starting the installed version.";
            _skip.Enabled = false;
            _play.Enabled = true;
        }
    }

    private void LaunchGame()
    {
        string contentDir = VelopackLocator.Current.AppContentDir ?? AppContext.BaseDirectory;
        string gamePath = Path.Combine(contentDir, GAME_EXE);
        _log.Write($"Starting {gamePath}.");
        Process.Start(new ProcessStartInfo(gamePath) { WorkingDirectory = contentDir, UseShellExecute = false });
        Close();
    }
}
