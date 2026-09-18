using System;
public struct ConnectionLogEntry
{
	public ConnectionLogEntry(uint ip, ushort port, string nickname, string time)
	{
		this.Ip = ip;
		this.Port = port;
		this.Nickname = nickname;
		this.Time = time;
	}
	public uint Ip;
	public ushort Port;
	public string Nickname;
	public string Time;
}
