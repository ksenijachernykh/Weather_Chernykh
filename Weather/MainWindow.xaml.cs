using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Weather.Classes;
using Weather.Models;

namespace Weather
{
    public partial class MainWindow : Window
    {
        private DataResponse response;
        private string userId = "user_" + Guid.NewGuid().ToString().Substring(0, 8);

        public MainWindow()
        {
            InitializeComponent();
            Loaded += Window_Loaded;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateStats();
            await LoadWeatherForCity("Пермь");
        }

        private async Task LoadWeatherForCity(string city)
        {
            try
            {
                // Показываем состояние загрузки
                ShowLoadingState($"Загрузка данных для города '{city}'...");

                Days.Items.Clear();
                StatusText.Text = $"Получаем погоду для {city}...";

                string cityName = string.IsNullOrEmpty(city) ? "Пермь" : city;
                response = await GetWeather.GetWeatherData(cityName, userId);

                if (response?.forecasts != null && response.forecasts.Count > 0)
                {
                    // Заполняем список дней
                    foreach (Forecast forecast in response.forecasts)
                    {
                        Days.Items.Add(forecast.date.ToString("dd.MM.yyyy"));
                    }

                    // Выбираем сегодняшний день
                    if (Days.Items.Count > 0)
                    {
                        Days.SelectedIndex = 0;
                    }

                    StatusText.Text = $"Данные для {city} получены успешно";

                    // Обновляем статистику
                    await UpdateStats();
                }
                else
                {
                    ShowErrorMessage("Не удалось получить данные о погоде");
                    StatusText.Text = "Ошибка получения данных";
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage($"Ошибка: {ex.Message}");
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void ShowLoadingState(string message)
        {
            parent.Children.Clear();
            parent.Children.Add(new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new ProgressBar
                        {
                            IsIndeterminate = true,
                            Width = 200,
                            Height = 10,
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new TextBlock
                        {
                            Text = message,
                            FontSize = 14,
                            Foreground = Brushes.Gray,
                            TextAlignment = TextAlignment.Center
                        }
                    }
                }
            });
        }

        private void ShowErrorMessage(string message)
        {
            parent.Children.Clear();
            parent.Children.Add(new Border
            {
                Padding = new Thickness(20),
                Child = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "⚠️",
                            FontSize = 40,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 10)
                        },
                        new TextBlock
                        {
                            Text = message,
                            FontSize = 14,
                            Foreground = Brushes.Red,
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            MaxWidth = 400
                        }
                    }
                }
            });
        }

        public void Create(int idForecast)
        {
            parent.Children.Clear();

            if (response?.forecasts != null && idForecast < response.forecasts.Count)
            {
                var forecast = response.forecasts[idForecast];

                if (forecast.hours != null)
                {
                    bool alternate = false;
                    foreach (Hour hour in forecast.hours)
                    {
                        var item = new Elements.Item(hour);

                        // Добавляем чередующийся фон для строк
                        if (alternate)
                        {
                            var border = new Border
                            {
                                Background = new SolidColorBrush(Color.FromArgb(10, 76, 175, 80)),
                                Child = item
                            };
                            parent.Children.Add(border);
                        }
                        else
                        {
                            parent.Children.Add(item);
                        }

                        alternate = !alternate;
                    }
                }
            }
        }

        private async void UpdateWeather(object sender, RoutedEventArgs e)
        {
            string city = CityTextBox.Text?.Trim();

            if (!string.IsNullOrEmpty(city))
            {
                await LoadWeatherForCity(city);
            }
            else
            {
                MessageBox.Show("Введите название города",
                    "Предупреждение",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void SelectDay(object sender, SelectionChangedEventArgs e)
        {
            if (Days.SelectedIndex >= 0)
                Create(Days.SelectedIndex);
        }

        private async Task UpdateStats()
        {
            try
            {
                var stats = await WeatherCache.GetStats(userId);

                Dispatcher.Invoke(() =>
                {
                    // Обновляем счетчики
                    RequestCounter.Text = $"Запросов: {stats.todayCount}/{WeatherCache.DAILY_LIMIT}";

                    // Цветовая индикация лимита
                    if (stats.todayCount >= WeatherCache.DAILY_LIMIT)
                    {
                        RequestCounter.Background = new SolidColorBrush(Color.FromRgb(244, 67, 54));
                    }
                    else if (stats.todayCount >= WeatherCache.DAILY_LIMIT * 0.8)
                    {
                        RequestCounter.Background = new SolidColorBrush(Color.FromRgb(255, 152, 0));
                    }
                    else
                    {
                        RequestCounter.Background = new SolidColorBrush(Color.FromRgb(33, 150, 243));
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обновлении статистики: {ex.Message}");
            }
        }
    }
}