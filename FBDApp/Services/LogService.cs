using System;
using System.IO;

namespace FBDApp.Services
{
    /// <summary>
    /// Provides logging functionality for the application.
    /// Logs are written to hourly files in the Logs directory.
    /// </summary>
    public static class LogService
    {
        /// <summary>
        /// Gets the path to the Logs directory within the application's base directory
        /// </summary>
        private static readonly string LogDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");

        /// <summary>
        /// Gets the current log file path based on the current hour
        /// </summary>
        private static string CurrentLogFile => GetCurrentLogFilePath();

        /// <summary>
        /// Generates the path for the current log file using the format: app_yyyy-MM-dd_HH.log
        /// </summary>
        /// <returns>The full path to the current log file</returns>
        private static string GetCurrentLogFilePath()
        {
            var now = DateTime.Now;
            var fileName = $"app_{now:yyyy-MM-dd_HH}.log";
            
            // Ensure the logs directory exists
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            return Path.Combine(LogDirectory, fileName);
        }

        /// <summary>
        /// Logs an error message along with exception details
        /// </summary>
        /// <param name="ex">The exception to log</param>
        /// <param name="message">Additional message to provide context (optional)</param>
        public static void LogError(Exception ex, string message = "")
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}\n{ex.Message}\n{ex.StackTrace}\n";
                if (ex.InnerException != null)
                {
                    logMessage += $"Inner Exception: {ex.InnerException.Message}\n{ex.InnerException.StackTrace}\n";
                }
                logMessage += "----------------------------------------\n";

                File.AppendAllText(CurrentLogFile, logMessage);
            }
            catch
            {
                // Silently fail if unable to write to log file
                // This prevents logging errors from causing additional exceptions
            }
        }

        /// <summary>
        /// Logs an informational message
        /// </summary>
        /// <param name="message">The message to log</param>
        public static void LogInfo(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n";
                File.AppendAllText(CurrentLogFile, logMessage);
            }
            catch
            {
                // Silently fail if unable to write to log file
                // This prevents logging errors from causing additional exceptions
            }
        }

        /// <summary>
        /// Logs a warning message
        /// </summary>
        /// <param name="message">The warning message to log</param>
        public static void LogWarning(string message)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARNING: {message}\n";
                File.AppendAllText(CurrentLogFile, logMessage);
            }
            catch
            {
                // Silently fail if unable to write to log file
                // This prevents logging errors from causing additional exceptions
            }
        }
    }
}
