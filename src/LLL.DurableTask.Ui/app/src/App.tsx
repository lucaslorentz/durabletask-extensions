import {
  Box,
  Button,
  Container,
  makeStyles,
  CircularProgress,
} from "@material-ui/core";
import AppBar from "@material-ui/core/AppBar";
import CssBaseline from "@material-ui/core/CssBaseline";
import Toolbar from "@material-ui/core/Toolbar";
import Typography from "@material-ui/core/Typography";
import React, { Suspense } from "react";
import { Redirect, Route, Switch, Link as RouterLink } from "react-router-dom";
import { Orchestration } from "./views/orchestration";
import { Orchestrations } from "./views/orchestrations";
import { Create } from "./views/create";

const useStyles = makeStyles((theme) => ({
  title: {
    marginRight: theme.spacing(4),
  },
}));

export function App() {
  const classes = useStyles();

  return (
    <div>
      <CssBaseline />
      <AppBar position="static" elevation={0}>
        <Toolbar>
          <Typography variant="h6" className={classes.title}>
            Durable Task UI
          </Typography>
          <Button component={RouterLink} to="/create" color="inherit">
            Create
          </Button>
          <Button component={RouterLink} to="/orchestrations" color="inherit">
            Orchestrations
          </Button>
        </Toolbar>
      </AppBar>
      <Container maxWidth="xl">
        <Box marginTop={3}>
          <Suspense fallback={<CircularProgress />}>
            <Switch>
              <Route path="/orchestrations" exact>
                <Orchestrations />
              </Route>
              <Route path="/orchestrations/:instanceId/:executionId?">
                <Orchestration />
              </Route>
              <Route path="/create">
                <Create />
              </Route>
              <Redirect to="/orchestrations" />
            </Switch>
          </Suspense>
        </Box>
      </Container>
    </div>
  );
}
