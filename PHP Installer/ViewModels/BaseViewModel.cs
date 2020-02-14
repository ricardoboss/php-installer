using PHP_Installer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHP_Installer.ViewModels
{
    public abstract class BaseViewModel<T> : BaseModel
        where T : BaseModel
    {
        protected T model;
        public T Model
        {
            get => model;
            set => SetProperty(ref model, value);
        }

        protected BaseViewModel(T model)
        {
            this.model = model;
        }
    }
}
