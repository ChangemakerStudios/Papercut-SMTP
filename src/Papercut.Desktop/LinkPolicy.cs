// Papercut
//
// Copyright © 2008 - 2012 Ken Robertson
// Copyright © 2013 - 2026 Jaben Cargman
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


namespace Papercut.Desktop;

internal enum LinkAction
{
    /// <summary>Load in the window.</summary>
    Allow,

    /// <summary>Hand to the OS: http(s) to the default browser, mailto: to the mail client.</summary>
    OpenExternally,

    Block
}

/// <summary>
/// One rule for every navigation and new-window request, user-initiated or not: the service
/// origin stays in the window, everything else leaves it or is blocked. Email HTML is untrusted,
/// so unlike Papercut.UI there is no "not user-initiated, so allow" exception.
/// </summary>
internal static class LinkPolicy
{
    public static LinkAction Decide(Uri uri, Uri? origin)
    {
        if (origin != null && IsSameOrigin(uri, origin)) return LinkAction.Allow;

        // iframes start at about:blank / about:srcdoc
        if (uri.Scheme == "about") return LinkAction.Allow;

        if (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto)
            return LinkAction.OpenExternally;

        return LinkAction.Block;
    }

    public static bool IsSameOrigin(Uri uri, Uri origin) =>
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.Equals(uri.Scheme, origin.Scheme, StringComparison.OrdinalIgnoreCase)
        && string.Equals(uri.Authority, origin.Authority, StringComparison.OrdinalIgnoreCase);
}
