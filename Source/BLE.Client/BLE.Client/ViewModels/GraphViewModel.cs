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
using BLE.Client;
using BLE.Client.Interfaces;

namespace BLE.Client.ViewModels
{
    public class GraphViewModel : BaseViewModel
    {
        private readonly IBluetoothLE _bluetoothLe;
        private readonly IUserDialogs _userDialogs;
        private readonly ISettings _settings;
        public double PrimalAxisMax { get; set; } = 500;
        private int MasterDeviceSamplingRate = 1;
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => _bluetoothLe.IsOn;
        public static Guid SlaveDeviceId { get; set; }
        public static Guid MasterDeviceId { get; set; }
        UInt16 ecg;
        UInt16 scg;
        UInt16 red;
        UInt16 ir;
        int count;
        /// <summary>
        /// Red(0), Ir(1), Ecg(2), Scg(3)
        /// </summary>
        public ObservableCollection<BleDataModel>[] DataCollections { get; set; }
        = { new ObservableCollection<BleDataModel>(), new ObservableCollection<BleDataModel>(), new ObservableCollection<BleDataModel>(), new ObservableCollection<BleDataModel>() };
        //public ObservableCollection<BleDataModel> DataRed { get; set; } = new ObservableCollection<BleDataModel>();
        //public ObservableCollection<BleDataModel> DataIr { get; set; } = new ObservableCollection<BleDataModel>();
        //public ObservableCollection<BleDataModel> DataEcg { get; set; } = new ObservableCollection<BleDataModel>();
        //public ObservableCollection<BleDataModel> DataScg { get; set; } = new ObservableCollection<BleDataModel>();
        public String ViewRed { get; set; }
        public String ViewIr { get; set; }
        public String ViewTemp { get; set; }
        public String ViewSpo2 { get; set; }
        public MvxCommand ScanDevices => new MvxCommand(() => ScanDevicesPage());
        public MvxCommand BeginRecognition => new MvxCommand(() => BeginSpeechRecognition());
        //public MvxCommand ExitApplication => new MvxCommand(() => QuitApplication());
        public ObservableCollection<DeviceListItemViewModel> Devices { get; set; } = new ObservableCollection<DeviceListItemViewModel>();
        private Byte[] CharacteristicValue = new Byte[20];
        readonly IPermissions _permissions;

        void NumericalAxis_ActualRangeChanged(object sender, ActualRangeChangedEventArgs e)
        {
            if (DataCollections[3].Count > 0)
            {
                e.VisibleMaximum = Convert.ToDouble(DataCollections[3].Max(dataList => dataList.Value) * 1.1);
                e.VisibleMinimum = Convert.ToDouble(DataCollections[3].Min(dataList => dataList.Value) * 0.9);
            }
        }


        public GraphViewModel(IBluetoothLE bluetoothLe, IAdapter adapter, IUserDialogs userDialogs, ISettings settings, IPermissions permissions) : base(adapter)
        {
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

        private void OnDeviceDisconnectedFromGraph(object sender, DeviceEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (e.Device.Id == MasterDeviceId)
                {
                    DataCollections[2].Clear();
                    DataCollections[3].Clear();
                }
                if (e.Device.Id == SlaveDeviceId)
                {
                    DataCollections[0].Clear();
                    DataCollections[1].Clear();
                }
            });
        }

        private void OnDeviceConnectionLostFromGraph(object sender, DeviceEventArgs e)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                if (e.Device.Id == MasterDeviceId)
                {
                    DataCollections[2].Clear();
                    DataCollections[3].Clear();
                }
                if (e.Device.Id == SlaveDeviceId)
                {
                    DataCollections[0].Clear();
                    DataCollections[1].Clear();
                }
            });
        }

        private async void OnNotification(IDevice device)
        {
            try
            {
                var Service = await device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
                var Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));
                Characteristic.ValueUpdated += CharacteristicOnValueUpdated;
            }
            catch (Exception e)
            {
                Trace.Message("Failed to retreive system connected devices. {0}", e.Message);
            }

        }

        private void CharacteristicOnValueUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs)
        {
            var data = characteristicUpdatedEventArgs.Characteristic.Value;
            Device.BeginInvokeOnMainThread(() =>
            {
                if (MasterDeviceId == characteristicUpdatedEventArgs.Characteristic.Service.Device.Id)
                {
                    //TODO: logging
                    //if data is from master device
                    if (count == MasterDeviceSamplingRate)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            ecg = (UInt16)((data[2 * i + 1]) | data[2 * i] << 8);
                            //ViewRed = "ecg: " + ecg.ToString();
                            scg = (UInt16)((data[2 * i + 11]) | data[2 * i + 10] << 8);
                            //Debug.WriteLine("asdfasdf" + ecg.ToString());
                            bool checkRedundancy = false;

                            if (!checkRedundancy)
                            {
                                if (!(DataCollections[2].Count < PrimalAxisMax))
                                {
                                    DataCollections[2].RemoveAt(0);
                                }
                                if (!(DataCollections[3].Count < PrimalAxisMax))
                                {
                                    DataCollections[3].RemoveAt(0);
                                }
                                //Debug.WriteLine("::::::::" + ecg.ToString());
                                DataCollections[2].Insert(DataCollections[2].Count, new BleDataModel(DataCollections[2].Count.ToString(), ecg));
                                DataCollections[3].Insert(DataCollections[3].Count, new BleDataModel(DataCollections[3].Count.ToString(), scg));
                                //count = 0;
                                checkRedundancy = true;
                            }
                            //});
                        }
                    }
                    //TODO: Signal Processing
                    count++;
                }
                if (SlaveDeviceId == characteristicUpdatedEventArgs.Characteristic.Service.Device.Id)
                {
                    //TODO: logging
                    if (data.Length == 5)
                    {
                        var num = (UInt16)data[3] + (UInt16)data[4] * 0.0625;
                        var tempnum = (int)(num * 10);
                        var temp = tempnum * .1;
                        ViewTemp = "TEMP: " + temp.ToString();
                    }
                    else
                    {
                        if (data.Length == 20)
                        {
                            for (int i = 0; i < 5; i++)
                            {
                                red = (UInt16)((data[2 * i + 1]) | data[2 * i] << 8);
                                ir = (UInt16)((data[2 * i + 11]) | data[2 * i + 10] << 8);
                                ViewRed = "IR: " + red.ToString();
                                ViewIr = "RED: " + ir.ToString();
                                if (!(DataCollections[0].Count < PrimalAxisMax))
                                {
                                    DataCollections[0].RemoveAt(0);
                                }
                                if (!(DataCollections[1].Count < PrimalAxisMax))
                                {
                                    DataCollections[1].RemoveAt(0);
                                }
                                DataCollections[1].Insert(DataCollections[1].Count, new BleDataModel(DataCollections[1].Count.ToString(), ir));
                                DataCollections[0].Insert(DataCollections[0].Count, new BleDataModel(DataCollections[0].Count.ToString(), red));
                            }
                        }
                    }
                    //TODO: Signal Processing

                }
            });
        }

        //show DeviceListPage
        private void ScanDevicesPage()
        {
            ShowViewModel<DeviceListViewModel>(new MvxBundle(new Dictionary<string, string> { }));
        }

        private void BeginSpeechRecognition()
        {
            DependencyService.Get<Voice>().StartRecord();
        }
    }

}