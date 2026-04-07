# Observability Guide — Delivery System

This guide covers the full observability stack (OpenTelemetry → Loki / Tempo / Prometheus / Mimir → Grafana) and walks through every step from first launch to using dashboards.

---

## Stack overview

```
API  ──OTLP gRPC──►  OTel Collector
                         ├── Logs    ──►  Loki   :3100
                         ├── Traces  ──►  Tempo  :3200
                         └── Metrics ──►  Prometheus :9090  ──remote_write──►  Mimir :9009
                                                                                    ▲
Grafana :3000  ◄── queries ─────────────────────────────────────────────────────────┘
                           ◄── queries Loki, Tempo
```

All datasources are **auto-provisioned** — Grafana connects to every backend on first boot with no manual setup.

---

## 1. Prerequisites

- Docker + Docker Compose installed.
- Project `.env` configured (see step 2).

---

## 2. Configure credentials

Copy the example env file and fill in your values:

```bash
cp .env.example .env
```

Open `.env` and set the Grafana credentials — find the section at the bottom:

```env
# --- Grafana Admin Credentials --------------------------------
GRAFANA__ADMIN_USER=admin          # change to your preferred username
GRAFANA__ADMIN_PASSWORD=admin      # change to a strong password (min 8 chars)
```

> **Security note:** The values in `.env` are validated at API startup via `GrafanaOptions`
> (`[Required]`, `StringLength(8–255)` on password). Docker Compose uses these same values to
> configure Grafana's `GF_SECURITY_ADMIN_USER` and `GF_SECURITY_ADMIN_PASSWORD`.

---

## 3. Start the full stack

```bash
docker compose up --build
```

Wait for all containers to become healthy. You can monitor progress:

```bash
docker compose ps
```

Expected healthy containers:
| Container | Port | Ready when |
|---|---|---|
| `delivery-system-api` | 8080 | Logs show `Application started` |
| `delivery-otel-collector` | 4317, 4318, 8888 | Starts within ~5s |
| `delivery-loki` | 3100 | Starts within ~5s |
| `delivery-tempo` | 3200, 9095 | Starts within ~10s |
| `delivery-prometheus` | 9090 | Starts within ~5s |
| `delivery-mimir` | 9009 | Healthcheck passes (~30s) |
| `delivery-grafana` | 3000 | Starts within ~15s |

---

## 4. Log in to Grafana

Open [http://localhost:3000](http://localhost:3000) in your browser.

- **Username:** value of `GRAFANA__ADMIN_USER` from your `.env`
- **Password:** value of `GRAFANA__ADMIN_PASSWORD` from your `.env`

On first login Grafana will prompt you to change the default password if you left it as `admin`.
You can skip this or set a new one — it only affects the UI, not the `.env` file.

---

## 5. Verify datasources

All four datasources are auto-provisioned from
[`docker/observability/grafana/provisioning/datasources/datasources.yaml`](../docker/observability/grafana/provisioning/datasources/datasources.yaml).

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

# Filter by route
{service_name="delivery-system-api"} | json | RoutePattern="/api/auth/login"

# Search for a specific status code
{service_name="delivery-system-api"} | json | StatusCode="401"
```

> **PII guarantee:** Emails, passwords, tokens, and GUIDs from URL paths are never present in logs.
> The `RequestTracingMiddleware` logs route templates (`/api/orders/{id}`), not actual paths.

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

In Tempo, search for trace ID `0af7651916cd43dd8448eb211c80319c` — you will see the API span as a **child** of your injected parent.

---

## 8. Explore metrics (Prometheus / Mimir)

1. Go to **Explore** → select **Prometheus** (real-time) or **Mimir** (long-term)
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

# .NET runtime: working set memory (bytes)
process_runtime_dotnet_gc_heap_size_bytes
```

---

## 9. Trace ↔ Log correlation

Grafana links traces and logs automatically using the `TraceId` field.

### From a log line → trace

1. In **Explore → Loki**, run any query
2. Expand a log line
3. If the log was emitted during an active HTTP request, a **"View Trace in Tempo"** button appears
4. Click it to jump directly to the full trace in Tempo

### From a trace span → logs

1. In **Explore → Tempo**, open any trace
2. Click a span
3. In the span details panel, click **"Logs for this span"**
4. Grafana opens Loki filtered to `±1 minute` around that span and the matching `TraceId`

### From a metric exemplar → trace

1. In **Explore → Prometheus**, run a histogram query
2. Enable **Exemplars** toggle (top right of the graph)
3. Dots on the graph represent individual requests — click one to jump to the trace in Tempo

---

## 10. Recommended dashboards

Import these from the Grafana dashboard library (**Home → Dashboards → Import**):

| Dashboard | ID | Purpose |
|---|---|---|
| ASP.NET Core | `19925` | HTTP request rates, errors, durations |
| .NET Runtime | `19924` | GC, heap, thread pool, CPU |
| OpenTelemetry Collector | `15983` | Collector throughput and pipeline health |
| Loki Logs | `13639` | Log volume, error rates by label |

**How to import:**
1. Go to **Home → Dashboards → New → Import**
2. Enter the dashboard ID above
3. Select the matching datasource (Prometheus for metric dashboards, Loki for log dashboards)
4. Click **Import**

---

## 11. Troubleshooting

### Grafana shows "Data source not found"
Wait 30–60 seconds for all containers to finish initializing, then reload the page. Mimir takes the longest.

### No logs appearing in Loki
Check that the API container started and is sending telemetry:
```bash
docker logs delivery-system-api | grep -i "otel\|telemetry\|export"
docker logs delivery-otel-collector
```

### No traces in Tempo
Verify the Collector is receiving data:
```bash
# Collector self-metrics — look for received_spans > 0
curl http://localhost:8888/metrics | grep otelcol_receiver_accepted_spans
```

### Mimir healthcheck failing
```bash
curl http://localhost:9009/ready
```
If it returns anything other than `ready`, wait for memberlist ring to stabilize (~60s on first boot).

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

The API will also pick up the new value on restart, since `GrafanaOptions` is validated at startup.
