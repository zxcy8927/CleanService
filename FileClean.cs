using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CleanService.Services;
//FileClean 繼承BackgroundService
public class FileClean : BackgroundService
{
    private readonly ILogger<FileClean> _logger; //寫Log
    private readonly IConfiguration _configuration; //讀取設定
    private string _targetDirectory = ""; //欲清理資料夾路徑
    private int _deleteAfterDays = 7; //幾天前的檔案才會被清除
    private int _intervalMinutes = 60; //多久執行一次清理

    //注入Log、設定
    public FileClean(ILogger<FileClean> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    //在服務啟動後會自動呼叫ExecuteAsync
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoadSettings();

        //直到服務停止
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                CleanOldFiles();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while cleaning files.");
            }
            //等待下一輪的清理(60min後)
            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
        }
    }

    //讀取設定值
    //從 appsettings.json 的 FileCleanerSettings 區塊讀取清理參數。
    private void LoadSettings()
    {
        _targetDirectory = _configuration["FileCleanerSettings:TargetDirectory"] ?? "C:\\Users\\User\\AppData\\Local\\Temp";
        _deleteAfterDays = int.Parse(_configuration["FileCleanerSettings:DeleteAfterDays"] ?? "7");
        _intervalMinutes = int.Parse(_configuration["FileCleanerSettings:IntervalMinutes"] ?? "60");

        //在log中顯示設定資訊
        _logger.LogInformation("Cleaner Settings: Dir={Directory}, Days={Days}, Interval={Interval}",
            _targetDirectory, _deleteAfterDays, _intervalMinutes);
    }

    private void CleanOldFiles()
    {
        //若目標目錄不存在也會寫進Log中
        if (!Directory.Exists(_targetDirectory))
        {
            _logger.LogWarning("Target directory does not exist: {Directory}", _targetDirectory);
            return;
        }

        var files = Directory.GetFiles(_targetDirectory);
        var now = DateTime.Now;

        foreach (var file in files)
        {
            var lastWrite = File.GetLastWriteTime(file); //檔案的最後修改時間

            //計算每個檔案的「最後修改時間」
            if ((now - lastWrite).TotalDays >= _deleteAfterDays)
            {
                try
                {
                    File.Delete(file);
                    //成功就在Log寫一條刪除哪些檔案的訊息
                    _logger.LogInformation("Deleted file: {File}", file);
                }
                catch (Exception ex)
                {
                    //失敗也會記 Log，但等級為 Warning。
                    _logger.LogWarning(ex, "Failed to delete file: {File}", file);
                }
            }
        }
        //清理完後記錄清理時間。
        _logger.LogInformation("File cleanup completed at: {Time}", now);
    }
}
