# Microsoft-Event-Hub-Lab

# Lab: Event Hubs Integration with Kubernetes

This lab demonstrates how to set up a Kubernetes deployment that interacts with Azure Event Hubs. The deployment includes a producer service that sends events and a consumer service that receives and logs them.

---

## Table of Contents
- [Prerequisites](#prerequisites)
- [Setup](#setup)
  - [Step 1: Create Azure Event Hubs](#step-1-create-azure-event-hubs)
  - [Step 2: Configure Kubernetes Secrets](#step-2-configure-kubernetes-secrets)
  - [Step 3: Deploy Producer Service](#step-3-deploy-producer-service)
  - [Step 4: Deploy Consumer Service](#step-4-deploy-consumer-service)
- [Files Overview](#files-overview)
- [Troubleshooting](#troubleshooting)

---

## Prerequisites

1. **Azure Account**: Access to an Azure subscription to set up Event Hubs.
2. **Kubernetes Cluster**: A running Kubernetes cluster with `kubectl` configured.
3. **Azure CLI**: Installed and authenticated.
4. **Docker**: Installed and running to build and push container images.
5. **Event Hubs Namespace**: Provisioned in Azure.
6. **.NET SDK**: Installed for building producer and consumer services.

---

## Setup

### Step 1: Create Azure Event Hubs
1. Log in to Azure:
   ```bash
   az login
   ```

2. Create a resource group:
   ```bash
   az group create --name EventHubLab --location eastus
   ```

3. Create an Event Hubs namespace:
   ```bash
   az eventhubs namespace create --name lab2-phx-eh --resource-group EventHubLab --location eastus
   ```

4. Create an Event Hub:
   ```bash
   az eventhubs eventhub create --name lab2-phx-eh-eh --namespace-name lab2-phx-eh --resource-group EventHubLab
   ```

5. Retrieve the connection string:
   ```bash
   az eventhubs namespace authorization-rule keys list --resource-group EventHubLab --namespace-name lab2-phx-eh --name RootManageSharedAccessKey
   ```

### Step 2: Configure Kubernetes Secrets
1. Create a secret to store the Event Hub connection string and name:
   ```bash
   kubectl create secret generic event-hub-credentials \ 
     --from-literal=EVENT_HUB_CONNECTION_STRING='<YourConnectionString>' \ 
     --from-literal=EVENT_HUB_NAME='lab2-phx-eh-eh'
   ```

2. Verify the secret:
   ```bash
   kubectl get secrets event-hub-credentials -o yaml
   ```

### Step 3: Deploy Producer Service
1. Build the producer Docker image:
   ```bash
   docker build -t phxlab2cr.azurecr.io/send-events:latest ./send-events
   ```

2. Push the image to your container registry:
   ```bash
   docker push phxlab2cr.azurecr.io/send-events:latest
   ```

3. Apply the producer deployment YAML:
   ```bash
   kubectl apply -f producer-deployment.yaml
   ```

4. Verify the deployment:
   ```bash
   kubectl get pods
   ```

### Step 4: Deploy Consumer Service
1. Build the consumer Docker image:
   ```bash
   docker build -t phxlab2cr.azurecr.io/receive-events:latest ./receive-events
   ```

2. Push the image to your container registry:
   ```bash
   docker push phxlab2cr.azurecr.io/receive-events:latest
   ```

3. Apply the consumer deployment YAML:
   ```bash
   kubectl apply -f consumer-deployment.yaml
   ```

4. Verify the deployment:
   ```bash
   kubectl get pods
   ```

---

## Files Overview

### YAML Files
- **producer-deployment.yaml**:
  Defines the deployment for the producer service that sends events to Event Hubs.
- **consumer-deployment.yaml**:
  Defines the deployment for the consumer service that receives events from Event Hubs.

### .NET Services
- **Producer**:
  Sends events periodically to Event Hubs.
- **Consumer**:
  Listens to events from Event Hubs and logs them.

### Secrets
- **Kubernetes Secret**:
  Contains `EVENT_HUB_CONNECTION_STRING` and `EVENT_HUB_NAME` for secure access.

### Git Configuration
- **`.gitignore`**:
  Ensures sensitive files like `.env` and temporary build files are not included in version control.

---

## Troubleshooting

### Common Issues
1. **Pods in `CrashLoopBackOff`**:
   - Check logs:
     ```bash
     kubectl logs <pod-name>
     ```
   - Ensure environment variables are correctly set in Kubernetes secrets.

2. **Unauthorized Access**:
   - Ensure the connection string in the secret has the correct permissions.

3. **Event Hubs Not Receiving Events**:
   - Verify the producer service logs to ensure events are being sent.
   - Verify the consumer service is listening to the correct Event Hub.

4. **Deployment Errors**:
   - Check the YAML for syntax issues.
   - Validate the secret and image references.

---

## Notes
- This lab demonstrates a complete flow of sending and receiving events using Azure Event Hubs in Kubernetes.
- Use role-based access control (RBAC) and Azure Managed Identities for enhanced security in production environments.
