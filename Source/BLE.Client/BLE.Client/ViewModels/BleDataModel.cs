using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLE.Client.ViewModels {
    public class BleDataModel {
        public string IndexValue { get; set; }
        public double Value { get; set; }

        public BleDataModel(string indexValue, double value) {
            IndexValue = indexValue;
            Value = value;
        }
    }
}