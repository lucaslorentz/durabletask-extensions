# LLL.DurableTask.Ui [![Nuget](https://img.shields.io/nuget/v/LLL.DurableTask.Ui)](https://www.nuget.org/packages/LLL.DurableTask.Ui/)

Beautifull UI to manage orchestrations built with React + Material UI.

Take a look at the [screenshots](../../readme/screenshots.md). History visualization is my favorite :-)

## Configuration

```C#
services.AddDurableTaskUi(options =>
{
    // Configure Durable Task UI
});
...
// Serve Durable Task Ui files under root path
app.UseDurableTaskUi();
```

Alternatively, you can define a path to serve the Ui from:

```C#
// Serve Durable Task Ui files under path /tasks
app.UseDurableTaskUi("/tasks");
```

You can configure Durable Task Ui with the following options:

| Option | Default value | Description |
| - | - | - |
| ApiBaseUrl | "/api" | The base url of Durable Task Api |
| UserNameClaims | "preferred_username", "name", "sub" | Prioritized claims used to refer to the logged in user |
| Oidc | null | Object with OIDC integration configuration. OIDC is disabled when null |

## OIDC/OAuth2 integration

You can enable OIDC integration by configuring OIDC options:

| Option | Default value | Description |
| - | - | - |
| Authority | null | The URL of the OIDC/OAuth2 provider |
| ClientId | null | Your client application's identifier as registered with the OIDC/OAuth2 provider |
| ResponseType | "id_token" | The type of response desired from the OIDC/OAuth2 provider |
| Scope | "openid" | The scope being requested from the OIDC/OAuth2 provider |
| Prompt | null | Information sent to IDP during OIDC authorization |
| Display | null | Information sent to IDP during OIDC authorization |
| LoadUserInfo | null | Flag to control if additional identity data is loaded from the user info endpoint in order to populate the user's profile. |

The **redirect_url** and **post_logout_redirect_uri** values are computed automatically from the url used to access Durable Task Ui. You should configure both redirect urls on your OIDC server with the same url you use to access Durable Task Ui.
