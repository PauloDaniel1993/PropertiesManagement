# API Conventions

Alsappan exposes a versioned REST API under `/v1`. These conventions apply to every module endpoint unless a later OpenSpec change explicitly narrows them.

## Routes and Names

- Use plural resource routes: `/v1/properties`, `/v1/residents`, `/v1/contracts`.
- Use English resource identifiers in API paths even when the UI route uses Portuguese slugs.
- Use endpoint names in the format `{Resource}_{Action}`, for example `Properties_List` and `System_GetInfo`.
- Use OpenAPI tags by product area: `System`, `Properties`, `Residents`, `Contracts`, `Payments`, `Documents`, `Identity`, `Settings`, `Audit`, `Timeline`, and `Notifications`.

## List Requests

List endpoints accept these common query parameters:

| Parameter | Default | Rule |
| --- | --- | --- |
| `page` | `1` | 1-based page number. |
| `pageSize` | `20` | Maximum `100` unless a module defines a lower limit. |
| `sort` | Module default | Comma-separated field names; prefix descending fields with `-`. |
| `search` | Empty | Text search over explicitly indexed fields. |

Module filters SHOULD use explicit query names such as `status`, `propertyId`, `residentId`, `dueFrom`, and `dueTo`.

## List Responses

Paged responses use a shared envelope:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 0,
  "totalPages": 0,
  "hasPreviousPage": false,
  "hasNextPage": false
}
```

## Mutations

- `POST /v1/{resources}` creates a record.
- `PUT /v1/{resources}/{id}` replaces editable fields.
- `PATCH /v1/{resources}/{id}/{action}` performs lifecycle transitions such as `archive`, `restore`, `activate`, or `mark-paid`.
- `DELETE /v1/{resources}/{id}` is reserved for soft archive semantics unless a module documents a stronger delete rule.
- Mutating endpoints MUST record audit metadata and, once the outbox exists, emit a module event envelope.

## Errors

Errors use RFC 7807 Problem Details with these extensions:

| Extension | Meaning |
| --- | --- |
| `code` | Stable application error code such as `validation`, `forbidden`, or `not_found`. |
| `traceId` | Request trace identifier for support and logs. |

Validation errors return `400` with a field error dictionary. Authorization failures return `401` or `403` with localized titles and stable `code` values.

## Localization

- `pt-BR` is the default and fallback locale.
- `en-US` is the first additional locale.
- Backend errors resolve messages from the user preference when available; until identity exists, `Accept-Language` is used.
- API payloads store canonical codes and may include localized display labels where the UI needs immediate rendering.

## OpenAPI

- The generated document is named `v1` and is available in development at `/openapi/v1.json`.
- Endpoint names, tags, success contracts, and Problem Details responses must be represented in OpenAPI.
- Bearer authentication is the planned security scheme for protected endpoints; endpoints remain anonymous only when explicitly documented as public.
- Frontend API types are generated or validated against this document once the first business endpoints exist.
