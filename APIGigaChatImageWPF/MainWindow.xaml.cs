using APIGigaChatImageWPF.Classes; // Использование классов из пространства имен классов (WallpaperSetter)
using APIGigaChatImageWPF.Services; // Использование сервисов (ApiService, CalendarService)
using Microsoft.Win32; // Использование стандартных диалогов Windows (SaveFileDialog)
using System; // Использование базовых классов .NET (Exception, DateTime, Uri и т.д.)
using System.Configuration; // Использование конфигурации приложения
using System.IO; // Использование классов для работы с файловой системой (File, Path, FileInfo)
using System.Threading.Tasks; // Использование асинхронного программирования (Task, async/await)
using System.Windows; // Использование базовых классов WPF (Window, RoutedEventArgs, Visibility)
using System.Windows.Controls; // Использование элементов управления WPF (ComboBox, TextBox, Button)
using System.Windows.Media.Imaging; // Использование классов для работы с изображениями (BitmapImage)

namespace APIGigaChatImageWPF // Основное пространство имен WPF-приложения
{
    // Главное окно приложения, частичный класс (другая часть определена в XAML)
    public partial class MainWindow : Window
    {
        // Поля для хранения экземпляров сервисов
        private ApiService _apiService; // Сервис для работы с API GigaChat
        private CalendarService _calendarService; // Сервис для работы с календарем праздников
        private WallpaperSetter _wallpaperSetter; // Сервис для установки обоев

        private string _lastImagePath; // Путь к последнему сгенерированному изображению

        // Конструктор главного окна
        public MainWindow()
        {
            InitializeComponent(); // Инициализация компонентов из XAML
            InitializeServices(); // Инициализация сервисов
            InitializeCalendarMode(); // Настройка режима "Календарь праздников"
            UpdatePreviewPlaceholder(); // Обновление плейсхолдера предпросмотра
        }

        // Метод для инициализации сервисов
        private void InitializeServices()
        {
            _apiService = new ApiService(); // Создание сервиса API
            _calendarService = new CalendarService(); // Создание сервиса календаря
            _wallpaperSetter = new WallpaperSetter(); // Создание сервиса установки обоев
        }

        // Метод для инициализации режима "Календарь праздников"
        private void InitializeCalendarMode()
        {
            HolidayComboBox.Items.Clear(); // Очистка комбобокса праздников
            var holidays = _calendarService.GetAllHolidays(); // Получение всех праздников

            // Добавление праздников в комбобокс
            foreach (var holiday in holidays)
            {
                HolidayComboBox.Items.Add(holiday); // Добавление объекта Holiday
            }

            // Установка первого праздника как выбранного
            if (HolidayComboBox.Items.Count > 0)
                HolidayComboBox.SelectedIndex = 0;
        }

        // Обработчик изменения выбора режима (Календарь/Ручной)
        private void ModeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            // Проверка инициализации радиокнопок
            if (CalendarModeRadio == null || ManualModeRadio == null)
                return;

            bool isCalendarMode = CalendarModeRadio.IsChecked == true; // Определение текущего режима

            // Переключение видимости панелей в зависимости от режима
            if (CalendarModePanel != null)
                CalendarModePanel.Visibility = isCalendarMode ? Visibility.Visible : Visibility.Collapsed;

            if (ManualModePanel != null)
                ManualModePanel.Visibility = isCalendarMode ? Visibility.Collapsed : Visibility.Visible;

            // Если выбран режим календаря, обновляем информацию о празднике
            if (isCalendarMode && HolidayComboBox != null)
            {
                UpdateHolidayInfo();
            }
        }

        // Обработчик изменения выбора праздника в комбобоксе
        private void HolidayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Проверка условий: режим календаря выбран и элемент выбран
            if (CalendarModeRadio == null || CalendarModeRadio.IsChecked != true || HolidayComboBox.SelectedItem == null)
                return;

            UpdateHolidayInfo(); // Обновление информации о выбранном празднике
        }

        // Метод для обновления информации о выбранном празднике
        private void UpdateHolidayInfo()
        {
            // Проверка, что выбранный элемент является объектом Holiday
            if (HolidayComboBox.SelectedItem is Holiday holiday)
            {
                // Обновление текстовых полей с информацией о празднике
                if (HolidayNameText != null)
                    HolidayNameText.Text = holiday.Name;

                if (HolidayDescriptionText != null)
                    HolidayDescriptionText.Text = holiday.Description;

                if (HolidayDateText != null)
                    HolidayDateText.Text = $"Дата праздника: {holiday.Date:dd.MM.yyyy}";

                // Показ панели с информацией
                if (HolidayInfoPanel != null)
                    HolidayInfoPanel.Visibility = Visibility.Visible;

                // Генерация промпта для нейросети на основе праздника
                string prompt = _calendarService.GeneratePromptForHoliday(holiday);

                if (GeneratedPromptText != null)
                    GeneratedPromptText.Text = prompt;

                // Показ панели с сгенерированным промптом
                if (GeneratedPromptPanel != null)
                    GeneratedPromptPanel.Visibility = Visibility.Visible;
            }
        }

        // Асинхронный обработчик нажатия кнопки "Генерировать"
        private async void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Обновление статуса и отключение кнопок на время генерации
                StatusTextBlock.Text = "Начинаю генерацию изображения...";
                GenerateButton.IsEnabled = false;
                DownloadButton.IsEnabled = false;
                SetWallpaperButton.IsEnabled = false;

                string prompt; // Переменная для промпта

                // ВЫБОР ПРОМПТА В ЗАВИСИМОСТИ ОТ РЕЖИМА

                // Режим "Календарь праздников"
                if (CalendarModeRadio.IsChecked == true)
                {
                    if (HolidayComboBox.SelectedItem is Holiday holiday)
                    {
                        prompt = _calendarService.GeneratePromptForHoliday(holiday);
                        StatusTextBlock.Text = $"Создаю обои к празднику: {holiday.Name}\nГенерация...";
                    }
                    else
                    {
                        StatusTextBlock.Text = "Выберите праздник";
                        GenerateButton.IsEnabled = true;
                        return;
                    }
                }
                // Режим "Ручной ввод"
                else
                {
                    prompt = PromptTextBox.Text;

                    // Проверка на пустой или дефолтный текст
                    if (string.IsNullOrWhiteSpace(prompt) || prompt == "Красивые обои на рабочий стол, горный пейзаж, закат")
                    {
                        prompt = PromptTextBox.Text; // Берем как есть
                    }

                    // Если промпт все еще пустой, используем значение по умолчанию
                    if (string.IsNullOrWhiteSpace(prompt))
                    {
                        prompt = "Красивые обои на рабочий стол, горный пейзаж, закат";
                    }

                    // Получение дополнительных параметров из комбобоксов
                    string style = ((ComboBoxItem)StyleComboBox.SelectedItem).Content.ToString();
                    string colorPalette = ((ComboBoxItem)ColorComboBox.SelectedItem).Content.ToString();
                    string aspectRatio = ((ComboBoxItem)AspectRatioComboBox.SelectedItem).Content.ToString();

                    // Создание улучшенного промпта с параметрами
                    prompt = $"{prompt}, стиль: {style}, цветовая палитра: {colorPalette}, " +
                            $"соотношение сторон: {aspectRatio}, высокое качество, детализированное, " +
                            "профессиональная графика, обои рабочего стола";

                    StatusTextBlock.Text = "Генерация изображения...";
                }

                // ГЕНЕРАЦИЯ ИЗОБРАЖЕНИЯ

                StatusTextBlock.Text = "Отправка запроса в GigaChat...";
                _lastImagePath = await _apiService.GenerateAndSaveImageAsync(prompt);

                // ОБРАБОТКА РЕЗУЛЬТАТА

                if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
                {
                    // Показ предпросмотра изображения
                    await ShowPreviewImage(_lastImagePath);

                    StatusTextBlock.Text = "Изображение сгенерировано успешно!";
                    DownloadButton.IsEnabled = true;
                    SetWallpaperButton.IsEnabled = true;

                    // Вывод информации о файле
                    var fileInfo = new FileInfo(_lastImagePath);
                    StatusTextBlock.Text += $"\nФайл: {Path.GetFileName(_lastImagePath)} ({fileInfo.Length / 1024} KB)";
                }
                else
                {
                    StatusTextBlock.Text = "Не удалось создать изображение";
                }
            }
            catch (Exception ex) // Обработка исключений
            {
                StatusTextBlock.Text = $"Ошибка: {ex.Message}";
                if (ex.InnerException != null) // Вывод внутренней ошибки, если есть
                {
                    StatusTextBlock.Text += $"\nВнутренняя ошибка: {ex.InnerException.Message}";
                }
            }
            finally // Блок, выполняющийся всегда
            {
                GenerateButton.IsEnabled = true; // Включение кнопки генерации
            }
        }

        // Асинхронный метод для отображения предпросмотра изображения
        private async Task ShowPreviewImage(string imagePath)
        {
            try
            {
                if (File.Exists(imagePath)) // Проверка существования файла
                {
                    // Использование Dispatcher для безопасного обновления UI из другого потока
                    await Dispatcher.InvokeAsync(() =>
                    {
                        try
                        {
                            // Создание и настройка BitmapImage
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit(); // Начало инициализации
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // Кэширование при загрузке
                            bitmap.UriSource = new Uri(imagePath); // Установка источника
                            bitmap.EndInit(); // Завершение инициализации

                            PreviewImage.Source = bitmap; // Установка изображения в элемент Image
                            PreviewPlaceholder.Visibility = Visibility.Collapsed; // Скрытие плейсхолдера
                        }
                        catch (Exception ex) // Обработка ошибок создания изображения
                        {
                            StatusTextBlock.Text = $"Ошибка создания предпросмотра: {ex.Message}";
                        }
                    });
                }
                else
                {
                    StatusTextBlock.Text = "Файл изображения не найден";
                }
            }
            catch (Exception ex) // Обработка общих ошибок загрузки
            {
                StatusTextBlock.Text = $"Ошибка загрузки изображения: {ex.Message}";
            }
        }

        // Обработчик нажатия кнопки "Скачать"
        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
                {
                    // Создание диалога сохранения файла
                    var saveDialog = new SaveFileDialog
                    {
                        FileName = $"wallpaper_{DateTime.Now:yyyyMMddHHmmss}.jpg",
                        Filter = "JPEG Image|*.jpg|PNG Image|*.png|All Files|*.*",
                        DefaultExt = ".jpg"
                    };

                    // Показ диалога и сохранение файла
                    if (saveDialog.ShowDialog() == true)
                    {
                        File.Copy(_lastImagePath, saveDialog.FileName, true); // Копирование с перезаписью
                        StatusTextBlock.Text = $"Изображение сохранено: {Path.GetFileName(saveDialog.FileName)}";
                    }
                }
                else
                {
                    StatusTextBlock.Text = "Нет изображения для сохранения";
                }
            }
            catch (Exception ex) // Обработка ошибок сохранения
            {
                StatusTextBlock.Text = $"Ошибка сохранения: {ex.Message}";
            }
        }

        // Обработчик нажатия кнопки "Установить обои"
        private void SetWallpaperButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!string.IsNullOrEmpty(_lastImagePath) && File.Exists(_lastImagePath))
                {
                    _wallpaperSetter.Set(_lastImagePath); // Вызов метода установки обоев
                    StatusTextBlock.Text = "Обои успешно установлены!";
                }
                else
                {
                    StatusTextBlock.Text = "Нет изображения для установки";
                }
            }
            catch (Exception ex) // Обработка ошибок установки
            {
                StatusTextBlock.Text = $"Ошибка установки обоев: {ex.Message}";
            }
        }

        // Метод для обновления состояния плейсхолдера предпросмотра
        private void UpdatePreviewPlaceholder()
        {
            if (PreviewImage != null && PreviewPlaceholder != null)
            {
                // Показ плейсхолдера, если нет изображения
                if (PreviewImage.Source == null)
                {
                    PreviewPlaceholder.Visibility = Visibility.Visible;
                }
                else // Скрытие плейсхолдера, если есть изображение
                {
                    PreviewPlaceholder.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}