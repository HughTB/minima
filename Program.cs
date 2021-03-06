using Newtonsoft.Json;
using System.Web;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.KnownProxies.Add(IPAddress.Parse("127.0.0.1"));
});

var app = builder.Build();

List<char> b64Alphabet = new List<char> { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-', '_' };
Dictionary<string, string> urlDict = new Dictionary<string, string>();
string domain = @"https://minima.tuberculosis.dev/";

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.MapGet("/", () => Results.Redirect("https://tuberculosis.dev/minima.html"));

app.MapGet("/{id}", (string id) =>
{
    try
    {
        if (urlDict.ContainsKey(id))
        {
            return Results.Redirect(HttpUtility.UrlDecode(urlDict[id]));
        }
        else
        {
            return Results.NotFound($"{id} was not found in the database");
        }
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
        string hash = Encode64(HashString(url)).Substring(0, 8);

        if (urlDict.ContainsValue(url) && urlDict[hash] == url)
        {
            return Results.Created($"/{hash}", $"{domain}{hash}");
        }
        else
        {
            string? ignore;

            while (urlDict.TryGetValue(hash, out ignore))
            {
                hash = Encode64(HashString(url, hash)).Substring(0, 8);
            }

            urlDict.Add(hash, url);
            Console.WriteLine($"{hash} relates to {url}");
            SaveDict(urlDict, "dict.json");
            return Results.Created($"/{hash}", $"{domain}{hash}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine(ex.Message);
        return Results.BadRequest();
    }
});

LoadDict(out urlDict, "dict.json");
app.Run();

string Encode64(string input)
{
    return System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input)).Replace('+', '-').Replace('/', '_');
}

string Decode64(string input)
{
    return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(input.Replace('-', '+').Replace('_', '/')));
}

string HashString(string input, string salt = "")
{
    if (string.IsNullOrEmpty(input)) { return String.Empty; }

    using (var sha = System.Security.Cryptography.SHA1.Create())
    {
        byte[] textBytes = System.Text.Encoding.UTF8.GetBytes(input + salt);
        byte[] hashBytes = sha.ComputeHash(textBytes);

        return BitConverter.ToString(hashBytes).Replace("-", String.Empty);
    }
}

bool LoadDict(out Dictionary<string, string>? dict, string filepath)
{
    bool success = false;
    Dictionary<string, string>? loadedDict = new Dictionary<string, string>();
    Console.WriteLine("Loading dictionary...");

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