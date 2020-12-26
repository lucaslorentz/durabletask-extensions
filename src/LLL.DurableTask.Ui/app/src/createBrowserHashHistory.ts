import {
  Action,
  createBrowserHistory,
  History,
  Location,
  LocationState,
  LocationDescriptorObject,
  UnregisterCallback,
} from "history";

type UrlParts = { pathname: string; search: string; hash: string };

interface BrowserHashHistoryOptions {
  clearSearch?: boolean;
}

const defaultOptions: BrowserHashHistoryOptions = {};

export function createBrowserHashHistory(
  options: BrowserHashHistoryOptions = defaultOptions
): History<LocationState> {
  const { clearSearch = false } = options;

  const browserHistory = createBrowserHistory();

  return {
    get length(): number {
      return browserHistory.length;
    },
    get action(): Action {
      return browserHistory.action;
    },
    get location(): Location<LocationState> {
      return patchOutgoing(browserHistory.location);
    },
    push(
      pathOrLocation: string | LocationDescriptorObject<LocationState>,
      state?: any
    ) {
      return browserHistory.push(patchIncoming(pathOrLocation, state));
    },
    replace(
      pathOrLocation: string | LocationDescriptorObject<LocationState>,
      state?: any
    ) {
      return browserHistory.replace(patchIncoming(pathOrLocation, state));
    },
    go(n: number): void {
      browserHistory.go(n);
    },
    goBack(): void {
      browserHistory.goBack();
    },
    goForward(): void {
      browserHistory.goForward();
    },
    block(
      prompt?:
        | string
        | boolean
        | History.TransitionPromptHook<LocationState>
        | undefined
    ): UnregisterCallback {
      return browserHistory.block(prompt);
    },
    listen(
      listener: History.LocationListener<LocationState>
    ): UnregisterCallback {
      return browserHistory.listen((location, action) => {
        listener(patchOutgoing(location), action);
      });
    },
    createHref(location: LocationDescriptorObject<LocationState>): string {
      return browserHistory.createHref(patchIncoming(location));
    },
  };

  function patchIncoming(
    pathOrLocation: string | LocationDescriptorObject<LocationState>,
    state?: any
  ): LocationDescriptorObject<LocationState> {
    let location: LocationDescriptorObject<LocationState>;
    if (typeof pathOrLocation === "string") {
      location = splitPath(pathOrLocation);
      if (arguments.length > 1) {
        location.state = state;
      }
    } else {
      location = pathOrLocation;
    }
    return {
      ...location,
      pathname: browserHistory.location.pathname,
      search: clearSearch ? "" : browserHistory.location.search,
      hash: combinePath(location),
    };
  }

  function patchOutgoing(
    location: Location<LocationState>
  ): Location<LocationState> {
    const urlParts = splitPath(location.hash);
    return {
      ...location,
      pathname: urlParts.pathname,
      search: urlParts.search,
      hash: urlParts.hash,
    };
  }

  function combinePath(parts: Partial<UrlParts>): string {
    let { pathname, search, hash } = parts;
    if (pathname && pathname.charAt(0) !== "/") {
      pathname = "/" + pathname;
    }
    if (search && search.charAt(0) !== "?") {
      search = "?" + search;
    } else if (search === "?") {
      search = "";
    }
    if (hash && hash.charAt(0) !== "#") {
      hash = "#" + hash;
    } else if (hash === "#") {
      hash = "";
    }
    return "#" + pathname + search + hash;
  }

  function splitPath(url: string): UrlParts {
    let parts = url.substr(1).split("?", 2);
    if (parts.length > 1) {
      parts = [parts[0], ...parts[1].split("#", 2)];
    }
    return {
      pathname: parts[0],
      search: parts[1] ? "?" + parts[1] : "",
      hash: parts[2] ? "#" + parts[2] : "",
    };
  }
}
