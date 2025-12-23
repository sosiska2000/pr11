using System; // Использование базовых классов .NET (DateTime, Exception и т.д.)
using System.Collections.Generic; // Использование коллекций (List<T>, IEnumerable и т.д.)
using System.Linq; // Использование LINQ-методов (Where, OrderBy, FirstOrDefault и т.д.)

namespace APIGigaChatImageWPF.Services // Пространство имен для сервисных классов WPF-приложения
{
    // Класс сервиса для работы с календарем праздников
    // Предоставляет методы для получения информации о праздниках и генерации промптов
    public class CalendarService
    {
        // Поле для хранения списка праздников
        private List<Holiday> _holidays;

        // Конструктор класса - инициализирует сервис и загружает праздники
        public CalendarService()
        {
            InitializeHolidays(); // Вызов метода инициализации праздников
        }

        // Приватный метод для инициализации списка праздников
        private void InitializeHolidays()
        {
            // Получаем текущий год для создания дат праздников
            int year = DateTime.Now.Year;

            // Инициализация списка праздников с основными российскими праздниками
            _holidays = new List<Holiday>
            {
                // 1 января - Новый год
                new Holiday(new DateTime(year, 1, 1), "Новый год",
                    "Новогодние праздники, ёлка, снег, зима", "#00FF00"), // Зеленый цвет
                
                // 7 января - Рождество Христово (православное)
                new Holiday(new DateTime(year, 1, 7), "Рождество Христово",
                    "Православное Рождество, церковь, ангелы", "#FFFFFF"), // Белый цвет
                
                // 23 февраля - День защитника Отечества
                new Holiday(new DateTime(year, 2, 23), "День защитника Отечества",
                    "Военный праздник, армия, защита, флаг", "#0000FF"), // Синий цвет
                
                // 8 марта - Международный женский день
                new Holiday(new DateTime(year, 3, 8), "Международный женский день",
                    "Весенний праздник, цветы, тюльпаны", "#FF69B4"), // Розовый цвет
                
                // 9 мая - День Победы
                new Holiday(new DateTime(year, 5, 9), "День Победы",
                    "Праздник победы в ВОВ, георгиевская лента, салют", "#FFD700"), // Золотой цвет
                
                // 12 июня - День России
                new Holiday(new DateTime(year, 6, 12), "День России",
                    "Государственный праздник, флаг, триколор", "#FFFFFF"), // Белый цвет
                
                // 4 ноября - День народного единства
                new Holiday(new DateTime(year, 11, 4), "День народного единства",
                    "Исторический праздник, единство, народ", "#FF4500") // Оранжево-красный цвет
            };
        }

        // Метод для получения ближайшего праздника (включая текущий день)
        public Holiday GetNearestHoliday()
        {
            var today = DateTime.Today; // Текущая дата без времени

            // Поиск ближайшего праздника с использованием LINQ
            var upcoming = _holidays
                .Where(h => h.Date >= today) // Фильтрация: праздники начиная с сегодняшнего дня
                .OrderBy(h => h.Date) // Сортировка по дате (от ближайшего к дальнему)
                .FirstOrDefault(); // Получение первого элемента или null, если нет праздников

            // Если найдены будущие праздники, возвращаем ближайший
            // Если нет - возвращаем первый праздник в списке (Новый год следующего года)
            return upcoming ?? _holidays.OrderBy(h => h.Date).First();
        }

        // Метод для получения всех праздников
        public List<Holiday> GetAllHolidays()
        {
            return _holidays; // Возврат полного списка праздников
        }

        // Метод для генерации промпта (запроса) для нейросети на основе праздника
        public string GeneratePromptForHoliday(Holiday holiday)
        {
            // Формирование детального промпта с параметрами для генерации изображения
            return $"Создай обои на рабочий стол в стиле 'реализм' на тему праздника '{holiday.Name}'. " +
                   $"Тема: {holiday.Description}. " +
                   $"Высокое качество, детализация, 4K разрешение, без текста.";
        }
    }

    // Класс, представляющий один праздник
    public class Holiday
    {
        // Свойства праздника
        public DateTime Date { get; set; } // Дата праздника
        public string Name { get; set; } // Название праздника
        public string Description { get; set; } // Описание/тема для генерации изображения
        public string ThemeColor { get; set; } // Цветовая тема в формате HEX (#RRGGBB)

        // Конструктор класса Holiday
        public Holiday(DateTime date, string name, string description, string themeColor)
        {
            Date = date; // Установка даты
            Name = name; // Установка названия
            Description = description; // Установка описания
            ThemeColor = themeColor; // Установка цветовой темы
        }

        // Переопределение метода ToString() для удобного отображения праздника
        public override string ToString()
        {
            // Формат: "Название праздника (дд.мм.гггг)"
            return $"{Name} ({Date:dd.MM.yyyy})";
        }
    }
}