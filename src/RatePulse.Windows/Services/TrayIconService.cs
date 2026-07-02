using System.Drawing;
using System.Windows;
using Forms = System.Windows.Forms;

namespace RatePulse.Windows.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Window window;
    private readonly Action requestExit;

    public TrayIconService(Window window, Action requestExit)
    {
        this.window = window;
        this.requestExit = requestExit;

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "RatePulse",
            Visible = true,
            ContextMenuStrip = BuildContextMenu()
        };

        notifyIcon.DoubleClick += (_, _) => RestoreWindow();
    }

    public void ShowInfo(string title, string message)
    {
        notifyIcon.BalloonTipTitle = title;
        notifyIcon.BalloonTipText = message;
        notifyIcon.ShowBalloonTip(2000);
    }

    public void RestoreWindow()
    {
        window.Show();
        window.WindowState = WindowState.Normal;
        window.Activate();
    }

    public void Dispose()
    {
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
    }

    private Forms.ContextMenuStrip BuildContextMenu()
    {
        var menu = new Forms.ContextMenuStrip();

        menu.Items.Add("Show", null, (_, _) => RestoreWindow());
        menu.Items.Add("Exit", null, (_, _) => requestExit());

        return menu;
    }
}
