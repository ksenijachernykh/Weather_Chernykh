// Файл: Classes/WeatherCache.cs
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using System;
using System.Data;
using System.Threading.Tasks;
using Weather.Models;

namespace Weather.Classes
{
    public class WeatherCache
    {
        private static string connectionString = "Server=localhost;Port=3306;Database=weather_cache;Uid=root;Pwd=;CharSet=utf8mb4;Allow User Variables=True";

        public static readonly int DAILY_LIMIT = 50;
        private static readonly int CACHE_EXPIRE_MINUTES = 60;

        static WeatherCache()
        {
            Task.Run(() => CleanupOldCache());
        }

        /// <summary>
        /// Получить данные о погоде с использованием кэша
        /// </summary>
        public static async Task<DataResponse> GetWeatherData(string cityName, string userId)
        {
            try
            {
                bool canRequest = await CheckAndIncrementLimit(userId);

                if (!canRequest)
                {
                    Console.WriteLine($"Лимит запросов исчерпан, ищем актуальные данные в кэше...");

                    var cachedData = await GetCachedWeather(cityName);
                    if (cachedData != null)
                    {
                        return cachedData;
                    }
                    var fallbackData = await GetLastCachedData(cityName);
                    if (fallbackData != null)
                    {
                        Console.WriteLine($"⚠ Лимит исчерпан! Используем устаревшие данные из кэша");
                        return fallbackData;
                    }

                    throw new Exception($"Дневной лимит запросов ({DAILY_LIMIT}) исчерпан. Попробуйте завтра.");
                }

                var cachedWeather = await GetCachedWeather(cityName);
                if (cachedWeather != null)
                {
                    return cachedWeather;
                }

                var coordinates = await Geocoder.GetCoordinates(cityName);
                var lat = coordinates.lat;
                var lon = coordinates.lon;

                var weatherData = await GetWeather.Get(lat, lon);

                await SaveToCache(cityName, lat, lon, weatherData);

                return weatherData;
            }
            catch (Exception ex)
            {
                var fallbackData = await GetLastCachedData(cityName);
                if (fallbackData != null)
                {
                    Console.WriteLine($"⚠ Ошибка API! Используем устаревшие данные из кэша: {ex.Message}");
                    return fallbackData;
                }
                throw new Exception($"Не удалось получить данные о погете: {ex.Message}");
            }
        }

        /// <summary>
        /// Получить данные из кэша
        /// </summary>
        public static async Task<DataResponse> GetCachedWeather(string cityName)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT response_data, request_count, 
                               TIMESTAMPDIFF(MINUTE, last_requested, NOW()) as minutes_passed,
                               created_at, expires_at
                        FROM weather_cache 
                        WHERE city_name = @city 
                        AND expires_at > NOW() 
                        ORDER BY created_at DESC 
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@city", cityName);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string json = reader.GetString("response_data");
                                int requestCount = reader.GetInt32("request_count");
                                int minutesPassed = reader.GetInt32("minutes_passed");
                                DateTime created = reader.GetDateTime("created_at");
                                DateTime expires = reader.GetDateTime("expires_at");

                                Console.WriteLine($"✓ Данные из кэша. Город: {cityName}");
                                Console.WriteLine($"  Создан: {created:dd.MM HH:mm}, Истекает: {expires:dd.MM HH:mm}");
                                Console.WriteLine($"  Запросов: {requestCount}, Прошло минут: {minutesPassed}");

                                await reader.CloseAsync();


                                string updateQuery = @"
                                    UPDATE weather_cache 
                                    SET request_count = request_count + 1, 
                                        last_requested = NOW() 
                                    WHERE city_name = @city 
                                    AND expires_at > NOW()";

                                using (var updateCmd = new MySqlCommand(updateQuery, connection))
                                {
                                    updateCmd.Parameters.AddWithValue("@city", cityName);
                                    await updateCmd.ExecuteNonQueryAsync();
                                }

                                return JsonConvert.DeserializeObject<DataResponse>(json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при чтении из кэша: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Получить последние доступные данные (даже устаревшие)
        /// </summary>
        private static async Task<DataResponse> GetLastCachedData(string cityName)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT response_data, 
                               TIMESTAMPDIFF(MINUTE, expires_at, NOW()) as expired_minutes
                        FROM weather_cache 
                        WHERE city_name = @city 
                        ORDER BY created_at DESC 
                        LIMIT 1";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@city", cityName);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                string json = reader.GetString("response_data");
                                int expiredMinutes = reader.GetInt32("expired_minutes");

                                Console.WriteLine($"⚠ Используем устаревшие данные. Истекло: {expiredMinutes} мин назад");

                                return JsonConvert.DeserializeObject<DataResponse>(json);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении устаревших данных: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Сохранить данные в кэш
        /// </summary>
        public static async Task SaveToCache(string cityName, float lat, float lon, DataResponse data)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();


                    string deleteQuery = "DELETE FROM weather_cache WHERE city_name = @city";
                    using (var deleteCmd = new MySqlCommand(deleteQuery, connection))
                    {
                        deleteCmd.Parameters.AddWithValue("@city", cityName);
                        await deleteCmd.ExecuteNonQueryAsync();
                    }

      
                    string insertQuery = @"
                        INSERT INTO weather_cache 
                        (city_name, latitude, longitude, response_data, created_at, expires_at, request_count, last_requested) 
                        VALUES (@city, @lat, @lon, @data, NOW(), DATE_ADD(NOW(), INTERVAL @minutes MINUTE), 1, NOW())";

                    using (var cmd = new MySqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@city", cityName);
                        cmd.Parameters.AddWithValue("@lat", lat);
                        cmd.Parameters.AddWithValue("@lon", lon);
                        cmd.Parameters.AddWithValue("@data", JsonConvert.SerializeObject(data));
                        cmd.Parameters.AddWithValue("@minutes", CACHE_EXPIRE_MINUTES);

                        await cmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine($"✓ Данные сохранены в кэш на {CACHE_EXPIRE_MINUTES} минут");
                    Console.WriteLine($"  Город: {cityName}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при сохранении в кэш: {ex.Message}");
            }
        }

        /// <summary>
        /// Проверить и увеличить счётчик запросов
        /// </summary>
        public static async Task<bool> CheckAndIncrementLimit(string userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    string checkQuery = "SELECT request_count FROM request_limits WHERE user_id = @userId AND request_date = @date";
                    int currentCount = 0;

                    using (var checkCmd = new MySqlCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.AddWithValue("@userId", userId);
                        checkCmd.Parameters.AddWithValue("@date", today);

                        var result = await checkCmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            currentCount = Convert.ToInt32(result);
                        }
                    }

    
                    if (currentCount >= DAILY_LIMIT)
                    {
                        Console.WriteLine($"✗ Лимит исчерпан! Сегодня уже {currentCount}/{DAILY_LIMIT} запросов");
                        return false;
                    }

          
                    string updateQuery = @"
                        INSERT INTO request_limits (user_id, request_date, request_count, last_request) 
                        VALUES (@userId, @date, 1, NOW()) 
                        ON DUPLICATE KEY UPDATE 
                        request_count = request_count + 1, 
                        last_request = NOW()";

                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        updateCmd.Parameters.AddWithValue("@userId", userId);
                        updateCmd.Parameters.AddWithValue("@date", today);
                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine($"✓ Запрос к API. Лимит: {currentCount + 1}/{DAILY_LIMIT}");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке лимита: {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Получить статистику
        /// </summary>
        public static async Task<(int todayCount, int remaining, int cachedCities, string cacheInfo)> GetStats(string userId)
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    string today = DateTime.Now.ToString("yyyy-MM-dd");
                    int todayCount = 0;
                    int cachedCities = 0;
                    string cacheInfo = "";

 
                    string limitQuery = "SELECT request_count FROM request_limits WHERE user_id = @userId AND request_date = @date";
                    using (var limitCmd = new MySqlCommand(limitQuery, connection))
                    {
                        limitCmd.Parameters.AddWithValue("@userId", userId);
                        limitCmd.Parameters.AddWithValue("@date", today);

                        var result = await limitCmd.ExecuteScalarAsync();
                        if (result != null && result != DBNull.Value)
                        {
                            todayCount = Convert.ToInt32(result);
                        }
                    }

     
                    string cacheQuery = @"
                        SELECT 
                            COUNT(DISTINCT city_name) as city_count,
                            GROUP_CONCAT(
                                CONCAT(
                                    city_name, 
                                    ' (',
                                    TIMESTAMPDIFF(MINUTE, NOW(), expires_at),
                                    'м)'
                                ) 
                                ORDER BY expires_at DESC 
                                SEPARATOR ', '
                            ) as cities
                        FROM weather_cache 
                        WHERE expires_at > NOW()";

                    using (var cacheCmd = new MySqlCommand(cacheQuery, connection))
                    {
                        using (var reader = await cacheCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                cachedCities = reader.GetInt32("city_count");
                                if (!reader.IsDBNull(reader.GetOrdinal("cities")))
                                {
                                    cacheInfo = reader.GetString("cities");
                                }
                            }
                        }
                    }

                    int remaining = Math.Max(0, DAILY_LIMIT - todayCount);
                    return (todayCount, remaining, cachedCities, cacheInfo);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении статистики: {ex.Message}");
                return (0, DAILY_LIMIT, 0, "");
            }
        }

        /// <summary>
        /// Очистить старый кэш
        /// </summary>
        public static async Task CleanupOldCache()
        {
            try
            {
                using (var connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    string cleanupQuery = "DELETE FROM weather_cache WHERE expires_at < DATE_SUB(NOW(), INTERVAL 1 DAY)";
                    using (var cmd = new MySqlCommand(cleanupQuery, connection))
                    {
                        int deleted = await cmd.ExecuteNonQueryAsync();
                        if (deleted > 0)
                        {
                            Console.WriteLine($"Очищено {deleted} устаревших записей кэша");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при очистке кэша: {ex.Message}");
            }
        }
    }
}