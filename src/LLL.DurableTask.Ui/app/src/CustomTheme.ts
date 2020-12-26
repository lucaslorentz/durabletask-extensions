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
  },
});
