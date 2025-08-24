# LabelPrintingAgent

Label printing agent that listens to Azure Service Bus queue for print jobs.

## Configuration

The application uses `appsettings.json` for configuration. You need to set your Azure Service Bus connection string in the configuration file.

### 1. Main Configuration (appsettings.json)

```json
{
  "ServiceBus": {
    "ConnectionString": "your_service_bus_connection_string_here"
  }
}
```

### 2. Development Configuration (appsettings.Development.json)

For development environment, you can override settings in `appsettings.Development.json`:

```json
{
  "ServiceBus": {
    "ConnectionString": "your_development_service_bus_connection_string_here"
  }
}
```

## Getting Your Service Bus Connection String

1. Go to Azure Portal
2. Navigate to your Service Bus namespace
3. Go to "Shared access policies"
4. Click on "RootManageSharedAccessKey"
5. Copy the "Primary Connection String"

## Running the Application

1. Update the connection string in `appsettings.json`
2. Run the application: `dotnet run`
3. The agent will start listening to the "PrintJob" queue

## Print Job Message Format

Messages should be in the format: `type|text`

- `qrlabel|Hello World` - Prints a label with QR code and text
- `label|Hello World` - Prints a text-only label
