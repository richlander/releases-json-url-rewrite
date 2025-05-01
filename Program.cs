using System.Text.Json;
using System.Text.Json.Nodes;

class Program
{
    private static readonly HttpClient s_httpClient = new HttpClient();

    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: no path provided.");
            return;
        }
        if (!Directory.Exists(args[0]))
        {
            Console.WriteLine("Error: directory does not exist.");
            return;
        }
        string releasesPath = args[0];
        string inputFilePath = Path.Combine(releasesPath, "releases.json");
        string outputFilePath = Path.Combine(releasesPath, "releases2.json");

        var json = JsonNode.Parse(File.ReadAllText(inputFilePath)) ?? throw new Exception("Failed to parse JSON");

        var releases = json["releases"];
        if (releases != null)
        {
            foreach (var release in releases.AsArray())
            {
                if (release == null) throw new Exception("Release is null");

                var runtime = release["runtime"];
                if (runtime != null)
                {
                    ProcessFiles(runtime, "Runtime");
                }

                var sdk = release["sdk"];
                if (sdk != null)
                {
                    ProcessFiles(sdk, "Sdk");
                }

                var sdks = release["sdks"]?.AsArray();
                if (sdks != null)
                {
                    foreach (var innerSdk in sdks)
                    {
                        if (innerSdk != null)
                        {
                            ProcessFiles(innerSdk, "Sdk");
                        }
                    }
                }

                var aspnetcore = release["aspnetcore-runtime"];
                if (aspnetcore != null)
                {
                    ProcessFiles(aspnetcore, "aspnetcore/Runtime");
                }

                var WindowsDesktop = release["windowsdesktop"];
                if (WindowsDesktop != null)
                {
                    ProcessFiles(WindowsDesktop, "WindowsDesktop");
                }
            }
        }

        var newJson = $"{json.ToJsonString(new JsonSerializerOptions { WriteIndented = true })}\n";
        File.WriteAllText(outputFilePath, newJson);
    }
    static void ProcessFiles(JsonNode componentNode, string component)
    {
        if (componentNode == null) return;
        var files = componentNode["files"];
        if (files == null) return;
        string version = componentNode["version"]?.ToString() ?? "";

        foreach (var file in files.AsArray())
        {
            if (file == null) continue;
            string url = file["url"]?.ToString() ?? "";
            if (url == "" || !url.StartsWith("https://download.visualstudio")) continue;
            string fileName = Path.GetFileName(url);

            string newUrl = $"https://builds.dotnet.microsoft.com/dotnet/{component}/{version}/{fileName}";
            var requestMessage = new HttpRequestMessage(HttpMethod.Head, newUrl);
            using var response = s_httpClient.Send(requestMessage, HttpCompletionOption.ResponseHeadersRead);
            int code = (int)response.StatusCode;
            Console.WriteLine($"{newUrl} {code}");
            if (code == 200)
            {
                file["url"] = newUrl;
            }
        }
    }
}
