using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Models.MediaStreams;

namespace DescargarVideosYoutube
{
    public class Tema
    {
        public string nombre { get; set; }
        public string link { get; set; }
        public string genero { get; set; }
    }
    class Program
    {
        /// <summary>
        /// Turns file size in bytes into human-readable string.
        /// </summary>
        private static string NormalizeFileSize(long fileSize)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };
            double size = fileSize;
            var unit = 0;

            while (size >= 1024)
            {
                size /= 1024;
                ++unit;
            }

            return $"{size:0.#} {units[unit]}";
        }

        /// <summary>
        /// If given a YouTube URL, parses video id from it.
        /// Otherwise returns the same string.
        /// </summary>
        private static string NormalizeVideoId(string input)
        {
            return YoutubeClient.TryParseVideoId(input, out var videoId)
                ? videoId
                : input;
        }

        public static async Task DescargarAsync(string nombre, string link, string rutaFinal, string rutaLog)
        {
            try
            {
                // Client
                var client = new YoutubeClient();

                // Get the video ID
                var videoId = link;//Console.ReadLine();
                videoId = NormalizeVideoId(videoId);
                Console.WriteLine();

                // Get the video info
                Console.Write("Obtaining general video info... ");
                var video = await client.GetVideoAsync(videoId);
                Console.WriteLine('✓');
                Console.WriteLine($"> {video.Title} by {video.Author}");
                Console.WriteLine();

                // Get media stream info set
                Console.Write("Obtaining media stream info set... ");
                var streamInfoSet = await client.GetVideoMediaStreamInfosAsync(videoId);
                Console.WriteLine('✓');
                Console.WriteLine("> " +
                                  $"{streamInfoSet.Muxed.Count} muxed streams, " +
                                  $"{streamInfoSet.Video.Count} video-only streams, " +
                                  $"{streamInfoSet.Audio.Count} audio-only streams");
                Console.WriteLine();

                // Get the best muxed stream
                var streamInfo = streamInfoSet.Muxed.WithHighestVideoQuality();
                Console.WriteLine("Selected muxed stream with highest video quality:");
                Console.WriteLine("> " +
                                  $"{streamInfo.VideoQualityLabel} video quality | " +
                                  $"{streamInfo.Container} format | " +
                                  $"{NormalizeFileSize(streamInfo.Size)}");
                Console.WriteLine();

                // Compose file name, based on metadata
                var fileExtension = streamInfo.Container.GetFileExtension();
                // var fileName = $"{video.Title}.{fileExtension}";
                var fileName = rutaFinal + $"{nombre}.{fileExtension}";

                // Replace illegal characters in file name
                //  fileName = fileName.Replace(Path.GetInvalidFileNameChars(), '_');

                // Download video
                Console.Write("Downloading... ");
                using (var progress = new ProgressBar())
                    await client.DownloadMediaStreamAsync(streamInfo, fileName, progress);
                Console.WriteLine();

                Console.WriteLine($"Video saved to '{fileName}'");

            }
            catch (Exception ex)
            {
             /*   using (System.IO.StreamWriter file = new System.IO.StreamWriter(rutaLog, true))
                {
                    file.WriteLine("Could not get video: " + nombre);
                }*/
                Console.Write("Could not get video: " + nombre);
            }

        }

        static void Main(string[] args)
        {
            string ruta = @"F:\RepertorioDogs\";
            string rutaLog = ruta + "log.txt";
            using (StreamReader r = new StreamReader(@"C:\Users\METRALLETA\Downloads\temas.json"))
            {
                string json = r.ReadToEnd();
                List<Tema> temas = JsonConvert.DeserializeObject<List<Tema>>(json);

                if (!Directory.Exists(ruta))
                    Directory.CreateDirectory(ruta);  
                
                if (File.Exists(rutaLog))
                    File.Delete(rutaLog);
                else
                    File.Create(rutaLog);

                foreach (Tema tema in temas)
                {
                    var carpeta = ruta + tema.genero + @"\";
                    if (!Directory.Exists(carpeta))
                        Directory.CreateDirectory(carpeta);

                    DescargarAsync(tema.nombre, tema.link, carpeta, rutaLog).GetAwaiter().GetResult();

                    // Descargar(tema.link, carpeta, tema.nombre);
                    //var youtube = VideoLibrary.YouTube.Default;
                    // var vid = youtube.GetVideo(tema.link);
                    // File.WriteAllBytes(carpeta + vid.FullName, vid.GetBytes());

                    // var inputFile = new MediaFile { Filename = carpeta + tema.nombre };
                    // var outputFile = new MediaFile { Filename = $"{carpeta + tema.nombre}.mp3" };

                    // using (var engine = new Engine())
                    // {
                    //     engine.GetMetadata(inputFile);
                    //     engine.Convert(inputFile, outputFile);
                    // }
                }
            }
        }
    }
}
