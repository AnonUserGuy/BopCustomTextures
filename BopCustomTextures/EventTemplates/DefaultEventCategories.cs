using System.Collections.Generic;

namespace BopCustomTextures.EventTemplates;
public class DefaultEventCategories
{
    // There's a bepinex way to get these from the game directly, but it requires a preloader .dll file (ew)
    public static readonly HashSet<string> DefaultCategories = [
        "_",
        "gameManager",
        "effects",
        "accessibility",
        "debug",
        "flipperSnapper",
        "flipperSnapperJungle",
        "sweetTooth",
        "sweetToothJungle",
        "rockPaperShowdown",
        "rockPaperShowdownJungle",
        "pantryParade",
        "pantryParadeJungle",
        "bBot",
        "bBotSky",
        "flowWorms",
        "flowWormsSky",
        "meetAndTweet",
        "meetAndTweetSky",
        "steadyBears",
        "steadyBearsSky",
        "popUpKitchen",
        "popUpKitchenOcean",
        "fireworkFestival",
        "fireworkFestivalOcean",
        "hammerTime",
        "hammerTimeOcean",
        "molecano",
        "molecanoOcean",
        "presidentBird",
        "presidentBirdFire",
        "snakedown",
        "snakedownFire",
        "octeaparty",
        "octeapartyFire",
        "globeTrotters",
        "globeTrottersFire",
    ];

    public static List<string> ModdedCategories
    {
        get => MixtapeEventTemplates.Categories.FindAll(category => !DefaultCategories.Contains(category));
    }
}
