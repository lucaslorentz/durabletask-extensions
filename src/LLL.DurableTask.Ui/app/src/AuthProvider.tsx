import { User, UserManager } from "oidc-client";
import React, { useContext, useLayoutEffect, useMemo, useState } from "react";
import { useHistory } from "react-router-dom";
import { apiAxios } from "./apiAxios";
import { useConfiguration } from "./ConfigurationProvider";

type Props = {
  children: React.ReactNode;
};

type Auth =
  | {
      enabled: false;
      signIn: undefined;
      signOut: undefined;
      user: undefined;
    }
  | {
      enabled: true;
      signIn: () => Promise<void>;
      signOut: () => Promise<void>;
      user?: User;
    };

const authContext = React.createContext<Auth | undefined>(undefined);

export function useAuth(): Auth {
  return useContext(authContext) as Auth;
}

export function AuthProvider(props: Props) {
  const { children } = props;

  const configuration = useConfiguration();

  const history = useHistory();
  const [user, setUser] = useState<User | undefined>();

  const userManager = useMemo(() => {
    if (!configuration.oidc) return undefined;

    const settings = {
      ...configuration.oidc,
    };

    if (!settings.redirect_uri) {
      settings.redirect_uri = window.location.origin + window.location.pathname;
    }

    if (!settings.post_logout_redirect_uri) {
      settings.post_logout_redirect_uri =
        window.location.origin + window.location.pathname;
    }

    return new UserManager(settings);
  }, [configuration]);

  const [loaded, setLoaded] = useState(!userManager);

  // Handle callbacks and load user
  useLayoutEffect(() => {
    if (!userManager) return;

    (async () => {
      if (
        window.location.search?.indexOf("session_state") > -1 ||
        window.location.search?.indexOf("error_description") > -1
      ) {
        const user = await userManager.signinRedirectCallback();
        history.replace(user.state);
        setUser(user);
      } else {
        const user = await userManager.getUser();
        setUser(user ?? undefined);
      }

      setLoaded(true);

      const unsetUser = () => setUser(undefined);

      userManager.events.addUserLoaded(setUser);
      userManager.events.addUserUnloaded(unsetUser);

      return () => {
        userManager.events.removeUserLoaded(setUser);
        userManager.events.removeUserUnloaded(unsetUser);
      };
    })();
  }, [userManager, history]);

  // Configure axios authorization header
  useLayoutEffect(() => {
    if (user) {
      apiAxios.defaults.headers.common.Authorization = `Bearer ${user.access_token}`;
    } else {
      delete apiAxios.defaults.headers.common.Authorization;
    }
  }, [user]);

  // Context value
  const auth = useMemo<Auth>(() => {
    if (!userManager) {
      return { enabled: false };
    }

    return {
      enabled: true,
      user: user,
      signIn: async () => {
        await userManager.signinRedirect({
          state: {
            pathname: history.location.pathname,
            search: history.location.search,
            hash: history.location.hash,
            state: history.location.state,
          },
        });
        await new Promise((resolve) => setTimeout(resolve, 30000));
      },
      signOut: () => userManager.signoutRedirect(),
    };
  }, [userManager, user, history]);

  if (!loaded) return null;

  return <authContext.Provider value={auth}>{children}</authContext.Provider>;
}
