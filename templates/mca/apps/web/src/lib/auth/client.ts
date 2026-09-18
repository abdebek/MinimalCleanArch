import { UserManager, WebStorageStateStore, type User } from "oidc-client-ts";

/**
 * Browser OIDC authorization-code + PKCE client for a generated `dotnet new mca --auth` API.
 * Public client `mca-spa-client` (no secret). Tokens stay in the browser; attach Bearer on API calls.
 */
export interface McaAuthOptions {
  /** OpenIddict authority, e.g. http://localhost:5xxx (the generated API). */
  authority: string;
  /** Must match OpenIddict Clients:Spa. Default mca-spa-client. */
  clientId?: string;
  /** Must be listed on the SPA client (default http://localhost:4321/callback). */
  redirectUri: string;
  postLogoutRedirectUri?: string;
  /** Default: openid profile email roles offline_access mca.api */
  scope?: string;
}

const defaultClientId = "mca-spa-client";
const defaultScope = "openid profile email roles offline_access mca.api";

export class McaAuthClient {
  private readonly manager: UserManager;

  constructor(options: McaAuthOptions) {
    const authority = options.authority.replace(/\/+$/, "");
    this.manager = new UserManager({
      authority,
      metadataUrl: `${authority}/.well-known/openid-configuration`,
      client_id: options.clientId ?? defaultClientId,
      redirect_uri: options.redirectUri,
      post_logout_redirect_uri:
        options.postLogoutRedirectUri ?? new URL("/", options.redirectUri).href,
      response_type: "code",
      scope: options.scope ?? defaultScope,
      automaticSilentRenew: true,
      loadUserInfo: true,
      userStore:
        typeof window === "undefined"
          ? undefined
          : new WebStorageStateStore({ store: window.sessionStorage }),
    });
  }

  /** Redirects the browser to `/connect/authorize` (PKCE). */
  login(): Promise<void> {
    return this.manager.signinRedirect();
  }

  /** Exchange `?code=` on the redirect URI. Call this from `/callback`. */
  handleCallback(): Promise<User> {
    return this.manager.signinRedirectCallback();
  }

  logout(): Promise<void> {
    return this.manager.signoutRedirect();
  }

  async getUser(): Promise<User | null> {
    let user = await this.manager.getUser();
    if (user && user.expired) {
      try {
        user = await this.manager.signinSilent();
      } catch {
        return null;
      }
    }

    return user;
  }

  /**
   * Current access token. Uses the refresh token (`grant_type=refresh_token`) via silent renew when expired.
   */
  async getAccessToken(): Promise<string | null> {
    let user = await this.manager.getUser();
    if (user && user.expired) {
      try {
        user = await this.manager.signinSilent();
      } catch {
        return null;
      }
    }

    return user?.access_token ?? null;
  }

  /** `fetch` that attaches `Authorization: Bearer` and retries once after refresh on 401. */
  async fetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response> {
    const headers = new Headers(init?.headers);
    const token = await this.getAccessToken();
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    }

    let response = await globalThis.fetch(input, { ...init, headers });
    if (response.status !== 401) {
      return response;
    }

    try {
      await this.manager.signinSilent();
    } catch {
      return response;
    }

    const retryToken = await this.getAccessToken();
    if (!retryToken) {
      return response;
    }

    headers.set("Authorization", `Bearer ${retryToken}`);
    return globalThis.fetch(input, { ...init, headers });
  }
}

export function createMcaAuth(options: McaAuthOptions): McaAuthClient {
  return new McaAuthClient(options);
}
