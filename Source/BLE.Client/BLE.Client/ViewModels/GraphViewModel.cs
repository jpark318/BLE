﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Acr.UserDialogs;
using BLE.Client.Extensions;
using MvvmCross.Core.ViewModels;
using MvvmCross.Platform;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Extensions;
using Plugin.Permissions.Abstractions;
using Plugin.Settings.Abstractions;
using Syncfusion.SfChart.XForms;
using Xamarin.Forms;

namespace BLE.Client.ViewModels {
    public class GraphViewModel : BaseViewModel {
        private readonly IBluetoothLE _bluetoothLe;
        private readonly IUserDialogs _userDialogs;
        private readonly ISettings _settings;
        public double maxRed { get; set; } = 60000;
        public double minRed { get; set; } = 60000;
        public double maxIr { get; set; } = 0;
        public double minIr { get; set; } = 0;
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => _bluetoothLe.IsOn;
        public string StateText => GetStateText();
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();
        public ObservableCollection<BleDataModel> DataRed { get; set; }
        public ObservableCollection<BleDataModel> DataIr { get; set; }
        public ObservableCollection<BleDataModel> DataTemp { get; set; }
        public MvxCommand RefreshCommand => new MvxCommand(() => TryStartScanning(true));
        public MvxCommand<DeviceListItemViewModel> DisconnectCommand => new MvxCommand<DeviceListItemViewModel>(DisconnectDevice);
        public MvxCommand<DeviceListItemViewModel> ConnectDisposeCommand => new MvxCommand<DeviceListItemViewModel>(ConnectAndDisposeDevice);
        public ObservableCollection<DeviceListItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceListItemViewModel>();
        private Guid _previousGuid;
        private CancellationTokenSource _cancellationTokenSource;
        private Byte[] CharacteristicValue = new Byte[20];
        private UInt16 red;
        private UInt16 ir;
        public MvxCommand ConnectToPreviousCommand => new MvxCommand(ConnectToPreviousDeviceAsync, CanConnectToPrevious);
        bool _useAutoConnect;
        readonly IPermissions _permissions;

        public Guid PreviousGuid {
            get { return _previousGuid; }
            set {
                _previousGuid = value;
                _settings.AddOrUpdateValue("lastguid", _previousGuid.ToString());
                RaisePropertyChanged();
                RaisePropertyChanged(() => ConnectToPreviousCommand);
            }
        }

        public DeviceListItemViewModel SelectedDevice {
            get { return null; }
            set {
                if (value != null) {
                    HandleSelectedDevice(value);
                }
                RaisePropertyChanged();
            }
        }

        public bool UseAutoConnect {
            get {
                return _useAutoConnect;
            }

            set {
                if (_useAutoConnect == value)
                    return;

                _useAutoConnect = value;
                RaisePropertyChanged();
            }
        }

        public MvxCommand StopScanCommand => new MvxCommand(() => {
            _cancellationTokenSource.Cancel();
            CleanupCancellationToken();
            RaisePropertyChanged(() => IsRefreshing);
        }, () => _cancellationTokenSource != null);

        public GraphViewModel(IBluetoothLE bluetoothLe, IAdapter adapter, IUserDialogs userDialogs, ISettings settings, IPermissions permissions) : base(adapter) {
            _permissions = permissions;
            _bluetoothLe = bluetoothLe;
            _userDialogs = userDialogs;
            _settings = settings;
            // quick and dirty :>
            _bluetoothLe.StateChanged += OnStateChanged;
            Adapter.DeviceDiscovered += OnDeviceDiscovered;
            Adapter.ScanTimeoutElapsed += Adapter_ScanTimeoutElapsed;
            Adapter.DeviceDisconnected += OnDeviceDisconnected;
            Adapter.DeviceConnectionLost += OnDeviceConnectionLost;
            //Adapter.DeviceConnected += (sender, e) => Adapter.DisconnectDeviceAsync(e.Device);

            DataRed = new ObservableCollection<BleDataModel>();
            DataIr = new ObservableCollection<BleDataModel>();
        }

        private Task GetPreviousGuidAsync() {
            return Task.Run(() => {
                var guidString = _settings.GetValueOrDefault("lastguid", string.Empty);
                PreviousGuid = !string.IsNullOrEmpty(guidString) ? Guid.Parse(guidString) : Guid.Empty;
            });
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e) {
            Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();

            _userDialogs.HideLoading();
            _userDialogs.ErrorToast("Error", $"Connection LOST {e.Device.Name}", TimeSpan.FromMilliseconds(6000));
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e) {
            RaisePropertyChanged(nameof(IsStateOn));
            RaisePropertyChanged(nameof(StateText));
            //TryStartScanning();
        }

        private string GetStateText() {
            switch (_bluetoothLe.State) {
                case BluetoothState.Unknown:
                    return "Unknown BLE state.";
                case BluetoothState.Unavailable:
                    return "BLE is not available on this device.";
                case BluetoothState.Unauthorized:
                    return "You are not allowed to use BLE.";
                case BluetoothState.TurningOn:
                    return "BLE is warming up, please wait.";
                case BluetoothState.On:
                    return "BLE is on.";
                case BluetoothState.TurningOff:
                    return "BLE is turning off. That's sad!";
                case BluetoothState.Off:
                    return "BLE is off. Turn it on!";
                default:
                    return "Unknown BLE state.";
            }
        }

        private void Adapter_ScanTimeoutElapsed(object sender, EventArgs e) {
            RaisePropertyChanged(() => IsRefreshing);

            CleanupCancellationToken();
        }

        private void OnDeviceDiscovered(object sender, DeviceEventArgs args) {
            AddOrUpdateDevice(args.Device);
        }

        private void AddOrUpdateDevice(IDevice device) {
            InvokeOnMainThread(() => {
                var vm = Devices.FirstOrDefault(d => d.Device.Id == device.Id);
                if (vm != null) {
                    vm.Update();
                } else {
                    Devices.Add(new DeviceListItemViewModel(device));
                }
            });
        }

        public override async void Resume() {
            base.Resume();

            await GetPreviousGuidAsync();
            //TryStartScanning();

            GetSystemConnectedOrPairedDevices();

        }

        private void GetSystemConnectedOrPairedDevices() {
            try {
                //heart rate
                var guid = Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb");

                // SystemDevices = Adapter.GetSystemConnectedOrPairedDevices(new[] { guid }).Select(d => new DeviceListItemViewModel(d)).ToList();
                // remove the GUID filter for test
                // Avoid to loose already IDevice with a connection, otherwise you can't close it
                // Keep the reference of already known devices and drop all not in returned list.
                var pairedOrConnectedDeviceWithNullGatt = Adapter.GetSystemConnectedOrPairedDevices();
                SystemDevices.RemoveAll(sd => !pairedOrConnectedDeviceWithNullGatt.Any(p => p.Id == sd.Id));
                SystemDevices.AddRange(pairedOrConnectedDeviceWithNullGatt.Where(d => !SystemDevices.Any(sd => sd.Id == d.Id)).Select(d => new DeviceListItemViewModel(d)));
                RaisePropertyChanged(() => SystemDevices);
            } catch (Exception ex) {
                Trace.Message("Failed to retreive system connected devices. {0}", ex.Message);
            }
        }

        public List<DeviceListItemViewModel> SystemDevices { get; private set; } = new List<DeviceListItemViewModel>();

        public override void Suspend() {
            base.Suspend();

            Adapter.StopScanningForDevicesAsync();
            RaisePropertyChanged(() => IsRefreshing);
        }

        private async void TryStartScanning(bool refresh = false) {
            if (Xamarin.Forms.Device.OS == Xamarin.Forms.TargetPlatform.Android) {
                var status = await _permissions.CheckPermissionStatusAsync(Permission.Location);
                if (status != PermissionStatus.Granted) {
                    var permissionResult = await _permissions.RequestPermissionsAsync(Permission.Location);

                    if (permissionResult.First().Value != PermissionStatus.Granted) {
                        _userDialogs.ShowError("Permission denied. Not scanning.");
                        return;
                    }
                }
            }

            if (IsStateOn && (refresh || !Devices.Any()) && !IsRefreshing) {
                ScanForDevices();
            }
        }

        private async void ScanForDevices() {
            Devices.Clear();

            foreach (var connectedDevice in Adapter.ConnectedDevices) {
                //update rssi for already connected evices (so tha 0 is not shown in the list)
                try {
                    await connectedDevice.UpdateRssiAsync();
                } catch (Exception ex) {
                    Mvx.Trace(ex.Message);
                    _userDialogs.ShowError($"Failed to update RSSI for {connectedDevice.Name}");
                }

                AddOrUpdateDevice(connectedDevice);
            }

            _cancellationTokenSource = new CancellationTokenSource();
            RaisePropertyChanged(() => StopScanCommand);

            RaisePropertyChanged(() => IsRefreshing);
            Adapter.ScanMode = ScanMode.LowLatency;
            await Adapter.StartScanningForDevicesAsync(_cancellationTokenSource.Token);
        }

        private void CleanupCancellationToken() {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            RaisePropertyChanged(() => StopScanCommand);
        }

        private async void DisconnectDevice(DeviceListItemViewModel device) {
            try {
                if (!device.IsConnected)
                    return;

                _userDialogs.ShowLoading($"Disconnecting {device.Name}...");

                await Adapter.DisconnectDeviceAsync(device.Device);
            } catch (Exception ex) {
                _userDialogs.Alert(ex.Message, "Disconnect error");
            } finally {
                device.Update();
                _userDialogs.HideLoading();
            }
        }

        private void HandleSelectedDevice(DeviceListItemViewModel device) {
            var config = new ActionSheetConfig();

            if (device.IsConnected) {
                config.Add("Update RSSI", async () => {
                    try {
                        _userDialogs.ShowLoading();

                        await device.Device.UpdateRssiAsync();
                        device.RaisePropertyChanged(nameof(device.Rssi));

                        _userDialogs.HideLoading();

                        _userDialogs.ShowSuccess($"RSSI updated {device.Rssi}", 1000);
                    } catch (Exception ex) {
                        _userDialogs.HideLoading();
                        _userDialogs.ShowError($"Failed to update rssi. Exception: {ex.Message}");
                    }
                });

                config.Destructive = new ActionSheetOption("Disconnect", () => DisconnectCommand.Execute(device));
            } else {
                config.Add("Connect", async () => {
                    if (await ConnectDeviceAsync(device)) {
                        var Service = await device.Device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
                        var Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));
                        //Debug.WriteLine("Canupdate to string                       " + Characteristic.CanUpdate.ToString());
                        await Characteristic.StartUpdatesAsync();
                        Characteristic.ValueUpdated += CharacteristicOnValueUpdated;
                        //Debug.WriteLine("valueofChar                       " + Characteristic.Value);
                        Messages.Insert(0, "");
                        Messages.Insert(0, "");
                        Messages.Insert(0, "");
                    }
                });

                config.Add("Connect & Dispose", () => ConnectDisposeCommand.Execute(device));
            }

            config.Add("Copy GUID", () => CopyGuidCommand.Execute(device));
            config.Cancel = new ActionSheetOption("Cancel");
            config.SetTitle("Device Options");
            _userDialogs.ActionSheet(config);
        }

        private void CharacteristicOnValueUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs) {

            var data = characteristicUpdatedEventArgs.Characteristic.Value;
            //Debug.WriteLine("                           " + data[0]);
            //Debug.WriteLine("                           " + data[1]);
            //if ((UInt16)data[0] == 17 && (UInt16)data[1] == 0)
            if (data.Length == 5) {
                Debug.WriteLine("temperature detected                               5 byte data");
                var num = (UInt16)data[3] + (UInt16)data[4] * 0.0625;
                var tempnum = (int)(num * 10);
                var temp = tempnum * .1;
                Messages[2] = $"temp: {temp}";
            } else {
                if (data.Length == 20) {
                    for (int i = 0; i < 5; i++) {
                        red = (UInt16)((data[2 * i + 1]) | data[2 * i] << 8);
                        ir = (UInt16)((data[2 * i + 11]) | data[2 * i + 10] << 8);
                        //Debug.WriteLine("data:                            " + data.Length);
                        //Debug.WriteLine("red:                             " + red);
                        //Debug.WriteLine("ir:                              " + ir);
                        Messages[0] = $"red: {red}";
                        Messages[1] = $"ir: {ir}";
                        if (!(DataRed.Count < 1000)) {
                            Device.BeginInvokeOnMainThread(() => {
                                DataRed.RemoveAt(0);
                            });
                        }
                        BleDataModel redDataReceived = new BleDataModel(DataRed.Count.ToString(), red);
                        Device.BeginInvokeOnMainThread(() => {
                            DataRed.Insert(DataRed.Count, redDataReceived);
                        });
                        //if (DataRed.Count > 1) {
                        //    minRed = DataRed.Sum(redData => redData.Value) / DataRed.Count;
                        //    maxRed = DataRed.Max(redData => redData.Value);
                        //}
                        //for data received as IR
                        if (!(DataIr.Count < 1000)) {
                            Device.BeginInvokeOnMainThread(() => {
                                DataIr.RemoveAt(0);
                            });
                        }
                        BleDataModel irDataReceived = new BleDataModel(DataIr.Count.ToString(), ir);
                        Device.BeginInvokeOnMainThread(() => {
                            DataIr.Insert(DataIr.Count, irDataReceived);
                        });
                    }
                }
                //RaisePropertyChanged(() => CharacteristicValue);
            }
        }

        private async Task<bool> ConnectDeviceAsync(DeviceListItemViewModel device, bool showPrompt = true) {
            if (showPrompt && !await _userDialogs.ConfirmAsync($"Connect to device '{device.Name}'?")) {
                return false;
            }
            try {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                var config = new ProgressDialogConfig() {
                    Title = $"Connecting to '{device.Id}'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                using (var progress = _userDialogs.Progress(config)) {
                    progress.Show();

                    await Adapter.ConnectToDeviceAsync(device.Device, new ConnectParameters(autoConnect: UseAutoConnect, forceBleTransport: false), tokenSource.Token);
                }

                _userDialogs.ShowSuccess($"Connected to {device.Device.Name}.");

                PreviousGuid = device.Device.Id;
                return true;

            } catch (Exception ex) {
                _userDialogs.Alert(ex.Message, "Connection error");
                Mvx.Trace(ex.Message);
                return false;
            } finally {
                _userDialogs.HideLoading();
                device.Update();
            }
        }

        private async void ConnectToPreviousDeviceAsync() {
            IDevice device;
            try {
                CancellationTokenSource tokenSource = new CancellationTokenSource();

                var config = new ProgressDialogConfig() {
                    Title = $"Searching for '{PreviousGuid}'",
                    CancelText = "Cancel",
                    IsDeterministic = false,
                    OnCancel = tokenSource.Cancel
                };

                using (var progress = _userDialogs.Progress(config)) {
                    progress.Show();

                    device = await Adapter.ConnectToKnownDeviceAsync(PreviousGuid, new ConnectParameters(autoConnect: UseAutoConnect, forceBleTransport: false), tokenSource.Token);

                }

                _userDialogs.ShowSuccess($"Connected to {device.Name}.");

                var deviceItem = Devices.FirstOrDefault(d => d.Device.Id == device.Id);
                if (deviceItem == null) {
                    deviceItem = new DeviceListItemViewModel(device);
                    Devices.Add(deviceItem);
                } else {
                    deviceItem.Update(device);
                }
            } catch (Exception ex) {
                _userDialogs.ShowError(ex.Message, 5000);
                return;
            }
        }

        private bool CanConnectToPrevious() {
            return PreviousGuid != default(Guid);
        }

        private async void ConnectAndDisposeDevice(DeviceListItemViewModel item) {
            try {
                using (item.Device) {
                    _userDialogs.ShowLoading($"Connecting to {item.Name} ...");
                    await Adapter.ConnectToDeviceAsync(item.Device);

                    // TODO make this configurable
                    var resultMTU = await item.Device.RequestMtuAsync(60);
                    System.Diagnostics.Debug.WriteLine($"Requested MTU. Result is {resultMTU}");

                    // TODO make this configurable
                    var resultInterval = item.Device.UpdateConnectionInterval(ConnectionInterval.High);
                    System.Diagnostics.Debug.WriteLine($"Set Connection Interval. Result is {resultInterval}");

                    item.Update();
                    _userDialogs.ShowSuccess($"Connected {item.Device.Name}");

                    _userDialogs.HideLoading();
                    for (var i = 5; i >= 1; i--) {
                        _userDialogs.ShowLoading($"Disconnect in {i}s...");

                        await Task.Delay(1000);

                        _userDialogs.HideLoading();
                    }
                }
            } catch (Exception ex) {
                _userDialogs.Alert(ex.Message, "Failed to connect and dispose.");
            } finally {
                _userDialogs.HideLoading();
            }
        }

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();
            _userDialogs.HideLoading();
            _userDialogs.Toast($"Disconnected {e.Device.Name}");
        }

        public MvxCommand<DeviceListItemViewModel> CopyGuidCommand => new MvxCommand<DeviceListItemViewModel>(device => {
            PreviousGuid = device.Id;
        });
    }
}