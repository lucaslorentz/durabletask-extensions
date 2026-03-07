import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  TextField,
} from "@mui/material";
import React, { useState } from "react";

interface ReasonDialogProps {
  open: boolean;
  title: string;
  description: string;
  onClose: () => void;
  onConfirm: (reason: string) => void;
}

export function ReasonDialog({
  open,
  title,
  description,
  onClose,
  onConfirm,
}: ReasonDialogProps) {
  const [reason, setReason] = useState("");

  const handleConfirm = () => {
    const value = reason;
    setReason("");
    onConfirm(value);
  };

  const handleClose = () => {
    setReason("");
    onClose();
  };

  return (
    <Dialog
      open={open}
      onClose={handleClose}
      fullWidth
      maxWidth="sm"
      disableRestoreFocus
    >
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>
        <DialogContentText sx={{ mb: 2 }}>{description}</DialogContentText>
        <TextField
          autoFocus
          label="Reason"
          fullWidth
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          onKeyDown={(e) => {
            if (e.key === "Enter") {
              e.preventDefault();
              handleConfirm();
            }
          }}
        />
      </DialogContent>
      <DialogActions>
        <Button onClick={handleClose}>Cancel</Button>
        <Button variant="contained" onClick={handleConfirm}>
          {title}
        </Button>
      </DialogActions>
    </Dialog>
  );
}
