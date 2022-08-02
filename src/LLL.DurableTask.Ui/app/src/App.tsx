import { Person } from "@mui/icons-material";
import {
  Box,
  Button,
  CircularProgress,
  Container,
  Grid,
  Menu,
  MenuItem,
} from "@mui/material";
import AppBar from "@mui/material/AppBar";
import CssBaseline from "@mui/material/CssBaseline";
import Link from "@mui/material/Link";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import makeStyles from "@mui/styles/makeStyles";
import React, { Suspense } from "react";
import { Link as RouterLink, Route, Routes } from "react-router-dom";
import { useApiClient } from "./ApiClientProvider";
import { useAuth } from "./AuthProvider";
import { AuthorizedGuard } from "./components/AuthorizedGuard";
import { useConfiguration } from "./ConfigurationProvider";
import { Create } from "./views/create";
import { Home } from "./views/home";
import { NotFound } from "./views/not_found";
import { Orchestration } from "./views/orchestration";
import { Orchestrations } from "./views/orchestrations";

const useStyles = makeStyles((theme) => ({
  title: {
    marginRight: theme.spacing(4),
  },
}));

export function App() {
  const classes = useStyles();

  const configuration = useConfiguration();
  const auth = useAuth();
  const apiClient = useApiClient();

  const [userAnchorEl, setUserAnchorEl] = React.useState<
    HTMLButtonElement | undefined
  >(undefined);

  function openUserMenu(event: React.MouseEvent<HTMLButtonElement>) {
    setUserAnchorEl(event.currentTarget);
  }

  function closeUserMenu() {
    setUserAnchorEl(undefined);
  }

  let userName: string | undefined;
  if (auth.user) {
    userName =
      (configuration.userNameClaims ?? ["preferred_username", "name", "sub"])
        .map((claim) => auth.user!.profile?.[claim])
        .find(Boolean) ?? "Authenticated";
  }

  return (
    <div>
      <CssBaseline />
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <Grid container alignItems="center">
            <Grid item>
              <Link
                component={RouterLink}
                to="/"
                underline="none"
                color="inherit"
              >
                <Typography variant="h4" className={classes.title}>
                  Durable Task UI
                </Typography>
              </Link>
            </Grid>
            {apiClient.isAuthorized("OrchestrationsCreate") && (
              <Grid item>
                <Button component={RouterLink} to="/create" color="inherit">
                  Create
                </Button>
              </Grid>
            )}
            {apiClient.isAuthorized("OrchestrationsList") && (
              <Grid item>
                <Button
                  component={RouterLink}
                  to="/orchestrations"
                  color="inherit"
                >
                  Orchestrations
                </Button>
              </Grid>
            )}
            {auth.enabled && (
              <Grid item style={{ marginLeft: "auto" }}>
                {userName ? (
                  <>
                    <Button
                      color="inherit"
                      onClick={openUserMenu}
                      startIcon={<Person />}
                    >
                      {userName}
                    </Button>
                    <Menu
                      id="menu-appbar"
                      anchorEl={userAnchorEl}
                      open={Boolean(userAnchorEl)}
                      keepMounted
                      anchorOrigin={{
                        vertical: "bottom",
                        horizontal: "right",
                      }}
                      transformOrigin={{
                        vertical: "top",
                        horizontal: "right",
                      }}
                      onClose={closeUserMenu}
                    >
                      <MenuItem onClick={auth.signOut}>Sign out</MenuItem>
                    </Menu>
                  </>
                ) : (
                  <Button color="inherit" onClick={auth.signIn}>
                    Sign in
                  </Button>
                )}
              </Grid>
            )}
          </Grid>
        </Toolbar>
      </AppBar>
      <Container maxWidth="xl">
        <Box marginTop={3}>
          <Suspense fallback={<CircularProgress />}>
            <Routes>
              <Route
                path="/orchestrations"
                element={
                  <AuthorizedGuard requiredEndpoints={["OrchestrationsList"]}>
                    <Orchestrations />
                  </AuthorizedGuard>
                }
              />
              <Route
                path="/orchestrations/:instanceId"
                element={
                  <AuthorizedGuard
                    requiredEndpoints={[
                      "OrchestrationsGet",
                      "OrchestrationsGetExecution",
                    ]}
                  >
                    <Orchestration />
                  </AuthorizedGuard>
                }
              />
              {apiClient.hasFeature("StatePerExecution") && (
                <Route
                  path="/orchestrations/:instanceId/:executionId"
                  element={
                    <AuthorizedGuard
                      requiredEndpoints={[
                        "OrchestrationsGet",
                        "OrchestrationsGetExecution",
                      ]}
                    >
                      <Orchestration />
                    </AuthorizedGuard>
                  }
                />
              )}
              <Route
                path="/create"
                element={
                  <AuthorizedGuard requiredEndpoints={["OrchestrationsCreate"]}>
                    <Create />
                  </AuthorizedGuard>
                }
              />
              <Route path="/" element={<Home />} />
              <Route element={<NotFound />} />
            </Routes>
          </Suspense>
        </Box>
      </Container>
    </div>
  );
}
