using CompanyWars.Content;
using CompanyWars.Tools;

namespace CompanyWars.Sim.Tests;

/// <summary>The real content database, loaded once per test run.</summary>
public static class TestContent
{
    public static string Root { get; } = RepoRoot.Find(AppContext.BaseDirectory);

    private static readonly Lazy<ContentDb> Lazy = new(() => ContentLoader.Load(Root));

    public static ContentDb Db => Lazy.Value;
}
