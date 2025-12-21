using System;
using System.IO;
using System.Windows;

namespace APIGigaChatImageWPF
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Создаем папку для сохранения изображений
            string savePath = System.Configuration.ConfigurationManager.AppSettings["SavePath"];
            if (string.IsNullOrEmpty(savePath))
            {
                savePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                                       "GigaChat Wallpapers");
            }

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }
        }
    }
}