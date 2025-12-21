using APIGigaChatImageWPF.Classes;
using APIGigaChatImageWPF.Services;
using Microsoft.Win32;
using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace APIGigaChatImageWPF
{
    public partial class MainWindow : Window
    {
        private ApiService _apiService;
        private CalendarService _calendarService;
        private WallpaperSetter _wallpaperSetter;

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
            if (CalendarModeRadio == null || ManualModeRadio == null)
                return;

            bool isCalendarMode = CalendarModeRadio.IsChecked == true;

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
                StatusTextBlock.Text = "⏳ Начинаю генерацию изображения...";
                GenerateButton.IsEnabled = false;
                DownloadButton.IsEnabled = false;
                SetWallpaperButton.IsEnabled = false;

                string prompt;

                // Выбираем промпт в зависимости от режима
                if (CalendarModeRadio.IsChecked == true)
                {
                    if (HolidayComboBox.SelectedItem is Holiday holiday)
                    {
                        prompt = _calendarService.GeneratePromptForHoliday(holiday);
                        StatusTextBlock.Text = $"🎉 Создаю обои к празднику: {holiday.Name}\n⏳ Генерация...";
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
                    if (string.IsNullOrWhiteSpace(prompt) || prompt == "Красивые обои на рабочий стол, горный пейзаж, закат")
                    {
                        prompt = PromptTextBox.Text; // Берем как есть
                    }

                    if (string.IsNullOrWhiteSpace(prompt))
                    {
                        prompt = "Красивые обои на рабочий стол, горный пейзаж, закат";
                    }

                    // Получаем параметры для ручного режима
                    string style = ((ComboBoxItem)StyleComboBox.SelectedItem).Content.ToString();
                    string colorPalette = ((ComboBoxItem)ColorComboBox.SelectedItem).Content.ToString();
                    string aspectRatio = ((ComboBoxItem)AspectRatioComboBox.SelectedItem).Content.ToString();

                    // Создаем промпт с параметрами
                    prompt = $"{prompt}, стиль: {style}, цветовая палитра: {colorPalette}, " +
                            $"соотношение сторон: {aspectRatio}, высокое качество, детализированное, " +
                            "профессиональная графика, обои рабочего стола";

                    StatusTextBlock.Text = "⏳ Генерация изображения...";
                }

                // Генерируем и скачиваем изображение за один вызов
                StatusTextBlock.Text = "⏳ Отправка запроса в GigaChat...";
                _lastImagePath = await _apiService.GenerateAndSaveImageAsync(prompt);

                if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
                {
                    // Показываем предпросмотр
                    await ShowPreviewImage(_lastImagePath);

                    StatusTextBlock.Text = "✅ Изображение сгенерировано успешно!";
                    DownloadButton.IsEnabled = true;
                    SetWallpaperButton.IsEnabled = true;

                    // Показываем информацию о файле
                    var fileInfo = new FileInfo(_lastImagePath);
                    StatusTextBlock.Text += $"\nФайл: {Path.GetFileName(_lastImagePath)} ({fileInfo.Length / 1024} KB)";
                }
                else
                {
                    StatusTextBlock.Text = "❌ Не удалось создать изображение";
                }
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"❌ Ошибка: {ex.Message}";
                if (ex.InnerException != null)
                {
                    StatusTextBlock.Text += $"\nВнутренняя ошибка: {ex.InnerException.Message}";
                }
            }
            finally
            {
                GenerateButton.IsEnabled = true;
            }
        }

        private async Task ShowPreviewImage(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.UriSource = new Uri(imagePath);
                            bitmap.EndInit();

                            PreviewImage.Source = bitmap;
                            PreviewPlaceholder.Visibility = Visibility.Collapsed;
                        }
                        catch (Exception ex)
                        {
                            StatusTextBlock.Text = $"❌ Ошибка создания предпросмотра: {ex.Message}";
                        }
                    });
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
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
                {
                    var saveDialog = new SaveFileDialog
                    {
                        FileName = $"wallpaper_{DateTime.Now:yyyyMMddHHmmss}.jpg",
                        Filter = "JPEG Image|*.jpg|PNG Image|*.png|All Files|*.*",
                        DefaultExt = ".jpg"
                    };

                    if (saveDialog.ShowDialog() == true)
                    {
                        File.Copy(_lastImagePath, saveDialog.FileName, true);
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