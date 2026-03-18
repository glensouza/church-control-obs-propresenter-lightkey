using YoutubeExplode;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;
using FFMpegCore;
using YoutubeExplode.Common;

namespace WorshipConsole.Services;

public class MediaService
{
    private readonly IConfiguration configuration;
    private readonly ILogger<MediaService> logger;
    private readonly string mediaRootPath;
    private readonly string welcomeVideoFolder;
    private readonly string welcomeVideoFileName;
    private readonly string backgroundVideosFolder;
    private readonly string youtubeDownloadsFolder;
    private readonly YoutubeClient youtubeClient;

    public MediaService(IConfiguration configuration, ILogger<MediaService> logger)
    {
        this.configuration = configuration;
        this.logger = logger;

        this.mediaRootPath = this.configuration["ProPresenter:MediaRootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "media");
        this.welcomeVideoFolder = this.configuration["ProPresenter:WelcomeVideoFolder"] ?? "Welcome";
        this.welcomeVideoFileName = this.configuration["ProPresenter:WelcomeVideoFileName"] ?? "Welcome.mp4";
        this.backgroundVideosFolder = this.configuration["ProPresenter:BackgroundVideosFolder"] ?? "Backgrounds";
        this.youtubeDownloadsFolder = this.configuration["ProPresenter:YouTubeDownloadsFolder"] ?? "YouTube";

        this.youtubeClient = new YoutubeClient();
        this.ConfigureFfmpegBinaryFolder();

        // Ensure directories exist
        this.EnsureDirectoryExists(this.GetWelcomeFolderPath());
        this.EnsureDirectoryExists(this.GetBackgroundVideosFolderPath());
        this.EnsureDirectoryExists(this.GetYouTubeFolderPath());
    }

    private void ConfigureFfmpegBinaryFolder()
    {
        string? configuredPath = this.configuration["ProPresenter:FfmpegPath"];
        if (string.IsNullOrWhiteSpace(configuredPath))
        {
            return;
        }

        string? binaryFolder = null;

        if (Directory.Exists(configuredPath))
        {
            binaryFolder = configuredPath;
        }
        else if (File.Exists(configuredPath))
        {
            binaryFolder = Path.GetDirectoryName(configuredPath);
        }

        if (string.IsNullOrWhiteSpace(binaryFolder))
        {
            this.logger.LogWarning("Configured FFmpeg path does not exist: {Path}", configuredPath);
            return;
        }

        GlobalFFOptions.Configure(options => options.BinaryFolder = binaryFolder);
        this.logger.LogInformation("Configured FFmpeg binary folder: {Path}", binaryFolder);
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            this.logger.LogInformation("Created directory: {Path}", path);
        }
    }

    public string GetWelcomeFolderPath() => Path.Combine(this.mediaRootPath, this.welcomeVideoFolder);
    public string GetBackgroundVideosFolderPath() => Path.Combine(this.mediaRootPath, this.backgroundVideosFolder);
    public string GetYouTubeFolderPath() => Path.Combine(this.mediaRootPath, this.youtubeDownloadsFolder);

    public string GetWelcomeVideoFilePath() => Path.Combine(this.GetWelcomeFolderPath(), this.welcomeVideoFileName);

    public async Task<bool> SaveWelcomeVideoAsync(Stream stream, string fileName)
    {
        string filePath = this.GetWelcomeVideoFilePath();
        string tempExtension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(tempExtension))
        {
            tempExtension = ".tmp";
        }

        string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{tempExtension}");
        string tempOutputFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp4");

        try
        {
            if (stream.CanSeek)
            {
                stream.Position = 0;
            }

            await using (FileStream fs = new(tempFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await stream.CopyToAsync(fs);
            }

            bool isValidVideo = await this.IsValidVideoFileAsync(tempFilePath);
            if (!isValidVideo)
            {
                this.logger.LogWarning("Rejected invalid welcome video upload: {FileName}", fileName);
                return false;
            }

            await this.ConvertToMp4Async(tempFilePath, tempOutputFilePath);

            bool isValidConvertedVideo = await this.IsValidVideoFileAsync(tempOutputFilePath);
            if (!isValidConvertedVideo)
            {
                this.logger.LogWarning("Rejected welcome video upload because MP4 conversion output is invalid: {FileName}", fileName);
                return false;
            }

            File.Copy(tempOutputFilePath, filePath, true);
            this.logger.LogInformation("Saved welcome video to {Path}", filePath);
            
            // Generate thumbnail
            await this.GenerateVideoThumbnail(filePath);
            
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error saving welcome video.");
            return false;
        }
        finally
        {
            if (File.Exists(tempFilePath))
            {
                File.Delete(tempFilePath);
            }

            if (File.Exists(tempOutputFilePath))
            {
                File.Delete(tempOutputFilePath);
            }
        }
    }

    private async Task ConvertToMp4Async(string sourcePath, string destinationPath)
    {
        await FFMpegArguments
            .FromFileInput(sourcePath)
            .OutputToFile(destinationPath, true, options => options
                .WithVideoCodec("libx264")
                .WithAudioCodec("aac"))
            .ProcessAsynchronously();
    }

    private async Task<bool> IsValidVideoFileAsync(string filePath)
    {
        try
        {
            IMediaAnalysis mediaInfo = await FFProbe.AnalyseAsync(filePath);
            return mediaInfo.Duration > TimeSpan.Zero && mediaInfo.VideoStreams.Any();
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Uploaded video failed validation: {VideoPath}", filePath);
            return false;
        }
    }

    public async Task<bool> SaveBackgroundVideoAsync(Stream stream, string fileName)
    {
        try
        {
            string filePath = Path.Combine(this.GetBackgroundVideosFolderPath(), fileName);
            await using (FileStream fs = new(filePath, FileMode.Create))
            {
                await stream.CopyToAsync(fs);
            }
            this.logger.LogInformation("Saved background video to {Path}", filePath);
            
            // Generate thumbnail
            await this.GenerateVideoThumbnail(filePath);
            
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error saving background video {FileName}.", fileName);
            return false;
        }
    }

    private async Task GenerateVideoThumbnail(string videoPath)
    {
        try
        {
            string thumbnailPath = Path.ChangeExtension(videoPath, ".jpg");
            // Capture frame at 1 second mark
            await FFMpeg.SnapshotAsync(videoPath, thumbnailPath, new System.Drawing.Size(320, 180), TimeSpan.FromSeconds(1));
            this.logger.LogInformation("Generated thumbnail for {VideoPath}", videoPath);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to generate thumbnail for {VideoPath}. The media may be invalid or FFmpeg/FFprobe may be unavailable.", videoPath);
        }
    }

    public List<string> ListBackgroundVideos()
    {
        string path = this.GetBackgroundVideosFolderPath();
        if (!Directory.Exists(path)) return [];

        return Directory.GetFiles(path)
            .Where(f => !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList()!;
    }

    public bool DeleteBackgroundVideo(string fileName)
    {
        try
        {
            string folder = this.GetBackgroundVideosFolderPath();
            string filePath = Path.Combine(folder, fileName);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                
                // Also delete thumbnail if it exists
                string thumbPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + ".jpg");
                if (File.Exists(thumbPath)) File.Delete(thumbPath);
                
                this.logger.LogInformation("Deleted background video: {FileName}", fileName);
                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error deleting background video {FileName}.", fileName);
            return false;
        }
    }

    public bool WelcomeVideoExists() => File.Exists(this.GetWelcomeVideoFilePath());

    #region YouTube Downloads

    public async Task<(bool Success, string Message)> DownloadYouTubeAsync(string url, bool audioOnly, IProgress<double>? progress = null)
    {
        try
        {
            Video video = await this.youtubeClient.Videos.GetAsync(url);
            string sanitizedTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));
            
            StreamManifest streamManifest = await this.youtubeClient.Videos.Streams.GetManifestAsync(video.Id);
            
            IStreamInfo streamInfo;

            if (audioOnly)
            {
                streamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                if (streamInfo == null) return (false, "No suitable audio stream found.");
            }
            else
            {
                streamInfo = streamManifest.GetMuxedStreams().GetWithHighestVideoQuality();
                if (streamInfo == null) return (false, "No suitable muxed video stream found.");
            }

            string extension = audioOnly ? "mp3" : streamInfo.Container.Name;
            string fileName = $"{sanitizedTitle}.{extension}";
            string filePath = Path.Combine(this.GetYouTubeFolderPath(), fileName);

            if (audioOnly)
            {
                // Download audio stream to temporary file and then convert to mp3
                string tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.{streamInfo.Container.Name}");
                try
                {
                    this.logger.LogInformation("Downloading temporary audio stream to {Path}", tempFilePath);
                    await this.youtubeClient.Videos.Streams.DownloadAsync(streamInfo, tempFilePath, progress);
                    
                    this.logger.LogInformation("Converting {TempPath} to {DestPath} (MP3)", tempFilePath, filePath);
                    await FFMpegArguments
                        .FromFileInput(tempFilePath)
                        .OutputToFile(filePath, true, options => options
                            .WithAudioCodec("libmp3lame")
                            .WithAudioBitrate(192))
                        .ProcessAsynchronously();
                    
                    this.logger.LogInformation("Conversion successful.");
                }
                catch (Exception ex)
                {
                    this.logger.LogError(ex, "FFmpeg conversion failed.");

                    if (ex is FileNotFoundException || ex.Message.Contains("ffmpeg", StringComparison.OrdinalIgnoreCase))
                    {
                        return (false, "Conversion failed: FFmpeg was not found. Install FFmpeg and set ProPresenter:FfmpegPath in appsettings.");
                    }

                    return (false, $"Conversion failed: {ex.Message}");
                }
                finally
                {
                    if (File.Exists(tempFilePath)) File.Delete(tempFilePath);
                }
            }
            else
            {
                await this.youtubeClient.Videos.Streams.DownloadAsync(streamInfo, filePath, progress);
            }

            // Download thumbnail
            try
            {
                string thumbUrl = video.Thumbnails.GetWithHighestResolution().Url;
                string thumbPath = Path.Combine(this.GetYouTubeFolderPath(), $"{sanitizedTitle}.jpg");
                using HttpClient client = new();
                byte[] thumbData = await client.GetByteArrayAsync(thumbUrl);
                await File.WriteAllBytesAsync(thumbPath, thumbData);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to download thumbnail for {Title}", video.Title);
            }

            this.logger.LogInformation("Downloaded YouTube {Type}: {Title}", audioOnly ? "Audio" : "Video", video.Title);
            return (true, fileName);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error downloading YouTube content from {Url}", url);
            return (false, ex.Message);
        }
    }

    public async Task EnsureThumbnailsAsync(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        string[] files = Directory.GetFiles(folderPath);
        foreach (string file in files)
        {
            string ext = Path.GetExtension(file).ToLower();
            if (ext == ".mp4" || ext == ".webm" || ext == ".m4v" || ext == ".mov")
            {
                string thumbPath = Path.ChangeExtension(file, ".jpg");
                if (!File.Exists(thumbPath))
                {
                    this.logger.LogInformation("Missing thumbnail detected for {File}, generating...", Path.GetFileName(file));
                    await this.GenerateVideoThumbnail(file);
                }
            }
        }
    }

    public List<string> ListYouTubeDownloads()
    {
        string path = this.GetYouTubeFolderPath();
        if (!Directory.Exists(path)) return [];

        return Directory.GetFiles(path)
            .Where(f => !f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName)
            .Where(f => !string.IsNullOrEmpty(f))
            .ToList()!;
    }

    public bool DeleteYouTubeDownload(string fileName)
    {
        try
        {
            string folder = this.GetYouTubeFolderPath();
            string filePath = Path.Combine(folder, fileName);
            if (!File.Exists(filePath))
            {
                return false;
            }

            File.Delete(filePath);
                
            // Also delete thumbnail
            string thumbPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(fileName) + ".jpg");
            if (File.Exists(thumbPath)) File.Delete(thumbPath);

            this.logger.LogInformation("Deleted YouTube download: {FileName}", fileName);
            return true;
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Error deleting YouTube download {FileName}.", fileName);
            return false;
        }
    }

    public bool BackgroundThumbnailExists(string fileName)
    {
        string thumbName = Path.GetFileNameWithoutExtension(fileName) + ".jpg";
        return File.Exists(Path.Combine(this.GetBackgroundVideosFolderPath(), thumbName));
    }

    public bool YouTubeDownloadExists(string fileName)
    {
        return File.Exists(Path.Combine(this.GetYouTubeFolderPath(), fileName));
    }

    #endregion
}
