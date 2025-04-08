using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace Vintagestory.Client.MaxObf;

public class SessionManager
{
	private const string PubKey = "<RSAKeyValue><Modulus>mRaP5hO0mWf6gIdPMFD0sg4KLhwsA08Tk2246fdwNk6G7cRk+BJYtTOwKO+plurICQMKF2ktDJWOkjz+Hq2BCjBDB/al7XNdnoOJ1w0BsgInEPOGz9nn8OM4GjQyNcuv0iY0XqwElgy5xCNjBRKJJuqQje/E5SIiHs2O78nJUsZWCv6xjaH+4N/3Kno+sQoBFpNqKmXsq1+2KGMu8t4x58LrojbXzxJUm3O3agK8MvDg/xTAmumd2PTjVJBnrlSBIPdsaQwzX1G9s29B7CzQC6T7TzQehA8hPmUSQLEnwBV6EaUXbcjOBh01i5k5MP6i22wrDCfQMnnkch+i+UsgyQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

	public bool IsCachedSessionKeyValid()
	{
		bool valid = false;
		try
		{
			RSACryptoServiceProvider rSACryptoServiceProvider = new RSACryptoServiceProvider();
			rSACryptoServiceProvider.FromXmlString("<RSAKeyValue><Modulus>mRaP5hO0mWf6gIdPMFD0sg4KLhwsA08Tk2246fdwNk6G7cRk+BJYtTOwKO+plurICQMKF2ktDJWOkjz+Hq2BCjBDB/al7XNdnoOJ1w0BsgInEPOGz9nn8OM4GjQyNcuv0iY0XqwElgy5xCNjBRKJJuqQje/E5SIiHs2O78nJUsZWCv6xjaH+4N/3Kno+sQoBFpNqKmXsq1+2KGMu8t4x58LrojbXzxJUm3O3agK8MvDg/xTAmumd2PTjVJBnrlSBIPdsaQwzX1G9s29B7CzQC6T7TzQehA8hPmUSQLEnwBV6EaUXbcjOBh01i5k5MP6i22wrDCfQMnnkch+i+UsgyQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>");
			byte[] computedHash = SHA256.HashData(Encoding.UTF8.GetBytes(ClientSettings.Sessionkey));
			byte[] signature = Convert.FromBase64String(ClientSettings.SessionSignature);
			valid = rSACryptoServiceProvider.VerifyHash(computedHash, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
			valid &= !string.IsNullOrEmpty(ClientSettings.PlayerUID);
			rSACryptoServiceProvider.Dispose();
		}
		catch (Exception)
		{
		}
		return valid;
	}

	public void ValidateSessionKeyWithServer(Action<EnumAuthServerResponse> OnValidationComplete)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[2]
		{
			new KeyValuePair<string, string>("uid", ClientSettings.PlayerUID),
			new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey)
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/clientvalidate");
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			if (args.State != 0)
			{
				OnValidationComplete(EnumAuthServerResponse.Offline);
			}
			else
			{
				ValidateResponse validateResponse = JsonConvert.DeserializeObject<ValidateResponse>(args.Response);
				if (validateResponse.valid == 1)
				{
					ClientSettings.MpToken = null;
					ClientSettings.Entitlements = validateResponse.entitlements;
					ClientSettings.HasGameServer = validateResponse.hasgameserver;
					GlobalConstants.SinglePlayerEntitlements = validateResponse.entitlements;
					OnValidationComplete(EnumAuthServerResponse.Good);
				}
				else
				{
					ScreenManager.Platform.Logger.Debug("Unable to validate session. Server says: {0}", validateResponse.reason);
					OnValidationComplete(EnumAuthServerResponse.Bad);
				}
			}
		});
	}

	public void RequestMpToken(Action<EnumAuthServerResponse, string> OnValidationComplete, string serverlogintoken)
	{
		TyronThreadPool.QueueTask(delegate
		{
			FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[3]
			{
				new KeyValuePair<string, string>("uid", ClientSettings.PlayerUID),
				new KeyValuePair<string, string>("serverlogintoken", serverlogintoken),
				new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey)
			});
			Uri uri = new Uri("https://auth3.vintagestory.at/v2.1/clientrequestmptoken");
			VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
			{
				if (args.State != 0)
				{
					OnValidationComplete(EnumAuthServerResponse.Offline, "offline");
				}
				else
				{
					MpTokenResponse mpTokenResponse = JsonConvert.DeserializeObject<MpTokenResponse>(args.Response);
					if (mpTokenResponse.valid == 1)
					{
						ClientSettings.MpToken = mpTokenResponse.mptokenv2;
						OnValidationComplete(EnumAuthServerResponse.Good, null);
					}
					else
					{
						ScreenManager.Platform.Logger.Debug("Unable to request mp token. Server says: {0}", mpTokenResponse.reason);
						OnValidationComplete(EnumAuthServerResponse.Bad, mpTokenResponse.reason);
					}
				}
			});
		}, "requestmptoken");
	}

	public void GetNewestVersion(Action<string> OnGetComplete)
	{
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				HttpResponseMessage response = await VSWebClient.Inst.GetAsync("http://api.vintagestory.at/lateststable.txt");
				string responseBody = await response.Content.ReadAsStringAsync();
				if (response.StatusCode == HttpStatusCode.OK)
				{
					OnGetComplete(responseBody);
				}
				else
				{
					OnGetComplete(null);
				}
			}
			catch (Exception)
			{
			}
		});
	}

	public void GetPlayerSkin(string playerUid, Action<byte[]> OnGetComplete)
	{
		TyronThreadPool.QueueTask(delegate
		{
			try
			{
				byte[] result = VSWebClient.Inst.GetByteArrayAsync("https://skins.vintagestory.at/" + playerUid).Result;
				OnGetComplete(result);
			}
			catch (Exception)
			{
				OnGetComplete(null);
			}
		}, "getplayerskin");
	}

	public void DoLogin(string email, string password, string totpCode, string prelogintoken, Action<EnumAuthServerResponse, string, string, string> OnLoginComplete)
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string>("email", email),
			new KeyValuePair<string, string>("password", password),
			new KeyValuePair<string, string>("totpcode", totpCode),
			new KeyValuePair<string, string>("prelogintoken", prelogintoken),
			new KeyValuePair<string, string>("gameloginversion", "1.20.7")
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/v2/gamelogin");
		VSWebClient.Inst.PostAsync(uri, postData, delegate(CompletedArgs args)
		{
			if (args.State != 0)
			{
				OnLoginComplete(EnumAuthServerResponse.Offline, "cantconnect", string.Empty, string.Empty);
				ScreenManager.Platform.Logger.Debug("Login attempt failed: {0}", args.ErrorMessage);
			}
			else
			{
				LoginResponse loginResponse = JsonConvert.DeserializeObject<LoginResponse>(args.Response);
				ScreenManager.Platform.Logger.Debug("Server login response: {0}, reason: {1}", (loginResponse.valid == 1) ? "valid" : "invalid", loginResponse.reason);
				ClientSettings.MpToken = null;
				if (loginResponse.valid == 1)
				{
					ClientSettings.UserEmail = email;
					ClientSettings.Sessionkey = loginResponse.sessionkey;
					ClientSettings.SessionSignature = loginResponse.sessionsignature;
					ClientSettings.HasGameServer = loginResponse.hasgameserver;
					ClientSettings.PlayerUID = loginResponse.uid;
					ClientSettings.PlayerName = loginResponse.playername;
					ClientSettings.Entitlements = loginResponse.entitlements;
					if (IsCachedSessionKeyValid())
					{
						OnLoginComplete(EnumAuthServerResponse.Good, loginResponse.reason, string.Empty, string.Empty);
					}
					else
					{
						OnLoginComplete(EnumAuthServerResponse.Bad, "invalidcachedsessionkey", string.Empty, string.Empty);
					}
				}
				else
				{
					OnLoginComplete(EnumAuthServerResponse.Bad, loginResponse.reason, loginResponse.reasondata, loginResponse.prelogintoken);
				}
			}
		});
	}

	public void DoLogout()
	{
		FormUrlEncodedContent postData = new FormUrlEncodedContent(new KeyValuePair<string, string>[2]
		{
			new KeyValuePair<string, string>("email", ClientSettings.UserEmail),
			new KeyValuePair<string, string>("sessionkey", ClientSettings.Sessionkey)
		});
		Uri uri = new Uri("https://auth3.vintagestory.at/gamelogout");
		VSWebClient.Inst.PostAsync(uri, postData, delegate
		{
		});
		ClientSettings.UserEmail = string.Empty;
		ClientSettings.MpToken = string.Empty;
		ClientSettings.Sessionkey = string.Empty;
		ClientSettings.SessionSignature = string.Empty;
		ClientSettings.PlayerUID = string.Empty;
		ClientSettings.PlayerName = string.Empty;
	}
}
