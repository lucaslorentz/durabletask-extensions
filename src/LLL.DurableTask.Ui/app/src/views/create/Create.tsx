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
import React, { useState } from "react";
import { Link as RouterLink, useHistory } from "react-router-dom";
import * as yup from "yup";
import { useForm } from "../../form/form-hooks";
import { Observe } from "../../form/observation-components";
import { TextField } from "../../form/fields";
import {
  CreateOrchestrationRequest,
  OrchestrationInstance,
} from "../../models/ApiModels";
import { apiAxios } from "../../apiAxios";
import { ErrorAlert } from "../../components/ErrorAlert";
import { useSnackbar } from "notistack";

const schema = yup
  .object({
    name: yup.string().label("Name").required(),
    version: yup.string().label("Version"),
    instanceId: yup.string().label("Instance Id"),
    input: yup.string().label("Input").json(),
    tags: yup
      .array(
        yup
          .object({
            key: yup.string().label("Key").required(),
            value: yup.string().label("Value").required(),
          })
          .required()
      )
      .defined(),
  })
  .required();

export function Create() {
  const form = useForm(schema, () => ({
    name: "",
    version: "",
    instanceId: "",
    input: "",
    tags: [],
  }));
  const [error, setError] = useState<any>();

  const history = useHistory();
  const { enqueueSnackbar } = useSnackbar();

  async function handleSaveClick() {
    try {
      setError(undefined);

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

      var response = await apiAxios.post<OrchestrationInstance>(
        `/v1/orchestrations`,
        request
      );

      enqueueSnackbar("Orchestration created", {
        variant: "success",
      });

      history.push(
        `/orchestrations/${encodeURIComponent(response.data.instanceId)}`
      );
    } catch (error) {
      setError(error);
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
              {form.field("name").render((field) => (
                <TextField field={field} />
              ))}
            </Grid>
            <Grid item xs={6}>
              {form.field("version").render((field) => (
                <TextField field={field} />
              ))}
            </Grid>
            <Grid item xs={6}>
              {form.field("instanceId").render((field) => (
                <TextField field={field} />
              ))}
            </Grid>
            <Grid item xs={6}></Grid>
            <Grid item xs={12}>
              {form.field("input").render((field) => (
                <TextField field={field} multiline rows={6} />
              ))}
            </Grid>
            <Grid item xs={12}>
              <Button
                onClick={() => form.field("tags").push({ key: "", value: "" })}
              >
                Add tag
              </Button>
            </Grid>
            {form.field("tags").render((field) =>
              field.map((tag, index) => (
                <Grid key={index} item xs={12}>
                  <Box display="flex">
                    <Box marginX={1} flex={1}>
                      {tag.field("key").render((field) => (
                        <TextField field={field} />
                      ))}
                    </Box>
                    <Box marginX={1} flex={1}>
                      {tag.field("value").render((field) => (
                        <TextField field={field} />
                      ))}
                    </Box>
                    <Box>
                      <IconButton onClick={() => field.remove(tag.value)}>
                        <DeleteIcon />
                      </IconButton>
                    </Box>
                  </Box>
                </Grid>
              ))
            )}
            {error && (
              <Grid item xs={12}>
                <ErrorAlert error={error} />
              </Grid>
            )}
            <Observe form={form}>
              {({ form }) => (
                <Grid
                  item
                  xs={12}
                  container
                  spacing={1}
                  justify="space-between"
                >
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
              )}
            </Observe>
          </Grid>
        </Box>
      </Paper>
    </div>
  );
}
