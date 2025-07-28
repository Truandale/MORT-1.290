using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace MORT
{
    /// <summary>
    /// Утилита для диагностики VOSK STT Engine
    /// Помогает отладить проблемы с распознаванием речи
    /// </summary>
    public static class VoskDiagnostics
    {
        private static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "vosk_diagnostics.log");
        
        /// <summary>
        /// Записывает диагностическое сообщение в лог
        /// </summary>
        public static void Log(string message, string category = "INFO")
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] [{category}] {message}{Environment.NewLine}";
                
                File.AppendAllText(LogFile, logEntry, Encoding.UTF8);
                
                // Также выводим в консоль отладки
                Debug.WriteLine($"VOSK: [{category}] {message}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка записи в лог VOSK: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Проверяет наличие и валидность модели VOSK
        /// </summary>
        public static bool ValidateModel(string modelPath, string language)
        {
            Log($"Проверка модели: {modelPath} для языка: {language}");
            
            if (string.IsNullOrEmpty(modelPath))
            {
                Log("Путь к модели не указан", "ERROR");
                return false;
            }
            
            if (!Directory.Exists(modelPath))
            {
                Log($"Директория модели не найдена: {modelPath}", "ERROR");
                return false;
            }
            
            // Проверяем обязательные файлы модели
            var requiredFiles = new[]
            {
                "am/final.mdl",
                "graph/Gr.fst",
                "graph/HCLr.fst",
                "conf/mfcc.conf",
                "conf/model.conf"
            };
            
            foreach (var file in requiredFiles)
            {
                var fullPath = Path.Combine(modelPath, file);
                if (!File.Exists(fullPath))
                {
                    Log($"Отсутствует обязательный файл модели: {fullPath}", "ERROR");
                    return false;
                }
            }
            
            Log($"Модель прошла валидацию: {modelPath}", "SUCCESS");
            return true;
        }
        
        /// <summary>
        /// Проверяет доступные модели VOSK
        /// </summary>
        public static void CheckAvailableModels()
        {
            Log("=== Проверка доступных моделей VOSK ===");
            
            var modelsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "mort_resource", "models", "vosk");
            
            if (!Directory.Exists(modelsPath))
            {
                Log($"Директория моделей не найдена: {modelsPath}", "ERROR");
                return;
            }
            
            var modelDirs = Directory.GetDirectories(modelsPath);
            
            if (modelDirs.Length == 0)
            {
                Log("Модели VOSK не найдены", "WARNING");
                return;
            }
            
            foreach (var modelDir in modelDirs)
            {
                var modelName = Path.GetFileName(modelDir);
                Log($"Найдена модель: {modelName}");
                
                // Определяем язык по имени модели
                string language = "unknown";
                if (modelName.Contains("-ru-"))
                    language = "Russian";
                else if (modelName.Contains("-en-"))
                    language = "English";
                
                var isValid = ValidateModel(modelDir, language);
                Log($"Модель {modelName} [{language}]: {(isValid ? "✓ Валидна" : "✗ Невалидна")}");
            }
        }
        
        /// <summary>
        /// Проверяет системные требования для VOSK
        /// </summary>
        public static void CheckSystemRequirements()
        {
            Log("=== Проверка системных требований ===");
            
            // Проверяем доступную память
            try
            {
                var totalMemory = GC.GetTotalMemory(false);
                Log($"Используется памяти: {totalMemory / 1024 / 1024} MB");
            }
            catch (Exception ex)
            {
                Log($"Не удалось проверить память: {ex.Message}", "ERROR");
            }
            
            // Проверяем .NET версию
            try
            {
                var dotnetVersion = Environment.Version;
                Log($".NET версия: {dotnetVersion}");
            }
            catch (Exception ex)
            {
                Log($"Не удалось определить .NET версию: {ex.Message}", "ERROR");
            }
            
            // Проверяем операционную систему
            try
            {
                var osVersion = Environment.OSVersion;
                Log($"ОС: {osVersion}");
            }
            catch (Exception ex)
            {
                Log($"Не удалось определить ОС: {ex.Message}", "ERROR");
            }
        }
        
        /// <summary>
        /// Выполняет полную диагностику VOSK
        /// </summary>
        public static void RunFullDiagnostics()
        {
            Log("========================================");
            Log("       НАЧАЛО ДИАГНОСТИКИ VOSK        ");
            Log("========================================");
            
            CheckSystemRequirements();
            CheckAvailableModels();
            
            Log("========================================");
            Log("       ДИАГНОСТИКА ЗАВЕРШЕНА          ");
            Log("========================================");
            Log($"Результаты сохранены в: {LogFile}");
        }
        
        /// <summary>
        /// Очищает лог диагностики
        /// </summary>
        public static void ClearLog()
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    File.Delete(LogFile);
                }
                Log("Лог очищен");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка очистки лога: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Записывает информацию об ошибке VOSK
        /// </summary>
        public static void LogError(string operation, Exception exception)
        {
            Log($"ОШИБКА при {operation}: {exception.Message}", "ERROR");
            Log($"Stack trace: {exception.StackTrace}", "ERROR");
        }
        
        /// <summary>
        /// Записывает информацию о successful операции VOSK
        /// </summary>
        public static void LogSuccess(string operation, string details = "")
        {
            var message = string.IsNullOrEmpty(details) ? 
                $"Успешно: {operation}" : 
                $"Успешно: {operation} - {details}";
            Log(message, "SUCCESS");
        }
    }
}
