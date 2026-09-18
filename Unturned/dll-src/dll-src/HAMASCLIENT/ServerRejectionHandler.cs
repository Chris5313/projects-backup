using System;
using System.Reflection;
using SDG.NetPak;
using SDG.Unturned;
internal class ServerRejectionHandler
{
	[HookMethodAttribute("ClientMessageHandler_Rejected", "ReadMessage", new Type[] { })]
	internal static void OnRejectedMessage(NetPakReader reader)
	{
		ServerRejectionHandler.IsWaitingForConnectResponseField.Set(false);
		ESteamRejection esteamRejection = ESteamRejection.SERVER_FULL;
		reader.ReadEnum(out esteamRejection);
		string text = null;
		reader.ReadString(out text, 11);
		Logger.LogClient("Rejected from the server");
		Logger.LogClient("Steam rejection type: " + esteamRejection.ToString());
		Logger.LogClient("Reason: " + text);
		ServerRejectionHandler.ConnectionFailureReasonField.value = string.Empty;
		bool flag = esteamRejection == ESteamRejection.WHITELISTED;
		bool flag2 = flag;
		if (flag2)
		{
			Provider._connectionFailureInfo = ESteamConnectionFailureInfo.WHITELISTED;
		}
		else
		{
			bool flag3 = esteamRejection == ESteamRejection.WRONG_PASSWORD;
			bool flag4 = flag3;
			if (flag4)
			{
				Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PASSWORD;
			}
			else
			{
				bool flag5 = esteamRejection == ESteamRejection.SERVER_FULL;
				bool flag6 = flag5;
				if (flag6)
				{
					Provider._connectionFailureInfo = ESteamConnectionFailureInfo.FULL;
				}
				else
				{
					bool flag7 = esteamRejection == ESteamRejection.WRONG_HASH_LEVEL;
					bool flag8 = flag7;
					if (flag8)
					{
						Provider._connectionFailureInfo = ESteamConnectionFailureInfo.HASH_LEVEL;
					}
					else
					{
						bool flag9 = esteamRejection == ESteamRejection.WRONG_HASH_ASSEMBLY;
						bool flag10 = flag9;
						if (flag10)
						{
							Provider._connectionFailureInfo = ESteamConnectionFailureInfo.HASH_ASSEMBLY;
						}
						else
						{
							bool flag11 = esteamRejection == ESteamRejection.WRONG_VERSION;
							bool flag12 = flag11;
							if (flag12)
							{
								Provider._connectionFailureInfo = ESteamConnectionFailureInfo.VERSION;
								ServerRejectionHandler.ConnectionFailureReasonField.value = text;
							}
							else
							{
								bool flag13 = esteamRejection == ESteamRejection.PRO_SERVER;
								bool flag14 = flag13;
								if (flag14)
								{
									Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PRO_SERVER;
								}
								else
								{
									bool flag15 = esteamRejection == ESteamRejection.PRO_CHARACTER;
									bool flag16 = flag15;
									if (flag16)
									{
										Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PRO_CHARACTER;
									}
									else
									{
										bool flag17 = esteamRejection == ESteamRejection.PRO_DESYNC;
										bool flag18 = flag17;
										if (flag18)
										{
											Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PRO_DESYNC;
										}
										else
										{
											bool flag19 = esteamRejection == ESteamRejection.PRO_APPEARANCE;
											bool flag20 = flag19;
											if (flag20)
											{
												Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PRO_APPEARANCE;
											}
											else
											{
												bool flag21 = esteamRejection == ESteamRejection.AUTH_VERIFICATION;
												bool flag22 = flag21;
												if (flag22)
												{
													Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_VERIFICATION;
												}
												else
												{
													bool flag23 = esteamRejection == ESteamRejection.AUTH_NO_STEAM;
													bool flag24 = flag23;
													if (flag24)
													{
														Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_NO_STEAM;
													}
													else
													{
														bool flag25 = esteamRejection == ESteamRejection.AUTH_LICENSE_EXPIRED;
														bool flag26 = flag25;
														if (flag26)
														{
															Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_LICENSE_EXPIRED;
														}
														else
														{
															bool flag27 = esteamRejection == ESteamRejection.AUTH_VAC_BAN;
															bool flag28 = flag27;
															if (flag28)
															{
																Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_VAC_BAN;
															}
															else
															{
																bool flag29 = esteamRejection == ESteamRejection.AUTH_ELSEWHERE;
																bool flag30 = flag29;
																if (flag30)
																{
																	Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_ELSEWHERE;
																}
																else
																{
																	bool flag31 = esteamRejection == ESteamRejection.AUTH_TIMED_OUT;
																	bool flag32 = flag31;
																	if (flag32)
																	{
																		Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_TIMED_OUT;
																	}
																	else
																	{
																		bool flag33 = esteamRejection == ESteamRejection.AUTH_USED;
																		bool flag34 = flag33;
																		if (flag34)
																		{
																			Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_USED;
																		}
																		else
																		{
																			bool flag35 = esteamRejection == ESteamRejection.AUTH_NO_USER;
																			bool flag36 = flag35;
																			if (flag36)
																			{
																				Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_NO_USER;
																			}
																			else
																			{
																				bool flag37 = esteamRejection == ESteamRejection.AUTH_PUB_BAN;
																				bool flag38 = flag37;
																				if (flag38)
																				{
																					Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_PUB_BAN;
																				}
																				else
																				{
																					bool flag39 = esteamRejection == ESteamRejection.AUTH_NETWORK_IDENTITY_FAILURE;
																					bool flag40 = flag39;
																					if (flag40)
																					{
																						Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_NETWORK_IDENTITY_FAILURE;
																					}
																					else
																					{
																						bool flag41 = esteamRejection == ESteamRejection.AUTH_ECON_DESERIALIZE;
																						bool flag42 = flag41;
																						if (flag42)
																						{
																							Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_ECON_DESERIALIZE;
																						}
																						else
																						{
																							bool flag43 = esteamRejection == ESteamRejection.AUTH_ECON_VERIFY;
																							bool flag44 = flag43;
																							if (flag44)
																							{
																								Provider._connectionFailureInfo = ESteamConnectionFailureInfo.AUTH_ECON_VERIFY;
																							}
																							else
																							{
																								bool flag45 = esteamRejection == ESteamRejection.ALREADY_CONNECTED;
																								bool flag46 = flag45;
																								if (flag46)
																								{
																									Provider._connectionFailureInfo = ESteamConnectionFailureInfo.ALREADY_CONNECTED;
																								}
																								else
																								{
																									bool flag47 = esteamRejection == ESteamRejection.ALREADY_PENDING;
																									bool flag48 = flag47;
																									if (flag48)
																									{
																										Provider._connectionFailureInfo = ESteamConnectionFailureInfo.ALREADY_PENDING;
																									}
																									else
																									{
																										bool flag49 = esteamRejection == ESteamRejection.LATE_PENDING;
																										bool flag50 = flag49;
																										if (flag50)
																										{
																											Provider._connectionFailureInfo = ESteamConnectionFailureInfo.LATE_PENDING;
																										}
																										else
																										{
																											bool flag51 = esteamRejection == ESteamRejection.NOT_PENDING;
																											bool flag52 = flag51;
																											if (flag52)
																											{
																												Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NOT_PENDING;
																											}
																											else
																											{
																												bool flag53 = esteamRejection == ESteamRejection.NAME_PLAYER_SHORT;
																												bool flag54 = flag53;
																												if (flag54)
																												{
																													Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PLAYER_SHORT;
																												}
																												else
																												{
																													bool flag55 = esteamRejection == ESteamRejection.NAME_PLAYER_LONG;
																													bool flag56 = flag55;
																													if (flag56)
																													{
																														Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PLAYER_LONG;
																													}
																													else
																													{
																														bool flag57 = esteamRejection == ESteamRejection.NAME_PLAYER_INVALID;
																														bool flag58 = flag57;
																														if (flag58)
																														{
																															Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PLAYER_INVALID;
																														}
																														else
																														{
																															bool flag59 = esteamRejection == ESteamRejection.NAME_PLAYER_NUMBER;
																															bool flag60 = flag59;
																															if (flag60)
																															{
																																Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PLAYER_NUMBER;
																															}
																															else
																															{
																																bool flag61 = esteamRejection == ESteamRejection.NAME_CHARACTER_SHORT;
																																bool flag62 = flag61;
																																if (flag62)
																																{
																																	Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_CHARACTER_SHORT;
																																}
																																else
																																{
																																	bool flag63 = esteamRejection == ESteamRejection.NAME_CHARACTER_LONG;
																																	bool flag64 = flag63;
																																	if (flag64)
																																	{
																																		Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_CHARACTER_LONG;
																																	}
																																	else
																																	{
																																		bool flag65 = esteamRejection == ESteamRejection.NAME_CHARACTER_INVALID;
																																		bool flag66 = flag65;
																																		if (flag66)
																																		{
																																			Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_CHARACTER_INVALID;
																																		}
																																		else
																																		{
																																			bool flag67 = esteamRejection == ESteamRejection.NAME_CHARACTER_NUMBER;
																																			bool flag68 = flag67;
																																			if (flag68)
																																			{
																																				Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_CHARACTER_NUMBER;
																																			}
																																			else
																																			{
																																				bool flag69 = esteamRejection == ESteamRejection.PING;
																																				bool flag70 = flag69;
																																				if (flag70)
																																				{
																																					Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PING;
																																					bool flag71 = MenuDashboardUI.localization.has("PingV2");
																																					bool flag72 = flag71;
																																					if (flag72)
																																					{
																																						int num = ((Provider.CurrentServerAdvertisement != null) ? Provider.CurrentServerAdvertisement.ping : (-1));
																																						ServerRejectionHandler.ConnectionFailureReasonField.value = MenuDashboardUI.localization.format("PingV2", num, text);
																																					}
																																					else
																																					{
																																						ServerRejectionHandler.ConnectionFailureReasonField.value = MenuDashboardUI.localization.format("Ping");
																																					}
																																				}
																																				else
																																				{
																																					bool flag73 = esteamRejection == ESteamRejection.PLUGIN;
																																					bool flag74 = flag73;
																																					if (flag74)
																																					{
																																						Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PLUGIN;
																																						ServerRejectionHandler.ConnectionFailureReasonField.value = text;
																																					}
																																					else
																																					{
																																						bool flag75 = esteamRejection == ESteamRejection.CLIENT_MODULE_DESYNC;
																																						bool flag76 = flag75;
																																						if (flag76)
																																						{
																																							Provider._connectionFailureInfo = ESteamConnectionFailureInfo.CLIENT_MODULE_DESYNC;
																																						}
																																						else
																																						{
																																							bool flag77 = esteamRejection == ESteamRejection.SERVER_MODULE_DESYNC;
																																							bool flag78 = flag77;
																																							if (flag78)
																																							{
																																								Provider._connectionFailureInfo = ESteamConnectionFailureInfo.SERVER_MODULE_DESYNC;
																																							}
																																							else
																																							{
																																								bool flag79 = esteamRejection == ESteamRejection.WRONG_LEVEL_VERSION;
																																								bool flag80 = flag79;
																																								if (flag80)
																																								{
																																									Provider._connectionFailureInfo = ESteamConnectionFailureInfo.LEVEL_VERSION;
																																									ServerRejectionHandler.ConnectionFailureReasonField.value = MenuDashboardUI.localization.format("Level_Version", text, Level.info.getLocalizedName(), Level.version);
																																								}
																																								else
																																								{
																																									bool flag81 = esteamRejection == ESteamRejection.WRONG_HASH_ECON;
																																									bool flag82 = flag81;
																																									if (flag82)
																																									{
																																										Provider._connectionFailureInfo = ESteamConnectionFailureInfo.ECON_HASH;
																																									}
																																									else
																																									{
																																										bool flag83 = esteamRejection == ESteamRejection.WRONG_HASH_MASTER_BUNDLE;
																																										bool flag84 = flag83;
																																										if (flag84)
																																										{
																																											Provider._connectionFailureInfo = ESteamConnectionFailureInfo.HASH_MASTER_BUNDLE;
																																											ServerRejectionHandler.ConnectionFailureReasonField.value = text;
																																										}
																																										else
																																										{
																																											bool flag85 = esteamRejection == ESteamRejection.LATE_PENDING_STEAM_AUTH;
																																											bool flag86 = flag85;
																																											if (flag86)
																																											{
																																												Provider._connectionFailureInfo = ESteamConnectionFailureInfo.LATE_PENDING_STEAM_AUTH;
																																											}
																																											else
																																											{
																																												bool flag87 = esteamRejection == ESteamRejection.LATE_PENDING_STEAM_ECON;
																																												bool flag88 = flag87;
																																												if (flag88)
																																												{
																																													Provider._connectionFailureInfo = ESteamConnectionFailureInfo.LATE_PENDING_STEAM_ECON;
																																												}
																																												else
																																												{
																																													bool flag89 = esteamRejection == ESteamRejection.LATE_PENDING_STEAM_GROUPS;
																																													bool flag90 = flag89;
																																													if (flag90)
																																													{
																																														Provider._connectionFailureInfo = ESteamConnectionFailureInfo.LATE_PENDING_STEAM_GROUPS;
																																													}
																																													else
																																													{
																																														bool flag91 = esteamRejection == ESteamRejection.NAME_PRIVATE_LONG;
																																														bool flag92 = flag91;
																																														if (flag92)
																																														{
																																															Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PRIVATE_LONG;
																																														}
																																														else
																																														{
																																															bool flag93 = esteamRejection == ESteamRejection.NAME_PRIVATE_INVALID;
																																															bool flag94 = flag93;
																																															if (flag94)
																																															{
																																																Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PRIVATE_INVALID;
																																															}
																																															else
																																															{
																																																bool flag95 = esteamRejection == ESteamRejection.NAME_PRIVATE_NUMBER;
																																																bool flag96 = flag95;
																																																if (flag96)
																																																{
																																																	Provider._connectionFailureInfo = ESteamConnectionFailureInfo.NAME_PRIVATE_NUMBER;
																																																}
																																																else
																																																{
																																																	bool flag97 = esteamRejection == ESteamRejection.WRONG_HASH_RESOURCES;
																																																	bool flag98 = flag97;
																																																	if (flag98)
																																																	{
																																																		Provider._connectionFailureInfo = ESteamConnectionFailureInfo.HASH_RESOURCES;
																																																	}
																																																	else
																																																	{
																																																		Provider._connectionFailureInfo = ESteamConnectionFailureInfo.REJECT_UNKNOWN;
																																																		ServerRejectionHandler.ConnectionFailureReasonField.value = esteamRejection.ToString();
																																																	}
																																																}
																																															}
																																														}
																																													}
																																												}
																																											}
																																										}
																																									}
																																								}
																																							}
																																						}
																																					}
																																				}
																																			}
																																		}
																																	}
																																}
																															}
																														}
																													}
																												}
																											}
																										}
																									}
																								}
																							}
																						}
																					}
																				}
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
		}
		Provider.RequestDisconnect(string.Format("Rejected by server ({0}) --- Reason: \"{1}\" Explanation: \"{2}\"", esteamRejection, Provider.connectionFailureReason, text));
	}
	public static ReflectedField<string> ConnectionFailureReasonField = new ReflectedField<string>(typeof(Provider), "_connectionFailureReason", BindingFlags.Static | BindingFlags.NonPublic);
	public static ReflectedField<bool> IsWaitingForConnectResponseField = new ReflectedField<bool>(typeof(Provider), "isWaitingForConnectResponse", BindingFlags.Static | BindingFlags.NonPublic);
}
