namespace PepeStealBot.Modules;

public class CommandModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ILogger<CommandModule> _logger;
    private readonly HttpClient _httpClient;

    public CommandModule(ILogger<CommandModule> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
    }

    [SlashCommand("test", "Just a test command")]
    public async Task TestCommand()
        => await RespondAsync("Hello There");

    [DefaultMemberPermissions(GuildPermission.Administrator)]
    [SlashCommand("steal-pepe", "Steal pepe emojis muahahahahahahaah")]
    public async Task StealPepesAsync([Summary("pepes", "Space separated pepe emojis"), MaxLength(6000)] string pepeString)
    {
        var path = "pepes";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        if (pepeString.Length == 0)
        {
            await RespondAsync("No pepes 😢");
            return;
        }

        await DeferAsync();

        var pepes = pepeString.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        var emotesDetected = pepes.Length;
        var stolen = 0;
        var duplicates = 0;
        var failed = 0;

        foreach (var pepe in pepes)
        {
            if (Emote.TryParse(pepe, out var pepeEmote))
            {
                try
                {
                    var filename = $"{pepeEmote.Id}.{(pepeEmote.Animated ? "gif" : "webp")}";

                    if (File.Exists(Path.Combine(path, filename)))
                    {
                        duplicates++;
                        continue;
                    }

                    await using (var pepeFileStream = File.OpenWrite(Path.Combine(path, filename)))
                    {
                        var result = await _httpClient.GetAsync(pepeEmote.Url);
                        await result.Content.CopyToAsync(pepeFileStream);
                    }

                    stolen++;
                }
                catch
                {
                    failed++;
                }
            }
            else
            {
                failed++;
            }
        }

        await FollowupAsync(embed: new EmbedBuilder()
            .WithColor(0xff00)
            .WithTitle("Stolen pepes")
            .AddField("Total pepes detected", $"`{emotesDetected}`", true)
            .AddField("Successfully stolen", $"`{stolen}`", true)
            .AddField("Failed", $"`{failed}`", true)
            .AddField("Duplicates", $"`{duplicates}`", true)
            .Build());

    }

}