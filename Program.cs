using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using System.Reflection;
using SpanningTree.GUI;

namespace SpanningTree
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // Извлечение нативной DLL при запуске
            ExtractNativeLibrary();
            
            try
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                // Регистрация обработчиков необработанных исключений
                Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
                AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

                Application.Run(new GUI.MainForm());
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private static void ExtractNativeLibrary()
        {
            try
            {
                string dllName = "SpanningTreeCore.dll";
                // Путь, куда будет извлечена DLL (рядом с EXE)
                string dllPath = Path.Combine(AppContext.BaseDirectory, dllName);

                // Проверяем, существует ли DLL (возможно, извлечена ранее)
                if (File.Exists(dllPath))
                {
                    // Опционально: можно добавить проверку версии/хэша, 
                    // чтобы перезаписывать DLL, если она обновилась в ресурсах.
                    // Пока просто выходим, если файл уже есть.
                    return; 
                }

                // Имя ресурса обычно Namespace.FileName
                // Убедитесь, что корневой namespace вашего проекта - SpanningTree
                string resourceName = "SpanningTree." + dllName;
                Assembly currentAssembly = Assembly.GetExecutingAssembly();

                using (Stream? resourceStream = currentAssembly.GetManifestResourceStream(resourceName))
                {
                    if (resourceStream == null)
                    {
                        throw new Exception($"Не удалось найти внедренный ресурс: {resourceName}. Убедитесь, что DLL скопирована к .csproj и свойство Build Action для нее установлено в Embedded Resource.");
                    }

                    using (FileStream fileStream = new FileStream(dllPath, FileMode.Create))
                    {
                        resourceStream.CopyTo(fileStream);
                    }
                }
                Console.WriteLine($"Библиотека {dllName} извлечена в {dllPath}");
            }
            catch (Exception ex)
            {
                 // Показываем ошибку пользователю, так как без DLL приложение не будет работать
                 MessageBox.Show($"Критическая ошибка при извлечении нативной библиотеки:\n{ex.Message}\n\nПриложение не может продолжить работу.", 
                                 "Ошибка инициализации", MessageBoxButtons.OK, MessageBoxIcon.Error);
                // Завершаем приложение, если не удалось извлечь DLL
                Environment.Exit(1); 
            }
        }

        private static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
        }

        private static void HandleException(Exception? ex)
        {
            if (ex == null) return;

            string errorMessage = $"Произошла непредвиденная ошибка: {ex.Message}\n\nПодробности записаны в error.log";
            LogException(ex);
            MessageBox.Show(errorMessage, "Критическая ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static void LogException(Exception ex)
        {
            try
            {
                string logFilePath = Path.Combine(AppContext.BaseDirectory, "error.log");
                string logMessage = $"[{DateTime.Now}] {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}\n\n";
                File.AppendAllText(logFilePath, logMessage);
            }
            catch
            {
                // Не удалось записать лог, игнорируем
            }
        }
    }
}
