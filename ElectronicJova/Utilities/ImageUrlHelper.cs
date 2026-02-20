namespace ElectronicJova.Utilities
{
    public static class ImageUrlHelper
    {
        public static string GetDisplayUrl(string? imageUrl, string placeholderUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                return placeholderUrl;
            }

            var normalized = imageUrl.Trim().Replace('\\', '/');

            if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return normalized;
            }

            if (normalized.StartsWith("~/", StringComparison.Ordinal))
            {
                normalized = normalized[1..];
            }

            if (!normalized.StartsWith("/", StringComparison.Ordinal))
            {
                normalized = "/" + normalized.TrimStart('/');
            }

            if (normalized.Contains("..", StringComparison.Ordinal))
            {
                return placeholderUrl;
            }

            return normalized;
        }
    }
}
