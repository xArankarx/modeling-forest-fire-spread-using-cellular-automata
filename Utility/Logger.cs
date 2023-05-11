using System;
using System.Globalization;
using System.IO;

namespace MFFSuCA.Utility; 

public static class Logger {
    private const string LogFilePath = @"logs/log.txt";
    
    private const string ErrorLogFilePath = @"logs/error_log.txt";
    
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
