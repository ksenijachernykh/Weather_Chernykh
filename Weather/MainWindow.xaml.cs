using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Weather.Classes;
using Weather.Models;

namespace Weather
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DataResponse response;
        public MainWindow()
        {
            InitializeComponent();
            Init();
        }

        private async void Init()
        {
            response = await GetWeather.Get(58.009671f, 56.226184f);
            Create(0);

            foreach(Forecast forecast in response.forecasts)
            {
                Days.Items.Add(forecast.date.ToString("dd.MM.yyyy"));
            }
        }

        public void Create(int idForecast)
        {
            parent.Children.Clear();
            foreach( Hour hour in response.forecasts[idForecast].hours)
            {
                parent.Children.Add(new Elements.Item(hour));
            }
        }

        private void SelectDay(object sender, SelectionChangedEventArgs e) =>
            Create(Days.SelectedIndex);
    }
}