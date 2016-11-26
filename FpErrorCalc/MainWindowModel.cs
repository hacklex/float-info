using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using FpErrorCalc.Annotations;

namespace FpErrorCalc
{
    public class MainWindowModel : INotifyPropertyChanged
    {
        private double _userInput;

        float GetFloat(bool negative, int exponent, int remaining23Bits)
        {
            var bytes = new byte[4];
            bytes[3] = (byte)(negative ? 0x80 : 0x0);
            exponent += 127;
            if (exponent > 255 || exponent < 0)
                throw new ArgumentException(
                    @"Exponent must be less than 129 and bigger than -126. -126 means denormalization!",
                    nameof(exponent));
            bytes[3] |= (byte)(exponent >> 1);
            bytes[2] = (byte) (((exponent & 1) == 0) ? 0 : 128);
            bytes[2] |= ((byte) ((remaining23Bits >> 16) & 0x7f));
            bytes[1] = ((byte)((remaining23Bits >> 8) & 0xff));
            bytes[0] = ((byte) (remaining23Bits & 0xff));
            return BitConverter.ToSingle(bytes, 0);
        }

        public double UserInput
        {
            get { return _userInput; }
            set
            {
                if (value == _userInput) return;
                _userInput = value;
                OnPropertyChanged();
                Recalculate();
            }
        }

        private void Recalculate()
        {
            NearestValue = (float) _userInput;
            var f = float.MinValue;
            if (NearestValue.Equals(float.NegativeInfinity))
            {
                PrevValue = float.NegativeInfinity;
                NearestValue = float.NegativeInfinity;
                NextValue = float.MinValue;
                Refresh();
                return;
            }
            if (NearestValue.Equals(float.PositiveInfinity))
            {
                PrevValue = float.MaxValue;
                NearestValue = float.PositiveInfinity;
                NextValue = float.PositiveInfinity;
                Refresh();
                return;
            }
            if (NearestValue.Equals(float.MinValue))
            {
                PrevValue = float.NegativeInfinity;
                NearestValue = float.MinValue;
                NextValue = BitConverter.ToSingle(new byte[] { 0xfe, 0xff, 0x7f, 0xff }, 0);
                Refresh();
                return;
            }
            if (NearestValue.Equals(float.MaxValue))
            {
                PrevValue = BitConverter.ToSingle(new byte[] { 0xfe, 0xff, 0x7f, 0x7f }, 0);
                NearestValue = float.MaxValue;
                NextValue = float.PositiveInfinity;
                Refresh();
                return;
            }
            
            byte[] bytes = BitConverter.GetBytes(NearestValue);
            bytes[0] = (byte) (bytes[0] ^ 1);
            var closestFloat = BitConverter.ToSingle(bytes, 0);
            if (closestFloat < NearestValue)
            {
                PrevValue = closestFloat;
                var delta = NearestValue - closestFloat;
                NextValue = NearestValue + delta;
                LargestAdditionNeutral = LargestSubtractionNeutral = delta/2;
            }
            else
            {
                NextValue = closestFloat;
                var delta = closestFloat - NearestValue;
                PrevValue = NearestValue - delta;
                LargestAdditionNeutral = LargestSubtractionNeutral = delta/2;
            }
            Refresh();

        }

        private void Refresh()
        {
            OnPropertyChanged(nameof(NearestValue));
            OnPropertyChanged(nameof(NextValue));
            OnPropertyChanged(nameof(PrevValue));
            OnPropertyChanged(nameof(LargestAdditionNeutral));
            OnPropertyChanged(nameof(LargestSubtractionNeutral));
        }

        public float NearestValue { get; private set; }
        public float NextValue { get; private set; }
        public float PrevValue { get; private set; }
        public float LargestAdditionNeutral { get; private set; }
        public float LargestSubtractionNeutral { get; private set; }


        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class FloatValueConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == DependencyProperty.UnsetValue) return DependencyProperty.UnsetValue;
            if (!(value is float)) return DependencyProperty.UnsetValue;
            return ((float) value).ToString("E12");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == DependencyProperty.UnsetValue) return DependencyProperty.UnsetValue;
            if (value == null) return DependencyProperty.UnsetValue;
            double f;
            if (!double.TryParse(value.ToString(), out f)) return DependencyProperty.UnsetValue;
            return (float)f;
        }
    }
}
