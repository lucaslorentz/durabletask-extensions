import { createTheme } from "@mui/material/styles";

const { breakpoints, spacing } = createTheme();

export const customTheme = createTheme({
  spacing: spacing,
  breakpoints: breakpoints,
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
