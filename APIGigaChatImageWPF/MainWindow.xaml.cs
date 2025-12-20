using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using APIGigaChatImageWPF.Services;
using APIGigaChatImageWPF.Classes;

namespace APIGigaChatImageWPF
{
    public partial class MainWindow : Window
    {
        private ApiService _apiService;
        private CalendarService _calendarService;
        private WallpaperSetter _wallpaperSetter;

        private byte[] _lastImageData;
        private string _lastImagePath;

        public MainWindow()
        {
            InitializeComponent();
            InitializeServices();
            InitializeCalendarMode();
            UpdatePreviewPlaceholder();
        }

        private void InitializeServices()
        {
            _apiService = new ApiService();
            _calendarService = new CalendarService();
            _wallpaperSetter = new WallpaperSetter();
        }

        private void InitializeCalendarMode()
        {
            HolidayComboBox.Items.Clear();
            var holidays = _calendarService.GetAllHolidays();

            foreach (var holiday in holidays)
            {
                HolidayComboBox.Items.Add(holiday);
            }

            if (HolidayComboBox.Items.Count > 0)
                HolidayComboBox.SelectedIndex = 0;
        }

        private void ModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Проверка на null перед использованием
            if (CalendarModeRadio == null || ManualModeRadio == null)
                return;

            bool isCalendarMode = CalendarModeRadio.IsChecked == true;

            // Показываем/скрываем панели
            if (CalendarModePanel != null)
                CalendarModePanel.Visibility = isCalendarMode ? Visibility.Visible : Visibility.Collapsed;

            if (ManualModePanel != null)
                ManualModePanel.Visibility = isCalendarMode ? Visibility.Collapsed : Visibility.Visible;

            if (isCalendarMode && HolidayComboBox != null)
            {
                UpdateHolidayInfo();
            }
        }

        private void HolidayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверка на null
            if (CalendarModeRadio == null || CalendarModeRadio.IsChecked != true || HolidayComboBox.SelectedItem == null)
                return;

            UpdateHolidayInfo();
        }

        private void UpdateHolidayInfo()
        {
            if (HolidayComboBox.SelectedItem is Holiday holiday)
            {
                if (HolidayNameText != null)
                    HolidayNameText.Text = holiday.Name;

                if (HolidayDescriptionText != null)
                    HolidayDescriptionText.Text = holiday.Description;

                if (HolidayDateText != null)
                    HolidayDateText.Text = $"Дата праздника: {holiday.Date:dd.MM.yyyy}";

                if (HolidayInfoPanel != null)
                    HolidayInfoPanel.Visibility = Visibility.Visible;

                // Генерируем промпт
                string prompt = _calendarService.GeneratePromptForHoliday(holiday);

                if (GeneratedPromptText != null)
                    GeneratedPromptText.Text = prompt;

                if (GeneratedPromptPanel != null)
                    GeneratedPromptPanel.Visibility = Visibility.Visible;
            }
        }

        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                StatusTextBlock.Text = "⏳ Генерация изображения...";
                GenerateButton.IsEnabled = false;
                DownloadButton.IsEnabled = false;
                SetWallpaperButton.IsEnabled = false;

                string prompt;

                // Выбираем промпт в зависимости от режима
                if (CalendarModeRadio != null && CalendarModeRadio.IsChecked == true)
                {
                    if (HolidayComboBox.SelectedItem is Holiday holiday)
                    {
                        prompt = _calendarService.GeneratePromptForHoliday(holiday);
                    }
                    else
                    {
                        StatusTextBlock.Text = "❌ Выберите праздник";
                        GenerateButton.IsEnabled = true;
                        return;
                    }
                }
                else
                {
                    prompt = PromptTextBox.Text;
                    if (string.IsNullOrWhiteSpace(prompt))
                    {
                        StatusTextBlock.Text = "❌ Введите описание обоев";
                        GenerateButton.IsEnabled = true;
                        return;
                    }
                }

                // Получаем параметры
                string style = ((ComboBoxItem)StyleComboBox.SelectedItem).Content.ToString();
                string colorPalette = ((ComboBoxItem)ColorComboBox.SelectedItem).Content.ToString();
                string aspectRatio = ((ComboBoxItem)AspectRatioComboBox.SelectedItem).Content.ToString();

                // Генерируем изображение
                var response = await _apiService.GenerateImageAsync(prompt, style, colorPalette, aspectRatio);

                if (response?.data?.Count > 0)
                {
                    // Скачиваем изображение
                    var imageUrl = response.data[0].url;
                    _lastImageData = await _apiService.DownloadImageAsync(imageUrl);

                    // Сохраняем временно
                    _lastImagePath = Path.Combine(Path.GetTempPath(),
                        $"wallpaper_{DateTime.Now:yyyyMMddHHmmss}.jpg");

                    // Используем синхронную версию для .NET Framework
                    File.WriteAllBytes(_lastImagePath, _lastImageData);

                    // Показываем предпросмотр
                    await ShowPreviewImage(_lastImagePath);

                    StatusTextBlock.Text = "✅ Изображение сгенерировано успешно!";
                    DownloadButton.IsEnabled = true;
                    SetWallpaperButton.IsEnabled = true;
                }
                else
                {
                    StatusTextBlock.Text = "❌ Ошибка: изображение не сгенерировано";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Ошибка: {ex.Message}";
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
        }

        private async Task ShowPreviewImage(string imagePath)
        {
            await Task.Run(() =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        if (File.Exists(imagePath))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(imagePath);
                            bitmap.EndInit();

                            PreviewImage.Source = bitmap;

                            if (PreviewPlaceholder != null)
                                PreviewPlaceholder.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            StatusTextBlock.Text = "❌ Файл изображения не найден";
                        }
                    }
                    catch (Exception ex)
                    {
                        StatusTextBlock.Text = $"❌ Ошибка загрузки изображения: {ex.Message}";
                    }
                });
            });
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_lastImageData != null && !string.IsNullOrEmpty(_lastImagePath))
                {
                    var saveDialog = new SaveFileDialog
                    {
                        FileName = $"wallpaper_{DateTime.Now:yyyyMMddHHmmss}.jpg",
                        Filter = "JPEG Image|*.jpg|PNG Image|*.png|All Files|*.*",
                        DefaultExt = ".jpg"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        // Используем синхронную версию
                        File.WriteAllBytes(saveDialog.FileName, _lastImageData);
                        StatusTextBlock.Text = $"✅ Изображение сохранено: {Path.GetFileName(saveDialog.FileName)}";
                    }
                }
                else
                {
                    StatusTextBlock.Text = "❌ Нет изображения для сохранения";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Ошибка сохранения: {ex.Message}";
            }
        }

        private void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
                {
                    _wallpaperSetter.Set(_lastImagePath);
                    StatusTextBlock.Text = "✅ Обои успешно установлены!";
                }
                else
                {
                    StatusTextBlock.Text = "❌ Нет изображения для установки";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Ошибка установки обоев: {ex.Message}";
            }
        }

        private void UpdatePreviewPlaceholder()
        {
            if (PreviewImage != null && PreviewPlaceholder != null)
            {
                if (PreviewImage.Source == null)
                {
                    PreviewPlaceholder.Visibility = Visibility.Visible;
                }
                else
                {
                    PreviewPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}