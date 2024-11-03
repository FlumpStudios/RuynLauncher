
namespace RuynLancher
{
    public static class Constants
    {        
        public const string SERVER_LOCATION = "https://levelserver.ruyn.co.uk";
        public const string K = "MtiDqKdlsswFXVI5XrQNFZZu2Me97EVr";
        public const string EDITOR_NAME = "NekoNeo";
        public const string EXE_NAME = "Ruyn";
        public const string SETTINGS_SAVE_FILE_NAME = "launcher_Settings.dat";
#if DEBUG
        public const string LEVELS_FOLDER = @"c:\projects\NekoEngine\Levels";
        public const string GAME_FILE_LOCATION = @"c:\projects\NekoEngine";

#else
        public const string LEVELS_FOLDER = @"\Levels";
        public const string GAME_FILE_LOCATION = @".\";
#endif
    }
}
