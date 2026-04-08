# Grafana Observability Setup Guide

This guide covers the full observability stack (OpenTelemetry → Loki / Tempo / Prometheus / Mimir → Grafana) and walks through every step from first launch to using dashboards.

---

## Stack overview

```
API  ──OTLP gRPC──►  OTel Collector
                         ├── Logs    ──►  Loki   :3100
                         ├── Traces  ──►  Tempo  :3200
                         └── Metrics ──►  Prometheus :9090  ──remote_write──►  Mimir :19009
                                                                                    ▲
Grafana :3000  ◄── queries ─────────────────────────────────────────────────────────┘
                           ◄── queries Loki, Tempo
```

All datasources are **auto-provisioned** — Grafana connects to every backend on first boot with no manual setup.

---

## 1. Prerequisites

- Docker + Docker Compose installed.
- Project `.env` configured (see step 2).
- Ports free: `3000`, `3100`, `3200`, `4317`, `4318`, `8080`, `8888`, `9090`, `19009`.

> **Windows note:** Port `9009` is commonly reserved by Hyper-V on Windows 11, so Mimir is mapped to host port `19009` instead. Container-to-container communication still uses internal port `9009`.

---

## 2. Configure credentials

Copy the example env file and fill in your values:

```bash
cp .env.example .env
```

Open `.env` and set the Grafana credentials at the bottom:

```env
# --- Grafana Admin Credentials --------------------------------
GRAFANA__ADMIN_USER=admin          # change to your preferred username
GRAFANA__ADMIN_PASSWORD=admin      # change to a strong password (min 8 chars)
```

> The API validates these values at startup via `GrafanaOptions` (`[Required]`, `StringLength(8–255)` on password). Docker Compose reads them to configure `GF_SECURITY_ADMIN_USER` and `GF_SECURITY_ADMIN_PASSWORD`.

---

## 3. Start the full stack

```bash
docker compose up --build
```

Wait for all containers to become healthy. You can monitor progress:

```bash
docker compose ps
```

Expected containers and their readiness:

| Container | Host Port | Ready when |
|---|---|---|
| `delivery-system-api` | 8080 | Logs show `Application started` |
| `delivery-otel-collector` | 4317, 4318, 8888 | Starts within ~5s |
| `delivery-loki` | 3100 | Starts within ~5s |
| `delivery-tempo` | 3200, 9095 | Starts within ~10s |
| `delivery-prometheus` | 9090 | Starts within ~5s |
| `delivery-mimir` | **19009** | Healthcheck passes (~30s) |
| `delivery-grafana` | 3000 | Starts within ~15s |

---

## 4. Log in to Grafana

Open [http://localhost:3000](http://localhost:3000) in your browser.

- **Username:** value of `GRAFANA__ADMIN_USER` from your `.env`
- **Password:** value of `GRAFANA__ADMIN_PASSWORD` from your `.env`

On first login Grafana will prompt you to change the default password if you left it as `admin`. You can skip this or set a new one — it only affects the UI session, not the `.env` file.

---

## 5. Verify datasources

All four datasources are auto-provisioned from
[`docker/observability/grafana/provisioning/datasources/datasources.yaml`](../../docker/observability/grafana/provisioning/datasources/datasources.yaml).

Confirm they are all working:

1. Go to **Home → Connections → Data sources**
2. You should see: **Prometheus** (default), **Mimir**, **Loki**, **Tempo**
3. Click each one and press **Save & test** — all should return a green confirmation

If any datasource shows an error, wait 30 more seconds and retry (Mimir takes the longest to initialize).

---

## 6. Explore logs (Loki)

1. Go to **Explore** (compass icon in the left sidebar)
2. Select **Loki** from the datasource dropdown
3. Use the **Label browser** or type a LogQL query:

```logql
# All API logs
{service_name="delivery-system-api"}

# Only errors
{service_name="delivery-system-api"} | json | level="error"

# Filter by route template
{service_name="delivery-system-api"} | json | RoutePattern="/api/auth/login"

# Search by HTTP status code
{service_name="delivery-system-api"} | json | StatusCode="401"
```

> **PII guarantee:** Emails, passwords, tokens, and GUIDs from URL paths are never present in logs.
> `RequestTracingMiddleware` logs route templates (`/api/orders/{id}`), not actual paths.

---

## 7. Explore traces (Tempo)

1. Go to **Explore** → select **Tempo**
2. Use **Search** mode to find traces:
   - **Service name:** `delivery-system-api`
   - **Span name:** e.g. `POST /api/auth/login`
   - **Duration:** filter slow requests, e.g. `> 200ms`
3. Click any trace to open the **waterfall view** — shows every span:
   - HTTP server span (root)
   - Entity Framework Core queries
   - Redis cache reads/writes
   - HttpClient calls (reCAPTCHA, Google OAuth, Resend)

### Test W3C Trace Context propagation

Send a request with a `traceparent` header to verify child span creation:

```bash
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -H "traceparent: 00-0af7651916cd43dd8448eb211c80319c-b7ad6b7169203331-01" \
  -d '{"email":"test@example.com","password":"Test@123","captchaToken":"fake"}'
```

In Tempo, search for trace ID `0af7651916cd43dd8448eb211c80319c` — the API span will appear as a **child** of your injected parent.

---

## 8. Explore metrics (Prometheus / Mimir)

1. Go to **Explore** → select **Prometheus** (real-time) or **Mimir** (long-term storage)
2. Useful PromQL queries:

```promql
# HTTP request rate per route (requests/sec over 5m)
rate(http_server_request_duration_seconds_count[5m])

# Average request duration per route
rate(http_server_request_duration_seconds_sum[5m])
/ rate(http_server_request_duration_seconds_count[5m])

# 95th percentile latency
histogram_quantile(0.95, rate(http_server_request_duration_seconds_bucket[5m]))

# Error rate (4xx + 5xx)
rate(http_server_request_duration_seconds_count{http_response_status_code=~"4..|5.."}[5m])

# .NET runtime: GC collections
rate(process_runtime_dotnet_gc_collections_total[5m])

# .NET runtime: heap size (bytes)
process_runtime_dotnet_gc_heap_size_bytes
```

---

## 9. Trace ↔ Log correlation

Grafana links traces and logs automatically using the `TraceId` field.

### From a log line → trace

1. In **Explore → Loki**, run any query
2. Expand a log line that was emitted during an HTTP request
3. A **"View Trace in Tempo"** button appears — click to jump directly to the trace

### From a trace span → logs

1. In **Explore → Tempo**, open any trace
2. Click a span → in the detail panel click **"Logs for this span"**
3. Grafana opens Loki filtered to `±1 minute` around that span with the matching `TraceId`

### From a metric exemplar → trace

1. In **Explore → Prometheus**, run a histogram query
2. Enable the **Exemplars** toggle (top right of the graph)
3. Dots on the graph represent individual requests — click one to jump to its trace in Tempo

---

## 10. Recommended dashboards

Import these from the Grafana dashboard library (**Home → Dashboards → New → Import**):

| Dashboard | ID | Datasource |
|---|---|---|
| ASP.NET Core | `19925` | Prometheus |
| .NET Runtime | `19924` | Prometheus |
| OpenTelemetry Collector | `15983` | Prometheus |
| Loki Logs | `13639` | Loki |

**How to import:**
1. **Home → Dashboards → New → Import**
2. Enter the ID above → click **Load**
3. Select the matching datasource → click **Import**

---

## 11. Troubleshooting

### Grafana shows "Data source not found"
Wait 30–60 seconds for all containers to finish initializing (Mimir is slowest), then reload.

### No logs in Loki
```bash
docker logs delivery-system-api 2>&1 | grep -i "otel\|otlp\|export"
docker logs delivery-otel-collector
```

### No traces in Tempo
Check that the Collector is receiving spans:
```bash
curl http://localhost:8888/metrics | grep otelcol_receiver_accepted_spans
```

### Mimir not ready
```bash
curl http://localhost:19009/ready
```
Returns `ready` when the ring has stabilized (~30–60s on first boot).

### Port conflict on Windows (Mimir)
If `19009` is also reserved, check available ports and pick a free one:
```bash
netsh interface ipv4 show excludedportrange protocol=tcp
```
Update `docker-compose.yml` and `grafana/provisioning/datasources/datasources.yaml` accordingly.

### Reset all observability data
```bash
docker compose down -v   # removes named volumes (loki-data, tempo-data, etc.)
docker compose up --build
```
> This does **not** affect `sqlserver-data` — application data is preserved.

---

## 12. Changing Grafana admin password

Update `.env`:
```env
GRAFANA__ADMIN_PASSWORD=NewStr0ng!Password
```

Then restart only the Grafana container:
```bash
docker compose restart grafana
```

The API will also pick up the new value on next restart, since `GrafanaOptions` is validated at startup.
