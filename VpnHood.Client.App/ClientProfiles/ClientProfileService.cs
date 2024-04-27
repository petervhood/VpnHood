﻿using System.Security.Authentication;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using VpnHood.Common;
using VpnHood.Common.Exceptions;
using VpnHood.Common.Logging;
using VpnHood.Common.Utils;

namespace VpnHood.Client.App.ClientProfiles;

public class ClientProfileService
{
    private const string FilenameProfiles = "vpn_profiles.json";
    private readonly string _folderPath;
    private readonly List<ClientProfile> _clientProfiles;
    private string[] _builtInAccessTokenIds = [];

    private string ClientProfilesFilePath => Path.Combine(_folderPath, FilenameProfiles);

    public ClientProfileService(string folderPath)
    {
        _folderPath = folderPath ?? throw new ArgumentNullException(nameof(folderPath));
        ClientProfileServiceLegacy.Migrate(folderPath, ClientProfilesFilePath);
        _clientProfiles = Load().ToList();
    }

    public ClientProfile? FindById(Guid clientProfileId)
    {
        return _clientProfiles.SingleOrDefault(x => x.ClientProfileId == clientProfileId);
    }

    public ClientProfile? FindByTokenId(string tokenId)
    {
        return _clientProfiles.SingleOrDefault(x => x.Token.TokenId == tokenId);
    }

    public ClientProfile Get(Guid clientProfileId)
    {
        return _clientProfiles.Single(x => x.ClientProfileId == clientProfileId);
    }

    public Token GetToken(string tokenId)
    {
        var clientProfile = FindByTokenId(tokenId) ?? throw new NotExistsException($"TokenId does not exist. TokenId: {tokenId}");
        return clientProfile.Token;
    }

    public ClientProfile[] List()
    {
        return _clientProfiles.ToArray();
    }

    public void Remove(Guid clientProfileId)
    {
        var clientProfile =
            _clientProfiles.SingleOrDefault(x => x.ClientProfileId == clientProfileId)
            ?? throw new NotExistsException();

        _clientProfiles.Remove(clientProfile);
        Save();
    }

    public void TryRemoveByTokenId(string tokenId)
    {
        var clientProfiles = _clientProfiles.Where(x => x.Token.TokenId == tokenId).ToArray();
        foreach (var clientProfile in clientProfiles)
            _clientProfiles.Remove(clientProfile);

        Save();
    }

    public ClientProfile Update(Guid clientProfileId, ClientProfileUpdateParams updateParams)
    {
        var clientProfile = _clientProfiles.SingleOrDefault(x => x.ClientProfileId == clientProfileId)
            ?? throw new NotExistsException("ClientProfile does not exists. ClientProfileId: {clientProfileId}");

        // update name
        if (updateParams.ClientProfileName != null)
        {
            var name = updateParams.ClientProfileName.Value?.Trim();
            if (name?.Length == 0) name = null;
            clientProfile.ClientProfileName = name;
        }

        // update region
        if (updateParams.RegionId != null)
        {
            if (updateParams.RegionId.Value != null &&
                clientProfile.Token.ServerToken.Regions?.SingleOrDefault(x => x.RegionId == updateParams.RegionId) == null)
                throw new NotExistsException("RegionId does not exist.");

            clientProfile.RegionId = updateParams.RegionId;
        }

        Save();
        return clientProfile;
    }


    public ClientProfile ImportAccessKey(string accessKey)
    {
        var token = Token.FromAccessKey(accessKey);
        return ImportAccessToken(token, overwriteNewer: true, allowOverwriteBuiltIn: false);
    }

    public ClientProfile ImportAccessToken(Token token)
    {
        return ImportAccessToken(token, overwriteNewer: true, allowOverwriteBuiltIn: false);
    }

    private ClientProfile ImportAccessToken(Token token, bool overwriteNewer, bool allowOverwriteBuiltIn)
    {
        // make sure no one overwrites built-in tokens
        if (!allowOverwriteBuiltIn && _builtInAccessTokenIds.Any(tokenId => tokenId == token.TokenId))
            throw new AuthenticationException("Could not overwrite BuiltIn tokens.");

        // update tokens
        foreach (var clientProfile in _clientProfiles.Where(clientProfile =>
                     clientProfile.Token.TokenId == token.TokenId))
        {
            if (overwriteNewer || token.IssuedAt >= clientProfile.Token.IssuedAt)
                clientProfile.Token = token;
        }

        // add if it is a new token
        if (_clientProfiles.All(x => x.Token.TokenId != token.TokenId))
            _clientProfiles.Add(new ClientProfile
            {
                ClientProfileId = Guid.NewGuid(),
                ClientProfileName = token.Name,
                Token = token
            });

        // save profiles
        Save();

        var ret = _clientProfiles.First(x => x.Token.TokenId == token.TokenId);
        return ret;
    }

    internal ClientProfile[] ImportBuiltInAccessKeys(string[] accessKeys)
    {
        var accessTokens = accessKeys.Select(Token.FromAccessKey).ToArray();
        var clientProfiles = accessTokens.Select(token => ImportAccessToken(token, overwriteNewer: false, allowOverwriteBuiltIn: true)).ToArray();
        _builtInAccessTokenIds = clientProfiles.Select(clientProfile => clientProfile.Token.TokenId).ToArray();
        return clientProfiles;
    }

    public Token UpdateTokenByAccessKey(Token token, string accessKey)
    {
        try
        {
            var newToken = Token.FromAccessKey(accessKey);
            if (VhUtil.JsonEquals(token, newToken))
                return token;

            if (token.TokenId != newToken.TokenId)
                throw new Exception("Could not update the token via access key because its token ID is not the same.");

            ImportAccessToken(newToken);
            VhLogger.Instance.LogInformation("ServerToken has been updated.");
            return newToken;
        }
        catch (Exception ex)
        {
            VhLogger.Instance.LogError(ex, "Could not update token from the given access-key.");
            return token;
        }

    }

    public async Task<bool> UpdateTokenByUrl(Token token)
    {
        if (string.IsNullOrEmpty(token.ServerToken.Url) || token.ServerToken.Secret == null)
            return false;

        // update token
        VhLogger.Instance.LogInformation("Trying to get a new ServerToken from url. Url: {Url}", VhLogger.FormatHostName(token.ServerToken.Url));
        try
        {
            using var client = new HttpClient();
            var encryptedServerToken = await VhUtil.RunTask(client.GetStringAsync(token.ServerToken.Url), TimeSpan.FromSeconds(20));
            var newServerToken = ServerToken.Decrypt(token.ServerToken.Secret, encryptedServerToken);

            // return older only if token body is same and created time is newer
            if (!token.ServerToken.IsTokenUpdated(newServerToken))
            {
                VhLogger.Instance.LogInformation("The remote ServerToken is not new and has not been updated.");
                return false;
            }

            //update store
            token = VhUtil.JsonClone<Token>(token);
            token.ServerToken = newServerToken;
            ImportAccessToken(token);
            VhLogger.Instance.LogInformation("ServerToken has been updated from url.");
            return true;
        }
        catch (Exception ex)
        {
            VhLogger.Instance.LogError(ex, "Could not update ServerToken from url.");
            return false;
        }
    }

    private void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ClientProfilesFilePath)!);
        File.WriteAllText(ClientProfilesFilePath, JsonSerializer.Serialize(_clientProfiles));
    }

    private IEnumerable<ClientProfile> Load()
    {
        try
        {
            var json = File.ReadAllText(ClientProfilesFilePath);
            return VhUtil.JsonDeserialize<ClientProfile[]>(json);
        }
        catch
        {
            return [];
        }
    }
}