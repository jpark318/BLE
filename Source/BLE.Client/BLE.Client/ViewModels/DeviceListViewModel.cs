using System;
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

namespace BLE.Client.ViewModels {
    public class DeviceListViewModel : BaseViewModel {
        private readonly IBluetoothLE _bluetoothLe;
        private readonly IUserDialogs _userDialogs;
        private readonly ISettings _settings;
        readonly IPermissions _permissions;
        private Guid _previousGuid;
        private CancellationTokenSource _cancellationTokenSource;
        private Byte[] CharacteristicValue = new Byte[20];
        bool _useAutoConnect;
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => _bluetoothLe.IsOn;
        public string StateText => GetStateText();
        public List<DeviceListItemViewModel> SystemDevices { get; private set; } = new List<DeviceListItemViewModel>();
        public ObservableCollection<DeviceListItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceListItemViewModel>();
        public MvxCommand RefreshCommand => new MvxCommand(() => TryStartScanning(true));
        public MvxCommand<DeviceListItemViewModel> DisconnectCommand => new MvxCommand<DeviceListItemViewModel>(DisconnectDevice);

        /// <summary>
        /// Attempt to scan BlueTooth devices.
        /// On android devices, check permission for bluetooth scanning.
        /// </summary>
        /// <param name="refresh"></param>
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

        /// <summary>
        /// Scan for devices. Clears list.
        /// 
        /// </summary>
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

        private void AddOrUpdateDevice(IDevice device) {
            InvokeOnMainThread(() => {
                var vm = Devices.FirstOrDefault(d => d.Device.Id == device.Id);
                if (vm != null) {
                    vm.Update();
                } else {
                    if (device.Name != "") {
                        Devices.Add(new DeviceListItemViewModel(device));
                    }
                }
            });
        }

        public MvxCommand StopScanCommand => new MvxCommand(() => {
            _cancellationTokenSource.Cancel();
            CleanupCancellationToken();
            RaisePropertyChanged(() => IsRefreshing);
        }, () => _cancellationTokenSource != null);
        
        /// <summary>
        /// Gets or sets the selected device as master.
        /// </summary>
        /// <value>The selected device as master.</value>
        public DeviceListItemViewModel SelectedDeviceAsMaster {
            get { return null; }
            set {
                if (value != null) {
                    HandleSelectedDevice(value, 2);
                }
                RaisePropertyChanged();
            }
        }

        public DeviceListItemViewModel SelectedDeviceAsSlave {
            get { return null; }
            set {
                if (value != null) {
                    HandleSelectedDevice(value, 1);
                }
                RaisePropertyChanged();
            }
        }

        private void HandleSelectedDevice(DeviceListItemViewModel device, int type) {
            //type = 1 if slave, type = 2 if master.
            var config = new ActionSheetConfig();
            if (device.IsConnected) {
                config.Destructive = new ActionSheetOption("Disconnect", () => DisconnectCommand.Execute(device));
            } else {
                config.Add("Connect", async () => {
                    if (await ConnectDeviceAsync(device)) {
                        switch (type) {
                            case 1:
                                device.IsSlave = true;
                                GraphViewModel.SlaveDeviceId = device.Device.Id;
                                var ServiceSlave = await device.Device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
                                var CharacteristicSlave = await ServiceSlave.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));
                                await CharacteristicSlave.StartUpdatesAsync();
                                break;
                            case 2:
                                device.IsMaster = true;
                                GraphViewModel.MasterDeviceId = device.Device.Id;
                                var ServiceMaster = await device.Device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
                                var CharacteristicMaster = await ServiceMaster.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));
                                await CharacteristicMaster.StartUpdatesAsync();
                                break;
                        }
                    }
                });
            }
            config.Cancel = new ActionSheetOption("Cancel");
            config.SetTitle("Device Options");
            _userDialogs.ActionSheet(config);
        }




        public Guid PreviousGuid {
            get { return _previousGuid; }
            set {
                _previousGuid = value;
                _settings.AddOrUpdateValue("lastguid", _previousGuid.ToString());
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

        public MvxCommand<DeviceListItemViewModel> CopyGuidCommand => new MvxCommand<DeviceListItemViewModel>(device => {
            PreviousGuid = device.Id;
        });

        public DeviceListViewModel(IBluetoothLE bluetoothLe, IAdapter adapter, IUserDialogs userDialogs, ISettings settings, IPermissions permissions) : base(adapter) {
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
            TryStartScanning(true);
            //Adapter.DeviceConnected += (sender, e) => Adapter.DisconnectDeviceAsync(e.Device);
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

        public override void Suspend() {
            base.Suspend();

            Adapter.StopScanningForDevicesAsync();
            RaisePropertyChanged(() => IsRefreshing);
        }

        private void CleanupCancellationToken() {
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
            RaisePropertyChanged(() => StopScanCommand);
        }

        private async void DisconnectDevice(DeviceListItemViewModel device) {
            try {
                device.IsMaster = false;
                device.IsSlave = false;
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

        private void OnDeviceDisconnected(object sender, DeviceEventArgs e) {
            Devices.FirstOrDefault(d => d.Id == e.Device.Id)?.Update();
            _userDialogs.HideLoading();
            _userDialogs.Toast($"Disconnected {e.Device.Name}");
        }
    }
}