using System.Collections.Generic;
using System.ComponentModel;

namespace Steam_Switcher.Models
{
    public class AccountsModel : INotifyPropertyChanged
    {
        private string login;
        public string Login
        {
            get { return login; }
            set { SetField(ref login, value, "Login"); }
        }

        private string pwd;
        public string Password
        {
            get { return pwd; }
            set { SetField(ref pwd, value, "Password"); }
        }

        private string steamid;
        public string SteamID
        {
            get { return steamid; }
            set { SetField(ref steamid, value, "SteamID"); }
        }

        private string sharedsecret;
        public string Shared_Secret
        {
            get { return sharedsecret; }
            set { SetField(ref sharedsecret, value, "Shared_Secret"); }
        }

        private string profilelink;
        public string ProfileLink
        {
            get { return profilelink; }
            set { SetField(ref profilelink, value, "ProfileLink"); }
        }

        private string tradelink;
        public string TradeLink
        {
            get { return tradelink; }
            set { SetField(ref tradelink, value, "TradeLink"); }
        }

        private string mail;
        public string Mail
        {
            get { return mail; }
            set { SetField(ref mail, value, "Mail"); }
        }

        private string mailpassword;
        public string MailPassword
        {
            get { return mailpassword; }
            set { SetField(ref mailpassword, value, "MailPassword"); }
        }

        private string note;
        public string Note
        {
            get { return note; }
            set { SetField(ref note, value, "Note"); }
        }

        private ushort gamescount;
        public ushort GamesCount
        {
            get { return gamescount; }
            set { SetField(ref gamescount, value, "GamesCount"); }
        }

        private ushort level;
        public ushort Level
        {
            get { return level; }
            set { SetField(ref level, value, "Level"); }
        }

        private string avatar;
        public string AvatarFilePath
        {
            get { return avatar; }
            set { SetField(ref avatar, value, "AvatarFilePath"); }
        }

        private string nickname;
        public string Nickname
        {
            get { return nickname; }
            set { SetField(ref nickname, value, "Nickname"); }
        }

        private int personastate;
        public int PersonaState
        {
            get { return personastate; }
            set { SetField(ref personastate, value, "PersonaState"); }
        }

        private int privacystatus;
        public int PrivacyStatus
        {
            get { return privacystatus; }
            set { SetField(ref privacystatus, value, "PrivacyStatus"); }
        }

        private bool communitybanned;
        public bool CommunityBanned
        {
            get { return communitybanned; }
            set { SetField(ref communitybanned, value, "CommunityBanned"); }
        }

        private bool vacbanned;
        public bool VACBanned
        {
            get { return vacbanned; }
            set { SetField(ref vacbanned, value, "VACBanned"); }
        }

        private string economyban;
        public string EconomyBan
        {
            get { return economyban; }
            set { SetField(ref economyban, value, "EconomyBan"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, string propertyName)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
