using Robust.Shared.Configuration;

namespace Content.Server._Jerry.CCCVars;
[CVarDefs]
public static class CCCVars
{
    /*
     *  Discord OAuth2
     */

    public static readonly CVarDef<string> DiscordApiUrl =
        CVarDef.Create("jerry.discord_api_url", "http://127.0.0.1:2424/api", CVar.CONFIDENTIAL | CVar.SERVERONLY);

    /*
     * Sponsors
     */

    public static readonly CVarDef<string> DiscordGuildID =
        CVarDef.Create("jerry.discord_guildId", "1126278315364339712", CVar.CONFIDENTIAL | CVar.SERVERONLY);
}
