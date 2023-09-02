using ConsoleUtils;
using EvRepair.Properties;
using EvRepair;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System;
using System.IO.Compression;

namespace EvRepair
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("EV 录屏恢复小工具 (其实就是调用 ffmpeg 和 recover_mp4)");
            Console.WriteLine("  作者: SlimeNull (https://github.com/SlimeNull)");
            Console.WriteLine("  制作: 2023-7-21");
            Console.WriteLine();

            ConsoleInput.Key("按任意键继续...");
            string? tempPath = Path.GetTempPath();

            bool foolMode = ConsoleInput.YesOrNo("使用傻瓜操作模式吗? 傻瓜模式下, 你只需要拖文件就好了, 其他的程序自动完成.", true);

            if (!foolMode && ConsoleInput.YesOrNo("修复视频时, 需要生成一些临时的文件, 默认生成在用户临时目录, 要更改吗?", false))
                tempPath = ConsoleInput.DirectoryPath("输入一个目录: ");

            string? ffmpegPath = PathUtils.FindExecutableInPath("ffmpeg.exe");
            string? recoverMp4Path = PathUtils.FindExecutableInPath("recover_mp4.exe");

            bool extractFFmpeg = false;
            bool extractRecoverMp4 = false;

            if (ffmpegPath == null)
            {
                int choice = foolMode ? 0 : ConsoleInput.SelectIndex("在你的电脑上没找到 ffmpeg, 下面进行什么?", "使用本程序内嵌的 ffmpeg", "自己选择一个程序作为 ffmpeg 使用");
                if (choice == 0)
                    extractFFmpeg = true;

                ffmpegPath = choice switch
                {
                    0 => ExtractGzipFileAndGetFullPath(tempPath, "ffmpeg.exe", Resources.FFmpegGZ),
                    1 => ConsoleInput.FilePath("输入可用的 ffmpeg 可执行文件路径: "),
                    _ => throw new InvalidOperationException(),
                };
            }

            if (recoverMp4Path == null)
            {
                int choice = foolMode ? 0 : ConsoleInput.SelectIndex("在你的电脑上没找到 recover_mp4.exe, 下面进行什么?", "使用本程序内嵌的 recover_mp4", "自己选择一个程序作为 recover_mp4 使用");
                if (choice == 0)
                    extractRecoverMp4 = true;

                recoverMp4Path = choice switch
                {
                    0 => ExtractFileAndGetFullPath(tempPath, "recover_mp4.exe", Resources.RecoverMp4),
                    1 => ConsoleInput.FilePath("输入可用的 recover_mp4 可执行文件路径: "),
                    _ => throw new InvalidOperationException(),
                };
            }

            string brokenVideo = ConsoleInput.FilePath("输入已损坏的视频路径: ");
            float fps = ConsoleInput.Single("视频的帧率: ");

            Console.WriteLine("要修复视频, 还需要一个与损坏视频格式相同的正常视频, 它将用来做格式分析");
            string fineVideo = ConsoleInput.FilePath("输入正常视频的路径: ");


            Console.WriteLine("分析格式...");
            ExecuteAndRedirectOutput(recoverMp4Path, fineVideo, "--analyze");

            string videoHdr = "video.hdr";
            string audioHdr = "audio.hdr";

            if (!File.Exists(videoHdr) || !File.Exists(audioHdr))
            {
                Console.WriteLine("分析失败了");
                ConsoleInput.Key("按任意键退出...");
                Environment.Exit(-2);
            }

            Console.WriteLine("从损坏视频中分离音视频流...");

            string recoveredH264 = Path.Combine(tempPath, "recovered.h264");
            string recoveredAAC = Path.Combine(tempPath, "recovered.aac");

            ExecuteAndRedirectOutput(recoverMp4Path, brokenVideo, recoveredH264, recoveredAAC);

            if (!File.Exists(recoveredH264) || !File.Exists(recoveredAAC))
            {
                Console.WriteLine("分离失败了");
                ConsoleInput.Key("按任意键退出...");
                Environment.Exit(-2);
            }


            Console.WriteLine("将音视频流重新拼接为视频...");
            string recoveredVideo = PathUtils.ChangeFileNameWithoutExtension(brokenVideo, Path.GetFileNameWithoutExtension(brokenVideo) + "_recovered");

            ExecuteAndRedirectOutput(ffmpegPath, "-r", $"{fps}", "-i", recoveredH264, "-i", recoveredAAC, "-bsf:a", "aac_adtstoasc", "-c:v", "copy", "-c:a", "copy", recoveredVideo);

            if (!File.Exists(recoveredVideo))
            {
                Console.WriteLine("拼接失败了");
                ConsoleInput.Key("按任意键退出...");
                Environment.Exit(-2);
            }

            File.Delete(videoHdr);
            File.Delete(audioHdr);
            File.Delete(recoveredH264);
            File.Delete(recoveredAAC);

            if (extractFFmpeg)
                File.Delete(ffmpegPath);
            if (extractRecoverMp4)
                File.Delete(recoverMp4Path);

            Console.WriteLine($"恢复完成了, 已将恢复的视频保存到以下文件:");
            Console.WriteLine($"  {recoveredVideo}");

            ConsoleInput.Key("按任意键继续");
        }

        static void ExecuteAndRedirectOutput(string executable, params string[] args)
        {
            string arguments =
            string.Join(" ", args.Select(arg => arg.Contains(' ') ? $"\"{arg}\"" : arg));

            Process? p = Process.Start(
                new ProcessStartInfo()
                {
                    FileName = executable,
                    Arguments = arguments,

                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,

                    UseShellExecute = false
                });

            if (p == null)
                throw new InvalidOperationException();

            while (p.StandardOutput.ReadLine() is string line)
            {
                Console.WriteLine(line);
            }
        }

        static string ExtractFileAndGetFullPath(string folder, string filename, byte[] bytes)
        {
            string fullPath = Path.Combine(folder, filename);
            File.WriteAllBytes(fullPath, bytes);

            return fullPath;
        }

        static string ExtractGzipFileAndGetFullPath(string folder, string filename, byte[] bytes)
        {
            string fullPath = Path.Combine(folder, filename);
            using (FileStream fs = File.Create(fullPath))
            using (GZipStream gz = new GZipStream(new MemoryStream(bytes), CompressionMode.Decompress))
            {
                gz.CopyTo(fs);
            }

            return fullPath;
        }
    }
}