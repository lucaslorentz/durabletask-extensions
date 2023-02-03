import { Box, CircularProgress, Container } from "@mui/material";
import CssBaseline from "@mui/material/CssBaseline";
import React, { Suspense } from "react";
import { Route, Routes } from "react-router-dom";
import { useApiClient } from "./hooks/useApiClient";
import { AuthorizedGuard } from "./components/AuthorizedGuard";
import { TopNav } from "./components/TopNav";
import { Create } from "./views/create";
import { Home } from "./views/home";
import { NotFound } from "./views/not_found";
import { Orchestration } from "./views/orchestration";
import { Orchestrations } from "./views/orchestrations";

export function App() {
  const apiClient = useApiClient();

  return (
    <div>
      <CssBaseline />
      <TopNav />
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
