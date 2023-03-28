namespace LobotJR.Shared.Chat
{

    /// <summary>
    /// The request object used to send a whisper.
    /// </summary>
    public class WhisperRequest
    {
        /// <summary>
        /// The content of the whisper to send.
        /// </summary>
        public string Message { get; set; }

        public WhisperRequest(string message)
        {
            Message = message;
        }
    }
}
