# AKS Cluster with Azure Event Hub and KEDA using Pod Identity and Workload Identity

This guide provides step-by-step instructions to set up an AKS cluster with Azure Event Hub and KEDA, leveraging Azure Managed Identity, pod identity, and workload identity for secure event processing.
---

## Pre-Steps

### 1. Install Azure CLI

- Download and install the Azure CLI from the official [Azure CLI installation guide](https://learn.microsoft.com/en-us/cli/azure/install-azure-cli).

### 2. Login to Azure

- Login to your Azure account:

```bash
az login
```

- If you're using a service principal for automation, use:

```bash
az login --service-principal --username <app-id> --password <password> --tenant <tenant-id>
```

### 3. Create a Subscription (Optional, if not already created)

- Check your existing subscriptions:

```bash
az account list --output table
```

- If needed, create a new subscription from the Azure Portal or CLI. For CLI-based subscription creation, refer to Azure documentation [here](https://learn.microsoft.com/en-us/azure/cost-management-billing/manage/create-subscription).

### 4. Set the Subscription

- Set the subscription you want to use:

```bash
az account set --subscription <subscription-id>
```

### 5. Create a Resource Group

- Create a resource group to organize your resources:

```bash
az group create --name <resource-group-name> --location <region>
```

---

## Steps

### 1. Create an AKS Cluster

- Deploy an AKS cluster with OpenID Connect (OIDC) enabled for workload identity:

```bash
az aks create --resource-group <resource-group> \
  --name <aks-cluster-name> \
  --enable-oidc-issuer \
  --node-count 3
```
---

### 2. Create Event Hub Namespace, Event Hub, Azure Storage, and Container Registry

- Create an **Event Hub namespace**:

```bash
az eventhubs namespace create --name <event-hub-namespace> \
  --resource-group <resource-group> --location <region>
```

- Add an **Event Hub**:

```bash
az eventhubs eventhub create --resource-group <resource-group> \
  --namespace-name <event-hub-namespace> --name <event-hub-name>
```

- Create a **Storage Account**:

```bash
az storage account create --name <storage-account-name> \
  --resource-group <resource-group> --location <region>
```

- Create a Blob Container:

```bash
az storage container create --account-name <storage-account-name> --name <blob-container-name>
```

- Bind the Event Hub to the Blob Container for checkpointing:

```bash
az eventhubs eventhub update --resource-group <resource-group> \
  --namespace-name <event-hub-namespace> \
  --name <event-hub-name> \
  --enable-capture true \
  --capture-destination-name <blob-container-name> \
  --capture-storage-account <storage-account-name>
```

- Create a **Container Registry**:

```bash
az acr create --name <acr-name> \
  --resource-group <resource-group> \
  --sku Basic
```

---

### 3. Create a Managed Identity and Assign Permissions

- Create a **Managed Identity**:

```bash
az identity create --name <managed-identity-name> --resource-group <resource-group>
```

- Assign the **Azure Event Hubs Data Receiver** role for consuming events:

```bash
az role assignment create --assignee <client-id> \
  --role "Azure Event Hubs Data Receiver" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.EventHub/namespaces/<event-hub-namespace>"
```

- Assign the **Azure Event Hubs Data Sender** role for sending events:

```bash
az role assignment create --assignee <client-id> \
  --role "Azure Event Hubs Data Sender" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.EventHub/namespaces/<event-hub-namespace>"
```

- Assign the **Storage Blob Data Reader** role for checkpointing:

```bash
az role assignment create --assignee <client-id> \
  --role "Storage Blob Data Reader" \
  --scope "/subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.Storage/storageAccounts/<storage-account-name>"
```

---

### 4. Push Images to a Container Registry

- Tag and push the sender and receiver images to Azure Container Registry (ACR):

```bash
az acr login --name <acr-name>
docker tag receive-events:latest <acr-name>.azurecr.io/receive-events:latest
docker tag send-events:latest <acr-name>.azurecr.io/send-events:latest
docker push <acr-name>.azurecr.io/receive-events:latest
docker push <acr-name>.azurecr.io/send-events:latest
```

---

### 5. Develop and Deploy Sender and Receiver Services

#### Develop Services in C#

1. **Producer (Sender)**:

   - Create a C# application to produce messages to the Event Hub.
   - Use the Azure SDK for Event Hubs to implement the producer logic.

   Example:

   ```csharp
   var producerClient = new EventHubProducerClient(new DefaultAzureCredential(), "<event-hub-namespace>", "<event-hub-name>");
   using EventDataBatch eventBatch = await producerClient.CreateBatchAsync();
   eventBatch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Message")));
   await producerClient.SendAsync(eventBatch);
   ```

2. **Consumer (Receiver)**:

   - Create a C# application to consume messages from the Event Hub.
   - Use the Azure SDK for Event Hubs and DefaultAzureCredential for Managed Identity.

   Example:

   ```csharp
   var processor = new EventProcessorClient(
       new BlobContainerClient(new DefaultAzureCredential(), "<storage-account-name>", "<blob-container-name>"),
       "$Default",
       "<event-hub-namespace>",
       "<event-hub-name>");
   processor.ProcessEventAsync += EventHandler;
   processor.ProcessErrorAsync += ErrorHandler;
   await processor.StartProcessingAsync();
   ```

#### Deploy to AKS

#### Receiver Deployment YAML:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: receive-events-deployment
  labels:
    app: receive-events
spec:
  replicas: 1
  selector:
    matchLabels:
      app: receive-events
  template:
    metadata:
      labels:
        app: receive-events
        aadpodidbinding: events-identity
    spec:
      containers:
      - name: receive-events
        image: <container-registry>/receive-events:latest
        ports:
        - containerPort: 80
        env:
        - name: EVENT_HUB_NAMESPACE
          value: <event-hub-namespace>
        - name: EVENT_HUB_NAME
          value: <event-hub-name>
        - name: CONSUMER_GROUP
          value: "$Default"
```

#### Sender Deployment YAML:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: send-events-deployment
  labels:
    app: send-events
spec:
  replicas: 1
  selector:
    matchLabels:
      app: send-events
  template:
    metadata:
      labels:
        app: send-events
        aadpodidbinding: events-identity
    spec:
      containers:
      - name: send-events
        image: <container-registry>/send-events:latest
        ports:
        - containerPort: 80
        env:
        - name: EVENT_HUB_NAMESPACE
          value: <event-hub-namespace>
        - name: EVENT_HUB_NAME
          value: <event-hub-name>
        - name: EVENT_SEND_DELAY
          value: "5"
```

Apply these deployments:

```bash
kubectl apply -f receive-events-deployment.yaml
kubectl apply -f send-events-deployment.yaml
```

---

### 6. Configure Azure Identity for Pod Identity

#### Create AzureIdentity YAML:

```yaml
apiVersion: "aadpodidentity.k8s.io/v1"
kind: AzureIdentity
metadata:
  name: events-identity
  namespace: default
spec:
  type: 0
  resourceID: /subscriptions/<subscription-id>/resourceGroups/<resource-group>/providers/Microsoft.ManagedIdentity/userAssignedIdentities/<managed-identity-name>
  clientID: <client-id>
```

#### Create AzureIdentityBinding YAML:

```yaml
apiVersion: "aadpodidentity.k8s.io/v1"
kind: AzureIdentityBinding
metadata:
  name: events-identity-binding
  namespace: default
spec:
  azureIdentity: events-identity
  selector: events-identity
```

Apply these resources:

```bash
kubectl apply -f azure-identity.yaml
kubectl apply -f azure-identity-binding.yaml
```

---

### 7. Deploy KEDA and Configure Scaling

#### Install KEDA:

```bash
helm install keda kedacore/keda --namespace keda --create-namespace --set podIdentity.activeDirectory.identity=<managed-identity-name>
```

#### Create Federated Identity Credential for KEDA:

```bash
az identity federated-credential create \
  --name keda-trigger-auth \
  --identity-name <managed-identity-name> \
  --resource-group <resource-group> \
  --issuer "<oidc-issuer-url>" \
  --subject "system:serviceaccount:kube-system:keda-operator"
```

#### TriggerAuthentication YAML:

```yaml
apiVersion: keda.sh/v1alpha1
kind: TriggerAuthentication
metadata:
  name: receive-events-auth
  namespace: default
spec:
  podIdentity:
    identityId: <client-id>
    provider: azure-workload
```

#### ScaledObject YAML:

```yaml
apiVersion: keda.sh/v1alpha1
kind: ScaledObject
metadata:
  name: receive-events-scaler
  namespace: default
spec:
  scaleTargetRef:
    name: receive-events-deployment
  pollingInterval: 30
  cooldownPeriod: 300
  minReplicaCount: 1
  maxReplicaCount: 10
  triggers:
    - type: azure-eventhub
      metadata:
        eventHubNamespace: <event-hub-namespace>
        eventHubName: <event-hub-name>
        consumerGroup: "$Default"
        threshold: "200"
        storageAccountName: <storage-account-name>
        blobContainer: <blob-container-name>
      authenticationRef:
        name: receive-events-auth
```

Apply these resources:

```bash
kubectl apply -f trigger-authentication.yaml
kubectl apply -f scaledobject.yaml
```

---

### 8. Test and Verify

- Verify the deployments:

```bash
kubectl get pods --namespace default
```

- Check KEDA operator logs for scaling events:

```bash
kubectl logs deployment/keda-operator --namespace keda
```

- Simulate traffic with the sender service and observe the scaling behavior of the receiver pods:

```bash
kubectl get hpa --namespace default
kubectl get pods --namespace default
```

