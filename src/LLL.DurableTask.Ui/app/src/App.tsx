import {
  Box,
  Button,
  CircularProgress,
  Container,
  Grid,
  makeStyles,
  Menu,
  MenuItem,
} from "@material-ui/core";
import AppBar from "@material-ui/core/AppBar";
import CssBaseline from "@material-ui/core/CssBaseline";
import Toolbar from "@material-ui/core/Toolbar";
import Typography from "@material-ui/core/Typography";
import { Person } from "@material-ui/icons";
import React, { Suspense } from "react";
import { Link as RouterLink, Redirect, Route, Switch } from "react-router-dom";
import { useAuth } from "./AuthProvider";
import { ProtectedRoute } from "./components/ProtectedRoute";
import { useConfiguration } from "./ConfigurationProvider";
import { useEntrypoint } from "./EntrypointProvider";
import { Create } from "./views/create";
import { Orchestration } from "./views/orchestration";
import { Orchestrations } from "./views/orchestrations";
import { NotFound } from "./views/not_found";
import { Home } from "./views/home";
import Link from "@material-ui/core/Link";

const useStyles = makeStyles((theme) => ({
  title: {
    marginRight: theme.spacing(4),
  },
}));

export function App() {
  const classes = useStyles();

  const configuration = useConfiguration();

  const auth = useAuth();

  const { endpoints } = useEntrypoint();

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
            {endpoints.OrchestrationsCreate.authorized && (
              <Grid item>
                <Button component={RouterLink} to="/create" color="inherit">
                  Create
                </Button>
              </Grid>
            )}
            {endpoints.OrchestrationsList.authorized && (
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
                      getContentAnchorEl={null}
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
            <Switch>
              <ProtectedRoute
                requiredEndpoints={["OrchestrationsList"]}
                path="/orchestrations"
                exact
              >
                <Orchestrations />
              </ProtectedRoute>
              <ProtectedRoute
                requiredEndpoints={[
                  "OrchestrationsGet",
                  "OrchestrationsGetExecution",
                ]}
                path="/orchestrations/:instanceId/:executionId?"
                exact
              >
                <Orchestration />
              </ProtectedRoute>
              <ProtectedRoute
                requiredEndpoints={["OrchestrationsCreate"]}
                path="/create"
                exact
              >
                <Create />
              </ProtectedRoute>
              <Route path="/" exact>
                <Home />
              </Route>
              <NotFound />
            </Switch>
          </Suspense>
        </Box>
      </Container>
    </div>
  );
}
