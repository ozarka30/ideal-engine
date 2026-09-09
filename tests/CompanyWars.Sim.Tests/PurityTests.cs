using System.Reflection;
using System.Text.RegularExpressions;
using Xunit;

namespace CompanyWars.Sim.Tests;

/// <summary>The dependency rule and the determinism contract, enforced by reflection (ARCHITECTURE.md §1).</summary>
public class PurityTests
{
    private static readonly Assembly Sim = typeof(Simulator).Assembly;

    [Fact]
    public void SimReferencesOnlyTheBaseLibrary()
    {
        foreach (AssemblyName r in Sim.GetReferencedAssemblies())
        {
            Assert.True(r.Name == "System.Runtime" || r.Name == "netstandard" || r.Name!.StartsWith("System.", StringComparison.Ordinal),
                $"CompanyWars.Sim references {r.Name}; it may reference nothing but the .NET base library");
        }
    }

    [Fact]
    public void SimUsesNoFloatingPointInAnyMember()
    {
        var banned = new HashSet<Type> { typeof(float), typeof(double), typeof(decimal), typeof(Half) };
        var offenders = new List<string>();
        foreach (Type t in Sim.GetTypes())
        {
            const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (FieldInfo f in t.GetFields(all))
            {
                if (banned.Contains(f.FieldType)) offenders.Add($"{t.Name}.{f.Name}");
            }
            foreach (PropertyInfo p in t.GetProperties(all))
            {
                if (banned.Contains(p.PropertyType)) offenders.Add($"{t.Name}.{p.Name}");
            }
            foreach (MethodInfo m in t.GetMethods(all))
            {
                if (banned.Contains(m.ReturnType)) offenders.Add($"{t.Name}.{m.Name}()");
                foreach (ParameterInfo pi in m.GetParameters())
                {
                    if (banned.Contains(pi.ParameterType)) offenders.Add($"{t.Name}.{m.Name}({pi.Name})");
                }
            }
        }
        Assert.Empty(offenders);
    }

    [Fact]
    public void SimSourceContainsNoFloatingPointKeywords()
    {
        string dir = Path.Combine(TestContent.Root, "src", "CompanyWars.Sim");
        var pattern = new Regex(@"\b(double|float|decimal|Half|Math\.Pow|Math\.Sqrt|System\.Random|DateTime|Stopwatch)\b");
        var offenders = new List<string>();
        foreach (string file in Directory.GetFiles(dir, "*.cs"))
        {
            int line = 0;
            foreach (string raw in File.ReadLines(file))
            {
                line++;
                string code = raw.Trim();
                if (code.StartsWith("//", StringComparison.Ordinal) || code.StartsWith("///", StringComparison.Ordinal)) continue;
                if (pattern.IsMatch(code)) offenders.Add($"{Path.GetFileName(file)}:{line}: {code}");
            }
        }
        Assert.Empty(offenders);
    }
}
