using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SerialDebugStudio.Application.Abstractions.Checksums;
using SerialDebugStudio.Application.Abstractions.Formatting;
using SerialDebugStudio.Application.Abstractions.Transports;
using SerialDebugStudio.Desktop.Models;
using SerialDebugStudio.Domain.Enums;
using SerialDebugStudio.Domain.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Text;

namespace SerialDebugStudio.Desktop.ViewModels;

public partial class MainWindowViewModel
    : ObservableObject, IAsyncDisposable
{
    private const int MaximumTrafficRowCount = 2000;

    private readonly ISerialPortDiscoveryService
        _serialPortDiscoveryService;

    private readonly ISerialTransportFactory
        _serialTransportFactory;

    private readonly IDataConversionService
        _dataConversionService;


    private readonly IChecksumService
        _checksumService;


    private ITransport? _transport;

    public MainWindowViewModel(
     ISerialPortDiscoveryService serialPortDiscoveryService,
     ISerialTransportFactory serialTransportFactory,
     IDataConversionService dataConversionService,
     IChecksumService checksumService)
    {
        ArgumentNullException.ThrowIfNull(
            serialPortDiscoveryService);

        ArgumentNullException.ThrowIfNull(
            serialTransportFactory);

        ArgumentNullException.ThrowIfNull(
            dataConversionService);

        ArgumentNullException.ThrowIfNull(
            checksumService);

        _serialPortDiscoveryService =
            serialPortDiscoveryService;

        _serialTransportFactory =
            serialTransportFactory;

        _dataConversionService =
            dataConversionService;

        _checksumService =
            checksumService;

        RefreshPorts();
    }

    public ObservableCollection<string> AvailablePorts
    {
        get;
    } = [];

    public ObservableCollection<RawTrafficRow> TrafficRows
    {
        get;
    } = [];

    public IReadOnlyList<int> BaudRates
    {
        get;
    } =
    [
        1200,
        2400,
        4800,
        9600,
        19200,
        38400,
        57600,
        115200,
        230400,
        460800,
        921600
    ];

    public IReadOnlyList<SerialDataBits> DataBitsOptions
    {
        get;
    } = Enum.GetValues<SerialDataBits>();

    public IReadOnlyList<SerialParity> ParityOptions
    {
        get;
    } = Enum.GetValues<SerialParity>();

    public IReadOnlyList<SerialStopBits> StopBitsOptions
    {
        get;
    } = Enum.GetValues<SerialStopBits>();

    public IReadOnlyList<SerialFlowControl> FlowControlOptions
    {
        get;
    } = Enum.GetValues<SerialFlowControl>();

    public IReadOnlyList<DataInputMode> InputModeOptions
    {
        get;
    } = Enum.GetValues<DataInputMode>();

    public IReadOnlyList<TextEncodingKind> TextEncodingOptions
    {
        get;
    } = Enum.GetValues<TextEncodingKind>();


    public IReadOnlyList<NumericDataType> NumericDataTypes { get; } =
    Enum.GetValues<NumericDataType>();

    public IReadOnlyList<ByteOrder> ByteOrders { get; } =
        Enum.GetValues<ByteOrder>();

    public IReadOnlyList<LineEnding> LineEndingOptions { get; } =
    Enum.GetValues<LineEnding>();


    public IReadOnlyList<ChecksumAlgorithm>
    ChecksumAlgorithmOptions
    {
        get;
    } = Enum.GetValues<ChecksumAlgorithm>();


    [ObservableProperty]
    private NumericDataType _selectedNumericDataType =
    NumericDataType.Float32;

    [ObservableProperty]
    private ByteOrder _selectedByteOrder =
    ByteOrder.BigEndian;

    [ObservableProperty]
    private string? selectedPort;

    [ObservableProperty]
    private int selectedBaudRate = 115200;

    [ObservableProperty]
    private SerialDataBits selectedDataBits =
        SerialDataBits.Eight;

    [ObservableProperty]
    private SerialParity selectedParity =
        SerialParity.None;

    [ObservableProperty]
    private SerialStopBits selectedStopBits =
        SerialStopBits.One;

    [ObservableProperty]
    private SerialFlowControl selectedFlowControl =
        SerialFlowControl.None;

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
    private string sendInput =
        string.Empty;

    [ObservableProperty]
    private string sendErrorMessage =
        string.Empty;

    [ObservableProperty]
    private LineEnding _selectedLineEnding =
    LineEnding.None;


    [ObservableProperty]
    private ChecksumAlgorithm selectedChecksumAlgorithm =
    ChecksumAlgorithm.None;


    public bool IsEncodingSelectionEnabled =>
        SelectedInputMode == DataInputMode.Text;

    public bool CanEditSettings =>
        ConnectionState is
            ConnectionState.Disconnected or
            ConnectionState.Faulted;

    public bool IsLineEndingSelectionEnabled =>
        SelectedInputMode == DataInputMode.Text;

    public bool IsChecksumSelectionEnabled =>
        SelectedInputMode is
            DataInputMode.Hex or
            DataInputMode.Binary;



    public string ConnectionStatusText =>
        ConnectionState switch
        {
            ConnectionState.Disconnected =>
                "Bağlantı kapalı",

            ConnectionState.Connecting =>
                "Bağlanıyor...",

            ConnectionState.Connected =>
                "Bağlı",

            ConnectionState.Disconnecting =>
                "Bağlantı kapatılıyor...",

            ConnectionState.Faulted =>
                "Bağlantı hatası",

            _ => ConnectionState.ToString()
        };

    public string OpenCloseButtonText =>
        ConnectionState switch
        {
            ConnectionState.Connected =>
                "Portu Kapat",

            ConnectionState.Connecting or
            ConnectionState.Disconnecting =>
                "İşlem sürüyor...",

            ConnectionState.Faulted =>
                "Tekrar Dene",

            _ => "Portu Aç"
        };

    private bool CanRefreshPorts() =>
        CanEditSettings;

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

        return !string.IsNullOrWhiteSpace(
            SelectedPort);
    }

    private bool CanSendRawTest()
    {
        return ConnectionState ==
                   ConnectionState.Connected &&
               _transport?.IsOpen == true;
    }

    private bool CanSend()
    {
        return ConnectionState ==
                   ConnectionState.Connected &&
               _transport?.IsOpen == true &&
               !string.IsNullOrWhiteSpace(SendInput);
    }

    [RelayCommand(CanExecute = nameof(CanRefreshPorts))]
    private void RefreshPorts()
    {
        string? previousSelection = SelectedPort;

        try
        {
            IReadOnlyList<string> portNames =
                _serialPortDiscoveryService
                    .GetAvailablePortNames();

            AvailablePorts.Clear();

            foreach (string portName in portNames)
            {
                AvailablePorts.Add(portName);
            }

            if (previousSelection is not null &&
                AvailablePorts.Contains(previousSelection))
            {
                SelectedPort = previousSelection;
            }
            else
            {
                SelectedPort =
                    AvailablePorts.FirstOrDefault();
            }

            ConnectionErrorMessage = string.Empty;
        }
        catch (Exception exception)
        {
            ConnectionErrorMessage =
                $"COM portları listelenemedi: " +
                exception.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanToggleConnection))]
    private async Task ToggleConnectionAsync()
    {
        if (_transport?.IsOpen == true ||
            ConnectionState == ConnectionState.Connected)
        {
            await CloseTransportAsync();
            return;
        }

        await OpenTransportAsync();
    }

    [RelayCommand(CanExecute = nameof(CanSendRawTest))]
    private async Task SendRawTestAsync()
    {
        ITransport? transport = _transport;

        if (transport is null ||
            !transport.IsOpen)
        {
            ConnectionErrorMessage =
                "Ham veri göndermek için port açık olmalıdır.";

            return;
        }

        // ASCII karşılığı: RAW-TEST
        byte[] testData =
        [
            0x52,
            0x41,
            0x57,
            0x2D,
            0x54,
            0x45,
            0x53,
            0x54
        ];

        try
        {
            await transport.SendAsync(testData);

            var transferRecord =
                new DataTransferRecord(
                    DataDirection.Transmitted,
                    transport.Kind,
                    testData);

            RunOnUiThread(
                () =>
                {
                    SentByteCount +=
                        transferRecord.ByteCount;

                    AppendTrafficRecord(
                        transferRecord);
                });

            ConnectionErrorMessage = string.Empty;
        }
        catch (Exception exception)
        {
            ConnectionErrorMessage =
                $"Ham veri gönderilemedi: " +
                exception.Message;
        }
    }

    [RelayCommand(CanExecute = nameof(CanSend))]
    private async Task SendAsync()
    {
        ITransport? transport = _transport;

        if (transport is null ||
            !transport.IsOpen)
        {
            SendErrorMessage =
                "Veri göndermek için port açık olmalıdır.";

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

                    _ => throw new ArgumentOutOfRangeException(
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

            SendErrorMessage = string.Empty;
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
                $"Veri gönderilemedi: {exception.Message}";
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
        if (string.IsNullOrWhiteSpace(SelectedPort))
        {
            ConnectionErrorMessage =
                "Önce bir COM port seçmelisiniz.";

            return;
        }

        ConnectionErrorMessage = string.Empty;

        await DisposeCurrentTransportAsync();

        var settings = new SerialPortSettings
        {
            PortName = SelectedPort,
            BaudRate = SelectedBaudRate,
            DataBits = SelectedDataBits,
            Parity = SelectedParity,
            StopBits = SelectedStopBits,
            FlowControl = SelectedFlowControl
        };

        ITransport transport =
            _serialTransportFactory.Create(settings);

        _transport = transport;

        transport.ConnectionStateChanged +=
            OnTransportConnectionStateChanged;

        transport.DataReceived +=
            OnTransportDataReceived;

        try
        {
            await transport.OpenAsync();
        }
        catch (Exception exception)
        {
            transport.ConnectionStateChanged -=
                OnTransportConnectionStateChanged;

            transport.DataReceived -=
                OnTransportDataReceived;

            await transport.DisposeAsync();

            _transport = null;

            ConnectionState =
                ConnectionState.Faulted;

            ConnectionErrorMessage =
                $"Port açılamadı: {exception.Message}";
        }
    }

    private async Task CloseTransportAsync()
    {
        ITransport? transport = _transport;

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
                $"Port kapatılamadı: " +
                exception.Message;
        }
        finally
        {
            transport.ConnectionStateChanged -=
                OnTransportConnectionStateChanged;

            transport.DataReceived -=
                OnTransportDataReceived;

            await transport.DisposeAsync();

            _transport = null;

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

    partial void OnSelectedPortChanged(
        string? value)
    {
        ToggleConnectionCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSendInputChanged(
        string value)
    {
        SendErrorMessage = string.Empty;

        SendCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedInputModeChanged(
        DataInputMode value)
    {
        SendErrorMessage = string.Empty;

        OnPropertyChanged(
            nameof(IsEncodingSelectionEnabled));

        SendCommand
            .NotifyCanExecuteChanged();
    }

    partial void OnSelectedTextEncodingChanged(
        TextEncodingKind value)
    {
        SendErrorMessage = string.Empty;
    }

    partial void OnSelectedChecksumAlgorithmChanged(
        ChecksumAlgorithm value)
    {
        SendErrorMessage = string.Empty;
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

        RefreshPortsCommand
            .NotifyCanExecuteChanged();

        SendRawTestCommand
            .NotifyCanExecuteChanged();

        SendCommand
            .NotifyCanExecuteChanged();
    }

    private async Task DisposeCurrentTransportAsync()
    {
        ITransport? transport = _transport;

        if (transport is null)
        {
            return;
        }

        transport.ConnectionStateChanged -=
            OnTransportConnectionStateChanged;

        transport.DataReceived -=
            OnTransportDataReceived;

        await transport.DisposeAsync();

        _transport = null;
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeCurrentTransportAsync();
    }
}