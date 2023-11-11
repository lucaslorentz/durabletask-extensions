import DeleteIcon from "@mui/icons-material/Delete";
import { LoadingButton } from "@mui/lab";
import {
  Box,
  Breadcrumbs,
  Button,
  Grid,
  IconButton,
  Link,
  Paper,
  Stack,
  Typography,
} from "@mui/material";
import { useMutation } from "@tanstack/react-query";
import { useSnackbar } from "notistack";
import React, { useCallback } from "react";
import { Link as RouterLink, useNavigate } from "react-router-dom";
import * as yup from "yup";
import { CodeEditor } from "../../form/CodeEditor";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { useApiClient } from "../../hooks/useApiClient";
import { CreateOrchestrationRequest } from "../../models/ApiModels";
import { validateJson } from "../../utils/yup-utils";

const schema = yup
  .object({
    name: yup.string().label("Name").default("").required(),
    version: yup.string().label("Version").default(""),
    instanceId: yup.string().label("Instance Id").default(""),
    input: yup
      .string()
      .label("Input")
      .default("")
      .required()
      .test("JSON", "Must be a valid json", validateJson),
    tags: yup
      .array(
        yup
          .object({
            key: yup.string().label("Key").required(),
            value: yup.string().label("Value").required(),
          })
          .required(),
      )
      .default(() => [])
      .defined(),
  })
  .required();

export function Create() {
  const form = useForm(schema);
  const navigate = useNavigate();
  const apiClient = useApiClient();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();

  const createMutation = useMutation<
    Awaited<ReturnType<typeof apiClient.createOrchestration>>,
    unknown,
    Parameters<typeof apiClient.createOrchestration>
  >({
    mutationFn: (args) => apiClient.createOrchestration(...args),
  });

  const handleSaveClick = useCallback(async () => {
    try {
      const request: CreateOrchestrationRequest = {
        name: form.value.name,
        version: form.value.version,
        instanceId: form.value.instanceId,
        input: form.value.input ? JSON.parse(form.value.input) : null,
        tags: form.value.tags.reduce(
          (previous, current) => {
            previous[current.key!] = current.value!;
            return previous;
          },
          {} as Record<string, string>,
        ),
      };

      const instance = await createMutation.mutateAsync([request]);

      enqueueSnackbar("Orchestration created", {
        variant: "success",
      });

      navigate(`/orchestrations/${encodeURIComponent(instance.instanceId)}`);
    } catch (error) {
      enqueueSnackbar(String(error), {
        variant: "error",
        persist: true,
        action: (key) => (
          <Button color="inherit" onClick={() => closeSnackbar(key)}>
            Dismiss
          </Button>
        ),
      });
    }
  }, [closeSnackbar, createMutation, enqueueSnackbar, form, navigate]);

  return (
    <div>
      <Box marginBottom={2}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={RouterLink} to="/orchestrations" underline="hover">
            Orchestrations
          </Link>
          <Typography color="textPrimary">Create</Typography>
        </Breadcrumbs>
      </Box>
      <Paper variant="outlined">
        <Box padding={2}>
          <Grid container spacing={2}>
            <Grid item xs={12} sm={6}>
              <TextField field={form.field("name")} autoFocus />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField field={form.field("version")} />
            </Grid>
            <Grid item xs={12} sm={6}>
              <TextField field={form.field("instanceId")} />
            </Grid>
            <Grid item xs={12}>
              <CodeEditor
                field={form.field("input")}
                editorProps={{
                  height: 200,
                  defaultLanguage: "json",
                }}
              />
            </Grid>
            {apiClient.hasFeature("Tags") && (
              <>
                <Grid item xs={12}>
                  <Button
                    onClick={() =>
                      form.field("tags").push({ key: "", value: "" })
                    }
                  >
                    Add tag
                  </Button>
                </Grid>
                {form.field("tags").render((field) =>
                  field.fields().map((tagField) => (
                    <Grid key={tagField.path} item xs={12}>
                      <Stack direction="row" alignItems="start" spacing={1}>
                        <Grid container spacing={2}>
                          <Grid item xs={12} sm={6}>
                            <TextField field={tagField.field("key")} />
                          </Grid>
                          <Grid item xs={12} sm={6}>
                            <TextField field={tagField.field("value")} />
                          </Grid>
                        </Grid>
                        <IconButton
                          onClick={() => field.remove(tagField.value)}
                        >
                          <DeleteIcon />
                        </IconButton>
                      </Stack>
                    </Grid>
                  )),
                )}
              </>
            )}
            {form.render((form) => (
              <Grid
                item
                xs={12}
                container
                spacing={1}
                justifyContent="space-between"
              >
                <Grid item>
                  <LoadingButton
                    variant="contained"
                    color="primary"
                    loading={createMutation.isPending}
                    onClick={handleSaveClick}
                    disabled={
                      form.pendingValidation ||
                      Object.keys(form.errors).length > 0
                    }
                  >
                    Create
                  </LoadingButton>
                </Grid>
                <Grid item>
                  <Button
                    onClick={() => form.reset()}
                    disabled={createMutation.isPending}
                  >
                    Reset
                  </Button>
                </Grid>
              </Grid>
            ))}
          </Grid>
        </Box>
      </Paper>
    </div>
  );
}
