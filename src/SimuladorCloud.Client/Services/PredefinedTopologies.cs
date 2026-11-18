using SimuladorCloud.Client.Models;

namespace SimuladorCloud.Client.Services;

/// <summary>
/// Topologías predefinidas grande (50 nodos) y compleja (100 nodos).
/// IT-6 · HU-11: Escenarios de prueba predefinidos.
/// Las topologías se definen con listas explícitas de nodos y dependencias
/// (sin generación aleatoria), para que los resultados sean reproducibles y
/// puedan verificarse manualmente.
/// </summary>
public partial class GraphService
{
    private const double LayoutSpacingX = 105;
    private const double LayoutSpacingY = 100;
    private const double LayoutMargin   = 60;

    /// <summary>
    /// Topología grande: 50 nodos — plataforma de comercio electrónico con
    /// dos balanceadores, 18 microservicios, 12 almacenes de datos, colas,
    /// APIs externas, almacenamiento y observabilidad.
    /// </summary>
    private static GraphModel CreateLargeTopology()
    {
        var nodes = new (string Name, NodeType Type)[]
        {
            ("API Gateway", NodeType.API),
            ("Load Balancer Norte", NodeType.LoadBalancer), ("Load Balancer Sur", NodeType.LoadBalancer),
            ("Auth Service", NodeType.Service), ("User Service", NodeType.Service), ("Product Service", NodeType.Service),
            ("Catalog Service", NodeType.Service), ("Cart Service", NodeType.Service), ("Order Service", NodeType.Service),
            ("Payment Service", NodeType.Service), ("Shipping Service", NodeType.Service), ("Inventory Service", NodeType.Service),
            ("Pricing Service", NodeType.Service), ("Review Service", NodeType.Service), ("Recommendation Service", NodeType.Service),
            ("Search Service", NodeType.Service), ("Notification Service", NodeType.Service), ("Email Service", NodeType.Service),
            ("Promotion Service", NodeType.Service), ("Wishlist Service", NodeType.Service), ("Analytics Service", NodeType.Service),
            ("Session Cache", NodeType.Service), ("Product Cache", NodeType.Service), ("Order Queue", NodeType.Service),
            ("Event Bus", NodeType.Service), ("Email Queue", NodeType.Service),
            ("Payment Gateway API", NodeType.API), ("Shipping Carrier API", NodeType.API),
            ("SMS Provider API", NodeType.API), ("Maps API", NodeType.API),
            ("Auth DB", NodeType.Database), ("User DB", NodeType.Database), ("Product DB", NodeType.Database),
            ("Cart DB", NodeType.Database), ("Order DB", NodeType.Database), ("Payment DB", NodeType.Database),
            ("Shipping DB", NodeType.Database), ("Inventory DB", NodeType.Database), ("Review DB", NodeType.Database),
            ("Search Index", NodeType.Database), ("Promotion DB", NodeType.Database), ("Analytics DB", NodeType.Database),
            ("Image Storage", NodeType.Storage), ("Invoice Storage", NodeType.Storage),
            ("Backup Storage", NodeType.Storage), ("Log Archive", NodeType.Storage),
            ("Log Service", NodeType.Service), ("Metrics Service", NodeType.Service),
            ("Tracing Service", NodeType.Service), ("Alert Manager", NodeType.Service),
        };

        var edges = new (string From, string To)[]
        {
            ("API Gateway", "Load Balancer Norte"), ("API Gateway", "Load Balancer Sur"), ("API Gateway", "Auth Service"),
            ("Load Balancer Norte", "User Service"), ("Load Balancer Norte", "Product Service"), ("Load Balancer Norte", "Catalog Service"),
            ("Load Balancer Norte", "Cart Service"), ("Load Balancer Norte", "Order Service"), ("Load Balancer Norte", "Payment Service"),
            ("Load Balancer Norte", "Shipping Service"), ("Load Balancer Norte", "Inventory Service"), ("Load Balancer Norte", "Pricing Service"),
            ("Load Balancer Sur", "Review Service"), ("Load Balancer Sur", "Recommendation Service"), ("Load Balancer Sur", "Search Service"),
            ("Load Balancer Sur", "Notification Service"), ("Load Balancer Sur", "Promotion Service"), ("Load Balancer Sur", "Wishlist Service"),
            ("Load Balancer Sur", "Analytics Service"), ("Load Balancer Sur", "Product Service"),
            ("Auth Service", "Auth DB"), ("Auth Service", "Session Cache"),
            ("User Service", "User DB"), ("User Service", "Auth Service"),
            ("Product Service", "Product DB"), ("Product Service", "Product Cache"), ("Product Service", "Image Storage"),
            ("Catalog Service", "Product Service"), ("Catalog Service", "Search Index"),
            ("Cart Service", "Cart DB"), ("Cart Service", "Product Service"), ("Cart Service", "Pricing Service"), ("Cart Service", "Session Cache"),
            ("Order Service", "Order DB"), ("Order Service", "Cart Service"), ("Order Service", "Payment Service"),
            ("Order Service", "Inventory Service"), ("Order Service", "Shipping Service"), ("Order Service", "Order Queue"),
            ("Order Service", "Invoice Storage"),
            ("Payment Service", "Payment DB"), ("Payment Service", "Payment Gateway API"),
            ("Shipping Service", "Shipping DB"), ("Shipping Service", "Shipping Carrier API"), ("Shipping Service", "Maps API"),
            ("Inventory Service", "Inventory DB"),
            ("Pricing Service", "Promotion Service"),
            ("Review Service", "Review DB"), ("Review Service", "User Service"),
            ("Recommendation Service", "Analytics Service"), ("Recommendation Service", "Product Cache"),
            ("Search Service", "Search Index"),
            ("Notification Service", "Email Queue"), ("Notification Service", "SMS Provider API"),
            ("Email Queue", "Email Service"),
            ("Promotion Service", "Promotion DB"),
            ("Wishlist Service", "User Service"), ("Wishlist Service", "Product Service"),
            ("Analytics Service", "Analytics DB"), ("Analytics Service", "Event Bus"),
            ("Order Queue", "Event Bus"), ("Order Queue", "Notification Service"),
            ("Order DB", "Backup Storage"), ("Payment DB", "Backup Storage"),
            ("Log Service", "Log Archive"), ("Tracing Service", "Log Service"),
            ("Metrics Service", "Alert Manager"), ("Alert Manager", "Notification Service"),
            ("Order Service", "Log Service"), ("Payment Service", "Log Service"), ("Auth Service", "Metrics Service"),
            ("API Gateway", "Tracing Service"),
        };

        // Filas del diseño en el canvas (por capas de la arquitectura)
        var rows = new[]
        {
            new[] { "API Gateway" },
            new[] { "Load Balancer Norte", "Load Balancer Sur", "Tracing Service", "Metrics Service", "Alert Manager" },
            new[] { "Auth Service", "User Service", "Product Service", "Catalog Service", "Cart Service", "Order Service", "Payment Service", "Shipping Service", "Inventory Service" },
            new[] { "Pricing Service", "Review Service", "Recommendation Service", "Search Service", "Notification Service", "Email Service", "Promotion Service", "Wishlist Service", "Analytics Service" },
            new[] { "Session Cache", "Product Cache", "Order Queue", "Event Bus", "Email Queue", "Payment Gateway API", "Shipping Carrier API", "SMS Provider API", "Maps API" },
            new[] { "Auth DB", "User DB", "Product DB", "Cart DB", "Order DB", "Payment DB", "Shipping DB", "Inventory DB", "Review DB" },
            new[] { "Search Index", "Promotion DB", "Analytics DB", "Image Storage", "Invoice Storage", "Backup Storage", "Log Service", "Log Archive" },
        };

        return BuildTopology("Topología Grande (50 nodos)", nodes, edges, rows);
    }

    /// <summary>
    /// Topología compleja: 100 nodos — arquitectura empresarial multirregión
    /// con 10 servicios globales y tres regiones de 30 nodos cada una.
    /// </summary>
    private static GraphModel CreateComplexTopology()
    {
        var nodes = new List<(string Name, NodeType Type)>
        {
            ("Global DNS", NodeType.API), ("CDN Edge", NodeType.Storage), ("Global API Gateway", NodeType.API),
            ("Identity Provider", NodeType.Service), ("Global Config Service", NodeType.Service),
            ("Global Billing Service", NodeType.Service), ("Global Billing DB", NodeType.Database),
            ("Global Monitoring", NodeType.Service), ("Global Log Archive", NodeType.Storage),
            ("Disaster Recovery Storage", NodeType.Storage),
        };
        var edges = new List<(string From, string To)>
        {
            ("Global DNS", "CDN Edge"), ("Global DNS", "Global API Gateway"),
            ("Global API Gateway", "Identity Provider"),
            ("Global Billing Service", "Global Billing DB"),
            ("Global Monitoring", "Global Log Archive"),
            ("Global Billing DB", "Disaster Recovery Storage"),
        };
        var rows = new List<string[]>
        {
            new[] { "Global DNS", "CDN Edge", "Global API Gateway", "Identity Provider", "Global Config Service" },
            new[] { "Global Billing Service", "Global Billing DB", "Global Monitoring", "Global Log Archive", "Disaster Recovery Storage" },
        };

        // Plantilla de región: 30 nodos
        var serviceNames = new[]
        {
            "Auth", "Account", "Catalog", "Order", "Payment", "Inventory",
            "Shipping", "Notification", "Search", "Report", "Pricing", "Audit"
        };
        var dbNames = new[] { "Account DB", "Catalog DB", "Order DB", "Payment DB", "Inventory DB", "Shipping DB", "Search Index", "Audit DB" };

        foreach (var region in new[] { "R1", "R2", "R3" })
        {
            string N(string baseName) => $"{baseName} ({region})";
            string S(string svc) => N($"{svc} Service");

            nodes.Add((N("Load Balancer"), NodeType.LoadBalancer));
            foreach (var svc in serviceNames) nodes.Add((S(svc), NodeType.Service));
            foreach (var db in dbNames) nodes.Add((N(db), NodeType.Database));
            foreach (var c in new[] { "Session Cache", "Catalog Cache", "Rate Limiter" }) nodes.Add((N(c), NodeType.Service));
            foreach (var q in new[] { "Order Queue", "Event Stream" }) nodes.Add((N(q), NodeType.Service));
            foreach (var st in new[] { "Object Storage", "Report Storage" }) nodes.Add((N(st), NodeType.Storage));
            foreach (var o in new[] { "Log Collector", "Metrics Agent" }) nodes.Add((N(o), NodeType.Service));

            // Entrada a la región
            edges.Add(("Global API Gateway", N("Load Balancer")));
            edges.Add((N("Load Balancer"), N("Rate Limiter")));
            edges.Add((N("Load Balancer"), "Global Config Service"));
            foreach (var svc in serviceNames) edges.Add((N("Load Balancer"), S(svc)));

            // Servicios → datos y otros servicios
            edges.AddRange(new[]
            {
                (S("Auth"), "Identity Provider"), (S("Auth"), N("Session Cache")),
                (S("Account"), N("Account DB")), (S("Account"), S("Auth")),
                (S("Catalog"), N("Catalog DB")), (S("Catalog"), N("Catalog Cache")), (S("Catalog"), N("Object Storage")),
                (S("Order"), N("Order DB")), (S("Order"), S("Payment")), (S("Order"), S("Inventory")),
                (S("Order"), S("Shipping")), (S("Order"), N("Order Queue")), (S("Order"), S("Pricing")),
                (S("Payment"), N("Payment DB")), (S("Payment"), "Global Billing Service"),
                (S("Inventory"), N("Inventory DB")),
                (S("Shipping"), N("Shipping DB")),
                (S("Notification"), N("Event Stream")),
                (S("Search"), N("Search Index")), (S("Search"), N("Catalog Cache")),
                (S("Report"), N("Report Storage")), (S("Report"), N("Order DB")),
                (S("Pricing"), S("Catalog")),
                (S("Audit"), N("Audit DB")),
                (N("Order Queue"), N("Event Stream")), (N("Order Queue"), S("Notification")),
                (N("Event Stream"), S("Audit")),
                (N("Order DB"), "Disaster Recovery Storage"),
                (N("Log Collector"), "Global Monitoring"), (N("Metrics Agent"), "Global Monitoring"),
                (S("Order"), N("Log Collector")), (S("Payment"), N("Log Collector")),
                (N("Load Balancer"), N("Metrics Agent")),
            });

            rows.Add(new[] { N("Load Balancer"), N("Rate Limiter"), N("Session Cache"), N("Catalog Cache"), N("Order Queue"), N("Event Stream"), N("Log Collector"), N("Metrics Agent") });
            rows.Add(serviceNames.Take(6).Select(S).ToArray());
            rows.Add(serviceNames.Skip(6).Select(S).ToArray());
            rows.Add(dbNames.Select(N).Concat(new[] { N("Object Storage"), N("Report Storage") }).ToArray());
        }

        return BuildTopology("Topología Compleja (100 nodos)", nodes, edges, rows);
    }

    /// <summary>
    /// Construye un <see cref="GraphModel"/> a partir de listas de nodos,
    /// dependencias (por nombre) y filas de diseño para el canvas.
    /// </summary>
    private static GraphModel BuildTopology(string name,
                                            IEnumerable<(string Name, NodeType Type)> nodes,
                                            IEnumerable<(string From, string To)> edges,
                                            IEnumerable<string[]> rows)
    {
        var graph  = new GraphModel { Name = name };
        var byName = new Dictionary<string, Vertex>();
        foreach (var (nodeName, type) in nodes)
        {
            var v = new Vertex { Name = nodeName, NodeType = type };
            graph.Vertices.Add(v);
            byName.Add(nodeName, v);
        }

        foreach (var (from, to) in edges)
        {
            var e = new Edge { SourceId = byName[from].Id, TargetId = byName[to].Id };
            if (!graph.EdgeExists(e.SourceId, e.TargetId))
                graph.Edges.Add(e);
        }

        var rowList = rows.ToList();
        int maxCols = rowList.Max(r => r.Length);
        for (int r = 0; r < rowList.Count; r++)
        {
            var row    = rowList[r];
            double off = (maxCols - row.Length) * LayoutSpacingX / 2;
            for (int c = 0; c < row.Length; c++)
            {
                var v = byName[row[c]];
                v.PositionX = LayoutMargin + off + c * LayoutSpacingX;
                v.PositionY = LayoutMargin + r * LayoutSpacingY;
            }
        }
        return graph;
    }
}
