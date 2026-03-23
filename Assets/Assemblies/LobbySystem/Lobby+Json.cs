using Newtonsoft.Json;

namespace Resonance.Assemblies.LobbySystem
{
    public partial struct Lobby
    {
        public string ToJson() => JsonConvert.SerializeObject(this);
        public static Lobby FromJson(string json) => JsonConvert.DeserializeObject<Lobby>(json);
    }
}
