import { createMuiTheme } from "@material-ui/core/styles";
import createBreakpoints from "@material-ui/core/styles/createBreakpoints";
import createSpacing from "@material-ui/core/styles/createSpacing";

const breakpoints = createBreakpoints({});
const spacing = createSpacing(8);

export const customTheme = createMuiTheme({
  spacing: spacing,
  breakpoints: breakpoints,
  overrides: {
    MuiToolbar: {
      regular: {
        [breakpoints.up("sm")]: {
          minHeight: 54,
        },
      },
    },
    MuiAlert: {
      icon: {
        padding: 5,
        marginRight: spacing(0.5),
      },
    },
  } as any,
  typography: {
    fontSize: 13,
    h1: {
      fontSize: "2rem",
    },
    h2: {
      fontSize: "1.75rem",
    },
    h3: {
      fontSize: "1.5rem",
    },
    h4: {
      fontSize: "1.25rem",
    },
    h5: {
      fontSize: "1rem",
    },
    h6: {
      fontSize: "0.875rem",
    },
  },
});
