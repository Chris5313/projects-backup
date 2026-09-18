using System;
using SDG.NetTransport;
using SDG.Unturned;
using UnityEngine;
public class ChatSpammer : MonoBehaviour
{
	public static void SetSpamEnabled(bool state)
	{
		bool flag = state && ChatSpammer.Instance == null;
		bool flag2 = flag;
		if (flag2)
		{
			ChatSpammer.Instance = Bootstrapper.HostGameObject.AddComponent<ChatSpammer>();
		}
		else
		{
			bool flag3 = !state && ChatSpammer.Instance != null;
			bool flag4 = flag3;
			if (flag4)
			{
				UnityEngine.Object.Destroy(ChatSpammer.Instance);
			}
		}
	}
	private void Update()
	{
		this.SpamTimer += Time.deltaTime;
		bool flag = this.SpamTimer > MiscConfig.chatSpamDelay;
		bool flag2 = flag;
		if (flag2)
		{
			this.SpamTimer = 0f;
			ChatSpammer.SendChatRequest.Invoke(ENetReliability.Unreliable, (byte)MiscConfig.spamChatZone, MiscConfig.spamText);
		}
	}
	private static ChatSpammer Instance;
	private float SpamTimer = 0f;
	private static readonly ServerStaticMethod<byte, string> SendChatRequest = ServerStaticMethod<byte, string>.Get(new ServerStaticMethod<byte, string>.ReceiveDelegateWithContext(ChatManager.ReceiveChatRequest));
}
