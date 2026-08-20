using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialDebugStudio.Application.Abstractions.Checksums;
using SerialDebugStudio.Application.Abstractions.Formatting;
using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Desktop.Models;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Net;
using System.Text;

namespace SerialDebugStudio.Desktop.ViewModels;

public partial class UdpViewModel
    : ObservableObject, IAsyncDisposable
{
    private const int MaximumTrafficRowCount = 2000;

    private readonly IUdpTransportFactory
        _udpTransportFactory;

    private readonly IDataConversionService
        _dataConversionService;

    private readonly IChecksumService
        _checksumService;

    private ITransport? _transport;

    public UdpViewModel(
        IUdpTransportFactory udpTransportFactory,
        IDataConversionService dataConversionService,
        IChecksumService checksumService)
    {
        ArgumentNullException.ThrowIfNull(
            udpTransportFactory);

        ArgumentNullException.ThrowIfNull(
            dataConversionService);

        ArgumentNullException.ThrowIfNull(
            checksumService);

        _udpTransportFactory =
            udpTransportFactory;

        _dataConversionService =
            dataConversionService;

        _checksumService =
            checksumService;
    }

    public ObservableCollection<RawTrafficRow>
        TrafficRows
    {
        get;
    } = [];

    public IReadOnlyList<DataInputMode>
        InputModeOptions
    {
        get;
    } = Enum.GetValues<DataInputMode>();

    public IReadOnlyList<TextEncodingKind>
        TextEncodingOptions
    {
        get;
    } = Enum.GetValues<TextEncodingKind>();

    public IReadOnlyList<NumericDataType>
        NumericDataTypes
    {
        get;
    } = Enum.GetValues<NumericDataType>();

    public IReadOnlyList<ByteOrder>
        ByteOrders
    {
        get;
    } = Enum.GetValues<ByteOrder>();

    public IReadOnlyList<LineEnding>
        LineEndingOptions
    {
        get;
    } = Enum.GetValues<LineEnding>();

    public IReadOnlyList<ChecksumAlgorithm>
        ChecksumAlgorithmOptions
    {
        get;
    } = Enum.GetValues<ChecksumAlgorithm>();

    [ObservableProperty]
    private string localIpAddress =
        "0.0.0.0";

    [ObservableProperty]
    private int localPort =
        5000;

    [ObservableProperty]
    private string remoteIpAddress =
        "127.0.0.1";

    [ObservableProperty]
    private int remotePort =
        5001;

    [ObservableProperty]
    private ConnectionState connectionState =
        ConnectionState.Disconnected;

    [ObservableProperty]
    private string connectionErrorMessage =
        string.Empty;

    [ObservableProperty]
    private long receivedByteCount;

    [ObservableProperty]
    private long sentByteCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(
        nameof(IsLineEndingSelectionEnabled))]
    [NotifyPropertyChangedFor(
        nameof(IsChecksumSelectionEnabled))]
    private DataInputMode selectedInputMode =
        DataInputMode.Text;

    [ObservableProperty]
    private TextEncodingKind selectedTextEncoding =
        TextEncodingKind.Utf8;

    [ObservableProperty]
    private NumericDataType selectedNumericDataType =
        NumericDataType.Float32;

    [ObservableProperty]
    private ByteOrder selectedByteOrder =
        ByteOrder.BigEndian;

    [ObservableProperty]
    private LineEnding selectedLineEnding =
        LineEnding.None;

    [ObservableProperty]
    private ChecksumAlgorithm
        selectedChecksumAlgorithm =
            ChecksumAlgorithm.None;

    [ObservableProperty]
    private string sendInput =
        string.Empty;

    [ObservableProperty]
    private string sendErrorMessage =
        string.Empty;

    public bool IsEncodingSelectionEnabled =>
        SelectedInputMode == DataInputMode.Text;

    public bool IsLineEndingSelectionEnabled =>
        SelectedInputMode == DataInputMode.Text;

    public bool IsChecksumSelectionEnabled =>
        SelectedInputMode is
            DataInputMode.Hex or
            DataInputMode.Binary;

    public bool CanEditSettings =>
        ConnectionState is
            ConnectionState.Disconnected or
            ConnectionState.Faulted;

    public string ConnectionStatusText =>
        ConnectionState switch
        {
            ConnectionState.Disconnected =>
                "UDP dinleyicisi kapalı",

            ConnectionState.Connecting =>
                "UDP dinleyicisi başlatılıyor...",

            ConnectionState.Connected =>
                "UDP dinleyicisi aktif",

            ConnectionState.Disconnecting =>
                "UDP dinleyicisi durduruluyor...",

            ConnectionState.Faulted =>
                "UDP bağlantı hatası",

            _ => ConnectionState.ToString()
        };

    public string OpenCloseButtonText =>
        ConnectionState switch
        {
            ConnectionState.Connected =>
                "Dinlemeyi Durdur",

            ConnectionState.Connecting or
            ConnectionState.Disconnecting =>
                "İşlem sürüyor...",

            ConnectionState.Faulted =>
                "Tekrar Dene",

            _ =>
                "Dinlemeyi Başlat"
        };

    private bool CanToggleConnection()
    {
        if (ConnectionState is
            ConnectionState.Connecting or
            ConnectionState.Disconnecting)
        {
            return false;
        }

        if (ConnectionState ==
            ConnectionState.Connected)
        {
            return true;
        }

        return IsValidIpAddress(LocalIpAddress) &&
               IsValidIpAddress(RemoteIpAddress) &&
               IsValidPort(LocalPort) &&
               IsValidPort(RemotePort);
    }

    private bool CanSend()
    {
        return ConnectionState ==
                   ConnectionState.Connected &&
               _transport?.IsOpen == true &&
               !string.IsNullOrWhiteSpace(
                   SendInput);
    }

    [RelayCommand(
        CanExecute = nameof(CanToggleConnection))]
    private async Task ToggleConnectionAsync()
    {
        if (_transport?.IsOpen == true ||
            ConnectionState ==
            ConnectionState.Connected)
        {
            await CloseTransportAsync();
            return;
        }

        await OpenTransportAsync();
    }

    [RelayCommand(
        CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        ITransport? transport =
            _transport;

        if (transport is null ||
            !transport.IsOpen)
        {
            SendErrorMessage =
                "Veri göndermek için UDP dinleyicisi açık olmalıdır.";

            return;
        }

        try
        {
            byte[] payload =
                SelectedInputMode switch
                {
                    DataInputMode.Text =>
                        BuildTextPayload(),

                    DataInputMode.Hex =>
                        _dataConversionService.ParseHex(
                            SendInput),

                    DataInputMode.Binary =>
                        _dataConversionService.ParseBinary(
                            SendInput),

                    DataInputMode.Number =>
                        _dataConversionService.ParseIeee754(
                            SendInput,
                            SelectedNumericDataType,
                            SelectedByteOrder),

                    _ =>
                        throw new ArgumentOutOfRangeException(
                            nameof(SelectedInputMode),
                            SelectedInputMode,
                            "Desteklenmeyen giriş modu.")
                };

            if (payload.Length == 0)
            {
                SendErrorMessage =
                    "Gönderilecek veri boş olamaz.";

                return;
            }

            byte[] data =
                IsChecksumSelectionEnabled
                    ? _checksumService.Append(
                        payload,
                        SelectedChecksumAlgorithm)
                    : payload;

            await transport.SendAsync(data);

            var transferRecord =
                new DataTransferRecord(
                    DataDirection.Transmitted,
                    transport.Kind,
                    data);

            RunOnUiThread(
                () =>
                {
                    SentByteCount +=
                        transferRecord.ByteCount;

                    AppendTrafficRecord(
                        transferRecord);
                });

            SendErrorMessage =
                string.Empty;
        }
        catch (FormatException exception)
        {
            SendErrorMessage =
                SelectedInputMode switch
                {
                    DataInputMode.Hex =>
                        $"Geçersiz HEX girişi: {exception.Message}",

                    DataInputMode.Binary =>
                        $"Geçersiz Binary girişi: {exception.Message}",

                    DataInputMode.Number =>
                        $"Geçersiz IEEE 754 değeri: {exception.Message}",

                    _ =>
                        $"Geçersiz veri: {exception.Message}"
                };
        }
        catch (EncoderFallbackException exception)
        {
            SendErrorMessage =
                "Seçili encoding bu metni desteklemiyor: " +
                exception.Message;
        }
        catch (Exception exception)
        {
            SendErrorMessage =
                $"UDP verisi gönderilemedi: {exception.Message}";
        }
    }

    [RelayCommand]
    private void ClearTraffic()
    {
        TrafficRows.Clear();

        ReceivedByteCount = 0;
        SentByteCount = 0;
    }

    private async Task OpenTransportAsync()
    {
        if (!IsValidIpAddress(LocalIpAddress))
        {
            ConnectionErrorMessage =
                "Geçerli bir yerel IP adresi girin.";

            return;
        }

        if (!IsValidIpAddress(RemoteIpAddress))
        {
            ConnectionErrorMessage =
                "Geçerli bir uzak IP adresi girin.";

            return;
        }

        if (!IsValidPort(LocalPort))
        {
            ConnectionErrorMessage =
                "Yerel port 1-65535 arasında olmalıdır.";

            return;
        }

        if (!IsValidPort(RemotePort))
        {
            ConnectionErrorMessage =
                "Uzak port 1-65535 arasında olmalıdır.";

            return;
        }

        ConnectionErrorMessage =
            string.Empty;

        await DisposeCurrentTransportAsync();

        ITransport? transport = null;

        try
        {
            var settings =
                new UdpTransportSettings
                {
                    LocalIpAddress =
                        LocalIpAddress,

                    LocalPort =
                        LocalPort,

                    RemoteIpAddress =
                        RemoteIpAddress,

                    RemotePort =
                        RemotePort
                };

            transport =
                _udpTransportFactory.Create(
                    settings);

            _transport =
                transport;

            transport.ConnectionStateChanged +=
                OnTransportConnectionStateChanged;

            transport.DataReceived +=
                OnTransportDataReceived;

            await transport.OpenAsync();
        }
        catch (Exception exception)
        {
            if (transport is not null)
            {
                transport.ConnectionStateChanged -=
                    OnTransportConnectionStateChanged;

                transport.DataReceived -=
                    OnTransportDataReceived;

                await transport.DisposeAsync();
            }

            _transport =
                null;

            ConnectionState =
                ConnectionState.Faulted;

            ConnectionErrorMessage =
                "UDP dinleyicisi başlatılamadı: " +
                exception.Message;
        }
    }

    private async Task CloseTransportAsync()
    {
        ITransport? transport =
            _transport;

        if (transport is null)
        {
            ConnectionState =
                ConnectionState.Disconnected;

            return;
        }

        try
        {
            await transport.CloseAsync();

            ConnectionErrorMessage =
                string.Empty;
        }
        catch (Exception exception)
        {
            ConnectionState =
                ConnectionState.Faulted;

            ConnectionErrorMessage =
                "UDP dinleyicisi durdurulamadı: " +
                exception.Message;
        }
        finally
        {
            transport.ConnectionStateChanged -=
                OnTransportConnectionStateChanged;

            transport.DataReceived -=
                OnTransportDataReceived;

            await transport.DisposeAsync();

            _transport =
                null;

            if (ConnectionState !=
                ConnectionState.Faulted)
            {
                ConnectionState =
                    ConnectionState.Disconnected;
            }
        }
    }

    private void OnTransportConnectionStateChanged(
        object? sender,
        TransportConnectionStateChangedEventArgs eventArgs)
    {
        RunOnUiThread(
            () =>
            {
                ConnectionState =
                    eventArgs.CurrentState;

                if (!string.IsNullOrWhiteSpace(
                        eventArgs.ConnectionInfo.ErrorMessage))
                {
                    ConnectionErrorMessage =
                        eventArgs.ConnectionInfo.ErrorMessage;
                }
            });
    }

    private void OnTransportDataReceived(
        object? sender,
        TransportDataReceivedEventArgs eventArgs)
    {
        RunOnUiThread(
            () =>
            {
                ReceivedByteCount +=
                    eventArgs.ByteCount;

                AppendTrafficRecord(
                    eventArgs.Record);
            });
    }

    private byte[] BuildTextPayload()
    {
        string textWithLineEnding =
            _dataConversionService.ApplyLineEnding(
                SendInput,
                SelectedLineEnding);

        return _dataConversionService.EncodeText(
            textWithLineEnding,
            SelectedTextEncoding);
    }

    private void AppendTrafficRecord(
        DataTransferRecord record)
    {
        while (TrafficRows.Count >=
               MaximumTrafficRowCount)
        {
            TrafficRows.RemoveAt(0);
        }

        TrafficRows.Add(
            new RawTrafficRow(record));
    }

    private static bool IsValidIpAddress(
        string value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               IPAddress.TryParse(
                   value,
                   out _);
    }

    private static bool IsValidPort(
        int value)
    {
        return value is >= 1 and <= 65535;
    }

    private static void RunOnUiThread(
        Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        System.Windows.Threading.Dispatcher?
            dispatcher =
                System.Windows.Application
                    .Current?
                    .Dispatcher;

        if (dispatcher is null ||
            dispatcher.CheckAccess())
        {
            action();
            return;
        }

        _ = dispatcher.InvokeAsync(action);
    }

    partial void OnLocalIpAddressChanged(
        string value)
    {
        ConnectionErrorMessage =
            string.Empty;

        ToggleConnectionCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnLocalPortChanged(
        int value)
    {
        ConnectionErrorMessage =
            string.Empty;

        ToggleConnectionCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnRemoteIpAddressChanged(
        string value)
    {
        ConnectionErrorMessage =
            string.Empty;

        ToggleConnectionCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnRemotePortChanged(
        int value)
    {
        ConnectionErrorMessage =
            string.Empty;

        ToggleConnectionCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSendInputChanged(
        string value)
    {
        SendErrorMessage =
            string.Empty;

        SendCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedInputModeChanged(
        DataInputMode value)
    {
        SendErrorMessage =
            string.Empty;

        OnPropertyChanged(
            nameof(IsEncodingSelectionEnabled));

        SendCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedTextEncodingChanged(
        TextEncodingKind value)
    {
        SendErrorMessage =
            string.Empty;
    }

    partial void OnSelectedChecksumAlgorithmChanged(
        ChecksumAlgorithm value)
    {
        SendErrorMessage =
            string.Empty;
    }

    partial void OnConnectionStateChanged(
        ConnectionState value)
    {
        OnPropertyChanged(
            nameof(ConnectionStatusText));

        OnPropertyChanged(
            nameof(OpenCloseButtonText));

        OnPropertyChanged(
            nameof(CanEditSettings));

        ToggleConnectionCommand
            .NotifyCanExecuteChanged();

        SendCommand
            .NotifyCanExecuteChanged();
    }

    private async Task DisposeCurrentTransportAsync()
    {
        ITransport? transport =
            _transport;

        if (transport is null)
        {
            return;
        }

        transport.ConnectionStateChanged -=
            OnTransportConnectionStateChanged;

        transport.DataReceived -=
            OnTransportDataReceived;

        await transport.DisposeAsync();

        _transport =
            null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentTransportAsync();
    }
}