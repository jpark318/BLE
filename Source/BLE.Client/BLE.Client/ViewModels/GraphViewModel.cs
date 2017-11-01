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
        private UInt16 red;
        private UInt16 ir;
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => _bluetoothLe.IsOn;
        public static bool updateMaster, updateSlave;
        public ObservableCollection<BleDataModel> DataRed { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> DataIr { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> DataTemp { get; set; }
        public ObservableCollection<string> viewRed { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> viewIr { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> viewTemp { get; set; } = new ObservableCollection<string>();
        public ObservableCollection<string> viewSpo2 { get; set; } = new ObservableCollection<string>();
        public MvxCommand ScanDevices => new MvxCommand(() => ScanDevicesPage());
        public ObservableCollection<DeviceListItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceListItemViewModel>();
        private Guid _previousGuid;
        private CancellationTokenSource _cancellationTokenSource;
        private Byte[] CharacteristicValue = new Byte[20];
        bool _useAutoConnect;
        readonly IPermissions _permissions;

        public GraphViewModel(IBluetoothLE bluetoothLe, IAdapter adapter, IUserDialogs userDialogs, ISettings settings, IPermissions permissions) : base(adapter) {
            _permissions = permissions;
            _bluetoothLe = bluetoothLe;
            _userDialogs = userDialogs;
            _settings = settings;
            Adapter.DeviceConnected += (sender, e) => FillInData(e.Device);
            viewRed.Insert(0, "RED: 0");
            viewIr.Insert(0, "IR: 0" );
            viewTemp.Insert(0, "TEMP: 0");
            viewSpo2.Insert(0, "SPO2: 0");
        }

        private async void FillInData(IDevice device) {
                var Service = await device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
                var Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));
                Characteristic.ValueUpdated += CharacteristicOnSlaveValueUpdated;
        }

        private void CharacteristicOnSlaveValueUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs) {
            var data = characteristicUpdatedEventArgs.Characteristic.Value;
            if (data.Length == 5) {
                Debug.WriteLine("jpark318temperature detected                               5 byte data");
                var num = (UInt16)data[3] + (UInt16)data[4] * 0.0625;
                var tempnum = (int)(num * 10);
                var temp = tempnum * .1;
                viewTemp[0] = "TEMP: " + temp.ToString();
            } else {
                if (data.Length == 20) {
                    for (int i = 0; i < 5; i++) {
                        red = (UInt16)((data[2 * i + 1]) | data[2 * i] << 8);
                        ir = (UInt16)((data[2 * i + 11]) | data[2 * i + 10] << 8);
                        viewRed[0] = "IR: " + red.ToString();
                        viewIr[0] = "RED: " + ir.ToString();
                        if (!(DataRed.Count < 1000)) {
                            Device.BeginInvokeOnMainThread(() => {
                                DataRed.RemoveAt(0);
                            });
                        }
                        BleDataModel redDataReceived = new BleDataModel(DataRed.Count.ToString(), red);
                        Device.BeginInvokeOnMainThread(() => {
                            DataRed.Insert(DataRed.Count, redDataReceived);
                        });
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

        private void CharacteristicOnMasterValueUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs) {
            var data = characteristicUpdatedEventArgs.Characteristic.Value;
        }

        private void ScanDevicesPage() {
            ShowViewModel<DeviceListViewModel>(new MvxBundle(new Dictionary<string, string> { }));
        }
    }

}