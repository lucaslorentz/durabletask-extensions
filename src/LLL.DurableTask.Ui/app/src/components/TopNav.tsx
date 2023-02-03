import { Menu as MenuIcon, Person } from "@mui/icons-material";
import {
  Box,
  Button,
  Container,
  Divider,
  Drawer,
  Grid,
  IconButton,
  List,
  ListItem,
  ListItemButton,
  ListItemText,
  Menu,
  MenuItem,
} from "@mui/material";
import AppBar from "@mui/material/AppBar";
import Link from "@mui/material/Link";
import Toolbar from "@mui/material/Toolbar";
import Typography from "@mui/material/Typography";
import React from "react";
import { Link as RouterLink } from "react-router-dom";
import { useApiClient } from "../hooks/useApiClient";
import { useAuth } from "../hooks/useAuth";
import { useConfiguration } from "../ConfigurationProvider";

export function TopNav() {
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

  const userName = auth.user
    ? (configuration.userNameClaims ?? ["preferred_username", "name", "sub"])
        .map((claim) => auth.user!.profile?.[claim])
        .find(Boolean) ?? "Authenticated"
    : undefined;

  const pages = [
    {
      label: "Create",
      to: "/create",
      visible: apiClient.isAuthorized("OrchestrationsCreate"),
    },
    {
      label: "Orchestrations",
      to: "/orchestrations",
      visible: apiClient.isAuthorized("OrchestrationsList"),
    },
  ].filter((p) => p.visible);

  const logoLink = (
    <Link component={RouterLink} to="/" underline="none" color="inherit">
      <Typography variant="h4">Durable Task UI</Typography>
    </Link>
  );

  const [mobileOpen, setMobileOpen] = React.useState(false);
  const handleDrawerToggle = () => {
    setMobileOpen((prevState) => !prevState);
  };

  return (
    <>
      <AppBar position="static" elevation={0}>
        <Container maxWidth="xl">
          <Toolbar>
            <IconButton
              color="inherit"
              aria-label="open drawer"
              edge="start"
              onClick={handleDrawerToggle}
              sx={{ mr: 2, display: { md: "none" } }}
            >
              <MenuIcon />
            </IconButton>
            <Box sx={{ display: { xs: "none", md: "block" }, mr: 2 }}>
              {logoLink}
            </Box>
            <Box sx={{ flexGrow: 1, display: { xs: "none", md: "flex" } }}>
              {pages.map((page) => (
                <Grid key={page.label} item>
                  <Button component={RouterLink} to={page.to} color="inherit">
                    {page.label}
                  </Button>
                </Grid>
              ))}
            </Box>
            {auth.enabled && (
              <Box sx={{ flexGrow: 0 }}>
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
              </Box>
            )}
          </Toolbar>
        </Container>
      </AppBar>
      <Box component="nav">
        <Drawer
          variant="temporary"
          open={mobileOpen}
          onClose={handleDrawerToggle}
          ModalProps={{
            keepMounted: true, // Better open performance on mobile.
          }}
          sx={{
            display: { xs: "block", md: "none" },
            "& .MuiDrawer-paper": {
              boxSizing: "border-box",
              maxWidth: 350,
              width: "80%",
            },
          }}
        >
          <Box onClick={handleDrawerToggle} sx={{ textAlign: "center" }}>
            <Box marginY={2}>{logoLink}</Box>
            <Divider />
            <List>
              {pages.map((page) => (
                <ListItem
                  key={page.label}
                  component={RouterLink}
                  to={page.to}
                  sx={{ color: "inherit" }}
                  disablePadding
                >
                  <ListItemButton>
                    <ListItemText primary={page.label} />
                  </ListItemButton>
                </ListItem>
              ))}
            </List>
          </Box>
        </Drawer>
      </Box>
    </>
  );
}
