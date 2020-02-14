using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PHP_Installer.Models
{
    public abstract class BaseModel : INotifyPropertyChanging, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected void OnPropertyChanging([CallerMemberName] string propertyName = null)
        {
            PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null, params string[] additionalProperties)
        {
            if (EqualityComparer<T>.Default.Equals(property, value))
                return false;

            OnPropertyChanging(propertyName);
            foreach (var additionalPropertyName in additionalProperties)
                OnPropertyChanging(additionalPropertyName);

            property = value;

            OnPropertyChanged(propertyName);
            foreach (var additionalPropertyName in additionalProperties)
                OnPropertyChanging(additionalPropertyName);

            return true;
        }
    }
}
