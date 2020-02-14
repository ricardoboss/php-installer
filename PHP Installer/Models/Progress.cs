namespace PHP_Installer.Models
{
    public class Progress : BaseModel
    {
        private bool isIndeterminate;
        public bool IsIndeterminate
        {
            get => isIndeterminate;
            set => SetProperty(ref isIndeterminate, value);
        }

        private float progressValue;
        public float ProgressValue
        {
            get => progressValue;
            set => SetProperty(ref progressValue, value, nameof(ProgressValue), nameof(Percentage));
        }

        private float progressMax;
        public float ProgressMax
        {
            get => progressMax;
            set => SetProperty(ref progressMax, value, nameof(ProgressMax), nameof(Percentage));
        }

        public float Percentage => ProgressValue / ProgressMax;

        public Progress(float max, float value = 0)
        {
            progressValue = value;
            progressMax = max;
        }
    }
}
