import { UserManagerSettings } from "oidc-client";

export interface Configuration {
  apiBaseUrl: string;
  oidc?: UserManagerSettings;
  userNameClaims?: string[];
}
