using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace PHP_Installer.Converters
{
    [ValueConversion(typeof(object), typeof(FrameworkElement))]
    public class ObjectToFrameworkElementConverter : IValueConverter
    {
        public delegate void SelectionChangedEvent(DownloadEntry entry);

        public static event SelectionChangedEvent SelectionChanged;
        
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // MAYBE: use parameter to indicate type
            switch (value)
            {
                case TextBlock tb:
                    return tb;
                case DownloadEntry entry:
                    return ConvertDownloadEntry(entry);
            }

            return Binding.DoNothing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static FrameworkElement ConvertDownloadEntry(DownloadEntry entry)
        {
            var cellPadding = new Thickness(5,10,5,15);

            var title = new TextBlock
            {
                Text = entry.ToString(),
                Margin = new Thickness(cellPadding.Left, cellPadding.Top, cellPadding.Right, 0)
            };

            Grid.SetRow(title, 0);
            Grid.SetColumn(title, 1);

            var hyperlink = new Hyperlink
            {
                Inlines = { new Run(entry.FullUrl) },
                NavigateUri = new Uri(entry.FullUrl)
            };

            var link = new TextBlock(hyperlink)
            {
                FontSize = 11,
                Margin = new Thickness(cellPadding.Left, 0, cellPadding.Right, cellPadding.Bottom)
            };

            Grid.SetRow(link, 1);
            Grid.SetColumn(link, 1);

            var radioButton = new RadioButton
            {
                GroupName = "download",
                Margin = cellPadding
            };

            var grid = new Grid
            {
                RowDefinitions =
                {
                    new RowDefinition(),
                    new RowDefinition()
                },
                ColumnDefinitions =
                {
                    new ColumnDefinition
                    {
                        Width = GridLength.Auto
                    },
                    new ColumnDefinition()
                },

                Children =
                {
                    radioButton,
                    title,
                    link
                }
            };

            grid.MouseDown += (sender, args) =>
            {
                radioButton.IsChecked = true;

                SelectionChanged?.Invoke(entry);
            };
            grid.MouseEnter += (sender, args) => grid.Background = new SolidColorBrush(Color.FromArgb(36, 0, 0, 0));
            grid.MouseLeave += (sender, args) => grid.Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

            return grid;
        }
    }
}
