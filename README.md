# Nop.Plugin.AI.McpApp

Nop.Plugin.AI.McpApp is a nopCommerce plugin that exposes your store to AI agents through the Model Context Protocol (MCP). It lets an AI client connect to a nopCommerce store, authenticate with a personal access token (PAT), and call store-related tools such as searching the catalog, adding products to the cart, and viewing the shopping cart.

This plugin is maintained as a separate GitHub repository and should be downloaded or cloned first, then added into a nopCommerce project.

## Important notes

- Minimum supported nopCommerce version: 5.00
- The plugin targets .NET 10
- It uses the Model Context Protocol (MCP) over HTTP
- Authentication is PAT-based and tied to a customer account
- The MCP endpoint is exposed at `/mcp`

## Why this plugin exists

The plugin acts as a bridge between AI agents and a nopCommerce store. Instead of manually browsing the shop, an AI client can connect to the store through MCP and perform actions that are relevant to the storefront experience.

Typical use cases:
- Search products and return structured product information
- Add products to the cart
- Show the current customer cart
- Allow AI assistants to work with the store in a controlled, authenticated way

## Installation and setup

### 1. Download or clone the plugin repository

Clone the plugin repository from GitHub:

```bash
git clone <your-plugin-repo-url>
```

Or download the ZIP archive and extract it.

### 2. Add it to your nopCommerce source tree

Because this plugin project references nopCommerce projects using relative paths, the plugin should be placed inside your nopCommerce source tree under the plugins folder.

Recommended location:

```text
src/Plugins/Nop.Plugin.AI.McpApp
```

If you are using a standard nopCommerce source structure, place the cloned folder under:

```text
<your-nopcommerce-root>/src/Plugins/Nop.Plugin.AI.McpApp
```

### 3. Add the plugin project to the nopCommerce solution

Make sure the plugin project is included in your `NopCommerce.sln` solution.

### 4. Build and run

Build the solution and run your nopCommerce web application.

After deployment, the plugin will be available to the store and can be enabled from the admin area if needed.

## How the MCP app works

The working flow is simple:

1. A customer creates a PAT from the customer account area.
2. An MCP client (for example, GitHub Copilot, Cursor, or another MCP-compatible agent) sends the PAT to the nopCommerce MCP endpoint.
3. The plugin authenticates the request using the PAT header.
4. The current customer context is resolved from the token.
5. The MCP server routes the request to the appropriate tool or resource.
6. The tool executes against nopCommerce services and returns structured results to the agent.

### MCP endpoint

The plugin registers an MCP endpoint at:

```text
https://your-store-url/mcp
```

This endpoint expects the PAT in the request header:

```text
MCP-AUTH-TOKEN: <your-pat>
```

## PAT-based authentication

This plugin uses PAT-based authentication instead of session cookies for MCP requests.

### How PAT authentication works

- The plugin generates a token in the format `nop_pat_...`
- The token is created for a specific customer account
- The raw token is shown only once when it is first generated
- The token is stored securely as a hash in the database
- The plugin validates the token using a constant-time hash comparison
- The token can be revoked later
- The token is tied to the customer context so MCP tools run as that customer

### Creating a PAT

1. Sign in to the store as a customer
2. Open the MCP App section in the customer account area
3. Generate a new token
4. Copy the token immediately and store it securely

Important security notes:
- Do not share the token in chat, screenshots, or source code
- Use HTTPS in production
- Revoke tokens that are no longer needed
- Prefer one PAT per client or environment

## Plugin building blocks

The plugin is organized into a few main building blocks:

- `McpAppPlugin`  
  The plugin entry point and widget registration.

- `RouteProvider`  
  Registers the MCP endpoint and the customer-facing token management routes.

- `NopStartup`  
  Registers MCP server support, tool discovery, authentication, and CORS.

- `PatAuthenticationHandler`  
  Validates the PAT from the `MCP-AUTH-TOKEN` header and builds the customer identity.

- `PersonalAccessTokenService`  
  Creates, validates, revokes, and tracks PATs.

- `PatGenerator`  
  Generates the token format and hashing logic.

- `NopCatalogTools`  
  Exposes MCP tools such as catalog search, adding to cart, and showing the cart.

- `Views` and `Components`  
  Provide the customer-facing token management UI and account navigation integration.

## Current MCP tools

The plugin currently exposes the following MCP tools:

- `show_catalog`  
  Searches the catalog and returns structured product results.

- `add_to_cart`  
  Adds a selected product to the current customer cart.

- `show_cart`  
  Returns the current customer’s cart contents and totals.

## Configuration in AI agents

The plugin can be used with MCP-compatible agents such as GitHub Copilot in VS Code, Cursor, and similar clients.

The important requirement is that the agent must be configured to call your nopCommerce MCP endpoint and provide the PAT in the `MCP-AUTH-TOKEN` header.

### Generic MCP client configuration

A typical MCP server entry looks like this:

```json
{
  "servers": {
    "nopcommerce": {
      "type": "http",
      "url": "https://your-store.example/mcp",
      "headers": {
        "MCP-AUTH-TOKEN": "nop_pat_your_token_here"
      }
    }
  }
}
```

### VS Code / GitHub Copilot

If your VS Code or GitHub Copilot setup supports MCP server definitions, add an HTTP MCP server using the URL of your nopCommerce store's `/mcp` endpoint and include the PAT header.

Example concept:

```json
{
  "servers": {
    "nopcommerce": {
      "type": "http",
      "url": "https://your-store.example/mcp",
      "headers": {
        "MCP-AUTH-TOKEN": "nop_pat_your_token_here"
      }
    }
  }
}
```

If your client uses a workspace or user-level MCP config file, place the same definition there.

### Cursor

Cursor can also connect to HTTP-based MCP servers. Configure a new MCP server entry with:

- Server type: HTTP
- URL: `https://your-store.example/mcp`
- Header: `MCP-AUTH-TOKEN` with your PAT value

Example concept:

```json
{
  "servers": {
    "nopcommerce": {
      "type": "http",
      "url": "https://your-store.example/mcp",
      "headers": {
        "MCP-AUTH-TOKEN": "nop_pat_your_token_here"
      }
    }
  }
}
```

### Recommended setup tips

- Use a separate PAT for each AI agent or environment
- Keep the token in a secure secret store or environment variable
- Never hard-code the token into source code or public repositories
- Use HTTPS in production
- Revoke the PAT if you stop using the assistant or suspect exposure

## Example workflow

1. Create a customer account in nopCommerce
2. Generate a PAT from the MCP App page
3. Configure the MCP server in your AI client
4. Start the agent and ask it to search the catalog or add a product to the cart
5. The agent uses the PAT-authenticated MCP endpoint and performs the requested action

## Summary

Nop.Plugin.AI.McpApp is a lightweight way to connect AI agents to a nopCommerce store using the MCP standard. It is designed to be installed as a separate plugin repository, integrated into a nopCommerce 5.00+ solution, and used through PAT-based authentication.
