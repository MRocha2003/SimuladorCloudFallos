using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using SimuladorCloud.Client.Services;

namespace SimuladorCloud.Tests.Unit;

/// <summary>
/// Verifica que los tipos y miembros públicos de los servicios y modelos
/// tengan documentación XML (IT-8 · HU-16, criterio 1; PT-35).
/// </summary>
public class XmlDocumentationTests
{
    private static readonly string[] Namespaces = { "SimuladorCloud.Client.Models", "SimuladorCloud.Client.Services" };

    private static HashSet<string> LoadDocumentedMembers()
    {
        var assembly = typeof(GraphService).Assembly;
        var xmlPath  = Path.ChangeExtension(assembly.Location, ".xml");
        Assert.True(File.Exists(xmlPath), $"No se encontró la documentación XML en {xmlPath}. Revise GenerateDocumentationFile.");
        return XDocument.Load(xmlPath).Descendants("member")
                        .Select(m => (string)m.Attribute("name")!)
                        .ToHashSet();
    }

    private static IEnumerable<Type> PublicTypes() =>
        typeof(GraphService).Assembly.GetTypes()
            .Where(t => t.IsPublic && Namespaces.Contains(t.Namespace)
                        && t.GetCustomAttribute<CompilerGeneratedAttribute>() is null);

    [Fact]
    public void TiposPublicos_EstanDocumentados()
    {
        var docs = LoadDocumentedMembers();

        var missing = PublicTypes().Where(t => !docs.Contains($"T:{t.FullName}")).Select(t => t.FullName).ToList();

        Assert.True(missing.Count == 0, "Tipos sin documentar: " + string.Join(", ", missing));
    }

    [Fact]
    public void MiembrosPublicos_EstanDocumentados()
    {
        var docs    = LoadDocumentedMembers();
        var missing = new List<string>();
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        bool Has(string prefix, string name) =>
            docs.Contains(prefix + name) || docs.Any(d => d.StartsWith(prefix + name + "(", StringComparison.Ordinal));

        foreach (var type in PublicTypes())
        {
            // Los miembros de un tipo con documentación heredada (<inheritdoc/>) cuentan como documentados.
            foreach (var p in type.GetProperties(flags))
                if (!Has("P:", $"{type.FullName}.{p.Name}")) missing.Add($"{type.Name}.{p.Name}");

            foreach (var m in type.GetMethods(flags).Where(m => !m.IsSpecialName))
                if (!Has("M:", $"{type.FullName}.{m.Name}")) missing.Add($"{type.Name}.{m.Name}()");

            foreach (var f in type.GetFields(flags).Where(f => !f.IsSpecialName))
                if (!Has("F:", $"{type.FullName}.{f.Name}")) missing.Add($"{type.Name}.{f.Name}");

            foreach (var e in type.GetEvents(flags))
                if (!Has("E:", $"{type.FullName}.{e.Name}")) missing.Add($"{type.Name}.{e.Name}");

            foreach (var c in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).Where(c => c.GetParameters().Length > 0))
                if (!Has("M:", $"{type.FullName}.#ctor")) missing.Add($"{type.Name}.#ctor");
        }

        Assert.True(missing.Count == 0, "Miembros sin documentar: " + string.Join(", ", missing.Distinct()));
    }
}
