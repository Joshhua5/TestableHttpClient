namespace Codenizer.HttpClient.Testable.SlackServer.Handlers
{
    public class EmojiHandler
    {
        private readonly SlackState _state;

        public EmojiHandler(SlackState state)
        {
            _state = state;
        }

        public object List()
        {
            return new { ok = true, emoji = _state.CustomEmoji };
        }
    }
}
