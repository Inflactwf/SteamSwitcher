namespace Steam_Switcher.Models
{
    class GetAccountLevelModel
    {
        public Response response { get; set; }

        public class Response
        {
            public ushort player_level { get; set; }
        }
    }
}
