import { Button, Grid } from "@material-ui/core";
import React from "react";
import * as yup from "yup";
import { apiAxios } from "../../apiAxios";
import { TextField } from "../../form/fields";
import { useForm } from "../../form/form-hooks";
import { Observe } from "../../form/observation-components";
import { TerminateRequest } from "../../models/ApiModels";

type Props = {
  instanceId: string;
  onTerminate?: () => void;
};

const schema = yup
  .object({
    reason: yup.string().label("Reason"),
  })
  .required();

export function Terminate(props: Props) {
  const { instanceId, onTerminate } = props;

  const form = useForm(schema, () => ({
    reason: "",
  }));

  async function handleSaveClick() {
    const request: TerminateRequest = {
      reason: form.value.reason,
    };

    await apiAxios.post(`/v1/orchestrations/${instanceId}/terminate`, request);

    form.value = { reason: "" };

    onTerminate?.();
  }

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          {form.field("reason").render((field) => (
            <TextField field={field} multiline rows={6} />
          ))}
        </Grid>
        <Observe form={form}>
          {({ form }) => (
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
                  Terminate
                </Button>
              </Grid>
              <Grid item>
                <Button onClick={() => form.reset()}>Reset</Button>
              </Grid>
            </Grid>
          )}
        </Observe>
      </Grid>
    </div>
  );
}
