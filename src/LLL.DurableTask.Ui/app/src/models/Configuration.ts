import { UserManagerSettings } from "oidc-client-ts";

export interface Configuration {
  apiBaseUrl: string;
  oidc?: UserManagerSettings;
  userNameClaims?: string[];
}
