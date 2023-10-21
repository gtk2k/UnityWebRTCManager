namespace gtk2k.WebRTCSignaler
{
    public enum ProtocolType
    {
        UDP,
        WebSocket
    }

    public enum SignalerType
    {
        WebSocketClient,
        WebSocketServer,
        UDP
    }

    public enum Side
    {
        Local,
        Remote
    }
}