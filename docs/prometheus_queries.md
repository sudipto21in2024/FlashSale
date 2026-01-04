# Prometheus Query Examples for BoltTickets

## HTTP Metrics (ASP.NET Core)
- **Total HTTP Requests**: `http_server_requests_total`
- **Request Duration (95th percentile)**: `histogram_quantile(0.95, rate(http_server_duration_bucket[5m]))`
- **Request Rate**: `rate(http_server_requests_total[5m])`
- **Error Rate**: `rate(http_server_requests_total{http_status_code=~"5.."}[5m])`

## Runtime Metrics (.NET)
- **GC Collections**: `dotnet_gc_collections_total`
- **Total Memory**: `dotnet_total_memory_bytes / 1024 / 1024` (MB)
- **CPU Usage**: `rate(dotnet_process_cpu_time_total[5m]) * 100`
- **Thread Pool**: `dotnet_threadpool_threads_total`

## System Metrics
- **Target Health**: `up{job="bolttickets-api"}`
- **Scrape Duration**: `scrape_duration_seconds{job="bolttickets-api"}`

## Usage Tips
- Use `[5m]` for 5-minute windows.
- Add `{job="bolttickets-api"}` to filter.
- Create dashboards in Grafana for visualization.