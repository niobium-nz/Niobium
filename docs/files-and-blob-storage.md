# Files and Blob storage

This guide describes the [review baseline](README.md#evidence-baseline). Use `IFileService` at the application boundary and keep Azure SDK construction in `Niobium.File.Blob`. See [resource access](resource-access.md) for the separate question of who is allowed to obtain a backend credential.

## Storage contract and naming

[CloudBlobRepository](../Niobium.File.Blob/CloudBlobRepository.cs) implements `IFileService` using these meanings:

| Application argument | Blob interpretation |
| --- | --- |
| `partition` | Container name, not a Table partition or tenant claim |
| `filename` | Blob name within the container |
| `prefix` on `ListAsync` | A listing filter, not an authorization boundary |
| `tags` | Blob tags applied after upload, not part of an atomic upload-and-tag operation |

Get, Put, Tag and Delete prepend `/` to a filename that does not already start with it. Thus a supplied `tenant/template.html` is addressed as `/tenant/template.html`. Preserve this existing convention when provisioning or migrating content, and test both the SDK name and the application name. Do not silently strip the leading slash in a new adapter or provisioning tool.

A directory-looking prefix inside a container is only part of the blob name. It does not itself establish tenant ownership. Derive allowed container and filename values from trusted application state; do not let an untrusted request choose an arbitrary storage account, container or another tenant's template path.

## Composition and credential mode

[Platform Blob registration](../Niobium.Platform.Blob/DependencyModule.cs) provides `AddFile`, binds `StorageBlobOptions` for the host overload, and enables interactive identity in a development environment. `AddBlobResourceTokenSupport` separately registers identity support, the grant control and SAS issuer. Registering file access alone does not expose a resource-token route or authorize a client.

[AzureBlobClientFactory](../Niobium.File.Blob/AzureBlobClientFactory.cs) selects `DefaultAzureCredential` when the service endpoint is configured. With no server endpoint, it selects a container entitlement from `IAuthenticator` and requests a SAS. Ordinary server file operations use the service identity, not an end-user ownership policy and not `StorageBlobOptions.Key` authentication. Protect any key configured for SAS issuance as a server-only secret.

Choose an access design explicitly:

| Need | Appropriate boundary |
| --- | --- |
| Server reads private templates | Configured server endpoint, least-privilege server identity, trusted metadata/path selection |
| Client may access an entire authorized container | Resource-token flow, after reviewing the current control/issuer and client-cache limitations |
| Client may access only one blob or a virtual folder in a shared container | Server-mediated access or a separately designed narrower grant implementation; the current default issuer is not a per-file grant |

The default Blob grant is container-scoped. A requested row/id is not used as a blob-name restriction. Do not expose a shared template container to a client on the assumption that `tenant/` in a filename narrows the SAS. See [Blob grant behavior](resource-access.md#blob-grants).

## Read, create and overwrite semantics

`GetAsync` checks existence, downloads the blob, copies it into a `MemoryStream`, rewinds the stream and returns it. A missing blob returns null. The caller owns the returned stream. The current adapter buffers the whole content in memory; it is not a zero-copy streaming API or an application-level size limiter.

[`IFileService.GetAsync`, `ListAsync`, `PutAsync` and `DeleteAsync`](../Niobium.File/IFileService.cs) all default `createIfNotExist` to true. With that flag enabled, `CloudBlobRepository.GetBlobContainerAsync` calls `CreateIfNotExistsAsync` before the intended blob operation. This is not limited to reads or uploads.

**For delegated access using the framework's container-scoped service SAS, provision the container through an authorized server/infrastructure path and pass `createIfNotExist: false` to all four operations.** That SAS cannot authorize [Create Container](https://learn.microsoft.com/en-us/rest/api/storageservices/create-container). Leaving creation enabled can fail at the preliminary container request even when the credential has the intended blob permissions; do not rely on the container already existing to make that request authorized.

[`AzureBlobSignatureIssuer`](../Niobium.Platform.Blob/AzureBlobSignatureIssuer.cs) maps `FilePermissions.Create` to the Blob SAS Create permission. That permission concerns creating blobs, snapshots or copies, **not creating containers**. See the [service SAS permissions and unsupported operations](https://learn.microsoft.com/en-us/rest/api/storageservices/create-service-sas#specify-permissions). Do not broaden client credentials or disclose an account key to work around a container-creation failure.

For server-credential calls, explicitly provisioning containers and disabling creation on read-only paths is also the recommended least-privilege convention. These are calling requirements/recommendations, not changes to the adapter's defaults.

Illustrative read of an already authorized, provisioned location, with container creation disabled:

```csharp
// container and blobName must already have passed application authorization.
using Stream? content = await files.GetAsync(
    container,
    blobName,
    createIfNotExist: false,
    cancellationToken: cancellationToken);

if (content is null)
{
    throw new Niobium.ApplicationException(Niobium.InternalError.NotFound);
}

using StreamReader reader = new(content);
string template = await reader.ReadToEndAsync(cancellationToken);
```

This fragment illustrates ownership and options, not a complete host or a compiled sample project. It does not introduce a download-size limit; applications handling untrusted/large files need a deliberate limit and an adapter/API capable of enforcing it before exhausting memory.

`PutAsync` rewinds seekable input streams. Its default `replaceIfExist` is false; opt into overwrite intentionally. Upload and tag assignment are separate calls, so upload can succeed before tagging fails. `DeleteAsync` iterates filenames rather than providing an all-or-nothing multi-file transaction. `TagAsync` checks existence before setting nonempty tags. Concurrent modification and partial completion remain application concerns.

The delegated client factory compares requested permission names with individual entitlement strings. Some repository paths pass combined flags, such as Read plus Create, as one value. That can become a composite string rather than separate permissions. Test the actual least-privilege call path; do not solve a matching failure by granting every permission.

`GetPartitionsAsync` uses the server service-client path rather than the entitlement-selecting container path. Do not assume a container SAS allows account-wide container enumeration through this method.

## Notification's template pattern

[TemplateDomain](https://github.com/niobium-nz/Niobium.Notification/blob/adb9e59dd4428e1c8bf496fc40a42699bd003ba0/src/Niobium.Notification.Core/TemplateDomain.cs) reads a stored `Template`, derives the path as `<Template.Tenant>/<Template.Blob>`, and loads it through `IFileService` from `NotificationOptions.TemplateFolder`. It disposes the returned stream and uses `HtmlEncoder` when substituting supplied template values and repeated sections.

This shows useful separation: metadata and tenant selection belong to the application/domain; file transport belongs to the framework; rendering has its own output-encoding boundary. It does not show end-user direct access to the template container. The current call also uses the adapter's default container-create behavior, which should be included in deployment permission tests.

Treat template content and metadata as privileged inputs. HTML-encoding a substituted value is not the same as validating a complete template, a URL scheme or arbitrary uploaded content. The unsubscribe URL in this domain is built from email, tenant and channel; it is not a signed identity token. Review the endpoint's own policy separately.

## Development and verification checklist

Before introducing a new file feature, specify its tenant-to-container/path mapping, authorized readers/writers, overwrite/version expectations, content and size limits, and retention/deletion policy. Do not log full SAS URLs, account keys, private template contents or recipient data.

Test missing containers and blobs, read-only credentials, `createIfNotExist` on/off, leading-slash compatibility, overwrite conflicts, upload-success/tag-failure, cancellation, large content, unauthorized tenant paths and account switching. For delegated mode, also test credential expiry and SDK-client reuse: the static resource client cache is keyed by container, not every host/principal/expiry dimension.

For each of Get, List, Put and Delete, use a provisioned container and a service SAS with the intended blob permissions. Verify that `createIfNotExist: false` avoids the container-creation request; separately exercise the enabled/default path and its authorization failure. A missing container should be handled by the server/provisioning path, not by escalating the client's grant. These are recommended integration cases, not live Azure tests run by this documentation.

Keep these tests separate from template rendering tests. A fake `IFileService` can prove application path selection and stream disposal, but not backend permission scope or Azure credential refresh. See [security review](security-review-checklist.md) for related framework follow-ups.
