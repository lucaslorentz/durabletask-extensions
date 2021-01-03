import {
  Box,
  Breadcrumbs,
  Button,
  Grid,
  IconButton,
  Link,
  Paper,
  Typography,
} from "@material-ui/core";
import DeleteIcon from "@material-ui/icons/Delete";
import { useSnackbar } from "notistack";
import React from "react";
import { Link as RouterLink, useHistory } from "react-router-dom";
import * as yup from "yup";
import { useApiClient } from "../../ApiClientProvider";
import { TextField } from "../../form/TextField";
import { useForm } from "../../form/useForm";
import { CreateOrchestrationRequest } from "../../models/ApiModels";

const schema = yup
  .object({
    name: yup.string().label("Name").default("").required(),
    version: yup.string().label("Version").default(""),
    instanceId: yup.string().label("Instance Id").default(""),
    input: yup.string().label("Input").default("").json(),
    tags: yup
      .array(
        yup
          .object({
            key: yup.string().label("Key").required(),
            value: yup.string().label("Value").required(),
          })
          .required()
      )
      .default(() => [])
      .defined(),
  })
  .required();

export function Create() {
  const form = useForm(schema);
  const history = useHistory();
  const apiClient = useApiClient();
  const { enqueueSnackbar, closeSnackbar } = useSnackbar();

  async function handleSaveClick() {
    try {
      const request: CreateOrchestrationRequest = {
        name: form.value.name,
        version: form.value.version,
        instanceId: form.value.instanceId,
        input: form.value.input ? JSON.parse(form.value.input) : null,
        tags: form.value.tags.reduce((previous, current) => {
          previous[current.key] = current.value;
          return previous;
        }, {} as Record<string, string>),
      };

      const instance = await apiClient.createOrchestration(request);

      enqueueSnackbar("Orchestration created", {
        variant: "success",
      });

      history.push(
        `/orchestrations/${encodeURIComponent(instance.instanceId)}`
      );
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
  }

  return (
    <div>
      <Box marginBottom={2}>
        <Breadcrumbs aria-label="breadcrumb">
          <Link component={RouterLink} to="/orchestrations">
            Orchestrations
          </Link>
          <Typography color="textPrimary">Create</Typography>
        </Breadcrumbs>
      </Box>
      <Paper variant="outlined">
        <Box padding={2}>
          <Grid container spacing={2}>
            <Grid item xs={6}>
              <TextField field={form.field("name")} />
            </Grid>
            <Grid item xs={6}>
              <TextField field={form.field("version")} />
            </Grid>
            <Grid item xs={6}>
              <TextField field={form.field("instanceId")} />
            </Grid>
            <Grid item xs={6}></Grid>
            <Grid item xs={12}>
              <TextField field={form.field("input")} multiline rows={6} />
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
                      <Box display="flex">
                        <Box marginX={1} flex={1}>
                          <TextField field={tagField.field("key")} />
                        </Box>
                        <Box marginX={1} flex={1}>
                          <TextField field={tagField.field("value")} />
                        </Box>
                        <Box>
                          <IconButton
                            onClick={() => field.remove(tagField.value)}
                          >
                            <DeleteIcon />
                          </IconButton>
                        </Box>
                      </Box>
                    </Grid>
                  ))
                )}
              </>
            )}
            {form.render((form) => (
              <Grid item xs={12} container spacing={1} justify="space-between">
                <Grid item>
                  <Button
                    variant="contained"
                    color="primary"
                    onClick={handleSaveClick}
                    disabled={
                      form.pendingValidation ||
                      Object.keys(form.errors).length > 0
                    }
                  >
                    Create
                  </Button>
                </Grid>
                <Grid item>
                  <Button onClick={() => form.reset()}>Reset</Button>
                </Grid>
              </Grid>
            ))}
          </Grid>
        </Box>
      </Paper>
    </div>
  );
}
