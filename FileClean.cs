using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace CleanService.Services;
//FileClean �~��BackgroundService
public class FileClean : BackgroundService
{
    private readonly ILogger<FileClean> _logger; //�gLog
    private readonly IConfiguration _configuration; //Ū���]�w
    private string _targetDirectory = ""; //���M�z��Ƨ����|
    private int _deleteAfterDays = 7; //�X�ѫe���ɮפ~�|�Q�M��
    private int _intervalMinutes = 60; //�h�[����@���M�z

    //�`�JLog�B�]�w
    public FileClean(ILogger<FileClean> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    //�b�A�ȱҰʫ�|�۰ʩI�sExecuteAsync
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        LoadSettings();

        //����A�Ȱ���
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
            //���ݤU�@�����M�z(60min��)
            await Task.Delay(TimeSpan.FromMinutes(_intervalMinutes), stoppingToken);
        }
    }

    //Ū���]�w��
    //�q appsettings.json �� FileCleanerSettings �϶�Ū���M�z�ѼơC
    private void LoadSettings()
    {
        _targetDirectory = _configuration["FileCleanerSettings:TargetDirectory"] ?? "C:\\Users\\User\\AppData\\Local\\Temp";
        _deleteAfterDays = int.Parse(_configuration["FileCleanerSettings:DeleteAfterDays"] ?? "7");
        _intervalMinutes = int.Parse(_configuration["FileCleanerSettings:IntervalMinutes"] ?? "60");

        //�blog����ܳ]�w��T
        _logger.LogInformation("Cleaner Settings: Dir={Directory}, Days={Days}, Interval={Interval}",
            _targetDirectory, _deleteAfterDays, _intervalMinutes);
    }

    private void CleanOldFiles()
    {
        //�Y�ؼХؿ����s�b�]�|�g�iLog��
        if (!Directory.Exists(_targetDirectory))
        {
            _logger.LogWarning("Target directory does not exist: {Directory}", _targetDirectory);
            return;
        }

        var files = Directory.GetFiles(_targetDirectory);
        var now = DateTime.Now;

        foreach (var file in files)
        {
            var lastWrite = File.GetLastWriteTime(file); //�ɮת��̫�ק�ɶ�

            //�p��C���ɮת��u�̫�ק�ɶ��v
            if ((now - lastWrite).TotalDays >= _deleteAfterDays)
            {
                try
                {
                    File.Delete(file);
                    //���\�N�bLog�g�@���R�������ɮת��T��
                    _logger.LogInformation("Deleted file: {File}", file);
                }
                catch (Exception ex)
                {
                    //���Ѥ]�|�O Log�A�����Ŭ� Warning�C
                    _logger.LogWarning(ex, "Failed to delete file: {File}", file);
                }
            }
        }
        //�M�z����O���M�z�ɶ��C
        _logger.LogInformation("File cleanup completed at: {Time}", now);
    }
}
