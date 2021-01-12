import { User, UserManager } from "oidc-client";
import React, { useContext, useLayoutEffect, useMemo, useState } from "react";
import { useHistory } from "react-router-dom";
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

    if (!settings.silent_redirect_uri) {
      settings.silent_redirect_uri =
        window.location.origin + window.location.pathname;
    }

    return new UserManager(settings);
  }, [configuration]);

  const [shouldRender, setShouldRender] = useState(!userManager);

  // Listen to user changes
  useLayoutEffect(() => {
    if (!userManager) return;

    const unsetUser = () => setUser(undefined);

    userManager.events.addUserLoaded(setUser);
    userManager.events.addUserUnloaded(unsetUser);
    userManager.events.addAccessTokenExpired(unsetUser);

    return () => {
      userManager.events.removeUserLoaded(setUser);
      userManager.events.removeUserUnloaded(unsetUser);
      userManager.events.removeAccessTokenExpired(unsetUser);
    };
  }, [userManager]);

  // Handle callbacks and load user
  useLayoutEffect(() => {
    if (!userManager) return;

    (async () => {
      if (
        window.location.search?.indexOf("session_state") > -1 ||
        window.location.search?.indexOf("error_description") > -1
      ) {
        if (window.self !== window.top) {
          await userManager.signinSilentCallback();
        } else {
          const user = await userManager.signinRedirectCallback();
          if (user.state) {
            history.replace(user.state);
          }
          setUser(user);
          setShouldRender(true);
        }
      } else {
        let user = await userManager.getUser();
        if (user == null || user.expired) {
          try {
            user = await userManager.signinSilent();
          } catch (error) {
            await userManager.removeUser();
            user = null;
          }
        }
        if (user != null && user.expired) {
          await userManager.removeUser();
          user = null;
        }
        setUser(user ?? undefined);
        setShouldRender(true);
      }
    })();
  }, [userManager, history]);

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

  if (!shouldRender) return null;

  return <authContext.Provider value={auth}>{children}</authContext.Provider>;
}
