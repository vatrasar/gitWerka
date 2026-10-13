/// <summary>
/// Screen: MainWindow
/// Purpose: Top-level desktop window hosting the application shell frame.
/// Available Functionalities:
/// - Houses the Shell HostView.
/// Key UI Elements:
/// - HostViewControl.
/// Navigation:
/// - Navigate From: Application startup.
/// - Navigate To: N/A (root container).
/// </summary>
namespace GitWerka;

using Avalonia.ReactiveUI;
using ReactiveUI;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    public MainWindow()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.OneWayBind(ViewModel, x => x.HostViewModel, x => x.HostViewControl.ViewModel);
        });
    }
}