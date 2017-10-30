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
        public bool IsRefreshing => Adapter.IsScanning;
        public bool IsStateOn => _bluetoothLe.IsOn;
        public ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();
        public static ObservableCollection<BleDataModel> DataRed { get; set; }
        public static ObservableCollection<BleDataModel> DataIr { get; set; }
        public ObservableCollection<BleDataModel> DataRedChart { get; set; }
        public ObservableCollection<BleDataModel> DataIrChart { get; set; }
        public ObservableCollection<BleDataModel> DataTemp { get; set; }
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

            DataRed = new ObservableCollection<BleDataModel>();
            DataIr = new ObservableCollection<BleDataModel>();
            DataRedChart = new ObservableCollection<BleDataModel>();
            DataIrChart = new ObservableCollection<BleDataModel>();
            DataRed.Insert(0, new BleDataModel("0", 1));
            DataIr.Insert(0, new BleDataModel("0", 1));
            DataRedChart.Insert(0, new BleDataModel("0", 1));
            DataIrChart.Insert(0, new BleDataModel("0", 1));
            Adapter.DeviceConnected += (sender, e) => FillInData(e.Device); ;
        }

        private async void FillInData(IDevice device) {
                var Service = await device.GetServiceAsync(Guid.Parse("0000180d-0000-1000-8000-00805f9b34fb"));
                var Characteristic = await Service.GetCharacteristicAsync(Guid.Parse("00002a37-0000-1000-8000-00805f9b34fb"));
                Characteristic.ValueUpdated += FillData;
        }

        private void FillData(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs) {
            DataRedChart = DataRed;
            DataIrChart = DataIr;
            Debug.WriteLine("DataRedChart /t" + DataRedChart.Last().Value);
            Debug.WriteLine("DataIrChart /t" + DataIrChart.Last().Value);
        }

        private void ScanDevicesPage() {
            Debug.WriteLine("thisisdatared" + DataRed.Count);
            Debug.WriteLine("thisisdatared" + DataRedChart.Count);
            ShowViewModel<DeviceListViewModel>(new MvxBundle(new Dictionary<string, string> { }));
        }
    }

}