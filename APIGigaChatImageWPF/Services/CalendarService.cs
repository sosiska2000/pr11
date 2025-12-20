using System;
using System.Collections.Generic;
using System.Linq;

namespace APIGigaChatImageWPF.Services
{
    public class CalendarService
    {
        private List<Holiday> _holidays;

        public CalendarService()
        {
            InitializeHolidays();
        }

        private void InitializeHolidays()
        {
            int year = DateTime.Now.Year;

            _holidays = new List<Holiday>
            {
                new Holiday(new DateTime(year, 1, 1), "Новый год",
                    "Новогодние праздники, ёлка, снег, зима", "#00FF00"),
                new Holiday(new DateTime(year, 1, 7), "Рождество Христово",
                    "Православное Рождество, церковь, ангелы", "#FFFFFF"),
                new Holiday(new DateTime(year, 2, 23), "День защитника Отечества",
                    "Военный праздник, армия, защита, флаг", "#0000FF"),
                new Holiday(new DateTime(year, 3, 8), "Международный женский день",
                    "Весенний праздник, цветы, тюльпаны", "#FF69B4"),
                new Holiday(new DateTime(year, 5, 9), "День Победы",
                    "Праздник победы в ВОВ, георгиевская лента, салют", "#FFD700"),
                new Holiday(new DateTime(year, 6, 12), "День России",
                    "Государственный праздник, флаг, триколор", "#FFFFFF"),
                new Holiday(new DateTime(year, 11, 4), "День народного единства",
                    "Исторический праздник, единство, народ", "#FF4500")
            };
        }

        public Holiday GetNearestHoliday()
        {
            var today = DateTime.Today;
            var upcoming = _holidays
                .Where(h => h.Date >= today)
                .OrderBy(h => h.Date)
                .FirstOrDefault();

            return upcoming ?? _holidays.OrderBy(h => h.Date).First();
        }

        public List<Holiday> GetAllHolidays()
        {
            return _holidays;
        }

        public string GeneratePromptForHoliday(Holiday holiday)
        {
            return $"Создай обои на рабочий стол в стиле 'реализм' на тему праздника '{holiday.Name}'. " +
                   $"Тема: {holiday.Description}. " +
                   $"Высокое качество, детализация, 4K разрешение, без текста.";
        }
    }

    public class Holiday
    {
        public DateTime Date { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ThemeColor { get; set; }

        public Holiday(DateTime date, string name, string description, string themeColor)
        {
            Date = date;
            Name = name;
            Description = description;
            ThemeColor = themeColor;
        }

        public override string ToString()
        {
            return $"{Name} ({Date:dd.MM.yyyy})";
        }
    }
}