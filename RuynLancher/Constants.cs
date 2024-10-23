using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RuynLancher
{
    public static class Constants
    {
        public const string EDITOR_NAME = "NekoNeo";
        public const string EXE_NAME = "Ruyn";
#if DEBUG
        public const string LEVELS_FOLDER = @"c:\projects\NekoEngine\Levels";
        public const string GAME_FILE_LOCATION = @"c:\projects\NekoEngine";

#else
        public const string LEVELS_FOLDER = @"\Levels";
        public const string GAME_FILE_LOCATION = @".\";
#endif
    }
}
