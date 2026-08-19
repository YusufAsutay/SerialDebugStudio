using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Desktop.ViewModels;
using SerialDebugStudio.Infrastructure.Transports.Serial;
using SerialDebugStudio.Application.Abstractions.Formatting;
using SerialDebugStudio.Application.Services.Formatting;
using SerialDebugStudio.Application.Abstractions.Checksums;
using SerialDebugStudio.Application.Services.Checksums;

namespace SerialDebugStudio.Desktop;

public partial class App : System.Windows.Application
{
    private IHost? _host;

    protected override async void OnStartup(
        System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);

        HostApplicationBuilder builder =
            Host.CreateApplicationBuilder(e.Args);

        builder.Services.AddSingleton<
            IDataConversionService,
            DataConversionService>();

        builder.Services.AddSingleton<
            IChecksumService, 
            ChecksumService>();

        builder.Services.AddSingleton<
            ISerialPortDiscoveryService,
            SerialPortDiscoveryService>();

        builder.Services.AddSingleton<
            ISerialTransportFactory,
            SerialTransportFactory>();

        builder.Services.AddSingleton<
            MainWindowViewModel>();

        builder.Services.AddSingleton<
            MainWindow>();

        _host = builder.Build();

        await _host.StartAsync();

        MainWindow mainWindow =
            _host.Services
                .GetRequiredService<MainWindow>();

        MainWindow = mainWindow;

        mainWindow.Show();
    }

    protected override async void OnExit(
        System.Windows.ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();

            if (_host is IAsyncDisposable asyncHost)
            {
                await asyncHost.DisposeAsync();
            }
            else
            {
                _host.Dispose();
            }

            _host = null;
        }

        base.OnExit(e);
    }
}