using Devlooped;
using Devlooped.WhatsApp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

record UserProfile(string User, string? AccessToken = null, string? RefreshToken = null);

public class UserService(CloudStorageAccount account, IOptions<GoogleOptions> options)
{
    const string AuthUrlTemplate = 
        "https://accounts.google.com/o/oauth2/v2/auth?" +
        "scope=https://www.googleapis.com/auth/calendar.readonly&" +
        "access_type=offline&" +
        "include_granted_scopes=true&" +
        "response_type=code&" +
        "state={0}&" +
        "redirect_uri={1}{2}&" +
        "client_id={3}";

    Lazy<ITableRepository<UserProfile>> usersRepository = new(() =>
        TableRepository.Create<UserProfile>(
            account,
            "users",
            x => x.User,
            x => "profile"));

    public async Task<string?> GetAccessTokenAsync(string user)
        => (await GetUserAsync(user))?.AccessToken;

    public Task<string> GenerateAuthUrlAsync(string user)
        => Task.FromResult(string.Format(AuthUrlTemplate, user, options.Value.Endpoint, options.Value.CallbackUri, options.Value.ClientId));

    public async Task UpdateTokenAsync(string user, string accessToken, string refreshToken)
        => await usersRepository.Value.PutAsync(
            await GetUserOrCreateAsync(user) with
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });

    async Task<UserProfile?> GetUserAsync(string user)
        => await usersRepository.Value.GetAsync(user, "profile") ?? new(user);

    async Task<UserProfile> GetUserOrCreateAsync(string user)
        => await usersRepository.Value.GetAsync(user, "profile") ?? new(user);

}