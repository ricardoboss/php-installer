using PHP_Installer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PHP_Installer.ViewModels
{
    public class LoadingViewModel : BaseViewModel<Progress>
    {
        public LoadingViewModel(float max) :
            base(new Progress(max))
        {
        }
    }
}
