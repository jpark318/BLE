﻿using System;
using MvvmCross.Core.ViewModels;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;

namespace BLE.Client.ViewModels
{
    public class DeviceListItemViewModel : MvxNotifyPropertyChanged
    {
        public IDevice Device { get; private set; }

        public Guid Id => Device.Id;
        public bool IsConnected => Device.State == DeviceState.Connected;
        public bool IsSlave { get; set; } = false;
        public bool IsMaster { get; set; } = false;
        public int Rssi => Device.Rssi;
        public string Name => Device.Name;
        public DeviceListItemViewModel(IDevice device)
        {
            Device = device;
            if (GraphViewModel.MasterDeviceId == Id) {
                IsMaster = true;
            }
            if (GraphViewModel.SlaveDeviceId == Id) {
                IsSlave = true;
            }
        }

        public void Update(IDevice newDevice = null)
        {
            if (newDevice != null)
            {
                Device = newDevice;
            }
            RaisePropertyChanged(nameof(IsConnected));
            RaisePropertyChanged(nameof(Rssi));
            RaisePropertyChanged(nameof(IsSlave));
            RaisePropertyChanged(nameof(IsMaster));
        }
    }
}
