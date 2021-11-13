using System.Collections.Generic;

namespace Steam_Switcher.Models
{
    class GetOwnedGamesModel
    {
        public Response response { get; set; }

        public class Game
        {
            public int appid { get; set; }
            public int playtime_forever { get; set; }
            public int playtime_windows_forever { get; set; }
            public int playtime_mac_forever { get; set; }
            public int playtime_linux_forever { get; set; }
            public int? playtime_2weeks { get; set; }
        }

        public class Response
        {
            public ushort game_count { get; set; }
            public List<Game> games { get; set; }
        }
    }
}
