using Newtonsoft.Json;
using System.Web;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

var random = new Random();
var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

var app = builder.Build();

List<char> b64Alphabet = new List<char> { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_' };
Dictionary<string, string> ?urlDict = new Dictionary<string, string>();
string domain = @"https://minima.tuberculosis.dev/";

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapGet("/", () => "Hello dere");

app.MapGet("/{id}", (string id) =>
{
    try
    {
        return urlDict.ContainsKey(id) ? Results.Redirect(HttpUtility.UrlDecode(urlDict[id])) : Results.NotFound($"{id} was not found in the database");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.BadRequest();
    }
});

app.MapPost("/{url}", (string url) =>
{
    try
    {
        string hash;
        if (urlDict.ContainsValue(url))
        {
            hash = urlDict.FirstOrDefault(x => x.Value == url).Key;
            return Results.Created($"/{hash}", $"{domain}{hash}");
        }

        hash = newHash(8);

        urlDict.Add(hash, url);
        Console.WriteLine($"{hash} relates to {url}");
        SaveDict(urlDict, "dict.json");
        return Results.Created($"/{hash}", $"{domain}{hash}");
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.BadRequest();
    }
});

LoadDict(out urlDict, "dict.json");
app.Run();

string newHash(int length)
{
    string hash = "";
    bool used = true;

    while (used) 
    {
        for (int i = 0; i < length; i++)
        {
            hash += b64Alphabet[random.Next(b64Alphabet.Count)];
        }

        if (!urlDict.ContainsKey(hash)) { used = false; }
    }

    return hash;
}

bool LoadDict(out Dictionary<string, string> ?dict, string filepath)
{
    bool success = false;
    Dictionary<string, string> ?loadedDict = new Dictionary<string, string>();
    Console.WriteLine("Loading dictionary...");

    if (!File.Exists(filepath))
    {
        dict = loadedDict;
        SaveDict(loadedDict, filepath);
        return true;
    }

    using (StreamReader sr = new StreamReader(filepath))
    {
        try
        {
            loadedDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(sr.ReadToEnd());
            success = true;
            Console.WriteLine("Load successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        } 
    }

    dict = loadedDict;
    return success;
}

bool SaveDict(Dictionary<string, string> dict, string filePath)
{
    bool success = false;
    Console.WriteLine("Saving dictionary...");

    using (StreamWriter sw = new StreamWriter(filePath))
    {
        try
        {
            sw.Write(JsonConvert.SerializeObject(dict, Formatting.Indented));
            success = true;
            Console.WriteLine("Save successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

    return success;
}