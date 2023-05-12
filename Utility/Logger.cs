using System;
using System.Globalization;
using System.IO;

namespace MFFSuCA.Utility; 

/// <summary>
/// Class for logging messages and exceptions.
/// </summary>
public static class Logger {
    /// <summary>
    /// Path to the info log file.
    /// </summary>
    private const string LogFilePath = @"logs/log.txt";
    
    /// <summary>
    /// Path to the error log file.
    /// </summary>
    private const string ErrorLogFilePath = @"logs/error_log.txt";
    
    /// <summary>
    /// Logs a message to the info log file.
    /// </summary>
    /// <param name="message">Message to log.</param>
    public static void Log(string message) {
        try {
            if (!Directory.Exists("logs")) {
                Directory.CreateDirectory("logs");
            }
            if (!File.Exists(LogFilePath)) {
                File.Create(LogFilePath).Close();
            }
            File.AppendAllText(LogFilePath,
                               $"INFO -- [{DateTime.Now.ToString(CultureInfo.CurrentCulture)}]:" +
                               $" {message}\n");
        } catch (Exception) {
            // ignored
        }
    }
    
    /// <summary>
    /// Logs an exception to the error log file.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    public static void Log(Exception exception) {
        try {
            if (!Directory.Exists("logs")) {
                Directory.CreateDirectory("logs");
            }
            if (!File.Exists(ErrorLogFilePath)) {
                File.Create(ErrorLogFilePath).Close();
            }
            File.AppendAllText(ErrorLogFilePath,
                               $"ERROR -- [{DateTime.Now.ToString(CultureInfo.CurrentCulture)}]:" +
                               $" {exception.Message}\n{exception.StackTrace}\n");
        } catch (Exception) {
            // ignored
        }
    }
}
