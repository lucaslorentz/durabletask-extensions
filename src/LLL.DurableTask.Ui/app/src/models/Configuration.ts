import { OidcClientSettings } from "oidc-client";

export interface Configuration {
  apiBaseUrl: string;
  oidc?: OidcClientSettings;
  userNameClaims?: string[];
}
