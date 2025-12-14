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
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                await LoadWeatherForCity("Пермь");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке погоды: {ex.Message}");
            }
        }

        private async Task LoadWeatherForCity(string cityName)
        {
            try
            {
                // Получаем координаты города
                var coordinates = await Geocoder.GetCoordinates(cityName);

                // Получаем погоду по координатам
                response = await GetWeather.Get(coordinates.lat, coordinates.lon);
                Create(0);

                // Заполняем список дней
                Days.Items.Clear();
                foreach (Forecast forecast in response.forecasts)
                {
                    Days.Items.Add(forecast.date.ToString("dd.MM.yyyy"));
                }
                Days.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}");
            }
        }

        public void Create(int idForecast)
        {
            if (response == null || response.forecasts == null || idForecast >= response.forecasts.Count)
                return;

            parent.Children.Clear();
            foreach (Hour hour in response.forecasts[idForecast].hours)
            {
                parent.Children.Add(new Elements.Item(hour));
            }
        }

        private void SelectDay(object sender, SelectionChangedEventArgs e)
        {
            if (Days.SelectedIndex >= 0)
                Create(Days.SelectedIndex);
        }

        private async void GetWeatherByCity(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(CityTextBox.Text))
            {
                await LoadWeatherForCity(CityTextBox.Text);
            }
            else
            {
                MessageBox.Show("Введите название города");
            }
        }
    }
}