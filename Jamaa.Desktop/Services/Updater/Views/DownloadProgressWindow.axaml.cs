using Avalonia.Controls;
using NetSparkleUpdater.Interfaces;
using NetSparkleUpdater.Events;
using Huskui.Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Jamaa.Desktop.Services.Updater.Views;

public partial class DownloadProgressWindow : AppWindow, IDownloadProgress
{
    private TextBlock? _instructionText;
    private TextBlock? _fileNameText;
    private ProgressBar? _progressBar;
    private TextBlock? _progressText;
    private Button? _installButton;
    private Button? _cancelButton;

    public DownloadProgressWindow()
    {
        InitializeComponent();
        SetupControls();
    }

    public DownloadProgressWindow(string title, string fileName)
    {
        InitializeComponent();
        SetupControls();
        Title = title;
        if (_fileNameText != null) _fileNameText.Text = fileName;
        
        if (_cancelButton != null) _cancelButton.Click += (s, e) => Close();
        if (_installButton != null) _installButton.Click += (s, e) =>
        {
            DownloadProcessCompleted?.Invoke(this, new DownloadInstallEventArgs(true));
            Close();
        };
    }

    private void SetupControls()
    {
        _instructionText = this.FindControl<TextBlock>("InstructionText");
        _fileNameText = this.FindControl<TextBlock>("FileNameText");
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _progressText = this.FindControl<TextBlock>("ProgressText");
        _installButton = this.FindControl<Button>("InstallButton");
        _cancelButton = this.FindControl<Button>("CancelButton");
    }

    public event DownloadInstallEventHandler? DownloadProcessCompleted;

    public void OnDownloadProgressChanged(object sender, ItemDownloadProgressEventArgs e)
    {
        if (_progressBar != null) _progressBar.Value = e.ProgressPercentage;
        if (_progressText != null) _progressText.Text = $"{e.ProgressPercentage}%";
    }

    public void FinishedDownloadingFile(bool isSuccess)
    {
        if (isSuccess)
        {
            if (_instructionText != null) _instructionText.Text = "Download Complete";
            if (_progressBar != null) _progressBar.Value = 100;
            if (_progressText != null) _progressText.Text = "100%";
            if (_installButton != null) _installButton.IsVisible = true;
        }
    }

    public bool DisplayErrorMessage(string message)
    {
        if (_instructionText != null) _instructionText.Text = "Error: " + message;
        return true;
    }

    public void SetDownloadAndInstallButtonEnabled(bool enabled)
    {
        if (_installButton != null) _installButton.IsEnabled = enabled;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
