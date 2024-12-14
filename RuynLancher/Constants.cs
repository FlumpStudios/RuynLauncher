
using System;
using System.IO;

namespace RuynLancher
{
    public static class Constants
    {
        public const int LEVEL_SIZE_FOR_VALIDATION = 4626;
        public const string SERVER_LOCATION = "https://levelserver.ruyn.co.uk";
        public const string K = "MtiDqKdlsswFXVI5XrQNFZZu2Me97EVr";
        public const string EDITOR_NAME = "NekoNeo";
        public const string TWO_D_EDITOR_NAME = "NekoBuilder";

        public const string EXE_NAME = "Ruyn";
        public const string SETTINGS_SAVE_FILE_NAME = "launcher_Settings.dat";
        public const string HAD_FILE_EXTENSION = ".HAD";
        public const string HAD_FILE_TYPE = "HADFile";
#if DEBUG
        public const string LEVELS_FOLDER = @"c:\projects\NekoEngine\GameData\Levels";
        public const string GAME_FILE_LOCATION = @"c:\projects\NekoEngine\GameData";

#else

        public const string LEVELS_FOLDER = "Levels";
        public static string GAME_FILE_LOCATION = "GameData";

static Constants()
        {
            string exeDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string gameDataPath = Path.Combine(exeDirectory, "GameData");
            GAME_FILE_LOCATION = gameDataPath;
            Environment.CurrentDirectory = Constants.GAME_FILE_LOCATION;
        }

#endif
    }
}
