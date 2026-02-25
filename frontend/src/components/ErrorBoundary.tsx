import { Component, type ErrorInfo, type ReactNode } from "react";
import { Alert, AlertTitle, Box, Button } from "@mui/material";

interface Props {
  children: ReactNode;
}

interface State {
  hasError: boolean;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false };
  }

  static getDerivedStateFromError(): State {
    return { hasError: true };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error("ErrorBoundary caught an error:", error, info);
  }

  render() {
    if (this.state.hasError) {
      return (
        <Box
          sx={{
            display: "flex",
            justifyContent: "center",
            alignItems: "center",
            flexGrow: 1,
            p: 3,
          }}
        >
          <Alert
            severity="error"
            sx={{ maxWidth: 500, width: "100%" }}
            action={
              <Button
                color="inherit"
                size="small"
                onClick={() => window.location.reload()}
              >
                Reload page
              </Button>
            }
          >
            <AlertTitle>Something went wrong</AlertTitle>
            An unexpected error occurred. Please try reloading the page.
          </Alert>
        </Box>
      );
    }

    return this.props.children;
  }
}
