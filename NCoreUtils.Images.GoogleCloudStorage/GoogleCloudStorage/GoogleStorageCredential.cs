using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    public struct GoogleStorageCredential : IEquatable<GoogleStorageCredential>
    {
        public static string[] ReadOnlyScopes = new [] { "https://www.googleapis.com/auth/devstorage.read_only" };

        public static string[] ReadWriteScopes = new [] { "https://www.googleapis.com/auth/devstorage.read_write" };

        static bool Eq(GoogleCredential? a, GoogleCredential? b)
        {
            if (a is null)
            {
                return b is null;
            }
            if (b is null)
            {
                return false;
            }
            return ReferenceEquals(a, b) || a.Equals(b);
        }

        static Task<string> GetAccessTokenForRequestAsync(GoogleCredential credential, string[] scopes, CancellationToken cancellationToken)
            => credential.CreateScoped(scopes).UnderlyingCredential.GetAccessTokenForRequestAsync(cancellationToken: cancellationToken);

        static async Task<string> GetAccessTokenForRequestAsync(string[] scopes, CancellationToken cancellationToken)
        {
            var credential = await GoogleCredential.GetApplicationDefaultAsync(cancellationToken).ConfigureAwait(false);
            return await GetAccessTokenForRequestAsync(credential, scopes, cancellationToken).ConfigureAwait(false);
        }

        public static bool operator==(GoogleStorageCredential a, GoogleStorageCredential b)
            => a.Equals(b);

        public static bool operator!=(GoogleStorageCredential a, GoogleStorageCredential b)
            => !a.Equals(b);

        public static GoogleStorageCredential ViaAccessToken(string accessToken)
            => new GoogleStorageCredential(accessToken, default);

        public static GoogleStorageCredential ViaGoogleCredenatial(GoogleCredential credential)
            => new GoogleStorageCredential(default, credential);

        public string? AccessToken { get; }

        public GoogleCredential? Credential { get; }

        GoogleStorageCredential(string? accessToken, GoogleCredential? credential)
        {
            AccessToken = accessToken;
            Credential = credential;
        }

        public bool Equals(GoogleStorageCredential other)
            => StringComparer.InvariantCulture.Equals(AccessToken, other.AccessToken)
                && Eq(Credential, other.Credential);

        public override bool Equals(object obj)
            => obj is GoogleStorageCredential other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(AccessToken, Credential);

        public ValueTask<string> GetAccessTokenAsync(string[] scopes, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrEmpty(AccessToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                return new ValueTask<string>(AccessToken!);
            }
            var task = Credential is null
                ? GetAccessTokenForRequestAsync(scopes, cancellationToken)
                : GetAccessTokenForRequestAsync(Credential, scopes, cancellationToken);
            return new ValueTask<string>(task);
        }
    }
}