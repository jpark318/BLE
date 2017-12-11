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
using Xamarin.Forms.PlatformConfiguration;

namespace BLE.Client.ViewModels {
    public class GraphViewModel : BaseViewModel {
        private readonly IBluetoothLE _bluetoothLe;
        private readonly IUserDialogs _userDialogs;
        private readonly ISettings _settings;
        public double MaxRed { get; set; } = 60000;
        public double MinRed { get; set; } = 0;
        public double MaxIr { get; set; } = 60000;
        public double MinIr { get; set; } = 0;
        public double MaxEcg { get; set; } = 20000;
        public double MinEcg { get; set; } = 0;
        public double MaxScg { get; set; } = 60000;
        public double MinScg { get; set; } = 0;
        public double PrimalAxisMax { get; set; } = 200;
        private UInt16 red;
        private UInt16 ir;
        private UInt16 ecg;
        private UInt16 scg;
        private int MasterDeviceSamplingRate = 5;
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => _bluetoothLe.IsOn;
        public static Guid SlaveDeviceId { get; set; }
        public static Guid MasterDeviceId { get; set; }
        public ObservableCollection<BleDataModel> DataRed { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> DataIr { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> DataEcg { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> DataScg { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> tempDataRed { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> tempDataIr { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> tempDataEcg { get; set; } = new ObservableCollection<BleDataModel>();
        public ObservableCollection<BleDataModel> tempDataScg { get; set; } = new ObservableCollection<BleDataModel>();
        public String ViewRed { get; set; }
        public String ViewIr { get; set; }
        public String ViewTemp { get; set; }
        public String ViewSpo2 { get; set; }
        public MvxCommand ScanDevices => new MvxCommand(() => ScanDevicesPage());
        //public MvxCommand ExitApplication => new MvxCommand(() => QuitApplication());
        public ObservableCollection<DeviceListItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceListItemViewModel>();
        private Byte[] CharacteristicValue = new Byte[20];
        private int count;
        readonly IPermissions _permissions;

        void NumericalAxis_ActualRangeChanged(object sender, ActualRangeChangedEventArgs e) {
            if (DataEcg.Count > 0) {
                e.VisibleMaximum = Convert.ToDouble(DataEcg.Max(dataList => dataList.Value) * 1.1);
                e.VisibleMinimum = Convert.ToDouble(DataEcg.Min(dataList => dataList.Value) * 0.9);
                
                Debug.WriteLine(MaxEcg);
            }
        }
        

        public GraphViewModel(IBluetoothLE bluetoothLe, IAdapter adapter, IUserDialogs userDialogs, ISettings settings, IPermissions permissions) : base(adapter) {
            _permissions = permissions;
            _bluetoothLe = bluetoothLe;
            _userDialogs = userDialogs;
            _settings = settings;
            Adapter.DeviceConnected += (sender, e) => OnNotification(e.Device);
            Adapter.DeviceDisconnected += OnDeviceDisconnectedFromGraph;
            Adapter.DeviceConnectionLost += OnDeviceConnectionLostFromGraph;
            ViewRed = "RED: 0";
            ViewIr = "IR: 0";
            ViewTemp = "TEMP: 0";
            ViewSpo2 = "SPO2: 0";
        }

        private void OnDeviceDisconnectedFromGraph(object sender, DeviceEventArgs e) {
            Device.BeginInvokeOnMainThread(() => {
                if (e.Device.Id == MasterDeviceId) {
                    DataEcg.Clear();
                    DataScg.Clear();
                }
                if (e.Device.Id == SlaveDeviceId) {
                    DataRed.Clear();
                    DataIr.Clear();
                }
            });
        }

        private void OnDeviceConnectionLostFromGraph(object sender, DeviceEventArgs e) {
            Device.BeginInvokeOnMainThread(() => {
                if (e.Device.Id == MasterDeviceId) {
                    DataEcg.Clear();
                    DataScg.Clear();
                }
                if (e.Device.Id == SlaveDeviceId) {
                    DataRed.Clear();
                    DataIr.Clear();
                }
            });
        }

        private async void OnNotification(IDevice device) {
            var Service = await device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
            var Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));

            Characteristic.ValueUpdated += CharacteristicOnValueUpdated;
        }

        private void CharacteristicOnValueUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs) {
            var data = characteristicUpdatedEventArgs.Characteristic.Value;
            if (MasterDeviceId == characteristicUpdatedEventArgs.Characteristic.Service.Device.Id) {
                //if data is from master device
                if (count == MasterDeviceSamplingRate) {
                    for (int i = 0; i < 5; i++) {
                        ecg = (UInt16)((data[2 * i + 1]) | data[2 * i] << 8);
                        scg = (UInt16)((data[2 * i + 11]) | data[2 * i + 10] << 8);
                        Device.BeginInvokeOnMainThread(() => {
                            if (!(DataEcg.Count < PrimalAxisMax)) {
                                DataEcg.RemoveAt(0);
                            }
                            if (!(DataScg.Count < PrimalAxisMax)) {
                                DataScg.RemoveAt(0);
                            }
                            DataEcg.Insert(DataEcg.Count, new BleDataModel(DataEcg.Count.ToString(), ecg));
                            DataScg.Insert(DataScg.Count, new BleDataModel(DataScg.Count.ToString(), scg));
                            count = 0;
                        });
                    }
                }
                count++;
            }
            if (SlaveDeviceId == characteristicUpdatedEventArgs.Characteristic.Service.Device.Id) {
                if (data.Length == 5) {
                    var num = (UInt16)data[3] + (UInt16)data[4] * 0.0625;
                    var tempnum = (int)(num * 10);
                    var temp = tempnum * .1;
                    ViewTemp = "TEMP: " + temp.ToString();
                } else {
                    if (data.Length == 20) {
                        for (int i = 0; i < 5; i++) {
                            red = (UInt16)((data[2 * i + 1]) | data[2 * i] << 8);
                            ir = (UInt16)((data[2 * i + 11]) | data[2 * i + 10] << 8);
                            ViewRed = "IR: " + red.ToString();
                            ViewIr = "RED: " + ir.ToString();
                            Device.BeginInvokeOnMainThread(() => {
                                if (!(DataRed.Count < PrimalAxisMax)) {
                                    DataRed.RemoveAt(0);
                                }
                                if (!(DataIr.Count < PrimalAxisMax)) {
                                    DataIr.RemoveAt(0);
                                    MaxIr = DataIr.Max(dataList => dataList.Value) * 1.1;
                                    MinIr = DataRed.Min(dataList => dataList.Value) * 0.9;
                                }
                                DataIr.Insert(DataIr.Count, new BleDataModel(DataIr.Count.ToString(), ir));
                                DataRed.Insert(DataRed.Count, new BleDataModel(DataRed.Count.ToString(), red));
                                MaxRed = DataRed.Max(dataList => dataList.Value) * 1.1;
                                MinRed = DataRed.Min(dataList => dataList.Value) * 0.9;
                            });
                        }
                    }
                }
            }
        }

        //show DeviceListPage
        private void ScanDevicesPage() {
            ShowViewModel<DeviceListViewModel>(new MvxBundle(new Dictionary<string, string> { }));
        }
    }

}