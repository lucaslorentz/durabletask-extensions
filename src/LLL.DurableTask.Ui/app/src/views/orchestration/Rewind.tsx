import { Button, Grid } from "@material-ui/core";
import React, { useState } from "react";
import * as yup from "yup";
import { apiAxios } from "../../apiAxios";
import { ErrorAlert } from "../../components/ErrorAlert";
import { TextField } from "../../form/fields";
import { useForm } from "../../form/form-hooks";
import { Observe } from "../../form/observation-components";
import { RewindRequest } from "../../models/ApiModels";

type Props = {
  instanceId: string;
  onRewind?: () => void;
};

const schema = yup
  .object({
    reason: yup.string().label("Reason"),
  })
  .required();

export function Rewind(props: Props) {
  const { instanceId, onRewind } = props;

  const form = useForm(schema, () => ({
    reason: "",
  }));
  const [error, setError] = useState<any>();

  async function handleSaveClick() {
    try {
      setError(undefined);

      const request: RewindRequest = {
        reason: form.value.reason,
      };

      await apiAxios.post(
        `/v1/orchestrations/${encodeURIComponent(instanceId)}/rewind`,
        request
      );

      form.reset();

      onRewind?.();
    } catch (error) {
      setError(error);
    }
  }

  return (
    <div>
      <Grid container spacing={2}>
        <Grid item xs={12}>
          {form.field("reason").render((field) => (
            <TextField field={field} multiline rows={6} />
          ))}
        </Grid>
        {error && (
          <Grid item xs={12}>
            <ErrorAlert error={error} />
          </Grid>
        )}
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
                  Rewind
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
